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
    /// <summary>
    /// Mutable field used for caching resolved values to avoid redundant environment lookups and Transform applications.
    /// Eagerly populated when a concrete value is bound via Env.add.
    /// Also used for displaying resolved values in counterexamples.
    ///
    /// The cache eliminates the need to repeatedly look up values from Env and apply Transform,
    /// which is especially beneficial since most Var instances are created with concrete values
    /// that are immediately known, not from symbolic references that need delayed resolution.
    /// </summary>
    mutable ResolvedValue: 'T option
}
with

    member private this.DisplayText =
        // If resolved for display (during counterexample formatting), show the resolved value
        let value = this.ResolvedValue |> Option.orElse this.Default
        match value with
        | Some value -> $"%A{value}"
        | None -> "<unused>"

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
        // Check cache first to avoid redundant environment lookups and Transform applications
        match this.ResolvedValue with
        | Some cached ->
            value <- cached
            true
        | None ->
            // Try to look up in environment
            match Map.tryFind (Name this.Name) env.values with
            | Some v ->
                try
                    let resolved = this.Transform v
                    // Cache the resolved value for future calls (memoization)
                    this.ResolvedValue <- Some resolved
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
        { Name = -1; Default = Some value; Transform = unbox<'T>; ResolvedValue = None }


module internal Env =
    /// Empty environment
    let empty : Env = { values = Map.empty; nextId = 0 }

    /// Generate a fresh variable name
    let freshName (env: Env) : Name * Env =
        Name env.nextId, { env with nextId = env.nextId + 1 }
    
    /// Store a concrete value for a variable
    let add (v: Var<'a>) (value: 'a) (env: Env) : Env =
        // Eagerly cache the resolved value to avoid future environment lookups
        // Apply Transform to ensure the cache contains the correctly transformed value
        try
            let resolved = v.Transform (box value)
            v.ResolvedValue <- Some resolved
        with
            | _ -> () // If Transform fails, leave cache empty and let Resolve handle it later
        { env with values = Map.add (Name v.Name) (box value) env.values }

    /// Resolve a variable to its concrete value
    let resolve<'T> (v: Var<'T>) (env: Env) : 'T =
        v.Resolve(env)
