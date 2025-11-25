namespace Hedgehog

open Hedgehog.AutoGen

/// <summary>
/// Configuration for automatic generator discovery and registration.
/// </summary>
/// <remarks>
/// This interface provides settings that control how generators are automatically created
/// for custom types. It manages collection size ranges, recursion depth limits, and
/// registered custom generators.
/// </remarks>
type IAutoGenConfig = internal {
  /// <summary>The range for collection sizes. If None, uses the default exponential range 0-50.</summary>
  seqRange: Range<int> option
  /// <summary>The maximum recursion depth. If None, uses the default depth of 1.</summary>
  recursionDepth: int option
  /// <summary>The collection of registered custom generators.</summary>
  generators: GeneratorCollection
}

namespace Hedgehog.FSharp

open System
open System.Reflection
open Hedgehog
open Hedgehog.AutoGen

/// <summary>
/// Functions for configuring automatic generator discovery and registration.
/// </summary>
/// <remarks>
/// This module provides an F#-friendly API for customizing how generators are automatically
/// created for your types. Use these functions to set collection ranges, recursion depth,
/// and register custom generators.
/// </remarks>
module AutoGenConfig =

  let private defaultSeqRange = Range.exponential 0 50
  let private defaultRecursionDepth = 1

  let private mapGenerators f (config: IAutoGenConfig) =
    { config with generators = f config.generators }

  /// <summary>
  /// Gets the collection range from the configuration, or the default if not set.
  /// </summary>
  /// <param name="config">The configuration to query.</param>
  /// <returns>The range for collection sizes (default: exponential 0 to 50).</returns>
  let seqRange (config: IAutoGenConfig) = config.seqRange |> Option.defaultValue defaultSeqRange

  /// <summary>
  /// Sets the range for collection sizes.
  /// </summary>
  /// <param name="range">The range defining minimum and maximum collection sizes.</param>
  /// <param name="config">The configuration to modify.</param>
  /// <returns>The updated configuration.</returns>
  /// <remarks>
  /// This controls how many elements are generated when creating collections like lists, arrays, and sets.
  /// </remarks>
  let setSeqRange (range: Range<int>) (config: IAutoGenConfig) =
    { config with seqRange = Some range }

  /// <summary>
  /// Gets the recursion depth from the configuration, or the default if not set.
  /// </summary>
  /// <param name="config">The configuration to query.</param>
  /// <returns>The maximum recursion depth (default: 1).</returns>
  let recursionDepth (config: IAutoGenConfig) = config.recursionDepth |> Option.defaultValue defaultRecursionDepth

  /// <summary>
  /// Sets the maximum recursion depth for nested type generation.
  /// </summary>
  /// <param name="depth">The maximum recursion depth. A depth of 0 means no recursion is allowed.</param>
  /// <param name="config">The configuration to modify.</param>
  /// <returns>The updated configuration.</returns>
  /// <remarks>
  /// This controls how deeply nested structures can be generated. Use this to prevent infinite
  /// recursion when generating recursive data structures or to control the complexity of generated values.
  /// </remarks>
  let setRecursionDepth (depth: int) (config: IAutoGenConfig) =
    { config with recursionDepth = Some depth }

  /// <summary>
  /// Merges two configurations, with values from the second configuration taking precedence.
  /// </summary>
  /// <param name="baseConfig">The base configuration.</param>
  /// <param name="extraConfig">The configuration whose values take precedence when set.</param>
  /// <returns>The merged configuration.</returns>
  /// <remarks>
  /// This is useful for creating configuration hierarchies where you can have a base configuration
  /// and override specific settings with another configuration.
  /// </remarks>
  let merge (baseConfig: IAutoGenConfig) (extraConfig: IAutoGenConfig) =
    {
       seqRange = extraConfig.seqRange |> Option.orElse baseConfig.seqRange
       recursionDepth = extraConfig.recursionDepth |> Option.orElse baseConfig.recursionDepth
       generators = GeneratorCollection.merge baseConfig.generators extraConfig.generators
    }

  /// <summary>
  /// Adds a custom generator for a specific type to the configuration.
  /// </summary>
  /// <param name="gen">The generator to register.</param>
  /// <returns>A function that takes a configuration and returns the updated configuration with the generator added.</returns>
  /// <remarks>
  /// Use this function to override or provide a generator for a specific type.
  /// This is useful when you want to customize how instances of a particular type are generated.
  /// </remarks>
  let addGenerator (gen: Gen<'a>) =
    let targetType = typeof<'a>
    mapGenerators (GeneratorCollection.addGenerator targetType targetType [||] (fun _ _ -> gen))

  /// <summary>
  /// Registers all generator methods defined in a given type.
  /// </summary>
  /// <typeparam name="a">The type containing static generator methods.</typeparam>
  /// <param name="config">The configuration to modify.</param>
  /// <returns>The updated configuration.</returns>
  /// <remarks>
  /// <para>
  /// The type is expected to have static methods that return <see cref="T:Hedgehog.Gen`1"/>.
  /// These methods can have parameters which will be automatically injected:
  /// </para>
  /// <list type="bullet">
  /// <item><description><see cref="T:Hedgehog.AutoGenContext"/> - provides context information such as recursion depth and collection range</description></item>
  /// <item><description><see cref="T:Hedgehog.IAutoGenConfig"/> - the current auto-generation configuration</description></item>
  /// <item><description><see cref="T:Hedgehog.Gen`1"/> - other generators that have been registered</description></item>
  /// </list>
  /// <para>
  /// This enables dependency injection of generators, allowing you to compose complex generators from simpler ones.
  /// The system will use reflection to discover methods and automatically wire up their dependencies.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // a type containing generators for generic types
  /// // methods should return Gen&lt;_&gt; and are allowed to take Gen&lt;_&gt; and AutoGenContext as parameters
  /// type GenericGenerators =
  ///
  ///   // Generate generic types
  ///   static member MyGenericType&lt;'a&gt;(valueGen : Gen&lt;'a&gt;) : Gen&lt;MyGenericType&lt;'a&gt;&gt; =
  ///      valueGen | Gen.map (fun x -> MyGenericType(x))
  ///
  ///   // Generate generic types with recursion support and access to AutoGenContext
  ///   static member ImmutableList&lt;'a&gt;(context: AutoGenContext, valueGen: Gen&lt;'a&gt;) : Gen&lt;ImmutableList&lt;'a&gt;&gt; =
  ///     if context.CanRecurse then
  ///       valueGen |> Gen.list context.CollectionRange |> Gen.map ImmutableList.CreateRange
  ///     else
  ///       Gen.constant ImmutableList&lt;'a&gt;.Empty
  ///
  /// // register the generic generators in AutoGenConfig
  /// let config =
  ///   GenX.defaults
  ///   |> AutoGenConfig.addGenerators&lt;GenericGenerators&gt;
  /// </code>
  /// </example>
  let addGenerators<'a> (config: IAutoGenConfig) =
      let getGenType (t: Type) =
          if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Gen<_>>
            then Some (t.GetGenericArguments().[0])
            else None

      let getAutogenContextType (t: Type) =
          if t = typeof<AutoGenContext>
            then Some t
            else None

      let tryUnwrapParameters (methodInfo: MethodInfo) : Option<Type[]> =
          methodInfo.GetParameters()
          |> Array.fold (fun acc param ->
              match acc with
              | None -> None
              | Some types ->
                  getGenType param.ParameterType
                    |> Option.orElseWith (fun () -> getAutogenContextType param.ParameterType)
                    |> Option.map (fun t -> Array.append types [| t |])
          ) (Some [||])

      typeof<'a>.GetMethods(BindingFlags.Static ||| BindingFlags.Public)
      |> Seq.choose (fun methodInfo ->
          match getGenType methodInfo.ReturnType, tryUnwrapParameters methodInfo with
          | Some targetType, Some typeArray ->
              let factory: Type[] -> obj[] -> obj = fun types args ->
                  let methodToCall =
                      if Array.isEmpty types then methodInfo
                      else methodInfo.MakeGenericMethod(types)
                  methodToCall.Invoke(null, args)
              Some (targetType, typeArray, factory)
          | _ -> None)
      |> Seq.fold (fun cfg (targetType, typeArray, factory) ->
          cfg |> mapGenerators (GeneratorCollection.addGenerator targetType targetType typeArray factory))
          config


namespace Hedgehog

open Hedgehog.AutoGen
open Hedgehog.FSharp

/// <summary>
/// C#-friendly entry points for automatic generator configuration.
/// </summary>
/// <remarks>
/// This module provides compiled names for F# functions to make them more accessible from C#.
/// </remarks>
module AutoGenConfig =
    /// <summary>
    /// Creates an empty configuration with no custom settings.
    /// </summary>
    /// <returns>An empty configuration that will use all default values.</returns>
    /// <remarks>
    /// Use this as a starting point to build a custom configuration.
    /// </remarks>
    [<CompiledName("Empty")>]
    let empty = {
      seqRange = None
      recursionDepth = None
      generators = GeneratorCollection.empty
    }

    /// <summary>
    /// Creates a default configuration with standard generators for common types.
    /// </summary>
    /// <returns>A configuration pre-populated with generators from <see cref="T:Hedgehog.AutoGen.DefaultGenerators"/>.</returns>
    /// <remarks>
    /// This configuration includes generators for standard collection types such as List, Array, Dictionary,
    /// ImmutableList, ImmutableArray, and other common .NET types. Use this as a base configuration
    /// and add your own custom generators as needed.
    /// </remarks>
    [<CompiledName("Defaults")>]
    let defaults =
      empty |> AutoGenConfig.addGenerators<DefaultGenerators>
