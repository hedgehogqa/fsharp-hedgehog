namespace Hedgehog.Stateful


[<RequireQualifiedAccess>]
module Var =

    /// <summary>
    /// Initialise a symbolic (unbound) variable with a default value for the initial state.
    /// A symbolic variable is not yet bound to a generated value and is used to represent
    /// a placeholder in the model before generation or binding occurs.
    /// </summary>
    /// <param name="defaultValue">The default value to assign to the variable.</param>
    /// <returns>A new symbolic (unbound) <c>Var&lt;T&gt;</c> with the given default value.</returns>
    [<CompiledName("Symbolic")>]
    let symbolic (defaultValue: 'T) : Var<'T> =
        Symbolic (Some defaultValue)

namespace Hedgehog.Stateful.FSharp

open Hedgehog.Stateful


[<RequireQualifiedAccess>]
module Var =
    /// <summary>
    /// Resolve a variable using its default value if symbolic.
    /// </summary>
    /// <param name="env">The environment capability token.</param>
    /// <param name="v">The variable to resolve.</param>
    /// <returns>The resolved value of the variable.</returns>
    let resolve (env: Env) (v: Var<'T>) : 'T =
        v.Resolve(env)

    /// <summary>
    /// Resolve a variable with an explicit fallback value.
    /// </summary>
    /// <param name="fallback">The fallback value to use if the variable is symbolic without a default.</param>
    /// <param name="env">The environment capability token.</param>
    /// <param name="v">The variable to resolve.</param>
    /// <returns>The resolved value or the fallback if not found.</returns>
    let resolveOr (fallback: 'T) (env: Env) (v: Var<'T>) : 'T =
        v.ResolveOr(env, fallback)

    /// <summary>
    /// Resolve a variable, returning <c>Error</c> if it's symbolic without a default value.
    /// </summary>
    /// <param name="v">The variable to resolve.</param>
    /// <param name="env">The environment capability token.</param>
    /// <returns>The resolved value as <c>Ok</c>, or <c>Error</c> with failure reason if symbolic without default.</returns>
    let tryResolve<'T> (v: Var<'T>) (env: Env) : Result<'T, string> =
        match v with
        | Concrete value -> Ok value
        | Symbolic (Some defaultValue) -> Ok defaultValue
        | Symbolic None -> Error "Symbolic variable has no default value"

    /// <summary>
    /// Map a function over a variable, creating a new variable that projects
    /// a value from the original variable's output. This allows extracting
    /// fields from structured command outputs.
    /// </summary>
    /// <param name="f">The projection function to apply.</param>
    /// <param name="v">The variable to map over.</param>
    /// <returns>A new variable with the projection applied.</returns>
    let map (f: 'T -> 'U) (v: Var<'T>) : Var<'U> =
        match v with
        | Symbolic (Some defaultValue) -> Symbolic (Some (f defaultValue))
        | Symbolic None -> Symbolic None
        | Concrete value -> Concrete (f value)

    /// Create a symbolic var placeholder (used during generation)
    let internal symbolicEmpty () : Var<'T> =
        Symbolic None

    /// Convert from obj var to typed var (used internally)
    let internal convertFrom<'T> (v: Var<obj>) : Var<'T> =
        match v with
        | Symbolic (Some value) -> Symbolic (Some (unbox<'T> value))
        | Symbolic None -> Symbolic None
        | Concrete value -> Concrete (unbox<'T> value)
