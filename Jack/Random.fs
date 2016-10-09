namespace Jack

open FsControl.Operators

/// Tests are parameterized by the `Size` of the randomly-generated data,
/// the meaning of which depends on the particular generator used.
type Size = int

/// A generator for random values of type 'a
type Random<'a> =
    | Random of (Seed -> Size -> 'a)

module Random =
    let private unsafeRun  (seed : Seed) (size : Size) (Random r : Random<'a>) : 'a =
        r seed size

    let run (seed : Seed) (size : Size) (r : Random<'a>) : 'a =
        unsafeRun seed (max 1 size) r

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

    /// Used to construct generators that depend on the size parameter.
    let sized (f : Size -> Random<'a>) : Random<'a> =
        Random <| fun seed size ->
            unsafeRun seed size (f size)

    /// Overrides the size parameter. Returns a generator which uses the
    /// given size instead of the runtime-size parameter.
    let resize (newSize : Size) (r : Random<'a>) : Random<'a> =
        Random <| fun seed _ ->
          run seed newSize r

    /// Generates a random element in the given inclusive range.
    let inline choose (lo : ^a) (hi : ^a) : Random<'a> =
        Random <| fun seed _ ->
            let x, _ = Seed.nextBigInt (toBigInt lo) (toBigInt hi) seed
            fromBigInt x
