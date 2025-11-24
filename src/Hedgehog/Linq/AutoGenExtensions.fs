namespace Hedgehog.Linq

open System.Runtime.CompilerServices
open Hedgehog
open Hedgehog.FSharp

/// <summary>
/// Extension methods for <see cref="T:Hedgehog.IAutoGenConfig"/> to support fluent API for C#.
/// </summary>
/// <remarks>
/// This class provides a C#-friendly interface for configuring automatic generator discovery and registration.
/// Use these methods to customize how generators are created for your custom types when using Auto.Gen&lt;T&gt;().
/// </remarks>
[<AbstractClass; Sealed;>]
type AutoGenExtensions() =

  /// <summary>
  /// Registers a custom generator for type T.
  /// </summary>
  /// <param name="self">The configuration to modify.</param>
  /// <param name="generator">The generator to register for type T.</param>
  /// <returns>The updated configuration.</returns>
  /// <remarks>
  /// Use this method to override or provide a generator for a specific type.
  /// This is useful when you want to customize how instances of a particular type are generated.
  /// </remarks>
  [<Extension>]
  static member AddGenerator(self : IAutoGenConfig, generator : Gen<'T>) =
      self |> AutoGenConfig.addGenerator generator

  /// <summary>
  /// Registers all generator methods defined in a given type.
  /// </summary>
  /// <typeparam name="T">The type containing static generator methods.</typeparam>
  /// <param name="self">The configuration to modify.</param>
  /// <returns>The updated configuration.</returns>
  /// <remarks>
  /// <para>
  /// The type is expected to have static methods returning Gen&lt;T&gt;.
  /// These methods can take parameters that will be automatically injected:
  /// </para>
  /// <list type="bullet">
  /// <item><description><see cref="T:Hedgehog.AutoGenContext"/> - provides context information such as recursion depth and collection range</description></item>
  /// <item><description><see cref="T:Hedgehog.IAutoGenConfig"/> - the current auto-generation configuration</description></item>
  /// <item><description><see cref="T:Hedgehog.Gen&lt;U&gt;"/> - other generators that have been registered for type <c>U</c></description></item>
  /// </list>
  /// <para>
  /// This enables dependency injection of generators, allowing you to compose complex generators from simpler ones.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// public sealed class MyGenerators
  ///  {
  ///      public static Gen&lt;ImmutableList&lt;T&gt;&gt; ImmutableListGen&lt;T&gt;(
  ///          AutoGenContext ctx, // context can be injected
  ///          Gen&lt;T&gt; valueGen // value generator is injected
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

  /// <summary>
  /// Sets the range for collection sizes when generating collections (lists, arrays, sets, etc.).
  /// </summary>
  /// <param name="self">The configuration to modify.</param>
  /// <param name="range">The range defining minimum and maximum collection sizes.</param>
  /// <returns>The updated configuration.</returns>
  /// <remarks>
  /// This setting controls how many elements are generated when creating collections.
  /// The default range is exponential from 0 to 50. Use this to generate smaller or larger collections as needed.
  /// </remarks>
  [<Extension>]
  static member SetCollectionRange(self : IAutoGenConfig, range : Range<int>) =
    self |> AutoGenConfig.setSeqRange range

  /// <summary>
  /// Sets the maximum recursion depth for nested type generation.
  /// </summary>
  /// <param name="self">The configuration to modify.</param>
  /// <param name="depth">The maximum recursion depth. Default is 1.</param>
  /// <returns>The updated configuration.</returns>
  /// <remarks>
  /// <para>
  /// This controls how deeply nested structures can be generated. A depth of 0 means no recursion is allowed.
  /// A depth of 1 allows one level of recursion, and so on.
  /// </para>
  /// <para>
  /// Use this to prevent infinite recursion when generating recursive data structures or to control
  /// the complexity of generated values. Higher values create more complex nested structures.
  /// </para>
  /// </remarks>
  [<Extension>]
  static member SetRecursionDepth(self : IAutoGenConfig, depth: int) =
    self |> AutoGenConfig.setRecursionDepth depth

  /// <summary>
  /// Gets the currently configured collection range.
  /// </summary>
  /// <param name="self">The configuration to query.</param>
  /// <returns>The range defining minimum and maximum collection sizes.</returns>
  [<Extension>]
  static member GetCollectionRange(self : IAutoGenConfig) =
    AutoGenConfig.seqRange self

  /// <summary>
  /// Gets the currently configured recursion depth.
  /// </summary>
  /// <param name="self">The configuration to query.</param>
  /// <returns>The maximum recursion depth.</returns>
  static member GetRecursionDepth(self : IAutoGenConfig)=
    AutoGenConfig.recursionDepth self
