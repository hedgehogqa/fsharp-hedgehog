module Tests

open Fable.Mocha
open Hedgehog

let smokeTests = testList "Smoke tests" [
    testCase "Smoke tests" <| fun _ ->
        // For now just test if everything compiles
        property {
            let! x = Gen.int (Range.linear 100 200)
            return x = x
        } |> Property.check
]

let allTests = testList "All tests" [
    smokeTests
    GenTests.allTests
]

[<EntryPoint>]
let main (args: string[]) = Mocha.runTests allTests