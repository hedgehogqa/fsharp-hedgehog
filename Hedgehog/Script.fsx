#if INTERACTIVE
#load "../paket-files/fsprojects/FSharpx.Collections/src/FSharpx.Collections/Collections.fs"
      "../paket-files/fsprojects/FSharpx.Collections/src/FSharpx.Collections/LazyList.fsi"
      "../paket-files/fsprojects/FSharpx.Collections/src/FSharpx.Collections/LazyList.fs"
      "Numeric.fs"
      "Seed.fs"
      "Tree.fs"
      "Random.fs"
      "Shrink.fs"
      "Gen.fs"
      "Property.fs"
#endif

open Hedgehog
open System

//
// Combinators
//

Property.print <| property {
    let! x = Gen.range 1 100
    let! ys = Gen.item ["a"; "b"; "c"; "d"] |> Gen.seq
    counterexample (sprintf "tryHead ys = %A" <| Seq.tryHead ys)
    return x < 25 || Seq.length ys <= 3 || Seq.contains "a" ys
}

Property.print <| property {
    let! xs = Gen.string
    return String.length xs <= 5
}

//
// reverse (reverse xs) = xs, ∀xs :: [α] ― The standard "hello-world" property.
//

Property.print <| property {
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
    Gen.range 2000 3000 |> Gen.filter DateTime.IsLeapYear

Gen.printSample genLeapYear

//
// Conditional Properties
//

// Fails due to integer overflow
Property.print <| property {
    let! x = Gen.int
    let! y = Gen.int
    where (x > 0 && y > 0)
    counterexample (sprintf "x * y = %d" <| x * y)
    return x * y > 0
}

//
// Lazy Properties
//

Property.print <| property {
    let! n = Gen.int
    where (n <> 0)
    return 1 / n = 1 / n
}

//
// Loops
//

Property.print <| property {
    for x in "abcd" do
        // Custom operations (i.e. counterexample) can't be used in computation
        // expressions which have control flow :( we can fake it using return!
        // however.
        return! Property.counterexample (sprintf "x = %A" x)

        // Note, return can be used multiple times, its a bit like 'assert'.
        return x <> 'w'
        return x <> 'z'

    let mutable n = 0
    while n < 10 do
        n <- n + 1
        let! k = Gen.range 0 n
        return! Property.counterexample (sprintf "n = %d" n)
        return! Property.counterexample (sprintf "k = %d" k)
        return k <> 5
}

let gs =
    [ (fun x -> x + 1)
      (fun x -> x * 2)
      (fun x -> x / 3) ]
    |> List.map Gen.constant

Gen.printSample <| gen {
    let mutable x = 10
    for g in gs do
        let! f = g
        x <- f x
    return x
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

//
// Printing Samples ― Complex Types
//

Gen.byte
|> Gen.map int
|> Gen.tuple
|> Gen.map (fun (ma, mi) -> Version (ma, mi))
|> Gen.printSample

Gen.byte
|> Gen.map int
|> Gen.tuple3
|> Gen.map (fun (ma, mi, bu) -> Version (ma, mi, bu))
|> Gen.printSample

Gen.byte
|> Gen.map int
|> Gen.tuple4
|> Gen.map (fun (ma, mi, bu, re) -> Version (ma, mi, bu, re))
|> Gen.printSample

//
// Printing Samples ― System.Net.IPAddress
//

Gen.printSample <| gen {
    let! addr = Gen.byte |> Gen.array' 4 4
    return System.Net.IPAddress addr
}

//
// Printing Samples ― System.Guid
//

Gen.printSample <| Gen.guid

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

Property.print <| property {
    let! x = genExp
    match x with
    | Add (Add _, Add _) when evalExp x > 100 ->
        return false
    | _ ->
        return true
}
