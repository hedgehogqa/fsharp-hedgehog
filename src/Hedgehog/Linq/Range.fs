namespace Hedgehog.Linq

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog

type Range private () =

    //
    // Combinators - Constant
    //

    /// Construct a range which represents a constant single value.
    static member FromValue (value : 'T) : Range<'T> =
        Range.singleton value

    /// Construct a range which is unaffected by the size parameter with a
    /// origin point which may differ from the bounds.
    static member Constant (z : 'T, x : 'T, y : 'T) : Range<'T> =
        Range.constantFrom z x y

    /// Construct a range which is unaffected by the size parameter.
    static member Constant (x : 'T, y : 'T) : Range<'T> =
        Range.constant x y

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    static member ConstantBoundedSByte () : Range<sbyte> =
        Range.constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    static member ConstantBoundedInt16 () : Range<int16> =
        Range.constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    static member ConstantBoundedInt32 () : Range<int32> =
        Range.constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    static member ConstantBoundedInt64 () : Range<int64> =
        Range.constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    static member ConstantBoundedByte () : Range<byte> =
        Range.constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    static member ConstantBoundedUInt16 () : Range<uint16> =
        Range.constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    static member ConstantBoundedUInt32 () : Range<uint32> =
        Range.constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    static member ConstantBoundedUInt64 () : Range<uint64> =
        Range.constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    static member ConstantBoundedSingle () : Range<single> =
        Range.constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    static member ConstantBoundedDouble () : Range<double> =
        Range.constantBounded ()

    /// Construct a range which is unaffected by the size parameter using the
    /// full range of a data type.
    static member ConstantBoundedDecimal () : Range<decimal> =
        Range.constantBounded ()

    //
    // Combinators - Linear
    //

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    static member LinearFromSByte (z, x, y) : Range<sbyte> =
        Range.linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    static member LinearFromInt16 (z, x, y) : Range<int16> =
        Range.linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    static member LinearFromInt32 (z, x, y) : Range<int32> =
        Range.linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    static member LinearFromInt64 (z, x, y) : Range<int64> =
        Range.linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    static member LinearFromByte (z, x, y) : Range<byte> =
        Range.linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    static member LinearFromUInt16 (z, x, y) : Range<uint16> =
        Range.linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    static member LinearFromUInt32 (z, x, y) : Range<uint32> =
        Range.linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    static member LinearFromUInt64 (z, x, y) : Range<uint64> =
        Range.linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    static member LinearFromSingle (z, x, y) : Range<single> =
        Range.linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    static member LinearFromDouble (z, x, y) : Range<double> =
        Range.linearFrom z x y

    /// Construct a range which scales the bounds relative to the size
    /// parameter.
    static member LinearFromDecimal (z, x, y) : Range<decimal> =
        Range.linearFrom z x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    static member LinearSByte (x, y) : Range<sbyte> =
        Range.linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    static member LinearInt16 (x, y) : Range<int16> =
        Range.linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    static member LinearInt32 (x, y) : Range<int32> =
        Range.linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    static member LinearInt64 (x, y) : Range<int64> =
        Range.linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    static member LinearByte (x, y) : Range<byte> =
        Range.linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    static member LinearUInt16 (x, y) : Range<uint16> =
        Range.linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    static member LinearUInt32 (x, y) : Range<uint32> =
        Range.linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    static member LinearUInt64 (x, y) : Range<uint64> =
        Range.linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    static member LinearSingle (x, y) : Range<single> =
        Range.linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    static member LinearDouble (x, y) : Range<double> =
        Range.linear x y

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    static member LinearDecimal (x, y) : Range<decimal> =
        Range.linear x y

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    static member LinearBoundedSByte () : Range<sbyte> =
        Range.linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    static member LinearBoundedInt16 () : Range<int16> =
        Range.linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    static member LinearBoundedInt32 () : Range<int32> =
        Range.linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    static member LinearBoundedInt64 () : Range<int64> =
        Range.linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    static member LinearBoundedByte () : Range<byte> =
        Range.linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    static member LinearBoundedUInt16 () : Range<uint16> =
        Range.linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    static member LinearBoundedUInt32 () : Range<uint32> =
        Range.linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    static member LinearBoundedUInt64 () : Range<uint64> =
        Range.linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    static member LinearBoundedSingle () : Range<single> =
        Range.linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    static member LinearBoundedDouble () : Range<double> =
        Range.linearBounded ()

    /// Construct a range which is scaled relative to the size parameter and
    /// uses the full range of a data type.
    static member LinearBoundedDecimal () : Range<decimal> =
        Range.linearBounded ()

    //
    // Combinators - Exponential
    //

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    static member ExponentialFromSByte (z, x, y) : Range<sbyte> =
        Range.exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    static member ExponentialFromInt16 (z, x, y) : Range<int16> =
        Range.exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    static member ExponentialFromInt32 (z, x, y) : Range<int32> =
        Range.exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    static member ExponentialFromInt64 (z, x, y) : Range<int64> =
        Range.exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    static member ExponentialFromByte (z, x, y) : Range<byte> =
        Range.exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    static member ExponentialFromUInt16 (z, x, y) : Range<uint16> =
        Range.exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    static member ExponentialFromUInt32 (z, x, y) : Range<uint32> =
        Range.exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    static member ExponentialFromUInt64 (z, x, y) : Range<uint64> =
        Range.exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    static member ExponentialFromSingle (z, x, y) : Range<single> =
        Range.exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    static member ExponentialFromDouble (z, x, y) : Range<double> =
        Range.exponentialFrom z x y

    /// Construct a range which scales the bounds exponentially relative to the
    /// size parameter.
    static member ExponentialFromDecimal (z, x, y) : Range<decimal> =
        Range.exponentialFrom z x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    static member ExponentialSByte (x, y) : Range<sbyte> =
        Range.exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    static member ExponentialInt16 (x, y) : Range<int16> =
        Range.exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    static member ExponentialInt32 (x, y) : Range<int32> =
        Range.exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    static member ExponentialInt64 (x, y) : Range<int64> =
        Range.exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    static member ExponentialByte (x, y) : Range<byte> =
        Range.exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    static member ExponentialUInt16 (x, y) : Range<uint16> =
        Range.exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    static member ExponentialUInt32 (x, y) : Range<uint32> =
        Range.exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    static member ExponentialUInt64 (x, y) : Range<uint64> =
        Range.exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    static member ExponentialSingle (x, y) : Range<single> =
        Range.exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    static member ExponentialDouble (x, y) : Range<double> =
        Range.exponential x y

    /// Construct a range which scales the second bound exponentially relative
    /// to the size parameter.
    static member ExponentialDecimal (x, y) : Range<decimal> =
        Range.exponential x y

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    static member ExponentialBoundedSByte () : Range<sbyte> =
        Range.exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    static member ExponentialBoundedInt16 () : Range<int16> =
        Range.exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    static member ExponentialBoundedInt32 () : Range<int32> =
        Range.exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    static member ExponentialBoundedInt64 () : Range<int64> =
        Range.exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    static member ExponentialBoundedByte () : Range<byte> =
        Range.exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    static member ExponentialBoundedUInt16 () : Range<uint16> =
        Range.exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    static member ExponentialBoundedUInt32 () : Range<uint32> =
        Range.exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    static member ExponentialBoundedUInt64 () : Range<uint64> =
        Range.exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    static member ExponentialBoundedSingle () : Range<single> =
        Range.exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    static member ExponentialBoundedDouble () : Range<double> =
        Range.exponentialBounded ()

    /// Construct a range which is scaled exponentially relative to the size
    /// parameter and uses the full range of a data type.
    static member ExponentialBoundedDecimal () : Range<decimal> =
        Range.exponentialBounded ()

[<Extension>]
[<AbstractClass; Sealed>]
type RangeExtensions private () =

    [<Extension>]
    static member Select (range : Range<'T>, mapper : Func<'T, 'TResult>) : Range<'TResult> =
        Range.map mapper.Invoke range

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
    [<Extension>]
    static member Origin (range : Range<'T>) : 'T =
        Range.origin range

    /// Get the extents of a range, for a given size.
    [<Extension>]
    static member Bounds (range : Range<'T>, sz : Size) : 'T * 'T =
        Range.bounds sz range

    /// Get the lower bound of a range for the given size.
    [<Extension>]
    static member LowerBound (range : Range<'T>, sz : Size) : 'T =
        Range.lowerBound sz range

    /// Get the upper bound of a range for the given size.
    [<Extension>]
    static member UpperBound (range : Range<'T>, sz : Size) : 'T =
        Range.upperBound sz range

#endif
