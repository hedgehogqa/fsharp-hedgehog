module Hedgehog.Tests.Main

open TestDsl

#if !FABLE_COMPILER
// Add TestsAttribute so that allTests is discovered when run with `dotnet test`
[<Expecto.Tests>]
#endif
let allTests = testList "All tests" [
    TreeTests.treeTests
    RangeTests.rangeTests
    GenTests.genTests
    ListGenTests.listGenTests
    SeedTests.seedTests
    ShrinkTests.shrinkTests
    MinimalTests.minimalTests
    ReportTests.reportTests
    PropertyTests.propertyTests
    PropertyBindTests.propertyBindTests
    PropertyAsyncTests.asyncTests
#if !FABLE_COMPILER
    PropertyAsyncAndTaskTests.asyncAndTaskTests
#endif
]

[<EntryPoint>]
let main (args: string[]) =
#if FABLE_COMPILER
    Fable.Mocha.Mocha.runTests allTests
#else
    Expecto.Tests.runTestsInAssemblyWithCLIArgs [] args
#endif
