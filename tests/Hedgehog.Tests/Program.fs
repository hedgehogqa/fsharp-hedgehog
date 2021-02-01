module Hedgehog.Tests.Main

open Hedgehog
open TestDsl

#if !FABLE_COMPILER
// Add TestsAttribute so that allTests is discovered when run with `dotnet test`
[<Expecto.Tests>]
#endif
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
    Expecto.Tests.runTestsInAssembly Expecto.Tests.defaultConfig args
#endif
