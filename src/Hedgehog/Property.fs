namespace Hedgehog

open System

[<Struct>]
type Property =
    | Property of Gen<Journal * Outcome>

type PropertyConfig = internal {
    TestLimit : int<tests>
    ShrinkLimit : int<shrinks> option
}

module PropertyConfig =

    /// The default configuration for a property test.
    let defaultConfig : PropertyConfig =
        { TestLimit = 100<tests>
          ShrinkLimit = None }

    /// Set the number of times a property is allowed to shrink before the test
    /// runner gives up and prints the counterexample.
    let withShrinks (shrinkLimit : int<shrinks>) (config : PropertyConfig) : PropertyConfig =
        { config with ShrinkLimit = Some shrinkLimit }

    /// Restores the default shrinking behavior.
    let withoutShrinks (config : PropertyConfig) : PropertyConfig =
        { config with ShrinkLimit = None }

    /// Set the number of times a property should be executed before it is
    /// considered successful.
    let withTests (testLimit : int<tests>) (config : PropertyConfig) : PropertyConfig =
        { config with TestLimit = testLimit }

module Property =

    let ofGen (x : Gen<Journal * Outcome>) : Property =
        Property x

    let toGen (Property x : Property) : Gen<Journal * Outcome> =
        x

    let tryFinally (after : unit -> unit) (m : Property) : Property =
        Gen.tryFinally after (toGen m) |> ofGen

    let tryWith (k : exn -> Property) (m : Property) : Property =
        Gen.tryWith (toGen << k) (toGen m) |> ofGen

    let delay (f : unit -> Property) : Property =
        Gen.delay (toGen << f) |> ofGen

    let using (x : 'a) (k : 'a -> Property) : Property
        when
            'a :> IDisposable and
            'a : null =
        delay (fun () -> k x)
        |> tryFinally (fun () ->
            match x with
            | null ->
                ()
            | _ ->
                x.Dispose ())

    let ofOutcome (x : Outcome) : Property =
        (Journal.empty, x) |> Gen.constant |> ofGen

    let failure : Property =
        Failure |> ofOutcome

    let discard : Property =
        Discard |> ofOutcome

    let success : Property =
        Success |> ofOutcome

    let ofBool (x : bool) : Property =
        if x then
            success
        else
            failure

    let counterexample (msg : unit -> string) : Property =
        Gen.constant (Journal.singleton msg, Success) |> ofGen

    let private mapGen
            (f : Gen<Journal * Outcome> -> Gen<Journal * Outcome>)
            (x : Property) : Property =
        toGen x |> f |> ofGen

    let private bindGen
            (k : unit -> Gen<Journal * Outcome>)
            (m : Gen<Journal * Outcome>) : Gen<Journal * Outcome> =
        m |> Gen.bind (fun (journal, result) ->
            match result with
            | Failure ->
                Gen.constant (journal, Failure)
            | Discard ->
                Gen.constant (journal, Discard)
            | Success ->
                GenTuple.mapFst (Journal.append journal) (k ()))

    let bind (k : unit -> Property) (m : Property) : Property =
        bindGen (toGen << k) (toGen m) |> ofGen

    let forAll (k : 'a -> Property) (gen : Gen<'a>) : Property =
        let handle (e : exn) =
            Gen.constant (Journal.singletonMessage (string e), Failure) |> ofGen
        let prepend (x : 'a) =
            counterexample (fun () -> sprintf "%A" x)
            |> bind (fun () -> try k x with e -> handle e)
            |> toGen

        gen |> Gen.bind prepend |> ofGen

    let forAll' (gen : Gen<'a>) : Property =
        gen |> forAll (fun _ -> success)

    //
    // Runner
    //

    let rec private takeSmallest
            (renderRecheck : bool)
            (size : Size)
            (seed : Seed)
            (Node ((journal, x), xs) : Tree<Journal * Outcome>)
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

    let private reportWith' (renderRecheck : bool) (size0 : Size) (seed : Seed) (config : PropertyConfig) (p : Property) : Report =
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
                | Success _ ->
                    loop seed2 (nextSize size) (tests + 1<tests>) discards
                | Discard ->
                    loop seed2 (nextSize size) tests (discards + 1<discards>)

        loop seed size0 0<tests> 0<discards>

    let reportWith (config : PropertyConfig) (p : Property) : Report =
        let seed = Seed.random ()
        p |> reportWith' true 1 seed config

    let report (p : Property) : Report =
        p |> reportWith PropertyConfig.defaultConfig

    let checkWith (config : PropertyConfig) (p : Property) : unit =
        reportWith config p
        |> Report.tryRaise

    let check (p : Property) : unit =
        report p
        |> Report.tryRaise

    /// Converts a possibly-throwing function to
    /// a property by treating "no exception" as success.
    let ofThrowing (f : 'a -> unit) (x : 'a) : Property =
        try
            f x
            success
        with
        | _ -> failure

    let reportRecheckWith (size : Size) (seed : Seed) (config : PropertyConfig) (p : Property) : Report =
        reportWith' false size seed config p

    let reportRecheck (size : Size) (seed : Seed) (p : Property) : Report =
        reportWith' false size seed PropertyConfig.defaultConfig p

    let recheckWith (size : Size) (seed : Seed) (config : PropertyConfig) (p : Property) : unit =
        reportRecheckWith size seed config p
        |> Report.tryRaise

    let recheck (size : Size) (seed : Seed) (p : Property) : unit =
        reportRecheck size seed p
        |> Report.tryRaise

    let printWith (config : PropertyConfig) (p : Property) : unit =
        reportWith config p
        |> Report.render
        |> printfn "%s"

    let print (p : Property) : unit =
        report p
        |> Report.render
        |> printfn "%s"

[<AutoOpen>]
module PropertyBuilder =
    let rec private loop (p : unit -> bool) (m : Property) : Property =
        if p () then
            m |> Property.bind (fun _ -> loop p m)
        else
            Property.success

    type Builder internal () =
        member __.For(m : Property, k : unit -> Property) : Property =
            m |> Property.bind k

        member __.For(xs : #seq<'a>, k : 'a -> Property) : Property =
            let xse = xs.GetEnumerator ()
            Property.using xse (fun xse ->
                let mv = xse.MoveNext
                let kc = Property.delay (fun () -> k xse.Current)
                loop mv kc)

        member __.While(p : unit -> bool, m : Property) : Property =
            loop p m

        member __.Yield(_) : Property =
            Property.success

        member __.Combine(m : Property, n : Property) : Property =
            m |> Property.bind (fun _ -> n)

        member __.TryFinally(m : Property, after : unit -> unit) : Property =
            m |> Property.tryFinally after

        member __.TryWith(m : Property, k : exn -> Property) : Property =
            m |> Property.tryWith k

        member __.Using(a : 'a, k : 'a -> Property) : Property
            when
                'a :> IDisposable and
                'a : null =
            Property.using a k

        member __.Bind(m : Gen<'a>, k : 'a -> Property) : Property =
            m |> Property.forAll k

        member __.Return(b : bool) : Property =
            Property.ofBool b

        member __.ReturnFrom(m : Property) : Property =
            m

        member __.Delay(f : unit -> Property) : Property =
            Property.delay f

        member __.Zero() : Property =
            Property.success

        [<CustomOperation("counterexample", MaintainsVariableSpace = true)>]
        member __.Counterexample(property : Property, [<ProjectionParameter>] f : unit -> string) : Property =
            property
            |> Property.bind (fun () -> Property.counterexample f)

    let property = Builder ()
