namespace Hedgehog.FSharp

/// Utilities for formatting values in test output
[<RequireQualifiedAccess>]
module internal ValueFormatting =

    /// Formats a value for display in test output.
    /// Converts ResizeArray to list for better readability.
    let printValue (value: obj) : string =
        let prepareForPrinting (value: obj) : obj =
        #if FABLE_COMPILER
            value
        #else
            if value = null then
                value
            else
                let t = value.GetType()
                let t = System.Reflection.IntrospectionExtensions.GetTypeInfo(t)
                let isList = t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<ResizeArray<_>>
                if isList
                then value :?> System.Collections.IEnumerable |> Seq.cast<obj> |> List.ofSeq :> obj
                else value
        #endif

        value |> prepareForPrinting |> sprintf "%A"
