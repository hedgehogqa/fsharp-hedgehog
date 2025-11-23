module Hedgehog.Benchmarks.Program

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Running

open Hedgehog
open Hedgehog.FSharp

[<SimpleJob(RuntimeMoniker.Net80)>]
type Benchmarks () =

    [<Benchmark>]
    member _.GenInts () =
        property {
            let! i = Gen.int32 (Range.constant 0 10000)
            return i >= 0
        }
        |> Property.falseToFailure
        |> Property.check

    [<Benchmark>]
    member _.GenAsciiStrings () =
        property {
            let! i = Gen.string (Range.constant 0 100) Gen.ascii
            return i.Length >= 0
        }
        |> Property.falseToFailure
        |> Property.check

    [<Benchmark>]
    member _.BigExampleFromTests () =
        Tests.MinimalTests.perfectMinimalShrink ()

[<SimpleJob(RuntimeMoniker.Net80)>]
type ScaledBenchmarks () =

    [<Params(100, 1000, 10000)>]
    member val N = 1 with get, set

    [<Benchmark>]
    member this.ForLoopTest () =
        property {
            for _ = 0 to this.N do
                ()

            return true
        }
        |> Property.check

[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<Benchmarks> () |> ignore
    BenchmarkRunner.Run<ScaledBenchmarks> () |> ignore
    BenchmarkRunner.Run<GenBenchmarks>() |> ignore
    0
