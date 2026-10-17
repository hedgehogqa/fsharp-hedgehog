module Hedgehog.Tests.RandomTests

open System
open Hedgehog
open Hedgehog.FSharp
open TestDsl

let private flip f b a = f a b

let private initialSeed = Seed.from 0UL

// More maps than any stack can hold: .NET needs about a hundred bytes of stack per nested map.
// JavaScript has a much smaller stack, and is slower at creating this many closures, so fewer are enough there.
#if FABLE_COMPILER
let private manyMaps = 100_000
#else
let private manyMaps = 1_000_000
#endif

let randomTests = testList "Random tests" [

    testCase "map applies the function to the generated value" <| fun () ->
        Random.constant 3
        |> Random.map (fun x -> x * 2)
        |> Random.run initialSeed 0
        =! 6

    testCase "map applies the functions in the order of the calls" <| fun () ->
        Random.constant 5
        |> Random.map (fun x -> x * 2)
        |> Random.map (fun x -> x + 1)
        |> Random.run initialSeed 0
        =! 11

    yield! testCases "map applies the functions in the order of the calls, however many there are"
        [ 0; 1; 2; 63; 64; 65; 66; 100; 1000 ] <| fun n ->
        let expected = String.concat "" [ for i in 0 .. n - 1 -> string i ]
        [ 0 .. n - 1 ]
        |> List.fold (fun r i -> Random.map (fun s -> s + string i) r) (Random.constant "")
        |> Random.run initialSeed 0
        =! expected

    testCase "map can change the type" <| fun () ->
        Random.constant 3.14159
        |> Random.map (sprintf "%f")
        |> Random.map String.length
        |> Random.map (fun n -> n % 2 = 0)
        |> Random.run initialSeed 0
        =! true

    yield! testCases "map can change the type after any number of maps"
        [ 0; 1; 63; 64; 65; 66; 1000 ] <| fun n ->
        List.replicate n (fun x -> x + 1)
        |> List.fold (flip Random.map) (Random.constant 0)
        |> Random.map string
        |> Random.map String.length
        |> Random.run initialSeed 0
        =! (string n).Length

    yield! testCases "map keeps the seed and the size, however many maps there are"
        [ 1; 63; 64; 65; 1000 ] <| fun n ->
        List.replicate n (fun (seed, size) -> seed, size + 1)
        |> List.fold (flip Random.map) (Random.variable (fun seed size -> seed, size))
        |> Random.run initialSeed 10
        =! (initialSeed, 10 + n)

    testCase "map id does not change the random" <| fun () ->
        property {
            let! seed =
                Range.constant UInt64.MinValue UInt64.MaxValue
                |> Gen.uint64
            let! size = Range.constant 1 100 |> Gen.int32
            let r = Random.integral (Range.linear 0 1000)
            let actual = r |> Random.map id |> Random.run (Seed.from seed) size
            let expected = r |> Random.run (Seed.from seed) size
            actual =! expected
        }
        |> Property.check

    testCase "map (f >> g) is the same as map f >> map g" <| fun () ->
        property {
            let! seed =
                Range.constant UInt64.MinValue UInt64.MaxValue
                |> Gen.uint64
            let! size = Range.constant 1 100 |> Gen.int32
            let r = Random.integral (Range.linear 0 1000)
            let f x = x + 1
            let g x = x * 2
            let actual = r |> Random.map (f >> g) |> Random.run (Seed.from seed) size
            let expected = r |> Random.map f |> Random.map g |> Random.run (Seed.from seed) size
            actual =! expected
        }
        |> Property.check

    // https://github.com/hedgehogqa/fsharp-hedgehog/issues/289
    // This would overflow the stack if every map wrapped the previous generator in a closure,
    // because running the generator would then recurse once per map.
    testCase "map does not overflow the stack" <| fun () ->
        let n = manyMaps
        List.replicate n (fun x -> x + 1)
        |> List.fold (flip Random.map) (Random.constant 0)
        |> Random.run initialSeed 0
        =! n

    testCase "bind and map work on top of a long chain of maps" <| fun () ->
        let n = manyMaps
        let actual =
            List.replicate n (fun x -> x + 1)
            |> List.fold (flip Random.map) (Random.constant 0)
            |> Random.bind (fun x -> Random.constant (x + 1))
            |> Random.map (fun x -> x * 2)
            |> Random.run initialSeed 0
        actual =! (n + 1) * 2

    testCase "bind splits the seed" <| fun () ->
        let r = Random.integral (Range.constant 0 1000)
        let seed1, seed2 = Seed.split initialSeed
        let expected = Random.run seed1 10 r, Random.run seed2 10 r
        let actual =
            r
            |> Random.bind (fun a -> r |> Random.map (fun b -> a, b))
            |> Random.run initialSeed 10
        actual =! expected

    testCase "sized and resize pass the size on to the mapped random" <| fun () ->
        Random.sized Random.constant
        |> Random.resize 7
        |> Random.map (fun size -> size + 1)
        |> Random.run initialSeed 100
        =! 8

    testCase "tryWith catches an exception thrown by a mapped function" <| fun () ->
        Random.constant 1
        |> Random.map (fun _ -> failwith "boom")
        |> Random.tryWith (fun _ -> Random.constant 42)
        |> Random.run initialSeed 0
        =! 42

    testCase "tryFinally runs the cleanup after the mapped functions" <| fun () ->
        let log = ResizeArray<string>()
        let actual =
            Random.constant 1
            |> Random.map (fun x ->
                log.Add "map"
                x + 1)
            |> Random.tryFinally (fun () -> log.Add "finally")
            |> Random.run initialSeed 0
        actual =! 2
        List.ofSeq log =! [ "map"; "finally" ]
]
