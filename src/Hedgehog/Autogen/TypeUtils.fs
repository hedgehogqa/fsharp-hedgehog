namespace Hedgehog.AutoGen

open System

[<RequireQualifiedAccess>]
module internal TypeUtils =

    let satisfies (value: Type) (gen: Type) : bool =
        if (gen.IsGenericTypeDefinition || gen.IsGenericType) && value.IsGenericType then
            let genDef = if gen.IsGenericType then gen.GetGenericTypeDefinition() else gen
            let valueDef = value.GetGenericTypeDefinition()
            if genDef = valueDef then
                let genArgs = gen.GetGenericArguments()
                let valueArgs = value.GetGenericArguments()
                Array.forall2 (fun (g: Type) v -> if g.IsGenericParameter then true else g = v) genArgs valueArgs
            else false
        else
            gen = value
