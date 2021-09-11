open System

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running

open Hedgehog

[<CoreJob>]
type Benchmarks () =

    [<Benchmark>]
    member _.GenInts () =
        Property.check (property {
            let! i = Gen.int32 (Range.constant 0 10000)
            return i >= 0
        })

    [<Benchmark>]
    member _.GenAsciiStrings () =
        Property.check (property {
            let! i = Gen.string (Range.constant 0 100) Gen.ascii
            return i.Length >= 0
        })

    [<Benchmark>]
    member _.BigExampleFromTests () =
        Tests.MinimalTests.perfectMinimalShrink ()

[<CoreJob>]
type ScaledBenchmarks () =

    [<Params (100, 1000)>] // 10000 is too big at the moment, overflows.
    member val N = 1 with get, set

    [<Benchmark>]
    member this.ForLoopTest () =

        Property.check (property {
            for _ = 0 to this.N do
                ()

            return true
        })

[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<Benchmarks> () |> ignore
    BenchmarkRunner.Run<ScaledBenchmarks> () |> ignore

    0 // return an integer exit code
