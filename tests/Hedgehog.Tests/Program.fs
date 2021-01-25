module Hedgehog.Tests.Main

open Hedgehog
open TestHelpers

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
#if FABLE_COMPILER
    Fable.Mocha.Mocha.runTests allTests
#else
    Expecto.Tests.runTests Expecto.Tests.defaultConfig allTests
#endif
