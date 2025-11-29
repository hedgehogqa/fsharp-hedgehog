module Hedgehog.Tests.PropertyAsyncTests

open Hedgehog.FSharp
open TestDsl


let asyncTests = testList "Property async tests" [
    testCaseAsync "async property works correctly" (
            property {
                let! x = Gen.int32 (Range.constant 0 100)
                let! y = async { return x + 1 }
                return y = x + 1
            } |> Property.checkBoolAsync)
]
