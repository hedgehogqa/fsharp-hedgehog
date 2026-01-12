namespace Hedgehog.NUnit

open System
open Hedgehog
open Hedgehog.FSharp

/// Set a Generator for a parameter of a test annotated with `Property`
///
/// Example usage:
///
/// ```
///
/// type ConstantInt(i: int) =
///   inherit GenAttribute<int>()
///   override _.Generator = Gen.constant i
///
/// [<Property>]
/// let ``is always 2`` ([<ConstantInt(2)>] i) =
///   Assert.AreEqual(2, i)
///
/// ```
[<AbstractClass>]
[<AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)>]
type GenAttribute<'a>() =
    inherit Attribute()

    abstract member Generator: Gen<'a>
    member this.Box() = this.Generator |> Gen.map box
