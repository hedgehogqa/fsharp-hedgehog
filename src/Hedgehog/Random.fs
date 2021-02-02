namespace Hedgehog

open Hedgehog.Numeric

/// A generator for random values of type 'a
[<Struct>]
type Random<'a> =
    | Random of (Seed -> Size -> 'a)

module Random =

    let run (seed : Seed) (size : Size) (Random r : Random<'a>) : 'a =
        r seed size

    let delay (f : unit -> Random<'a>) : Random<'a> =
        Random (fun seed size ->
            f () |> run seed size)

    let tryFinally (after : unit -> unit) (r : Random<'a>) : Random<'a> =
        Random (fun seed size ->
            try
                run seed size r
            finally
                after ())

    let tryWith (k : exn -> Random<'a>) (r : Random<'a>) : Random<'a> =
        Random (fun seed size ->
            try
                run seed size r
            with
                x -> run seed size (k x))

    let constant (x : 'a) : Random<'a> =
        Random (fun _ _ -> x)

    let map (f : 'a -> 'b) (r : Random<'a>) : Random<'b> =
        Random (fun seed size ->
            r
            |> run seed size
            |> f)

    let bind (k : 'a -> Random<'b>) (r : Random<'a>) : Random<'b> =
        Random (fun seed size ->
            let seed1, seed2 = Seed.split seed
            r
            |> run seed1 size
            |> k
            |> run seed2 size)

    let replicate (times : int) (r : Random<'a>) : Random<List<'a>> =
        Random (fun seed0 size ->
            let rec loop seed k acc =
                if k <= 0 then
                    acc
                else
                    let seed1, seed2 = Seed.split seed
                    let x = run seed1 size r
                    loop seed2 (k - 1) (x :: acc)
            loop seed0 times [])

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
            run seed size (f size))

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
