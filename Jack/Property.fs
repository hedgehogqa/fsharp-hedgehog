namespace Jack

open FSharpx.Collections

type Result =
    | Failure of List<string>
    | Success
    | Discard

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
        | Discard ->
            false

    let mapFailure (f : List<string> -> List<string>) (x : Result) : Result =
        match x with
        | Failure msgs ->
            Failure (f msgs)
        | Success ->
            Success
        | Discard ->
            Discard

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

    let discard : Property =
        Discard |> ofResult

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
        | Discard ->
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

    let private renderDiscards : int -> string = function
        | 0 ->
            ""
        | 1 ->
            " and 1 discard"
        | n ->
            sprintf " and %d discards" n

    let check' (n : int) (p : Property) : bool =
        let random = toGen p |> Gen.toRandom

        let nextSize size =
            if size >= 100 then
                1
            else
                size + 1

        let rec loop seed size tests discards =
            if tests = n then
                tests, discards, Tree.singleton Success
            else
                let seed1, seed2 = Seed.split seed
                let result = Random.run seed1 size random

                match Tree.outcome result with
                | Failure _ ->
                    tests + 1, discards, result
                | Success ->
                    loop seed2 (nextSize size) (tests + 1) discards
                | Discard ->
                    loop seed2 size tests (discards + 1)

        let seed = Seed.random ()
        let tests, discards, result = loop seed 1 0 0

        match takeSmallest result 0 with
        | None ->
            printfn "+++ OK, passed %s." (renderTests tests)
            true
        | Some (nshrinks, msgs) ->
            printfn "*** Failed! Falsifiable (after %s%s%s):"
                (renderTests tests)
                (renderShrinks nshrinks)
                (renderDiscards discards)
            List.map (printfn "%s") msgs |> ignore
            false

    let check (p : Property) : bool =
        check' 100 p

[<AutoOpen>]
module ForAllBuilder =
    type Builder internal () =
        member __.Return(b : bool) : Property =
            Property.ofBool b

        member __.ReturnFrom(p : Property) : Property =
            p

        member __.Bind(m : Gen<'a>, k : 'a -> Property) : Property =
            Property.forAll m k

        member __.Delay(f : unit -> Property) : Property=
            Gen.delay (Property.toGen << f) |> Property.ofGen

        member __.Zero() : Property =
            Property.discard

    let forAll = Builder ()
