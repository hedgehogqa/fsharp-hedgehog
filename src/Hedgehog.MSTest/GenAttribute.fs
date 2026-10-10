namespace Hedgehog.MSTest

open System
open Hedgehog
open Hedgehog.FSharp

/// The non-generic base of `GenAttribute<'a>`.
///
/// The adapter finds the generator of a parameter through this type, so an attribute derived from another
/// generator attribute (for example from `IntAttribute`) is found wherever it sits in the inheritance chain.
[<AbstractClass>]
[<AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)>]
type GenAttribute() =
    inherit Attribute()

    /// Returns the generator of the parameter with its values boxed.
    abstract member Box: unit -> Gen<obj>

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
/// member _.``is always 2`` ([<ConstantInt(2)>] i) =
///   Assert.AreEqual(2, i)
///
/// ```
[<AbstractClass>]
[<AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)>]
type GenAttribute<'a>() =
    inherit GenAttribute()

    abstract member Generator: Gen<'a>
    override this.Box() = this.Generator |> Gen.map box
