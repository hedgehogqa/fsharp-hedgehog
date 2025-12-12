namespace Hedgehog.Stateful

[<RequireQualifiedAccess>]
module Var =

    /// Initialise an unbound var with a default value (for initial state)
    [<CompiledName("Symbolic")>]
    let symbolic (defaultValue: 'T) : Var<'T> =
        { Name = -1; Bounded = false; Default = Some defaultValue }

namespace Hedgehog.Stateful.FSharp

open Hedgehog.Stateful

[<RequireQualifiedAccess>]
module Var =
    /// Resolve a var using its default if not found in env
    let resolve (env: Env) (v: Var<'T>) : 'T =
        v.Resolve(env)

    /// Resolve a var with explicit fallback (overrides var's default)
    let resolveOr (fallback: 'T) (env: Env) (v: Var<'T>) : 'T =
        v.ResolveOr(env, fallback)

    /// Create a bounded var from a Name (used during generation)
    let internal bound (name: Name) : Var<'T> =
        let (Name n) = name
        { Name = n; Bounded = true; Default = None }

    let internal convertFrom<'T> (v: Var<obj>) : Var<'T> =
        { Name = v.Name
          Bounded = v.Bounded
          Default = v.Default |> Option.map unbox<'T> }

    /// Resolve a variable, returning None if not found
    let tryResolve<'T> (v: Var<'T>) (env: Env) : 'T option =
        if not v.Bounded then
            v.Default
        else
            env.values
            |> Map.tryFind (Name v.Name)
            |> Option.map unbox<'T>
