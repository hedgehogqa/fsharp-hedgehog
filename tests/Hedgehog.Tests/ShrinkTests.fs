module Hedgehog.Tests.ShrinkTests

open Hedgehog
open Swensen.Unquote
open Xunit

[<Fact>]
let ``removes permutes a list by removing 'k' consecutive elements from it``() =
    let actual = Shrink.removes 2 [ 1; 2; 3; 4; 5; 6 ]

    let expected =
        Seq.ofList [ [ 3; 4; 5; 6 ]
                     [ 1; 2; 5; 6 ]
                     [ 1; 2; 3; 4 ] ]
    // http://stackoverflow.com/a/17101488
    test <@ Seq.fold (&&) true (Seq.zip expected actual |> Seq.map (fun (a, b) -> a = b)) @>

[<Fact>]
let ``removes produces all permutations of removing 'k' elements from a list - example 1`` () =
    let actual =
        Seq.toList <| Shrink.removes 2 [1; 2; 3; 4; 5; 6]
    [[3; 4; 5; 6]; [1; 2; 5; 6]; [1; 2; 3; 4]] =! actual

[<Fact>]
let ``removes produces all permutations of removing 'k' elements from a list - example 2`` () =
    let actual =
        Seq.toList <| Shrink.removes 3 [1; 2; 3; 4; 5; 6]
    [[4; 5; 6]; [1; 2; 3]] =! actual

[<Fact>]
let ``removes produces all permutations of removing 'k' elements from a list - example 3`` () =
    let actual =
        Seq.toList <| Shrink.removes 2 ["a"; "b"; "c"; "d"; "e"; "f"]
    [["c"; "d"; "e"; "f"]; ["a"; "b"; "e"; "f"]; ["a"; "b"; "c"; "d"]] =! actual

[<Fact>]
let ``halves produces a list containing the progressive halving of an integral - example 1`` () =
    let actual =
        Seq.toList <| Shrink.halves 15
    [15; 7; 3; 1] =! actual

[<Fact>]
let ``halves produces a list containing the progressive halving of an integral - example 2`` () =
    let actual =
        Seq.toList <| Shrink.halves 100
    [100; 50; 25; 12; 6; 3; 1] =! actual

[<Fact>]
let ``halves produces a list containing the progressive halving of an integral - example 3`` () =
    let actual =
        Seq.toList <| Shrink.halves -26
    [-26; -13; -6; -3; -1] =! actual

[<Fact>]
let ``list shrinks a list by edging towards the empty list - example 1`` () =
    let actual =
        Seq.toList <| Shrink.list [1; 2; 3]
    [[]; [2; 3]; [1; 3]; [1; 2]] =! actual

[<Fact>]
let ``list shrinks a list by edging towards the empty list - example 2`` () =
    let actual =
        Seq.toList <| Shrink.list ["a"; "b"; "c"; "d"]
    [ []
      [ "c"; "d" ]
      [ "a"; "b" ]
      [ "b"; "c"; "d" ]
      [ "a"; "c"; "d" ]
      [ "a"; "b"; "d" ]
      [ "a"; "b"; "c" ] ]
    =! actual

[<Fact>]
let ``towards shrinks an integral number by edging towards a destination - exmaple 1`` () =
    let actual =
        Seq.toList <| Shrink.towards 0 100
    [0; 50; 75; 88; 94; 97; 99] =! actual

[<Fact>]
let ``towards shrinks an integral number by edging towards a destination - exmaple 2`` () =
    let actual =
        Seq.toList <| Shrink.towards 500 1000
    [500; 750; 875; 938; 969; 985; 993; 997; 999] =! actual

[<Fact>]
let ``towards shrinks an integral number by edging towards a destination - exmaple 3`` () =
    let actual =
        Seq.toList <| Shrink.towards -50 -26
    [-50; -38; -32; -29; -27] =! actual

[<Fact>]
let ``towardsDouble shrinks a floating-point number by edging towards a destination - example 1`` () =
    let actual =
        Seq.toList << Seq.take 7 <| Shrink.towardsDouble 0.0 100.0
    [0.0; 50.0; 75.0; 87.5; 93.75; 96.875; 98.4375] =! actual

[<Fact>]
let ``towardsDouble shrinks a floating-point number by edging towards a destination - example 2`` () =
    let actual =
        Seq.toList << Seq.take 7 <| Shrink.towardsDouble 1.0 0.5
    [1.0; 0.75; 0.625; 0.5625; 0.53125; 0.515625; 0.5078125] =! actual

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
    let actual = Shrink.halves n |> Seq.toList

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
    let actual = Shrink.list xs |> Seq.toList
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
            Seq.singleton x

    let actual =
        xs
        |> Shrink.elems shrinker
        |> Seq.toList

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
        |> Seq.toList
    test <@ actual |> List.forall (fun x1 -> x1 < x0 && x1 >= destination) @>

[<Theory>]
[<InlineData(   1,    1)>]
[<InlineData(  30,   30)>]
[<InlineData(1024, 1024)>]
let ``towards returns empty list when run out of shrinks`` x0 destination =
    let actual =
        x0
        |> Shrink.towards destination
        |> Seq.toList
    test <@ actual |> List.isEmpty @>

[<Theory>]
[<InlineData(   2.0,   1.0)>]
[<InlineData(   3.0,   1.0)>]
[<InlineData(  30.0,   1.0)>]
[<InlineData( 128.0,  64.0)>]
[<InlineData( 256.0, 128.0)>]
[<InlineData( 512.0, 256.0)>]
[<InlineData(1024.0, 512.0)>]
let ``towardsDouble shrinks by edging towards a destination number`` x0 destination =
    let actual =
        x0
        |> Shrink.towardsDouble destination
        |> Seq.toList
    test <@ actual |> List.forall (fun x1 -> x1 < x0 && x1 >= destination) @>

[<Theory>]
[<InlineData(   1.0,    1.0)>]
[<InlineData(  30.0,   30.0)>]
[<InlineData(1024.0, 1024.0)>]
let ``towardsDouble returns empty list when run out of shrinks`` x0 destination =
    let actual =
        x0
        |> Shrink.towards destination
        |> Seq.toList
    test <@ actual |> List.isEmpty @>
