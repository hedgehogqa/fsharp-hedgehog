module Hedgehog.Tests.PropertyTests

open Hedgehog
open Expecto
open TestDsl

let propertyTests = testList "Property tests" [
    fableIgnore "generated C# list of five elements is not abbreviated in the failure report" <| fun _ ->
        let report =
            property {
                let! xs = Range.singleton 0 |> Gen.int32 |> Gen.list (Range.singleton 5) |> Gen.map ResizeArray
                return false
            }
            |> Property.falseToFailure
            |> Property.renderWith (PropertyConfig.withShrinks 0<shrinks> PropertyConfig.defaultConfig)
        Expect.isNotMatch report "\.\.\." "Abbreviation (...) found"

    testCase "exception thrown in map leads to Outcome.Failure" <| fun () ->
        let prop =
            property {
                let! b = Gen.bool
                return b
            }
            |> Property.map (fun _ -> failwith "exception in map")

        let report = prop |> Property.report

        let isFailure =
            match report.Status with
            | Failed _ -> true
            | _ -> false
        Expect.isTrue isFailure


    testCase "counterexample example" <| fun () ->
        // based on examlpe from https://hedgehogqa.github.io/fsharp-hedgehog/index.html#Custom-Operations
        let guid = System.Guid.NewGuid().ToString()
        let tryAdd a b =
            if a > 100 then None // Nasty bug.
            else Some(a + b)
        let report =
            property {
                let! a = Range.constantBounded () |> Gen.int32
                let! b = Range.constantBounded () |> Gen.int32
                counterexample guid
                Some(a + b) =! tryAdd a b
            }
            |> Property.renderWith (PropertyConfig.withShrinks 0<shrinks> PropertyConfig.defaultConfig)
#if FABLE_COMPILER
// See the discussion in this PR for what needs to happen for Fable to support Expect.stringContains https://github.com/hedgehogqa/fsharp-hedgehog/pull/328
        Expect.isTrue (report.Contains guid)
#else
        Expect.stringContains report guid "Missing counterexample text"
#endif

    testCase "Report containing None renders without throwing an exception" <| fun () ->
        property {
            let! opt = Gen.constant () |> Gen.option
            return opt |> Option.isSome
        }
        |> Property.falseToFailure
        |> Property.report
        |> Report.render
        |> ignore
]
