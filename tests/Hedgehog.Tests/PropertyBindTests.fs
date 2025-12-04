module Hedgehog.Tests.PropertyBindTests

open Hedgehog
open Hedgehog.FSharp
open TestDsl

/// Tests to validate that Property.bind (via CE's let!) preserves semantics:
/// - Laziness: property test assertions not executed during tree construction.
/// - Shrinking: failures shrink to minimal counterexamples.
/// - Failure short-circuiting: bind doesn't call continuation for failures.
let propertyBindTests = testList "Property.bind semantics" [
    testCase "sync bind defers test assertions until tree outcome is accessed (CE syntax)" <| fun () ->
        let mutable assertionExecuted = false
        
        // Create a property where the test assertion has a side effect.
        let prop = property {
            let! x = Gen.constant 42
            // This assertion should not execute during tree construction.
            assertionExecuted <- true
            return x = 42
        }
        
        // Build the tree (generation phase) - should not execute assertions yet.
        let seed = Seed.from 12345UL
        let tree = prop |> Property.toGen |> Gen.toRandom |> Random.run seed 10
        
        // Tree construction does NOT force the lazy (unwrapSync creates a new lazy wrapper).
        Expect.isFalse assertionExecuted
        
        // Only when we force the outcome lazy does the assertion execute.
        let lazyResult = Tree.outcome tree
        let _ = lazyResult.Value
        Expect.isTrue assertionExecuted

    fableIgnore "async bind defers async execution until property is run (CE syntax)" <| fun () ->
        let mutable asyncStarted = false
        
        // Create an async property.
        let prop = property {
            do! async {
                asyncStarted <- true
                return ()
            }
            return true
        }
        
        // Build the tree (generation phase) - async should not start.
        let seed = Seed.from 12345UL
        let tree = prop |> Property.toGen |> Gen.toRandom |> Random.run seed 10
        
        // Async should still not have started (tree construction doesn't await asyncs).
        Expect.isFalse asyncStarted
        
        // When we force the lazy via unwrapSync, it will call Async.RunSynchronously.
        // (because toGen uses unwrapSync which blocks on async).
        let lazyResult = Tree.outcome tree
        let _ = lazyResult.Value
        
        // Async HAS started because unwrapSync calls Async.RunSynchronously.
        Expect.isTrue asyncStarted

    testCase "sync bind preserves shrinking - failures shrink to minimal case (CE syntax)" <| fun () ->
        let mutable finalX = 0
        let mutable finalY = 0
        
        let prop = property {
            let! x = Range.exponential 1 100 |> Gen.int32
            let! y = Range.exponential 1 100 |> Gen.int32
            let! _ = Property.counterexample (fun () -> $"x = {x}, y = {y}")
            // This will fail when y >= 5, should shrink towards y=5, x=1
            if y < 5 then
                return true
            else
                // Capture values only when failing
                finalX <- x
                finalY <- y
                return false
        }
        
        let report = prop |> Property.reportBool
        
        match report.Status with
        | Failed failure ->
            // After shrinking completes, finalX and finalY contain the minimal counterexample
            let journalLines = failure.Journal |> Journal.eval |> List.ofSeq
            
            // Verify we have the expected journal structure
            let counterexamples = 
                journalLines 
                |> List.choose (function Counterexample msg -> Some msg | _ -> None)
            
            // The shrunk counterexample should have y=5 (minimal failing value)
            // and x should be shrunk towards 1 (the origin of the range)
            Expect.equal finalY 5 $"y should shrink to minimal failing value of 5. Journal: {counterexamples}"
        | _ -> failwith "Expected failure"

    fableIgnoreAsync "async bind preserves shrinking - same as sync test but with async" <| async {
        let mutable finalY = 0
        
        let prop = property {
            let! x = Range.exponential 0 100 |> Gen.int32
            do! async { return () }
            let! y = Range.exponential 0 100 |> Gen.int32
            let! _ = Property.counterexample (fun () -> $"x = {x}, y = {y}")
            if y < 50 then
                return true
            else
                finalY <- y
                return false
        }
        
        let! report = prop |> Property.reportBoolAsync
        
        match report.Status with
        | Failed _ -> finalY =! 50
        | _ -> failwith "Expected failure"
    }

    testCase "sync bind with Failure short-circuits without calling continuation (CE syntax)" <| fun () ->
        let mutable continuationCalled = false
        
        // Use proper CE syntax - let! after a failure.
        let prop = property {
            let! _ = Property.failure
            continuationCalled <- true
            return true
        }
        
        let report = prop |> Property.reportBool
        
        Expect.isFalse continuationCalled
        match report.Status with
        | Failed _ -> ()
        | _ -> failwith "Expected failure"

    testCaseAsync "async bind with Failure short-circuits without calling continuation (CE syntax)" <| async {
        let mutable continuationCalled = false
        
        // Use CE syntax with async that fails, followed by continuation.
        let prop = property {
            do! async {
                return failwith "test failure"
            }
            continuationCalled <- true
            return true
        }
        
        let! report = prop |> Property.reportBoolAsync
        
        Expect.isFalse continuationCalled
        match report.Status with
        | Failed _ -> ()
        | _ -> failwith "Expected failure"
    }

    // ============================================================================
    // Blocking behavior tests
    // ============================================================================

    fableIgnoreAsync "interleaved async (gen-async-gen) MUST preserve shrinking after fix" <| async {
        let mutable finalY = 0
        
        let prop = property {
            let! x = Range.exponential 0 100 |> Gen.int32
            do! async { return () }
            let! y = Range.exponential 0 100 |> Gen.int32
            let! _ = Property.counterexample (fun () -> $"x = {x}, y = {y}")
            if y < 50 then
                return true
            else
                finalY <- y
                return false
        }
        
        let! report = prop |> Property.reportBoolAsync
        
        match report.Status with
        | Failed _ -> finalY =! 50
        | _ -> failwith "Expected failure"
    }

    // ============================================================================
    // Shrinking tests: MUST preserve full shrinking for async properties
    // ============================================================================

    testCaseAsync "async property with single gen MUST shrink fully" <| async {
        let mutable finalX = 0
        
        let prop = property {
            let! x = Range.exponential 0 100 |> Gen.int32
            let! _ = Property.counterexample (fun () -> $"x = {x}")
            do! async { return () }
            if x < 50 then
                return true
            else
                finalX <- x
                return false
        }
        
        let! report = prop |> Property.reportBoolAsync
        
        match report.Status with
        | Failed _ -> finalX =! 50
        | _ -> failwith "Expected failure"
    }

    fableIgnoreAsync "async bind with two gens MUST preserve shrinking for both" <| async {
        let mutable finalY = 0
        
        let prop = property {
            let! x = Range.exponential 0 100 |> Gen.int32
            do! async { return () }
            let! y = Range.exponential 0 100 |> Gen.int32
            let! _ = Property.counterexample (fun () -> $"x = {x}, y = {y}")
            if y < 50 then
                return true
            else
                finalY <- y
                return false
        }
        
        let! report = prop |> Property.reportBoolAsync
        
        match report.Status with
        | Failed _ -> finalY =! 50
        | _ -> failwith "Expected failure"
    }

    fableIgnoreAsync "async bind that returns Gen MUST preserve full shrinking" <| async {
        let mutable finalY = 0
        
        let prop = property {
            let! x = Range.exponential 0 100 |> Gen.int32
            let! genY = async { return Range.exponential 0 100 |> Gen.int32 }
            let! y = genY
            let! _ = Property.counterexample (fun () -> $"x = {x}, y = {y}")
            if y < 50 then
                return true
            else
                finalY <- y
                return false
        }
        
        let! report = prop |> Property.reportBoolAsync
        
        match report.Status with
        | Failed _ -> finalY =! 50
        | _ -> failwith "Expected failure"
    }

    fableIgnoreAsync "deeply nested async binds MUST preserve shrinking" <| async {
        let mutable finalZ = 0
        
        let prop = property {
            let! x = Range.exponential 0 50 |> Gen.int32
            do! async { return () }
            let! y = Range.exponential 0 50 |> Gen.int32
            do! async { return () }
            let! z = Range.exponential 0 50 |> Gen.int32
            let! _ = Property.counterexample (fun () -> $"x = {x}, y = {y}, z = {z}")
            if z < 25 then
                return true
            else
                finalZ <- z
                return false
        }
        
        let! report = prop |> Property.reportBoolAsync
        
        match report.Status with
        | Failed _ -> finalZ =! 25
        | _ -> failwith "Expected failure"
    }
]
