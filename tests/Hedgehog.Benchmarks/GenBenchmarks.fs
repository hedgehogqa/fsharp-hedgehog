namespace Hedgehog.Benchmarks

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Jobs

open Hedgehog

[<SimpleJob(RuntimeMoniker.NetCoreApp31)>]
type GenBenchmarks () =

    [<Benchmark>]
    member __.GenSampleLarge () =
        let check () =
            Range.constant -100 100
            |> Gen.int32
            |> Gen.sample 100 100000
            |> Seq.iter ignore
        check ()
