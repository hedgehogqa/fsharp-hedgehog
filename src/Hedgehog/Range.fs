namespace Hedgehog

open System
open Hedgehog.Numeric

/// Tests are parameterized by the `Size` of the randomly-generated data,
/// the meaning of which depends on the particular generator used.
type Size = int

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
    let origin (Range (origin, _) : Range<'a>) : 'a =
        origin

    /// Map a modification function over a Range.
    let map (f : 'a -> 'b) (Range (origin, g) : Range<'a>) : Range<'b> =
        Range (f origin, fun sz ->
            bimap f f (g sz))

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
    let constantFrom (origin : 'a) (lowerBound : 'a) (upperBound : 'a) : Range<'a> =
        Range (origin, fun _ -> lowerBound, upperBound)

    /// Construct a range which is unaffected by the size parameter.
    let constant (lowerBound : 'a) (upperBound : 'a) : Range<'a> =
        constantFrom lowerBound lowerBound upperBound

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

        /// Truncate `value` so it stays within bounds.
        let clamp (bound1 : 'a) (bound2 : 'a) (value : 'a) : 'a =
            if bound1 > bound2 then
                min bound1 (max bound2 value)
            else
                min bound2 (max bound1 value)

        /// Scale an integral linearly with the size parameter.
        let inline scaleLinear (sz : Size) (origin : 'a) (bound : 'a) : 'a =
            let sz = max 0 (min 99 sz)
            let origin = toBigInt origin
            let bound = toBigInt bound
            let diff = ((bound - origin) * bigint sz) / (bigint 99)
            fromBigInt (origin + diff)

        /// Scale an integral exponentially with the size parameter.
        let inline scaleExponential (lo : 'a) (hi : 'a) (sz : Size) (origin : 'a) (bound : 'a) : 'a =
            let sz = clamp 0 99 sz
            let origin = toBigInt origin
            let bound = toBigInt bound
            let diff = (((float (abs (bound - origin) + 1I)) ** (float sz / 99.0)) - 1.0) * float (sign (bound - origin))

            // https://github.com/hedgehogqa/fsharp-hedgehog/issues/185
            fromBigInt (clamp (toBigInt lo) (toBigInt hi) (bigint (round (float origin + diff))))

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    let inline linearFrom (origin : 'a) (lowerBound : 'a) (upperBound : 'a) : Range<'a> =
        Range (origin, fun sz ->
            let xSized =
                clamp lowerBound upperBound (scaleLinear sz origin lowerBound)
            let ySized =
                clamp lowerBound upperBound (scaleLinear sz origin upperBound)
            xSized, ySized)

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    let inline linear (lowerBound : 'a) (upperBound : 'a) : Range<'a> =
        linearFrom lowerBound lowerBound upperBound

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
    let inline exponentialFrom (origin : 'a) (lowerBound : 'a) (upperBound : 'a) : Range<'a> =
        Range (origin, fun sz ->
            let scale =
                // https://github.com/hedgehogqa/fsharp-hedgehog/issues/185
                scaleExponential lowerBound upperBound sz origin
            scale lowerBound, scale upperBound)

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    let inline exponential (lowerBound : 'a) (upperBound : 'a) : Range<'a> =
        exponentialFrom lowerBound lowerBound upperBound

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    let inline exponentialBounded () : Range<'a> =
        let lo = minValue ()
        let hi = maxValue ()
        let zero = LanguagePrimitives.GenericZero

        exponentialFrom zero lo hi
