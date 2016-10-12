namespace Jack

//
// The handful of ad-hoc polymorphic things we need from FsControl.
//

open FsControl

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
