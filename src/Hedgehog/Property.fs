namespace Hedgehog



type internal TestReturnedFalseException() =
  inherit System.Exception("Expected 'true' but was 'false'.")


[<RequireQualifiedAccess>]
type PropertyResult<'a> =
    | Sync of Journal * Outcome<'a>
    | Async of System.Threading.Tasks.Task<Journal * Outcome<'a>>

[<RequireQualifiedAccess>]
module internal PropertyResult =
    open System.Threading.Tasks

    /// Apply a function to the outcome, handling both sync and async cases
    let map (f : Journal * Outcome<'a> -> Journal * Outcome<'b>)
            (result : PropertyResult<'a>) : PropertyResult<'b> =
        match result with
        | PropertyResult.Sync (journal, outcome) ->
            PropertyResult.Sync (f (journal, outcome))
        | PropertyResult.Async taskResult ->
            PropertyResult.Async (task {
                let! result = taskResult
                return f result
            })

    /// Combine two PropertyResults, applying a function to their values
    let map2 (f : Journal * Outcome<'a> -> Journal * Outcome<'b> -> Journal * Outcome<'c>)
             (left : PropertyResult<'a>)
             (right : PropertyResult<'b>) : PropertyResult<'c> =
        match left, right with
        | PropertyResult.Sync (j1, o1), PropertyResult.Sync (j2, o2) ->
            PropertyResult.Sync (f (j1, o1) (j2, o2))
        | PropertyResult.Sync (j1, o1), PropertyResult.Async taskRight ->
            PropertyResult.Async (task {
                let! rightResult = taskRight
                return f (j1, o1) rightResult
            })
        | PropertyResult.Async taskLeft, PropertyResult.Sync (j2, o2) ->
            PropertyResult.Async (task {
                let! leftResult = taskLeft
                return f leftResult (j2, o2)
            })
        | PropertyResult.Async taskLeft, PropertyResult.Async taskRight ->
            PropertyResult.Async (task {
                let! leftResult = taskLeft
                let! rightResult = taskRight
                return f leftResult rightResult
            })

    /// Unwrap synchronously (blocking for async case - for backward compatibility)
    let toSync (result : PropertyResult<'a>) : Journal * Outcome<'a> =
        match result with
        | PropertyResult.Sync (journal, outcome) -> (journal, outcome)
        | PropertyResult.Async task -> task.Result

[<Struct>]
type Property<'a> =
    | Property of Gen<Lazy<PropertyResult<'a>>>

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
                | PropertyResult.Async task -> task.Result))  // Blocking for now

    let ofGen (x : Gen<Lazy<Journal * Outcome<'a>>>) : Property<'a> =
        Property (wrapSync x)

    let toGen (Property x : Property<'a>) : Gen<Lazy<Journal * Outcome<'a>>> =
        unwrapSync x

    // Internal version that doesn't unwrap - keeps PropertyResult
    let private toGenInternal (Property x : Property<'a>) : Gen<Lazy<PropertyResult<'a>>> =
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

    /// Converts a Task<'T> into a Property<'T>.
    /// Evaluates the task and makes its result available to property combinators.
    /// The property succeeds if the task completes successfully,
    /// fails if the task throws an exception or is canceled.
    let ofTask (inputTask : System.Threading.Tasks.Task<'T>) : Property<'T> =
        Gen.constant (lazy (
            PropertyResult.Async (task {
                try
                    let! result = inputTask
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
            PropertyResult.Async (task {
                try
                    do! inputTask
                    return (Journal.empty, Success ())
                with
                | :? System.OperationCanceledException ->
                    return (Journal.singletonMessage "Task was canceled", Failure)
                | ex ->
                    return (Journal.singletonMessage (string ex), Failure)
            })))
        |> Property

    /// Converts an Async<'T> into a Property<'T>.
    /// The async computation is converted to Task internally.
    let ofAsync (asyncComputation : Async<'T>) : Property<'T> =
        asyncComputation
        |> Async.StartAsTask
        |> ofTask

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
            | Failure -> PropertyResult.Sync (journal, Failure)
            | Discard -> PropertyResult.Sync (journal, Discard)
            | Success a -> evalNext journal a

        m |> Gen.bind (fun lazyResult ->
            Gen.constant (lazy (
                lazyResult.Value
                |> PropertyResult.map (fun (journal, outcome) ->
                    handleOutcome journal outcome |> PropertyResult.toSync))))

    /// Monadic bind operation for properties.
    /// Applies a property-returning function to the result of another property,
    /// sequencing their execution and combining their journals.
    let bind (k : 'a -> Property<'b>) (m : Property<'a>) : Property<'b> =
        let kTry a =
            try
                k a |> toGenInternal
            with e ->
                (Journal.singletonMessage (string e), Failure)
                |> PropertyResult.Sync
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
            |> toGen
            |> Gen.map (fun lazyOutcome ->
                lazy (
                    let (j, outcome) = lazyOutcome.Value
                    (Journal.append customJournal j, outcome))))
        |> ofGen

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
            |> toGen

        gen |> Gen.bind prepend |> ofGen

    let forAll' (gen : Gen<'a>) : Property<'a> =
        gen |> forAll success

    //
    // Runner
    //

    // Helper to unwrap PropertyResult (synchronously for backward compatibility)
    let private unwrapResult (lazyResult : Lazy<PropertyResult<'a>>) : Journal * Outcome<'a> =
        PropertyResult.toSync lazyResult.Value

    let private shrinkInput
            (language: Language)
            (data : RecheckData)
            (shrinkLimit : int<shrinks> Option) =
        let rec loop
                (nshrinks : int<shrinks>)
                (shrinkPathRev : ShrinkOutcome list)
                (Node (root, xs) : Tree<Lazy<PropertyResult<'a>>>) =
            let getFailed () =
                Failed {
                    Shrinks = nshrinks
                    Journal = unwrapResult root |> fst
                    RecheckInfo =
                        Some { Language = language
                               Data = { data with ShrinkPath = List.rev shrinkPathRev } } }
            match shrinkLimit with
            | Some shrinkLimit' when nshrinks >= shrinkLimit' -> getFailed ()
            | _ ->
                match xs |> Seq.indexed |> Seq.tryFind (snd >> Tree.outcome >> unwrapResult >> snd >> Outcome.isFailure) with
                | None -> getFailed ()
                | Some (idx, tree) ->
                    let nextShrinkPathRev = ShrinkOutcome.Pass idx :: shrinkPathRev
                    loop (nshrinks + 1<shrinks>) nextShrinkPathRev tree
        loop 0<shrinks> []

    let rec private followShrinkPath
            (Node (root, children) : Tree<Lazy<PropertyResult<'a>>>)
            shrinkPath =
        match shrinkPath with
        | [] ->
            let journal, outcome = unwrapResult root
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

                match snd (unwrapResult (Tree.outcome result)) with
                | Failure ->
                    { Tests = tests + 1<tests>
                      Discards = discards
                      Status = shrinkInput args.Language data config.ShrinkLimit result }
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
          Status = followShrinkPath result recheckData.ShrinkPath }

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

    /// Helper to unwrap PropertyResult asynchronously without blocking
    let private unwrapResultAsync (lazyResult : Lazy<PropertyResult<'a>>) : Async<Journal * Outcome<'a>> =
        async {
            match lazyResult.Value with
            | PropertyResult.Sync (journal, outcome) ->
                return (journal, outcome)
            | PropertyResult.Async taskResult ->
                return! taskResult |> Async.AwaitTask
        }

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

                let! journal, outcome = unwrapResultAsync (Tree.outcome result)
                match outcome with
                | Failure ->
                    return { Tests = tests + 1<tests>
                             Discards = discards
                             Status = shrinkInput args.Language data config.ShrinkLimit result }
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

    /// Non-blocking report generation with config that returns Task<Report>
    let reportTaskWith (config : IPropertyConfig) (p : Property<unit>) : System.Threading.Tasks.Task<Report> =
        p |> reportAsyncWith config |> Async.StartAsTask

    /// Non-blocking report generation that returns Task<Report>
    let reportTask (p : Property<unit>) : System.Threading.Tasks.Task<Report> =
        p |> reportAsync |> Async.StartAsTask

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

    /// Non-blocking check with config that returns Task<unit>
    let checkTaskWith (config : IPropertyConfig) (p : Property<unit>) : System.Threading.Tasks.Task<unit> =
        p |> checkAsyncWith config |> Async.StartAsTask

    /// Non-blocking check that returns Task<unit>
    let checkTask (p : Property<unit>) : System.Threading.Tasks.Task<unit> =
        p |> checkAsync |> Async.StartAsTask


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

        // Allow binding bare Task<'a> - automatically converts to async Property<'a>
        member __.Bind(m : System.Threading.Tasks.Task<'a>, k : 'a -> Property<'b>) : Property<'b> =
            Property.ofTask m |> Property.bind k

        // Allow binding Property<Async<'a>> - automatically converts to async Property<'a>
        member inline __.Bind(m : Property<Async<'a>>, k : 'a -> Property<'b>) : Property<'b> =
            m |> Property.bind (fun asyncVal -> Property.ofAsync asyncVal |> Property.bind k)

        // Allow binding Property<Task<'a>> - automatically converts to async Property<'a>
        member inline __.Bind(m : Property<System.Threading.Tasks.Task<'a>>, k : 'a -> Property<'b>) : Property<'b> =
            m |> Property.bind (fun taskVal -> Property.ofTask taskVal |> Property.bind k)

        member __.Return(b : bool) : Property<unit> =
            Property.ofBool b

        // Allow returning Async<unit> directly - automatically converts to async Property<unit>
        member __.Return(asyncUnit : Async<unit>) : Property<unit> =
            Property.ofAsync asyncUnit

        // Allow returning Task<unit> directly - automatically converts to async Property<unit>
        member __.Return(taskUnit : System.Threading.Tasks.Task<unit>) : Property<unit> =
            Property.ofTaskUnit taskUnit

        // Allow returning Task directly - automatically converts to async Property<unit>
        member __.Return(task : System.Threading.Tasks.Task) : Property<unit> =
            Property.ofTaskUnit task

        member __.BindReturn(m : Gen<'a>, f: 'a -> 'b) =
            m
            |> Gen.map (fun a -> Lazy.constant ((Journal.singleton (fun () -> Property.printValue a)), Success a))
            |> Property.ofGen
            |> Property.map f

        // BindReturn for Async<'a> - handles let! x = async { ... } in return f x
        member __.BindReturn(m : Async<'a>, f: 'a -> 'b) : Property<'b> =
            Property.ofAsync m |> Property.map f

        // BindReturn for Task<'a> - handles let! x = task { ... } in return f x
        member __.BindReturn(m : System.Threading.Tasks.Task<'a>, f: 'a -> 'b) : Property<'b> =
            Property.ofTask m |> Property.map f

        member __.MergeSources(ga, gb) =
            Gen.zip ga gb

        member __.ReturnFrom(m : Property<'a>) : Property<'a> =
            m

        // Allow return! with Async<'a> - automatically converts to async Property<'a>
        member __.ReturnFrom(asyncValue : Async<'a>) : Property<'a> =
            Property.ofAsync asyncValue

        // Allow return! with Task<'a> - automatically converts to async Property<'a>
        member __.ReturnFrom(taskValue : System.Threading.Tasks.Task<'a>) : Property<'a> =
            Property.ofTask taskValue

        // Allow return! with Task (non-generic) - automatically converts to async Property<unit>
        member __.ReturnFrom(task : System.Threading.Tasks.Task) : Property<unit> =
            Property.ofTaskUnit task

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
