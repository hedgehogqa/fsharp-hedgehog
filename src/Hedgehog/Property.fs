namespace Hedgehog

open System

[<Struct>]
type Property<'a> =
    | Property of Gen<Journal * Outcome<'a>>

module Property =

    let ofGen (x : Gen<Journal * Outcome<'a>>) : Property<'a> =
        Property x

    let toGen (Property x : Property<'a>) : Gen<Journal * Outcome<'a>> =
        x

    let tryFinally (after : unit -> unit) (m : Property<'a>) : Property<'a> =
        Gen.tryFinally after (toGen m) |> ofGen

    let tryWith (k : exn -> Property<'a>) (m : Property<'a>) : Property<'a> =
        Gen.tryWith (toGen << k) (toGen m) |> ofGen

    let delay (f : unit -> Property<'a>) : Property<'a> =
        Gen.delay (toGen << f) |> ofGen

    let using (x : 'a) (k : 'a -> Property<'b>) : Property<'b> when
            'a :> IDisposable and
            'a : null =
        delay (fun () -> k x)
        |> tryFinally (fun () ->
            match x with
            | null ->
                ()
            | _ ->
                x.Dispose ())

    let filter (p : 'a -> bool) (m : Property<'a>) : Property<'a> =
        GenTuple.mapSnd (Outcome.filter p) (toGen m) |> ofGen

    let ofOutcome (x : Outcome<'a>) : Property<'a> =
        (Journal.empty, x) |> Gen.constant |> ofGen

    let failure : Property<unit> =
        Failure |> ofOutcome

    let discard : Property<unit> =
        Discard |> ofOutcome

    let success (x : 'a) : Property<'a> =
        Success x |> ofOutcome

    let ofBool (x : bool) : Property<unit> =
        if x then
            success ()
        else
            failure

    let counterexample (msg : unit -> string) : Property<unit> =
        Gen.constant (Journal.singleton msg, Success ()) |> ofGen

    let private mapGen
            (f : Gen<Journal * Outcome<'a>> -> Gen<Journal * Outcome<'b>>)
            (x : Property<'a>) : Property<'b> =
        toGen x |> f |> ofGen

    let map (f : 'a -> 'b) (x : Property<'a>) : Property<'b> =
        (mapGen << GenTuple.mapSnd << Outcome.map) f x

    let private bindGen
            (k : 'a -> Gen<Journal * Outcome<'b>>)
            (m : Gen<Journal * Outcome<'a>>) : Gen<Journal * Outcome<'b>> =
        m |> Gen.bind (fun (journal, result) ->
            match result with
            | Failure ->
                Gen.constant (journal, Failure)
            | Discard ->
                Gen.constant (journal, Discard)
            | Success x ->
                GenTuple.mapFst (Journal.append journal) (k x))

    let bind (k : 'a -> Property<'b>) (m : Property<'a>) : Property<'b> =
        bindGen (toGen << k) (toGen m) |> ofGen

    let forAll (k : 'a -> Property<'b>) (gen : Gen<'a>) : Property<'b> =
        let handle (e : exn) =
            Gen.constant (Journal.singletonMessage (string e), Failure) |> ofGen
        let prepend (x : 'a) =
            counterexample (fun () -> sprintf "%A" x)
            |> bind (fun _ -> try k x with e -> handle e)
            |> toGen

        gen |> Gen.bind prepend |> ofGen

    let forAll' (gen : Gen<'a>) : Property<'a> =
        gen |> forAll success

    //
    // Runner
    //

    let rec private takeSmallest
            (renderRecheck : bool)
            (size : Size)
            (seed : Seed)
            (Node ((journal, x), xs) : Tree<Journal * Outcome<'a>>)
            (nshrinks : int<shrinks>)
            (shrinkLimit : int<shrinks> Option) : Status =
        let failed =
            Failed {
                Size = size
                Seed = seed
                Shrinks = nshrinks
                Journal = journal
                RenderRecheck = renderRecheck
            }
        let takeSmallest tree = takeSmallest renderRecheck size seed tree (nshrinks + 1<shrinks>) shrinkLimit
        match x with
        | Failure ->
            match Seq.tryFind (Outcome.isFailure << snd << Tree.outcome) xs with
            | None -> failed
            | Some tree ->
                match shrinkLimit with
                | None -> takeSmallest tree
                | Some shrinkLimit' ->
                    if nshrinks < shrinkLimit' then
                        takeSmallest tree
                    else failed
        | Discard ->
            GaveUp
        | Success _ ->
            OK

    let private reportWith' (renderRecheck : bool) (size0 : Size) (seed : Seed) (config : PropertyConfig) (p : Property<unit>) : Report =
        let random = toGen p |> Gen.toRandom

        let nextSize size =
            if size >= 100 then
                1
            else
                size + 1

        let rec loop seed size tests discards =
            if tests = config.TestLimit then
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
                      Status = takeSmallest renderRecheck size seed result 0<shrinks> config.ShrinkLimit}
                | Success () ->
                    loop seed2 (nextSize size) (tests + 1<tests>) discards
                | Discard ->
                    loop seed2 (nextSize size) tests (discards + 1<discards>)

        loop seed size0 0<tests> 0<discards>

    let reportWith (config : PropertyConfig) (p : Property<unit>) : Report =
        let seed = Seed.random ()
        p |> reportWith' true 1 seed config

    let report (p : Property<unit>) : Report =
        p |> reportWith PropertyConfig.defaultConfig

    let reportBoolWith (config : PropertyConfig) (p : Property<bool>) : Report =
        p |> bind ofBool |> reportWith config

    let reportBool (p : Property<bool>) : Report =
        p |> bind ofBool |> report

    let checkWith (config : PropertyConfig) (p : Property<unit>) : unit =
        reportWith config p
        |> Report.tryRaise

    let check (p : Property<unit>) : unit =
        report p
        |> Report.tryRaise

    let checkBool (g : Property<bool>) : unit =
        g |> bind ofBool |> check

    let checkBoolWith (config : PropertyConfig) (g : Property<bool>) : unit =
        g |> bind ofBool |> checkWith config

    /// Converts a possibly-throwing function to
    /// a property by treating "no exception" as success.
    let ofThrowing (f : 'a -> unit) (x : 'a) : Property<unit> =
        try
            f x
            success ()
        with
        | _ -> failure

    let reportRecheckWith (size : Size) (seed : Seed) (config : PropertyConfig) (p : Property<unit>) : Report =
        reportWith' false size seed config p

    let reportRecheck (size : Size) (seed : Seed) (p : Property<unit>) : Report =
        reportWith' false size seed PropertyConfig.defaultConfig p

    let reportRecheckBoolWith (size : Size) (seed : Seed) (config : PropertyConfig) (p : Property<bool>) : Report =
        p |> bind ofBool |> reportRecheckWith size seed config

    let reportRecheckBool (size : Size) (seed : Seed) (p : Property<bool>) : Report =
        p |> bind ofBool |> reportRecheck size seed

    let recheckWith (size : Size) (seed : Seed) (config : PropertyConfig) (p : Property<unit>) : unit =
        reportRecheckWith size seed config p
        |> Report.tryRaise

    let recheck (size : Size) (seed : Seed) (p : Property<unit>) : unit =
        reportRecheck size seed p
        |> Report.tryRaise

    let recheckBoolWith (size : Size) (seed : Seed) (config : PropertyConfig) (g : Property<bool>) : unit =
        g |> bind ofBool |> recheckWith size seed config

    let recheckBool (size : Size) (seed : Seed) (g : Property<bool>) : unit =
        g |> bind ofBool |> recheck size seed

    let printWith (config : PropertyConfig) (p : Property<unit>) : unit =
        reportWith config p
        |> Report.render
        |> printfn "%s"

    let print (p : Property<unit>) : unit =
        report p
        |> Report.render
        |> printfn "%s"

    let printBoolWith (config : PropertyConfig) (p : Property<bool>) : unit =
        reportBoolWith config p
        |> Report.render
        |> printfn "%s"

    let printBool (p : Property<bool>) : unit =
        reportBool p
        |> Report.render
        |> printfn "%s"

[<AutoOpen>]
module PropertyBuilder =
    let rec private loop (p : unit -> bool) (m : Property<unit>) : Property<unit> =
        if p () then
            m |> Property.bind (fun _ -> loop p m)
        else
            Property.success ()

    type Builder internal () =
        member __.For(m : Property<'a>, k : 'a -> Property<'b>) : Property<'b> =
            m |> Property.bind k

        member __.For(xs : #seq<'a>, k : 'a -> Property<unit>) : Property<unit> =
            let xse = xs.GetEnumerator ()
            Property.using xse (fun xse ->
                let mv = xse.MoveNext
                let kc = Property.delay (fun () -> k xse.Current)
                loop mv kc)

        member __.While(p : unit -> bool, m : Property<unit>) : Property<unit> =
            loop p m

        member __.Yield(x : 'a) : Property<'a> =
            Property.success x

        member __.Combine(m : Property<unit>, n : Property<'a>) : Property<'a> =
            m |> Property.bind (fun _ -> n)

        member __.TryFinally(m : Property<'a>, after : unit -> unit) : Property<'a> =
            m |> Property.tryFinally after

        member __.TryWith(m : Property<'a>, k : exn -> Property<'a>) : Property<'a> =
            m |> Property.tryWith k

        member __.Using(a : 'a, k : 'a -> Property<'b>) : Property<'b> when
                'a :> IDisposable and
                'a : null =
            Property.using a k

        member __.Bind(m : Gen<'a>, k : 'a -> Property<'b>) : Property<'b> =
            m |> Property.forAll k

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
            m |> Property.bind (fun x ->
                Property.counterexample (fun () -> f x)
                |> Property.map (fun () -> x))

        [<CustomOperation("where", MaintainsVariableSpace = true)>]
        member __.Where(m : Property<'a>, [<ProjectionParameter>] p : 'a -> bool) : Property<'a> =
            Property.filter p m

    let property = Builder ()
