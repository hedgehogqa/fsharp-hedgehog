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
        { Name = -1; Bounded = false; Default = Some defaultValue; Transform = unbox<'T>; ResolvedValue = None }

namespace Hedgehog.Stateful.FSharp

open Hedgehog.Stateful


[<RequireQualifiedAccess>]
module Var =
    /// <summary>
    /// Resolve a variable using its default value if not found in the environment.
    /// </summary>
    /// <param name="env">The environment to resolve the variable from.</param>
    /// <param name="v">The variable to resolve.</param>
    /// <returns>The resolved value of the variable.</returns>
    let resolve (env: Env) (v: Var<'T>) : 'T =
        v.Resolve(env)

    /// <summary>
    /// Resolve a variable with an explicit fallback value, overriding the variable's default.
    /// </summary>
    /// <param name="fallback">The fallback value to use if the variable is not found.</param>
    /// <param name="env">The environment to resolve the variable from.</param>
    /// <param name="v">The variable to resolve.</param>
    /// <returns>The resolved value or the fallback if not found.</returns>
    let resolveOr (fallback: 'T) (env: Env) (v: Var<'T>) : 'T =
        v.ResolveOr(env, fallback)

    /// <summary>
    /// Resolve a variable, returning <c>Error</c> if not found in the environment or if resolution fails.
    /// </summary>
    /// <param name="v">The variable to resolve.</param>
    /// <param name="env">The environment to resolve the variable from.</param>
    /// <returns>The resolved value as <c>Ok</c>, or <c>Error</c> with failure reason if not found or transform fails.</returns>
    let tryResolve<'T> (v: Var<'T>) (env: Env) : Result<'T, string> =
        if not v.Bounded then
            match v.Default with
            | Some d -> Ok d
            | None -> Error "Symbolic variable has no default value"
        else
            match env.values |> Map.tryFind (Name v.Name) with
            | None -> Error $"Var_{v.Name} not found in environment"
            | Some value ->
                try
                    Ok (v.Transform value)
                with ex ->
                    Error $"Transform failed: {ex.GetType().Name}"

    /// <summary>
    /// Map a function over a variable, creating a new variable that projects
    /// a value from the original variable's output. This allows extracting
    /// fields from structured command outputs.
    /// </summary>
    /// <param name="f">The projection function to apply.</param>
    /// <param name="v">The variable to map over.</param>
    /// <returns>A new variable with the projection applied.</returns>
    let map (f: 'T -> 'U) (v: Var<'T>) : Var<'U> =
        { Name = v.Name
          Bounded = v.Bounded
          Default = v.Default |> Option.map f
          Transform = v.Transform >> f
          ResolvedValue = None }

    /// Create a bounded var from a Name (used during generation)
    let internal bound (name: Name) : Var<'T> =
        let (Name n) = name
        { Name = n; Bounded = true; Default = None; Transform = unbox<'T>; ResolvedValue = None }

    let internal convertFrom<'T> (v: Var<obj>) : Var<'T> =
        { Name = v.Name
          Bounded = v.Bounded
          Default = v.Default |> Option.map unbox<'T>
          Transform = unbox<'T>
          ResolvedValue = None }
