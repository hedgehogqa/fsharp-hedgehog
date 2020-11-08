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

[<Fact>]
let ``dateTime randomly generates value between max and min ticks`` () =
    let seed0 = Seed.random()
    let (seed1, _) = Seed.split seed0
    let range =
        Range.constant
            System.DateTime.MinValue.Ticks
            System.DateTime.MaxValue.Ticks
    let ticks =
        Random.integral range
        |> Random.run seed1 0
    let expected = System.DateTime ticks

    let actual = Gen.dateTime

    let result = actual |> Gen.toRandom |> Random.run seed0 0 |> Tree.outcome
    expected =! result

[<Fact>]
let ``dateTime shrinks to correct mid-value`` () =
    let result =
        property {
            let! actual = Gen.dateTime
            System.DateTime.Now =! actual
        }
        |> Property.report
        |> Report.render
        |> (fun x -> x.Split ([|System.Environment.NewLine|], System.StringSplitOptions.None))
        |> Array.item 2
        |> System.DateTime.Parse
    System.DateTime (2000, 1, 1) =! result

[<Fact>]
let ``int64 can create exponentially bounded integer`` () =
    Property.check <| property {
        let! _ = Gen.int64 (Range.exponentialBounded ())
        return true
    }

[<Fact>]
let ``uint64 can create exponentially bounded integer`` () =
    Property.check <| property {
        let! _ = Gen.uint64 (Range.exponentialBounded ())
        return true
    }