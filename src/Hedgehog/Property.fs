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
        (Journal.singleton msg, Success ()) |> Gen.constant |> ofGen

    let private mapGen
            (f : Gen<Journal * Outcome<'a>> -> Gen<Journal * Outcome<'b>>)
            (p : Property<'a>) : Property<'b> =
        p |> toGen |> f |> ofGen

    let map (f : 'a -> 'b) (x : Property<'a>) : Property<'b> =
        let g (j, outcome) =
            try
                (j, outcome |> Outcome.map f)
            with e ->
                (Journal.append j (Journal.singletonMessage (string e)), Failure)
        let h = g |> Gen.map |> mapGen
        h x

    let internal set (a: 'a) (property : Property<'b>) : Property<'a> =
        property |> map (fun _ -> a)

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
        let kTry a =
            try
                k a |> toGen
            with e ->
                (Journal.singletonMessage (string e), Failure) |> Gen.constant
        m
        |> toGen
        |> bindGen kTry
        |> ofGen

    let failOnFalse p =
        p |> bind ofBool

    let private printValue (value) : string =
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
            (args : PropertyArgs)
            (shrinkLimit : int<shrinks> Option) =
        let rec loop
                (nshrinks : int<shrinks>)
                (Node ((journal, _), xs) : Tree<Journal * Outcome<'a>>) =
            let failed =
                Failed {
                    Size = args.Size
                    Seed = args.Seed
                    Shrinks = nshrinks
                    Journal = journal
                    RecheckType = args.RecheckType
                }
            match shrinkLimit, Seq.tryFind (Tree.outcome >> snd >> Outcome.isFailure) xs with
            | Some shrinkLimit', _ when nshrinks >= shrinkLimit' -> failed
            | _, None -> failed
            | _, Some tree -> loop (nshrinks + 1<shrinks>) tree
        loop 0<shrinks>

    let private reportWith' (args : PropertyArgs) (config : PropertyConfig) (p : Property<unit>) : Report =
        let random = toGen p |> Gen.toRandom

        let nextSize size =
            if size >= 100 then
                1
            else
                size + 1

        let rec loop args tests discards =
            if tests = config.TestLimit then
                { Tests = tests
                  Discards = discards
                  Status = OK }
            elif discards >= 100<discards> then
                { Tests = tests
                  Discards = discards
                  Status = GaveUp }
            else
                let seed1, seed2 = Seed.split args.Seed
                let result = Random.run seed1 args.Size random
                let nextArgs = {
                    args with
                        Seed = seed2
                        Size = nextSize args.Size
                }

                match snd (Tree.outcome result) with
                | Failure ->
                    { Tests = tests + 1<tests>
                      Discards = discards
                      Status = shrinkInput args config.ShrinkLimit result }
                | Success () ->
                    loop nextArgs (tests + 1<tests>) discards
                | Discard ->
                    loop nextArgs tests (discards + 1<discards>)

        loop args 0<tests> 0<discards>

    let reportWith (config : PropertyConfig) (p : Property<unit>) : Report =
        p |> reportWith' PropertyArgs.init config

    let report (p : Property<unit>) : Report =
        p |> reportWith PropertyConfig.defaultConfig

    let reportBoolWith (config : PropertyConfig) (p : Property<bool>) : Report =
        p |> failOnFalse |> reportWith config

    let reportBool (p : Property<bool>) : Report =
        p |> failOnFalse |> report

    let checkWith (config : PropertyConfig) (p : Property<unit>) : unit =
        p |> reportWith config |> Report.tryRaise

    let check (p : Property<unit>) : unit =
        p |> report |> Report.tryRaise

    let checkBool (g : Property<bool>) : unit =
        g |> failOnFalse |> check

    let checkBoolWith (config : PropertyConfig) (g : Property<bool>) : unit =
        g |> failOnFalse |> checkWith config

    let reportRecheckWith (size : Size) (seed : Seed) (config : PropertyConfig) (p : Property<unit>) : Report =
        let args = {
            PropertyArgs.init with
                RecheckType = RecheckType.None
                Seed = seed
                Size = size
        }
        p |> reportWith' args config

    let reportRecheck (size : Size) (seed : Seed) (p : Property<unit>) : Report =
        p |> reportRecheckWith size seed PropertyConfig.defaultConfig

    let reportRecheckBoolWith (size : Size) (seed : Seed) (config : PropertyConfig) (p : Property<bool>) : Report =
        p |> failOnFalse |> reportRecheckWith size seed config

    let reportRecheckBool (size : Size) (seed : Seed) (p : Property<bool>) : Report =
        p |> failOnFalse |> reportRecheck size seed

    let recheckWith (size : Size) (seed : Seed) (config : PropertyConfig) (p : Property<unit>) : unit =
        p |> reportRecheckWith size seed config |> Report.tryRaise

    let recheck (size : Size) (seed : Seed) (p : Property<unit>) : unit =
        p |> reportRecheck size seed |> Report.tryRaise

    let recheckBoolWith (size : Size) (seed : Seed) (config : PropertyConfig) (g : Property<bool>) : unit =
        g |> failOnFalse |> recheckWith size seed config

    let recheckBool (size : Size) (seed : Seed) (g : Property<bool>) : unit =
        g |> failOnFalse |> recheck size seed

    let renderWith (n : PropertyConfig) (p : Property<unit>) : string =
        p |> reportWith n |> Report.render

    let render (p : Property<unit>) : string =
        p |> report |> Report.render

    let renderBool (property : Property<bool>) : string =
        property |> failOnFalse |> render

    let renderBoolWith (config : PropertyConfig) (p : Property<bool>) : string =
        p |> failOnFalse |> renderWith config


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
            m |> Property.bind (fun a ->
                Property.counterexample (fun () -> f a)
                |> Property.set a)

        [<CustomOperation("where", MaintainsVariableSpace = true)>]
        member __.Where(m : Property<'a>, [<ProjectionParameter>] p : 'a -> bool) : Property<'a> =
            Property.filter p m

    let property = Builder ()
