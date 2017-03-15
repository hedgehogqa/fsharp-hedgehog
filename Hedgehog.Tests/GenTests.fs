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
