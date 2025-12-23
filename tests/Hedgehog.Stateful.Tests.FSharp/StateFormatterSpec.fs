module Hedgehog.Stateful.Tests.StateFormatterSpec

open Hedgehog.Stateful
open Hedgehog.Stateful.FSharp
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
let ``StateFormatter resolves vars inside Option`` () =
    let var1 = Var.bound (Name 0)
    let state = {
        OptionalVar = Some var1
        ResultVar = Error "not used"
        UnionField = CaseNoVars 123
        ListOfVars = []
    }

    let env = Env.empty |> Env.add var1 42

    StateFormatter.formatForDisplay env state |> ignore

    Assert.Contains("42", $"%A{var1}")

[<Fact>]
let ``StateFormatter resolves vars inside Result Ok`` () =
    let var2: Var<string> = Var.bound (Name 0)
    let state = {
        OptionalVar = None
        ResultVar = Ok var2
        UnionField = CaseNoVars 123
        ListOfVars = []
    }

    let env = Env.empty |> Env.add var2 "hello"

    StateFormatter.formatForDisplay env state |> ignore

    Assert.Contains("\"hello\"",  $"%A{var2}")

[<Fact>]
let ``StateFormatter resolves vars inside custom union cases`` () =
    let var1 = Var.bound (Name 0)
    let var2: Var<string> = Var.bound (Name 1)
    let var3: Var<bool> = Var.bound (Name 2)

    let state = {
        OptionalVar = None
        ResultVar = Error "not used"
        UnionField = CaseTwoVars(var2, var3)
        ListOfVars = [var1]
    }

    let env =
        Env.empty
        |> Env.add var1 42
        |> Env.add var2 "world"
        |> Env.add var3 true

    StateFormatter.formatForDisplay env state |> ignore

    Assert.Contains("42", $"%A{var1}")
    Assert.Contains("\"world\"", $"%A{var2}")
    Assert.Contains("true", $"%A{var3}")

[<Fact>]
let ``StateFormatter handles nested Option Some Some`` () =
    let var1: Var<string> = Var.bound (Name 0)
    let nestedOption = Some (Some var1)

    let env =
        Env.empty
        |> Env.add var1 "deeply nested"

    StateFormatter.formatForDisplay env nestedOption |> ignore

    Assert.Contains("\"deeply nested\"", sprintf "%A" var1)

[<Fact>]
let ``StateFormatter handles vars in lists`` () =
    let var1 = Var.bound (Name 0)
    let var2 = Var.bound (Name 1)
    let var3 = Var.bound (Name 2)

    let state = {
        OptionalVar = None
        ResultVar = Error "not used"
        UnionField = CaseNoVars 0
        ListOfVars = [var1; var2; var3]
    }

    let env =
        Env.empty
        |> Env.add var1 10
        |> Env.add var2 20
        |> Env.add var3 30

    StateFormatter.formatForDisplay env state |> ignore

    Assert.Contains("10", $"%A{var1}")
    Assert.Contains("20", $"%A{var2}")
    Assert.Contains("30", $"%A{var3}")

[<Fact>]
let ``StateFormatter preserves object identity`` () =
    let var1 = Var.bound (Name 0)
    let state = {
        OptionalVar = Some var1
        ResultVar = Error "test"
        UnionField = CaseNoVars 99
        ListOfVars = []
    }

    let env = Env.empty |> Env.add var1 42

    let formatted = StateFormatter.formatForDisplay env state

    Assert.True(obj.ReferenceEquals(state, formatted))

[<Fact>]
let ``StateFormatter handles Choice types`` () =
    let var1 = Var.bound (Name 0)
    let var2: Var<string> = Var.bound (Name 1)
    let var3: Var<bool> = Var.bound (Name 2)

    let choice1: Choice<Var<int>, string, bool> = Choice1Of3 var1
    let choice2: Choice<int, Var<string>, bool> = Choice2Of3 var2
    let choice3: Choice<int, string, Var<bool>> = Choice3Of3 var3

    let env =
        Env.empty
        |> Env.add var1 100
        |> Env.add var2 "choice"
        |> Env.add var3 false

    StateFormatter.formatForDisplay env choice1 |> ignore
    StateFormatter.formatForDisplay env choice2 |> ignore
    StateFormatter.formatForDisplay env choice3 |> ignore

    Assert.Contains("100", $"%A{var1}")
    Assert.Contains("\"choice\"", $"%A{var2}")
    Assert.Contains("false", $"%A{var3}")

[<Fact>]
let ``StateFormatter handles vars in arrays`` () =
    let var1 = Var.bound (Name 0)
    let var2 = Var.bound (Name 1)

    let arr = [| var1; var2 |]

    let env =
        Env.empty
        |> Env.add var1 5
        |> Env.add var2 10

    StateFormatter.formatForDisplay env arr |> ignore

    Assert.Contains("5", $"%A{var1}")
    Assert.Contains("10", $"%A{var2}")

[<Fact>]
let ``StateFormatter handles vars in records`` () =
    let var1 = Var.bound (Name 0)
    let var2: Var<string> = Var.bound (Name 1)

    let record = { X = var1; Y = var2 }

    let env =
        Env.empty
        |> Env.add var1 999
        |> Env.add var2 "record field"

    StateFormatter.formatForDisplay env record |> ignore

    Assert.Contains("999", $"%A{var1}")
    Assert.Contains("\"record field\"", $"%A{var2}")

[<Fact>]
let ``StateFormatter handles circular references without infinite loop`` () =
    let var1 = Var.bound (Name 0)
    let node = { Value = var1; Next = None }
    node.Next <- Some node  // Create circular reference

    let env =
        Env.empty
        |> Env.add var1 42

    let formatted = StateFormatter.formatForDisplay env node

    Assert.Contains("42", $"%A{var1}")
    Assert.True(obj.ReferenceEquals(node, formatted))
