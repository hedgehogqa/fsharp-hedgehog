module Hedgehog.Tests.PropertyTests

open Hedgehog
open Expecto
open TestDsl

let propertyTests = testList "Property tests" [
    testCase "generated C# list of five elements is not abbreviated in the failure report" <| fun _ ->
        Expect.throwsC
            (fun () ->
                property {
                    let! xs = Range.singleton 0 |> Gen.int |> Gen.list (Range.singleton 5) |> Gen.map ResizeArray
                    return Seq.forall ((>) 0) xs
                }
                |> Property.checkWith (PropertyConfig.withShrinks 0<shrinks> PropertyConfig.defaultConfig)
            )
            (fun ex ->
                Expect.isNotMatch ex.Message "\.\.\." "Abbreviation (...) found"
            )

]
