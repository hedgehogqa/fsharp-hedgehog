module Hedgehog.Tests.ArrayCoreTests

open System
open Hedgehog
open Hedgehog.FSharp
open TestDsl
open TreeComparison

// 'Gen.list', 'Gen.array' and 'Gen.seq' are built on the array core. The values and the shrinks that they generate
// for a seed must be the same as those of the generators on lists that they replaced (see 'ListReference'),
// otherwise the rechecks that were recorded before stop working.

let private element : Gen<int> =
    Gen.int32 (Range.constant 0 100)

/// The values of these elements are larger the larger the size is.
let private sizedElement : Gen<int> =
    Gen.int32 (Range.linear 0 100)

let private elementTree (value : int) : Tree<int> =
    Shrink.createTree 0 value

let rec private combinations (alphabet : int list) (length : int) : int list list =
    if length = 0 then
        [ [] ]
    else
        [ for value in alphabet do
            for rest in combinations alphabet (length - 1) do
                value :: rest ]

/// The first six shrinks of every node, down to the third level, for the given number of seeds.
let private sameShrinksOf
        (seedCount : int)
        (element : Gen<int>)
        (description : string)
        (kind : string)
        (range : Range<int>)
        (reference : Range<int> -> Gen<int> -> Gen<'c>)
        (actual : Range<int> -> Gen<int> -> Gen<'c>)
        (toList : 'c -> int list) =
    testCase (sprintf "%s shrinks like the generator on lists, %s" kind description) <| fun _ ->
        let referenceGen = reference range element
        let actualGen = actual range element
        let found =
            firstDifference [ 0 .. seedCount - 1 ] (fun seed ->
                let size = sizeOf seed
                let expected = draw seed size referenceGen |> Tree.map toList
                let actual = draw seed size actualGen |> Tree.map toList
                difference [] 3 6 expected actual)
        found =! None

/// The same for 500 seeds, with the elements that do not depend on the size.
let private sameShrinks
        (kind : string)
        (rangeName : string)
        (range : Range<int>)
        (reference : Range<int> -> Gen<int> -> Gen<'c>)
        (actual : Range<int> -> Gen<int> -> Gen<'c>)
        (toList : 'c -> int list) =
    sameShrinksOf 500 element (sprintf "%s range" rangeName) kind range reference actual toList

/// Every shrink, all the way down, for small ranges where the whole tree is small.
let private sameTrees (rangeName : string) (range : Range<int>) =
    testCase (sprintf "the whole trees of Gen.list, Gen.array and Gen.seq are those of the generator on lists, %s range" rangeName) <| fun _ ->
        let small = Gen.int32 (Range.constant 0 2)
        let referenceList = ListReference.list range small
        let actualList = Gen.list range small
        let actualArray = Gen.array range small
        let actualSeq = Gen.seq range small
        let found =
            firstDifference [ 0 .. 99 ] (fun seed ->
                let size = sizeOf seed
                let expected = draw seed size referenceList
                [ "list", wholeDifference expected (draw seed size actualList)
                  "array", wholeDifference expected (draw seed size actualArray |> Tree.map Array.toList)
                  "seq", wholeDifference expected (draw seed size actualSeq |> Tree.map Seq.toList) ]
                |> List.tryPick (fun (kind, found) -> found |> Option.map (sprintf "%s: %s" kind)))
        found =! None

/// Every list of the given length out of the alphabet, for every minimum length.
let private sameAsSequenceList (length : int) (alphabet : int list) =
    testCase (sprintf "sequence gives every shrink of every list of %d elements out of %A, for every minimum length" length alphabet) <| fun _ ->
        let found =
            [ for values in combinations alphabet length do
                for minLength in 0 .. length do
                    let trees = values |> List.map elementTree
                    let expected =
                        Shrink.sequenceList trees
                        |> Tree.filter (fun xs -> List.length xs >= minLength)
                    let actual = ArrayCore.sequence minLength List.ofArray (Array.ofList trees)
                    match wholeDifference expected actual with
                    | Some d -> yield sprintf "%A with the minimum length of %d: %s" values minLength d
                    | None -> () ]
        found =! []

let rec private noShorterThan (minLength : int) (depth : int) (width : int) (tree : Tree<int list>) : bool =
    List.length (Tree.outcome tree) >= minLength
    && (depth = 0
        || Tree.shrinks tree
           |> Seq.truncate width
           |> Seq.forall (noShorterThan minLength (depth - 1) width))

let private throws (f : unit -> unit) : bool =
    try
        f ()
        false
    with _ ->
        true

#if !FABLE_COMPILER
let private throwsArgumentException (f : unit -> unit) : bool =
    try
        f ()
        false
    with
    | :? ArgumentException -> true
    | _ -> false
#endif

/// Properties, the recheck data that was recorded for them with the generators on lists, and what they shrink to.
let private recorded : (string * Property<unit> * string * (obj -> unit)) list =
    let sumBelow150 minLength =
        property {
            let! xs = Gen.list (Range.linear minLength 20) element
            return List.sum xs < 150
        }
        |> Property.falseToFailure

    let allBelow90 =
        property {
            let! xs = Gen.array (Range.linear 0 20) element
            return Array.forall (fun x -> x < 90) xs
        }
        |> Property.falseToFailure

    let shorterThan7 =
        property {
            let! xs = Gen.seq (Range.linear 0 20) element
            return Seq.length xs < 7
        }
        |> Property.falseToFailure

    let noZ =
        property {
            let! s = Gen.string (Range.linear 0 30) Gen.alpha
            return not (s.Contains "z")
        }
        |> Property.falseToFailure

    let fewerThan12 =
        property {
            let! xss = Gen.list (Range.linear 0 8) (Gen.list (Range.linear 0 8) (Gen.int32 (Range.constant 0 9)))
            return List.sumBy List.length xss < 12
        }
        |> Property.falseToFailure

    let isList (expected : int list) (value : obj) = unbox<int list> value =! expected
    let isArray (expected : int[]) (value : obj) = unbox<int[]> value =! expected
    let isSeq (expected : int list) (value : obj) = unbox<seq<int>> value |> Seq.toList =! expected
    let isString (expected : string) (value : obj) = unbox<string> value =! expected
    let isLists (expected : int list list) (value : obj) = unbox<int list list> value =! expected

    [ "list, the sum is below 150, seed 1", sumBelow150 0, "11_15721441921077876724_13321890633601507531_6-8-8", isList [ 54; 96 ]
      "list, the sum is below 150, seed 2", sumBelow150 0, "17_11950373334259027979_13068789111551781425_5-5-6-6", isList [ 7; 75; 68 ]
      "list, the sum is below 150, seed 3", sumBelow150 0, "16_1088024433781186896_3577560834400624783_8-10", isList [ 75; 54; 21 ]
      "list of at least 3, the sum is below 150, seed 1", sumBelow150 3, "1_16204969531660614133_5610259966137620355_3-4-4", isList [ 56; 93; 1 ]
      "list of at least 3, the sum is below 150, seed 2", sumBelow150 3, "1_16338323044187625535_2432515666882051939_0-2-2-5-5", isList [ 0; 54; 96 ]
      "array, all are below 90, seed 1", allBelow90, "11_15721441921077876724_13321890633601507531_1-5", isArray [| 90 |]
      "array, all are below 90, seed 2", allBelow90, "19_15586536279145145870_7173342554290493371_1-7", isArray [| 90 |]
      "seq, shorter than 7, seed 1", shorterThan7, "41_8159875476836680732_8591561771547071455_7-10-10-10-10-10-10-10", isSeq [ 0; 0; 0; 0; 0; 0; 0 ]
      "seq, shorter than 7, seed 2", shorterThan7, "39_17237174295672711967_6627366803204445163_10-10-10-10-10-10-10", isSeq [ 0; 0; 0; 0; 0; 0; 0 ]
      "string, there is no z, seed 1", noZ, "32_1911417039179348576_2926946523224549775_2-2-2", isString "z"
      "string, there is no z, seed 2", noZ, "29_5617280372923958988_7401802914847361625_2-2-1", isString "z"
      "lists of lists, fewer than 12 elements, seed 1", fewerThan12, "60_1048248564456421564_11017300701383858047_10-17-17-17-17-20-20-27-27", isLists [ [ 0; 0 ]; [ 0; 0; 0; 0 ]; [ 0; 0 ]; [ 0; 0; 0; 0 ] ]
      "lists of lists, fewer than 12 elements, seed 2", fewerThan12, "65_1793033688935072442_5633628224352526089_5-8-8-8-15-15-15-15-23-23-23-23-23", isLists [ [ 0; 0; 0 ]; [ 0; 0; 0; 0 ]; [ 0; 0; 0; 0; 0 ] ] ]

let arrayCoreTests = testList "Array core tests" [

    // The checks of the prototype: 500 seeds in each of three ranges, the first six shrinks of every node, down to
    // the third level.
    for rangeName, range in
        [ "constant", Range.constant 0 8
          "linear from 0", Range.linear 0 12
          "linear with a minimum", Range.linear 3 15 ] do
        sameShrinks "Gen.list" rangeName range ListReference.list Gen.list id
        sameShrinks "Gen.array" rangeName range ListReference.array Gen.array Array.toList
        sameShrinks "Gen.seq" rangeName range ListReference.seq Gen.seq Seq.toList

    // Ranges whose lower bound is after the upper one, and ranges whose lower bound depends on the size.
    for rangeName, range in
        [ "reversed", Range.constant 6 2
          "linear and reversed", Range.linear 10 0
          "linear from an origin", Range.linearFrom 5 0 10
          "exponential from an origin", Range.exponentialFrom 4 0 12 ] do
        sameShrinksOf 200 element (sprintf "%s range" rangeName) "Gen.list" range ListReference.list Gen.list id
        sameShrinksOf 200 element (sprintf "%s range" rangeName) "Gen.array" range ListReference.array Gen.array Array.toList
        sameShrinksOf 200 element (sprintf "%s range" rangeName) "Gen.seq" range ListReference.seq Gen.seq Seq.toList

    // Elements that depend on the size get the size that the collection gets.
    sameShrinksOf 200 sizedElement "elements that depend on the size" "Gen.list" (Range.linear 0 12) ListReference.list Gen.list id
    sameShrinksOf 200 sizedElement "elements that depend on the size" "Gen.array" (Range.linear 0 12) ListReference.array Gen.array Array.toList
    sameShrinksOf 200 sizedElement "elements that depend on the size" "Gen.seq" (Range.linear 0 12) ListReference.seq Gen.seq Seq.toList

    // Whole trees of small lists.
    for rangeName, range in
        [ "constant", Range.constant 0 3
          "constant with a minimum", Range.constant 2 3
          "linear from 0", Range.linear 0 3
          "linear with a minimum", Range.linear 1 3
          "singleton", Range.singleton 3 ] do
        sameTrees rangeName range

    // Whole trees of every small list of trees.
    sameAsSequenceList 0 [ 0; 1; 2 ]
    sameAsSequenceList 1 [ 0; 1; 2 ]
    sameAsSequenceList 2 [ 0; 1; 2 ]
    sameAsSequenceList 3 [ 0; 1; 2 ]
    sameAsSequenceList 4 [ 0; 1 ]

    testCase "sequence filters by the minimum length like Tree.filter does, for larger lists" <| fun _ ->
        let found =
            [ for values in [ [ 3; 1; 2; 0; 2 ]; [ 1; 1; 1; 1; 1 ]; [ 0; 0; 0; 0; 0; 0 ]; [ 3; 0; 3; 0; 3; 0; 3 ] ] do
                for minLength in 0 .. values.Length do
                    let trees = values |> List.map elementTree
                    let expected =
                        Shrink.sequenceList trees
                        |> Tree.filter (fun xs -> List.length xs >= minLength)
                    let actual = ArrayCore.sequence minLength List.ofArray (Array.ofList trees)
                    // The whole trees are too large, so look at the first shrinks only.
                    match difference [] 4 5 expected actual with
                    | Some d -> yield sprintf "%A with the minimum length of %d: %s" values minLength d
                    | None -> () ]
        found =! []

    testCase "sequence gives every shrink of the first levels of larger lists" <| fun _ ->
        // All the shrinks of a node are compared, however many there are, but only the first levels of the trees.
        let found =
            [ for values, depth in
                [ [ 0; 1; 2; 3; 4 ], 3
                  [ 4; 4; 4; 4; 4 ], 2
                  [ 1; 0; 2; 0; 3; 0 ], 2
                  [ 3; 2; 1; 0; 1; 2; 3 ], 2
                  [ 4; 3; 2; 1; 0; 1; 2; 3 ], 2 ] do
                for minLength in [ 0; 1; values.Length / 2; values.Length - 1; values.Length ] do
                    let trees = values |> List.map elementTree
                    let expected =
                        Shrink.sequenceList trees
                        |> Tree.filter (fun xs -> List.length xs >= minLength)
                    let actual = ArrayCore.sequence minLength List.ofArray (Array.ofList trees)
                    match difference [] depth Int32.MaxValue expected actual with
                    | Some d -> yield sprintf "%A with the minimum length of %d: %s" values minLength d
                    | None -> () ]
        found =! []

    testCase "no shrink is shorter than the lower bound of the range" <| fun _ ->
        let range = Range.linear 2 10
        let lists = Gen.list range element
        let arrays = Gen.array range element |> Gen.map Array.toList
        let seqs = Gen.seq range element |> Gen.map Seq.toList
        for seed in 0 .. 59 do
            let size = sizeOf seed
            let minLength = Range.lowerBound size range
            for gen in [ lists; arrays; seqs ] do
                Expect.isTrue (noShorterThan minLength 3 8 (draw seed size gen))

    testCase "the empty range gives the empty collection that does not shrink" <| fun _ ->
        let range = Range.singleton 0
        let lists = draw 1 30 (Gen.list range element)
        let arrays = draw 1 30 (Gen.array range element)
        let seqs = draw 1 30 (Gen.seq range element)
        Tree.outcome lists =! []
        Tree.outcome arrays =! [||]
        Tree.outcome seqs |> Seq.toList =! []
        Expect.isTrue (Tree.shrinks lists |> Seq.isEmpty)
        Expect.isTrue (Tree.shrinks arrays |> Seq.isEmpty)
        Expect.isTrue (Tree.shrinks seqs |> Seq.isEmpty)

    testCase "a negative length is an error" <| fun _ ->
        let range = Range.constant -3 -1
#if !FABLE_COMPILER
        // The generator on lists failed with an ArgumentException from Seq.take. In Fable, Seq.take gives nothing instead.
        Expect.isTrue (throwsArgumentException (fun () -> draw 1 30 (ListReference.list range element) |> ignore))
        Expect.isTrue (throwsArgumentException (fun () -> draw 1 30 (Gen.list range element) |> ignore))
        Expect.isTrue (throwsArgumentException (fun () -> draw 1 30 (Gen.array range element) |> ignore))
        Expect.isTrue (throwsArgumentException (fun () -> draw 1 30 (Gen.seq range element) |> ignore))
#else
        Expect.isTrue (throws (fun () -> draw 1 30 (Gen.list range element) |> ignore))
        Expect.isTrue (throws (fun () -> draw 1 30 (Gen.array range element) |> ignore))
        Expect.isTrue (throws (fun () -> draw 1 30 (Gen.seq range element) |> ignore))
#endif

    testCase "Gen.seq generates F# lists, like it did" <| fun _ ->
        // The runtime type decides how a counterexample is printed in a report.
        let tree = draw 1 30 (Gen.seq (Range.singleton 3) element)
        Expect.isTrue ((box (Tree.outcome tree)) :? (int list))
        Expect.isTrue (Tree.shrinks tree |> Seq.forall (fun shrink -> (box (Tree.outcome shrink)) :? (int list)))

    testCase "the shrinks of the elements are computed lazily, like in the generator on lists" <| fun _ ->
        // Any length can be drawn and none of the shrinks is filtered out, so look for a seed that gives three elements.
        let range = Range.constant 0 3
        let seed =
            [ 0 .. 99 ]
            |> List.find (fun seed -> List.length (Tree.outcome (draw seed 30 (Gen.list range element))) = 3)
        let counts (generator : Range<int> -> Gen<int> -> Gen<int list>) =
            let enumerated = ref 0
            let counting =
                Random.constant ()
                |> Random.map (fun () ->
                    Node (5, seq {
                        enumerated.Value <- enumerated.Value + 1
                        yield Tree.singleton 0
                    }))
                |> Gen.ofRandom
            let tree = draw seed 30 (generator range counting)
            let afterDraw = enumerated.Value
            let first = Tree.shrinks tree |> Seq.head |> Tree.outcome
            let afterFirst = enumerated.Value
            let shrinkCount = Tree.shrinks tree |> Seq.length
            afterDraw, afterFirst, enumerated.Value, first, shrinkCount
        let afterDraw, afterFirst, _, first, _ = counts Gen.list
        // Neither drawing nor the shrink to the empty list looks at the shrinks of the elements.
        afterDraw =! 0
        afterFirst =! 0
        first =! []
        counts Gen.list =! counts ListReference.list

    testCase "the arrays of the outcomes are not shared between the nodes" <| fun _ ->
        let gen = Gen.array (Range.singleton 4) (Gen.int32 (Range.constant 0 9))
        let untouched = draw 7 30 gen |> Tree.map Array.toList
        let tree = draw 7 30 gen
        let overwrite (xs : int[]) =
            for i in 0 .. xs.Length - 1 do
                xs.[i] <- -1
        // A collection is free to keep the array, and the user may write over it.
        overwrite (Tree.outcome tree)
        for shrink in Tree.shrinks tree |> Seq.truncate 10 do
            overwrite (Tree.outcome shrink)
        let expectedShrinks = Tree.shrinks untouched |> Seq.toList
        let actualShrinks = Tree.shrinks tree |> Seq.map (Tree.map Array.toList) |> Seq.toList
        List.length actualShrinks =! List.length expectedShrinks
        for expected, actual in List.zip expectedShrinks actualShrinks do
            difference [] 2 20 expected actual =! None
        // Every walk builds new arrays.
        let first = Tree.shrinks tree |> Seq.map Tree.outcome |> Seq.toList
        let second = Tree.shrinks tree |> Seq.map Tree.outcome |> Seq.toList
        for a, b in List.zip first second do
            Expect.isFalse (obj.ReferenceEquals (a, b))
            a =! b
        // And the shrinks of one walk do not share an array either, nor with the array of the root.
        let root = Tree.outcome tree
        for i, a in List.indexed first do
            Expect.isFalse (obj.ReferenceEquals (a, root))
            for b in List.skip (i + 1) first do
                Expect.isFalse (obj.ReferenceEquals (a, b))

    testCase "a long collection shrinks without a stack overflow" <| fun _ ->
        let length = 20000
        let tree = ArrayCore.sequence 0 id (Array.create length (Tree.singleton 0))
        (Tree.outcome tree).Length =! length
        // The empty array first, then the two halves, then the four quarters that are left out one by one.
        let lengths = Tree.shrinks tree |> Seq.truncate 7 |> Seq.map (fun shrink -> (Tree.outcome shrink).Length) |> Seq.toList
        lengths =! [ 0; 10000; 10000; 15000; 15000; 15000; 15000 ]

    for name, property, recheckData, check in recorded do
        testCase (sprintf "Property.reportRecheck follows the recheck data that was recorded with the generators on lists: %s" name) <| fun _ ->
            let report = Property.reportRecheck recheckData property
            match report.Status with
            | Failed failure ->
                let values =
                    Journal.eval failure.Journal
                    |> Seq.choose (function GeneratedValue value -> Some value | _ -> None)
                    |> Seq.toList
                check (List.exactlyOne values)
            | _ ->
                failwith "The recheck should have failed."
]
