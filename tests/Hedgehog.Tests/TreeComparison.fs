/// Helpers for the tests that compare the shrink trees of a generator with those of a reference.
module internal Hedgehog.Tests.TreeComparison

open System
open Hedgehog
open Hedgehog.FSharp

/// Draws the tree of a generator for a seed and a size, so that it is the same every time.
let draw (seed : int) (size : Size) (gen : Gen<'a>) : Tree<'a> =
    Random.run (Seed.from (uint64 seed)) size (Gen.toRandom gen)

/// Where two trees differ, looking only at the first 'width' shrinks of every node down to the given depth.
let rec difference (path : int list) (depth : int) (width : int) (expected : Tree<'a>) (actual : Tree<'a>) : string option =
    let x = Tree.outcome expected
    let y = Tree.outcome actual
    if x <> y then
        Some (sprintf "the outcome at %A is %A but %A is expected" (List.rev path) y x)
    elif depth = 0 then
        None
    else
        let expectedShrinks = Tree.shrinks expected |> Seq.truncate width |> Seq.toList
        let actualShrinks = Tree.shrinks actual |> Seq.truncate width |> Seq.toList
        if List.length actualShrinks <> List.length expectedShrinks then
            Some (sprintf "there are %d shrinks at %A but %d are expected" (List.length actualShrinks) (List.rev path) (List.length expectedShrinks))
        else
            List.zip expectedShrinks actualShrinks
            |> List.indexed
            |> List.tryPick (fun (i, (e, a)) -> difference (i :: path) (depth - 1) width e a)

/// Where two trees differ, looking at all the shrinks of every node.
let wholeDifference (expected : Tree<'a>) (actual : Tree<'a>) : string option =
    difference [] Int32.MaxValue Int32.MaxValue expected actual

/// The first difference found for one of the seeds.
let firstDifference (seeds : int list) (find : int -> string option) : string option =
    seeds |> List.tryPick (fun seed -> find seed |> Option.map (sprintf "seed %d: %s" seed))

/// A size that is different for every seed and goes through all of them, from 1 to 100.
let sizeOf (seed : int) : Size =
    1 + seed % 100
