namespace Hedgehog

open Hedgehog.Numeric

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
    /// Get the origin of a range. This might be the mid-point or the lower bound,
    /// depending on what the range represents.
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
    let constant (x : 'a) : ('a -> Range<'a>) =
        constantFrom x x

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    let inline constantBounded () : Range<'a> =
        let lo = minValue ()
        let hi = maxValue ()
        let zero = LanguagePrimitives.GenericZero

        constantFrom zero lo hi
