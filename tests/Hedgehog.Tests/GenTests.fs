module Hedgehog.Tests.GenTests

open Hedgehog
open Hedgehog.Gen.Operators
open TestHelpers

let dtRange = Range.constantFrom (System.DateTime (2000, 1, 1)) System.DateTime.MinValue System.DateTime.MaxValue

[<Tests>]
let genTests = testList "Gen tests" [
    theory "dateTime creates System.DateTime instances"
            [ 8; 16; 32; 64; 128; 256; 512 ] <| fun count ->
            let actual = Gen.dateTime dtRange |> Gen.sample 0 count
            actual
            |> List.distinct
            |> List.length
            =! actual.Length

    fact "unicode doesn't return any surrogate" <| fun _ ->
        let actual = Gen.sample 100 100000 Gen.unicode
        [] =! List.filter System.Char.IsSurrogate actual

    theory "unicode doesn't return any noncharacter"
            [ 65534; 65535 ] <| fun nonchar ->
            let actual = Gen.sample 100 100000 Gen.unicode
            [] =! List.filter (fun ch -> ch = char nonchar) actual

    fact "dateTime randomly generates value between max and min ticks" <| fun _ ->
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

        let actual = Gen.dateTime dtRange

        let result = actual |> Gen.toRandom |> Random.run seed0 0 |> Tree.outcome
        expected =! result

    fact "dateTime shrinks to correct mid-value" <| fun _ ->
        let result =
            property {
                let! actual = Gen.dateTime dtRange
                System.DateTime.Now =! actual
            }
            |> Property.report
            |> Report.render
            |> (fun (x: string) -> x.Split('\n'))
            |> Array.item 1
            |> System.DateTime.Parse
        System.DateTime (2000, 1, 1) =! result

    factNoFable "int64 can create exponentially bounded integer" <| fun _ ->
        Property.check (property {
            let! _ = Gen.int64 (Range.exponentialBounded ())
            return true
        })

    factNoFable "uint64 can create exponentially bounded integer" <| fun _ ->
        Property.check (property {
            let! _ = Gen.uint64 (Range.exponentialBounded ())
            return true
        })
]

[<Fact>]
let ``apply is chainable`` () =
    let _ : Gen<int> =
        Gen.constant (+)
        |> Gen.apply (Gen.constant 1)
        |> Gen.apply (Gen.constant 1)
    ()

[<Fact>]
let ``apply operator works as expected`` () =
    let _ : Gen<int> = (+) <!> (Gen.constant 1) <*> (Gen.constant 1)
    ()
