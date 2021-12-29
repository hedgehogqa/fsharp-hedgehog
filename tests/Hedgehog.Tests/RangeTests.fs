module Hedgehog.Tests.RangeTests

open System
open Hedgehog
open TestDsl

let rangeTests = testList "Range tests" [
    yield! testCases "singleton bounds returns correct result"
        [ (   2,   1)
          (   3,   1)
          (  30,   1)
          ( 128,  64)
          ( 256, 128)
          ( 512, 256)
          (1024, 512) ] <| fun (sz, x) ->

        let actual =
            Range.singleton x
            |> Range.bounds sz
        actual =! (x, x)

    yield! testCases "singleton origin returns correct result"
        [ 1; 2; 3; 30; 128; 256; 512; 1024 ] <| fun x ->

        let actual =
            Range.singleton x
            |> Range.origin
        actual =! x

    yield! testCases "constant bounds returns correct result"
        [ (   2,   0,   1)
          (   3,   0,   1)
          (  30,   0,   1)
          ( 128,  63,  64)
          ( 256, 127, 128)
          ( 512, 255, 256)
          (1024, 511, 512) ] <| fun (sz, x, y) ->

        let actual =
            Range.constant x y
            |> Range.bounds sz
        actual =! (x, y)

    yield! testCases "constant origin returns correct result"
        [ (   2,   0)
          (   3,   0)
          (  30,   0)
          ( 128,  63)
          ( 256, 127)
          ( 512, 255)
          (1024, 511) ] <| fun (x, y) ->

        let actual =
            Range.constant x y
            |> Range.origin
        actual =! x

    yield! testCases "range from -x to x, with the origin at"
        [ 1; 2; 3; 30; 128; 256; 512; 1024] <| fun x ->

        let actual =
            Range.constantFrom x -10 10
            |> Range.origin
        actual =! x

    yield! testCases "range from -x to x, with the bounds at"
        [ (   2,   0)
          (   3,   0)
          (  30,   0)
          ( 128,  63)
          ( 256, 127)
          ( 512, 255)
          (1024, 511) ] <| fun (sz, x) ->

        let actual =
            Range.constantFrom 0 -x x
            |> Range.bounds sz
        actual =! (-x, x)

    yield! testCases "constantBounded bounds returns correct result - Byte range"
        [ 1; 2; 3; 30; 128; 256; 512; 1024 ] <| fun sz ->

        let x =
            (Range.constantBounded () : Range<Byte>)
            |> Range.bounds sz
        (Byte.MinValue, Byte.MaxValue) =!
            x

    yield! testCases "constantBounded bounds returns correct result - Int32 range"
        [ 1; 2; 3; 30; 128; 256; 512; 1024 ] <| fun sz ->

        let x =
            (Range.constantBounded () : Range<Int32>)
            |> Range.bounds sz
        (Int32.MinValue, Int32.MaxValue) =!
            x

    yield! testCases "constantBounded bounds returns correct result - Int64 range"
        [ 1; 2; 3; 30; 128; 256; 512; 1024 ] <| fun sz ->

        let x =
            (Range.constantBounded () : Range<Int64>)
            |> Range.bounds sz
        (Int64.MinValue, Int64.MaxValue) =!
            x

    yield! testCases "clamp truncates a value so it stays within some range"
        [ (5, 10, 15, 10)
          (5, 10,  0,  5) ] <| fun (x, y, n, expected) ->

        let actual =
            Range.Internal.clamp x y n
        actual =! expected

    testCase "linear scales the second bound relative to the size - example 1" <| fun _ ->
        let actual =
            Range.linear 0 10
            |> Range.bounds 0
        actual =! (0, 0)

    testCase "linear scales the second bound relative to the size - example 2" <| fun _ ->
        let actual =
            Range.linear 0 10
            |> Range.bounds 50
        actual =! (0, 5)

    testCase "linear scales the second bound relative to the size - example 3" <| fun _ ->
        let actual =
            Range.linear 0 10
            |> Range.bounds 99
        actual =! (0, 10)

    testCase "linearFrom scales the bounds relative to the size - example 1" <| fun _ ->
        let actual =
            Range.linearFrom 0 -10 10
            |> Range.bounds 0
        actual =! (0, 0)

    testCase "linearFrom scales the bounds relative to the size - example 2" <| fun _ ->
        let actual =
            Range.linearFrom 0 -10 20
            |> Range.bounds 50
        actual =! (-5, 10)

    testCase "linearFrom scales the bounds relative to the size - example 3" <| fun _ ->
        let actual =
            Range.linearFrom 0 -10 20
            |> Range.bounds 99
        actual =! (-10, 20)

    testCase "linearBounded uses the full range of a data type - example 1" <| fun _ ->
        let actual =
            (Range.linearBounded () : Range<sbyte>)
            |> Range.bounds 0
        actual =! (-0y, 0y)

    testCase "linearBounded uses the full range of a data type - example 2" <| fun _ ->
        let actual =
            (Range.linearBounded () : Range<sbyte>)
            |> Range.bounds 50
        actual =! (-64y, 64y)

    testCase "linearBounded uses the full range of a data type - example 3" <| fun _ ->
        let actual =
            (Range.linearBounded () : Range<sbyte>)
            |> Range.bounds 99
        actual =! (-128y, 127y)

    testCase "exponential scales the second bound exponentially relative to the size - example 1" <| fun _ ->
        let actual =
            Range.exponential 1 512
            |> Range.bounds 0
        actual =! (1, 1)

    testCase "exponential scales the second bound exponentially relative to the size - example 2" <| fun _ ->
        let actual =
            Range.exponential 1 512
            |> Range.bounds 77
        actual =! (1, 128)

    testCase "exponential scales the second bound exponentially relative to the size - example 3" <| fun _ ->
        let actual =
            Range.exponential 1 512
            |> Range.bounds 99
        actual =! (1, 512)

    testCase "exponentialFrom scales the bounds exponentially relative to the size - example 1" <| fun _ ->
        let actual =
            Range.exponentialFrom 0 -128 512
            |> Range.bounds 0
        actual =! (0, 0)

    testCase "exponentialFrom scales the bounds exponentially relative to the size - example 2" <| fun _ ->
        let actual =
            Range.exponentialFrom 0 -128 512
            |> Range.bounds 50
        actual =! (-11, 22)

    testCase "exponentialFrom scales the bounds exponentially relative to the size - example 3" <| fun _ ->
        let actual =
            Range.exponentialFrom 3 -128 512
            |> Range.bounds 99
        actual =! (-128, 512)

    testCase "exponentialBounded uses the full range of a data type - example 1" <| fun _ ->
        let actual =
            (Range.exponentialBounded () : Range<sbyte>)
            |> Range.bounds 0
        actual =! (-0y, 0y)

    testCase "exponentialBounded uses the full range of a data type - example 2" <| fun _ ->
        let actual =
            (Range.exponentialBounded () : Range<sbyte>)
            |> Range.bounds 50
        actual =! (-11y, 11y)

    testCase "exponentialBounded uses the full range of a data type - example 3" <| fun _ ->
        let actual =
            (Range.exponentialBounded () : Range<sbyte>)
            |> Range.bounds 99
        actual =! (-128y, 127y)
]
