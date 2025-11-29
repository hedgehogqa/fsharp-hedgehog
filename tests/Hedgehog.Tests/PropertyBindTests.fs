module Hedgehog.Tests.PropertyBindTests

open System
open Hedgehog
open Hedgehog.FSharp
open Expecto

/// Tests to validate that Property.bind preserves the semantics from before async support:
/// - Laziness: property results (lazy Journal * Outcome) not forced during tree construction
/// - Shrinking: failures shrink to minimal counterexamples  
/// - Failure short-circuiting: bind doesn't call continuation for failures
let propertyBindTests = testList "Property.bind semantics" [
    testCase "sync bind preserves laziness - lazy result not forced during tree construction" <| fun () ->
        let mutable lazyForced = false
        
        // Create property with explicit lazy that tracks when forced
        let prop =
            Property.success ()
            |> Property.bind (fun () ->
                Gen.constant (lazy (
                    lazyForced <- true
                    PropertyResult.Sync (Journal.empty, Success ())
                )) |> Property)
        
        // Build the tree (generation phase)
        let seed = Seed.from 12345UL
        let tree = prop |> Property.toGen |> Gen.toRandom |> Random.run seed 10
        
        // Lazy should not be forced during tree construction
        Expect.isFalse lazyForced "Lazy should not be forced during tree construction"
        
        // Now force the lazy by accessing the outcome
        let lazyResult = Tree.outcome tree
        let _ = lazyResult.Value
        
        // Now lazy should have been forced
        Expect.isTrue lazyForced "Lazy should be forced when result is accessed"

    testCase "async bind preserves laziness - async not started during tree construction" <| fun () ->
        let mutable asyncStarted = false
        
        let prop =
            Property.ofAsync (async {
                asyncStarted <- true
                return ()
            })
            |> Property.bind (fun () ->
                Property.success ())
        
        // Build the tree (generation phase) - toGenInternal keeps PropertyResult, not available publicly
        // So just verify async doesn't start until property is run
        Expect.isFalse asyncStarted "Async should not start during property construction"
        
        // Run the property - this will execute the async
        let _ = prop |> Property.report
        
        // Now async should have run
        Expect.isTrue asyncStarted "Async should execute when property is run"

    testCase "sync bind preserves shrinking - failures shrink to minimal case" <| fun () ->
        let prop =
            property {
                let! x = Range.linear 0 100 |> Gen.int32
                return ()
            }
            |> Property.bind (fun () ->
                property {
                    let! y = Range.linear 0 100 |> Gen.int32
                    return y < 50 // Will fail for larger values
                })
        
        let report = prop |> Property.reportBool
        
        match report.Status with
        | Failed failure ->
            // Should shrink to minimal failing case
            Expect.isTrue (int failure.Shrinks > 0) "Should perform shrinks"
        | _ -> failwith "Expected failure"

    testCase "sync bind with Failure short-circuits without calling continuation" <| fun () ->
        let mutable continuationCalled = false
        
        let prop =
            Property.failure
            |> Property.bind (fun () ->
                continuationCalled <- true
                Property.success ())
        
        let report = prop |> Property.report
        
        Expect.isFalse continuationCalled "Continuation should not be called for Failure"
        match report.Status with
        | Failed _ -> ()
        | _ -> failwith "Expected failure"

    testCase "async bind with Failure short-circuits without calling continuation" <| fun () ->
        let mutable continuationCalled = false
        
        let prop =
            Property.ofAsync (async {
                return failwith "test failure"
            })
            |> Property.bind (fun () ->
                continuationCalled <- true
                Property.success ())
        
        let report = prop |> Property.report
        
        Expect.isFalse continuationCalled "Continuation should not be called for async Failure"
        match report.Status with
        | Failed _ -> ()
        | _ -> failwith "Expected failure"
]
