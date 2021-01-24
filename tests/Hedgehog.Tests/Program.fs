module Hedgehog.Tests.Main

open Hedgehog

#if FABLE_COMPILER
open Fable.Mocha

let allTests = testList "All tests" [
    TreeTests.treeTests
    RangeTests.rangeTests
    GenTests.genTests
    SeedTests.seedTests
    ShrinkTests.shrinkTests
    MinimalTests.minimalTests
]

[<EntryPoint>]
let main (args: string[]) =
    Mocha.runTests allTests
#else
open Expecto

[<EntryPoint>]
let main (args: string[]) =
    runTestsInAssembly defaultConfig args
#endif
