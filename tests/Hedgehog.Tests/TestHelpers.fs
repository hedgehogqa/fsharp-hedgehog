module Hedgehog.Tests.TestHelpers

#if FABLE_COMPILER

open Fable.Mocha

// Alias test functions so we do not have to deal with different open statements in every test file.
let testCase = Fable.Mocha.Test.testCase
let ptestCase = Fable.Mocha.Test.ptestCase
let testList = Fable.Mocha.Test.testList

#else

open Expecto

// Alias test functions so we do not have to deal with different open statements in every test file.
let testCase = Expecto.Tests.testCase
let ptestCase = Expecto.Tests.ptestCase
let testList = Expecto.Tests.testList

#endif

let inline testCases name testData testFun =
    let nameWithData name data = sprintf "%s: (%A)" name data

    [ for data in testData do
        testCase (nameWithData name data) (fun _ ->
            testFun data) ]

/// Some tests are not running in javascript world.
/// Use this to ignore such tests.
let testCaseNoFable name testFun =
#if FABLE_COMPILER
    ptestCase name testFun
#else
    testCase name testFun
#endif

let inline (=!) actual expected = Expect.equal expected actual "Should be equal"

[<RequireQualifiedAccess>]
module Expect =
    let inline isTrue value = Expect.isTrue value "Should be true"

#if FABLE_COMPILER
// Add dummy TestAttribute when compiling with Fable.
// This attribute is used only to find tests when running `dotnet test`
type TestsAttribute() = inherit System.Attribute()
#else
// Alias TestsAttribute from Expecto namespace so we do not have to
// open Expecto namespace and guard it with `#if !FABLE_COMPILER` in every file
type TestsAttribute = Expecto.TestsAttribute
#endif
