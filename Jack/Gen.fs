#light
namespace Jack

open FSharpx.Collections

/// A generator for values and shrink trees of type 'a.
type Gen<'a> =
    | Gen of Random<Tree<'a>>

module Gen =
    let ofRandom (r : Random<Tree<'a>>) : Gen<'a> =
        Gen r

    let toRandom (Gen r : Gen<'a>) : Random<Tree<'a>> =
        r

    let create (shrink : 'a -> LazyList<'a>) (random : Random<'a>) : Gen<'a> =
        Random.map (Tree.unfold id shrink) random |> ofRandom

    let result (x : 'a) : Gen<'a> =
        Tree.result x |> Random.result |> ofRandom

    let mapRandom (f : Random<Tree<'a>> -> Random<Tree<'b>>) (g : Gen<'a>) : Gen<'b> =
        toRandom g |> f |> ofRandom

    let mapTree (f : Tree<'a> -> Tree<'b>) (g : Gen<'a>) : Gen<'b> =
        mapRandom (Random.map f) g

    let map (f : 'a -> 'b) (g : Gen<'a>) : Gen<'b> =
       mapTree (Tree.map f) g

    let private bindRandom (m : Random<Tree<'a>>) (k : 'a -> Random<Tree<'b>>) : Random<Tree<'b>> =
        Random <| fun seed0 size ->
          let seed1, seed2 =
              Seed.split seed0

          let run (seed : Seed) (random : Random<'x>) : 'x =
              Random.run seed size random

          Tree.bind (run seed1 m) (run seed2 << k)

    let bind (m0 : Gen<'a>) (k0 : 'a -> Gen<'b>) : Gen<'b> =
        bindRandom (toRandom m0) (toRandom << k0) |> ofRandom

    type Builder internal () =
        member __.Return(a) =
            result a
        member __.ReturnFrom(g) =
            g
        member __.Bind(m, k) =
            bind m k
        member __.Zero() =
            result ()

    let private gen = Builder ()

    /// Used to construct generators that depend on the size parameter.
    let sized (f : Size -> Gen<'a>) : Gen<'a> =
        Random.sized (toRandom << f) |> ofRandom

    /// Overrides the size parameter. Returns a generator which uses the
    /// given size instead of the runtime-size parameter.
    let resize (n : int) (g : Gen<'a>) : Gen<'a> =
        mapRandom (Random.resize n) g

    /// Adjust the size parameter, by transforming it with the given
    /// function.
    let scale (f : int -> int) (g : Gen<'a>) : Gen<'a> =
        sized <| fun n ->
            resize (f n) g

    //
    // Combinators
    //

    /// Generates a random element in the given inclusive range.
    let inline choose (lo : ^a) (hi : ^a) : Gen<'a> =
        create (Shrink.towards lo) (Random.choose lo hi)

    /// Randomly selects one of the values in the array.
    /// <i>The input array must be non-empty.</i>
    let elements (xs : List<'a>) : Gen<'a> = gen {
        let! ix = choose 0 (List.length xs - 1)
        return List.item ix xs
    }

    //
    // Sampling
    //

    let sampleTree (size : Size) (count : int) (g : Gen<'a>) : List<Tree<'a>> =
        let seed = Seed.random ()
        toRandom g
        |> Random.replicate count
        |> Random.run seed size

    let sample (size : Size) (count : int) (g : Gen<'a>) : List<'a> =
        sampleTree size count g
        |> List.map Tree.outcome

    /// Run a generator. The size passed to the generator is always 30;
    /// if you want another size then you should explicitly use 'resize'.
    let generateTree (g : Gen<'a>) : Tree<'a> =
        let seed = Seed.random ()
        toRandom g
        |> Random.run seed 30

    let printSample (g : Gen<'a>) : unit =
        let forest = List.take 5 (sampleTree 10 10 g)
        for tree in forest do
            printfn "=== Outcome ==="
            printfn "%A" <| Tree.outcome tree
            printfn "=== Shrinks ==="
            for shrink in Tree.shrinks tree do
                printfn "%A" <| Tree.outcome shrink
            printfn "."

[<AutoOpen>]
module GenBuilder =
    let gen = Gen.Builder ()
