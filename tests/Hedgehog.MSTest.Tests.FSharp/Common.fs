// The configurations and generator attributes that the tests share, as in the xUnit and NUnit suites.

namespace Hedgehog.MSTest.Tests

open System
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.MSTest

/// An AutoGenConfig type whose every int is 13.
type Int13 =
    static member __ = AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)

/// An AutoGenConfig type whose every int is 2718.
type Int2718 =
    static member __ = AutoGenConfig.empty |> AutoGenConfig.addGenerator (Gen.constant 2718)

/// An AutoGenConfig type whose every int is 13 and every string is "A".
type Int13A =
    static member __ =
        AutoGenConfig.empty
        |> AutoGenConfig.addGenerator (Gen.constant 13)
        |> AutoGenConfig.addGenerator (Gen.constant "A")

/// Sets an int parameter to 5.
type Int5Attribute() =
    inherit GenAttribute<int>()
    override _.Generator = Gen.constant 5

/// Sets an int parameter to 6.
type Int6Attribute() =
    inherit GenAttribute<int>()
    override _.Generator = Gen.constant 6

/// Generates an int parameter from a constant range.
type IntConstantRangeAttribute(min: int, max: int) =
    inherit GenAttribute<int>()
    override _.Generator = Range.constant min max |> Gen.int32

/// A PropertyAttribute subclass that names Int13 itself.
type PropertyInt13Attribute() =
    inherit PropertyAttribute(typeof<Int13>)

/// A PropertiesAttribute subclass that names Int13 itself.
type PropertiesInt13Attribute() =
    inherit PropertiesAttribute(typeof<Int13>)
