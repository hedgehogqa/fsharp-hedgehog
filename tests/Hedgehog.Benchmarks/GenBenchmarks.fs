namespace Hedgehog.Benchmarks

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Jobs

module HRange = Hedgehog.Range
module HGen = Hedgehog.Gen
module FGen = FsCheck.Gen

[<SimpleJob(RuntimeMoniker.NetCoreApp31)>]
type GenBenchmarks () =

    [<Benchmark>]
    member __.GenSampleLargeHedgehog () =
        HRange.constant -100 100
        |> HGen.int32
        |> HGen.sample 100 100000
        |> Seq.iter ignore

    [<Benchmark>]
    member __.GenSampleLargeFsCheck () =
        ['0'..'9'] @ ['A'..'Z'] @ ['a'..'z']
        |> FGen.elements
        |> FGen.listOfLength 10
        |> FGen.map (List.toArray >> System.String.Concat)
        |> FGen.sample 0 10000
