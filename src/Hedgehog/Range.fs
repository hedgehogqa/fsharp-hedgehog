namespace Hedgehog

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
