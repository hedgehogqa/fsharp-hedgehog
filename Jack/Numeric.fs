namespace Jack

//
// The handful of ad-hoc polymorphic things we need from FsControl.
//

open System

type MinValue =
    static member MinValue (_:unit          , _:MinValue) = ()
    static member MinValue (_:bool          , _:MinValue) = false
    static member MinValue (_:char          , _:MinValue) = Char.MinValue
    static member MinValue (_:byte          , _:MinValue) = Byte.MinValue
    static member MinValue (_:sbyte         , _:MinValue) = SByte.MinValue
    static member MinValue (_:float         , _:MinValue) = Double.MinValue
    static member MinValue (_:int16         , _:MinValue) = Int16.MinValue
    static member MinValue (_:int           , _:MinValue) = Int32.MinValue
    static member MinValue (_:int64         , _:MinValue) = Int64.MinValue
    static member MinValue (_:float32       , _:MinValue) = Single.MinValue
    static member MinValue (_:uint16        , _:MinValue) = UInt16.MinValue
    static member MinValue (_:uint32        , _:MinValue) = UInt32.MinValue
    static member MinValue (_:uint64        , _:MinValue) = UInt64.MinValue
    static member MinValue (_:decimal       , _:MinValue) = Decimal.MinValue
    static member MinValue (_:DateTime      , _:MinValue) = DateTime.MinValue
    static member MinValue (_:DateTimeOffset, _:MinValue) = DateTimeOffset.MinValue
    static member MinValue (_:TimeSpan      , _:MinValue) = TimeSpan.MinValue

    static member inline Invoke() =
        let inline call_2 (a:^a, b:^b) = ((^a or ^b) : (static member MinValue: _*_ -> _) b, a)
        let inline call (a:'a) = call_2 (a, Unchecked.defaultof<'r>) :'r
        call Unchecked.defaultof<MinValue>

    static member inline MinValue ((_:'a*'b                  ), _:MinValue) = (MinValue.Invoke(), MinValue.Invoke())
    static member inline MinValue ((_:'a*'b*'c               ), _:MinValue) = (MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke())
    static member inline MinValue ((_:'a*'b*'c*'d            ), _:MinValue) = (MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke())
    static member inline MinValue ((_:'a*'b*'c*'d*'e         ), _:MinValue) = (MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke())
    static member inline MinValue ((_:'a*'b*'c*'d*'e*'f      ), _:MinValue) = (MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke())
    static member inline MinValue ((_:'a*'b*'c*'d*'e*'f*'g   ), _:MinValue) = (MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke())
    static member inline MinValue ((_:'a*'b*'c*'d*'e*'f*'g*'h), _:MinValue) = (MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke(), MinValue.Invoke())

type MaxValue =
    static member MaxValue (_:unit          , _:MaxValue) = ()
    static member MaxValue (_:bool          , _:MaxValue) = true
    static member MaxValue (_:char          , _:MaxValue) = Char.MaxValue
    static member MaxValue (_:byte          , _:MaxValue) = Byte.MaxValue
    static member MaxValue (_:sbyte         , _:MaxValue) = SByte.MaxValue
    static member MaxValue (_:float         , _:MaxValue) = Double.MaxValue
    static member MaxValue (_:int16         , _:MaxValue) = Int16.MaxValue
    static member MaxValue (_:int           , _:MaxValue) = Int32.MaxValue
    static member MaxValue (_:int64         , _:MaxValue) = Int64.MaxValue
    static member MaxValue (_:float32       , _:MaxValue) = Single.MaxValue
    static member MaxValue (_:uint16        , _:MaxValue) = UInt16.MaxValue
    static member MaxValue (_:uint32        , _:MaxValue) = UInt32.MaxValue
    static member MaxValue (_:uint64        , _:MaxValue) = UInt64.MaxValue
    static member MaxValue (_:decimal       , _:MaxValue) = Decimal.MaxValue
    static member MaxValue (_:DateTime      , _:MaxValue) = DateTime.MaxValue
    static member MaxValue (_:DateTimeOffset, _:MaxValue) = DateTimeOffset.MaxValue
    static member MaxValue (_:TimeSpan      , _:MaxValue) = TimeSpan.MaxValue

    static member inline Invoke() =
        let inline call_2 (a:^a, b:^b) = ((^a or ^b) : (static member MaxValue: _*_ -> _) b, a)
        let inline call (a:'a) = call_2 (a, Unchecked.defaultof<'r>) :'r
        call Unchecked.defaultof<MaxValue>

    static member inline MaxValue ((_:'a*'b                  ), _:MaxValue) = (MaxValue.Invoke(), MaxValue.Invoke())
    static member inline MaxValue ((_:'a*'b*'c               ), _:MaxValue) = (MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke())
    static member inline MaxValue ((_:'a*'b*'c*'d            ), _:MaxValue) = (MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke())
    static member inline MaxValue ((_:'a*'b*'c*'d*'e         ), _:MaxValue) = (MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke())
    static member inline MaxValue ((_:'a*'b*'c*'d*'e*'f      ), _:MaxValue) = (MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke())
    static member inline MaxValue ((_:'a*'b*'c*'d*'e*'f*'g   ), _:MaxValue) = (MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke())
    static member inline MaxValue ((_:'a*'b*'c*'d*'e*'f*'g*'h), _:MaxValue) = (MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke(), MaxValue.Invoke())

type FromBigInt =
    static member        FromBigInt (_:int32     , _:FromBigInt) = fun (x:bigint) -> int             x
    static member        FromBigInt (_:int64     , _:FromBigInt) = fun (x:bigint) -> int64           x
    static member        FromBigInt (_:nativeint , _:FromBigInt) = fun (x:bigint) -> nativeint  (int x)
    static member        FromBigInt (_:unativeint, _:FromBigInt) = fun (x:bigint) -> unativeint (int x)
    static member        FromBigInt (_:bigint    , _:FromBigInt) = fun (x:bigint) ->                 x
    static member        FromBigInt (_:float     , _:FromBigInt) = fun (x:bigint) -> float           x
    static member        FromBigInt (_:sbyte     , _:FromBigInt) = fun (x:bigint) -> sbyte      (int x)
    static member        FromBigInt (_:int16     , _:FromBigInt) = fun (x:bigint) -> int16      (int x)
    static member        FromBigInt (_:byte      , _:FromBigInt) = fun (x:bigint) -> byte       (int x)
    static member        FromBigInt (_:uint16    , _:FromBigInt) = fun (x:bigint) -> uint16     (int x)
    static member        FromBigInt (_:uint32    , _:FromBigInt) = fun (x:bigint) -> uint32     (int x)
    static member        FromBigInt (_:uint64    , _:FromBigInt) = fun (x:bigint) -> uint64     (int64 x)
    static member        FromBigInt (_:float32   , _:FromBigInt) = fun (x:bigint) -> float32    (int x)
    static member        FromBigInt (_:decimal   , _:FromBigInt) = fun (x:bigint) -> decimal    (int x)

    static member inline Invoke (x:bigint)   :'Num    =
        let inline call_2 (a:^a, b:^b) = ((^a or ^b) : (static member FromBigInt: _*_ -> _) b, a)
        let inline call (a:'a) = fun (x:'x) -> call_2 (a, Unchecked.defaultof<'r>) x :'r
        call Unchecked.defaultof<FromBigInt> x

type ToBigInt =
    static member        ToBigInt (x:sbyte     ) = bigint (int x)
    static member        ToBigInt (x:int16     ) = bigint (int x)
    static member        ToBigInt (x:int32     ) = bigint      x
    static member        ToBigInt (x:int64     ) = bigint      x
    static member        ToBigInt (x:nativeint ) = bigint (int x)
    static member        ToBigInt (x:byte      ) = bigint (int x)
    static member        ToBigInt (x:uint16    ) = bigint (int x)
    static member        ToBigInt (x:unativeint) = bigint (int x)
    static member        ToBigInt (x:bigint    ) =             x
    static member        ToBigInt (x:uint32    ) = bigint (int x)
    static member        ToBigInt (x:uint64    ) = bigint (int64 x)

    static member inline Invoke    (x:'Integral) :bigint =
        let inline call_2 (a:^a, b:^b) = ((^a or ^b) : (static member ToBigInt: _ -> _) b)
        call_2 (Unchecked.defaultof<ToBigInt>, x)

module Numeric =
    /// Returns the smallest possible value.
    let inline minValue () : ^a =
        MinValue.Invoke ()

    /// Returns the largest possible value.
    let inline maxValue () : ^a =
        MaxValue.Invoke ()

    /// Converts from a BigInt to the inferred destination type.
    let inline fromBigInt (x : bigint) : ^a =
        FromBigInt.Invoke x

    /// Converts to a BigInt.
    let inline toBigInt (x : ^a) : bigint =
        ToBigInt.Invoke x
