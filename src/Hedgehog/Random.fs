namespace Hedgehog

open Hedgehog.Numeric

/// A generator for random values of type 'a
[<Struct>]
type Random<'a> =
    | Random of (Seed -> Size -> 'a)

module Random =
    let private unsafeRun (seed : Seed) (size : Size) (Random r : Random<'a>) : 'a =
        r seed size

    let run (seed : Seed) (size : Size) (r : Random<'a>) : 'a =
        unsafeRun seed (max 1 size) r

    let delay (f : unit -> Random<'a>) : Random<'a> =
        Random <| fun seed size ->
            f () |> unsafeRun seed size

    let tryFinally (r : Random<'a>) (after : unit -> unit) : Random<'a> =
        Random <| fun seed size ->
            try
                unsafeRun seed size r
            finally
                after ()

    let tryWith (r : Random<'a>) (k : exn -> Random<'a>) : Random<'a> =
        Random <| fun seed size ->
            try
                unsafeRun seed size r
            with
                x -> unsafeRun seed size (k x)

    let constant (x : 'a) : Random<'a> =
        Random <| fun _ _ ->
            x

    let map (f : 'a -> 'b) (r : Random<'a>) : Random<'b> =
        Random <| fun seed size ->
            r
            |> unsafeRun seed size
            |> f

    let bind (r : Random<'a>) (k : 'a -> Random<'b>) : Random<'b> =
        Random <| fun seed size ->
            let seed1, seed2 = Seed.split seed
            r
            |> unsafeRun seed1 size
            |> k
            |> unsafeRun seed2 size

    let replicate (times : int) (r : Random<'a>) : Random<List<'a>> =
        Random <| fun seed0 size ->
            let rec loop seed k acc =
                if k <= 0 then
                    acc
                else
                    let seed1, seed2 = Seed.split seed
                    let x = unsafeRun seed1 size r
                    loop seed2 (k - 1) (x :: acc)
            loop seed0 times []

    type Builder internal () =
        member __.Return(x : 'a) : Random<'a> =
            constant x
        member __.ReturnFrom(m : Random<'a>) : Random<'a> =
            m
        member __.Bind(m : Random<'a>, k : 'a -> Random<'b>) : Random<'b> =
            bind m k

    /// Used to construct generators that depend on the size parameter.
    let sized (f : Size -> Random<'a>) : Random<'a> =
        Random <| fun seed size ->
            unsafeRun seed size (f size)

    /// Overrides the size parameter. Returns a generator which uses the
    /// given size instead of the runtime-size parameter.
    let resize (newSize : Size) (r : Random<'a>) : Random<'a> =
        Random <| fun seed _ ->
          run seed newSize r

    /// Generates a random integral number in the given inclusive range.
    let inline integral (range : Range<'a>) : Random<'a> =
        Random <| fun seed size ->
            let (lo, hi) = Range.bounds size range
            let x, _ = Seed.nextBigInt (toBigInt lo) (toBigInt hi) seed
            fromBigInt x

    /// Generates a random floating point number in the given inclusive range.
    let inline double (range : Range<double>) : Random<double> =
        Random <| fun seed size ->
            let (lo, hi) = Range.bounds size range
            let x, _ = Seed.nextDouble lo hi seed
            x

[<AutoOpen>]
module RandomBuilder =
    let random = Random.Builder ()
