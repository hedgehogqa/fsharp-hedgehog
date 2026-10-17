/// The generators of collections as they were before they got the array core: 'list' is built on F# lists and
/// 'Shrink.sequenceList', 'array' and 'seq' are mapped from it.
///
/// They are the reference that 'Gen.list', 'Gen.array' and 'Gen.seq' are compared with, because the values and the
/// shrinks that they generate for a seed must not change: a recorded recheck follows the shrinks by their position.
/// The benchmarks use them as the baseline.
module Hedgehog.Tests.ListReference

open Hedgehog
open Hedgehog.FSharp

let list (range : Range<int>) (gen : Gen<'a>) : Gen<List<'a>> =
    let sequence minLength trees =
        trees
        |> Seq.toList
        |> Shrink.sequenceList
        |> Tree.filter (fun list -> List.length list >= minLength)

    let replicate minLength times =
        Gen.toRandom gen
        |> Random.replicate times
        |> Random.map (sequence minLength)

    let sizedList size =
        let minLength = Range.lowerBound size range
        range
        |> Random.integral
        |> Random.bind (replicate minLength)

    Random.sized sizedList
    |> Gen.ofRandom

let array (range : Range<int>) (gen : Gen<'a>) : Gen<array<'a>> =
    list range gen |> Gen.map Array.ofList

let seq (range : Range<int>) (gen : Gen<'a>) : Gen<seq<'a>> =
    list range gen |> Gen.map Seq.ofList
