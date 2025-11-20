namespace Hedgehog.Benchmarks

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Jobs
open FsCheck.Fluent
open Hedgehog

[<SimpleJob(RuntimeMoniker.Net80)>]
type GenBenchmarks () =

    [<Params(1_000, 10_000)>]
    member val N = 1 with get, set

    [<Benchmark>]
    member this.HedgehogGenSampleInt32 () =
        Range.constant -100 100
        |> Gen.int32
        |> Gen.sample 100 this.N
        |> Seq.iter ignore

    [<Benchmark(Baseline = true)>]
    member this.FsCheckGenSampleInt32 () =
        ArbMap.Default.ArbFor<int>().Generator.Sample(this.N)
        |> Seq.iter ignore
