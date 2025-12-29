module Hedgehog.Stateful.Tests.VarMapSpec

open System.Threading.Tasks
open Hedgehog.FSharp
open Hedgehog.Linq
open Hedgehog.Stateful
open Hedgehog.Stateful.FSharp
open Xunit

// A structured result type
type Person = {
    Name: string
    Age: int
}

// SUT that manages people
type PersonRegistry() =
    let mutable people : Person list = []

    member _.AddPerson(name: string, age: int) : Person =
        let person = { Name = name; Age = age }
        people <- person :: people
        person

    member _.GetPeople() = people
    member _.Clear() = people <- []

// State that tracks individual fields from the last added person
type RegistryState = {
    LastPersonName: Var<string>
    LastPersonAge: Var<int>
}

/// Command that adds a person and returns the structured Person result
type AddPersonCommand() =
    inherit Command<PersonRegistry, RegistryState, string * int, Person>()

    override _.Name = "AddPerson"
    override _.Precondition _ = true

    override _.Execute(registry, _, _, (name, age)) =
        let person = registry.AddPerson(name, age)
        Task.FromResult(person)

    override _.Generate _ =
        Gen.zip (Gen.alpha |> Gen.string (Range.linear 1 10))
                (Gen.int32 (Range.linear 0 100))

    // Use Var.map to project individual fields from the Person result
    override _.Update(_, _, personVar) =
        { LastPersonName = Var.map _.Name personVar
          LastPersonAge = Var.map _.Age personVar }

    override _.Ensure(_env, _oldState, _, (name, age), result) =
        // Verify the returned person has correct values
        result.Name = name && result.Age = age

/// Specification for testing Var.map
type VarMapSpec() =
    inherit SequentialSpecification<PersonRegistry, RegistryState>()

    override _.SetupCommands = [||]

    override _.InitialState =
        { LastPersonName = Var.symbolic ""
          LastPersonAge = Var.symbolic 0 }

    override _.Range = Range.linear 1 20

    override _.Commands = [|
        AddPersonCommand()
    |]

[<Fact>]
let ``Var.map allows projecting fields from structured command outputs``() =
    let sut = PersonRegistry()
    VarMapSpec().ToProperty(sut).Check()

[<Fact>]
let ``Var.map preserves symbolic variable behavior``() =
    // Create a symbolic var with a default person
    let personVar = Var.symbolic { Name = "Alice"; Age = 30 }

    // Map to get name - should preserve symbolic behavior
    let nameVar = Var.map _.Name personVar

    // Resolve without environment should use default value
    let resolvedName = nameVar.Resolve(Env.empty)
    Assert.Equal("Alice", resolvedName)

[<Fact>]
let ``Var.map chains multiple projections``() =
    let personVar = Var.symbolic { Name = "Bob"; Age = 25 }

    // Chain projections
    let nameVar = Var.map _.Name personVar
    let nameLengthVar = Var.map String.length nameVar

    // Should resolve through the chain correctly
    let length = nameLengthVar.Resolve(Env.empty)
    Assert.Equal(3, length) // "Bob" has length 3
