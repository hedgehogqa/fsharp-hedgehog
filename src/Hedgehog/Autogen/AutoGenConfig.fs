namespace Hedgehog

open Hedgehog.AutoGen

type IAutoGenConfig = internal {
  seqRange: Range<int> option
  recursionDepth: int option
  generators: GeneratorCollection
}

namespace Hedgehog.FSharp

open System
open System.Reflection
open Hedgehog
open Hedgehog.AutoGen

module AutoGenConfig =

  let private defaultSeqRange = Range.exponential 0 50
  let private defaultRecursionDepth = 1

  let private mapGenerators f (config: IAutoGenConfig) =
    { config with generators = f config.generators }

  let seqRange (config: IAutoGenConfig) = config.seqRange |> Option.defaultValue defaultSeqRange
  let setSeqRange (range: Range<int>) (config: IAutoGenConfig) =
    { config with seqRange = Some range }

  let recursionDepth (config: IAutoGenConfig) = config.recursionDepth |> Option.defaultValue defaultRecursionDepth
  let setRecursionDepth (depth: int) (config: IAutoGenConfig) =
    { config with recursionDepth = Some depth }

  /// Merge two configurations.
  /// Values from the second configuration take precedence when they are set.
  let merge (baseConfig: IAutoGenConfig) (extraConfig: IAutoGenConfig) =
    {
       seqRange = extraConfig.seqRange |> Option.orElse baseConfig.seqRange
       recursionDepth = extraConfig.recursionDepth |> Option.orElse baseConfig.recursionDepth
       generators = GeneratorCollection.merge baseConfig.generators extraConfig.generators
    }

  /// Add a generator to the configuration.
  let addGenerator (gen: Gen<'a>) =
    let targetType = typeof<'a>
    mapGenerators (GeneratorCollection.addGenerator targetType targetType [||] (fun _ _ -> gen))

  /// Add generators from a given type.
  /// The type is expected to have static methods that return Gen<_>.
  /// These methods can have parameters which are required to be of type Gen<_>.
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

module AutoGenConfig =
    [<CompiledName("Empty")>]
    let empty = {
      seqRange = None
      recursionDepth = None
      generators = GeneratorCollection.empty
    }

    [<CompiledName("Defaults")>]
    let defaults =
      empty |> AutoGenConfig.addGenerators<DefaultGenerators>
