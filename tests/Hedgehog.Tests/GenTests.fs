module Hedgehog.Tests.GenTests

open System
open Hedgehog
open Hedgehog.Gen.Operators
open TestDsl


let private testGenPairViaApply gPair =
    // In addition to asserting that Gen.apply is applicative, this code
    // also asserts that the integral shrink tree is the one containing
    // duplicates that existed before PR
    // https://github.com/hedgehogqa/fsharp-hedgehog/pull/239
    // The duplicate-free shrink trees that result from the code in that PR
    // do not work well with the applicative behavior of Gen.apply because
    // some values would shrink more if using the monadic version of
    // Gen.apply, which should never happen.
    let actual =
        seq {
            while true do
                let t = gPair |> Gen.sampleTree 0 1 |> Seq.head
                if Tree.outcome t = (2, 1) then
                    yield t
        } |> Seq.head

    let expected =
        Node ((2, 1), [
            Node ((0, 1), [
                Node ((0, 0), [])
            ])
            Node ((1, 1), [
                Node ((0, 1), [
                    Node ((0, 0), [])
                ])
                Node ((1, 0), [
                    Node ((0, 0), [])
                ])
            ])
            Node ((2, 0), [
                Node ((0, 0), [])
                Node ((1, 0), [
                    Node ((0, 0), [])
                ])
            ])
        ])

    (actual      |> Tree.map (sprintf "%A") |> Tree.render)
    =! (expected |> Tree.map (sprintf "%A") |> Tree.render)
    Expect.isTrue <| Tree.equals actual expected


let genTests = testList "Gen tests" [

    yield! testCases "dateTime creates DateTime instances"
        [ 8; 16; 32; 64; 128; 256; 512 ] <| fun count ->

        let actual =
            (Range.constant DateTime.MinValue DateTime.MaxValue)
            |> Gen.dateTime
            |> Gen.sample 0 count
            |> Seq.toList

        actual
        |> List.distinct
        |> List.length
        =! actual.Length

    testCase "unicode does not return any surrogate" <| fun _ ->
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
        // This is a bad test because essentially the same logic used to
        // implement Gen.dateTime appears in this test. However, keeping it for
        // now.
        let seed = Seed.random ()
        let range =
            Range.constant
                DateTime.MinValue.Ticks
                DateTime.MaxValue.Ticks
        let ticks =
            Random.integral range
            |> Random.run seed 0

        let actual =
            Range.constant DateTime.MinValue DateTime.MaxValue
            |> Gen.dateTime
            |> Gen.toRandom
            |> Random.run seed 0
            |> Tree.outcome

        let expected = DateTime ticks
        actual =! expected

    testCase "dateTime shrinks to correct mid-value" <| fun _ ->
        let actual =
            property {
                let! actual =
                  (Range.constantFrom
                       (DateTime (2000, 1, 1))
                        DateTime.MinValue
                        DateTime.MaxValue)
                  |> Gen.dateTime
                actual =! DateTime.Now
            }
            |> Property.report
            |> Report.render
            |> (fun x -> x.Split ([|Environment.NewLine|], StringSplitOptions.None))
            |> Array.item 1
            |> DateTime.Parse

        actual =! DateTime (2000, 1, 1)

    fableIgnore "int64 can create exponentially bounded integer" <| fun _ ->
        property {
            let! _ = Gen.int64 (Range.exponentialBounded ())
            return true
        }
        |> Property.falseToFailure
        |> Property.check

    fableIgnore "uint64 can create exponentially bounded integer" <| fun _ ->
        property {
            let! _ = Gen.uint64 (Range.exponentialBounded ())
            return true
        }
        |> Property.falseToFailure
        |> Property.check

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
        actual =! 1

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
        }
        |> Property.check

    testCase "apply is applicative via function" <| fun () ->
        let gPair =
            Gen.constant (fun a b -> a, b)
            |> Gen.apply (Range.constant 0 2 |> Gen.int32)
            |> Gen.apply (Range.constant 0 1 |> Gen.int32)
        testGenPairViaApply gPair

    testCase "apply is applicative via CE" <| fun () ->
        let gPair =
            gen {
                let! a = Range.constant 0 2 |> Gen.int32
                and! b = Range.constant 0 1 |> Gen.int32
                return a, b
            }
        testGenPairViaApply gPair

]
