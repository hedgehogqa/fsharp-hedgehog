#if INTERACTIVE
#load "AutoOpen.fs"
      "Numeric.fs"
      "Seed.fs"
      "Seq.fs"
      "Tree.fs"
      "OptionTree.fs"
      "Range.fs"
      "Random.fs"
      "Shrink.fs"
      "Gen.fs"
      "Journal.fs"
      "Tuple.fs"
      "GenTuple.fs"
      "Outcome.fs"
      "Report.fs"
      "PropertyArgs.fs"
      "PropertyConfig.fs"
      "Property.fs"
#endif

open Hedgehog
open System

//
// Combinators
//

property {
    let! x = Gen.int32 (Range.constant 1 100)
    let! ys = Gen.item ["a"; "b"; "c"; "d"] |> Gen.seq (Range.linear 0 100)
    counterexample (sprintf "tryHead ys = %A" (Seq.tryHead ys))
    return x < 25 || Seq.length ys <= 3 || Seq.contains "a" ys
}
|> Property.render
|> printfn "%s"

property {
    let! xs = Gen.string (Range.constant 0 100) Gen.unicode
    return String.length xs <= 5
}
|> Property.render
|> printfn "%s"

//
// reverse (reverse xs) = xs, ∀xs :: [α] ― The standard "hello-world" property.
//

property {
    let! xs = Gen.list (Range.linear 0 100) Gen.alpha
    return xs = List.rev (List.rev xs)
}
|> Property.render
|> printfn "%s"

//
// Conditional Generators
//

let genLeapYear =
    Range.constant 2000 3000
    |> Gen.int32
    |> Gen.filter DateTime.IsLeapYear

genLeapYear
|> Gen.renderSample
|> printfn "%s"

//
// Conditional Properties
//

// Fails due to integer overflow
property {
    let! x = Range.constantBounded () |> Gen.int32
    let! y = Range.constantBounded () |> Gen.int32
    where (x > 0 && y > 0)
    counterexample (sprintf "x * y = %d" (x * y))
    return x * y > 0
}
|> Property.render
|> printfn "%s"

// https://github.com/hedgehogqa/fsharp-hedgehog/issues/124#issuecomment-335402728
property {
    let! x = Range.exponentialBounded () |> Gen.int32
    where (x <> 0)
    return true
}
|> Property.check

//
// Lazy Properties
//

property {
    let! n = Range.constantBounded () |> Gen.int32
    where (n <> 0)
    return 1 / n = 1 / n
}
|> Property.render
|> printfn "%s"

//
// Properties that can throw an exception
//

property {
    let! (x, y) = Range.constant 0 9 |> Gen.int32 |> Gen.tuple
    // The exception gets rendered and added to the journal.
    failwith "Uh oh"
    return x + y = x + y
}
|> Property.render
|> printfn "%s"

//
// Loops
//

property {
    for x in "abcd" do
        // Custom operations (i.e. counterexample) can't be used in computation
        // expressions which have control flow :( we can fake it using return!
        // however.
        return! Property.counterexample (fun () -> sprintf "x = %A" x)

        // Note, return can be used multiple times, its a bit like 'assert'.
        return x <> 'w'
        return x <> 'z'

    let mutable n = 0
    while n < 10 do
        n <- n + 1
        let! k = Range.constant 0 n |> Gen.int32
        return! Property.counterexample (fun () -> sprintf "n = %d" n)
        return! Property.counterexample (fun () -> sprintf "k = %d" k)
        return k <> 5
}
|> Property.render
|> printfn "%s"

let gs =
    [ (fun x -> x + 1)
      (fun x -> x * 2)
      (fun x -> x / 3) ]
    |> List.map Gen.constant

gen {
    let mutable x = 10
    for g in gs do
        let! f = g
        x <- f x
    return x
}
|> Gen.renderSample
|> printfn "%s"

//
// Printing Samples
//

gen {
    let! x = Gen.int32 (Range.constant 0 10)
    let! y = Gen.item [ "x"; "y"; "z" ]
    let! z = Gen.double (Range.constant 0.1 9.99)
    let! w = Gen.string (Range.constant 0 100) Gen.alphaNum
    return sprintf "%A + %s + %f + %s" x y z w
}
|> Gen.renderSample
|> printfn "%s"

//
// Printing Samples ― Complex Types
//

Range.constantBounded ()
|> Gen.byte
|> Gen.map int
|> Gen.tuple
|> Gen.map (fun (ma, mi) -> Version (ma, mi))
|> Gen.renderSample
|> printfn "%s"

Range.constantBounded ()
|> Gen.byte
|> Gen.map int
|> Gen.tuple3
|> Gen.map (fun (ma, mi, bu) -> Version (ma, mi, bu))
|> Gen.renderSample
|> printfn "%s"

Range.constantBounded ()
|> Gen.byte
|> Gen.map int
|> Gen.tuple4
|> Gen.map (fun (ma, mi, bu, re) -> Version (ma, mi, bu, re))
|> Gen.renderSample
|> printfn "%s"

//
// Printing Samples ― Prevent a generator from shrinking
//

Range.exponentialBounded ()
|> Gen.double
|> Gen.noShrink
|> Gen.renderSample
|> printfn "%s"

//
// Printing Samples ― System.Net.IPAddress
//
gen {
    let! addr =
        Range.constantBounded () |> Gen.byte |> Gen.array (Range.singleton 4)
    return Net.IPAddress addr
}
|> Gen.renderSample
|> printfn "%s"

//
// Printing Samples ― System.Guid
//

Gen.guid
|> Gen.renderSample
|> printfn "%s"

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

open Hedgehog.Gen.Operators

let rec genExp =
    Gen.delay (fun _ ->
        let choiceRec =
            Gen.choiceRec
                [ Lit <!> Gen.int32 (Range.constantBounded ()) ]
                [ Add <!> Gen.zip genExp genExp ]
        Gen.shrink shrinkExp choiceRec)

property {
    let! x = genExp
    match x with
    | Add (Add _, Add _) when evalExp x > 100 ->
        return false
    | _ ->
        return true
}
|> Property.render
|> printfn "%s"
