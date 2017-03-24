namespace Hedgehog

open FSharpx.Collections
open Hedgehog.Numeric

/// A generator for values and shrink trees of type 'a.
type Gen<'a> =
    | Gen of Random<Tree<'a>>

module Gen =
    let ofRandom (r : Random<Tree<'a>>) : Gen<'a> =
        Gen r

    let toRandom (Gen r : Gen<'a>) : Random<Tree<'a>> =
        r

    let delay (f : unit -> Gen<'a>) : Gen<'a> =
        Random.delay (toRandom << f) |> ofRandom

    let tryFinally (m : Gen<'a>) (after : unit -> unit) : Gen<'a> =
        Random.tryFinally (toRandom m) after |> ofRandom

    let tryWith (m : Gen<'a>) (k : exn -> Gen<'a>) : Gen<'a> =
        Random.tryWith (toRandom m) (toRandom << k) |> ofRandom

    let create (shrink : 'a -> LazyList<'a>) (random : Random<'a>) : Gen<'a> =
        Random.map (Tree.unfold id shrink) random |> ofRandom

    let constant (x : 'a) : Gen<'a> =
        Tree.singleton x |> Random.constant |> ofRandom

    let private bindRandom (m : Random<Tree<'a>>) (k : 'a -> Random<Tree<'b>>) : Random<Tree<'b>> =
        Random <| fun seed0 size ->
          let seed1, seed2 =
              Seed.split seed0

          let run (seed : Seed) (random : Random<'x>) : 'x =
              Random.run seed size random

          Tree.bind (run seed1 m) (run seed2 << k)

    let bind (m0 : Gen<'a>) (k0 : 'a -> Gen<'b>) : Gen<'b> =
        bindRandom (toRandom m0) (toRandom << k0) |> ofRandom

    let apply (gf : Gen<'a -> 'b>) (gx : Gen<'a>) : Gen<'b> =
        bind gf <| fun f ->
        bind gx <| fun x ->
        constant (f x)

    let mapRandom (f : Random<Tree<'a>> -> Random<Tree<'b>>) (g : Gen<'a>) : Gen<'b> =
        toRandom g |> f |> ofRandom

    let mapTree (f : Tree<'a> -> Tree<'b>) (g : Gen<'a>) : Gen<'b> =
        mapRandom (Random.map f) g

    let map (f : 'a -> 'b) (g : Gen<'a>) : Gen<'b> =
        mapTree (Tree.map f) g

    let map2 (f : 'a -> 'b -> 'c) (gx : Gen<'a>) (gy : Gen<'b>) : Gen<'c> =
        bind gx <| fun x ->
        bind gy <| fun y ->
        constant (f x y)

    let map3 (f : 'a -> 'b -> 'c -> 'd) (gx : Gen<'a>) (gy : Gen<'b>) (gz : Gen<'c>) : Gen<'d> =
        bind gx <| fun x ->
        bind gy <| fun y ->
        bind gz <| fun z ->
        constant (f x y z)

    let map4 (f : 'a -> 'b -> 'c -> 'd -> 'e) (gx : Gen<'a>) (gy : Gen<'b>) (gz : Gen<'c>) (gw : Gen<'d>) : Gen<'e> =
        bind gx <| fun x ->
        bind gy <| fun y ->
        bind gz <| fun z ->
        bind gw <| fun w ->
        constant (f x y z w)

    let zip (gx : Gen<'a>) (gy : Gen<'b>) : Gen<'a * 'b> =
        map2 (fun x y -> x, y) gx gy

    let zip3 (gx : Gen<'a>) (gy : Gen<'b>) (gz : Gen<'c>) : Gen<'a * 'b * 'c> =
        map3 (fun x y z -> x, y, z) gx gy gz

    let zip4 (gx : Gen<'a>) (gy : Gen<'b>) (gz : Gen<'c>) (gw : Gen<'d>) : Gen<'a * 'b * 'c * 'd> =
        map4 (fun x y z w -> x, y, z, w) gx gy gz gw

    let tuple  (g : Gen<'a>) : Gen<'a * 'a> =
        zip g g

    let tuple3 (g : Gen<'a>) : Gen<'a * 'a * 'a> =
        zip3 g g g

    let tuple4 (g : Gen<'a>) : Gen<'a * 'a * 'a * 'a> =
        zip4 g g g g

    type Builder internal () =
        let rec loop p m =
            if p () then
                bind m (fun _ -> loop p m)
            else
                constant ()

        member __.Return(a) =
            constant a
        member __.ReturnFrom(g) =
            g
        member __.Bind(m, k) =
            bind m k
        member __.For(xs, k) =
            let xse = (xs :> seq<'a>).GetEnumerator ()
            using xse <| fun xse ->
                let mv = xse.MoveNext
                let kc = delay (fun () -> k xse.Current)
                loop mv kc
        member __.Combine(m, n) =
            bind m (fun () -> n)
        member __.Delay(f) =
            delay f
        member __.Zero() =
            constant ()

    let private gen = Builder ()

    //
    // Combinators - Shrinking
    //

    /// Prevent a 'Gen' from shrinking.
    let noShrink (g : Gen<'a>) : Gen<'a> =
        let drop (Node (x, _)) =
            Node (x, LazyList.empty)
        mapTree drop g

    /// Apply an additional shrinker to all generated trees.
    let shrinkLazy (f : 'a -> LazyList<'a>) (g : Gen<'a>) : Gen<'a> =
        mapTree (Tree.expand f) g

    /// Apply an additional shrinker to all generated trees.
    let shrink (f : 'a -> List<'a>) (g : Gen<'a>) : Gen<'a>  =
        shrinkLazy (LazyList.ofList << f) g

    //
    // Combinators - Size
    //

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
    // Combinators - Numeric
    //

    /// Generates a random number in the given inclusive range.
    let inline range (lo : ^a) (hi : ^a) : Gen<'a> =
        create (Shrink.towards lo) (Random.range lo hi)

    /// Generates a random number from the whole range of the numeric type.
    let inline bounded () : Gen<'a> =
        let lo = minValue ()
        let hi = maxValue ()
        let zero = LanguagePrimitives.GenericZero

        create (Shrink.towards zero) (Random.range lo hi)

    /// Generates a random number in the given inclusive range, but smaller
    /// numbers are generated more often than bigger ones.
    let inline sizedRange (lo : ^a) (hi : ^a) : Gen<'a> =
        create (Shrink.towards lo) (Random.sizedRange lo hi)

    /// Generates a random number from the whole range of the numeric type, but
    /// smaller numbers are generated more often than bigger ones.
    let inline sizedBounded () : Gen<'a> =
        let lo = minValue ()
        let hi = maxValue ()
        let zero = LanguagePrimitives.GenericZero

        create (Shrink.towards zero) (Random.sizedRange lo hi)

    //
    // Combinators - Choice
    //

    let private crashEmpty (arg : string) : 'b =
        invalidArg arg (sprintf "'%s' must have at least one element" arg)

    /// Randomly selects one of the values in the list.
    /// <i>The input list must be non-empty.</i>
    let item (xs0 : seq<'a>) : Gen<'a> = gen {
        let xs = Array.ofSeq xs0
        if Array.isEmpty xs then
            return crashEmpty "xs"
        else
            let! ix = range 0 (Array.length xs - 1)
            return Array.item ix xs
    }

    /// Uses a weighted distribution to randomly select one of the gens in the list.
    /// <i>The input list must be non-empty.</i>
    let frequency (xs0 : seq<int * Gen<'a>>) : Gen<'a> = gen {
        let xs =
            List.ofSeq xs0

        let total =
            List.sum (List.map fst xs)

        let rec pick n = function
            | [] ->
                crashEmpty "xs"
            | (k, y) :: ys ->
                if n <= k then
                    y
                else
                    pick (n - k) ys

        let! n = range 1 total
        return! pick n xs
    }

    /// Randomly selects one of the gens in the list.
    /// <i>The input list must be non-empty.</i>
    let choice (xs0 : seq<Gen<'a>>) : Gen<'a> = gen {
        let xs = Array.ofSeq xs0
        if Array.isEmpty xs then
            return crashEmpty "xs" xs
        else
            let! ix = range 0 (Array.length xs - 1)
            return! Array.item ix xs
    }

    /// Randomly selects from one of the gens in either the non-recursive or the
    /// recursive list. When a selection is made from the recursive list, the size
    /// is halved. When the size gets to one or less, selections are no longer made
    /// from the recursive list.
    /// <i>The first argument (i.e. the non-recursive input list) must be non-empty.</i>
    let choiceRec (nonrecs : seq<Gen<'a>>) (recs : seq<Gen<'a>>) : Gen<'a> =
        sized <| fun n ->
            if n <= 1 then
                choice nonrecs
            else
                let halve x = x / 2
                choice <| Seq.append nonrecs (Seq.map (scale halve) recs)

    //
    // Combinators - Conditional
    //

    /// More or less the same logic as suchThatMaybe from QuickCheck, except
    /// modified to ensure that the shrinks also obey the predicate.
    let private tryFilterRandom (p : 'a -> bool) (r0 : Random<Tree<'a>>) : Random<Option<Tree<'a>>> =
        let rec tryN k = function
            | 0 ->
                Random.constant None
            | n ->
                let r = Random.resize (2 * k + n) r0
                Random.bind r <| fun x ->
                    if p (Tree.outcome x) then
                        Tree.filter p x |> Some |> Random.constant
                    else
                        tryN (k + 1) (n - 1)

        Random.sized (tryN 0 << max 1)

    /// Generates a value that satisfies a predicate.
    let filter (p : 'a -> bool) (g : Gen<'a>) : Gen<'a> =
        let rec loop () =
            Random.bind (toRandom g |> tryFilterRandom p) <| function
                | None ->
                    Random.sized <| fun n ->
                        Random.resize (n + 1) (Random.delay loop)
                | Some x ->
                    Random.constant x

        loop ()
        |> ofRandom

    /// Tries to generate a value that satisfies a predicate.
    let tryFilter (p : 'a -> bool) (g : Gen<'a>) : Gen<'a option> =
        ofRandom << Random.bind (toRandom g |> tryFilterRandom p) <| function
            | None ->
                None |> Tree.singleton |> Random.constant
            | Some x ->
                Tree.map Some x |> Random.constant

    /// Runs an option generator until it produces a 'Some'.
    let some (g : Gen<'a option>) : Gen<'a> =
        bind (filter Option.isSome g) <| function
        | Some x ->
            constant x
        | None ->
            invalidOp "internal error, unexpected None"

    //
    // Combinators - Collections
    //

    /// Generates a 'None' part of the time.
    let option (g : Gen<'a>) : Gen<'a option> =
        sized <| fun n ->
            frequency [
                2, constant None
                1 + n, map Some g
            ]

    let private atLeast (n : int) (xs : List<'a>) : bool =
        n = 0 || not (List.isEmpty (List.skip (n - 1) xs))

    /// Generates a list between 'n' and 'm' in length.
    let list' (n : int) (m : int) (g : Gen<'a>) : Gen<List<'a>> =
        ofRandom <| random {
            let! k = Random.range n m
            let! xs = Random.replicate k (toRandom g)
            return Shrink.sequenceList xs
                |> Tree.filter (atLeast (min n m))
        }

    /// Generates a list of random length. The maximum length depends on the
    /// size parameter.
    let list (g : Gen<'a>) : Gen<List<'a>> =
        sized (fun size -> list' 0 size g)

    /// Generates a non-empty list of random length. The maximum length depends
    /// on the size parameter.
    let list1 (g : Gen<'a>) : Gen<List<'a>> =
        sized (fun size -> list' 1 size g)

    /// Generates an array between 'n' and 'm' in length.
    let array' (n : int) (m : int) (g : Gen<'a>) : Gen<array<'a>> =
        list' n m g |> map Array.ofList

    /// Generates an array of random length. The maximum length depends on the
    /// size parameter.
    let array (g : Gen<'a>) : Gen<array<'a>> =
        list g |> map Array.ofList

    /// Generates a non-empty array of random length. The maximum length
    /// depends on the size parameter.
    let array1 (g : Gen<'a>) : Gen<array<'a>> =
        list1 g |> map Array.ofList

    /// Generates a sequence between 'n' and 'm' in length.
    let seq' (n : int) (m : int) (g : Gen<'a>) : Gen<seq<'a>> =
        list' n m g |> map Seq.ofList

    /// Generates a sequence of random length. The maximum length depends on
    /// the size parameter.
    let seq (g : Gen<'a>) : Gen<seq<'a>> =
        list g |> map Seq.ofList

    /// Generates a non-empty sequence of random length. The maximum length
    /// depends on the size parameter.
    let seq1 (g : Gen<'a>) : Gen<seq<'a>> =
        list1 g |> map Seq.ofList

    //
    // Combinators - Characters
    //

    // Generates a random character in the specified range.
    let charRange (lo : char) (hi : char) : Gen<char> =
        range (int lo) (int hi) |> map char

    /// Generates a random character.
    let char : Gen<char> =
        let lo = System.Char.MinValue
        let hi = System.Char.MaxValue
        sizedRange (int lo) (int hi) |> map char

    // Generates a random digit.
    let digit : Gen<char> =
        charRange '0' '9'

    // Generates a random lowercase character.
    let lower : Gen<char> =
        charRange 'a' 'z'

    // Generates a random uppercase character.
    let upper : Gen<char> =
        charRange 'A' 'Z'

    // Generates a random alpha character.
    let alpha : Gen<char> =
        choice [lower; upper]

    // Generates a random alpha-numeric character.
    let alphaNum : Gen<char> =
        choice [lower; upper; digit]

    /// Generates a random string using the specified character generator.
    let string' (g : Gen<char>) : Gen<string> =
        array g |> map (fun xs -> new System.String(xs))

    /// Generates a random string.
    let string : Gen<string> =
        choice [alphaNum; char] |> string'

    //
    // Combinators - Primitives
    //

    /// Generates a random boolean.
    let bool : Gen<bool> =
        item [false; true]

    /// Generates a random byte.
    let byte : Gen<byte> =
        sizedBounded ()

    /// Generates a random signed byte.
    let sbyte : Gen<sbyte> =
        sizedBounded ()

    /// Generates a random signed 16-bit integer.
    let int16 : Gen<int16> =
        sizedBounded ()

    /// Generates a random unsigned 16-bit integer.
    let uint16 : Gen<uint16> =
        sizedBounded ()

    /// Generates a random signed 32-bit integer.
    let int : Gen<int> =
        sizedBounded ()

    /// Generates a random unsigned 32-bit integer.
    let uint32 : Gen<uint32> =
        sizedBounded ()

    /// Generates a random signed 64-bit integer.
    let int64 : Gen<int64> =
        sizedBounded ()

    /// Generates a random unsigned 64-bit integer.
    let uint64 : Gen<uint64> =
        sizedBounded ()

    /// Generates a random 64-bit floating point number.
    let double : Gen<double> =
        create Shrink.double Random.sizedDouble

    /// Generates a random 64-bit floating point number.
    let float : Gen<float> =
        double |> map float

    //
    // Combinators - Constructed
    //

    /// Generates a random globally unique identifier.
    let guid : Gen<System.Guid> =
        gen { let! bs = byte |> array' 16 16
              return System.Guid bs }

    /// Generates a random instant in time expressed as a date and time of day.
    let dateTime : Gen<System.DateTime> =
        let yMin = System.DateTime.MinValue.Year
        let yMax = System.DateTime.MaxValue.Year
        gen { let! y = create (Shrink.towards 2000) (Random.range yMin yMax)
              let! m = range 1 12
              let! d = range 1 (System.DateTime.DaysInMonth (y, m))
              let! h = range 0 23
              let! min = range 0 59
              let! sec = range 0 59
              return System.DateTime (y, m, d, h, min, sec) }

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
        let forest = sampleTree 10 5 g
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

[<AutoOpen>]
module GenOperators =
    let (<!>) = Gen.map
    let (<*>) = Gen.apply
