namespace Hedgehog



type internal TestReturnedFalseException() =
  inherit System.Exception("Expected 'true' but was 'false'.")


[<Struct>]
type Property<'a> =
    internal Property of Gen<Lazy<PropertyResult<'a>>>

namespace Hedgehog.FSharp

open System
open Hedgehog

/// Functions for building and executing property-based tests.
/// Properties represent testable specifications that should hold true for all generated inputs.
module Property =

    // Internal helpers to convert between PropertyResult and plain tuples.
    // This allows us to keep using existing GenLazy/GenLazyTuple helpers.
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
                    Async.RunSynchronously asyncResult  // Blocking for now.
#endif
                ))

    /// Creates a property from a generator that produces journaled outcomes.
    /// This is a low-level function primarily used internally.
    let ofGen (x : Gen<Lazy<Journal * Outcome<'a>>>) : Property<'a> =
        Property (wrapSync x)

    /// Extracts the underlying generator from a property.
    /// This is a low-level function primarily used internally.
    let toGen (Property x : Property<'a>) : Gen<Lazy<Journal * Outcome<'a>>> =
        unwrapSync x

    // Internal version that doesn't unwrap - keeps PropertyResult.
    let private toGenInternal (Property x : Property<'a>) : Gen<Lazy<PropertyResult<'a>>> =
        x

    /// Ensures cleanup code runs after a property executes, whether it succeeds or fails.
    /// Useful for releasing resources like file handles or database connections.
    let tryFinally (after : unit -> unit) (m : Property<'a>) : Property<'a> =
        Gen.tryFinally after (toGenInternal m) |> Property

    /// Handles exceptions thrown during property execution by converting them into alternative properties.
    /// This allows graceful error handling and custom failure messages.
    let tryWith (k : exn -> Property<'a>) (m : Property<'a>) : Property<'a> =
        Gen.tryWith (toGenInternal << k) (toGenInternal m) |> Property

    /// Delays the evaluation of a property until it's actually needed.
    /// This enables recursive property definitions and lazy construction.
    let delay (f : unit -> Property<'a>) : Property<'a> =
        Gen.delay (toGenInternal << f) |> Property

    /// Safely uses a disposable resource within a property, ensuring it's properly disposed after use.
    /// The resource is automatically disposed even if the property fails.
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
    /// Converts a Task into a property that can be used in property-based tests.
    /// The property succeeds when the task completes successfully, and fails if the task throws an exception or is canceled.
    /// This enables testing of async/await code using C# Tasks.
    let ofTask (inputTask : System.Threading.Tasks.Task<'T>) : Property<'T> =
        Gen.constant (lazy (
            PropertyResult.Async (async {
                try
                    let! result = Async.AwaitTask inputTask
                    return (Journal.empty, Success result)
                with
                | :? OperationCanceledException ->
                    return (Journal.singleton (fun () -> Cancellation "Task was canceled"), Failure)
                | ex ->
                    return (Journal.exn ex, Failure)
            })))
        |> Property

    /// Converts a non-generic Task into a property.
    /// Use this for Tasks that don't return a value (void async methods in C#).
    let ofTaskUnit (inputTask : System.Threading.Tasks.Task) : Property<unit> =
        Gen.constant (lazy (
            PropertyResult.Async (async {
                try
                    do! Async.AwaitTask inputTask
                    return (Journal.empty, Success ())
                with
                | :? OperationCanceledException ->
                    return (Journal.singleton (fun () -> Cancellation "Task was canceled"), Failure)
                | ex ->
                    return (Journal.exn ex, Failure)
            })))
        |> Property
#endif

    /// Converts an F# async computation into a property that can be used in property-based tests.
    /// The property succeeds when the async computation completes successfully, and fails if it throws an exception.
    /// This enables testing of asynchronous F# code.
    let ofAsync (asyncComputation : Async<'T>) : Property<'T> =
        Gen.constant (lazy (PropertyResult.ofAsync asyncComputation))
        |> Property

    /// Create Property from an async computation that produces a journal and outcome
    let ofAsyncWithJournal (asyncComputation : Async<Journal * Outcome<'T>>) : Property<'T> =
        Gen.constant (lazy (PropertyResult.ofAsyncWithJournal asyncComputation))
        |> Property

    /// Discards test cases where the predicate returns false, causing Hedgehog to generate a new test case.
    /// Use sparingly as excessive filtering can lead to "gave up" results when too many cases are discarded.
    let filter (p : 'a -> bool) (m : Property<'a>) : Property<'a> =
        m |> toGenInternal |> Gen.map (Lazy.map (PropertyResult.map (fun (j, o) -> (j, Outcome.filter p o)))) |> Property

    /// Creates a property from an explicit test outcome (Success, Failure, or Discard).
    /// This is a low-level function primarily used internally or for custom property combinators.
    let ofOutcome (x : Outcome<'a>) : Property<'a> =
        (Journal.empty, x) |> GenLazy.constant |> ofGen

    /// A property that always fails. Use this to explicitly fail a test.
    let failure : Property<unit> =
        Failure |> ofOutcome

    // A failed property with a given exception recorded in the journal.
    let exn (ex: exn) : Property<unit> =
        (Journal.exn ex, Failure) |> GenLazy.constant |> ofGen

    /// A property that discards the current test case, causing a new one to be generated.
    /// Use sparingly to avoid "gave up" results.
    let discard : Property<unit> =
        Discard |> ofOutcome

    /// A property that always succeeds with the given value.
    /// This is the property monad's return/pure operation.
    let success (x : 'a) : Property<'a> =
        Success x |> ofOutcome

    /// Converts a boolean into a property: true becomes success, false becomes failure.
    /// Useful for asserting simple boolean conditions in property tests.
    let ofBool (x : bool) : Property<unit> =
        if x then
            success ()
        else
            failure

    /// Adds a message to the test journal that will be displayed if the test fails.
    /// Use this to provide context about generated values or intermediate results that led to a failure.
    /// The message function is only evaluated if the test fails.
    let counterexample (msg : unit -> string) : Property<unit> =
        (Journal.singleton (fun () -> Counterexample (msg ())), Success ()) |> GenLazy.constant |> ofGen

    /// Transforms the successful result of a property using the provided function.
    /// If the function throws an exception, the property fails with the exception message.
    /// This is the functor map operation for properties.
    let map (f : 'a -> 'b) (x : Property<'a>) : Property<'b> =
        let applyWithExceptionHandling (j, outcome) =
            try
                (j, outcome |> Outcome.map f)
            with 
            | :? TestReturnedFalseException ->
                // Don't include internal exception in journal - it's just a signal.
                (j, Failure)
            | e ->
                (Journal.append j (Journal.exn e), Failure)

        let g (lazyResult : Lazy<PropertyResult<'a>>) =
            lazy (PropertyResult.map applyWithExceptionHandling lazyResult.Value)

        x |> toGenInternal |> Gen.map g |> Property

    let internal set (a: 'a) (property : Property<'b>) : Property<'a> =
        property |> map (fun _ -> a)

    /// Discards the result of a property, converting it to Property<unit>.
    /// This is useful when using assertion libraries that return non-unit types (e.g., fluent assertions).
    /// Assertions that throw exceptions will still cause the property to fail.
    let ignoreResult (property : Property<'a>) : Property<unit> =
        property |> map (fun _ -> ())

    // Helper to handle Failure/Discard cases in bind - just wrap and return without calling continuation.
    // Note: Failure and Discard don't carry values, so we can safely change the type parameter.
    let private shortCircuit (journal : Journal) (outcome : Outcome<'a>) : Gen<Lazy<PropertyResult<'b>>> =
        let outcome' : Outcome<'b> =
            match outcome with
            | Failure -> Failure
            | Discard -> Discard
            | Success _ -> failwith "shortCircuit should only be called with Failure or Discard"
        PropertyResult.ofSync journal outcome'
        |> GenLazy.constant

    let private bindGen
            (f : 'a -> Gen<Lazy<PropertyResult<'b>>>)
            (m : Gen<Lazy<PropertyResult<'a>>>) : Gen<Lazy<PropertyResult<'b>>> =
        
        // Use GenLazy.bind pattern (like before async was introduced) but handle PropertyResult.
        m |> GenLazy.bind (fun propertyResultA ->
            // This function is called with the FORCED PropertyResult (lazy was forced by GenLazy.bind).
            // This happens during tree construction for proper shrinking via Gen.bind.
            
            match propertyResultA with
            | PropertyResult.Sync (journalA, outcomeA) ->
                // Synchronous case: pattern match and continue (original behavior).
                match outcomeA with
                | Failure -> shortCircuit journalA Failure
                | Discard -> shortCircuit journalA Discard
                | Success a ->
                    // Call continuation and append journals (preserves shrinking via f a).
                    f a |> GenLazy.map (PropertyResult.map (fun (journalB, outcomeB) ->
                        (Journal.append journalA journalB, outcomeB)))

            | PropertyResult.Async asyncResultA ->
                // Async case: Block during generation to preserve full shrinking.
                //
                // Per async-properties.md: Blocking is acceptable when generators are
                // interleaved with async (gen → async → gen pattern).
                //
                // Trade-off: We choose Laziness + Full Shrinking over Non-blocking.
                // The async is awaited during tree construction (generation phase) so we can
                // get the value 'a' and build the full continuation tree with all its shrinks..
                //
                // This preserves the monad laws and shrinking behavior identical to sync properties.
#if FABLE_COMPILER
                failwith "Async property binding with interleaved generators is not supported in Fable. Use Property.checkAsync or Property.reportAsync instead of Property.check or Property.report."
#else
                // Block to get the result (this happens during generation phase).
                let journalA, outcomeA = Async.RunSynchronously asyncResultA
                
                // Now handle just like the sync case.
                match outcomeA with
                | Failure -> shortCircuit journalA Failure
                | Discard -> shortCircuit journalA Discard
                | Success a ->
                    // Call continuation and append journals (preserves full shrinking via f a).
                    f a |> GenLazy.map (PropertyResult.map (fun (journalB, outcomeB) ->
                        (Journal.append journalA journalB, outcomeB)))
#endif
        )

    /// Sequences two properties together, passing the result of the first to a function that produces the second.
    /// This is the monadic bind operation that enables property composition and dependent testing.
    /// The journals from both properties are combined in the final result.
    let bind (k : 'a -> Property<'b>) (m : Property<'a>) : Property<'b> =
        let kTry a =
            try
                k a |> toGenInternal
            with e ->
                PropertyResult.ofSync (Journal.exn e) Failure
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

    /// Converts a boolean property to a unit property, treating false as a failure.
    /// This is used internally to support boolean property testing.
    let falseToFailure p =
        p |> map (fun b -> if not b then raise (TestReturnedFalseException()))

    /// Creates a property that tests whether a condition holds for all values generated by the given generator.
    /// Generated values are automatically added to the test journal and will be shown if the test fails.
    /// This is the primary way to introduce generated test data into your properties.
    let forAll (k : 'a -> Property<'b>) (gen : Gen<'a>) : Property<'b> =
        let prepend (x : 'a) =
            let journalEntry = Journal.singleton (fun () -> GeneratedValue (box x))
            (journalEntry, Success x) |> GenLazy.constant |> ofGen
            |> bind k
            |> toGenInternal

        gen |> Gen.bind prepend |> Property

    /// Creates a property that succeeds with the generated value, logging it to the journal.
    /// Useful when you just want to generate a value and return it without additional assertions.
    let forAll' (gen : Gen<'a>) : Property<'a> =
        gen |> forAll success

    //
    // Shrinking
    //

    /// Module containing shrinking logic for property test failures.
    module private Shrinking =
        
        /// Shrink a failing test synchronously, finding the smallest input that still fails.
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

        /// Shrink a failing test asynchronously, finding the smallest input that still fails.
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
                    let rec findFirstFailure (trees : seq<int * Tree<Lazy<PropertyResult<'a>>>>) = async {
                        use enumerator = trees.GetEnumerator()
                        let rec loop () = async {
                            if enumerator.MoveNext() then
                                let idx, tree = enumerator.Current
                                let! _, outcome = PropertyResult.unwrapAsync (Tree.outcome tree)
                                if Outcome.isFailure outcome then
                                    return Some (idx, tree)
                                else
                                    return! loop ()
                            else
                                return None
                        }
                        return! loop ()
                    }
                    
                    let! found = xs |> Seq.indexed |> findFirstFailure
                    match found with
                    | None -> return! getFailed ()
                    | Some (idx, tree) ->
                        let nextShrinkPathRev = ShrinkOutcome.Pass idx :: shrinkPathRev
                        return! loop (nshrinks + 1<shrinks>) nextShrinkPathRev tree
            }
            loop 0<shrinks> [] tree

        /// Follow a previously recorded shrink path to replay a failure.
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

    /// Runs a property test with custom configuration and returns a detailed report.
    /// The report includes the number of tests run, discards, and failure information with shrunk counterexamples.
    /// This blocks until all tests complete.
    let reportWith (config : IPropertyConfig) (p : Property<unit>) : Report =
        p |> reportWith' (PropertyArgs.init ()) config

    /// Runs a property test with default configuration and returns a detailed report.
    /// By default, runs 100 tests. This blocks until all tests complete.
    let report (p : Property<unit>) : Report =
        p |> reportWith PropertyConfig.defaults

    /// Runs a boolean property test with custom configuration and returns a detailed report.
    /// Converts false results to failures. This blocks until all tests complete.
    let reportBoolWith (config : IPropertyConfig) (p : Property<bool>) : Report =
        p |> falseToFailure |> reportWith config

    /// Runs a boolean property test with default configuration and returns a detailed report.
    /// Converts false results to failures. This blocks until all tests complete.
    let reportBool (p : Property<bool>) : Report =
        p |> falseToFailure |> report

    /// Runs a property test with custom configuration and throws an exception if it fails.
    /// This blocks until all tests complete. Use this in test frameworks that expect exceptions on failure.
    let checkWith (config : IPropertyConfig) (p : Property<unit>) : unit =
        p |> reportWith config |> Report.tryRaise

    /// Runs a property test with default configuration and throws an exception if it fails.
    /// This blocks until all tests complete. This is the most common way to run property tests.
    let check (p : Property<unit>) : unit =
        p |> report |> Report.tryRaise

    /// Runs a boolean property test with default configuration and throws an exception if it fails.
    /// Converts false results to failures. This blocks until all tests complete.
    let checkBool (g : Property<bool>) : unit =
        g |> falseToFailure |> check

    /// Runs a boolean property test with custom configuration and throws an exception if it fails.
    /// Converts false results to failures. This blocks until all tests complete.
    let checkBoolWith (config : IPropertyConfig) (g : Property<bool>) : unit =
        g |> falseToFailure |> checkWith config

    /// Re-runs a previously failed test case using the recheck data from a failure report.
    /// This is useful for debugging specific failures by reproducing them exactly.
    /// The config parameter is currently ignored but kept for API consistency.
    let reportRecheckWith (recheckData: string) (_: IPropertyConfig) (p : Property<unit>) : Report =
        let recheckData = recheckData |> RecheckData.deserialize
        let result, _ = splitAndRun p recheckData
        { Tests = 1<tests>
          Discards = 0<discards>
          Status = Shrinking.followPath result recheckData.ShrinkPath }

    /// Re-runs a previously failed test case using the recheck data from a failure report.
    /// This uses default configuration and is useful for quickly reproducing specific failures.
    let reportRecheck (recheckData: string) (p : Property<unit>) : Report =
        p |> reportRecheckWith recheckData PropertyConfig.defaults

    /// Re-runs a previously failed boolean property test using recheck data with custom configuration.
    /// Converts false results to failures.
    let reportRecheckBoolWith (recheckData: string) (config : IPropertyConfig) (p : Property<bool>) : Report =
        p |> falseToFailure |> reportRecheckWith recheckData config

    /// Re-runs a previously failed boolean property test using recheck data with default configuration.
    /// Converts false results to failures.
    let reportRecheckBool (recheckData: string) (p : Property<bool>) : Report =
        p |> falseToFailure |> reportRecheck recheckData

    /// Re-runs a previously failed test case and throws an exception if it still fails.
    /// Uses custom configuration. This is useful for debugging failures in test frameworks.
    let recheckWith (recheckData: string) (config : IPropertyConfig) (p : Property<unit>) : unit =
        p |> reportRecheckWith recheckData config |> Report.tryRaise

    /// Re-runs a previously failed test case and throws an exception if it still fails.
    /// Uses default configuration. This is the most common way to reproduce specific failures.
    let recheck (recheckData: string) (p : Property<unit>) : unit =
        p |> reportRecheck recheckData |> Report.tryRaise

    /// Re-runs a previously failed boolean property test and throws an exception if it still fails.
    /// Uses custom configuration. Converts false results to failures.
    let recheckBoolWith (recheckData: string) (config : IPropertyConfig) (g : Property<bool>) : unit =
        g |> falseToFailure |> recheckWith recheckData config

    /// Re-runs a previously failed boolean property test and throws an exception if it still fails.
    /// Uses default configuration. Converts false results to failures.
    let recheckBool (recheckData: string) (g : Property<bool>) : unit =
        g |> falseToFailure |> recheck recheckData

    /// Runs a property test with custom configuration and returns the report as a formatted string.
    /// This is useful for custom test result formatting or logging.
    let renderWith (n : IPropertyConfig) (p : Property<unit>) : string =
        p |> reportWith n |> Report.render

    /// Runs a property test with default configuration and returns the report as a formatted string.
    /// This is useful for custom test result formatting or logging.
    let render (p : Property<unit>) : string =
        p |> report |> Report.render

    /// Runs a boolean property test with default configuration and returns the report as a formatted string.
    /// Converts false results to failures.
    let renderBool (property : Property<bool>) : string =
        property |> falseToFailure |> render

    /// Runs a boolean property test with custom configuration and returns the report as a formatted string.
    /// Converts false results to failures.
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

                let! _, outcome = PropertyResult.unwrapAsync (Tree.outcome result)
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

    /// Runs a property test asynchronously with custom configuration, returning an F# Async that produces a report.
    /// This is non-blocking and properly handles async properties without blocking threads.
    /// Use this when testing async code or when you need non-blocking test execution.
    let reportAsyncWith (config : IPropertyConfig) (p : Property<unit>) : Async<Report> =
        p |> reportWithAsync' (PropertyArgs.init ()) config

    /// Runs a property test asynchronously with default configuration, returning an F# Async that produces a report.
    /// This is non-blocking and properly handles async properties without blocking threads.
    let reportAsync (p : Property<unit>) : Async<Report> =
        p |> reportAsyncWith PropertyConfig.defaults

    /// Runs a property test asynchronously with custom configuration, throwing an exception if it fails.
    /// This is non-blocking and properly handles async properties. Returns an F# Async computation.
    let checkAsyncWith (config : IPropertyConfig) (p : Property<unit>) : Async<unit> =
        async {
            let! report = reportAsyncWith config p
            return Report.tryRaise report
        }

    /// Runs a property test asynchronously with default configuration, throwing an exception if it fails.
    /// This is non-blocking and properly handles async properties. This is the recommended way to test async code.
    let checkAsync (p : Property<unit>) : Async<unit> =
        async {
            let! report = reportAsync p
            return Report.tryRaise report
        }

    /// Runs a boolean property test asynchronously with custom configuration, returning an F# Async that produces a report.
    /// Converts false results to failures. This is non-blocking and properly handles async properties.
    let reportBoolAsyncWith (config : IPropertyConfig) (p : Property<bool>) : Async<Report> =
        p |> falseToFailure |> reportAsyncWith config

    /// Runs a boolean property test asynchronously with default configuration, returning an F# Async that produces a report.
    /// Converts false results to failures. This is non-blocking and properly handles async properties.
    let reportBoolAsync (p : Property<bool>) : Async<Report> =
        p |> falseToFailure |> reportAsync

    /// Runs a boolean property test asynchronously with custom configuration, throwing an exception if it fails.
    /// Converts false results to failures. Returns an F# Async computation.
    let checkBoolAsyncWith (config : IPropertyConfig) (p : Property<bool>) : Async<unit> =
        p |> falseToFailure |> checkAsyncWith config

    /// Runs a boolean property test asynchronously with default configuration, throwing an exception if it fails.
    /// Converts false results to failures. This is the recommended way to test async boolean properties.
    let checkBoolAsync (p : Property<bool>) : Async<unit> =
        p |> falseToFailure |> checkAsync

#if !FABLE_COMPILER
    /// Runs a property test asynchronously with custom configuration, returning a C# Task that produces a report.
    /// This is non-blocking and properly handles async properties. Use this from C# or when you need Task-based APIs.
    let reportTaskWith (config : IPropertyConfig) (p : Property<unit>) : System.Threading.Tasks.Task<Report> =
        p |> reportAsyncWith config |> Async.StartAsTask

    /// Runs a property test asynchronously with default configuration, returning a C# Task that produces a report.
    /// This is non-blocking and properly handles async properties. Use this from C# or when you need Task-based APIs.
    let reportTask (p : Property<unit>) : System.Threading.Tasks.Task<Report> =
        p |> reportAsync |> Async.StartAsTask

    /// Runs a property test asynchronously with custom configuration, throwing an exception if it fails.
    /// Returns a C# Task. This is non-blocking and properly handles async properties. Use this from C# test frameworks.
    let checkTaskWith (config : IPropertyConfig) (p : Property<unit>) : System.Threading.Tasks.Task<unit> =
        p |> checkAsyncWith config |> Async.StartAsTask

    /// Runs a property test asynchronously with default configuration, throwing an exception if it fails.
    /// Returns a C# Task. This is non-blocking and recommended for testing async C# code.
    let checkTask (p : Property<unit>) : System.Threading.Tasks.Task<unit> =
        p |> checkAsync |> Async.StartAsTask

    /// Runs a boolean property test asynchronously with custom configuration, returning a C# Task that produces a report.
    /// Converts false results to failures. Use this from C# or when you need Task-based APIs.
    let reportBoolTaskWith (config : IPropertyConfig) (p : Property<bool>) : System.Threading.Tasks.Task<Report> =
        p |> reportBoolAsyncWith config |> Async.StartAsTask

    /// Runs a boolean property test asynchronously with default configuration, returning a C# Task that produces a report.
    /// Converts false results to failures. Use this from C# or when you need Task-based APIs.
    let reportBoolTask (p : Property<bool>) : System.Threading.Tasks.Task<Report> =
        p |> reportBoolAsync |> Async.StartAsTask

    /// Runs a boolean property test asynchronously with custom configuration, throwing an exception if it fails.
    /// Converts false results to failures. Returns a C# Task. Use this from C# test frameworks.
    let checkBoolTaskWith (config : IPropertyConfig) (p : Property<bool>) : System.Threading.Tasks.Task<unit> =
        p |> checkBoolAsyncWith config |> Async.StartAsTask

    /// Runs a boolean property test asynchronously with default configuration, throwing an exception if it fails.
    /// Converts false results to failures. Returns a C# Task. Recommended for testing async boolean C# code.
    let checkBoolTask (p : Property<bool>) : System.Threading.Tasks.Task<unit> =
        p |> checkBoolAsync |> Async.StartAsTask
#endif

/// Computation expression builder for properties, enabling F# computation expression syntax.
/// Use the 'property' builder to write tests in a natural, imperative style while maintaining functional purity.
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

        // Allow binding bare Async<'a> - automatically converts to async Property<'a>.
        member __.Bind(m : Async<'a>, k : 'a -> Property<'b>) : Property<'b> =
            Property.ofAsync m |> Property.bind k

        // Allow binding Property<Async<'a>> - automatically converts to async Property<'a>.
        member inline __.Bind(m : Property<Async<'a>>, k : 'a -> Property<'b>) : Property<'b> =
            m |> Property.bind (fun asyncVal -> Property.ofAsync asyncVal |> Property.bind k)

        member __.Return(b : bool) : Property<unit> =
            Property.ofBool b

        // Allow returning Async<unit> directly - automatically converts to async Property<unit>
        member __.Return(asyncUnit : Async<unit>) : Property<unit> =
            Property.ofAsync asyncUnit

        member __.BindReturn(m : Gen<'a>, f: 'a -> 'b) =
            m
            |> Gen.map (fun a -> Lazy.constant ((Journal.singleton (fun () -> GeneratedValue (box a))), Success a))
            |> Property.ofGen
            |> Property.map f

        // BindReturn for Property<'a> - handles let! x = prop in return f x
        // Uses bind + return instead of map to preserve shrinking behavior
        member __.BindReturn(m : Property<'a>, f: 'a -> 'b) : Property<'b> =
            m |> Property.bind (fun a -> Property.success (f a))

        // BindReturn for Async<'a> - handles let! x = async { ... } in return f x.
        member __.BindReturn(m : Async<'a>, f: 'a -> 'b) : Property<'b> =
            Property.ofAsync m |> Property.map f

        member __.MergeSources(ga, gb) =
            Gen.zip ga gb

        member __.ReturnFrom(m : Property<'a>) : Property<'a> =
            m

        // Allow return! with Async<'a> - automatically converts to async Property<'a>.
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
        // Allow binding bare Task<'a> - automatically converts to async Property<'a>.
        member __.Bind(m : System.Threading.Tasks.Task<'a>, k : 'a -> Property<'b>) : Property<'b> =
            Property.ofTask m |> Property.bind k

        // Allow binding Property<Task<'a>> - automatically converts to async Property<'a>.
        member inline __.Bind(m : Property<System.Threading.Tasks.Task<'a>>, k : 'a -> Property<'b>) : Property<'b> =
            m |> Property.bind (fun taskVal -> Property.ofTask taskVal |> Property.bind k)

        // Allow returning Task<unit> directly - automatically converts to async Property<unit>.
        member __.Return(taskUnit : System.Threading.Tasks.Task<unit>) : Property<unit> =
            Property.ofTaskUnit taskUnit

        // Allow returning Task directly - automatically converts to async Property<unit>.
        member __.Return(task : System.Threading.Tasks.Task) : Property<unit> =
            Property.ofTaskUnit task

        // BindReturn for Task<'a> - handles let! x = task { ... } in return f x.
        member __.BindReturn(m : System.Threading.Tasks.Task<'a>, f: 'a -> 'b) : Property<'b> =
            Property.ofTask m |> Property.map f

        // Allow return! with Task<'a> - automatically converts to async Property<'a>.
        member __.ReturnFrom(taskValue : System.Threading.Tasks.Task<'a>) : Property<'a> =
            Property.ofTask taskValue

        // Allow return! with Task (non-generic) - automatically converts to async Property<unit>.
        member __.ReturnFrom(task : System.Threading.Tasks.Task) : Property<unit> =
            Property.ofTaskUnit task
#endif

    /// The property computation expression builder.
    /// Use this to write property tests using F#'s computation expression syntax: property { ... }
    /// Supports let! for generators and async/task values, return for values, and standard control flow.
    let property = Builder ()
