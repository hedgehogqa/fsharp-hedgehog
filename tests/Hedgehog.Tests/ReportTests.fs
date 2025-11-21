module Hedgehog.Tests.ReportTests

open Hedgehog
open Hedgehog.FSharp
open TestDsl


let reportTests = testList "Report tests" [
    testCase "Roundtrip RecheckData serialization" <| fun () ->
        property {
            let! size = Range.linear 0 1000 |> Gen.int32
            let! path =
                Range.linear 0 3
                |> Gen.int32
                |> Gen.map ShrinkOutcome.Pass
                |> Gen.list (Range.linear 0 10)
            let expected = {
                Size = size
                Seed = Seed.random ()
                ShrinkPath = path }
            let actual =
                expected
                |> RecheckData.serialize
                |> RecheckData.deserialize
            actual =! expected
        }
        |> Property.check
]
