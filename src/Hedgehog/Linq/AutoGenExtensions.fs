namespace Hedgehog.Linq

open System.Runtime.CompilerServices
open Hedgehog

[<AbstractClass; Sealed;>]
type AutoGenExtensions() =

  [<Extension>]
  static member AddGenerator(self : AutoGenConfig, generator : Gen<'T>) =
      self |> AutoGenConfig.addGenerator generator

  /// Add generators from a given class.
  /// The type is expected to have static methods that return Gen<T>.
  /// These methods can have parameters which are required to be of type Gen<T>.
  [<Extension>]
  static member AddGenerators<'T>(self : AutoGenConfig) =
    self |> AutoGenConfig.addGenerators<'T>

  [<Extension>]
  static member SetCollectionRange(self : AutoGenConfig, range : Range<int>) =
    self |> AutoGenConfig.setSeqRange range

  [<Extension>]
  static member SetRecursionDepth(self : AutoGenConfig, depth: int) =
    self |> AutoGenConfig.setRecursionDepth depth

  [<Extension>]
  static member GetCollectionRange(self : AutoGenConfig) =
    AutoGenConfig.seqRange self

  static member GetRecursionDepth(self : AutoGenConfig)=
    AutoGenConfig.recursionDepth self
