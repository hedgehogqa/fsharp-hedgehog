module Hedgehog.Tests.GenTests

open System
open Hedgehog
open Hedgehog.Gen.Operators
open TestDsl

let genTests = testList "Gen tests" [
    yield! testCases "dateTime creates DateTime instances"
        [ 8; 16; 32; 64; 128; 256; 512 ] <| fun count->

        let actual =
            (Range.constant
                DateTime.MinValue
                DateTime.MaxValue)
            |> Gen.dateTime
            |> Gen.sample 0 count
            |> Seq.toList

        actual
        |> List.distinct
        |> List.length
        =! actual.Length

    testCase "unicode doesn't return any surrogate" <| fun _ ->
        let actual =
            Gen.sample 100 100000 Gen.unicode
            |> Seq.toList
        [] =! List.filter Char.IsSurrogate actual

    yield! testCases "unicode doesn't return any noncharacter"
        [ 65534; 65535 ] <| fun nonchar ->

        let actual =
            Gen.sample 100 100000 Gen.unicode
            |> Seq.toList
        [] =! List.filter (fun ch -> ch = char nonchar) actual

    testCase "dateTime randomly generates value between max and min ticks" <| fun _ ->
        let seed0 = Seed.random ()
        let (seed1, _) = Seed.split seed0
        let range =
            Range.constant
                DateTime.MinValue.Ticks
                DateTime.MaxValue.Ticks
        let ticks =
            Random.integral range
            |> Random.run seed1 0

        let actual =
            Range.constant DateTime.MinValue DateTime.MaxValue
            |> Gen.dateTime
            |> Gen.toRandom
            |> Random.run seed0 0
            |> Tree.outcome

        let expected =
            DateTime ticks
        expected =! actual

    testCase "dateTime shrinks to correct mid-value" <| fun _ ->
        let actual =
            property {
                let! actual =
                  (Range.constantFrom
                       (DateTime (2000, 1, 1))
                        DateTime.MinValue
                        DateTime.MaxValue)
                  |> Gen.dateTime
                DateTime.Now =! actual
            }
            |> Property.report
            |> Report.render
            |> (fun x -> x.Split ([|Environment.NewLine|], StringSplitOptions.None))
            |> Array.item 1
            |> DateTime.Parse

        DateTime (2000, 1, 1) =! actual

    fableIgnore "int64 can create exponentially bounded integer" <| fun _ ->
        Property.check (property {
            let! _ = Gen.int64 (Range.exponentialBounded ())
            return true
        })

    fableIgnore "uint64 can create exponentially bounded integer" <| fun _ ->
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

    testCase "frequency shrink tree is free of duplicates" <| fun _ ->
        let actual =
            [(100, Gen.constant "a")]
            |> Gen.frequency
            |> Gen.toRandom
            |> Random.run (Seed.from 0UL) 0
            |> Tree.toSeq
            |> Seq.length
        1 =! actual

    testCase "frequency shrink tree is balanced" <| fun _ ->
        let isBalanced a subtrees =
            let subtreesCount = subtrees |> Seq.length
            let depth = Node (true, subtrees) |> Tree.depth
            let difference = subtreesCount - depth |> abs
            difference <= 1
        property {
            let! seed =
                Range.constant UInt64.MinValue UInt64.MaxValue
                |> Gen.uint64
            let isBalanced =
                (1, Gen.constant "a")
                |> List.replicate 16
                |> Gen.frequency
                |> Gen.toRandom
                |> Random.run (Seed.from seed) 0
                |> Tree.mapWithSubtrees isBalanced
                |> Tree.cata (Seq.fold (&&))
            Expect.isTrue isBalanced
        } |> Property.check
]
