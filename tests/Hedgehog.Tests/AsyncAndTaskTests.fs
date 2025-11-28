module Hedgehog.Tests.PropertyAsyncAndTaskTests

open Hedgehog
open Hedgehog.FSharp
open TestDsl

#if !FABLE_COMPILER
let asyncAndTaskTests = testList "Property async tests" [

    testCase "async and task work correctly" <| fun () ->
        let property =
             property {
                let! x = Gen.int32 (Range.constant 0 100)
                let! y = async { return x + 1 }
                let! z = task { return y + 1 }
                return z = x + 2
             }

        property |> Property.reportBool |> Report.tryRaise

    testCase "async property can return async" <| fun () ->
        // Test that async properties can be created and executed
        let property =
            property {
                let! x = Gen.int32 (Range.constant 0 100)
                let! y = task { return x + 1 }
                return! async { return y = x + 1 }
            }
        property |> Property.reportBool |> Report.tryRaise

    testCase "async property can return task" <| fun () ->
        // Test that async properties can be created and executed
        let property =
            property {
                let! x = Gen.int32 (Range.constant 0 100)
                let! y = async { return x + 1 }
                return! task { return y = x + 1 }
            }
        property |> Property.reportBool |> Report.tryRaise
    
    testCase "async property can fail async" <| fun () ->
        // Test that async properties can be created, executed, and shrunk
        let property =
            property {
                let! x = Gen.int32 (Range.constant 0 100)
                return! async {
                    do! Async.Sleep 10  // Simulate async work
                    if x > 50 then
                        failwith $"Value {x} is too large"  // This will fail and shrink
                }
            }
        let report =
            property
            |> Property.report
        
        match report.Status with
        | Failed failure -> 
            // Verify it failed and shrunk to minimal case (should be 51)
            if failure.Shrinks <= 0<shrinks> then
                failwith "Should have shrunk to find minimal failing case"
            printfn $"Successfully found failing case after %A{failure.Shrinks} shrinks"
        | _ -> 
            failwith "Expected property to fail (x > 50 should cause failure)"
]
#endif
