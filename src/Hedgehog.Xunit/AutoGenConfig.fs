namespace Hedgehog.Xunit

open System
open Hedgehog

module internal AutoGenConfig =
  let instantiate (configType: Type) (configArgs: obj array) =
    let configArgs = configArgs |> Option.ofObj |> Option.defaultValue [||]

    configType.GetMethods()
    |> Seq.filter (fun p -> p.IsStatic && p.ReturnType = typeof<IAutoGenConfig>)
    |> Seq.seqTryExactlyOne
    |> Option.requireSome $"%s{configType.FullName} must have exactly one public static property that returns an AutoGenConfig.

An example type definition:

type %s{configType.Name} =
  static member __ =
    AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)
"
    |> fun methodInfo ->
        let methodInfo =
          if methodInfo.IsGenericMethod then
            methodInfo.GetParameters()
            |> Array.map (_.ParameterType.IsGenericParameter)
            |> Array.zip configArgs
            |> Array.filter snd
            |> Array.map (fun (arg, _) -> arg.GetType())
            |> fun argTypes -> methodInfo.MakeGenericMethod argTypes
          else
            methodInfo

        methodInfo.Invoke(null, configArgs) :?> IAutoGenConfig
