namespace Hedgehog.Benchmarks

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Jobs

module HRange = Hedgehog.Range
module HGen = Hedgehog.Gen
module FArb = FsCheck.Arb
module FGen = FsCheck.Gen

[<SimpleJob(RuntimeMoniker.NetCoreApp31)>]
type GenBenchmarks () =

    [<Benchmark>]
    member __.Hedgehog_Gen_Sample_Int32_100000 () =
        HRange.constant -100 100
        |> HGen.int32
        |> HGen.sample 100 100000
        |> Seq.iter ignore

    [<Benchmark>]
    member __.FsCheck_Gen_Sample_Int32_100000 () =
        FArb.generate<int>
        |> FGen.sample 100 100000
        |> Seq.iter ignore
