namespace Hedgehog

open FSharpx.Collections
open System

type Journal =
    | Journal of List<string>

type Result<'a> =
    | Failure
    | Discard
    | Success of 'a

type Property<'a> =
    | Property of Gen<Journal * Result<'a>>

[<Measure>] type tests
[<Measure>] type discards
[<Measure>] type shrinks

type Status =
    | Failed of int<shrinks> * Journal
    | GaveUp
    | OK

type Report = {
    Tests : int<tests>
    Discards : int<discards>
    Status : Status
}

[<AutoOpen>]
module private Tuple =
    let first (f : 'a -> 'c) (x : 'a, y : 'b) : 'c * 'b =
        f x, y

    let second (f : 'b -> 'c) (x : 'a, y : 'b) : 'a * 'c =
        x, f y

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Journal =
    let ofList (xs : List<string>) : Journal =
        Journal xs

    let toList (Journal xs : Journal) : List<string> =
        xs

    let empty : Journal =
        List.empty |> ofList

    let singleton (x : string) : Journal =
        List.singleton x |> ofList

    let map (f : List<string> -> List<string>) (xs : Journal) : Journal =
        toList xs |> f |> ofList

    let addFailure (msg : string) (x : Journal) : Journal =
        map (List.cons msg) x

    let append (xs : Journal) (ys : Journal) : Journal =
        toList xs @ toList ys |> ofList

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

[<AutoOpen>]
module private Pretty =
    open System.Text

    let private renderTests : int<tests> -> string = function
        | 1<tests> ->
            "1 test"
        | n ->
            sprintf "%d tests" n

    let private renderDiscards : int<discards> -> string = function
        | 1<discards> ->
            "1 discard"
        | n ->
            sprintf "%d discards" n

    let private renderAndDiscards : int<discards> -> string = function
        | 0<discards> ->
            ""
        | 1<discards> ->
            " and 1 discard"
        | n ->
            sprintf " and %d discards" n

    let private renderAndShrinks : int<shrinks> -> string = function
        | 0<shrinks> ->
            ""
        | 1<shrinks> ->
            " and 1 shrink"
        | n ->
            sprintf " and %d shrinks" n

    let private append (sb : StringBuilder) (msg : string) : unit =
        sb.AppendLine msg |> ignore

    let renderOK (tests : int<tests>) : string =
        sprintf "+++ OK, passed %s." (renderTests tests)

    let renderGaveUp (tests : int<tests>) (discards : int<discards>) : string =
        sprintf "*** Gave up after %s, passed %s."
            (renderDiscards discards)
            (renderTests tests)

    let renderFailed
            (tests : int<tests>)
            (discards : int<discards>)
            (shrinks : int<shrinks>)
            (journal : Journal) : string =
        let sb = StringBuilder ()

        sprintf "*** Failed! Falsifiable (after %s%s%s):"
            (renderTests tests)
            (renderAndShrinks shrinks)
            (renderAndDiscards discards)
            |> append sb

        List.iter (append sb) (Journal.toList journal)

        sb.ToString(0, sb.Length - 1) // exclude extra newline

[<AbstractClass>]
type HedgehogException (message : string) =
    inherit Exception (message)

type GaveUpException (tests : int<tests>, discards : int<discards>) =
    inherit HedgehogException (renderGaveUp tests discards)

    member __.Tests =
        tests

type FailedException (tests : int<tests>, discards : int<discards>, shrinks : int<shrinks>, journal : Journal) =
    inherit HedgehogException (renderFailed tests discards shrinks journal)

    member __.Tests =
        tests

    member __.Discards =
        discards

    member __.Shrinks =
        shrinks

    member __.Journal =
        journal

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Report =

    let render (report : Report) : string =
        match report.Status with
        | OK ->
            renderOK report.Tests
        | GaveUp ->
            renderGaveUp report.Tests report.Discards
        | Failed (shrinks, journal) ->
            renderFailed report.Tests report.Discards shrinks journal

    let tryRaise (report : Report) : unit =
        match report.Status with
        | OK ->
            ()
        | GaveUp ->
            raise <| GaveUpException (report.Tests, report.Discards)
        | Failed (shrinks, journal)  ->
            raise <| FailedException (report.Tests, report.Discards, shrinks, journal)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Property =
    let ofGen (x : Gen<Journal * Result<'a>>) : Property<'a> =
        Property x

    let toGen (Property x : Property<'a>) : Gen<Journal * Result<'a>> =
        x

    let tryFinally (m : Property<'a>) (after : unit -> unit) : Property<'a> =
        Gen.tryFinally (toGen m) after |> ofGen

    let tryWith (m : Property<'a>) (k : exn -> Property<'a>) : Property<'a> =
        Gen.tryWith (toGen m) (toGen << k) |> ofGen

    let delay (f : unit -> Property<'a>) : Property<'a> =
        Gen.delay (toGen << f) |> ofGen

    let using (x : 'a) (k : 'a -> Property<'b>) : Property<'b> when
            'a :> IDisposable and
            'a : null =
        let k' = delay <| fun () -> k x
        tryFinally k' <| fun () ->
            match x with
            | null ->
                ()
            | _ ->
                x.Dispose ()

    let filter (p : 'a -> bool) (m : Property<'a>) : Property<'a> =
        Gen.map (second <| Result.filter p) (toGen m) |> ofGen

    let ofResult (x : Result<'a>) : Property<'a> =
        (Journal.empty, x) |> Gen.constant |> ofGen

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

    let counterexample (msg : string) : Property<unit> =
        Gen.constant (Journal.singleton msg, Success ()) |> ofGen

    let private mapGen
            (f : Gen<Journal * Result<'a>> -> Gen<Journal * Result<'b>>)
            (x : Property<'a>) : Property<'b> =
        toGen x |> f |> ofGen

    let map (f : 'a -> 'b) (x : Property<'a>) : Property<'b> =
        (mapGen << Gen.map << second << Result.map) f x

    let private bindGen
            (m : Gen<Journal * Result<'a>>)
            (k : 'a -> Gen<Journal * Result<'b>>) : Gen<Journal * Result<'b>> =
        Gen.bind m <| fun (journal, result) ->
            match result with
            | Failure ->
                Gen.constant (journal, Failure)
            | Discard ->
                Gen.constant (journal, Discard)
            | Success x ->
                Gen.map (first (Journal.append journal)) (k x)

    let bind (m : Property<'a>) (k : 'a -> Property<'b>) : Property<'b> =
        bindGen (toGen m) (toGen << k) |> ofGen

    let forAll (gen : Gen<'a>) (k : 'a -> Property<'b>) : Property<'b> =
        let handle (e : exn) =
            Gen.constant (Journal.singleton (string e), Failure) |> ofGen
        let prepend (x : 'a) =
            bind (counterexample (sprintf "%A" x)) (fun _ -> try k x with e -> handle e) |> toGen
        Gen.bind gen prepend |> ofGen

    //
    // Runner
    //

    let rec private takeSmallest
            (Node ((journal, x), xs) : Tree<Journal * Result<'a>>)
            (nshrinks : int<shrinks>) : Status =
        match x with
        | Failure ->
            match LazyList.tryFind (Result.isFailure << snd << Tree.outcome) xs with
            | None ->
                Failed (nshrinks, journal)
            | Some tree ->
                takeSmallest tree (nshrinks + 1<shrinks>)
        | Discard ->
            GaveUp
        | Success _ ->
            OK

    let report' (n : int<tests>) (p : Property<unit>) : Report =
        let random = toGen p |> Gen.toRandom

        let nextSize size =
            if size >= 100 then
                1
            else
                size + 1

        let rec loop seed size tests discards =
            if tests = n then
                { Tests = tests
                  Discards = discards
                  Status = OK }
            elif discards >= 100<discards> then
                { Tests = tests
                  Discards = discards
                  Status = GaveUp }
            else
                let seed1, seed2 = Seed.split seed
                let result = Random.run seed1 size random

                match snd (Tree.outcome result) with
                | Failure ->
                    { Tests = tests + 1<tests>
                      Discards = discards
                      Status = takeSmallest result 0<shrinks> }
                | Success () ->
                    loop seed2 (nextSize size) (tests + 1<tests>) discards
                | Discard ->
                    loop seed2 size tests (discards + 1<discards>)

        let seed = Seed.random ()
        loop seed 1 0<tests> 0<discards>

    let report (p : Property<unit>) : Report =
        report' 100<tests> p

    let check' (n : int<tests>) (p : Property<unit>) : unit =
        report' n p
        |> Report.tryRaise

    let check (p : Property<unit>) : unit =
        report p
        |> Report.tryRaise

    let print' (n : int<tests>) (p : Property<unit>) : unit =
        report' n p
        |> Report.render
        |> printfn "%s"

    let print (p : Property<unit>) : unit =
        report p
        |> Report.render
        |> printfn "%s"

[<AutoOpen>]
module PropertyBuilder =
    let rec private loop (p : unit -> bool) (m : Property<unit>) : Property<unit> =
        if p () then
            Property.bind m (fun _ -> loop p m)
        else
            Property.success ()

    type Builder internal () =
        member __.For(m : Property<'a>, k : 'a -> Property<'b>) : Property<'b> =
            Property.bind m k

        member __.For(xs : seq<'a>, k : 'a -> Property<unit>) : Property<unit> =
            let xse = xs.GetEnumerator ()
            Property.using xse <| fun xse ->
                let mv = xse.MoveNext
                let kc = Property.delay (fun () -> k xse.Current)
                loop mv kc

        member __.While(p : unit -> bool, m : Property<unit>) : Property<unit> =
            loop p m

        member __.Yield(x : 'a) : Property<'a> =
            Property.success x

        member __.Combine(m : Property<unit>, n : Property<'a>) : Property<'a> =
            Property.bind m (fun _ -> n)

        member __.TryFinally(m : Property<'a>, after : unit -> unit) : Property<'a> =
            Property.tryFinally m after

        member __.TryWith(m : Property<'a>, k : exn -> Property<'a>) : Property<'a> =
            Property.tryWith m k

        member __.Using(x : 'a, k : 'a -> Property<'b>) : Property<'b> when
                'a :> IDisposable and
                'a : null =
            Property.using x k

        member __.Bind(m : Gen<'a>, k : 'a -> Property<'b>) : Property<'b> =
            Property.forAll m k

        member __.Return(b : bool) : Property<unit> =
            Property.ofBool b

        member __.ReturnFrom(m : Property<'a>) : Property<'a> =
            m

        member __.Delay(f : unit -> Property<'a>) : Property<'a> =
            Property.delay f

        member __.Zero() : Property<unit> =
            Property.success ()

        [<CustomOperation("counterexample", MaintainsVariableSpace = true)>]
        member __.Counterexample(m : Property<'a>, [<ProjectionParameter>] f : 'a -> string) : Property<'a> =
            Property.bind m <| fun x ->
            Property.bind (Property.counterexample (f x)) <| fun _ ->
            Property.success x

        [<CustomOperation("where", MaintainsVariableSpace = true)>]
        member __.Where(m : Property<'a>, [<ProjectionParameter>] p : 'a -> bool) : Property<'a> =
            Property.filter p m

    let property = Builder ()
