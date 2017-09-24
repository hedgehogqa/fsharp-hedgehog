#if INTERACTIVE
#load "../../paket-files/fsprojects/FSharpx.Collections/src/FSharpx.Collections/Collections.fs"
      "../../paket-files/fsprojects/FSharpx.Collections/src/FSharpx.Collections/LazyList.fsi"
      "../../paket-files/fsprojects/FSharpx.Collections/src/FSharpx.Collections/LazyList.fs"
      "Numeric.fs"
      "Seed.fs"
      "Tree.fs"
      "Range.fs"
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
    let! x = Gen.int <| Range.constant 1 100
    let! ys = Gen.item ["a"; "b"; "c"; "d"] |> Gen.seq (Range.linear 0 100)
    counterexample (sprintf "tryHead ys = %A" <| Seq.tryHead ys)
    return x < 25 || Seq.length ys <= 3 || Seq.contains "a" ys
}

Property.print <| property {
    let! xs = Gen.string (Range.constant 0 100) Gen.unicode
    return String.length xs <= 5
}

//
// reverse (reverse xs) = xs, ∀xs :: [α] ― The standard "hello-world" property.
//

Property.print <| property {
    let! xs = Gen.list (Range.linear 0 100) Gen.alpha
    return xs
            |> List.rev
            |> List.rev
            = xs
}

//
// Conditional Generators
//

let genLeapYear =
    Gen.int <| Range.constant 2000 3000 |> Gen.filter DateTime.IsLeapYear

Gen.printSample genLeapYear

//
// Conditional Properties
//

// Fails due to integer overflow
Property.print <| property {
    let! x = Gen.int <| Range.constantBounded ()
    let! y = Gen.int <| Range.constantBounded ()
    where (x > 0 && y > 0)
    counterexample (sprintf "x * y = %d" <| x * y)
    return x * y > 0
}

//
// Lazy Properties
//

Property.print <| property {
    let! n = Gen.int <| Range.constantBounded ()
    where (n <> 0)
    return 1 / n = 1 / n
}

//
// Properties that can throw an exception
//

Property.print <| property {
    let! (x, y) = Range.constant 0 9 |> Gen.int |> Gen.tuple
    // The exception gets rendered and added to the journal.
    failwith "Uh oh"
    return x + y = x + y
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
        let! k = Gen.int <| Range.constant 0 n
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
    let! x = Gen.int <| Range.constant 0 10
    let! y = Gen.item [ "x"; "y"; "z" ]
    let! z = Gen.double <| Range.constant 0.1 9.99
    let! w = Gen.string (Range.constant 0 100) Gen.alphaNum
    return sprintf "%A + %s + %f + %s" x y z w
}

//
// Printing Samples ― Complex Types
//

Range.constantBounded ()
|> Gen.byte
|> Gen.map int
|> Gen.tuple
|> Gen.map (fun (ma, mi) -> Version (ma, mi))
|> Gen.printSample

Range.constantBounded ()
|> Gen.byte
|> Gen.map int
|> Gen.tuple3
|> Gen.map (fun (ma, mi, bu) -> Version (ma, mi, bu))
|> Gen.printSample

Range.constantBounded ()
|> Gen.byte
|> Gen.map int
|> Gen.tuple4
|> Gen.map (fun (ma, mi, bu, re) -> Version (ma, mi, bu, re))
|> Gen.printSample

//
// Printing Samples ― System.Net.IPAddress
//

Gen.printSample <| gen {
    let! addr =
        Gen.array (Range.constant 4 4) (Gen.byte <| Range.constantBounded ())
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
        Lit <!> Gen.int (Range.constantBounded ())
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
