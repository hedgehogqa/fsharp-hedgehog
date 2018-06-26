module Hedgehog.Tests.GenTests

open Hedgehog
open Swensen.Unquote
open Xunit

[<Theory>]
[<InlineData(  8)>]
[<InlineData( 16)>]
[<InlineData( 32)>]
[<InlineData( 64)>]
[<InlineData(128)>]
[<InlineData(256)>]
[<InlineData(512)>]
let ``dateTime creates System.DateTime instances`` count =
    let actual = Gen.dateTime |> Gen.sample 0 count
    actual
    |> List.distinct
    |> List.length
    =! actual.Length

[<Fact>]
let ``unicode doesn't return any surrogate`` () =
    let actual = Gen.sample 100 100000 Gen.unicode 
    [] =! List.filter System.Char.IsSurrogate actual

[<Theory>]
[<InlineData(65534)>]
[<InlineData(65535)>]
let ``unicode doesn't return any noncharacter`` nonchar =
    let isNoncharacter = (=) <| Operators.char nonchar
    let actual = Gen.sample 100 100000 Gen.unicode
    [] =! List.filter isNoncharacter actual