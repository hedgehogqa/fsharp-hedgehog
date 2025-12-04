namespace Hedgehog.FSharp

/// Utilities for formatting values in test output
[<RequireQualifiedAccess>]
module ValueFormatting =

    /// Formats a value for display in test output.
    /// Converts ResizeArray to list for better readability.
    let printValue (value: obj) : string =
        let prepareForPrinting (value: obj) : obj =
            if isNull value then
                value
            else
                let typeInfo = System.Reflection.IntrospectionExtensions.GetTypeInfo(value.GetType())
                let isResizeArray = typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() = typedefof<ResizeArray<_>>
                if isResizeArray then
                    value :?> System.Collections.IEnumerable
                    |> Seq.cast<obj>
                    |> List.ofSeq
                    :> obj
                else
                    value

        value |> prepareForPrinting |> sprintf "%A"
