namespace Hedgehog.Linq

open System.Runtime.CompilerServices
open Hedgehog
open Hedgehog.FSharp

/// Extension methods for IAutoGenConfig to support fluent API for C#.
[<AbstractClass; Sealed;>]
type AutoGenExtensions() =

  /// Registers a generator.
  [<Extension>]
  static member AddGenerator(self : IAutoGenConfig, generator : Gen<'T>) =
      self |> AutoGenConfig.addGenerator generator

  /// <summary>
  /// Registers generators that are defined in a given type.
  /// The type is expected to have static methods returning Gen&lt;T&gt;.
  /// These methods can take parameters:
  /// <list type="bullet">
  /// <item><description><see cref="IAutoGenConfig"/>  - the current configuration</description></item>
  /// <item><description>Gen&lt;U&gt; - other generators that have been registered</description></item>
  /// </list>
  /// </summary>
  /// <example>
  /// <code>
  /// public sealed class MyGenerators
  ///  {
  ///      public static Gen&lt;ImmutableList&lt;T&gt;&gt; ImmutableListGen&lt;T&gt;(
  ///          AutoGenContext ctx, // &lt;-- this is new, some context can be injected
  ///          Gen&lt;T&gt; valueGen // &lt;-- value generator is injected
  ///      ) {
  ///          if (ctx.CanRecurse)
  ///              // if recursion is possible then construct a list of values
  ///              // respecting the configured collection range
  ///              return valueGen.List(ctx.CollectionRange).Select(ImmutableList.CreateRange);
  ///          else
  ///              // cannot recurse anymore, return the base case
  ///              return ImmutableList.Empty&lt;T&gt;();
  ///      }
  ///  }
  /// </code>
  /// </example>
  [<Extension>]
  static member AddGenerators<'T>(self : IAutoGenConfig) =
    self |> AutoGenConfig.addGenerators<'T>

  [<Extension>]
  static member SetCollectionRange(self : IAutoGenConfig, range : Range<int>) =
    self |> AutoGenConfig.setSeqRange range

  [<Extension>]
  static member SetRecursionDepth(self : IAutoGenConfig, depth: int) =
    self |> AutoGenConfig.setRecursionDepth depth

  [<Extension>]
  static member GetCollectionRange(self : IAutoGenConfig) =
    AutoGenConfig.seqRange self

  static member GetRecursionDepth(self : IAutoGenConfig)=
    AutoGenConfig.recursionDepth self
