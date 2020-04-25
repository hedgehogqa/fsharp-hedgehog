module Hedgehog.Fable.Tests.MochaXUnitAdapter

open Fable.Mocha

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
let pfact name testFun = [ ptestCase name testFun ]
let xunitTests name tests = testList name (List.concat tests)

let inline (=!) actual expected = Expect.equal expected actual "Should be equal"
