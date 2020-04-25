module Hedgehog.Fable.Tests.GenTests

open Hedgehog

let genTests = xunitTests "Gen tests" [
    theory "dateTime creates System.DateTime instances" 
            [ 8; 16; 32; 64; 128; 256; 512 ] <| fun count ->
            let actual = Gen.dateTime |> Gen.sample 0 count
            actual
            |> List.distinct
            |> List.length
            =! actual.Length
    
    pfact "unicode doesn't return any surrogate" <| fun _ ->
        let actual = Gen.sample 100 100000 Gen.unicode 
        [] =! List.filter System.Char.IsSurrogate actual

    ptheory "unicode doesn't return any noncharacter" 
            [ 65534; 65535 ] <| fun nonchar ->
            let isNoncharacter = (=) <| Operators.char nonchar
            let actual = Gen.sample 100 100000 Gen.unicode
            [] =! List.filter isNoncharacter actual

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

        let actual = Gen.dateTime

        let result = actual |> Gen.toRandom |> Random.run seed0 0 |> Tree.outcome
        expected =! result

    fact "dateTime shrinks to correct mid-value" <| fun _ ->
        let result =
            property {
                let! actual = Gen.dateTime
                System.DateTime.Now =! actual
            }
            |> Property.report
            |> Report.render
            |> (fun (x: string) -> x.Split('\n'))
            |> Array.item 1
            |> System.DateTime.Parse
        System.DateTime (2000, 1, 1) =! result

    fact "int64 can create exponentially bounded integer" <| fun _ ->
        Property.check <| property {
            let! _ = Gen.int64 (Range.exponentialBounded ())
            return true
        }

    fact "uint64 can create exponentially bounded integer" <| fun _ ->
        Property.check <| property {
            let! _ = Gen.uint64 (Range.exponentialBounded ())
            return true
        }

    fact "int can create exponentially bounded integer" <| fun _ ->
        Property.check <| property {
            let! _ = Gen.int (Range.exponentialBounded ())
            return true
        }

    fact "uint32 can create exponentially bounded integer" <| fun _ ->
        Property.check <| property {
            let! _ = Gen.uint32 (Range.exponentialBounded ())
            return true
        }
]