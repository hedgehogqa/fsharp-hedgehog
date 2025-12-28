module Hedgehog.Stateful.Tests.VarReferenceSpec

open System.Threading.Tasks
open Hedgehog.FSharp
open Hedgehog.Linq
open Hedgehog.Stateful
open Hedgehog.Stateful.FSharp
open Xunit

/// Simple ID registry SUT that creates and looks up IDs
type IdRegistry() =
    let mutable nextId = 1
    let mutable registeredIds : Map<int, string> = Map.empty

    member _.Register(name: string) : int =
        let id = nextId
        nextId <- nextId + 1
        registeredIds <- Map.add id name registeredIds
        id

    member _.Lookup(id: int) : string option =
        Map.tryFind id registeredIds

    member _.Clear() =
        nextId <- 1
        registeredIds <- Map.empty

/// State that tracks registered IDs as symbolic variables
type RegistryState = {
    RegisteredIds: Var<int> list  // List of symbolic IDs from Register commands
}

/// Command that registers a name and returns an ID
type RegisterCommand() =
    inherit Command<IdRegistry, RegistryState, string, int>()

    override _.Name = "Register"
    override _.Precondition _ = true

    override _.Execute(registry, _, _, name) =
        let id = registry.Register(name)
        Task.FromResult(id)

    override _.Generate _ =
        Gen.alpha |> Gen.string (Range.linear 1 5)

    // Add the new ID (as Var<int>) to the list of registered IDs
    override _.Update(state, _, idVar) =
        { RegisteredIds = idVar :: state.RegisteredIds }

    override _.Ensure(_, _, _, _, result) =
        // IDs should be positive
        result > 0

/// Command that looks up a previously registered ID
type LookupCommand() =
    inherit Command<IdRegistry, RegistryState, Var<int>, string option>()

    override _.Name = "Lookup"

    // Can only lookup if we have registered IDs
    override _.Precondition state =
        not (List.isEmpty state.RegisteredIds)

    override _.Execute(registry, env, _, idVar) =
        // Resolve the symbolic ID to get the actual ID value
        let actualId = Var.resolve env idVar
        let result = registry.Lookup(actualId)
        Task.FromResult(result)

    // Pick a random ID from the list of registered IDs
    override _.Generate state =
        Gen.item state.RegisteredIds

    override _.Ensure(_, _, _, _, result) =
        // The ID we looked up should exist in the registry
        result.IsSome

/// Specification for testing Var<T> references in command inputs
type VarReferenceSpec() =
    inherit SequentialSpecification<IdRegistry, RegistryState>()

    override _.SetupCommands = [||]

    override _.InitialState =
        { RegisteredIds = [] }

    override _.Range = Range.linear 1 20

    override _.Commands = [|
        RegisterCommand()
        LookupCommand()
    |]

[<Fact>]
let ``Commands can use Var<T> from state as input``() =
    let sut = IdRegistry()
    VarReferenceSpec().ToProperty(sut).Check()

[<Fact>]
let ``Var<T> resolves correctly during execution``() =
    // Manually test that a Var can be stored and resolved
    let registry = IdRegistry()

    // Register a name and get an ID
    let id = registry.Register("Alice")

    // Create a bound var and bind it to the ID
    let env = Env.empty
    let name, env' = Env.freshName env
    let boundVar = Var.bound name
    let env'' = Env.add boundVar id env'

    // Resolve the var to get the ID back
    let resolvedId = Var.resolve env'' boundVar

    // Lookup using the resolved ID
    let result = registry.Lookup(resolvedId)

    Assert.Equal(Some "Alice", result)
    Assert.Equal(id, resolvedId)
