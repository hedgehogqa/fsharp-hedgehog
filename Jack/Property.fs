namespace Jack

open FSharpx.Collections

type Report =
    | Report of List<string>

type Result<'a> =
    | Failure
    | Discard
    | Success of 'a

type Property<'a> =
    | Property of Gen<Report * Result<'a>>

module Tuple =
    let first (f : 'a -> 'c) (x : 'a, y : 'b) : 'c * 'b =
        f x, y

    let second (f : 'b -> 'c) (x : 'a, y : 'b) : 'a * 'c =
        x, f y

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Result =
    let map (f : 'a -> 'b) (r : Result<'a>) : Result<'b> =
        match r with
        | Failure ->
            Failure
        | Discard ->
            Discard
        | Success x ->
            Success (f x)

    let filter (f : 'a -> bool) (r : Result<'a>) : Result<'a> =
        match r with
        | Failure ->
            Failure
        | Discard ->
            Discard
        | Success x ->
            if f x then
              Success x
            else
              Discard

    let isFailure (x : Result<'a>) : bool =
        match x with
        | Failure ->
            true
        | Discard ->
            false
        | Success _ ->
            false

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Report =
    let ofList (xs : List<string>) : Report =
        Report xs

    let toList (Report xs : Report) : List<string> =
        xs

    let empty : Report =
        List.empty |> ofList

    let map (f : List<string> -> List<string>) (xs : Report) : Report =
        toList xs |> f |> ofList

    let addFailure (msg : string) (x : Report) : Report =
        map (List.cons msg) x

    let append (xs : Report) (ys : Report) : Report =
        toList xs @ toList ys |> ofList

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Property =
    open Tuple

    let ofGen (x : Gen<Report * Result<'a>>) : Property<'a> =
        Property x

    let toGen (Property x : Property<'a>) : Gen<Report * Result<'a>> =
        x

    let ofResult (x : Result<'a>) : Property<'a> =
        (Report.empty, x) |> Gen.constant |> ofGen

    let failure : Property<unit> =
        Failure |> ofResult

    let discard : Property<unit> =
        Discard |> ofResult

    let success (x : 'a) : Property<'a> =
        Success x |> ofResult

    let ofBool (x : bool) : Property<unit> =
        if x then
            success ()
        else
            failure

    let private mapGen
            (f : Gen<Report * Result<'a>> -> Gen<Report * Result<'b>>)
            (x : Property<'a>) : Property<'b> =
        toGen x |> f |> ofGen

    let map (f : 'a -> 'b) (x : Property<'a>) : Property<'b> =
        (mapGen << Gen.map << second << Result.map) f x

    let counterexample (msg : string) (x : Property<'a>) : Property<'a> =
        (mapGen << Gen.map << first << Report.addFailure) msg x

    let private bindGen
            (m : Gen<Report * Result<'a>>)
            (k : 'a -> Gen<Report * Result<'b>>) : Gen<Report * Result<'b>> =
        Gen.bind m <| fun (report, result) ->
            match result with
            | Failure ->
                Gen.constant (report, Failure)
            | Discard ->
                Gen.constant (report, Discard)
            | Success x ->
                Gen.map (first (Report.append report)) (k x)

    let bind (m : Property<'a>) (k : 'a -> Property<'b>) : Property<'b> =
        bindGen (toGen m) (toGen << k) |> ofGen

    let forAll (gen : Gen<'a>) (k : 'a -> Property<'b>) : Property<'b> =
        let prepend (x : 'a) =
            counterexample (sprintf "%A" x) (k x) |> toGen
        Gen.bind gen prepend |> ofGen

    //
    // Runner
    //

    let rec private takeSmallest
            (Node ((report, x), xs) : Tree<Report * Result<'a>>)
            (nshrinks : int) : Option<Report * int> =
        match x with
        | Failure ->
            match LazyList.tryFind (Result.isFailure << snd << Tree.outcome) xs with
            | None ->
                Some (report, nshrinks)
            | Some tree ->
                takeSmallest tree (nshrinks + 1)
        | Discard ->
            None
        | Success _ ->
            None

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

    let check' (n : int) (p : Property<unit>) : bool =
        let random = toGen p |> Gen.toRandom

        let nextSize size =
            if size >= 100 then
                1
            else
                size + 1

        let rec loop seed size tests discards =
            if tests = n then
                tests, discards, Tree.singleton (Report.empty, Success ())
            else
                let seed1, seed2 = Seed.split seed
                let result = Random.run seed1 size random

                match snd (Tree.outcome result) with
                | Failure ->
                    tests + 1, discards, result
                | Success () ->
                    loop seed2 (nextSize size) (tests + 1) discards
                | Discard ->
                    loop seed2 size tests (discards + 1)

        let seed = Seed.random ()
        let tests, discards, result = loop seed 1 0 0

        match takeSmallest result 0 with
        | None ->
            printfn "+++ OK, passed %s." (renderTests tests)
            true
        | Some (report, nshrinks) ->
            printfn "*** Failed! Falsifiable (after %s%s%s):"
                (renderTests tests)
                (renderShrinks nshrinks)
                (renderDiscards discards)
            List.map (printfn "%s") (Report.toList report) |> ignore
            false

    let check (p : Property<unit>) : bool =
        check' 100 p

[<AutoOpen>]
module PropertyBuilder =
    open Tuple

    type Builder internal () =
        member __.For(m : Property<'a>, k : 'a -> Property<'b>) : Property<'b> =
            Property.bind m k

        member __.Yield(x : 'a) : Property<'a> =
            Property.success x

        member __.Bind(m : Gen<'a>, k : 'a -> Property<'b>) : Property<'b> =
            Property.forAll m k

        member __.Return(b : bool) : Property<unit> =
            Property.ofBool b

        member __.Delay(f : unit -> Property<'a>) : Property<'a> =
            Gen.delay (Property.toGen << f) |> Property.ofGen

        member __.Zero() : Property<unit> =
            Property.discard

        [<CustomOperation("counterexample", MaintainsVariableSpace = true)>]
        member __.Counterexample(g : Property<'a>, [<ProjectionParameter>] f : 'a -> string) : Property<'a> =
            Property.bind g <| fun x ->
                Property.counterexample (f x) (Property.success x)

        [<CustomOperation("where", MaintainsVariableSpace = true)>]
        member __.Where(g : Property<'a>, [<ProjectionParameter>] p : 'a -> bool) : Property<'a> =
            Gen.map (second <| Result.filter p) (Property.toGen g) |> Property.ofGen

    let property = Builder ()
