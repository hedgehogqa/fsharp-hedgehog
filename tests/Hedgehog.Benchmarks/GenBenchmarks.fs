namespace Hedgehog.Benchmarks

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Jobs
open FsCheck.Fluent

module HRange = Hedgehog.Range
module HGen = Hedgehog.Gen

[<SimpleJob(RuntimeMoniker.Net80)>]
type GenBenchmarks () =

    [<Params(1_000, 10_000, 100_000)>]
    member val N = 1 with get, set

    [<Benchmark>]
    member this.HedgehogGenSampleInt32 () =
        HRange.constant -100 100
        |> HGen.int32
        |> HGen.sample 100 this.N
        |> Seq.iter ignore

    [<Benchmark(Baseline = true)>]
    member this.FsCheckGenSampleInt32 () =
        ArbMap.Default.ArbFor<int>().Generator.Sample(this.N)
        |> Seq.iter ignore
