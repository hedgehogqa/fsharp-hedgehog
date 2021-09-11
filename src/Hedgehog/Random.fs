namespace Hedgehog

open Hedgehog.Numeric

/// A generator for random values of type 'a
[<Struct>]
type Random<'a> =
    | Random of (Seed -> Size -> 'a)

module Random =
    let private unsafeRun (seed: Seed) (size: Size) (Random r: Random<'a>) : 'a =
        r seed size

    let run (seed: Seed) (size: Size) (r: Random<'a>) : 'a =
        unsafeRun seed (max 1 size) r

    let delay (f: unit -> Random<'a>) : Random<'a> =
        Random (fun seed size ->
            f () |> unsafeRun seed size)

    let tryFinally (after: unit -> unit) (r: Random<'a>) : Random<'a> =
        Random (fun seed size ->
            try
                r |> unsafeRun seed size
            finally
                after ())

    let tryWith (f: exn -> Random<'a>) (r: Random<'a>) : Random<'a> =
        Random (fun seed size ->
            try
                r |> unsafeRun seed size
            with e ->
                e |> f |> unsafeRun seed size)

    let constant (x : 'a) : Random<'a> =
        Random (fun _ _ -> x)

    let map (f: 'a -> 'b) (r: Random<'a>) : Random<'b> =
        Random (fun seed size ->
            r
            |> unsafeRun seed size
            |> f)

    let join (r: Random<Random<'a>>) : Random<'a> =
        Random (fun seed size ->
            let seed1, seed2 = Seed.split seed
            r
            |> unsafeRun seed1 size
            |> unsafeRun seed2 size)

    let bind (f: 'a -> Random<'b>) (r: Random<'a>) : Random<'b> =
        r |> map f |> join

    type Builder internal () =
        member __.Return(x : 'a) : Random<'a> =
            constant x
        member __.ReturnFrom(m : Random<'a>) : Random<'a> =
            m
        member __.Bind(m : Random<'a>, k : 'a -> Random<'b>) : Random<'b> =
            m |> bind k

    /// Used to construct generators that depend on the size parameter.
    let sized (f : Size -> Random<'a>) : Random<'a> =
        Random (fun seed size ->
            unsafeRun seed size (f size))

    /// Overrides the size parameter. Returns a generator which uses the
    /// given size instead of the runtime-size parameter.
    let resize (newSize : Size) (r : Random<'a>) : Random<'a> =
        Random (fun seed _ ->
          run seed newSize r)

    /// Generates a random integral number in the given inclusive range.
    let inline integral (range : Range<'a>) : Random<'a> =
        Random (fun seed size ->
            let (lo, hi) = Range.bounds size range
            let x, _ = Seed.nextBigInt (toBigInt lo) (toBigInt hi) seed
            fromBigInt x)

    /// Generates a random floating point number in the given inclusive range.
    let inline double (range : Range<double>) : Random<double> =
        Random (fun seed size ->
            let (lo, hi) = Range.bounds size range
            let x, _ = Seed.nextDouble lo hi seed
            x)

[<AutoOpen>]
module RandomBuilder =
    let random = Random.Builder ()
