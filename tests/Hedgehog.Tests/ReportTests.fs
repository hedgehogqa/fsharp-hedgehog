module Hedgehog.Tests.ReportTests

open Hedgehog

open TestDsl


let reportTests = testList "Report tests" [
    testCase "Roundtrip RecheckData serialization" <| fun () ->
        property {
            let! size = Range.linear 0 1000 |> Gen.int32
            let expected = {
                Size = size
                Seed = Seed.random () }
            let actual =
                expected
                |> RecheckData.serialize
                |> RecheckData.deserialize
            actual =! expected
        }
        |> Property.check
]
