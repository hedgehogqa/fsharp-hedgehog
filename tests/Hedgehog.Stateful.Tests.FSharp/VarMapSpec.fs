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

/// Command that verifies we can resolve the projected fields
type VerifyLastPersonCommand() =
    inherit Command<PersonRegistry, RegistryState, unit, bool>()

    override _.Name = "VerifyLastPerson"
    override _.Precondition state =
        // Only run if we have a bounded var (at least one person added)
        state.LastPersonName.IsBounded

    override _.Execute(registry, _, _, _) =
        let people = registry.GetPeople()
        let hasData = not (List.isEmpty people)
        Task.FromResult(hasData)

    override _.Generate _ = Gen.constant ()

    override _.Update(state, _, _) = state  // No state change

    override _.Ensure(env, state, _, _, result) =
        if result then
            // Verify we can resolve the mapped fields
            let name = state.LastPersonName.Resolve(env)
            let age = state.LastPersonAge.Resolve(env)

            // Name should be non-empty and age should be in valid range
            not (System.String.IsNullOrWhiteSpace(name)) && age >= 0 && age <= 100
        else
            true

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
        VerifyLastPersonCommand()
    |]

[<Fact>]
let ``Var.map allows projecting fields from structured command outputs``() =
    let sut = PersonRegistry()
    VarMapSpec().ToProperty(sut).Check()

[<Fact>]
let ``Var.map preserves symbolic status``() =
    // Create a symbolic var with a default person
    let personVar = Var.symbolic { Name = "Alice"; Age = 30 }

    // Map to get name
    let nameVar = Var.map _.Name personVar

    // Both vars should be symbolic (not bounded)
    Assert.False(personVar.IsBounded)
    Assert.False(nameVar.IsBounded)

[<Fact>]
let ``Var.map chains multiple projections``() =
    let personVar = Var.symbolic { Name = "Bob"; Age = 25 }

    // Chain projections
    let nameVar = Var.map _.Name personVar
    let nameLengthVar = Var.map String.length nameVar

    // All should remain symbolic
    Assert.False(nameLengthVar.IsBounded)
