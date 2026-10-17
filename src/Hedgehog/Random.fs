namespace Hedgehog

/// What 'Random.map' keeps, instead of nesting more closures, once a generator has been mapped many times:
/// the generator to start from and the maps to apply to its value, in a list that is run in a loop.
/// The type of the generated value is erased so that the maps can have different types.
[<AllowNullLiteral>]
type internal Mappings (source : Seed -> Size -> obj, applied : (obj -> obj) list) =
    /// The generator that the maps are applied to.
    member _.Source = source

    /// The maps, in reverse order: the function of the last map comes first.
    member _.Applied = applied

    member _.Run (seed : Seed, size : Size) : obj =
        let mutable x = source seed size
        for f in List.rev applied do
            x <- f x
        x

/// A generator for random values of type 'a.
///
/// 'Random.map' nests a closure in the generator, which is as cheap as it gets. But running a generator that was
/// mapped many times would take a call stack as deep as the number of maps, so after the first few maps the rest
/// are kept in a list that is run in a loop instead. The representation is internal;
/// use 'Random.variable' to create a generator from a function.
[<Struct>]
type Random<'a> =
    internal
    // 'nesting' is how many maps are nested in 'run'. 'mappings' is null until a generator has more maps than that,
    // and from then on 'run' evaluates it.
    | Random of run : (Seed -> Size -> 'a) * nesting : int * mappings : Mappings

namespace Hedgehog.FSharp

open Hedgehog
open Hedgehog.Numeric

module Random =
    /// How many maps are nested in a generator before 'map' starts to keep them in a list. Each nested map takes
    /// a couple of stack frames, so this is small enough for the stack and large enough to leave most generators alone.
    [<Literal>]
    let private MaxNesting = 64

    let private unsafeRun (seed : Seed) (size : Size) (Random (run, _, _) : Random<'a>) : 'a =
        run seed size

    let run (seed : Seed) (size : Size) (r : Random<'a>) : 'a =
        unsafeRun seed (max 1 size) r

    /// Creates a generator from a function of the seed and the size.
    /// Compare 'constant', which ignores both.
    let variable (f : Seed -> Size -> 'a) : Random<'a> =
        Random (f, 0, null)

    let delay (f : unit -> Random<'a>) : Random<'a> =
        variable (fun seed size ->
            f () |> unsafeRun seed size)

    let tryFinally (after : unit -> unit) (r : Random<'a>) : Random<'a> =
        variable (fun seed size ->
            try
                unsafeRun seed size r
            finally
                after ())

    let tryWith (k : exn -> Random<'a>) (r : Random<'a>) : Random<'a> =
        variable (fun seed size ->
            try
                unsafeRun seed size r
            with
                x -> unsafeRun seed size (k x))

    let constant (x : 'a) : Random<'a> =
        variable (fun _ _ -> x)

    /// Applies the function to the generated value.
    /// However many times this is applied the stack does not grow: after the first few maps, the functions are
    /// run one after another in a loop instead of one inside another.
    let map (f : 'a -> 'b) (Random (run, nesting, mappings) : Random<'a>) : Random<'b> =
        if isNull mappings && nesting < MaxNesting then
            Random ((fun seed size -> f (run seed size)), nesting + 1, null)
        else
            let source, applied =
                if isNull mappings then
                    (fun seed size -> box (run seed size)), []
                else
                    mappings.Source, mappings.Applied
            let mappings = Mappings (source, (fun x -> box (f (unbox<'a> x))) :: applied)
            Random ((fun seed size -> unbox<'b> (mappings.Run (seed, size))), 0, mappings)

    let bind (k : 'a -> Random<'b>) (r : Random<'a>) : Random<'b> =
        variable (fun seed size ->
            let seed1, seed2 = Seed.split seed
            r
            |> unsafeRun seed1 size
            |> k
            |> unsafeRun seed2 size)

    let replicate (times : int) (r : Random<'a>) : Random<'a seq> =
        variable (fun seed0 size ->
            let seq = seq {
                let mutable seed = seed0
                while true do
                    let seed1, seed2 = Seed.split seed
                    yield unsafeRun seed1 size r
                    seed <- seed2
            }
            seq |> Seq.take times)

    /// Used to construct generators that depend on the size parameter.
    let sized (f : Size -> Random<'a>) : Random<'a> =
        variable (fun seed size ->
            unsafeRun seed size (f size))

    /// Overrides the size parameter. Returns a generator which uses the
    /// given size instead of the runtime-size parameter.
    let resize (newSize : Size) (r : Random<'a>) : Random<'a> =
        variable (fun seed _ ->
          run seed newSize r)

    /// Generates a random integral number in the given inclusive range.
    let inline integral (range : Range<'a>) : Random<'a> =
        variable (fun seed size ->
            let (lo, hi) = Range.bounds size range
            if typeof<'a> = typeof<int32> then
                let x, _ = Seed.nextInt32 (unbox (box lo)) (unbox (box hi)) seed
                unbox (box x)
            elif typeof<'a> = typeof<int64> then
                let x, _ = Seed.nextInt64 (unbox (box lo)) (unbox (box hi)) seed
                unbox (box x)
            else
                let x, _ = Seed.nextBigInt (toBigInt lo) (toBigInt hi) seed
                fromBigInt x)

    /// Generates a random floating point number in the given inclusive range.
    let inline double (range : Range<double>) : Random<double> =
        variable (fun seed size ->
            let (lo, hi) = Range.bounds size range
            let x, _ = Seed.nextDouble lo hi seed
            x)

    /// A random value that always matches the seed. Useful for testing.
    let seed : Random<Seed> =
        variable (fun seed _ -> seed)
