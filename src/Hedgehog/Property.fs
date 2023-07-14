namespace Hedgehog

open System


[<Struct>]
type Property<'a> =
    | Property of Gen<Lazy<Journal * Outcome<'a>>>


module Property =

    let ofGen (x : Gen<Lazy<Journal * Outcome<'a>>>) : Property<'a> =
        Property x

    let toGen (Property x : Property<'a>) : Gen<Lazy<Journal * Outcome<'a>>> =
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
        m |> toGen |> GenLazyTuple.mapSnd (Outcome.filter p) |> ofGen

    let ofOutcome (x : Outcome<'a>) : Property<'a> =
        (Journal.empty, x) |> GenLazy.constant |> ofGen

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
        (Journal.singleton msg, Success ()) |> GenLazy.constant |> ofGen

    let map (f : 'a -> 'b) (x : Property<'a>) : Property<'b> =
        let g (j, outcome) =
            try
                (j, outcome |> Outcome.map f)
            with e ->
                (Journal.append j (Journal.singletonMessage (string e)), Failure)
        x |> toGen |> GenLazy.map g |> ofGen

    let internal set (a: 'a) (property : Property<'b>) : Property<'a> =
        property |> map (fun _ -> a)

    let private bindGen
            (f : 'a -> Gen<Lazy<Journal * Outcome<'b>>>)
            (m : Gen<Lazy<Journal * Outcome<'a>>>) : Gen<Lazy<Journal * Outcome<'b>>> =
        m |> GenLazy.bind (fun (journal, result) ->
            match result with
            | Failure ->
                GenLazy.constant (journal, Failure)
            | Discard ->
                GenLazy.constant (journal, Discard)
            | Success a ->
                GenLazyTuple.mapFst (Journal.append journal) (f a))

    let bind (k : 'a -> Property<'b>) (m : Property<'a>) : Property<'b> =
        let kTry a =
            try
                k a |> toGen
            with e ->
                (Journal.singletonMessage (string e), Failure) |> GenLazy.constant
        m
        |> toGen
        |> bindGen kTry
        |> ofGen

    let falseToFailure p =
        p |> bind ofBool

    let internal printValue (value) : string =
        // sprintf "%A" is not prepared for printing ResizeArray<_> (C# List<T>) so we prepare the value instead
        let prepareForPrinting (value: obj) : obj =
        #if FABLE_COMPILER
            value
        #else
            if value = null then
                value
            else
                let t = value.GetType()
                // have to use TypeInfo due to targeting netstandard 1.6
                let t = System.Reflection.IntrospectionExtensions.GetTypeInfo(t)
                let isList = t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<ResizeArray<_>>
                if isList
                then value :?> System.Collections.IEnumerable |> Seq.cast<obj> |> List.ofSeq :> obj
                else value
        #endif

        value |> prepareForPrinting |> sprintf "%A"

    let forAll (k : 'a -> Property<'b>) (gen : Gen<'a>) : Property<'b> =
        let prepend (x : 'a) =
            counterexample (fun () -> printValue x)
            |> set x
            |> bind k
            |> toGen

        gen |> Gen.bind prepend |> ofGen

    let forAll' (gen : Gen<'a>) : Property<'a> =
        gen |> forAll success

    //
    // Runner
    //

    let private shrinkInput
            (language: Language)
            (data : RecheckData)
            (shrinkLimit : int<shrinks> Option) =
        let rec loop
                (nshrinks : int<shrinks>)
                (shrinkPathRev : ShrinkOutcome list)
                (Node (root, xs) : Tree<Lazy<Journal * Outcome<'a>>>) =
            let getFailed () =
                Failed {
                    Shrinks = nshrinks
                    Journal = root.Value |> fst
                    RecheckInfo =
                        Some { Language = language
                               Data = { data with ShrinkPath = List.rev shrinkPathRev } } }
            match shrinkLimit with
            | Some shrinkLimit' when nshrinks >= shrinkLimit' -> getFailed ()
            | _ ->
                match xs |> Seq.indexed |> Seq.tryFind (snd >> Tree.outcome >> Lazy.value >> snd >> Outcome.isFailure) with
                | None -> getFailed ()
                | Some (idx, tree) ->
                    let nextShrinkPathRev = ShrinkOutcome.Pass idx :: shrinkPathRev
                    loop (nshrinks + 1<shrinks>) nextShrinkPathRev tree
        loop 0<shrinks> []

    let rec private followShrinkPath
            (Node (root, children) : Tree<Lazy<Journal * Outcome<'a>>>)
            shrinkPath =
        match shrinkPath with
        | [] ->
            let journal, outcome = root.Value
            match outcome with
            | Failure ->
                { Shrinks = 0<shrinks>
                  Journal = journal
                  RecheckInfo = None }
                |> Failed
            | Success _ -> OK
            | Discard -> failwith "Unexpected 'Discard' result when rechecking. This should never happen."
        | ShrinkOutcome.Pass i :: shinkPathTail ->
            let nextRoot =
                children
                |> Seq.skip i
                |> Seq.tryHead
                |> Option.defaultWith (fun () -> failwith "The shrink path lead to a dead end, which means the generators have changed. Thus, 'recheck' is not possible. Use 'check' instead.")
            followShrinkPath nextRoot shinkPathTail

    let private splitAndRun p data =
        let seed1, seed2 = Seed.split data.Seed
        let result = p |> toGen |> Gen.toRandom |> Random.run seed1 data.Size
        result, seed2

    let private reportWith' (args : PropertyArgs) (config : PropertyConfig) (p : Property<unit>) : Report =
        let nextSize size =
            if size >= 100 then
                1
            else
                size + 1

        let rec loop data tests discards =
            if tests = config.TestLimit then
                { Tests = tests
                  Discards = discards
                  Status = OK }
            elif discards >= 100<discards> then
                { Tests = tests
                  Discards = discards
                  Status = GaveUp }
            else
                let result, seed2 = splitAndRun p data
                let nextData = {
                    data with
                        Seed = seed2
                        Size = nextSize data.Size
                }

                match snd (Tree.outcome result).Value with
                | Failure ->
                    { Tests = tests + 1<tests>
                      Discards = discards
                      Status = shrinkInput args.Language data config.ShrinkLimit result }
                | Success () ->
                    loop nextData (tests + 1<tests>) discards
                | Discard ->
                    loop nextData tests (discards + 1<discards>)

        loop args.RecheckData 0<tests> 0<discards>

    let reportWith (config : PropertyConfig) (p : Property<unit>) : Report =
        p |> reportWith' PropertyArgs.init config

    let report (p : Property<unit>) : Report =
        p |> reportWith PropertyConfig.defaultConfig

    let reportBoolWith (config : PropertyConfig) (p : Property<bool>) : Report =
        p |> falseToFailure |> reportWith config

    let reportBool (p : Property<bool>) : Report =
        p |> falseToFailure |> report

    let checkWith (config : PropertyConfig) (p : Property<unit>) : unit =
        p |> reportWith config |> Report.tryRaise

    let check (p : Property<unit>) : unit =
        p |> report |> Report.tryRaise

    let checkBool (g : Property<bool>) : unit =
        g |> falseToFailure |> check

    let checkBoolWith (config : PropertyConfig) (g : Property<bool>) : unit =
        g |> falseToFailure |> checkWith config

    let reportRecheckWith (recheckData: string) (config : PropertyConfig) (p : Property<unit>) : Report =
        let recheckData = recheckData |> RecheckData.deserialize
        let result, _ = splitAndRun p recheckData
        { Tests = 1<tests>
          Discards = 0<discards>
          Status = followShrinkPath result recheckData.ShrinkPath }

    let reportRecheck (recheckData: string) (p : Property<unit>) : Report =
        p |> reportRecheckWith recheckData PropertyConfig.defaultConfig

    let reportRecheckBoolWith (recheckData: string) (config : PropertyConfig) (p : Property<bool>) : Report =
        p |> falseToFailure |> reportRecheckWith recheckData config

    let reportRecheckBool (recheckData: string) (p : Property<bool>) : Report =
        p |> falseToFailure |> reportRecheck recheckData

    let recheckWith (recheckData: string) (config : PropertyConfig) (p : Property<unit>) : unit =
        p |> reportRecheckWith recheckData config |> Report.tryRaise

    let recheck (recheckData: string) (p : Property<unit>) : unit =
        p |> reportRecheck recheckData |> Report.tryRaise

    let recheckBoolWith (recheckData: string) (config : PropertyConfig) (g : Property<bool>) : unit =
        g |> falseToFailure |> recheckWith recheckData config

    let recheckBool (recheckData: string) (g : Property<bool>) : unit =
        g |> falseToFailure |> recheck recheckData

    let renderWith (n : PropertyConfig) (p : Property<unit>) : string =
        p |> reportWith n |> Report.render

    let render (p : Property<unit>) : string =
        p |> report |> Report.render

    let renderBool (property : Property<bool>) : string =
        property |> falseToFailure |> render

    let renderBoolWith (config : PropertyConfig) (p : Property<bool>) : string =
        p |> falseToFailure |> renderWith config


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

        member __.BindReturn(m : Gen<'a>, f: 'a -> 'b) =
            m
            |> Gen.map (fun a -> Lazy.constant ((Journal.singleton (fun () -> Property.printValue a)), Success a))
            |> Property.ofGen
            |> Property.map f

        member __.MergeSources(ga, gb) =
            Gen.zip ga gb

        member __.ReturnFrom(m : Property<'a>) : Property<'a> =
            m

        member __.Delay(f : unit -> Property<'a>) : Property<'a> =
            Property.delay f

        member __.Zero() : Property<unit> =
            Property.success ()

        [<CustomOperation("counterexample", MaintainsVariableSpace = true)>]
        member __.Counterexample(m : Property<'a>, [<ProjectionParameter>] f : 'a -> string) : Property<'a> =
            m |> Property.bind (fun a ->
                Property.counterexample (fun () -> f a)
                |> Property.set a)

        [<CustomOperation("where", MaintainsVariableSpace = true)>]
        member __.Where(m : Property<'a>, [<ProjectionParameter>] p : 'a -> bool) : Property<'a> =
            Property.filter p m

    let property = Builder ()
