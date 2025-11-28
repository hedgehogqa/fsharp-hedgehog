namespace Hedgehog



type internal TestReturnedFalseException() =
  inherit System.Exception("Expected 'true' but was 'false'.")


[<Struct>]
type Property<'a> =
    internal Property of Gen<Lazy<PropertyResult<'a>>>

namespace Hedgehog.FSharp

open System
open Hedgehog

module Property =

    // Internal helpers to convert between PropertyResult and plain tuples
    // This allows us to keep using existing GenLazy/GenLazyTuple helpers
    let private wrapSync (gen : Gen<Lazy<Journal * Outcome<'a>>>) : Gen<Lazy<PropertyResult<'a>>> =
        gen |> Gen.map (Lazy.map PropertyResult.Sync)

    let private unwrapSync (gen : Gen<Lazy<PropertyResult<'a>>>) : Gen<Lazy<Journal * Outcome<'a>>> =
        gen |> Gen.map (fun lazyResult ->
            lazy (
                match lazyResult.Value with
                | PropertyResult.Sync (journal, outcome) -> (journal, outcome)
                | PropertyResult.Async asyncResult ->
#if FABLE_COMPILER
                    failwith "Synchronous unwrapping of async PropertyResult is not supported in Fable. Use Property.checkAsync or Property.reportAsync instead of Property.check or Property.report."
#else
                    Async.RunSynchronously asyncResult  // Blocking for now
#endif
                ))

    let ofGen (x : Gen<Lazy<Journal * Outcome<'a>>>) : Property<'a> =
        Property (wrapSync x)

    let toGen (Property x : Property<'a>) : Gen<Lazy<Journal * Outcome<'a>>> =
        unwrapSync x

    // Internal version that doesn't unwrap - keeps PropertyResult
    let private toGenInternal (Property x : Property<'a>) : Gen<Lazy<PropertyResult<'a>>> =
        x

    let tryFinally (after : unit -> unit) (m : Property<'a>) : Property<'a> =
        Gen.tryFinally after (toGenInternal m) |> Property

    let tryWith (k : exn -> Property<'a>) (m : Property<'a>) : Property<'a> =
        Gen.tryWith (toGenInternal << k) (toGenInternal m) |> Property

    let delay (f : unit -> Property<'a>) : Property<'a> =
        Gen.delay (toGenInternal << f) |> Property

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

#if !FABLE_COMPILER
    /// Converts a Task<'T> into a Property<'T>.
    /// Evaluates the task and makes its result available to property combinators.
    /// The property succeeds if the task completes successfully,
    /// fails if the task throws an exception or is canceled.
    let ofTask (inputTask : System.Threading.Tasks.Task<'T>) : Property<'T> =
        Gen.constant (lazy (
            PropertyResult.Async (async {
                try
                    let! result = Async.AwaitTask inputTask
                    return (Journal.empty, Success result)
                with
                | :? System.OperationCanceledException ->
                    return (Journal.singletonMessage "Task was canceled", Failure)
                | ex ->
                    return (Journal.singletonMessage (string ex), Failure)
            })))
        |> Property

    /// Converts a non-generic Task into a Property<unit>.
    /// Helper for tasks that don't return a value.
    let ofTaskUnit (inputTask : System.Threading.Tasks.Task) : Property<unit> =
        Gen.constant (lazy (
            PropertyResult.Async (async {
                try
                    do! Async.AwaitTask inputTask
                    return (Journal.empty, Success ())
                with
                | :? System.OperationCanceledException ->
                    return (Journal.singletonMessage "Task was canceled", Failure)
                | ex ->
                    return (Journal.singletonMessage (string ex), Failure)
            })))
        |> Property
#endif

    /// Converts an Async<'T> into a Property<'T>.
    /// The async computation is wrapped in a PropertyResult.Async.
    let ofAsync (asyncComputation : Async<'T>) : Property<'T> =
        Gen.constant (lazy (PropertyResult.ofAsyncWith asyncComputation))
        |> Property

    let filter (p : 'a -> bool) (m : Property<'a>) : Property<'a> =
        m |> toGenInternal |> Gen.map (Lazy.map (PropertyResult.map (fun (j, o) -> (j, Outcome.filter p o)))) |> Property

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
        let applyWithExceptionHandling (j, outcome) =
            try
                (j, outcome |> Outcome.map f)
            with 
            | :? TestReturnedFalseException ->
                // Don't include internal exception in journal - it's just a signal
                (j, Failure)
            | e ->
                let unwrapped = Exceptions.unwrap e
                (Journal.append j (Journal.singletonMessage (string unwrapped)), Failure)

        let g (lazyResult : Lazy<PropertyResult<'a>>) =
            lazy (PropertyResult.map applyWithExceptionHandling lazyResult.Value)

        x |> toGenInternal |> Gen.map g |> Property

    let internal set (a: 'a) (property : Property<'b>) : Property<'a> =
        property |> map (fun _ -> a)

    let private bindGen
            (f : 'a -> Gen<Lazy<PropertyResult<'b>>>)
            (m : Gen<Lazy<PropertyResult<'a>>>) : Gen<Lazy<PropertyResult<'b>>> =

        // Evaluate the next property and combine journals
        let evalNext (journal : Journal) (a : 'a) : PropertyResult<'b> =
            let nextTree = f a |> Gen.toRandom |> Random.run (Seed.from 0UL) 0
            let nextResult = (Tree.outcome nextTree).Value
            PropertyResult.map (fun (j2, outcome2) -> (Journal.append journal j2, outcome2)) nextResult

        // Handle the outcome after getting the result
        let handleOutcome (journal : Journal) (outcome : Outcome<'a>) : PropertyResult<'b> =
            match outcome with
            | Failure -> PropertyResult.ofSync journal Failure
            | Discard -> PropertyResult.ofSync journal Discard
            | Success a -> evalNext journal a

        m |> Gen.bind (fun lazyResult ->
            Gen.constant (lazy (
                lazyResult.Value 
                |> PropertyResult.bind (fun (journal, outcome) ->
                    handleOutcome journal outcome))))

    /// Monadic bind operation for properties.
    /// Applies a property-returning function to the result of another property,
    /// sequencing their execution and combining their journals.
    let bind (k : 'a -> Property<'b>) (m : Property<'a>) : Property<'b> =
        let kTry a =
            try
                k a |> toGenInternal
            with e ->
                PropertyResult.ofSync (Journal.singletonMessage (string e)) Failure
                |> GenLazy.constant
        m
        |> toGenInternal
        |> bindGen kTry
        |> Property

    /// Binds a generator to a property-returning function while adding custom journal entries.
    /// This allows you to add contextual information (like formatted parameter names and values)
    /// that will appear in test failure reports before the property's own journal entries.
    /// Use this when the property function returns Property<'b> and you want to enhance
    /// the failure output with information about the generated input values.
    let bindWith (journalFrom : 'a -> Journal) (k : 'a -> Property<'b>) (m : Gen<'a>) : Property<'b> =
        m
        |> Gen.bind (fun a -> 
            let customJournal = journalFrom a
            let innerProperty = k a
            innerProperty 
            |> toGenInternal
            |> Gen.map (fun lazyResult ->
                lazy (
                    lazyResult.Value
                    |> PropertyResult.map (fun (j, outcome) ->
                        (Journal.append customJournal j, outcome)))))
        |> Property

    /// Binds a generator to a value-returning function while adding custom journal entries.
    /// This allows you to add contextual information (like formatted parameter names and values)
    /// that will appear in test failure reports. The function's return value is automatically
    /// checked for success (e.g., awaiting tasks, validating booleans/Results, handling exceptions).
    /// Use this for test functions that don't return Property<'b> - instead they return plain values,
    /// Task, Async, bool, Result, etc. The function is wrapped with exception handling via Property.map.
    let bindReturnWith (journalFrom : 'a -> Journal) (f: 'a -> 'b) (m : Gen<'a>) : Property<'b> =
        m
        |> Gen.map (fun a -> Lazy.constant ((journalFrom a), Success a))
        |> ofGen
        |> map f

    let falseToFailure p =
        p |> map (fun b -> if not b then raise (TestReturnedFalseException()))

    let internal printValue value : string =
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
            |> toGenInternal

        gen |> Gen.bind prepend |> Property

    let forAll' (gen : Gen<'a>) : Property<'a> =
        gen |> forAll success

    //
    // Shrinking
    //

    /// Module containing shrinking logic for property test failures
    module private Shrinking =

        /// Shrink a failing test synchronously, finding the smallest input that still fails
        let shrinkSync
                (language: Language)
                (data : RecheckData)
                (shrinkLimit : int<shrinks> Option)
                (tree : Tree<Lazy<PropertyResult<'a>>>) : Status =
            let rec loop
                    (nshrinks : int<shrinks>)
                    (shrinkPathRev : ShrinkOutcome list)
                    (Node (root, xs)) =
                let getFailed () =
                    Failed {
                        Shrinks = nshrinks
                        Journal = PropertyResult.unwrapSync root |> fst
                        RecheckInfo =
                            Some { Language = language
                                   Data = { data with ShrinkPath = List.rev shrinkPathRev } } }
                match shrinkLimit with
                | Some shrinkLimit' when nshrinks >= shrinkLimit' -> getFailed ()
                | _ ->
                    match xs |> Seq.indexed |> Seq.tryFind (snd >> Tree.outcome >> PropertyResult.unwrapSync >> snd >> Outcome.isFailure) with
                    | None -> getFailed ()
                    | Some (idx, tree) ->
                        let nextShrinkPathRev = ShrinkOutcome.Pass idx :: shrinkPathRev
                        loop (nshrinks + 1<shrinks>) nextShrinkPathRev tree
            loop 0<shrinks> [] tree

        /// Shrink a failing test asynchronously, finding the smallest input that still fails
        let shrinkAsync
                (language: Language)
                (data : RecheckData)
                (shrinkLimit : int<shrinks> Option)
                (tree : Tree<Lazy<PropertyResult<'a>>>) : Async<Status> =
            let rec loop
                    (nshrinks : int<shrinks>)
                    (shrinkPathRev : ShrinkOutcome list)
                    (Node (root, xs)) = async {
                let getFailed () = async {
                    let! journal, _ = PropertyResult.unwrapAsync root
                    return Failed {
                        Shrinks = nshrinks
                        Journal = journal
                        RecheckInfo =
                            Some { Language = language
                                   Data = { data with ShrinkPath = List.rev shrinkPathRev } } }
                }
                match shrinkLimit with
                | Some shrinkLimit' when nshrinks >= shrinkLimit' -> return! getFailed ()
                | _ ->
                    let rec findFirstFailure trees = async {
                        match trees with
                        | [] -> return None
                        | (idx, tree) :: rest ->
                            let! _, outcome = PropertyResult.unwrapAsync (Tree.outcome tree)
                            if Outcome.isFailure outcome then
                                return Some (idx, tree)
                            else
                                return! findFirstFailure rest
                    }

                    let! found = xs |> Seq.indexed |> List.ofSeq |> findFirstFailure
                    match found with
                    | None -> return! getFailed ()
                    | Some (idx, tree) ->
                        let nextShrinkPathRev = ShrinkOutcome.Pass idx :: shrinkPathRev
                        return! loop (nshrinks + 1<shrinks>) nextShrinkPathRev tree
            }
            loop 0<shrinks> [] tree

        /// Follow a previously recorded shrink path to replay a failure
        let followPath (tree : Tree<Lazy<PropertyResult<'a>>>) (shrinkPath : ShrinkOutcome list) : Status =
            let rec loop (Node (root, children)) path =
                match path with
                | [] ->
                    let journal, outcome = PropertyResult.unwrapSync root
                    match outcome with
                    | Failure ->
                        { Shrinks = 0<shrinks>
                          Journal = journal
                          RecheckInfo = None }
                        |> Failed
                    | Success _ -> OK
                    | Discard -> failwith "Unexpected 'Discard' result when rechecking. This should never happen."
                | ShrinkOutcome.Pass i :: pathTail ->
                    let nextRoot =
                        children
                        |> Seq.skip i
                        |> Seq.tryHead
                        |> Option.defaultWith (fun () -> failwith "The shrink path lead to a dead end, which means the generators have changed. Thus, 'recheck' is not possible. Use 'check' instead.")
                    loop nextRoot pathTail
            loop tree shrinkPath

    //
    // Runner
    //

    let private splitAndRun p data =
        let seed1, seed2 = Seed.split data.Seed
        let result = p |> toGenInternal |> Gen.toRandom |> Random.run seed1 data.Size
        result, seed2

    let private reportWith' (args : PropertyArgs) (config : IPropertyConfig) (p : Property<unit>) : Report =
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

                match snd (PropertyResult.unwrapSync (Tree.outcome result)) with
                | Failure ->
                    { Tests = tests + 1<tests>
                      Discards = discards
                      Status = Shrinking.shrinkSync args.Language data config.ShrinkLimit result }
                | Success () ->
                    loop nextData (tests + 1<tests>) discards
                | Discard ->
                    loop nextData tests (discards + 1<discards>)

        loop args.RecheckData 0<tests> 0<discards>

    let reportWith (config : IPropertyConfig) (p : Property<unit>) : Report =
        p |> reportWith' PropertyArgs.init config

    let report (p : Property<unit>) : Report =
        p |> reportWith PropertyConfig.defaults

    let reportBoolWith (config : IPropertyConfig) (p : Property<bool>) : Report =
        p |> falseToFailure |> reportWith config

    let reportBool (p : Property<bool>) : Report =
        p |> falseToFailure |> report

    let checkWith (config : IPropertyConfig) (p : Property<unit>) : unit =
        p |> reportWith config |> Report.tryRaise

    let check (p : Property<unit>) : unit =
        p |> report |> Report.tryRaise

    let checkBool (g : Property<bool>) : unit =
        g |> falseToFailure |> check

    let checkBoolWith (config : IPropertyConfig) (g : Property<bool>) : unit =
        g |> falseToFailure |> checkWith config

    let reportRecheckWith (recheckData: string) (_: IPropertyConfig) (p : Property<unit>) : Report =
        let recheckData = recheckData |> RecheckData.deserialize
        let result, _ = splitAndRun p recheckData
        { Tests = 1<tests>
          Discards = 0<discards>
          Status = Shrinking.followPath result recheckData.ShrinkPath }

    let reportRecheck (recheckData: string) (p : Property<unit>) : Report =
        p |> reportRecheckWith recheckData PropertyConfig.defaults

    let reportRecheckBoolWith (recheckData: string) (config : IPropertyConfig) (p : Property<bool>) : Report =
        p |> falseToFailure |> reportRecheckWith recheckData config

    let reportRecheckBool (recheckData: string) (p : Property<bool>) : Report =
        p |> falseToFailure |> reportRecheck recheckData

    let recheckWith (recheckData: string) (config : IPropertyConfig) (p : Property<unit>) : unit =
        p |> reportRecheckWith recheckData config |> Report.tryRaise

    let recheck (recheckData: string) (p : Property<unit>) : unit =
        p |> reportRecheck recheckData |> Report.tryRaise

    let recheckBoolWith (recheckData: string) (config : IPropertyConfig) (g : Property<bool>) : unit =
        g |> falseToFailure |> recheckWith recheckData config

    let recheckBool (recheckData: string) (g : Property<bool>) : unit =
        g |> falseToFailure |> recheck recheckData

    let renderWith (n : IPropertyConfig) (p : Property<unit>) : string =
        p |> reportWith n |> Report.render

    let render (p : Property<unit>) : string =
        p |> report |> Report.render

    let renderBool (property : Property<bool>) : string =
        property |> falseToFailure |> render

    let renderBoolWith (config : IPropertyConfig) (p : Property<bool>) : string =
        p |> falseToFailure |> renderWith config

    //
    // Async Execution (Non-Blocking)
    //

    let private reportWithAsync' (args : PropertyArgs) (config : IPropertyConfig) (p : Property<unit>) : Async<Report> =
        let nextSize size =
            if size >= 100 then
                1
            else
                size + 1

        let rec loop data tests discards = async {
            if tests = config.TestLimit then
                return { Tests = tests
                         Discards = discards
                         Status = OK }
            elif discards >= 100<discards> then
                return { Tests = tests
                         Discards = discards
                         Status = GaveUp }
            else
                let result, seed2 = splitAndRun p data
                let nextData = {
                    data with
                        Seed = seed2
                        Size = nextSize data.Size
                }

                let! journal, outcome = PropertyResult.unwrapAsync (Tree.outcome result)
                match outcome with
                | Failure ->
                    let! status = Shrinking.shrinkAsync args.Language data config.ShrinkLimit result
                    return { Tests = tests + 1<tests>
                             Discards = discards
                             Status = status }
                | Success () ->
                    return! loop nextData (tests + 1<tests>) discards
                | Discard ->
                    return! loop nextData tests (discards + 1<discards>)
        }

        loop args.RecheckData 0<tests> 0<discards>

    /// Non-blocking report generation with config that returns Async<Report>
    let reportAsyncWith (config : IPropertyConfig) (p : Property<unit>) : Async<Report> =
        p |> reportWithAsync' PropertyArgs.init config

    /// Non-blocking report generation that returns Async<Report>
    let reportAsync (p : Property<unit>) : Async<Report> =
        p |> reportAsyncWith PropertyConfig.defaults

    /// Non-blocking async check with config that returns Async<unit>
    let checkAsyncWith (config : IPropertyConfig) (p : Property<unit>) : Async<unit> =
        async {
            let! report = reportAsyncWith config p
            return Report.tryRaise report
        }

    /// Non-blocking async check that returns Async<unit>
    let checkAsync (p : Property<unit>) : Async<unit> =
        async {
            let! report = reportAsync p
            return Report.tryRaise report
        }

    /// Non-blocking async report generation with config for bool properties that returns Async<Report>
    let reportBoolAsyncWith (config : IPropertyConfig) (p : Property<bool>) : Async<Report> =
        p |> falseToFailure |> reportAsyncWith config

    /// Non-blocking async report generation for bool properties that returns Async<Report>
    let reportBoolAsync (p : Property<bool>) : Async<Report> =
        p |> falseToFailure |> reportAsync

    /// Non-blocking async check with config for bool properties that returns Async<unit>
    let checkBoolAsyncWith (config : IPropertyConfig) (p : Property<bool>) : Async<unit> =
        p |> falseToFailure |> checkAsyncWith config

    /// Non-blocking async check for bool properties that returns Async<unit>
    let checkBoolAsync (p : Property<bool>) : Async<unit> =
        p |> falseToFailure |> checkAsync

#if !FABLE_COMPILER
    /// Non-blocking report generation with config that returns Task<Report>
    let reportTaskWith (config : IPropertyConfig) (p : Property<unit>) : System.Threading.Tasks.Task<Report> =
        p |> reportAsyncWith config |> Async.StartAsTask

    /// Non-blocking report generation that returns Task<Report>
    let reportTask (p : Property<unit>) : System.Threading.Tasks.Task<Report> =
        p |> reportAsync |> Async.StartAsTask

    /// Non-blocking check with config that returns Task<unit>
    let checkTaskWith (config : IPropertyConfig) (p : Property<unit>) : System.Threading.Tasks.Task<unit> =
        p |> checkAsyncWith config |> Async.StartAsTask

    /// Non-blocking check that returns Task<unit>
    let checkTask (p : Property<unit>) : System.Threading.Tasks.Task<unit> =
        p |> checkAsync |> Async.StartAsTask

    /// Non-blocking report generation with config for bool properties that returns Task<Report>
    let reportBoolTaskWith (config : IPropertyConfig) (p : Property<bool>) : System.Threading.Tasks.Task<Report> =
        p |> reportBoolAsyncWith config |> Async.StartAsTask

    /// Non-blocking report generation for bool properties that returns Task<Report>
    let reportBoolTask (p : Property<bool>) : System.Threading.Tasks.Task<Report> =
        p |> reportBoolAsync |> Async.StartAsTask

    /// Non-blocking check with config for bool properties that returns Task<unit>
    let checkBoolTaskWith (config : IPropertyConfig) (p : Property<bool>) : System.Threading.Tasks.Task<unit> =
        p |> checkBoolAsyncWith config |> Async.StartAsTask

    /// Non-blocking check for bool properties that returns Task<unit>
    let checkBoolTask (p : Property<bool>) : System.Threading.Tasks.Task<unit> =
        p |> checkBoolAsync |> Async.StartAsTask
#endif

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

        member __.Bind(m : Property<'a>, k : 'a -> Property<'b>) : Property<'b> =
            m |> Property.bind k

        // Allow binding bare Async<'a> - automatically converts to async Property<'a>
        member __.Bind(m : Async<'a>, k : 'a -> Property<'b>) : Property<'b> =
            Property.ofAsync m |> Property.bind k

        // Allow binding Property<Async<'a>> - automatically converts to async Property<'a>
        member inline __.Bind(m : Property<Async<'a>>, k : 'a -> Property<'b>) : Property<'b> =
            m |> Property.bind (fun asyncVal -> Property.ofAsync asyncVal |> Property.bind k)

        member __.Return(b : bool) : Property<unit> =
            Property.ofBool b

        // Allow returning Async<unit> directly - automatically converts to async Property<unit>
        member __.Return(asyncUnit : Async<unit>) : Property<unit> =
            Property.ofAsync asyncUnit

        member __.BindReturn(m : Gen<'a>, f: 'a -> 'b) =
            m
            |> Gen.map (fun a -> Lazy.constant ((Journal.singleton (fun () -> Property.printValue a)), Success a))
            |> Property.ofGen
            |> Property.map f

        // BindReturn for Async<'a> - handles let! x = async { ... } in return f x
        member __.BindReturn(m : Async<'a>, f: 'a -> 'b) : Property<'b> =
            Property.ofAsync m |> Property.map f

        member __.MergeSources(ga, gb) =
            Gen.zip ga gb

        member __.ReturnFrom(m : Property<'a>) : Property<'a> =
            m

        // Allow return! with Async<'a> - automatically converts to async Property<'a>
        member __.ReturnFrom(asyncValue : Async<'a>) : Property<'a> =
            Property.ofAsync asyncValue

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

        // ========================================
        // Task support
        // ========================================
#if !FABLE_COMPILER
        // Allow binding bare Task<'a> - automatically converts to async Property<'a>
        member __.Bind(m : System.Threading.Tasks.Task<'a>, k : 'a -> Property<'b>) : Property<'b> =
            Property.ofTask m |> Property.bind k

        // Allow binding Property<Task<'a>> - automatically converts to async Property<'a>
        member inline __.Bind(m : Property<System.Threading.Tasks.Task<'a>>, k : 'a -> Property<'b>) : Property<'b> =
            m |> Property.bind (fun taskVal -> Property.ofTask taskVal |> Property.bind k)

        // Allow returning Task<unit> directly - automatically converts to async Property<unit>
        member __.Return(taskUnit : System.Threading.Tasks.Task<unit>) : Property<unit> =
            Property.ofTaskUnit taskUnit

        // Allow returning Task directly - automatically converts to async Property<unit>
        member __.Return(task : System.Threading.Tasks.Task) : Property<unit> =
            Property.ofTaskUnit task

        // BindReturn for Task<'a> - handles let! x = task { ... } in return f x
        member __.BindReturn(m : System.Threading.Tasks.Task<'a>, f: 'a -> 'b) : Property<'b> =
            Property.ofTask m |> Property.map f

        // Allow return! with Task<'a> - automatically converts to async Property<'a>
        member __.ReturnFrom(taskValue : System.Threading.Tasks.Task<'a>) : Property<'a> =
            Property.ofTask taskValue

        // Allow return! with Task (non-generic) - automatically converts to async Property<unit>
        member __.ReturnFrom(task : System.Threading.Tasks.Task) : Property<unit> =
            Property.ofTaskUnit task
#endif

    let property = Builder ()
