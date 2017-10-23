﻿namespace Hedgehog

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
    let inline integral (range : Range<'a>) : Gen<'a> =
        create (Shrink.towards <| Range.origin range) (Random.integral range)

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
            let! ix = integral <| Range.constant 0 (Array.length xs - 1)
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

        let! n = integral <| Range.constant 1 total
        return! pick n xs
    }

    /// Randomly selects one of the gens in the list.
    /// <i>The input list must be non-empty.</i>
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

    /// Generates a list using a 'Range' to determine the length.
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
    let array (range : Range<int>) (g : Gen<'a>) : Gen<array<'a>> =
        list range g |> map Array.ofList

    /// Generates a sequence using a 'Range' to determine the length.
    let seq (range : Range<int>) (g : Gen<'a>) : Gen<seq<'a>> =
        list range g |> map Seq.ofList

    //
    // Combinators - Characters
    //

    // Generates a random character in the specified range.
    let char (lo : char) (hi : char) : Gen<char> =
        integral <| Range.constant (int lo) (int hi) |> map char

    /// Generates a Unicode character, including invalid standalone surrogates:
    /// '\000'..'\65535'
    let unicodeAll : Gen<char> =
        let lo = System.Char.MinValue
        let hi = System.Char.MaxValue
        char lo hi

    // Generates a random digit.
    let digit : Gen<char> =
        char '0' '9'

    // Generates a random lowercase character.
    let lower : Gen<char> =
        char 'a' 'z'

    // Generates a random uppercase character.
    let upper : Gen<char> =
        char 'A' 'Z'

    /// Generates an ASCII character: '\000'..'\127'
    let ascii : Gen<char> =
        char '\000' '\127'

    /// Generates a Latin-1 character: '\000'..'\255'
    let latin1 : Gen<char> =
        char '\000' '\255'

    /// Generates a Unicode character, excluding invalid standalone surrogates:
    /// '\000'..'\65535' (excluding '\55296'..'\57343')
    let unicode : Gen<char> =
        filter (not << System.Char.IsSurrogate) unicodeAll

    // Generates a random alpha character.
    let alpha : Gen<char> =
        choice [lower; upper]

    // Generates a random alpha-numeric character.
    let alphaNum : Gen<char> =
        choice [lower; upper; digit]

    /// Generates a random string using 'Range' to determine the length and the
    /// specified character generator.
    let string (range : Range<int>) (g : Gen<char>) : Gen<string> =
        sized <| fun size ->
            g |> array range
        |> map System.String

    //
    // Combinators - Primitives
    //

    /// Generates a random boolean.
    let bool : Gen<bool> =
        item [false; true]

    /// Generates a random byte.
    let byte (range : Range<byte>) : Gen<byte> =
        integral range

    /// Generates a random signed byte.
    let sbyte (range : Range<sbyte>) : Gen<sbyte> =
        integral range

    /// Generates a random signed 16-bit integer.
    let int16 (range : Range<int16>) : Gen<int16> =
        integral range

    /// Generates a random unsigned 16-bit integer.
    let uint16 (range : Range<uint16>) : Gen<uint16> =
        integral range

    /// Generates a random signed 32-bit integer.
    let int (range : Range<int>) : Gen<int> =
        integral range

    /// Generates a random unsigned 32-bit integer.
    let uint32 (range : Range<uint32>) : Gen<uint32> =
        integral range

    /// Generates a random signed 64-bit integer.
    let int64 (range : Range<int64>) : Gen<int64> =
        integral range

    /// Generates a random unsigned 64-bit integer.
    let uint64 (range : Range<uint64>) : Gen<uint64> =
        integral range

    /// Generates a random 64-bit floating point number.
    let double (range : Range<double>) : Gen<double> =
        create (Shrink.towardsDouble <| Range.origin range) (Random.double range)

    /// Generates a random 64-bit floating point number.
    let float (range : Range<float>) : Gen<float> =
        (double range) |> map float

    //
    // Combinators - Constructed
    //

    /// Generates a random globally unique identifier.
    let guid : Gen<System.Guid> = gen {
        let! bs = array (Range.constant 16 16) (byte <| Range.constantBounded ())
        return System.Guid bs
    }

    /// Generates a random instant in time expressed as a date and time of day.
    let dateTime : Gen<System.DateTime> =
        let yMin = System.DateTime.MinValue.Year
        let yMax = System.DateTime.MaxValue.Year
        gen {
            let! y =
                integral <| Range.linearFrom 2000 yMin yMax
            let! m =
                integral <| Range.constant 1 12
            let! d =
                integral <| Range.constant 1 (System.DateTime.DaysInMonth (y, m))
            let! h =
                integral <| Range.constant 0 23
            let! min =
                integral <| Range.constant 0 59
            let! sec =
                integral <| Range.constant 0 59

            return System.DateTime (y, m, d, h, min, sec)
        }

    //
    // Combinators - Convenience
    //

    /// Fisher-Yates shuffle / Knuth shuffle from
    /// https://www.rosettacode.org/wiki/Knuth_shuffle#F.23
    let private shuffle lst =
        let arr = lst |> List.toArray
        let swap i j =
            let item = arr.[i]
            arr.[i] <- arr.[j]
            arr.[j] <- item
        let rnd = new System.Random()
        let ln = arr.Length
        [0..(ln - 2)] |> Seq.iter (fun i -> swap i (rnd.Next(i, ln)))
        arr |> Array.toList

    /// Generates a permutation the specified list (shuffles its elements).
    let permutationOf (list : 'a list) : Gen<'a list> =
        gen { return list |> shuffle }

    let private randomizeCase (s:string) =
        let r = System.Random()

        let randomizeCharCase c =
            let f = if r.Next() % 2 = 0
                    then System.Char.ToUpper
                    else System.Char.ToLower
            f c

        s |> Seq.map randomizeCharCase |> System.String.Concat

    /// Randomizes the case of the characters in the string.
    let casePermutationOf (str : string) : Gen<string> =
        gen { return str |> randomizeCase }

    /// Generates a character that is not whitespace.
    let notWhiteSpace : (Gen<char> -> Gen<char>) =
        filter (not << System.Char.IsWhiteSpace)

    /// Generates a character that is not a control character.
    let notControl : (Gen<char> -> Gen<char>) =
        filter (not << System.Char.IsControl)

    /// Generates a character that is not punctuation.
    let notPunctuation : (Gen<char> -> Gen<char>) =
        filter (not << System.Char.IsPunctuation)

    /// Shortcut for Gen.list (Range.exponential lower upper).
    let eList (lower : int) (upper : int) : (Gen<'a> -> Gen<List<'a>>) =
        list (Range.exponential lower upper)

    /// Shortcut for Gen.list (Range.linear lower upper).
    let lList (lower : int) (upper : int) : (Gen<'a> -> Gen<List<'a>>) =
        list (Range.linear lower upper)

    /// Shortcut for Gen.list (Range.constant lower upper).
    let cList (lower : int) (upper : int) : (Gen<'a> -> Gen<List<'a>>) =
        list (Range.constant lower upper)

    /// Shortcut for Gen.array (Range.exponential lower upper).
    let eArray (lower : int) (upper : int) : (Gen<'a> -> Gen<array<'a>>) =
        array (Range.exponential lower upper)

    /// Shortcut for Gen.array (Range.linear lower upper).
    let lArray (lower : int) (upper : int) : (Gen<'a> -> Gen<array<'a>>) =
        array (Range.linear lower upper)

    /// Shortcut for Gen.array (Range.constant lower upper).
    let cArray (lower : int) (upper : int) : (Gen<'a> -> Gen<array<'a>>) =
        array (Range.constant lower upper)

    /// Shortcut for Gen.seq (Range.exponential lower upper).
    let eSeq (lower : int) (upper : int) : (Gen<'a> -> Gen<seq<'a>>) =
        seq (Range.exponential lower upper)

    /// Shortcut for Gen.seq (Range.linear lower upper).
    let lSeq (lower : int) (upper : int) : (Gen<'a> -> Gen<seq<'a>>) =
        seq (Range.linear lower upper)

    /// Shortcut for Gen.seq (Range.constant lower upper).
    let cSeq (lower : int) (upper : int) : (Gen<'a> -> Gen<seq<'a>>) =
        seq (Range.constant lower upper)

    /// Shortcut for Gen.string (Range.exponential lower upper).
    let eString (lower : int) (upper : int) : (Gen<char> -> Gen<string>) =
        string (Range.exponential lower upper)

    /// Shortcut for Gen.string (Range.linear lower upper).
    let lString (lower : int) (upper : int) : (Gen<char> -> Gen<string>) =
        string (Range.linear lower upper)

    /// Shortcut for Gen.string (Range.constant lower upper).
    let cString (lower : int) (upper : int) : (Gen<char> -> Gen<string>) =
        string (Range.constant lower upper)

    /// Generates null part of the time.
    let withNull (g : Gen<'a>) : Gen<'a> =
        g |> option |> map (fun xOpt ->
            match xOpt with Some x -> x | None -> null)

    /// Generates a value that is not null.
    let noNull (g : Gen<'a>) : Gen<'a> =
        g |> filter (not << isNull)

    /// Generates a value that is not equal to another value.
    let notEqualTo (other : 'a) : (Gen<'a> -> Gen<'a>) =
        filter ((<>) other)

    /// Generates a value that is not equal to another option-wrapped value.
    let notEqualToOpt (other : 'a option) : (Gen<'a> -> Gen<'a>) =
        filter (fun x -> match other with Some o -> x <> o | None -> true)

    /// Generates a value that is not contained in the specified list.
    let notIn (list: 'a list) (g : Gen<'a>) : Gen<'a> =
        g |> filter (fun x -> not <| List.contains x list)

    /// Generates a list that does not contain the specified element.
    /// Shortcut for Gen.filter (not << List.contains x)
    let notContains (x: 'a) : (Gen<'a list> -> Gen<'a list>) =
      filter (not << List.contains x)

    /// Generates a string that is not equal to another string using
    /// StringComparison.OrdinalIgnoreCase.
    let iNotEqualTo (str : string) : (Gen<string> -> Gen<string>) =
        filter <| fun s ->
            not <| str.Equals(s, System.StringComparison.OrdinalIgnoreCase)

    /// Generates a string that is not a substring of another string.
    let notSubstringOf (str : string) : (Gen<string> -> Gen<string>) =
      filter <| fun s -> not <| str.Contains s

    /// Generates a string that is not a substring of another string using
    /// StringComparison.OrdinalIgnoreCase.
    let iNotSubstringOf (str : string) : (Gen<string> -> Gen<string>) =
      filter <| fun s ->
          str.IndexOf(s, System.StringComparison.OrdinalIgnoreCase) = -1

    /// Generates a string that does not start with another string.
    let notStartsWith (str : string) : (Gen<string> -> Gen<string>) =
      filter <| fun s -> not <| s.StartsWith str

    /// Generates a string that does not start with another string using
    /// StringComparison.OrdinalIgnoreCase.
    let iNotStartsWith (str : string) : (Gen<string> -> Gen<string>) =
      filter <| fun s ->
          not <| s.StartsWith(str, System.StringComparison.OrdinalIgnoreCase)

    /// Generates a 2-tuple with sorted elements.
    let sorted2 (g : Gen<'a * 'a>) : Gen<'a * 'a> =
        g |> map (fun (x1, x2) ->
            let l = [x1; x2] |> List.sort
            (l.Item 0, l.Item 1))

    /// Generates a 3-tuple with sorted elements.
    let sorted3 (g : Gen<'a * 'a * 'a>) : Gen<'a * 'a * 'a> =
        g |> map (fun (x1, x2, x3) ->
            let l = [x1; x2; x3] |> List.sort
            (l.Item 0, l.Item 1, l.Item 2))

    /// Generates a 4-tuple with sorted elements.
    let sorted4 (g : Gen<'a * 'a * 'a * 'a>) : Gen<'a * 'a * 'a * 'a> =
        g |> map (fun (x1, x2, x3, x4) ->
            let l = [x1; x2; x3; x4] |> List.sort
            (l.Item 0, l.Item 1, l.Item 2, l.Item 3))

    /// Generates a 2-tuple with distinct elements.
    let distinct2 (g : Gen<'a * 'a>) : Gen<'a * 'a> =
        g |> filter (fun (x1, x2) -> x1 <> x2)

    /// Generates a 3-tuple with distinct elements.
    let distinct3 (g : Gen<'a * 'a * 'a>) : Gen<'a * 'a * 'a> =
        g |> filter (fun (x1, x2, x3) ->
            [x1; x2; x3] |> List.distinct = [x1; x2; x3])

    /// Generates a 4-tuple with distinct elements.
    let distinct4 (g : Gen<'a * 'a * 'a * 'a>) : Gen<'a * 'a * 'a * 'a> =
        g |> filter (fun (x1, x2, x3, x4) ->
            [x1; x2; x3; x4] |> List.distinct = [x1; x2; x3; x4])

    /// Generates a 2-tuple with strictly increasing elements.
    let increasing2 (g : Gen<'a * 'a>) : Gen<'a * 'a> =
        g |> sorted2 |> distinct2

    /// Generates a 3-tuple with strictly increasing elements.
    let increasing3 (g : Gen<'a * 'a * 'a>) : Gen<'a * 'a * 'a> =
        g |> sorted3 |> distinct3

    /// Generates a 4-tuple with strictly increasing elements.
    let increasing4 (g : Gen<'a * 'a * 'a * 'a>) : Gen<'a * 'a * 'a * 'a> =
        g |> sorted4 |> distinct4

    /// Generates a tuple of datetimes where the range determines the minimum
    /// and maximum number of days apart. Positive numbers means the datetimes
    /// will be in increasing order, and vice versa.
    let dateInterval (dayRange : Range<int>)
            : Gen<System.DateTime * System.DateTime> =
        gen {
            let tickRange =
                dayRange
                |> Range.map (fun days ->
                    Operators.int64 days * System.TimeSpan.TicksPerDay)
            let! ticksApart = integral tickRange
            let! dt1 = dateTime |> filter (fun dt ->
                dt.Ticks + ticksApart > System.DateTime.MinValue.Ticks
                && dt.Ticks + ticksApart < System.DateTime.MaxValue.Ticks)
            let dt2 = dt1.AddTicks ticksApart
            return dt1, dt2
        }

    /// Generates a list using inpGen together with a function that maps each
    /// of the distinct elements in the list to values generated by outGen.
    /// Distinct elements in the input list may map to the same output values.
    /// For example, [2; 3; 2] may map to ['A'; 'B'; 'A'] or ['A'; 'A'; 'A'],
    /// but never ['A'; 'B'; 'C']. The generated function throws if called with
    /// values not present in the input list.
    let withMapTo (outGen : Gen<'b>) (inpGen : Gen<'a list>)
            : Gen<'a list * ('a -> 'b)> =
        gen {
            let! inputs = inpGen
            let inputsDistinct = inputs |> List.distinct
            let! outputs = outGen |> list (Range.singleton inputsDistinct.Length)
            let inOutMap = List.zip inputsDistinct outputs |> Map.ofList
            return inputs, (fun x -> inOutMap.Item x)
        }

    /// Generates a list using inpGen together with a function that maps each
    /// of the distinct elements in the list to values generated by outGen.
    /// Distinct elements in the input list are guaranteed to map to distinct
    /// output values. For example, [2; 3; 2] may map to ['A'; 'B'; 'A'], but
    /// never ['A'; 'A'; 'A'] or ['A'; 'B'; 'C']. Only use this if the output
    /// space is large enough that the required number of distinct output values
    /// are likely to be generated. The generated function throws if called with
    /// values not present in the input list.
    let withDistinctMapTo (outGen : Gen<'b>) (inpGen : Gen<'a list>)
            : Gen<'a list * ('a -> 'b)> =
        gen {
          let rec distinctOutGen (xs : 'b list) (length : int) : Gen<'b list> =
              gen {
                  if xs.Length = length then return xs
                  else
                      let! x = outGen |> notIn xs
                      return! distinctOutGen (x::xs) length
              }

          let! inputs = inpGen
          let inputsDistinct = inputs |> List.distinct
          let! outputs = distinctOutGen [] inputsDistinct.Length
          let inOutMap = List.zip inputsDistinct outputs |> Map.ofList
          return inputs, (fun x -> inOutMap.Item x)
        }

    /// Inserts the given element at a random place in the list
    let addElement (x : 'a) (g : Gen<'a list>) : Gen<'a list> =
        gen {
          let! xs = g
          let! i = integral (Range.constant 0 xs.Length)
          let l1, l2 = xs |> List.splitAt i
          return List.concat [l1; [x]; l2]
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
