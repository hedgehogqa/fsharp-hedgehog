module Hedgehog.Stateful.Tests.VarDisplaySpec

open Hedgehog.Stateful
open VerifyXunit
open Xunit

// Test types with various F# constructs
type MyUnion =
    | CaseWithVar of Var<int>
    | CaseTwoVars of Var<string> * Var<bool>
    | CaseNoVars of int

type NestedState = {
    OptionalVar: Var<int> option
    ResultVar: Result<Var<string>, string>
    UnionField: MyUnion
    ListOfVars: Var<int> list
}

type SimpleRecord = { X: Var<int>; Y: Var<string> }

type Node = { Value: Var<int>; mutable Next: Node option }

[<Fact>]
let ``Concrete vars display their values`` () =
    Concrete 42 |> Verifier.VerifyFormatted

[<Fact>]
let ``Symbolic vars with default display the default`` () =
    Symbolic (Some "hello") |> Verifier.VerifyFormatted

[<Fact>]
let ``Symbolic vars without default display as symbolic`` () =
    Symbolic None |> Verifier.VerifyFormatted

[<Fact>]
let ``Vars in Option types display correctly`` () =
    let concreteVar = Concrete 42
    let symbolicVar : Var<string> = Symbolic (Some "test")
    {
        OptionalVar = Some concreteVar
        ResultVar = Ok symbolicVar
        UnionField = CaseNoVars 123
        ListOfVars = []
    } |> Verifier.VerifyFormatted

[<Fact>]
let ``Vars in union cases display correctly`` () =
    let concreteInt = Concrete 42
    let concreteStr : Var<string> = Concrete "world"
    let concreteBool = Concrete true

    {
        OptionalVar = None
        ResultVar = Error "not used"
        UnionField = CaseTwoVars(concreteStr, concreteBool)
        ListOfVars = [concreteInt]
    } |> Verifier.VerifyFormatted

[<Fact>]
let ``Vars in lists display correctly`` () =
    let var1 = Concrete 10
    let var2 = Concrete 20
    let var3 = Symbolic (Some 30)

    {
        OptionalVar = None
        ResultVar = Error "not used"
        UnionField = CaseNoVars 0
        ListOfVars = [var1; var2; var3]
    } |> Verifier.VerifyFormatted


[<Fact>]
let ``Vars in arrays display correctly`` () =
    let var1 = Concrete 5
    let var2 = Symbolic (Some 10)

    [| var1; var2 |] |> Verifier.VerifyFormatted
