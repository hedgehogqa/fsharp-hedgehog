module Hedgehog.Tests.RangeTests

open Hedgehog
open Swensen.Unquote
open System
open Xunit

[<Theory>]
[<InlineData(   2,   1)>]
[<InlineData(   3,   1)>]
[<InlineData(  30,   1)>]
[<InlineData( 128,  64)>]
[<InlineData( 256, 128)>]
[<InlineData( 512, 256)>]
[<InlineData(1024, 512)>]
let ``singleton bounds returns correct result`` sz x =
    let actual =
        Range.singleton x
        |> Range.bounds sz
    (x, x) =!
        actual

[<Theory>]
[<InlineData(   1)>]
[<InlineData(   2)>]
[<InlineData(   3)>]
[<InlineData(  30)>]
[<InlineData( 128)>]
[<InlineData( 256)>]
[<InlineData( 512)>]
[<InlineData(1024)>]
let ``singleton origin returns correct result`` x =
    let actual =
        Range.singleton x
        |> Range.origin
    x =!
        actual

[<Theory>]
[<InlineData(   2,   0,   1)>]
[<InlineData(   3,   0,   1)>]
[<InlineData(  30,   0,   1)>]
[<InlineData( 128,  63,  64)>]
[<InlineData( 256, 127, 128)>]
[<InlineData( 512, 255, 256)>]
[<InlineData(1024, 511, 512)>]
let ``constant bounds returns correct result`` sz x y =
    let actual =
        Range.constant x y
        |> Range.bounds sz
    (x, y) =!
        actual

[<Theory>]
[<InlineData(   2,   0)>]
[<InlineData(   3,   0)>]
[<InlineData(  30,   0)>]
[<InlineData( 128,  63)>]
[<InlineData( 256, 127)>]
[<InlineData( 512, 255)>]
[<InlineData(1024, 511)>]
let ``constant origin returns correct result`` x y =
    let actual =
        Range.constant x y
        |> Range.origin
    x =!
        actual

[<Theory>]
[<InlineData(   1)>]
[<InlineData(   2)>]
[<InlineData(   3)>]
[<InlineData(  30)>]
[<InlineData( 128)>]
[<InlineData( 256)>]
[<InlineData( 512)>]
[<InlineData(1024)>]
let ``range from -x to x, with the origin at`` x =
    let actual =
        Range.constantFrom x -10 10
        |> Range.origin
    x =!
        actual

[<Theory>]
[<InlineData(   2,   0)>]
[<InlineData(   3,   0)>]
[<InlineData(  30,   0)>]
[<InlineData( 128,  63)>]
[<InlineData( 256, 127)>]
[<InlineData( 512, 255)>]
[<InlineData(1024, 511)>]
let ``range from -x to x, with the bounds at`` sz x =
    let actual =
        Range.constantFrom 0 -x x
        |> Range.bounds sz
    (-x, x) =!
        actual

[<Theory>]
[<InlineData(   1)>]
[<InlineData(   2)>]
[<InlineData(   3)>]
[<InlineData(  30)>]
[<InlineData( 128)>]
[<InlineData( 256)>]
[<InlineData( 512)>]
[<InlineData(1024)>]
let ``constantBounded bounds returns correct result - Byte range`` sz =
    let x =
        Range.constantBounded () : Range<Byte>
        |> Range.bounds sz
    (Byte.MinValue, Byte.MaxValue) =!
        x

[<Theory>]
[<InlineData(   1)>]
[<InlineData(   2)>]
[<InlineData(   3)>]
[<InlineData(  30)>]
[<InlineData( 128)>]
[<InlineData( 256)>]
[<InlineData( 512)>]
[<InlineData(1024)>]
let ``constantBounded bounds returns correct result - Int32 range`` sz =
    let x =
        Range.constantBounded () : Range<Int32>
        |> Range.bounds sz
    (Int32.MinValue, Int32.MaxValue) =!
        x

[<Theory>]
[<InlineData(   1)>]
[<InlineData(   2)>]
[<InlineData(   3)>]
[<InlineData(  30)>]
[<InlineData( 128)>]
[<InlineData( 256)>]
[<InlineData( 512)>]
[<InlineData(1024)>]
let ``constantBounded bounds returns correct result - Int64 range`` sz =
    let x =
        Range.constantBounded () : Range<Int64>
        |> Range.bounds sz
    (Int64.MinValue, Int64.MaxValue) =!
        x

[<Theory>]
[<InlineData(5, 10, 15, 10)>]
[<InlineData(5, 10,  0,  5)>]
let ``clamp truncates a value so it stays within some range `` x y n expected =
    let actual =
        Range.Internal.clamp x y n
    expected =!
        actual

[<Fact>]
let ``linear scales the second bound relative to the size - example 1`` () =
    let actual =
        Range.linear 0 10
        |> Range.bounds 0
    (0, 0) =!
        actual

[<Fact>]
let ``linear scales the second bound relative to the size - example 2`` () =
    let actual =
        Range.linear 0 10
        |> Range.bounds 50
    (0, 5) =!
        actual

[<Fact>]
let ``linear scales the second bound relative to the size - example 3`` () =
    let actual =
        Range.linear 0 10
        |> Range.bounds 99
    (0, 10) =!
        actual

[<Fact>]
let ``linearFrom scales the bounds relative to the size - example 1`` () =
    let actual =
        Range.linearFrom 0 -10 10
        |> Range.bounds 0
    (0, 0) =!
        actual

[<Fact>]
let ``linearFrom scales the bounds relative to the size - example 2`` () =
    let actual =
        Range.linearFrom 0 -10 20
        |> Range.bounds 50
    (-5, 10) =!
        actual

[<Fact>]
let ``linearFrom scales the bounds relative to the size - example 3`` () =
    let actual =
        Range.linearFrom 0 -10 20
        |> Range.bounds 99
    (-10, 20) =!
        actual

[<Fact>]
let ``linearBounded uses the full range of a data type - example 1`` () =
    let actual =
        Range.linearBounded () : Range<sbyte>
        |> Range.bounds 0
    (-0y, 0y) =!
        actual

[<Fact>]
let ``linearBounded uses the full range of a data type - example 2`` () =
    let actual =
        Range.linearBounded () : Range<sbyte>
        |> Range.bounds 50
    (-64y, 64y) =!
        actual

[<Fact>]
let ``linearBounded uses the full range of a data type - example 3`` () =
    let actual =
        Range.linearBounded () : Range<sbyte>
        |> Range.bounds 99
    (-128y, 127y) =!
        actual

[<Fact>]
let ``exponential scales the second bound exponentially relative to the size - example 1`` () =
    let actual =
        Range.exponential 1 512
        |> Range.bounds 0
    (1, 1) =!
        actual

[<Fact>]
let ``exponential scales the second bound exponentially relative to the size - example 2`` () =
    let actual =
        Range.exponential 1 512
        |> Range.bounds 77
    (1, 128) =!
        actual

[<Fact>]
let ``exponential scales the second bound exponentially relative to the size - example 3`` () =
    let actual =
        Range.exponential 1 512
        |> Range.bounds 99
    (1, 512) =!
        actual

[<Fact>]
let ``exponentialFrom scales the bounds exponentially relative to the size - example 1`` () =
    let actual =
        Range.exponentialFrom 0 -128 512
        |> Range.bounds 0
    (0, 0) =!
        actual

[<Fact>]
let ``exponentialFrom scales the bounds exponentially relative to the size - example 2`` () =
    let actual =
        Range.exponentialFrom 0 -128 512
        |> Range.bounds 50
    (-11, 22) =!
        actual

[<Fact>]
let ``exponentialFrom scales the bounds exponentially relative to the size - example 3`` () =
    let actual =
        Range.exponentialFrom 3 -128 512
        |> Range.bounds 99
    (-128, 512) =!
        actual

[<Fact>]
let ``exponentialBounded uses the full range of a data type - example 1`` () =
    let actual =
        Range.exponentialBounded () : Range<sbyte>
        |> Range.bounds 0
    (-0y, 0y) =!
        actual

[<Fact>]
let ``exponentialBounded uses the full range of a data type - example 2`` () =
    let actual =
        Range.exponentialBounded () : Range<sbyte>
        |> Range.bounds 50
    (-11y, 11y) =!
        actual

[<Fact>]
let ``exponentialBounded uses the full range of a data type - example 3`` () =
    let actual =
        Range.exponentialBounded () : Range<sbyte>
        |> Range.bounds 99
    (-128y, 127y) =!
        actual
