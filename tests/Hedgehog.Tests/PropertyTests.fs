module Hedgehog.Tests.PropertyTests

open Hedgehog
open Expecto
open TestDsl

let propertyTests = testList "Property tests" [
    fableIgnore "generated C# list of five elements is not abbreviated in the failure report" <| fun _ ->
        let prop = property {
            let! xs = Range.singleton 0 |> Gen.int |> Gen.list (Range.singleton 5) |> Gen.map ResizeArray
            return false
        }
        let report = Property.renderWith (PropertyConfig.withShrinks 0<shrinks> PropertyConfig.defaultConfig) prop
        Expect.isNotMatch report "\.\.\." "Abbreviation (...) found"

]
