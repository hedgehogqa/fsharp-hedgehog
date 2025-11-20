namespace Hedgehog.AutoGen

open System
open TypeShape.Core

module internal AutoGenHelpers =

  let addGenMsg = "You can use 'AutoGenConfig.defaults |> AutoGenConfig.addGenerator myGen |> Gen.autoWith' to generate types not inherently supported by Gen.auto."

  let unsupportedTypeException<'a> () =
    NotSupportedException (sprintf "Unable to auto-generate %s. %s" typeof<'a>.FullName addGenMsg)

  let resolveGenericTypeArgs (registeredType: Type) (typeArgs: GenericArgument array) (args: Type array) =
    // If the type is generic, we need to find the actual types to use.
    // We match generic parameters by their GenericParameterPosition property,
    // which tells us their position in the method's generic parameter declaration.

    // The registeredType contains the method's generic parameters as they appear in the return type.
    // For example:
    // - Id<'a> has 'a at position 0 in the type
    // - Or<'A, 'A> has 'A at positions 0 and 1 in the type (but GenericParameterPosition=0 for both)
    // - Foo<'A, 'A, 'B, 'C> has 'A at 0,1 (GenericParameterPosition=0), 'B at 2 (GenericParameterPosition=1), 'C at 3 (GenericParameterPosition=2)

    let registeredGenArgs =
      if registeredType.IsGenericType
      then registeredType.GetGenericArguments()
      else Array.empty

    // Build a mapping from method generic parameter position to concrete type
    // by finding where each method parameter first appears in the registered type
    let methodGenParamCount =
      registeredGenArgs
      |> Array.filter _.IsGenericParameter
      |> Array.map _.GenericParameterPosition
      |> Array.distinct
      |> Array.length

    let genericTypes = Array.zeroCreate methodGenParamCount

    // For each position in registeredType, if it's a generic parameter,
    // map it to the corresponding concrete type from typeArgs
    for i = 0 to registeredGenArgs.Length - 1 do
      let regArg = registeredGenArgs[i]
      if regArg.IsGenericParameter then
        let paramPosition = regArg.GenericParameterPosition
        // Only set it if we haven't seen this parameter position before (use first occurrence)
        if genericTypes[paramPosition] = null
        then genericTypes[paramPosition] <- box typeArgs[i].argType

    let genericTypes = genericTypes |> Array.map unbox<Type>

    // Build argumentTypes: substitute generic parameters with concrete types
    let argTypes =
      args
      |> Array.map (fun arg ->
        if arg.IsGenericParameter then
          // Find where this parameter first appears in the registered type
          let paramPosition = arg.GenericParameterPosition
          let firstOccurrenceIndex =
            registeredGenArgs
            |> Array.findIndex (fun t -> t.IsGenericParameter && t.GenericParameterPosition = paramPosition)
          typeArgs[firstOccurrenceIndex].argType
        else arg)

    {| genericTypes = genericTypes; argumentTypes = argTypes |}

  let prepareFactoryArgTypes (typeShape: TypeShape<'a>) (registeredType: Type) (args: Type array) =
    match typeShape with
    | GenericShape (_, typeArgs) ->
      resolveGenericTypeArgs registeredType typeArgs args
    | _ -> {| genericTypes = Array.empty; argumentTypes = args |}
