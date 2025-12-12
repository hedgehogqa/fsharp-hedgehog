namespace Hedgehog.Stateful

open System.Threading.Tasks
open Hedgehog

/// Unique identifier for a symbolic variable
[<Struct>]
type Name = internal Name of int

/// Environment mapping symbolic names to concrete values.
/// The SUT is passed separately to Execute, not stored in Env.
type Env = private {
    values: Map<Name, obj>
    nextId: int
}

/// Symbolic variable referencing a command's output. Variables let us chain commands
/// by using one command's result as input to another, even before execution.
[<StructuredFormatDisplay("{DisplayText}")>]
type Var<'T> = private {
    Name: int
    Bounded: bool
    Default: 'T option
}
with
    member private this.DisplayText =
        if this.Bounded then
            match this.Default with
            | Some d -> $"Var_%d{this.Name} (default=%A{d}))"
            | None -> $"Var_%d{this.Name}"
        else
            match this.Default with
            | Some d -> $"%A{d} (symbolic)"
            | None -> "<no value> (symbolic)"

    /// Resolve the variable using its default if not found in env
    member this.Resolve(env: Env) : 'T =
        if not this.Bounded then
            match this.Default with
            | Some d -> d
            | None -> failwithf "Symbolic var must have a default value"
        else
            match Map.tryFind (Name this.Name) env.values with
            | Some v -> unbox<'T> v
            | None ->
                match this.Default with
                | Some d -> d
                | None -> failwithf $"Var %A{Name this.Name} not bound in environment and no default provided"

    /// Resolve the variable with an explicit fallback (overrides var's default)
    member this.ResolveOr(env: Env, fallback: 'T) : 'T =
        if not this.Bounded then
            fallback  // Override default for unbounded
        else
            match Map.tryFind (Name this.Name) env.values with
            | Some v -> unbox<'T> v
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
