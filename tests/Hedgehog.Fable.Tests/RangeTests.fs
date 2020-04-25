module Hedgehog.Fable.Tests.RangeTests

open System
open Hedgehog

let rangeTests = xunitTests "Range tests" [
    theory "singleton bounds returns correct result"
        [ (   2,   1)
          (   3,   1)
          (  30,   1)
          ( 128,  64)
          ( 256, 128)
          ( 512, 256)
          (1024, 512) ] <| fun (sz, x) ->
        
        let actual =
            Range.bounds sz <| Range.singleton x
        (x, x) =! actual

    theory "singleton origin returns correct result"
        [ 1; 2; 3; 30; 128; 256; 512; 1024 ] <| fun x ->
        
        let actual =
            Range.origin <| Range.singleton x
        x =! actual

    theory "constant bounds returns correct result"
        [ (   2,   0,   1)
          (   3,   0,   1)
          (  30,   0,   1)
          ( 128,  63,  64)
          ( 256, 127, 128)
          ( 512, 255, 256)
          (1024, 511, 512) ] <| fun (sz, x, y) ->

        let actual =
            Range.bounds sz <| Range.constant x y
        (x, y) =! actual

    theory "constant origin returns correct result"
        [ (   2,   0)
          (   3,   0)
          (  30,   0)
          ( 128,  63)
          ( 256, 127)
          ( 512, 255)
          (1024, 511) ] <| fun (x, y) ->

        let actual =
            Range.origin <| Range.constant x y
        x =! actual        

    theory "range from -x to x, with the origin at"
        [ 1; 2; 3; 30; 128; 256; 512; 1024] <| fun x ->

        let actual =
            Range.origin <| Range.constantFrom x -10 10
        x =! actual

    theory "range from -x to x, with the bounds at"
        [ (   2,   0)
          (   3,   0)
          (  30,   0)
          ( 128,  63)
          ( 256, 127)
          ( 512, 255)
          (1024, 511) ] <| fun (sz, x) ->

        let actual =
            Range.bounds sz <| Range.constantFrom 0 -x x
        (-x, x) =! actual

    theory "constantBounded bounds returns correct result - Byte range" 
        [ 1; 2; 3; 30; 128; 256; 512; 1024 ] <| fun sz ->

        let x =
            Range.bounds sz <| (Range.constantBounded () : Range<Byte>)
        (Byte.MinValue, Byte.MaxValue) =! x

    theory "constantBounded bounds returns correct result - Int32 range" 
        [ 1; 2; 3; 30; 128; 256; 512; 1024 ] <| fun sz ->

        let x =
            Range.bounds sz <| (Range.constantBounded () : Range<Int32>)
        (Int32.MinValue, Int32.MaxValue) =! x

    theory "constantBounded bounds returns correct result - Int64 range" 
        [ 1; 2; 3; 30; 128; 256; 512; 1024 ] <| fun sz ->

        let x =
            Range.bounds sz <| (Range.constantBounded () : Range<Int64>)
        (Int64.MinValue, Int64.MaxValue) =! x

    theory "clamp truncates a value so it stays within some range" 
        [ (5, 10, 15, 10)
          (5, 10,  0,  5) ] <| fun (x, y, n, expected) ->

        let actual =
            Range.Internal.clamp x y n
        expected =! actual

    fact "linear scales the second bound relative to the size - example 1" <| fun _ ->
        let actual =
            Range.bounds 0 <| Range.linear 0 10
        (0, 0) =! actual

    fact "linear scales the second bound relative to the size - example 2" <| fun _ ->
        let actual =
            Range.bounds 50 <| Range.linear 0 10
        (0, 5) =! actual

    fact "linear scales the second bound relative to the size - example 3" <| fun _ ->
        let actual =
            Range.bounds 99 <| Range.linear 0 10
        (0, 10) =! actual

    fact "linearFrom scales the bounds relative to the size - example 1" <| fun _ ->
        let actual =
            Range.bounds 0 <| Range.linearFrom 0 -10 10
        (0, 0) =! actual

    fact "linearFrom scales the bounds relative to the size - example 2" <| fun _ ->
        let actual =
            Range.bounds 50 <| Range.linearFrom 0 -10 20
        (-5, 10) =! actual

    fact "linearFrom scales the bounds relative to the size - example 3" <| fun _ ->
        let actual =
            Range.bounds 99 <| Range.linearFrom 0 -10 20
        (-10, 20) =! actual

    fact "linearBounded uses the full range of a data type - example 1" <| fun _ ->
        let actual =
            Range.bounds 0 <| (Range.linearBounded () : Range<sbyte>)
        (-0y, 0y) =! actual

    fact "linearBounded uses the full range of a data type - example 2" <| fun _ ->
        let actual =
            Range.bounds 50 <| (Range.linearBounded () : Range<sbyte>)
        (-64y, 64y) =! actual

    fact "linearBounded uses the full range of a data type - example 3" <| fun _ ->
        let actual =
            Range.bounds 99 <| (Range.linearBounded () : Range<sbyte>)
        (-128y, 127y) =! actual

    fact "exponential scales the second bound exponentially relative to the size - example 1" <| fun _ ->
        let actual =
            Range.bounds 0 <| Range.exponential 1 512
        (1, 1) =! actual

    fact "exponential scales the second bound exponentially relative to the size - example 2" <| fun _ ->
        let actual =
            Range.bounds 77 <| Range.exponential 1 512
        (1, 128) =! actual

    fact "exponential scales the second bound exponentially relative to the size - example 3" <| fun _ ->
        let actual =
            Range.bounds 99 <| Range.exponential 1 512
        (1, 512) =! actual

    fact "exponentialFrom scales the bounds exponentially relative to the size - example 1" <| fun _ ->
        let actual =
            Range.bounds 0 <| Range.exponentialFrom 0 -128 512
        (0, 0) =! actual

    fact "exponentialFrom scales the bounds exponentially relative to the size - example 2" <| fun _ ->
        let actual =
            Range.bounds 50 <| Range.exponentialFrom 0 -128 512
        (-11, 22) =! actual

    fact "exponentialFrom scales the bounds exponentially relative to the size - example 3" <| fun _ ->
        let actual =
            Range.bounds 99 <| Range.exponentialFrom 3 -128 512
        (-128, 512) =! actual

    fact "exponentialBounded uses the full range of a data type - example 1" <| fun _ ->
        let actual =
            Range.bounds 0 <| (Range.exponentialBounded () : Range<sbyte>)
        (-0y, 0y) =! actual

    fact "exponentialBounded uses the full range of a data type - example 2" <| fun _ ->
        let actual =
            Range.bounds 50 <| (Range.exponentialBounded () : Range<sbyte>)
        (-11y, 11y) =! actual

    fact "exponentialBounded uses the full range of a data type - example 3" <| fun _ ->
        let actual =
            Range.bounds 99 <| (Range.exponentialBounded () : Range<sbyte>)
        (-128y, 127y) =! actual

]