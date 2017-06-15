namespace Hedgehog

open Hedgehog.Numeric

/// $setup
/// >>> let x = 3

/// Tests are parameterized by the `Size` of the randomly-generated data,
/// the meaning of which depends on the particular generator used.
type Size = int

/// A range describes the bounds of a number to generate, which may or may not
/// be dependent on a 'Size'.
type Range<'a> =
    | Range of ('a * (Size -> 'a * 'a))

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
    ///
    /// >>> Range.bounds x <| Range.singleton 5
    /// (5, 5)
    ///
    /// >>> Range.origin <| Range.singleton 5
    /// 5
    ///
    let singleton (x : 'a) : Range<'a> =
        Range (x, fun _ -> x, x)

    /// Construct a range which is unaffected by the size parameter with a
    /// origin point which may differ from the bounds.
    ///
    /// A range from @-10@ to @10@, with the origin at @0@:
    ///
    /// >>> Range.bounds x <| Range.constantFrom 0 (-10) 10
    /// (-10, 10)
    ///
    /// >>> Range.origin <| Range.constantFrom 0 (-10) 10
    /// 0
    ///
    /// A range from @1970@ to @2100@, with the origin at @2000@:
    ///
    /// >>> Range.bounds x <| Range.constantFrom 2000 1970 2100
    /// (1970, 2100)
    ///
    /// >>> Range.origin <| Range.constantFrom 2000 1970 2100
    /// 2000
    ///
    let constantFrom (z : 'a) (x : 'a) (y : 'a) : Range<'a> =
        Range (z, fun _ -> x, y)

    /// Construct a range which is unaffected by the size parameter.
    ///
    /// A range from @0@ to @10@, with the origin at @0@:
    ///
    /// >>> Range.bounds x <| Range.constant 0 10
    /// (0, 10)
    ///
    /// >>> Range.origin <| Range.constant 0 10
    /// 0
    ///
    let constant (x : 'a) : ('a -> Range<'a>) =
        constantFrom x x

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    ///
    /// A range from @-128@ to @127@, with the origin at @0@:
    ///
    /// >>> Range.bounds x (Range.constantBounded () : Range<sbyte>)
    /// (-128y, 127y)
    ///
    /// >>> Range.origin <| (Range.constantBounded () : Range<sbyte>)
    /// 0y
    ///
    let inline constantBounded () : Range<'a> =
        let lo = minValue ()
        let hi = maxValue ()
        let zero = LanguagePrimitives.GenericZero

        constantFrom zero lo hi

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
        ///
        /// >>> Range.Internal.clamp 5 10 15
        /// 10
        ///
        /// >>> Range.Internal.clamp 5 10 0
        /// 5
        ///
        let clamp (x : 'a) (y : 'a) (n : 'a) =
            if x > y then
                min x (max y n)
            else
                min y (max x n)

        /// Scale an integral linearly with the size parameter.
        let inline scaleLinear (sz0 : Size) (z0 : 'a) (n0 : 'a) : 'a =
            let sz =
                max 0 (min 99 sz0)

            let z =
                toBigInt z0

            let n =
                toBigInt n0

            let diff =
                ((n - z) * bigint sz) / (bigint 99)

            fromBigInt (z + diff)

        /// Scale an integral exponentially with the size parameter.
        let inline scaleExponential (sz0 : Size) (z0 : 'a) (n0 : 'a) : 'a =
            let sz =
                clamp 0 99 sz0

            let z =
                toBigInt z0

            let n =
                toBigInt n0

            let diff =
                 (((float (abs (n - z) + 1I)) ** (float sz / 99.0)) - 1.0) * float (sign (n - z))

            fromBigInt (bigint (round (float z + diff)))

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    ///
    /// >>> Range.bounds 0 <| Range.linearFrom 0 (-10) 10
    /// (0, 0)
    ///
    /// >>> Range.bounds 50 <| Range.linearFrom 0 (-10) 20
    /// (-5, 10)
    ///
    /// >>> Range.bounds 99 <| Range.linearFrom 0 (-10) 20
    /// (-10, 20)
    ///
    let inline linearFrom (z : 'a) (x : 'a) (y : 'a) : Range<'a> =
        Range (z, fun sz ->
            let x_sized =
                clamp x y (scaleLinear sz z x)
            let y_sized =
                clamp x y (scaleLinear sz z y)
            x_sized, y_sized)

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    ///
    /// >>> Range.bounds 0 <| Range.linear 0 10
    /// (0, 0)
    ///
    /// >>> Range.bounds 50 <| Range.linear 0 10
    /// (0, 5)
    ///
    /// >>> Range.bounds 99 <| Range.linear 0 10
    /// (0, 10)
    ///
    let inline linear (x : 'a) : ('a -> Range<'a>) =
      linearFrom x x

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    ///
    /// >>> Range.bounds 0 (Range.linearBounded () : Range<sbyte>)
    /// (-0y, 0y)
    ///
    /// >>> Range.bounds 50 (Range.linearBounded () : Range<sbyte>)
    /// (-64y, 64y)
    ///
    /// >>> Range.bounds 99 (Range.linearBounded () : Range<sbyte>)
    /// (-128y, 127y)
    ///
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
    ///
    /// >>> Range.bounds 0 (Range.exponentialFrom 0 -128 512)
    /// (0, 0)
    ///
    /// >>> Range.bounds 25 (Range.exponentialFrom 0 -128 512)
    /// (-2, 4)
    ///
    /// >>> Range.bounds 50 (Range.exponentialFrom 0 -128 512)
    /// (-11, 22)
    ///
    /// >>> Range.bounds 75 (Range.exponentialFrom 0 -128 512)
    /// (-39, 112)
    ///
    /// >>> Range.bounds 99 (Range.exponentialFrom x -128 512)
    /// (-128, 512)
    ///
    let inline exponentialFrom (z : 'a) (x : 'a) (y : 'a) : Range<'a> =
        Range (z, fun sz ->
            let x_sized =
                clamp x y (scaleExponential sz z x)
            let y_sized =
                clamp x y (scaleExponential sz z y)
            x_sized, y_sized)

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    ///
    /// >>> Range.bounds 0 (Range.exponential 1 512)
    /// (1, 1)
    ///
    /// >>> Range.bounds 11 (Range.exponential 1 512)
    /// (1, 2)
    ///
    /// >>> Range.bounds 22 (Range.exponential 1 512)
    /// (1, 4)
    ///
    /// >>> Range.bounds 77 (Range.exponential 1 512)
    /// (1, 128)
    ///
    /// >>> Range.bounds 88 (Range.exponential 1 512)
    /// (1, 256)
    ///
    /// >>> Range.bounds 99 (Range.exponential 1 512)
    /// (1, 512)
    ///
    let inline exponential (x : 'a) (y : 'a) : Range<'a> =
        exponentialFrom x x y

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    ///
    /// >>> Range.bounds 0 (Range.exponentialBounded () : Range<sbyte>)
    /// (0y, 0y)
    ///
    /// >>> Range.bounds 50 (Range.exponentialBounded () : Range<sbyte>)
    /// (-11y, 11y)
    ///
    /// >>> Range.bounds 99 (Range.exponentialBounded () : Range<sbyte>)
    /// (-128y, 127y)
    ///
    let inline exponentialBounded () : Range<'a> =
        let lo = minValue ()
        let hi = maxValue ()
        let zero = LanguagePrimitives.GenericZero

        exponentialFrom zero lo hi
