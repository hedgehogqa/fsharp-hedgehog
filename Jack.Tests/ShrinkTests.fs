module Jack.Tests.ShrinkTests

open FSharpx.Collections
open Jack
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
