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

    [<CompiledName("Map")>]
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
    [<CompiledName("Origin")>]
    let origin (Range (z, _) : Range<'a>) : 'a =
        z

    /// Get the extents of a range, for a given size.
    [<CompiledName("Bounds")>]
    let bounds (sz : Size) (Range (_, f) : Range<'a>) : 'a * 'a =
        f sz

    /// Get the lower bound of a range for the given size.
    [<CompiledName("LowerBound")>]
    let lowerBound (sz : Size) (range : Range<'a>) : 'a =
        let (x, y) =
            bounds sz range
        min x y

    /// Get the upper bound of a range for the given size.
    [<CompiledName("UpperBound")>]
    let upperBound (sz : Size) (range : Range<'a>) : 'a =
        let (x, y) =
            bounds sz range
        max x y

    //
    // Combinators - Constant
    //

    /// Construct a range which represents a constant single value.
    [<CompiledName("Singleton")>]
    let singleton (x : 'a) : Range<'a> =
        Range (x, fun _ -> x, x)

    /// Construct a range which is unaffected by the size parameter with a
    /// origin point which may differ from the bounds.
    [<CompiledName("ConstantFrom")>]
    let constantFrom (z : 'a) (x : 'a) (y : 'a) : Range<'a> =
        Range (z, fun _ -> x, y)

    /// Construct a range which is unaffected by the size parameter.
    [<CompiledName("Constant")>]
    let constant (x : 'a) (y : 'a) : Range<'a> =
        constantFrom x x y

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    [<CompiledName("`ConstantBounded")>]
    let inline constantBounded () : Range<'a> =
        let lo = minValue ()
        let hi = maxValue ()
        let zero = LanguagePrimitives.GenericZero

        constantFrom zero lo hi

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    [<CompiledName("ConstantBoundedInt8")>]
    let __constantBoundedInt8 : Range<int8> =
        constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    [<CompiledName("ConstantBoundedInt16")>]
    let __constantBoundedInt16 : Range<Int16> =
        constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    [<CompiledName("ConstantBoundedInt32")>]
    let __constantBoundedInt32 : Range<Int32> =
        constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    [<CompiledName("ConstantBoundedInt64")>]
    let __constantBoundedInt64 : Range<Int64> =
        constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    [<CompiledName("ConstantBoundedUInt8")>]
    let __constantBoundedUInt8 : Range<uint8> =
        constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    [<CompiledName("ConstantBoundedUInt16")>]
    let __constantBoundedUInt16 : Range<UInt16> =
        constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    [<CompiledName("ConstantBoundedUInt32")>]
    let __constantBoundedUInt32 : Range<UInt32> =
        constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    [<CompiledName("ConstantBoundedUInt64")>]
    let __constantBoundedUInt64 : Range<UInt64> =
        constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    [<CompiledName("ConstantBoundedFloat")>]
    let __constantBoundedFloat : Range<float> =
        constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    [<CompiledName("ConstantBoundedDouble")>]
    let __constantBoundedDouble : Range<double> =
        constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    [<CompiledName("ConstantBoundedDecimal")>]
    let __constantBoundedDecimal : Range<decimal> =
        constantBounded ()

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
        let inline scaleExponential (lo : 'a) (hi : 'a) (sz0 : Size) (z0 : 'a) (n0 : 'a) : 'a =
            let sz =
                clamp 0 99 sz0

            let z =
                toBigInt z0

            let n =
                toBigInt n0

            let diff =
                 (((float (abs (n - z) + 1I)) ** (float sz / 99.0)) - 1.0) * float (sign (n - z))

            // https://github.com/hedgehogqa/fsharp-hedgehog/issues/185
            fromBigInt (clamp (toBigInt lo) (toBigInt hi) (bigint (round (float z + diff))))

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    [<CompiledName("`LinearFrom")>]
    let inline linearFrom (z : 'a) (x : 'a) (y : 'a) : Range<'a> =
        Range (z, fun sz ->
            let x_sized =
                clamp x y (scaleLinear sz z x)
            let y_sized =
                clamp x y (scaleLinear sz z y)
            x_sized, y_sized)

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    [<CompiledName("LinearFromInt8")>]
    let __linearFromInt8 (z, x, y) : Range<int8> =
        linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    [<CompiledName("LinearFromInt16")>]
    let __linearFromInt16 (z, x, y) : Range<Int16> =
        linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    [<CompiledName("LinearFromInt32")>]
    let __linearFromInt32 (z, x, y) : Range<Int32> =
        linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    [<CompiledName("LinearFromInt64")>]
    let __linearFromInt64 (z, x, y) : Range<Int64> =
        linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    [<CompiledName("LinearFromUInt8")>]
    let __linearFromUInt8 (z, x, y) : Range<uint8> =
        linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    [<CompiledName("LinearFromUInt16")>]
    let __linearFromUInt16 (z, x, y) : Range<UInt16> =
        linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    [<CompiledName("LinearFromUInt32")>]
    let __linearFromUInt32 (z, x, y) : Range<UInt32> =
        linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    [<CompiledName("LinearFromUInt64")>]
    let __linearFromUInt64 (z, x, y) : Range<UInt64> =
        linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    [<CompiledName("LinearFromFloat")>]
    let __linearFromFloat (z, x, y) : Range<float> =
        linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    [<CompiledName("LinearFromDouble")>]
    let __linearFromDouble (z, x, y) : Range<double> =
        linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    [<CompiledName("LinearFromDecimal")>]
    let __linearFromDecimal (z, x, y) : Range<decimal> =
        linearFrom z x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    [<CompiledName("`Linear")>]
    let inline linear (x : 'a) (y : 'a) : Range<'a> =
      linearFrom x x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    [<CompiledName("LinearInt8")>]
    let __linearInt8 (x, y) : Range<int8> =
        linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    [<CompiledName("LinearInt16")>]
    let __linearInt16 (x, y) : Range<Int16> =
        linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    [<CompiledName("LinearInt32")>]
    let __linearInt32 (x, y) : Range<Int32> =
        linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    [<CompiledName("LinearInt64")>]
    let __linearInt64 (x, y) : Range<Int64> =
        linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    [<CompiledName("LinearUInt8")>]
    let __linearUInt8 (x, y) : Range<uint8> =
        linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    [<CompiledName("LinearUInt16")>]
    let __linearUInt16 (x, y) : Range<UInt16> =
        linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    [<CompiledName("LinearUInt32")>]
    let __linearUInt32 (x, y) : Range<UInt32> =
        linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    [<CompiledName("LinearUInt64")>]
    let __linearUInt64 (x, y) : Range<UInt64> =
        linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    [<CompiledName("LinearFloat")>]
    let __linearFloat (x, y) : Range<float> =
        linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    [<CompiledName("LinearDouble")>]
    let __linearDouble (z, x, y) : Range<double> =
        linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    [<CompiledName("LinearDecimal")>]
    let __linearDecimal (z, x, y) : Range<decimal> =
        linear x y

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    [<CompiledName("`LinearBounded")>]
    let inline linearBounded () : Range<'a> =
        let lo = minValue ()
        let hi = maxValue ()
        let zero = LanguagePrimitives.GenericZero

        linearFrom zero lo hi

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    [<CompiledName("LinearBoundedInt8")>]
    let __linearBoundedInt8 : Range<int8> =
        linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    [<CompiledName("LinearBoundedInt16")>]
    let __linearBoundedInt16 : Range<Int16> =
        linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    [<CompiledName("LinearBoundedInt32")>]
    let __linearBoundedInt32 : Range<Int32> =
        linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    [<CompiledName("LinearBoundedInt64")>]
    let __linearBoundedInt64 : Range<Int64> =
        linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    [<CompiledName("LinearBoundedUInt8")>]
    let __linearBoundedUInt8 : Range<uint8> =
        linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    [<CompiledName("LinearBoundedUInt16")>]
    let __linearBoundedUInt16 : Range<UInt16> =
        linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    [<CompiledName("LinearBoundedUInt32")>]
    let __linearBoundedUInt32 : Range<UInt32> =
        linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    [<CompiledName("LinearBoundedUInt64")>]
    let __linearBoundedUInt64 : Range<UInt64> =
        linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    [<CompiledName("LinearBoundedFloat")>]
    let __linearBoundedFloat : Range<float> =
        linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    [<CompiledName("LinearBoundedDouble")>]
    let __linearBoundedDouble : Range<double> =
        linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    [<CompiledName("LinearBoundedDecimal")>]
    let __linearBoundedDecimal : Range<decimal> =
        linearBounded ()

    //
    // Combinators - Exponential
    //

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    [<CompiledName("`ExponentialFrom")>]
    let inline exponentialFrom (z : 'a) (x : 'a) (y : 'a) : Range<'a> =
        Range (z, fun sz ->
            let scale =
                // https://github.com/hedgehogqa/fsharp-hedgehog/issues/185
                scaleExponential x y sz z
            let x_sized =
                scale x
            let y_sized =
                scale y
            x_sized, y_sized)

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    [<CompiledName("ExponentialFromInt8")>]
    let __exponentialFromInt8 (z, x, y) : Range<int8> =
        exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    [<CompiledName("ExponentialFromInt16")>]
    let __exponentialFromInt16 (z, x, y) : Range<Int16> =
        exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    [<CompiledName("ExponentialFromInt32")>]
    let __exponentialFromInt32 (z, x, y) : Range<Int32> =
        exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    [<CompiledName("ExponentialFromInt64")>]
    let __exponentialFromInt64 (z, x, y) : Range<Int64> =
        exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    [<CompiledName("ExponentialFromUInt8")>]
    let __exponentialFromUInt8 (z, x, y) : Range<uint8> =
        exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    [<CompiledName("ExponentialFromUInt16")>]
    let __exponentialFromUInt16 (z, x, y) : Range<UInt16> =
        exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    [<CompiledName("ExponentialFromUInt32")>]
    let __exponentialFromUInt32 (z, x, y) : Range<UInt32> =
        exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    [<CompiledName("ExponentialFromUInt64")>]
    let __exponentialFromUInt64 (z, x, y) : Range<UInt64> =
        exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    [<CompiledName("ExponentialFromFloat")>]
    let __exponentialFromFloat (z, x, y) : Range<float> =
        exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    [<CompiledName("ExponentialFromDouble")>]
    let __exponentialFromDouble (z, x, y) : Range<double> =
        exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    [<CompiledName("ExponentialFromDecimal")>]
    let __exponentialFromDecimal (z, x, y) : Range<decimal> =
        exponentialFrom z x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    [<CompiledName("`Exponential")>]
    let inline exponential (x : 'a) (y : 'a) : Range<'a> =
        exponentialFrom x x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    [<CompiledName("ExponentialInt8")>]
    let __exponentialInt8 (x, y) : Range<int8> =
        exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    [<CompiledName("ExponentialInt16")>]
    let __exponentialInt16 (x, y) : Range<Int16> =
        exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    [<CompiledName("ExponentialInt32")>]
    let __exponentialInt32 (x, y) : Range<Int32> =
        exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    [<CompiledName("ExponentialInt64")>]
    let __exponentialInt64 (x, y) : Range<Int64> =
        exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    [<CompiledName("ExponentialUInt8")>]
    let __exponentialUInt8 (x, y) : Range<uint8> =
        exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    [<CompiledName("ExponentialUInt16")>]
    let __exponentialUInt16 (x, y) : Range<UInt16> =
        exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    [<CompiledName("ExponentialUInt32")>]
    let __exponentialUInt32 (x, y) : Range<UInt32> =
        exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    [<CompiledName("ExponentialUInt64")>]
    let __exponentialUInt64 (x, y) : Range<UInt64> =
        exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    [<CompiledName("ExponentialFloat")>]
    let __exponentialFloat (x, y) : Range<float> =
        exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    [<CompiledName("ExponentialDouble")>]
    let __exponentialDouble (x, y) : Range<double> =
        exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    [<CompiledName("ExponentialDecimal")>]
    let __exponentialDecimal (x, y) : Range<decimal> =
        exponential x y

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    [<CompiledName("`ExponentialBounded")>]
    let inline exponentialBounded () : Range<'a> =
        let lo = minValue ()
        let hi = maxValue ()
        let zero = LanguagePrimitives.GenericZero

        exponentialFrom zero lo hi

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    [<CompiledName("ExponentialBoundedInt8")>]
    let __exponentialBoundedInt8 : Range<int8> =
        exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    [<CompiledName("ExponentialBoundedInt16")>]
    let __exponentialBoundedInt16 : Range<Int16> =
        exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    [<CompiledName("ExponentialBoundedInt32")>]
    let __exponentialBoundedInt32 : Range<Int32> =
        exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    [<CompiledName("ExponentialBoundedInt64")>]
    let __exponentialBoundedInt64 : Range<Int64> =
        exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    [<CompiledName("ExponentialBoundedUInt8")>]
    let __exponentialBoundedUInt8 : Range<uint8> =
        exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    [<CompiledName("ExponentialBoundedUInt16")>]
    let __exponentialBoundedUInt16 : Range<UInt16> =
        exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    [<CompiledName("ExponentialBoundedUInt32")>]
    let __exponentialBoundedUInt32 : Range<UInt32> =
        exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    [<CompiledName("ExponentialBoundedUInt64")>]
    let __exponentialBoundedUInt64 : Range<UInt64> =
        exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    [<CompiledName("ExponentialBoundedFloat")>]
    let __exponentialBoundedFloat : Range<float> =
        exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    [<CompiledName("ExponentialBoundedDouble")>]
    let __exponentialBoundedDouble : Range<double> =
        exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    [<CompiledName("ExponentialBoundedDecimal")>]
    let __exponentialBoundedDecimal : Range<decimal> =
        exponentialBounded ()
