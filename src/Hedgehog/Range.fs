namespace Hedgehog

open System
open Hedgehog.Numeric

/// A range describes the bounds of a number to generate, which may or may not
/// be dependent on a 'Size'.
///
/// The constructor takes an origin between the lower and upper bound, and a
/// function from 'Size' to bounds.  As the size goes towards 0, the values
/// go towards the origin.
type Range<'a> =
    | Range of origin : 'a * (Size -> 'a * 'a)

module Range =
    let private bimap (f : 'a -> 'b) (g : 'c -> 'd) (a : 'a, b : 'c) : 'b * 'd =
        f a, g b

    let map (f : 'a -> 'b) (Range (z, g) : Range<'a>) : Range<'b> =
        Range (f z, fun sz ->
            bimap f f (g sz))

    //
    // Combinators - Range
    //

    /// Get the origin of a range. This might be the mid-point or the lower
    /// bound, depending on what the range represents.
    ///
    /// The 'bounds' of a range are scaled around this value when using the
    /// 'linear' family of combinators.
    ///
    /// When using a 'Range' to generate numbers, the shrinking function will
    /// shrink towards the origin.
    let origin (Range (z, _) : Range<'a>) : 'a =
        z

    /// Get the extents of a range, for a given size.
    let bounds (sz : Size) (Range (_, f) : Range<'a>) : 'a * 'a =
        f sz

    /// Get the lower bound of a range for the given size.
    let lowerBound (sz : Size) (range : Range<'a>) : 'a =
        let (x, y) =
            bounds sz range
        min x y

    /// Get the upper bound of a range for the given size.
    let upperBound (sz : Size) (range : Range<'a>) : 'a =
        let (x, y) =
            bounds sz range
        max x y

    //
    // Combinators - Constant
    //

    /// Construct a range which represents a constant single value.
    let singleton (x : 'a) : Range<'a> =
        Range (x, fun _ -> x, x)

    /// Construct a range which is unaffected by the size parameter with a
    /// origin point which may differ from the bounds.
    let constantFrom (z : 'a) (x : 'a) (y : 'a) : Range<'a> =
        Range (z, fun _ -> x, y)

    /// Construct a range which is unaffected by the size parameter.
    let constant (x : 'a) (y : 'a) : Range<'a> =
        constantFrom x x y

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    let inline constantBounded () : Range<'a> =
        let lo = minValue ()
        let hi = maxValue ()
        let zero = LanguagePrimitives.GenericZero

        constantFrom zero lo hi

    //
    // Factories
    //

    let internal ofArray (xs : 'a array) : Range<int> =
        constant 0 (Array.length xs - 1)

    //
    // Combinators - Linear
    //

    [<AutoOpen>]
    module Internal =
        // The functions in this module where initially marked as internal
        // but then the F# compiler complained with the following message:
        //
        // The value 'linearFrom' was marked inline but its implementation
        // makes use of an internal or private function which is not
        // sufficiently accessible.

        /// Truncate a value so it stays within some range.
        let clamp (x : 'a) (y : 'a) (n : 'a) : 'a =
            if x > y then
                min x (max y n)
            else
                min y (max x n)

        /// Scale an integral linearly with the size parameter.
        let inline scaleLinear (sz0 : Size) (z0 : 'a) (n0 : 'a) : 'a =
            let z =
                toBigInt z0

            let n =
                toBigInt n0

            let lerp =
                sz0 |> Size.BigInt.lerp z n

            fromBigInt lerp

        /// Scale an integral exponentially with the size parameter.
        let inline scaleExponential (lo : 'a) (hi : 'a) (sz0 : Size) (z0 : 'a) (n0 : 'a) : 'a =
            let z = toBigInt z0
            let n = toBigInt n0

            let diff =
                 (((float (abs (n - z) + 1I)) ** (Size.Double.normalized sz0)) - 1.0) * float (sign (n - z))

            // https://github.com/hedgehogqa/fsharp-hedgehog/issues/185
            fromBigInt (clamp (toBigInt lo) (toBigInt hi) (bigint (round (float z + diff))))

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    let inline linearFrom (z : 'a) (x : 'a) (y : 'a) : Range<'a> =
        Range (z, fun sz ->
            let xSized =
                clamp x y (scaleLinear sz z x)
            let ySized =
                clamp x y (scaleLinear sz z y)
            xSized, ySized)

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    let inline linear (x : 'a) (y : 'a) : Range<'a> =
      linearFrom x x y

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    let inline linearBounded () : Range<'a> =
        let lo = minValue ()
        let hi = maxValue ()
        let zero = LanguagePrimitives.GenericZero

        linearFrom zero lo hi

    //
    // Combinators - Exponential
    //

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    let inline exponentialFrom (z : 'a) (x : 'a) (y : 'a) : Range<'a> =
        Range (z, fun sz ->
            let scale =
                // https://github.com/hedgehogqa/fsharp-hedgehog/issues/185
                scaleExponential x y sz z
            let xSized =
                scale x
            let ySized =
                scale y
            xSized, ySized)

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    let inline exponential (x : 'a) (y : 'a) : Range<'a> =
        exponentialFrom x x y

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    let inline exponentialBounded () : Range<'a> =
        let lo = minValue ()
        let hi = maxValue ()
        let zero = LanguagePrimitives.GenericZero

        exponentialFrom zero lo hi
