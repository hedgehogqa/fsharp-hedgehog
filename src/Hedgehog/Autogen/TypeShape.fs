namespace Hedgehog.AutoGen

open System
open TypeShape.Core

[<AutoOpen>]
module internal TypeShape =

  type GenericArgument = {
    argType: Type
    argTypeDefinition: Type
  }

  let (|GenericShape|_|) (s: TypeShape) =
    match s.ShapeInfo with
    | Generic (td, ta) ->
      td.GetGenericArguments()
      |> Seq.zip ta
      |> Seq.map (fun (t, d) -> { argType = t; argTypeDefinition = d })
      |> Seq.toArray
      |> fun args -> Some (td, args)
    | _ -> None
