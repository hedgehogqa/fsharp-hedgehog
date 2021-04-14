module Hedgehog.Tests.PropertyTests

open Hedgehog
open Expecto
open TestDsl

let propertyTests = testList "Property tests" [
    fableIgnore "generated C# list of five elements is not abbreviated in the failure report" <| fun _ ->
        let report =
            property {
                let! xs = Range.singleton 0 |> Gen.int |> Gen.list (Range.singleton 5) |> Gen.map ResizeArray
                return false
            }
            |> Property.renderWith (PropertyConfig.withShrinks 0<shrinks> PropertyConfig.defaultConfig)
        Expect.isNotMatch report "\.\.\." "Abbreviation (...) found"

    testCase "counterexample example" <| fun () ->
        // based on examlpe from https://hedgehogqa.github.io/fsharp-hedgehog/index.html#Custom-Operations
        let guid = System.Guid.NewGuid().ToString()
        let tryAdd a b =
            if a > 100 then None // Nasty bug.
            else Some(a + b)
        let report =
            property {
                let! a = Range.constantBounded () |> Gen.int
                let! b = Range.constantBounded () |> Gen.int
                counterexample guid
                Some(a + b) =! tryAdd a b
            }
            |> Property.renderWith (PropertyConfig.withShrinks 0<shrinks> PropertyConfig.defaultConfig)
        Expect.stringContains report guid "Missing counterexample text"
]
