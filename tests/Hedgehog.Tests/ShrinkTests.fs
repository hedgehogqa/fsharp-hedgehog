module Hedgehog.Tests.ShrinkTests

open Hedgehog
open TestHelpers

[<Tests>]
let shrinkTests = testList "Shrink tests" [
    testCase "removes permutes a list by removing 'k' consecutive elements from it" <| fun _ ->
        let actual = Shrink.removes 2 [ 1; 2; 3; 4; 5; 6 ]

        let expected =
            Seq.ofList [ [ 3; 4; 5; 6 ]
                         [ 1; 2; 5; 6 ]
                         [ 1; 2; 3; 4 ] ]
        // http://stackoverflow.com/a/17101488
        Seq.zip expected actual
        |> Seq.forall (fun (a, b) -> a = b)
        |> Expect.isTrue

    testCase "removes produces all permutations of removing 'k' elements from a list - example 1" <| fun _ ->
        let actual =
            Shrink.removes 2 [1; 2; 3; 4; 5; 6]
            |> Seq.toList
        [[3; 4; 5; 6]; [1; 2; 5; 6]; [1; 2; 3; 4]] =! actual

    testCase "removes produces all permutations of removing 'k' elements from a list - example 2" <| fun _ ->
        let actual =
            Shrink.removes 3 [1; 2; 3; 4; 5; 6]
            |> Seq.toList
        [[4; 5; 6]; [1; 2; 3]] =! actual

    testCase "removes produces all permutations of removing 'k' elements from a list - example 3" <| fun _ ->
        let actual =
            Shrink.removes 2 ["a"; "b"; "c"; "d"; "e"; "f"]
            |> Seq.toList
        [["c"; "d"; "e"; "f"]; ["a"; "b"; "e"; "f"]; ["a"; "b"; "c"; "d"]] =! actual

    testCase "halves produces a list containing the progressive halving of an integral - example 1" <| fun _ ->
        let actual =
            Shrink.halves 15
            |> Seq.toList
        [15; 7; 3; 1] =! actual

    testCase "halves produces a list containing the progressive halving of an integral - example 2" <| fun _ ->
        let actual =
            Shrink.halves 100
            |> Seq.toList
        [100; 50; 25; 12; 6; 3; 1] =! actual

    testCase "halves produces a list containing the progressive halving of an integral - example 3" <| fun _ ->
        let actual =
            Shrink.halves -26
            |> Seq.toList
        [-26; -13; -6; -3; -1] =! actual

    testCase "list shrinks a list by edging towards the empty list - example 1" <| fun _ ->
        let actual =
            Shrink.list [1; 2; 3]
            |> Seq.toList
        [[]; [2; 3]; [1; 3]; [1; 2]] =! actual

    testCase "list shrinks a list by edging towards the empty list - example 2" <| fun _ ->
        let actual =
            Shrink.list ["a"; "b"; "c"; "d"]
            |> Seq.toList
        [ []
          [ "c"; "d" ]
          [ "a"; "b" ]
          [ "b"; "c"; "d" ]
          [ "a"; "c"; "d" ]
          [ "a"; "b"; "d" ]
          [ "a"; "b"; "c" ] ]
        =! actual

    testCase "towards shrinks an integral number by edging towards a destination - exmaple 1" <| fun _ ->
        let actual =
            Shrink.towards 0 100
            |> Seq.toList
        [0; 50; 75; 88; 94; 97; 99] =! actual

    testCase "towards shrinks an integral number by edging towards a destination - exmaple 2" <| fun _ ->
        let actual =
            Shrink.towards 500 1000
            |> Seq.toList
        [500; 750; 875; 938; 969; 985; 993; 997; 999] =! actual

    testCase "towards shrinks an integral number by edging towards a destination - exmaple 3" <| fun _ ->
        let actual =
            Shrink.towards -50 -26
            |> Seq.toList
        [-50; -38; -32; -29; -27] =! actual

    testCase "towardsDouble shrinks a floating-point number by edging towards a destination - example 1" <| fun _ ->
        let actual =
            Shrink.towardsDouble 0.0 100.0
            |> Seq.take 7
            |> Seq.toList
        [0.0; 50.0; 75.0; 87.5; 93.75; 96.875; 98.4375] =! actual

    testCase "towardsDouble shrinks a floating-point number by edging towards a destination - example 2" <| fun _ ->
        let actual =
            Shrink.towardsDouble 1.0 0.5
            |> Seq.take 7
            |> Seq.toList
        [1.0; 0.75; 0.625; 0.5625; 0.53125; 0.515625; 0.5078125] =! actual

    yield! testCases "halves Produces a list containing the results of halving a number"
        [ -4096; -2048; -8; -1; 0; 1; 2; 3; 30; 128; 256; 8192; 10240 ] <| fun n ->
        let actual = Shrink.halves n |> Seq.toList

        let expected =
            n |> List.unfold (fun x ->
                     match x with
                     | 0 -> None
                     | _ -> Some (x, x / 2))
        expected =! actual

    yield! testCases "list produces a smaller permutation of the input list"
        [ 1; 2; 3; 30; 128; 256; 512; 1024 ] <| fun n ->
        let xs = [ 1 .. n ]
        let actual = Shrink.list xs |> Seq.toList
        actual |> List.forall (fun xs' -> xs.Length > xs'.Length) =! true

    yield! testCases "elems shrinks each element in input list using a supplied shrinker"
        [ 1; 2; 3; 30; 128; 256; 512; 1024 ] <| fun n ->
        let xs = [ 1..n ]
        let shrinker =
            fun x ->
                Expect.isTrue (List.contains x xs)
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

    yield! testCases "towards shrinks by edging towards a destination number"
        [ (   2,   1)
          (   3,   1)
          (  30,   1)
          ( 128,  64)
          ( 256, 128)
          ( 512, 256)
          (1024, 512) ] <| fun (x0, destination) ->

        let actual =
            x0
            |> Shrink.towards destination
            |> Seq.toList
        actual
        |> List.forall (fun x1 -> x1 < x0 && x1 >= destination)
        |> Expect.isTrue

    yield! testCases "towards returns empty list when run out of shrinks"
        [ (   1,    1)
          (  30,   30)
          (1024, 1024) ] <| fun (x0, destination) ->

        let actual =
            x0
            |> Shrink.towards destination
            |> Seq.toList
        actual
        |> List.isEmpty
        |> Expect.isTrue

    yield! testCases "towardsDouble shrinks by edging towards a destination number"
        [ (   2.0,   1.0)
          (   3.0,   1.0)
          (  30.0,   1.0)
          ( 128.0,  64.0)
          ( 256.0, 128.0)
          ( 512.0, 256.0)
          (1024.0, 512.0) ] <| fun (x0, destination) ->

        let actual =
            x0
            |> Shrink.towardsDouble destination
            |> Seq.toList
        actual
        |> List.forall (fun x1 -> x1 < x0 && x1 >= destination)
        |> Expect.isTrue

    yield! testCases "towardsDouble returns empty list when run out of shrinks"
        [ (   1.0,    1.0)
          (  30.0,   30.0)
          (1024.0, 1024.0) ] <| fun (x0, destination) ->

        let actual =
            x0
            |> Shrink.towards destination
            |> Seq.toList
        actual
        |> List.isEmpty
        |> Expect.isTrue

]
