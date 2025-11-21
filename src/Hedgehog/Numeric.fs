namespace Hedgehog

// The handful of ad-hoc polymorphic things we need from FsControl, copied from
// https://github.com/gmpl/FsControl/blob/f125b96d252080f722a1ff7a9efcfaf53420a339/FsControl.Core/Numeric.fs

open System

[<Sealed; AbstractClass>]
type MinValue =

    static member MinValue (_: unit, _: MinValue) : unit =
        ()

    static member MinValue (_: bool, _: MinValue) : bool =
        false

    static member MinValue (_: char, _: MinValue) : char =
        Char.MinValue

    static member MinValue (_: uint8, _: MinValue) : uint8 =
        Byte.MinValue

    static member MinValue (_: uint16, _: MinValue) : uint16 =
        UInt16.MinValue

    static member MinValue (_: uint32, _: MinValue) : uint32 =
        UInt32.MinValue

    static member MinValue (_: uint64, _: MinValue) : uint64 =
        UInt64.MinValue

    static member MinValue (_: int8, _: MinValue) : int8 =
        SByte.MinValue

    static member MinValue (_: int16, _: MinValue) : int16 =
        Int16.MinValue

    static member MinValue (_: int32, _: MinValue) : int32 =
        Int32.MinValue

    static member MinValue (_: int64, _: MinValue) : int64 =
        Int64.MinValue

    static member MinValue (_: single, _: MinValue) : single =
        Single.MinValue

    static member MinValue (_: double, _: MinValue) : double =
        Double.MinValue

    static member MinValue (_: decimal, _: MinValue) : decimal =
        Decimal.MinValue

    static member MinValue (_: DateTime, _: MinValue) : DateTime =
        DateTime.MinValue

    static member MinValue (_: DateTimeOffset, _: MinValue) : DateTimeOffset =
        DateTimeOffset.MinValue

#if !FABLE_COMPILER
    static member MinValue (_: TimeSpan, _: MinValue) : TimeSpan =
        TimeSpan.MinValue
#endif

    static member inline Invoke () =
        let inline call2 (a: ^a, b: ^b) =
            ((^a or ^b): (static member MinValue: _ * _ -> _) b, a)
        let inline call1 (a: 'a) =
            call2 (a, Unchecked.defaultof<'r>) : 'r
        call1 Unchecked.defaultof<MinValue>

    static member inline MinValue ((_: 'a * 'b), _: MinValue) =
        (MinValue.Invoke(), MinValue.Invoke())

    static member inline MinValue ((_: 'a * 'b * 'c), _: MinValue) =
        (MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke())

    static member inline MinValue ((_: 'a * 'b * 'c * 'd), _: MinValue) =
        (MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke())

    static member inline MinValue ((_: 'a * 'b * 'c * 'd * 'e), _: MinValue) =
        (MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke())

    static member inline MinValue ((_: 'a * 'b * 'c * 'd * 'e * 'f), _: MinValue) =
        (MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke())

    static member inline MinValue ((_: 'a * 'b * 'c * 'd * 'e * 'f * 'g), _: MinValue) =
        (MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke())

    static member inline MinValue ((_: 'a * 'b * 'c * 'd * 'e * 'f * 'g * 'h), _: MinValue) =
        (MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke())

[<Sealed; AbstractClass>]
type MaxValue =

    static member MaxValue (_: unit, _: MaxValue) : unit =
        ()

    static member MaxValue (_: bool, _: MaxValue) : bool =
        true

    static member MaxValue (_: char, _:  MaxValue) : char =
        Char.MaxValue

    static member MaxValue (_: uint8, _: MaxValue) : uint8 =
        Byte.MaxValue

    static member MaxValue (_: uint16, _: MaxValue) : uint16 =
        UInt16.MaxValue

    static member MaxValue (_: uint32, _: MaxValue) : uint32 =
        UInt32.MaxValue

    static member MaxValue (_: uint64, _: MaxValue) : uint64 =
        UInt64.MaxValue

    static member MaxValue (_: int8, _: MaxValue) : int8 =
        SByte.MaxValue

    static member MaxValue (_: int16, _: MaxValue) : int16 =
        Int16.MaxValue

    static member MaxValue (_: int32, _: MaxValue) : int32 =
        Int32.MaxValue

    static member MaxValue (_: int64, _: MaxValue) : int64 =
        Int64.MaxValue

    static member MaxValue (_: single, _: MaxValue) : single =
        Single.MaxValue

    static member MaxValue (_: double, _: MaxValue) : double =
        Double.MaxValue

    static member MaxValue (_: decimal, _: MaxValue) : decimal =
        Decimal.MaxValue

    static member MaxValue (_: DateTime, _: MaxValue) : DateTime =
        DateTime.MaxValue

    static member MaxValue (_: DateTimeOffset, _: MaxValue) : DateTimeOffset =
        DateTimeOffset.MaxValue

#if !FABLE_COMPILER
    static member MaxValue (_: TimeSpan, _: MaxValue) : TimeSpan =
        TimeSpan.MaxValue
#endif

    static member inline Invoke () =
        let inline call2 (a: ^a, b: ^b) =
            ((^a or ^b): (static member MaxValue: _ * _ -> _) b, a)
        let inline call1 (a: 'a) =
            call2 (a, Unchecked.defaultof<'r>) : 'r
        call1 Unchecked.defaultof<MaxValue>

    static member inline MaxValue ((_: 'a * 'b), _: MaxValue) =
        (MaxValue.Invoke(), MaxValue.Invoke())

    static member inline MaxValue ((_: 'a  *'b *'c), _: MaxValue) =
        (MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke())

    static member inline MaxValue ((_: 'a *'b *'c *'d), _: MaxValue) =
        (MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke())

    static member inline MaxValue ((_: 'a * 'b * 'c * 'd * 'e), _: MaxValue) =
        (MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke())

    static member inline MaxValue ((_: 'a * 'b * 'c * 'd *'e *'f), _: MaxValue) =
        (MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke())

    static member inline MaxValue ((_: 'a * 'b * 'c * 'd * 'e * 'f *'g), _: MaxValue) =
        (MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke())

    static member inline MaxValue ((_: 'a * 'b * 'c * ' d *'e * 'f *'g *'h), _: MaxValue) =
        (MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke())

type FromBigInt =

    static member FromBigInt (_: bigint, _: FromBigInt) : bigint -> bigint =
        id

    static member FromBigInt (_: uint8, _: FromBigInt) : bigint -> uint8 =
        int32 >> uint8

    static member FromBigInt (_: uint16, _: FromBigInt) : bigint -> uint16 =
        int32 >> uint16

    static member FromBigInt (_: uint32, _: FromBigInt) : bigint -> uint32 =
        uint32

    static member FromBigInt (_: uint64, _: FromBigInt) : bigint -> uint64 =
        uint64

    static member FromBigInt (_: int8, _: FromBigInt) : bigint -> int8 =
        int32 >> int8

    static member FromBigInt (_: int16, _: FromBigInt) : bigint -> int16 =
        int32 >> int16

    static member FromBigInt (_: int32, _: FromBigInt) : bigint -> int32 =
        int32

    static member FromBigInt (_: int64, _: FromBigInt) : bigint -> int64 =
        int64

    static member FromBigInt (_: single, _: FromBigInt) : bigint -> single =
        single

    static member FromBigInt (_: double, _: FromBigInt) : bigint -> double =
        double

    static member FromBigInt (_: decimal, _: FromBigInt) : bigint -> decimal =
#if !FABLE_COMPILER
        decimal
#else
        // This is a workaround for a [bug in Fable](https://github.com/fable-compiler/Fable/issues/3500)
        // Once this issue is fixed we can remove this and use just `decimal`
        fun (x: bigint) ->
            if x.Sign > 0 then
                decimal x
            else
                let negValue = -x
                let asDecimal = decimal negValue
                let result = -asDecimal
                result
#endif

#if !FABLE_COMPILER
    static member FromBigInt (_: nativeint , _: FromBigInt) : bigint -> nativeint =
        int32 >> nativeint

    static member FromBigInt (_: unativeint, _: FromBigInt) : bigint -> unativeint =
        int32 >> unativeint
#endif

    static member inline Invoke (x: bigint) : 'Num =
        let inline call2 (a: ^a, b: ^b) =
            ((^a or ^b): (static member FromBigInt: _ * _ -> _) b, a)
        let inline call1 (a: 'a) =
            fun (x: 'x) -> call2 (a, Unchecked.defaultof<'r>) x : 'r
        call1 Unchecked.defaultof<FromBigInt> x

type ToBigInt =

    static member ToBigInt (x: bigint) : bigint =
        x

    static member ToBigInt (x: uint8) : bigint =
        bigint (int32 x)

    static member ToBigInt (x: uint16) : bigint =
        bigint (int32 x)

    static member ToBigInt (x: uint32) : bigint =
        bigint x

    static member ToBigInt (x: uint64) : bigint =
        bigint x

    static member ToBigInt (x: int8) : bigint =
        bigint (int32 x)

    static member ToBigInt (x: int16) : bigint =
        bigint (int32 x)

    static member ToBigInt (x: int32) : bigint =
        bigint x

    static member ToBigInt (x: int64) : bigint =
        bigint x

    static member ToBigInt (x: single) : bigint =
        bigint x

    static member ToBigInt (x: double) : bigint =
        bigint x

    static member ToBigInt (x: decimal) : bigint =
        bigint x

#if !FABLE_COMPILER
    static member ToBigInt (x : nativeint) : bigint =
        bigint (int32 x)

    static member ToBigInt (x : unativeint) : bigint =
        bigint (int32 x)
#endif

    static member inline Invoke (x: 'Integral) : bigint =
        let inline call (a: ^a, b: ^b) : ^r =
            ((^a or ^b): (static member ToBigInt: _ -> _) b)
        call (Unchecked.defaultof<ToBigInt>, x)

module Numeric =

    /// Returns the smallest possible value.
    let inline minValue () : ^a =
        MinValue.Invoke ()

    /// Returns the largest possible value.
    let inline maxValue () : ^a =
        MaxValue.Invoke ()

    /// Converts from a BigInt to the inferred destination type.
    let inline fromBigInt (x: bigint) : ^a =
        FromBigInt.Invoke x

    /// Converts to a BigInt.
    let inline toBigInt (x: ^a) : bigint =
        ToBigInt.Invoke x
