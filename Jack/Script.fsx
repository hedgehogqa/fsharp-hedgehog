#r "../packages/FSharpx.Collections/lib/net40/FSharpx.Collections.dll"
#r "../packages/FsControl/lib/net40/FsControl.dll"

#load "Numeric.fs"
#load "Seed.fs"
#load "Tree.fs"
#load "Shrink.fs"
#load "Random.fs"
#load "Gen.fs"
#load "Property.fs"

open Jack
open System

//
// Combinators
//

Property.check <| forAll {
    let! x = Gen.range 1 100
    let! ys = Gen.item ["a"; "b"; "c"; "d"] |> Gen.seq1
    return x < 50 || Seq.length ys <= 3 || Seq.contains "a" ys
}

Property.check <| forAll {
    let! xs = Gen.string
    return String.length xs <= 5
}

//
// Hutton's Razor
//

type Exp =
  | Lit of int
  | Add of Exp * Exp

let rec evalExp = function
    | Lit x ->
        x
    | Add (x, y) ->
        evalExp x + evalExp y

let shrinkExp = function
    | Lit _ ->
        []
    | Add (x, y) ->
        [x; y]

#nowarn "40"
let rec genExp =
    Gen.delay <| fun _ ->
    Gen.shrink shrinkExp <|
    Gen.choiceRec [
        Lit <!> Gen.int
    ] [
        Add <!> Gen.zip genExp genExp
    ]

Property.check <| forAll {
    let! x = genExp
    match x with
    | Add (Add _, Add _) when evalExp x > 100 ->
        return false
    | _ ->
        return true
}

//
// reverse (reverse xs) = xs, ∀xs :: [α] ― The standard "hello-world" property.
//

Property.check <| forAll {
    let! xs = Gen.list Gen.int
    return xs
            |> List.rev
            |> List.rev
            = xs
}

//
// Conditional Generators
//

let genLeapYear =
    Gen.range 2000 3000 |> Gen.suchThat DateTime.IsLeapYear

Gen.printSample genLeapYear

//
// Conditional Properties
//

// Fails due to integer overflow
Property.check <| forAll {
    let! x = Gen.int
    let! y = Gen.int
    if x > 0 && y > 0 then
        return x * y > 0
}

//
// Lazy Properties
//

Property.check <| forAll {
    let! n = Gen.int
    if n <> 0 then
        return 1 / n = 1 / n
}

//
// Printing Samples
//

Gen.printSample <| gen {
    let! x = Gen.range 0 10
    let! y = Gen.item [ "x"; "y"; "z"; "w" ]
    let! z = Gen.double
    let! w = Gen.string' Gen.alphaNum
    return sprintf "%A + %s + %f + %s" x y z w
}
