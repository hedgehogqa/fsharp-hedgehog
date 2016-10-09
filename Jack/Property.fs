#light
namespace Jack

open FSharpx.Collections

type Result =
    | Failure of List<string>
    | Success

type Property =
    | Property of Gen<Result>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Result =
    let isFailure (x : Result) : bool =
        match x with
        | Failure _ ->
            true
        | Success ->
            false

    let mapFailure (f : List<string> -> List<string>) (x : Result) : Result =
        match x with
        | Failure msgs ->
            Failure (f msgs)
        | Success ->
            Success

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Property =

    let ofGen (x : Gen<Result>) : Property =
        Property x

    let toGen (Property x : Property) : Gen<Result> =
        x

    let ofResult (x : Result) : Property =
        x |> Gen.constant |> ofGen

    let ofBool (x : bool) : Property =
        if x then
            Success |> ofResult
        else
            Failure [] |> ofResult

    let mapGen (f : Gen<Result> -> Gen<Result>) (Property x : Property) : Property =
        Property <| f x

    let counterexample (msg : string) (x : Property) : Property =
        (mapGen << Gen.map << Result.mapFailure) (List.cons msg) x

    let forAll (gen : Gen<'a>) (f : 'a -> Property) : Property =
        let prepend (x : 'a) =
            counterexample (sprintf "%A" x) (f x) |> toGen
        Gen.bind gen prepend |> ofGen


    //
    // Runner
    //

    let rec private takeSmallest (Node (x, xs) : Tree<Result>) (nshrinks : int) : Option<int * List<string>> =
        match x with
        | Success ->
            None
        | Failure msgs ->
            match LazyList.tryFind (Result.isFailure << Tree.outcome) xs with
            | None ->
                Some (nshrinks, msgs)
            | Some tree ->
                takeSmallest tree (nshrinks + 1)

    let private renderTests : int -> string = function
        | 1 ->
            "1 test"
        | n ->
            sprintf "%d tests" n

    let private renderShrinks : int -> string = function
        | 0 ->
            ""
        | 1 ->
            " and 1 shrink"
        | n ->
            sprintf " and %d shrinks" n

    let check' (n : int) (p : Property) : bool =
        let random = toGen p |> Gen.toRandom

        let nextSize size =
            if size >= 100 then
                1
            else
                size + 1

        let rec loop seed size tests =
            if tests = n then
                tests, Tree.singleton Success
            else
                let seed1, seed2 = Seed.split seed
                let result = Random.run seed1 size random

                match Tree.outcome result with
                | Failure _ ->
                    tests + 1, result
                | Success ->
                    loop seed2 (nextSize size) (tests + 1)

        let seed = Seed.random ()
        let tests, result = loop seed 1 0

        match takeSmallest result 0 with
        | None ->
            printfn "+++ OK, passed %s." (renderTests tests)
            true
        | Some (nshrinks, msgs) ->
            printfn "*** Failed! Falsifiable (after %s%s):" (renderTests tests) (renderShrinks nshrinks)
            List.map (printfn "%s") msgs |> ignore
            false

    let check (p : Property) : bool =
        check' 100 p

[<AutoOpen>]
module ForAll =
    type Builder internal () =
        member __.ReturnFrom(b : bool) : Property =
            Property.ofBool b
        member __.ReturnFrom(p : Property) : Property =
            p
        member __.Bind(m : Gen<'a>, k : 'a -> Property) : Property =
            Property.forAll m k

    let forAll = Builder ()
