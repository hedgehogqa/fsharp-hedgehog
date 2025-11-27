module Hedgehog.Tests.ShrinkTests

open Hedgehog
open Hedgehog.FSharp
open TestDsl

let shrinkTests = testList "Shrink tests" [
    testCase "removes permutes a list by removing 'k' consecutive elements from it" <| fun _ ->
        let actual = Shrink.removes 2 [ 1; 2; 3; 4; 5; 6 ]

        let expected =
            Seq.ofList [ [ 3; 4; 5; 6 ]
                         [ 1; 2; 5; 6 ]
                         [ 1; 2; 3; 4 ] ]
        // http://stackoverflow.com/a/17101488
        Expect.isTrue (Seq.fold (&&) true (Seq.zip expected actual |> Seq.map (fun (a, b) -> a = b)))

    testCase "removes produces all permutations of removing 'k' elements from a list - example 1" <| fun _ ->
        let actual =
            Shrink.removes 2 [1; 2; 3; 4; 5; 6]
            |> Seq.toList
        actual =! [[3; 4; 5; 6]; [1; 2; 5; 6]; [1; 2; 3; 4]]

    testCase "removes produces all permutations of removing 'k' elements from a list - example 2" <| fun _ ->
        let actual =
            Shrink.removes 3 [1; 2; 3; 4; 5; 6]
            |> Seq.toList
        actual =! [[4; 5; 6]; [1; 2; 3]]

    testCase "removes produces all permutations of removing 'k' elements from a list - example 3" <| fun _ ->
        let actual =
            Shrink.removes 2 ["a"; "b"; "c"; "d"; "e"; "f"]
            |> Seq.toList
        actual =! [["c"; "d"; "e"; "f"]; ["a"; "b"; "e"; "f"]; ["a"; "b"; "c"; "d"]]

    testCase "halves produces a list containing the progressive halving of an integral - example 1" <| fun _ ->
        let actual =
            Shrink.halves 15
            |> Seq.toList
        actual =! [15; 7; 3; 1]

    testCase "halves produces a list containing the progressive halving of an integral - example 2" <| fun _ ->
        let actual =
            Shrink.halves 100
            |> Seq.toList
        actual =! [100; 50; 25; 12; 6; 3; 1]

    testCase "halves produces a list containing the progressive halving of an integral - example 3" <| fun _ ->
        let actual =
            Shrink.halves -26
            |> Seq.toList
        actual =! [-26; -13; -6; -3; -1]

    testCase "list shrinks a list by edging towards the empty list - example 1" <| fun _ ->
        let actual =
            Shrink.list [1; 2; 3]
            |> Seq.toList
        actual =! [[]; [2; 3]; [1; 3]; [1; 2]]

    testCase "list shrinks a list by edging towards the empty list - example 2" <| fun _ ->
        let actual =
            Shrink.list ["a"; "b"; "c"; "d"]
            |> Seq.toList
        actual =!
            [ []
              [ "c"; "d" ]
              [ "a"; "b" ]
              [ "b"; "c"; "d" ]
              [ "a"; "c"; "d" ]
              [ "a"; "b"; "d" ]
              [ "a"; "b"; "c" ] ]

    testCase "towards correct on input 0, 100" <| fun _ ->
        let actual =
            Shrink.towards 0 100
            |> Seq.toList
        actual =! [0; 50; 75; 88; 94; 97; 99]

    testCase "towards correct on input 500, 1000" <| fun _ ->
        let actual =
            Shrink.towards 500 1000
            |> Seq.toList
        actual =! [500; 750; 875; 938; 969; 985; 993; 997; 999]

    testCase "towards correct on input -50, -26" <| fun _ ->
        let actual =
            Shrink.towards -50 -26
            |> Seq.toList
        actual =! [-50; -38; -32; -29; -27]

    testCase "towards correct on input 4, 5" <| fun _ ->
        let actual =
            Shrink.towards 4 5
            |> Seq.toList
        actual =! [4]

    testCase "towardsDouble shrinks a floating-point number by edging towards a destination - example 1" <| fun _ ->
        let actual =
            Shrink.towardsDouble 0.0 100.0
            |> Seq.take 7
            |> Seq.toList
        actual =! [0.0; 50.0; 75.0; 87.5; 93.75; 96.875; 98.4375]

    testCase "towardsDouble shrinks a floating-point number by edging towards a destination - example 2" <| fun _ ->
        let actual =
            Shrink.towardsDouble 1.0 0.5
            |> Seq.take 7
            |> Seq.toList
        actual =! [1.0; 0.75; 0.625; 0.5625; 0.53125; 0.515625; 0.5078125]

    yield! testCases "halves Produces a list containing the results of halving a number"
        [ -4096; -2048; -8; -1; 0; 1; 2; 3; 30; 128; 256; 8192; 10240 ] <| fun n ->
        let actual = Shrink.halves n |> Seq.toList

        let expected =
            n |> List.unfold (fun x ->
                     match x with
                     | 0 -> None
                     | _ -> Some (x, x / 2))
        actual =! expected

    yield! testCases "list produces a smaller permutation of the input list"
        [ 1; 2; 3; 30; 128; 256; 512; 1024 ] <| fun n ->
        let xs = [ 1 .. n ]
        let actual = Shrink.list xs |> Seq.toList
        Expect.isTrue (actual |> List.forall (fun xs' -> xs.Length > xs'.Length))

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
                for _ in 1..n do
                    yield [ 1..n ]
            } |> Seq.toList
        actual =! expected

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
        Expect.isTrue (actual |> List.forall (fun x1 -> x1 < x0 && x1 >= destination))

    yield! testCases "towards returns empty list when run out of shrinks"
        [ (   1,    1)
          (  30,   30)
          (1024, 1024) ] <| fun (x0, destination) ->

        let actual =
            x0
            |> Shrink.towards destination
            |> Seq.toList
        Expect.isTrue (actual |> List.isEmpty)

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
        Expect.isTrue (actual |> List.forall (fun x1 -> x1 < x0 && x1 >= destination))

    yield! testCases "towardsDouble returns empty list when run out of shrinks"
        [ (   1.0,    1.0)
          (  30.0,   30.0)
          (1024.0, 1024.0) ] <| fun (x0, destination) ->

        let actual =
            x0
            |> Shrink.towards destination
            |> Seq.toList
        Expect.isTrue (actual |> List.isEmpty)

    yield! testCases "Property.reportWith respects shrinkLimit"
        [ 0<shrinks>; 1<shrinks>; 2<shrinks> ] <| fun shrinkLimit ->

        let propConfig =
            PropertyConfig.defaults
            |> PropertyConfig.withShrinks shrinkLimit
        let report =
            property {
                let! actual = Range.linear 1 1_000_000 |> Gen.int32
                return actual < 500_000
            }
            |> Property.falseToFailure
            |> Property.reportWith propConfig
        match report.Status with
        | Failed failureData ->
            failureData.Shrinks =! shrinkLimit
        | _ -> failwith "impossible"

    testCase "createTree correct for 0,0" <| fun _ ->
        let actual = Shrink.createTree 0 0 |> Tree.map (sprintf "%A") |> Tree.render
        let expected = "0"
        actual =! expected

    testCase "createTree correct for 0,1" <| fun _ ->
        let actual = Shrink.createTree 0 1 |> Tree.map (sprintf "%A") |> Tree.renderList
        let expected =
          [ "1"
            "└-0" ]
        actual =! expected

    testCase "createTree correct for 0,2" <| fun _ ->
        let actual = Shrink.createTree 0 2 |> Tree.map (sprintf "%A") |> Tree.renderList
        let expected =
          [ "2"
            "├-0"
            "└-1" ]
        actual =! expected

    testCase "createTree correct for 0,3" <| fun _ ->
        let actual = Shrink.createTree 0 3 |> Tree.map (sprintf "%A") |> Tree.renderList
        let expected =
            [ "3"
              "├-0"
              "└-2"
              "  └-1" ]
        actual =! expected

    testCase "createTree correct for 0,4" <| fun _ ->
        let actual = Shrink.createTree 0 4 |> Tree.map (sprintf "%A") |> Tree.renderList
        let expected =
            [ "4"
              "├-0"
              "├-2"
              "| └-1"
              "└-3" ]
        actual =! expected

    testCase "createTree correct for 0,5" <| fun _ ->
        let actual = Shrink.createTree 0 5 |> Tree.map (sprintf "%A") |> Tree.renderList
        let expected =
            [ "5"
              "├-0"
              "├-3"
              "| └-2"
              "|   └-1"
              "└-4" ]
        actual =! expected

    testCase "createTree correct for 0,6" <| fun _ ->
        let actual = Shrink.createTree 0 6 |> Tree.map (sprintf "%A") |> Tree.renderList
        let expected =
            [ "6"
              "├-0"
              "├-3"
              "| └-2"
              "|   └-1"
              "└-5"
              "  └-4" ]
        actual =! expected

    testCase "createTree correct for 0,7" <| fun _ ->
        let actual = Shrink.createTree 0 7 |> Tree.map (sprintf "%A") |> Tree.renderList
        let expected =
            [ "7"
              "├-0"
              "├-4"
              "| ├-2"
              "| | └-1"
              "| └-3"
              "└-6"
              "  └-5" ]
        actual =! expected

    testCase "createTree correct for 4,5" <| fun _ ->
        let actual = Shrink.createTree 4 5 |> Tree.map (sprintf "%A") |> Tree.renderList
        let expected =
            [ "5"
              "└-4" ]
        actual =! expected

    testCase "createTree 0,n creates a tree containing each value in [0,n] exactly once" <| fun _ ->
        for n in [0..100] do
            let actual = Shrink.createTree 0 n |> Tree.toSeq |> Seq.sort |> Seq.toList
            let expected = [0..n]
            actual =! expected

    testCase "elems does not stack overflow on large lists" <| fun _ ->
        // This test would stack overflow with the old recursive implementation.
        // The iterative implementation handles large lists without issue.
        let largeList = [ 1 .. 10000 ]
        let shrinkNothing _ = Seq.empty
        // Just verify it doesn't throw - we don't need to enumerate all results.
        let result = Shrink.elems shrinkNothing largeList
        // Force evaluation of the sequence structure (but not all elements).
        Expect.isTrue (result |> Seq.isEmpty)

    testCase "elems produces correct shrinks for each position" <| fun _ ->
        let xs = [ 'a'; 'b'; 'c' ]
        let shrinkToX _ = Seq.singleton 'x'
        let actual = Shrink.elems shrinkToX xs |> Seq.toList
        let expected = [ ['x'; 'b'; 'c']; ['a'; 'x'; 'c']; ['a'; 'b'; 'x'] ]
        actual =! expected
]
