module Hedgehog.Autogen.Tests.AutoGenContextTests

open Hedgehog
open Xunit
open Swensen.Unquote

type RecursiveType<'a> =
    { Value: Option<RecursiveType<'a>>}
    member this.Depth =
        match this.Value with
        | None -> 0
        | Some x -> x.Depth + 1

type RecursiveGenerators =
    // override Option to always generate Some when recursion is allowed
    // using the AutoGenContext to assert recursion context preservation
    static member Option<'a>(context: AutoGenContext) =
        if context.CanRecurse then
            printfn $"CurrentRecursionDepth: %d{context.CurrentRecursionDepth}"
            context.AutoGenerate<'a>() |> Gen.map Some
        else
            Gen.constant None

[<Fact>]
let ``Should preserve recursion with generic types when using AutoGenContext.AutoGenerate``() =
    property {
        let! recDepth = Gen.int32 (Range.constant 2 5)
        let config =
           AutoGenConfig.defaults
           |> AutoGenConfig.addGenerators<RecursiveGenerators>
           |> AutoGenConfig.setRecursionDepth recDepth

        let! result = Gen.autoWith<RecursiveType<int>> config
        test <@ result.Depth = recDepth @>
    } |> Property.check
