namespace Hedgehog.Benchmarks

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Jobs

module HRange = Hedgehog.Range
module HGen = Hedgehog.Gen
module FArb = FsCheck.Arb
module FGen = FsCheck.Gen

[<SimpleJob(RuntimeMoniker.NetCoreApp31)>]
type GenBenchmarks () =

    [<Params(100, 1_000, 10_000, 100_000)>]
    member val N = 1 with get, set

    [<Benchmark>]
    member this.HedgehogGenSampleInt32 () =
        HRange.constant -100 100
        |> HGen.int32
        |> HGen.sample 100 this.N
        |> Seq.iter ignore

    [<Benchmark>]
    member this.FsCheckGenSampleInt32 () =
        FArb.generate<int>
        |> FGen.sample 100 this.N
        |> Seq.iter ignore
