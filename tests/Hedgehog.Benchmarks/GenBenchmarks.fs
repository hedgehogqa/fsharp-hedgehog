namespace Hedgehog.Benchmarks

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Jobs
open FsCheck.Fluent
open Hedgehog
open Hedgehog.FSharp

[<SimpleJob(RuntimeMoniker.NetCoreApp31)>]
type GenBenchmarks () =

    [<Params(100, 1_000, 10_000, 100_000)>]
    member val N = 1 with get, set

    [<Benchmark>]
    member this.HedgehogGenSampleInt32 () =
        Range.constant -100 100
        |> Gen.int32
        |> Gen.sample 100 this.N
        |> Seq.iter ignore

    [<Benchmark>]
    member this.FsCheckGenSampleInt32 () =
        ArbMap.Default.ArbFor<int>().Generator.Sample(this.N)
        |> Seq.iter ignore
