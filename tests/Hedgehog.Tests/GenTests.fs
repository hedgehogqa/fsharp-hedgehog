module Hedgehog.Tests.GenTests

open Hedgehog
open Hedgehog.Gen.Operators
open TestDsl

let genTests = testList "Gen tests" [
    yield! testCases "dateTime creates System.DateTime instances" [ 8; 16; 32; 64; 128; 256; 512 ] <| fun count->
        let actual = Gen.dateTime (Range.constant System.DateTime.MinValue System.DateTime.MaxValue) |> Gen.sample 0 count
        actual
        |> List.distinct
        |> List.length
        =! actual.Length

    testCase "unicode doesn't return any surrogate" <| fun _ ->
        let actual = Gen.sample 100 100000 Gen.unicode
        [] =! List.filter System.Char.IsSurrogate actual

    yield! testCases "unicode doesn't return any noncharacter" [ 65534; 65535 ] <| fun nonchar ->
        let actual = Gen.sample 100 100000 Gen.unicode
        [] =! List.filter (fun ch -> ch = char nonchar) actual

    testCase "dateTime randomly generates value between max and min ticks" <| fun _ ->
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

        let actual = Gen.dateTime (Range.constant System.DateTime.MinValue System.DateTime.MaxValue)

        let result = actual |> Gen.toRandom |> Random.run seed0 0 |> Tree.outcome
        expected =! result

    testCase "dateTime shrinks to correct mid-value" <| fun _ ->
        let result =
            property {
                let! actual =
                  Range.constantFrom (System.DateTime (2000, 1, 1)) System.DateTime.MinValue System.DateTime.MaxValue
                  |> Gen.dateTime
                System.DateTime.Now =! actual
            }
            |> Property.report
            |> Report.render
            |> (fun x -> x.Split ([|System.Environment.NewLine|], System.StringSplitOptions.None))
            |> Array.item 1
            |> System.DateTime.Parse
        System.DateTime (2000, 1, 1) =! result

    testCaseNoFable "int64 can create exponentially bounded integer" <| fun _ ->
        Property.check (property {
            let! _ = Gen.int64 (Range.exponentialBounded ())
            return true
        })

    testCaseNoFable "uint64 can create exponentially bounded integer" <| fun _ ->
        Property.check (property {
            let! _ = Gen.uint64 (Range.exponentialBounded ())
            return true
        })
    testCase "apply is chainable" <| fun _ ->
        let _ : Gen<int> =
            Gen.constant (+)
            |> Gen.apply (Gen.constant 1)
            |> Gen.apply (Gen.constant 1)
        ()

    testCase "apply operator works as expected" <| fun _ ->
        let _ : Gen<int> = (+) <!> (Gen.constant 1) <*> (Gen.constant 1)
        ()
]
