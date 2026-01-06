namespace Hedgehog.Stateful

open System.Diagnostics.CodeAnalysis

/// <summary>
/// Unique identifier for a symbolic variable.
/// </summary>
[<Struct>]
type Name = internal Name of int


/// <summary>
/// Environment mapping symbolic variable names to concrete values.
/// The SUT is passed separately to Execute, not stored in Env.
/// </summary>
type Env = private {
    values: Map<Name, obj>
    nextId: int
}


/// <summary>
/// Symbolic variable referencing a command's output. Symbolic variables are placeholders
/// that let us chain commands by using one command's result as input to another, even before execution.
/// A symbolic variable is not yet bound to a generated value and is used to represent a value in the model before binding occurs.
/// </summary>
[<StructuredFormatDisplay("{DisplayText}")>]
[<CustomEquality; CustomComparison>]
type Var<'T> = private {
    /// <summary>
    /// The unique integer name of the variable.
    /// Symbolic (unbound) variables have Name = -1.
    /// Bound variables have Name >= 0 and can be looked up in the environment.
    /// </summary>
    Name: int
    /// <summary>
    /// The optional default value for the variable.
    /// </summary>
    Default: 'T option
    /// <summary>
    /// Transform function applied when resolving the variable from the environment.
    /// Handles unboxing and any projections/mappings applied via Var.map.
    /// </summary>
    Transform: obj -> 'T
}
with

    member private this.DisplayText =
        $"Var_{this.Name}<{typeof<'T>}>"

    /// <summary>
    /// Resolve the variable using its default if not found in the environment.
    /// </summary>
    /// <param name="env">The environment to resolve the variable from.</param>
    /// <returns>The resolved value of the variable.</returns>
    member this.Resolve(env: Env) : 'T =
        let mutable value = Unchecked.defaultof<'T>
        if this.TryResolve(env, &value) then
            value
        else
            failwithf $"Var<{typeof<'T>}>(%A{Name this.Name}) not found in environment and no default provided.
This likely indicates a missing Require check in your command specification.
Commands that use Var<T> inputs must override Require to call TryResolve and return false if the variable cannot be resolved."


    /// <summary>
    /// Resolve the variable with an explicit fallback value, overriding the variable's default.
    /// </summary>
    /// <param name="env">The environment to resolve the variable from.</param>
    /// <param name="fallback">The fallback value to use if the variable is not found.</param>
    /// <returns>The resolved value or the fallback if not found.</returns>
    member this.ResolveOr(env: Env, fallback: 'T) : 'T =
        let mutable value = Unchecked.defaultof<'T>
        if this.TryResolve(env, &value) then
            value
        else
            fallback

    /// <summary>
    /// Attempts to resolve a variable from the environment.
    /// </summary>
    /// <param name="env">The environment to resolve the variable from.</param>
    /// <param name="value">When this method returns, contains the resolved value if successful; otherwise, the default value for the type.</param>
    /// <returns>true if the variable was successfully resolved; otherwise, false.</returns>
    member this.TryResolve(env: Env, [<System.Runtime.InteropServices.Out>] [<NotNullWhen(true)>] value: byref<'T>) : bool =
            // Try to look up in environment
        match Map.tryFind (Name this.Name) env.values with
        | Some v ->
            try
                let resolved = this.Transform v
                // Cache the resolved value for future calls (memoization)
                value <- resolved
                true
            with _ ->
                value <- Unchecked.defaultof<'T>
                false
        | None ->
            // Try default value
            match this.Default with
            | Some d ->
                value <- d
                true
            | None ->
                value <- Unchecked.defaultof<'T>
                false

    static member internal CreateSymbolic(value: 'T) : Var<'T> =
        { Name = -1; Default = Some value; Transform = unbox<'T> }

    override this.Equals(other: obj) : bool =
        match other with
        | :? Var<'T> as otherVar -> this.Name = otherVar.Name
        | _ -> false

    override this.GetHashCode() : int =
        hash this.Name

    interface System.IComparable with
        member this.CompareTo(other: obj) : int =
            match other with
            | :? Var<'T> as otherVar -> compare this.Name otherVar.Name
            | _ -> invalidArg (nameof other) "Cannot compare values of different types"


module internal Env =
    /// Empty environment
    let empty : Env = { values = Map.empty; nextId = 0 }

    /// Generate a fresh variable name
    let freshName (env: Env) : Name * Env =
        Name env.nextId, { env with nextId = env.nextId + 1 }
    
    /// Store a concrete value for a variable
    let add (v: Var<'a>) (value: 'a) (env: Env) : Env =
        { env with values = Map.add (Name v.Name) (box value) env.values }

    /// Resolve a variable to its concrete value
    let resolve<'T> (v: Var<'T>) (env: Env) : 'T =
        v.Resolve(env)
