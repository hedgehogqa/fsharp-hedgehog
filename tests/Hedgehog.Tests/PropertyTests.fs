module Hedgehog.Tests.PropertyTests

open System
open Hedgehog
open Expecto
open TestDsl


let propertyTests = testList "Property tests" [
    fableIgnore "generated C# list of five elements is not abbreviated in the failure report" <| fun _ ->
        let report =
            property {
                let! _ = Range.singleton 0 |> Gen.int32 |> Gen.list (Range.singleton 5) |> Gen.map ResizeArray
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


    testCase "recheck only tests shrunken input" <| fun () ->
        let mutable count = 0
        let prop =
            property {
                let! i = Range.constant 0 1_000_000 |> Gen.int32
                count <- count + 1
                return i =! 0
            }
    
        let report1 = Property.report prop
        match report1.Status with
        | OK -> failwith "Initial report should be Failed, not OK"
        | GaveUp -> failwith "Initial report should be Failed, not GaveUp"
        | Failed failure1 ->
            count <- 0
            let recheckData =
                match failure1.RecheckInfo with
                | Some { Data = recheckData } -> recheckData
                | _ -> failwith "Impossible since this is a an F# test"
            let report2 =
                Property.reportRecheck
                    (RecheckData.serialize recheckData)
                    prop
            match report2.Status with
            | OK -> failwith "Recheck report should be Failed, not OK"
            | GaveUp -> failwith "Recheck report should be Failed, not GaveUp"
            | Failed _ ->
                let _ = Report.render report2
                count =! 1
                //render.Contains "actual: 1" =! true // comment out for now since it causes the Fable test to fail

    testCase "BindReturn adds value to Journal" <| fun () ->
        let actual =
            property {
                let! b = Gen.bool
                return Expect.isTrue b
            }
            |> Property.report
            |> Report.render
            |> (fun x -> x.Split ([|Environment.NewLine|], StringSplitOptions.None))
            |> Array.item 1

        actual =! "false"

    testCase "and! syntax is applicative" <| fun () ->
        // Based on https://well-typed.com/blog/2019/05/integrated-shrinking/#:~:text=For%20example%2C%20consider%20the%20property%20that
        let actual =
            property {
                let! x = Range.constant 0 1_000_000_000 |> Gen.int32
                and! y = Range.constant 0 1_000_000_000 |> Gen.int32
                return x <= y |> Expect.isTrue
            }
            |> Property.report
            |> Report.render
            |> (fun x -> x.Split ([|Environment.NewLine|], StringSplitOptions.None))
            |> Array.item 1

        let actual =
            // normalize printing of a pair between .NET and Fable/JS
            actual.Replace("(", "")
                  .Replace(" ", "")
                  .Replace(")", "")

        actual =! "1,0"

]
