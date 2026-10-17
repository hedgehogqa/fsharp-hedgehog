namespace Hedgehog.Benchmarks

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Tests

module private Bench =

    // The seed and the size are fixed, so every run draws the same collections and the allocated memory is reproducible.
    let seed = Seed.from 42UL
    let size = 30

    let element : Gen<int> =
        Gen.int32 (Range.constant 0 1000)

    /// Draws a collection together with the root of its tree of shrinks.
    let draw (gen : Gen<'a>) : Tree<'a> =
        Random.run seed size (Gen.toRandom gen)

    /// Starts to shrink: goes through the first level of the shrinks and builds the value of every one of them.
    let startShrinking (tree : Tree<'a>) : int =
        let mutable count = 0
        for shrink in Tree.shrinks tree do
            Tree.outcome shrink |> ignore
            count <- count + 1
        count

/// <summary>
/// 'Gen.list', 'Gen.array' and 'Gen.seq', which are built on the array core, against the generators on lists that they
/// replaced ('ListReference'), which are the baseline. A collection is empty, small or larger. It is either drawn
/// or starts to shrink.
/// </summary>
/// <remarks>
/// Run with
///
///     dotnet run -c Release --project tests/Hedgehog.Benchmarks -- --filter "*CollectionBenchmarks*"
///
/// The allocated memory per operation does not depend on the machine, the time does. There are 36 benchmarks, so the
/// job does fewer iterations than the default one.
/// </remarks>
[<MemoryDiagnoser>]
[<SimpleJob(RuntimeMoniker.Net80, launchCount = 1, warmupCount = 3, iterationCount = 10)>]
[<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)>]
[<CategoriesColumn>]
type CollectionBenchmarks () =

    [<Params(0, 5, 100)>]
    member val Length = 0 with get, set

    member val ListBasedList = Unchecked.defaultof<Gen<int list>> with get, set
    member val ListBasedArray = Unchecked.defaultof<Gen<int[]>> with get, set
    member val ListBasedSeq = Unchecked.defaultof<Gen<seq<int>>> with get, set
    member val ArrayCoreList = Unchecked.defaultof<Gen<int list>> with get, set
    member val ArrayCoreArray = Unchecked.defaultof<Gen<int[]>> with get, set
    member val ArrayCoreSeq = Unchecked.defaultof<Gen<seq<int>>> with get, set

    member val ListBasedListTree = Unchecked.defaultof<Tree<int list>> with get, set
    member val ListBasedArrayTree = Unchecked.defaultof<Tree<int[]>> with get, set
    member val ListBasedSeqTree = Unchecked.defaultof<Tree<seq<int>>> with get, set
    member val ArrayCoreListTree = Unchecked.defaultof<Tree<int list>> with get, set
    member val ArrayCoreArrayTree = Unchecked.defaultof<Tree<int[]>> with get, set
    member val ArrayCoreSeqTree = Unchecked.defaultof<Tree<seq<int>>> with get, set

    [<GlobalSetup>]
    member this.Setup () =
        let range = Range.singleton this.Length
        this.ListBasedList <- ListReference.list range Bench.element
        this.ListBasedArray <- ListReference.array range Bench.element
        this.ListBasedSeq <- ListReference.seq range Bench.element
        this.ArrayCoreList <- Gen.list range Bench.element
        this.ArrayCoreArray <- Gen.array range Bench.element
        this.ArrayCoreSeq <- Gen.seq range Bench.element
        this.ListBasedListTree <- Bench.draw this.ListBasedList
        this.ListBasedArrayTree <- Bench.draw this.ListBasedArray
        this.ListBasedSeqTree <- Bench.draw this.ListBasedSeq
        this.ArrayCoreListTree <- Bench.draw this.ArrayCoreList
        this.ArrayCoreArrayTree <- Bench.draw this.ArrayCoreArray
        this.ArrayCoreSeqTree <- Bench.draw this.ArrayCoreSeq

    //
    // Draw
    //

    [<Benchmark(Baseline = true); BenchmarkCategory("Draw list")>]
    member this.DrawListListBased () = Bench.draw this.ListBasedList

    [<Benchmark; BenchmarkCategory("Draw list")>]
    member this.DrawListArrayCore () = Bench.draw this.ArrayCoreList

    [<Benchmark(Baseline = true); BenchmarkCategory("Draw array")>]
    member this.DrawArrayListBased () = Bench.draw this.ListBasedArray

    [<Benchmark; BenchmarkCategory("Draw array")>]
    member this.DrawArrayArrayCore () = Bench.draw this.ArrayCoreArray

    [<Benchmark(Baseline = true); BenchmarkCategory("Draw seq")>]
    member this.DrawSeqListBased () = Bench.draw this.ListBasedSeq

    [<Benchmark; BenchmarkCategory("Draw seq")>]
    member this.DrawSeqArrayCore () = Bench.draw this.ArrayCoreSeq

    //
    // Start to shrink
    //

    [<Benchmark(Baseline = true); BenchmarkCategory("Shrink list")>]
    member this.ShrinkListListBased () = Bench.startShrinking this.ListBasedListTree

    [<Benchmark; BenchmarkCategory("Shrink list")>]
    member this.ShrinkListArrayCore () = Bench.startShrinking this.ArrayCoreListTree

    [<Benchmark(Baseline = true); BenchmarkCategory("Shrink array")>]
    member this.ShrinkArrayListBased () = Bench.startShrinking this.ListBasedArrayTree

    [<Benchmark; BenchmarkCategory("Shrink array")>]
    member this.ShrinkArrayArrayCore () = Bench.startShrinking this.ArrayCoreArrayTree

    [<Benchmark(Baseline = true); BenchmarkCategory("Shrink seq")>]
    member this.ShrinkSeqListBased () = Bench.startShrinking this.ListBasedSeqTree

    [<Benchmark; BenchmarkCategory("Shrink seq")>]
    member this.ShrinkSeqArrayCore () = Bench.startShrinking this.ArrayCoreSeqTree
