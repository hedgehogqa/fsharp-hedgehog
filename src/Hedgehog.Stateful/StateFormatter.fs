namespace Hedgehog.Stateful

open System
open System.Reflection
open System.Collections.Generic

/// <summary>
/// Utilities for formatting state with resolved variable values for display in test failures.
/// </summary>
[<RequireQualifiedAccess>]
module internal StateFormatter =

    /// Check if a type is Var<T>
    let private isVarType (t: Type) =
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Var<_>>

    /// Mutate a Var<T> object to set its ResolvedValue field for counterexample formatting
    let private mutateVarForDisplay (env: Env) (varObj: obj) : unit =
        if varObj = null then ()
        else
            try
                // Call SetResolvedValue method using reflection
                let setMethod = varObj.GetType().GetMethod("SetResolvedValue", BindingFlags.NonPublic ||| BindingFlags.Instance)
                setMethod.Invoke(varObj, [| env |]) |> ignore
            with
            | _ -> () // If resolution fails, leave the var as-is

    /// Recursively walk an object and mutate all Var<T> fields/properties for display
    let rec private mutateVarsInObject (env: Env) (visited: HashSet<obj>) (obj: obj) : unit =
        if obj = null then ()
        else
            let objType = obj.GetType()

            // Avoid infinite loops on circular references
            if visited.Contains(obj) then ()
            else
                visited.Add(obj) |> ignore

                // Check if this is a Var<T> itself
                if isVarType objType then
                    mutateVarForDisplay env obj

                // Handle primitive types and strings - no traversal needed
                elif objType.IsPrimitive || objType = typeof<string> || objType.IsEnum then
                    ()

                // Handle collections (including arrays) and other types
                elif obj :? System.Collections.IEnumerable then
                    let enumerable = obj :?> System.Collections.IEnumerable
                    for element in enumerable do
                        if element <> null then
                            mutateVarsInObject env visited element

                    // Also traverse fields/properties in case it's a complex collection
                    traverseFieldsAndProperties env visited obj objType

                // Handle types with fields/properties
                else
                    traverseFieldsAndProperties env visited obj objType

    and private traverseFieldsAndProperties (env: Env) (visited: HashSet<obj>) (obj: obj) (objType: Type) : unit =
        try
            // Traverse all readable properties
            let properties =
                objType.GetProperties(BindingFlags.Public ||| BindingFlags.Instance)
                |> Array.filter _.CanRead

            for prop in properties do
                try
                    let value = prop.GetValue(obj)
                    if value <> null then
                        mutateVarsInObject env visited value
                with
                | _ -> () // Skip properties that can't be read

            // Traverse all fields
            let fields =
                objType.GetFields(BindingFlags.Public ||| BindingFlags.Instance)

            for field in fields do
                try
                    let value = field.GetValue(obj)
                    if value <> null then
                        mutateVarsInObject env visited value
                with
                | _ -> () // Skip fields that can't be read
        with
        | _ -> () // If anything fails, continue silently

    /// <summary>
    /// Format a state object for display by resolving all Var&lt;T&gt; fields to their concrete values.
    /// Mutates Var instances in-place by setting their ResolvedValue field.
    /// This should only be called during counterexample formatting on test failure.
    /// </summary>
    /// <param name="env">The environment containing variable bindings.</param>
    /// <param name="state">The state object to format.</param>
    /// <returns>The same state object with vars mutated for display.</returns>
    let formatForDisplay (env: Env) (state: 'TState) : 'TState =
        let visited = HashSet<obj>(ReferenceEqualityComparer.Instance)
        mutateVarsInObject env visited (box state)
        state
