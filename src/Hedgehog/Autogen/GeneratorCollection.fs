namespace Hedgehog.AutoGen

open System
open System.Collections.Concurrent
open System.Collections.Immutable

// A generator factory which can be backed by a generic method.
// It takes an array of genetic type parameters and an array of arguments to create the generator.
type private GeneratorFactory = Type[] -> obj[] -> obj

/// Represents a normalized key for generator lookup that distinguishes between
/// different patterns of generic vs concrete type arguments.
/// For example:
/// - Either<'a, string> and Either<'b, string> have the same key
/// - Either<'a, 'b> and Either<'x, 'y> have the same key
/// - But Either<'a, string> and Either<'a, 'b> have different keys
type internal GeneratorKey = {
  /// The generic type definition (e.g., Either<,>)
  GenericTypeDefinition: Type
  /// For each type argument position, Some(type) if concrete, None if generic parameter
  /// E.g., Either<'a, string> -> [None; Some(string)]
  ConcreteTypes: Type option list
}

type internal GeneratorCollection = {
  // A dictionary of generators.
  // The key distinguishes between different patterns of generic vs concrete type arguments
  // The value is a tuple of:
  // 1. The original reflected type (with generic parameters intact for type resolution)
  // 2. An array types of arguments for the generator factory
  // 3. A generator factory, which can be backed by a generic method,
  //    so it takes an array of genetic type parameters,
  //    and an array of arguments to create the generator.
  Generators: ImmutableDictionary<GeneratorKey, Type * Type[] * GeneratorFactory>
  // The cache memoizes tryFindFor results to avoid repeated reflection calls.
  Cache: ConcurrentDictionary<Type, (Type * (Type[] * GeneratorFactory)) option>
}

module internal GeneratorCollection =

  let empty = {
    Generators = ImmutableDictionary.Empty
    Cache = ConcurrentDictionary<Type, (Type * (Type[] * GeneratorFactory)) option>()
  }

  /// Create a GeneratorKey from a type by identifying which positions are generic vs concrete
  let private createKey (t: Type) : GeneratorKey =
    if t.IsGenericType then
      let concreteTypes =
        t.GetGenericArguments()
        |> Seq.map (fun arg -> if arg.IsGenericParameter then None else Some arg)
        |> List.ofSeq
      { GenericTypeDefinition = t.GetGenericTypeDefinition(); ConcreteTypes = concreteTypes }
    else
      // Non-generic types use themselves as the key
      { GenericTypeDefinition = t; ConcreteTypes = [] }

  let merge (gens1: GeneratorCollection) (gens2: GeneratorCollection) =
    // Create a new cache when merging since the generator set has changed
    {
      Generators = gens1.Generators.SetItems(gens2.Generators)
      Cache = ConcurrentDictionary<Type, (Type * (Type[] * GeneratorFactory)) option>()
    }

  let addGenerator (normalizedType: Type) (originalType: Type) (paramTypes: Type[]) (factory: Type[] -> obj[] -> obj) =
    fun (gc: GeneratorCollection) ->
      let key = createKey normalizedType
      // Reset cache when adding a generator since lookup results may change
      {
        Generators = gc.Generators.SetItem(key, (originalType, paramTypes, factory))
        Cache = ConcurrentDictionary<Type, (Type * (Type[] * GeneratorFactory)) option>()
      }

  /// Count the number of generic parameters in a type
  let private countGenericParameters (t: Type) =
    if t.IsGenericType then t.GetGenericArguments() |> Seq.filter _.IsGenericParameter |> Seq.length
    else 0

  // Find a generator that can satisfy the given required type.
  // It also takes care of finding 'generic' generators (like Either<'a, 'b>)
  // to satisfy specific types (like Either<int, string>).
  // When multiple generators match, returns the most specific one (fewest generic parameters).
  // Returns the original reflected type along with the args and factory.
  // Results are memoized to avoid repeated reflection calls.
  let tryFindFor (targetType: Type) (gc: GeneratorCollection) =
    gc.Cache.GetOrAdd(targetType, fun targetType ->
      let targetKey = createKey targetType
      gc.Generators
      |> Seq.choose (fun (KeyValue (key, (originalType, paramTypes, factory))) ->
          // Only consider generators with the same generic type definition
          if key.GenericTypeDefinition = targetKey.GenericTypeDefinition then
            // Check if the stored type can satisfy the target type
            if originalType |> TypeUtils.satisfies targetType then
              Some (originalType, paramTypes, factory)
            else
              None
          else
            None
      )
      |> Seq.sortBy (fun (originalType, _, _) -> countGenericParameters originalType)
      |> Seq.tryHead
      |> Option.map (fun (originalType, paramTypes, factory) -> (originalType, (paramTypes, factory)))
    )
