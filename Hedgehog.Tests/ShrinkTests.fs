module Hedgehog.Tests.ShrinkTests

open FSharpx.Collections
open Hedgehog
open Swensen.Unquote
open Xunit

[<Fact>]
let ``removes permutes a list by removing 'k' consecutive elements from it``() =
    let actual = Shrink.removes 2 [ 1; 2; 3; 4; 5; 6 ]

    let expected =
        LazyList.ofList [ [ 3; 4; 5; 6 ]
                          [ 1; 2; 5; 6 ]
                          [ 1; 2; 3; 4 ] ]
    // http://stackoverflow.com/a/17101488
    test <@ Seq.fold (&&) true (Seq.zip expected actual |> Seq.map (fun (a, b) -> a = b)) @>

[<Theory>]
[<InlineData(-4096)>]
[<InlineData(-2048)>]
[<InlineData(   -8)>]
[<InlineData(   -1)>]
[<InlineData(    0)>]
[<InlineData(    1)>]
[<InlineData(    2)>]
[<InlineData(    3)>]
[<InlineData(   30)>]
[<InlineData(  128)>]
[<InlineData(  256)>]
[<InlineData( 8192)>]
[<InlineData(10240)>]
let ``halves Produces a list containing the results of halving a number`` n =
    let actual = Shrink.halves n |> LazyList.toList

    let expected =
        n |> List.unfold (fun x ->
                 match x with
                 | 0 -> None
                 | _ -> Some (x, x / 2))
    expected =! actual

[<Theory>]
[<InlineData(   1)>]
[<InlineData(   2)>]
[<InlineData(   3)>]
[<InlineData(  30)>]
[<InlineData( 128)>]
[<InlineData( 256)>]
[<InlineData( 512)>]
[<InlineData(1024)>]
let ``list produces a smaller permutation of the input list`` n =
    let xs = [ 1 .. n ]
    let actual = Shrink.list xs |> LazyList.toList
    test <@ actual |> List.forall (fun xs' -> xs.Length > xs'.Length) @>

[<Theory>]
[<InlineData(   1)>]
[<InlineData(   2)>]
[<InlineData(   3)>]
[<InlineData(  30)>]
[<InlineData( 128)>]
[<InlineData( 256)>]
[<InlineData( 512)>]
[<InlineData(1024)>]
let ``elems shrinks each element in input list using a supplied shrinker`` n =
    let xs = [ 1..n ]
    let shrinker =
        fun x ->
            test <@ List.contains x xs @>
            LazyList.singleton x

    let actual =
        xs
        |> Shrink.elems shrinker
        |> LazyList.toList

    let expected =
        seq {
            for i in 1..n do
                yield [ 1..n ]
        }
    Seq.toList expected =! actual

[<Theory>]
[<InlineData(   2,   1)>]
[<InlineData(   3,   1)>]
[<InlineData(  30,   1)>]
[<InlineData( 128,  64)>]
[<InlineData( 256, 128)>]
[<InlineData( 512, 256)>]
[<InlineData(1024, 512)>]
let ``towards shrinks by edging towards a destination number`` x0 destination =
    let actual =
        x0
        |> Shrink.towards destination
        |> LazyList.toList
    test <@ actual |> List.forall (fun x1 -> x1 < x0 && x1 >= destination) @>

[<Theory>]
[<InlineData(   1,    1)>]
[<InlineData(  30,   30)>]
[<InlineData(1024, 1024)>]
let ``towards returns empty list when run out of shrinks`` x0 destination =
    let actual =
        x0
        |> Shrink.towards destination
        |> LazyList.toList
    test <@ actual |> List.isEmpty @>

[<Theory>]
[<InlineData(   1.0)>]
[<InlineData(   2.1)>]
[<InlineData(   3.2)>]
[<InlineData(  30.3)>]
[<InlineData( 128.4)>]
[<InlineData( 256.5)>]
[<InlineData( 512.6)>]
[<InlineData(1024.7)>]
let ``double shrinks a floating point number`` x =
    let actual = Shrink.double x |> LazyList.toList
    test <@ actual |> List.forall (fun x' -> x' < x) @>
