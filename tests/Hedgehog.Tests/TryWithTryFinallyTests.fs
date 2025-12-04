module Hedgehog.Tests.TryWithTryFinallyTests

open Hedgehog
open Hedgehog.FSharp
open TestDsl

// Simulate fluent assertion library for testing
type AssertionResult = { Success: bool }
let shouldBe expected actual =
    if actual = expected then { Success = true }
    else failwithf $"Expected %d{expected} but got %d{actual}"

/// Tests for Property.tryFinally and Property.ignoreResult
let tryWithTryFinallyTests = testList "Property.tryFinally and ignoreResult tests" [
    
    testCase "tryFinally cleanup runs on success" <| fun () ->
        let mutable cleanupCalled = false
        property {
            let! x = Gen.constant 42
            return x = 42
        }
        |> Property.tryFinally (fun () -> cleanupCalled <- true)
        |> Property.checkBool

        Expect.isTrue cleanupCalled
    
    testCase "tryFinally cleanup runs on failure" <| fun () ->
        let mutable cleanupCalled = false
        let report =
            property {
                let! x = Gen.constant 42
                return false
            }
            |> Property.tryFinally (fun () -> cleanupCalled <- true)
            |> Property.reportBool

        match report.Status with
        | Failed _ -> ()
        | _ -> failwith "Expected Failed status"

        Expect.isTrue cleanupCalled
    
    testCase "ignoreResult converts Property<'a> to Property<unit>" <| fun () ->
        let mutable testRan = false
        property {
            let! x = Gen.constant 42
            testRan <- true
            return shouldBe 42 x  // Returns AssertionResult
        }
        |> Property.ignoreResult  // Convert to Property<unit>
        |> Property.check

    
    testCase "ignoreResult preserves failures" <| fun () ->
        let report =
            property {
                let! x = Gen.constant 42
                return shouldBe 99 x  // Will throw
            }
            |> Property.ignoreResult
            |> Property.report

        match report.Status with
        | Failed _ -> ()
        | _ -> failwith "Expected Failed status"
]
