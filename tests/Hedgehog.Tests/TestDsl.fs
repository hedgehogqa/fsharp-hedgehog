module internal Hedgehog.Tests.TestDsl

open Hedgehog
open Hedgehog.FSharp

#if FABLE_COMPILER
open Fable.Mocha

let testCase = Test.testCase
let ptestCase = Test.ptestCase
let testList = Test.testList
let testCaseAsync = Test.testCaseAsync

#else
open Expecto

let testCase = Tests.testCase
let testCaseAsync = Tests.testCaseAsync
let ptestCase = Tests.ptestCase
let testList = Tests.testList

type TestCase = Test
#endif

let testCases (label : string) (xs : seq<'a>) (f : 'a -> unit) : List<TestCase> =
    [ for x in xs do
        testCase (sprintf "%s: (%A)" label x) (fun _ -> f x) ]

let fableIgnore (label : string) (test : unit -> unit) : TestCase =
#if FABLE_COMPILER
    // Some tests are not running in Node.js.
    ptestCase label test
#else
    testCase label test
#endif

let fableIgnoreAsync (label : string) (test : Async<unit>) : TestCase =
#if FABLE_COMPILER
    // Some tests are not running in Node.js.
    ptestCaseAsync label test
#else
    testCaseAsync label test
#endif

let inline (=!) (actual : 'a) (expected : 'a) : unit =
    Expect.equal actual expected "Should be equal"

/// Runs a property, expects it to fail, and returns the journal for inspection
let expectFailure (prop : Property<unit>) : Journal =
    let report = Property.report prop
    match report.Status with
    | Failed failure -> failure.Journal
    | OK -> failwith "Expected property to fail but it passed"
    | GaveUp -> failwith "Expected property to fail but it gave up"

/// Expects the property to fail and returns the generated values from the journal
let expectFailureWithGeneratedValues (prop: Property<unit>) : obj list =
    let journal = expectFailure prop
    Journal.eval journal
    |> Seq.choose (function GeneratedValue v -> Some v | _ -> None)
    |> List.ofSeq

[<RequireQualifiedAccess>]
module Expect =
    let isTrue value =
        Expect.isTrue value "Should be true"

    let isFalse value =
        Expect.isFalse value "Should be false"

    let inline equal actual expected label =
        Expect.equal actual expected label
