module Hedgehog.Tests.TestHelpers

#if FABLE_COMPILER
open Fable.Mocha

// Add dummy TestAttribute when compiling with Fable
// This attribute is used only to find tests when running `dotnet test`
type TestsAttribute() = inherit System.Attribute()

#else

open Expecto

// Alias TestsAttribute from Expecto namespace so we do not have to
// open Expecto namespace and guard it with `#if !FABLE_COMPILER` in every file
type TestsAttribute = Expecto.TestsAttribute

#endif

let nameWithData name data = sprintf "%s: (%A)" name data

let theory name testData testFun =
    [ for data in testData do
        testCase (nameWithData name data) <| fun _ ->
            testFun data ]

let ptheory name testData testFun =
    [ for data in testData do
        ptestCase (nameWithData name data) <| fun _ ->
            testFun data ]

let fact name testFun = [ testCase name testFun ]

/// Some tests are not running in javascript world.
/// Use this to ignore such tests.
let factNoFable name testFun =
#if FABLE_COMPILER
    [ ptestCase name testFun ]
#else
    [ testCase name testFun ]
#endif

let testList name tests = testList name (List.concat tests)

let inline (=!) actual expected = Expect.equal expected actual "Should be equal"

[<RequireQualifiedAccess>]
module Expect =
    let inline isTrue value = Expect.isTrue value "Should be true"
