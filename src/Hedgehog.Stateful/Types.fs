namespace Hedgehog.Stateful

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
    /// </summary>
    Name: int
    /// <summary>
    /// Indicates if the variable is bound to a generated value.
    /// </summary>
    Bounded: bool
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
    /// <summary>
    /// Gets the unique integer name of the variable.
    /// </summary>
    member this.VarName = this.Name

    /// <summary>
    /// Gets whether the variable is bound to a generated value.
    /// </summary>
    member this.IsBounded = this.Bounded

    member private this.DisplayText =
        if this.Bounded then
            match this.Default with
            | Some d -> $"Var_%d{this.Name} (default=%A{d}))"
            | None -> $"Var_%d{this.Name}"
        else
            match this.Default with
            | Some d -> $"%A{d} (default)"
            | None -> "<no value> (symbolic)"

    /// <summary>
    /// Resolve the variable using its default if not found in the environment.
    /// </summary>
    /// <param name="env">The environment to resolve the variable from.</param>
    /// <returns>The resolved value of the variable.</returns>
    member this.Resolve(env: Env) : 'T =
        if not this.Bounded then
            match this.Default with
            | Some d -> d
            | None -> failwithf "Symbolic var must have a default value"
        else
            match Map.tryFind (Name this.Name) env.values with
            | Some v -> this.Transform v
            | None ->
                match this.Default with
                | Some d -> d
                | None -> failwithf $"Var %A{Name this.Name} not bound in environment and no default provided"

    /// <summary>
    /// Resolve the variable with an explicit fallback value, overriding the variable's default.
    /// </summary>
    /// <param name="env">The environment to resolve the variable from.</param>
    /// <param name="fallback">The fallback value to use if the variable is not found.</param>
    /// <returns>The resolved value or the fallback if not found.</returns>
    member this.ResolveOr(env: Env, fallback: 'T) : 'T =
        if not this.Bounded then
            fallback  // Override default for unbounded
        else
            match Map.tryFind (Name this.Name) env.values with
            | Some v -> this.Transform v
            | None -> fallback


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
