module Hedgehog.Tests.PropertyBindTests

open System
open Hedgehog
open Hedgehog.FSharp
open Expecto

/// Tests to validate that Property.bind (via CE's let!) preserves semantics:
/// - Laziness: property test assertions not executed during tree construction
/// - Shrinking: failures shrink to minimal counterexamples  
/// - Failure short-circuiting: bind doesn't call continuation for failures
let propertyBindTests = testList "Property.bind semantics" [
    testCase "sync bind defers test assertions until tree outcome is accessed (CE syntax)" <| fun () ->
        let mutable assertionExecuted = false
        
        // Create a property where the test assertion has a side effect
        let prop = property {
            let! x = Gen.constant 42
            // This assertion should not execute during tree construction
            assertionExecuted <- true
            return x = 42
        }
        
        // Build the tree (generation phase) - should not execute assertions yet
        let seed = Seed.from 12345UL
        let tree = prop |> Property.toGen |> Gen.toRandom |> Random.run seed 10
        
        // Tree construction does NOT force the lazy (unwrapSync creates a new lazy wrapper)
        Expect.isFalse assertionExecuted "Assertion should not execute during tree construction"
        
        // Only when we force the outcome lazy does the assertion execute
        let lazyResult = Tree.outcome tree
        let _ = lazyResult.Value
        Expect.isTrue assertionExecuted "Assertion should execute when lazy is forced"

    testCase "async bind defers async execution until property is run (CE syntax)" <| fun () ->
        let mutable asyncStarted = false
        
        // Create an async property
        let prop = property {
            do! async {
                asyncStarted <- true
                return ()
            }
            return true
        }
        
        // Build the tree (generation phase) - async should not start
        let seed = Seed.from 12345UL
        let tree = prop |> Property.toGen |> Gen.toRandom |> Random.run seed 10
        
        // Async should still not have started (tree construction doesn't await asyncs)
        Expect.isFalse asyncStarted "Async should not start during tree construction"
        
        // When we force the lazy via unwrapSync, it will call Async.RunSynchronously
        // (because toGen uses unwrapSync which blocks on async)
        let lazyResult = Tree.outcome tree
        let _ = lazyResult.Value
        
        // Async HAS started because unwrapSync calls Async.RunSynchronously
        Expect.isTrue asyncStarted "Async executes (and blocks) when toGen unwraps it via Async.RunSynchronously"

    testCase "sync bind preserves shrinking - failures shrink to minimal case (CE syntax)" <| fun () ->
        // Use CE syntax with multiple let! binds
        let prop = property {
            let! x = Range.linear 0 100 |> Gen.int32
            let! y = Range.linear 0 100 |> Gen.int32
            return y < 50 // Will fail for larger values
        }
        
        let report = prop |> Property.reportBool
        
        match report.Status with
        | Failed failure ->
            // Should shrink to minimal failing case
            Expect.isTrue (int failure.Shrinks > 0) "Should perform shrinks"
        | _ -> failwith "Expected failure"

    testCase "sync bind with Failure short-circuits without calling continuation (CE syntax)" <| fun () ->
        let mutable continuationCalled = false
        
        // Use proper CE syntax - let! after a failure
        let prop = property {
            let! _ = Property.failure
            continuationCalled <- true
            return true
        }
        
        let report = prop |> Property.reportBool
        
        Expect.isFalse continuationCalled "Continuation should not be called for Failure"
        match report.Status with
        | Failed _ -> ()
        | _ -> failwith "Expected failure"

    testCase "async bind with Failure short-circuits without calling continuation (CE syntax)" <| fun () ->
        let mutable continuationCalled = false
        
        // Use CE syntax with async that fails, followed by continuation
        let prop = property {
            do! async {
                return failwith "test failure"
            }
            continuationCalled <- true
            return true
        }
        
        let report = prop |> Property.reportBool
        
        Expect.isFalse continuationCalled "Continuation should not be called for async Failure"
        match report.Status with
        | Failed _ -> ()
        | _ -> failwith "Expected failure"

    // ============================================================================
    // Shrinking tests: MUST preserve full shrinking for async properties
    // ============================================================================

    testCase "async property with single gen MUST shrink fully" <| fun () ->
        // An async property with a failing generator should shrink just like sync
        let prop = property {
            let! x = Range.linear 0 100 |> Gen.int32
            do! async { return () }
            return x < 50
        }
        
        let report = prop |> Property.reportBoolAsync |> Async.RunSynchronously
        
        match report.Status with
        | Failed failure ->
            // Should shrink to minimal failing case (x = 50)
            // This requires ~50 shrinks
            Expect.isTrue ((int failure.Shrinks) > 40) 
                "Async property MUST shrink x from 100 down to 50, requiring many shrinks"
        | _ -> failwith "Expected failure"

    testCase "async bind with two gens MUST preserve shrinking for both" <| fun () ->
        // Two generators in an async property should both shrink
        let prop = property {
            let! x = Range.linear 0 100 |> Gen.int32
            do! async { return () }
            let! y = Range.linear 0 100 |> Gen.int32
            return x + y < 100
        }
        
        let report = prop |> Property.reportBoolAsync |> Async.RunSynchronously
        
        match report.Status with
        | Failed failure ->
            // Should shrink both x and y to minimal failing case
            // For x + y < 100, minimal is x=50, y=50 (or similar)
            // This requires shrinking both variables
            Expect.isTrue ((int failure.Shrinks) > 50) 
                "Both generators MUST shrink, requiring many shrink steps"
        | _ -> failwith "Expected failure"

    testCase "async bind that returns Gen MUST preserve full shrinking" <| fun () ->
        // Binding an async that produces a Gen should preserve that Gen's shrinking
        let prop = property {
            let! x = Range.linear 0 100 |> Gen.int32
            let! genY = async { return Range.linear 0 100 |> Gen.int32 }
            let! y = genY
            return x + y < 100
        }
        
        let report = prop |> Property.reportBoolAsync |> Async.RunSynchronously
        
        match report.Status with
        | Failed failure ->
            // The Gen returned from async should shrink fully
            Expect.isTrue ((int failure.Shrinks) > 50)
                "Gen produced by async bind MUST have full shrinking capability"
        | _ -> failwith "Expected failure"

    testCase "deeply nested async binds MUST preserve shrinking" <| fun () ->
        // Multiple levels of async binding should all preserve shrinking
        let prop = property {
            let! x = Range.linear 0 50 |> Gen.int32
            do! async { return () }
            let! y = Range.linear 0 50 |> Gen.int32
            do! async { return () }
            let! z = Range.linear 0 50 |> Gen.int32
            return x + y + z < 100
        }
        
        let report = prop |> Property.reportBoolAsync |> Async.RunSynchronously
        
        match report.Status with
        | Failed failure ->
            // All three variables should shrink to minimal failing case
            Expect.isTrue ((int failure.Shrinks) > 30)
                "All generators across multiple async boundaries MUST shrink"
        | _ -> failwith "Expected failure"
]
