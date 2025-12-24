namespace Hedgehog.Stateful

/// <summary>
/// Unique identifier for a symbolic variable.
/// </summary>
[<Struct>]
type Name = internal Name of int


/// <summary>
/// Environment used for generating unique action IDs and as a capability token
/// to enforce compile-time safety. The presence of an Env parameter indicates
/// the execution phase, where symbolic variables can be resolved.
/// The SUT is passed separately to Execute, not stored in Env.
/// </summary>
type Env = private {
    nextId: int
}


/// <summary>
/// Symbolic variable referencing a command's output. Symbolic variables are placeholders
/// that let us chain commands by using one command's result as input to another, even before execution.
/// Variables can be either Symbolic (during generation, with optional default values) or
/// Concrete (during execution, with actual runtime values).
/// </summary>
[<StructuredFormatDisplay("{DisplayText}")>]
type Var<'T> =
    internal
    /// <summary>
    /// Symbolic variable with an optional default value.
    /// None indicates a placeholder created during generation (not yet executed).
    /// Some indicates an initial state default value.
    /// </summary>
    | Symbolic of 'T option
    /// <summary>
    /// Concrete variable with an actual runtime value from execution.
    /// </summary>
    | Concrete of 'T
with
    /// <summary>
    /// Gets whether the variable has been bound to a concrete execution value.
    /// </summary>
    member this.IsBounded =
        match this with
        | Concrete _ -> true
        | Symbolic _ -> false

    member private this.DisplayText =
        match this with
        | Concrete value -> $"%A{value}"
        | Symbolic (Some defaultValue) -> $"%A{defaultValue}"
        | Symbolic None -> "<symbolic>"

    /// <summary>
    /// Resolve the variable to its value.
    /// Requires Env parameter as a capability token to enforce that resolution
    /// only happens during execution phase (Execute, Require, Ensure methods).
    /// </summary>
    /// <param name="env">The environment capability token.</param>
    /// <returns>The resolved value of the variable.</returns>
    member this.Resolve(env: Env) : 'T =
        match this with
        | Concrete value -> value
        | Symbolic (Some defaultValue) -> defaultValue
        | Symbolic None -> failwith "Cannot resolve symbolic variable without a default value"

    /// <summary>
    /// Resolve the variable with an explicit fallback value.
    /// </summary>
    /// <param name="env">The environment capability token.</param>
    /// <param name="fallback">The fallback value to use if the variable is symbolic without a default.</param>
    /// <returns>The resolved value or the fallback.</returns>
    member this.ResolveOr(env: Env, fallback: 'T) : 'T =
        match this with
        | Concrete value -> value
        | Symbolic (Some defaultValue) -> defaultValue
        | Symbolic None -> fallback


module internal Env =
    /// Empty environment
    let empty : Env = { nextId = 0 }

    /// Generate a fresh action ID
    let freshName (env: Env) : Name * Env =
        Name env.nextId, { env with nextId = env.nextId + 1 }
