namespace Hedgehog

open System
open Hedgehog.Numeric

/// A generator for values and shrink trees of type 'a.
[<Struct>]
type Gen<'a> =
    | Gen of Random<Tree<'a>>

module Gen =
    [<CompiledName("OfRandom")>]
    let ofRandom (r : Random<Tree<'a>>) : Gen<'a> =
        Gen r

    [<CompiledName("ToRandom")>]
    let toRandom (Gen r : Gen<'a>) : Random<Tree<'a>> =
        r

    [<CompiledName("Delay")>]
    let delay (f : unit -> Gen<'a>) : Gen<'a> =
        Random.delay (toRandom << f) |> ofRandom

    [<CompiledName("TryFinally")>]
    let tryFinally (m : Gen<'a>) (after : unit -> unit) : Gen<'a> =
        Random.tryFinally (toRandom m) after |> ofRandom

    [<CompiledName("TryWith")>]
    let tryWith (m : Gen<'a>) (k : exn -> Gen<'a>) : Gen<'a> =
        Random.tryWith (toRandom m) (toRandom << k) |> ofRandom

    [<CompiledName("Create")>]
    let create (shrink : 'a -> seq<'a>) (random : Random<'a>) : Gen<'a> =
        Random.map (Tree.unfold id shrink) random |> ofRandom

    [<CompiledName("Constant")>]
    let constant (x : 'a) : Gen<'a> =
        Tree.singleton x |> Random.constant |> ofRandom

    let private bindRandom (m : Random<Tree<'a>>) (k : 'a -> Random<Tree<'b>>) : Random<Tree<'b>> =
        Hedgehog.Random <| fun seed0 size ->
          let seed1, seed2 =
              Seed.split seed0

          let run (seed : Seed) (random : Random<'x>) : 'x =
              Random.run seed size random

          Tree.bind (run seed1 m) (run seed2 << k)

    [<CompiledName("Bind")>]
    let bind (m0 : Gen<'a>) (k0 : 'a -> Gen<'b>) : Gen<'b> =
        bindRandom (toRandom m0) (toRandom << k0) |> ofRandom

    [<CompiledName("Apply")>]
    let apply (gf : Gen<'a -> 'b>) (gx : Gen<'a>) : Gen<'b> =
        bind gf <| fun f ->
        bind gx <| fun x ->
        constant (f x)

    [<CompiledName("MapRandom")>]
    let mapRandom (f : Random<Tree<'a>> -> Random<Tree<'b>>) (g : Gen<'a>) : Gen<'b> =
        toRandom g |> f |> ofRandom

    [<CompiledName("MapTree")>]
    let mapTree (f : Tree<'a> -> Tree<'b>) (g : Gen<'a>) : Gen<'b> =
        mapRandom (Random.map f) g

    [<CompiledName("Map")>]
    let map (f : 'a -> 'b) (g : Gen<'a>) : Gen<'b> =
        mapTree (Tree.map f) g

    [<CompiledName("Map")>]
    let map2 (f : 'a -> 'b -> 'c) (gx : Gen<'a>) (gy : Gen<'b>) : Gen<'c> =
        bind gx <| fun x ->
        bind gy <| fun y ->
        constant (f x y)

    [<CompiledName("Map")>]
    let map3 (f : 'a -> 'b -> 'c -> 'd) (gx : Gen<'a>) (gy : Gen<'b>) (gz : Gen<'c>) : Gen<'d> =
        bind gx <| fun x ->
        bind gy <| fun y ->
        bind gz <| fun z ->
        constant (f x y z)

    [<CompiledName("Map")>]
    let map4 (f : 'a -> 'b -> 'c -> 'd -> 'e) (gx : Gen<'a>) (gy : Gen<'b>) (gz : Gen<'c>) (gw : Gen<'d>) : Gen<'e> =
        bind gx <| fun x ->
        bind gy <| fun y ->
        bind gz <| fun z ->
        bind gw <| fun w ->
        constant (f x y z w)

    [<CompiledName("Zip")>]
    let zip (gx : Gen<'a>) (gy : Gen<'b>) : Gen<'a * 'b> =
        map2 (fun x y -> x, y) gx gy

    [<CompiledName("Zip")>]
    let zip3 (gx : Gen<'a>) (gy : Gen<'b>) (gz : Gen<'c>) : Gen<'a * 'b * 'c> =
        map3 (fun x y z -> x, y, z) gx gy gz

    [<CompiledName("Zip")>]
    let zip4 (gx : Gen<'a>) (gy : Gen<'b>) (gz : Gen<'c>) (gw : Gen<'d>) : Gen<'a * 'b * 'c * 'd> =
        map4 (fun x y z w -> x, y, z, w) gx gy gz gw

    [<CompiledName("Tuple2")>]
    let tuple  (g : Gen<'a>) : Gen<'a * 'a> =
        zip g g

    [<CompiledName("Tuple3")>]
    let tuple3 (g : Gen<'a>) : Gen<'a * 'a * 'a> =
        zip3 g g g

    [<CompiledName("Tuple4")>]
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
    [<CompiledName("NoShrink")>]
    let noShrink (g : Gen<'a>) : Gen<'a> =
        let drop (Node (x, _)) =
            Node (x, Seq.empty)
        mapTree drop g

    /// Apply an additional shrinker to all generated trees.
    [<CompiledName("ShrinkLazy")>]
    let shrinkLazy (f : 'a -> seq<'a>) (g : Gen<'a>) : Gen<'a> =
        mapTree (Tree.expand f) g

    /// Apply an additional shrinker to all generated trees.
    [<CompiledName("Shrink")>]
    let shrink (f : 'a -> List<'a>) (g : Gen<'a>) : Gen<'a>  =
        shrinkLazy (Seq.ofList << f) g

    //
    // Combinators - Size
    //

    /// Used to construct generators that depend on the size parameter.
    [<CompiledName("Sized")>]
    let sized (f : Size -> Gen<'a>) : Gen<'a> =
        Random.sized (toRandom << f) |> ofRandom

    /// Overrides the size parameter. Returns a generator which uses the
    /// given size instead of the runtime-size parameter.
    [<CompiledName("Resize")>]
    let resize (n : int) (g : Gen<'a>) : Gen<'a> =
        mapRandom (Random.resize n) g

    /// Adjust the size parameter, by transforming it with the given
    /// function.
    [<CompiledName("Scale")>]
    let scale (f : int -> int) (g : Gen<'a>) : Gen<'a> =
        sized <| fun n ->
            resize (f n) g

    //
    // Combinators - Numeric
    //

    /// Generates a random number in the given inclusive range.
    [<CompiledName("Integral")>]
    let inline integral (range : Range<'a>) : Gen<'a> =
        create (Shrink.towards <| Range.origin range) (Random.integral range)

    //
    // Combinators - Choice
    //

    let private crashEmpty (arg : string) : 'b =
        invalidArg arg (sprintf "'%s' must have at least one element" arg)

    /// Randomly selects one of the values in the list.
    /// <i>The input list must be non-empty.</i>
    [<CompiledName("Item")>]
    let item (xs0 : seq<'a>) : Gen<'a> = gen {
        let xs = Array.ofSeq xs0
        if Array.isEmpty xs then
            return crashEmpty "xs"
        else
            let! ix = integral <| Range.constant 0 (Array.length xs - 1)
            return Array.item ix xs
    }

    /// Uses a weighted distribution to randomly select one of the gens in the list.
    /// <i>The input list must be non-empty.</i>
    [<CompiledName("Frequency")>]
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

        let! n = integral <| Range.constant 1 total
        return! pick n xs
    }

    /// Randomly selects one of the gens in the list.
    /// <i>The input list must be non-empty.</i>
    [<CompiledName("Choice")>]
    let choice (xs0 : seq<Gen<'a>>) : Gen<'a> = gen {
        let xs = Array.ofSeq xs0
        if Array.isEmpty xs then
            return crashEmpty "xs" xs
        else
            let! ix = integral <| Range.constant 0 (Array.length xs - 1)
            return! Array.item ix xs
    }

    /// Randomly selects from one of the gens in either the non-recursive or the
    /// recursive list. When a selection is made from the recursive list, the size
    /// is halved. When the size gets to one or less, selections are no longer made
    /// from the recursive list.
    /// <i>The first argument (i.e. the non-recursive input list) must be non-empty.</i>
    [<CompiledName("ChoiceRecursive")>]
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
    [<CompiledName("Filter")>]
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
    [<CompiledName("TryFilter")>]
    let tryFilter (p : 'a -> bool) (g : Gen<'a>) : Gen<'a option> =
        ofRandom << Random.bind (toRandom g |> tryFilterRandom p) <| function
            | None ->
                None |> Tree.singleton |> Random.constant
            | Some x ->
                Tree.map Some x |> Random.constant

    /// Runs an option generator until it produces a 'Some'.
    [<CompiledName("Some")>]
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
    [<CompiledName("Option")>]
    let option (g : Gen<'a>) : Gen<'a option> =
        sized <| fun n ->
            frequency [
                2, constant None
                1 + n, map Some g
            ]

    let private atLeast (n : int) (xs : List<'a>) : bool =
        n = 0 || not (List.isEmpty (List.skip (n - 1) xs))

    /// Generates a list using a 'Range' to determine the length.
    [<CompiledName("List")>]
    let list (range : Range<int>) (g : Gen<'a>) : Gen<List<'a>> =
        ofRandom
        <| (Random.sized
        <| fun size -> random {
               let! k = Random.integral range
               let! xs = Random.replicate k (toRandom g)
               return Shrink.sequenceList xs
                   |> Tree.filter (atLeast (Range.lowerBound size range))
           })

    /// Generates an array using a 'Range' to determine the length.
    [<CompiledName("Array")>]
    let array (range : Range<int>) (g : Gen<'a>) : Gen<array<'a>> =
        list range g |> map Array.ofList

    /// Generates a sequence using a 'Range' to determine the length.
    [<CompiledName("Enumerable")>]
    let seq (range : Range<int>) (g : Gen<'a>) : Gen<seq<'a>> =
        list range g |> map Seq.ofList

    //
    // Combinators - Characters
    //

    [<CompiledName("Char")>]
    // Generates a random character in the specified range.
    let char (lo : char) (hi : char) : Gen<char> =
        integral <| Range.constant (int lo) (int hi) |> map char

    /// Generates a Unicode character, including invalid standalone surrogates:
    /// '\000'..'\65535'
    [<CompiledName("UnicodeAll")>]
    let unicodeAll : Gen<char> =
        let lo = System.Char.MinValue
        let hi = System.Char.MaxValue
        char lo hi

    // Generates a random digit.
    [<CompiledName("Digit")>]
    let digit : Gen<char> =
        char '0' '9'

    // Generates a random lowercase character.
    [<CompiledName("Lower")>]
    let lower : Gen<char> =
        char 'a' 'z'

    // Generates a random uppercase character.
    [<CompiledName("Upper")>]
    let upper : Gen<char> =
        char 'A' 'Z'

    /// Generates an ASCII character: '\000'..'\127'
    [<CompiledName("Ascii")>]
    let ascii : Gen<char> =
        char '\000' '\127'

    /// Generates a Latin-1 character: '\000'..'\255'
    [<CompiledName("Latin1")>]
    let latin1 : Gen<char> =
        char '\000' '\255'

    /// Generates a Unicode character, excluding noncharacters
    /// ('\65534', '\65535') and invalid standalone surrogates
    /// ('\000'..'\65535' excluding '\55296'..'\57343').
    [<CompiledName("Unicode")>]
    let unicode : Gen<char> =
        let isNoncharacter x = 
               x = Operators.char 65534
            || x = Operators.char 65535
        unicodeAll
        |> filter (not << isNoncharacter)
        |> filter (not << System.Char.IsSurrogate)

    // Generates a random alpha character.
    [<CompiledName("Alpha")>]
    let alpha : Gen<char> =
        choice [lower; upper]

    [<CompiledName("AlphaNum")>]
    // Generates a random alpha-numeric character.
    let alphaNum : Gen<char> =
        choice [lower; upper; digit]

    /// Generates a random string using 'Range' to determine the length and the
    /// specified character generator.
    [<CompiledName("String")>]
    let string (range : Range<int>) (g : Gen<char>) : Gen<string> =
        sized <| fun size ->
            g |> array range
        |> map System.String

    //
    // Combinators - Primitives
    //

    /// Generates a random boolean.
    [<CompiledName("Bool")>]
    let bool : Gen<bool> =
        item [false; true]

    /// Generates a random byte.
    [<CompiledName("Byte")>]
    let byte (range : Range<byte>) : Gen<byte> =
        integral range

    /// Generates a random signed byte.
    [<CompiledName("SByte")>]
    let sbyte (range : Range<sbyte>) : Gen<sbyte> =
        integral range

    /// Generates a random signed 16-bit integer.
    [<CompiledName("Int16")>]
    let int16 (range : Range<int16>) : Gen<int16> =
        integral range

    /// Generates a random unsigned 16-bit integer.
    [<CompiledName("UInt16")>]
    let uint16 (range : Range<uint16>) : Gen<uint16> =
        integral range

    /// Generates a random signed 32-bit integer.
    [<CompiledName("Int32")>]
    let int (range : Range<int>) : Gen<int> =
        integral range

    /// Generates a random unsigned 32-bit integer.
    [<CompiledName("UInt32")>]
    let uint32 (range : Range<uint32>) : Gen<uint32> =
        integral range

    /// Generates a random signed 64-bit integer.
    [<CompiledName("Int64")>]
    let int64 (range : Range<int64>) : Gen<int64> =
        integral range

    /// Generates a random unsigned 64-bit integer.
    [<CompiledName("UInt64")>]
    let uint64 (range : Range<uint64>) : Gen<uint64> =
        integral range

    /// Generates a random 64-bit floating point number.
    [<CompiledName("Double")>]
    let double (range : Range<double>) : Gen<double> =
        create (Shrink.towardsDouble <| Range.origin range) (Random.double range)

    /// Generates a random 64-bit floating point number.
    [<CompiledName("Float")>]
    let float (range : Range<float>) : Gen<float> =
        (double range) |> map float

    //
    // Combinators - Constructed
    //

    /// Generates a random globally unique identifier.
    [<CompiledName("Guid")>]
    let guid : Gen<System.Guid> = gen {
        let! bs = array (Range.constant 16 16) (byte <| Range.constantBounded ())
        return System.Guid bs
    }

    /// Generates a random instant in time expressed as a date and time of day.
    [<CompiledName("DateTime")>]
    let dateTime : Gen<System.DateTime> =
        let minTicks =
            System.DateTime.MinValue.Ticks
        let maxTicks =
            System.DateTime.MaxValue.Ticks
        gen {
            let! ticks =
                Range.constantFrom
                    (System.DateTime (2000, 1, 1)).Ticks minTicks maxTicks
                |> integral
            return System.DateTime ticks
        }

    //
    // Sampling
    //

    [<CompiledName("SampleTree")>]
    let sampleTree (size : Size) (count : int) (g : Gen<'a>) : List<Tree<'a>> =
        let seed = Seed.random ()
        toRandom g
        |> Random.replicate count
        |> Random.run seed size

    [<CompiledName("Sample")>]
    let sample (size : Size) (count : int) (g : Gen<'a>) : List<'a> =
        sampleTree size count g
        |> List.map Tree.outcome

    /// Run a generator. The size passed to the generator is always 30;
    /// if you want another size then you should explicitly use 'resize'.
    [<CompiledName("GenerateTree")>]
    let generateTree (g : Gen<'a>) : Tree<'a> =
        let seed = Seed.random ()
        toRandom g
        |> Random.run seed 30

    [<CompiledName("PrintSample")>]
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
