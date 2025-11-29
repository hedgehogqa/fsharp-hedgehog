namespace Hedgehog

/// Internal discriminated union representing either a synchronous or asynchronous property result
[<RequireQualifiedAccess>]
type internal PropertyResult<'a> =
    | Sync of Journal * Outcome<'a>
    | Async of Async<Journal * Outcome<'a>>

[<RequireQualifiedAccess>]
module internal PropertyResult =

    /// Create a synchronous PropertyResult from a journal and outcome
    let ofSync (journal : Journal) (outcome : Outcome<'a>) : PropertyResult<'a> =
        PropertyResult.Sync (journal, outcome)

    /// Create an async PropertyResult from an async computation that produces a journal and outcome
    let ofAsync (asyncResult : Async<Journal * Outcome<'a>>) : PropertyResult<'a> =
        PropertyResult.Async asyncResult

    /// Create an async PropertyResult by capturing exceptions from an async computation
    let ofAsyncWith (asyncComputation : Async<'a>) : PropertyResult<'a> =
        PropertyResult.Async (async {
            try
                let! result = asyncComputation
                return (Journal.empty, Success result)
            with
#if !FABLE_COMPILER
            | :? System.OperationCanceledException ->
                return (Journal.singletonMessage "Async computation was canceled", Failure)
#endif
            | ex ->
                return (Journal.singletonMessage (string ex), Failure)
        })

    /// Apply a function to the outcome, handling both sync and async cases
    let map (f : Journal * Outcome<'a> -> Journal * Outcome<'b>) 
            (result : PropertyResult<'a>) : PropertyResult<'b> =
        match result with
        | PropertyResult.Sync (journal, outcome) -> 
            PropertyResult.Sync (f (journal, outcome))
        | PropertyResult.Async asyncResult ->
            PropertyResult.Async (async {
                let! result = asyncResult
                return f result
            })

    /// Bind operation that flattens nested PropertyResults
    let bind (f : Journal * Outcome<'a> -> PropertyResult<'b>)
             (result : PropertyResult<'a>) : PropertyResult<'b> =
        match result with
        | PropertyResult.Sync (journal, outcome) ->
            f (journal, outcome)
        | PropertyResult.Async asyncResult ->
            PropertyResult.Async (async {
                let! journal, outcome = asyncResult
                match f (journal, outcome) with
                | PropertyResult.Sync (j, o) -> return (j, o)
                | PropertyResult.Async innerAsync -> return! innerAsync
            })

    /// Combine two PropertyResults, applying a function to their values
    let map2 (f : Journal * Outcome<'a> -> Journal * Outcome<'b> -> Journal * Outcome<'c>)
             (left : PropertyResult<'a>)
             (right : PropertyResult<'b>) : PropertyResult<'c> =
        match left, right with
        | PropertyResult.Sync (j1, o1), PropertyResult.Sync (j2, o2) -> 
            PropertyResult.Sync (f (j1, o1) (j2, o2))
        | PropertyResult.Sync (j1, o1), PropertyResult.Async asyncRight ->
            PropertyResult.Async (async {
                let! rightResult = asyncRight
                return f (j1, o1) rightResult
            })
        | PropertyResult.Async asyncLeft, PropertyResult.Sync (j2, o2) ->
            PropertyResult.Async (async {
                let! leftResult = asyncLeft
                return f leftResult (j2, o2)
            })
        | PropertyResult.Async asyncLeft, PropertyResult.Async asyncRight ->
            PropertyResult.Async (async {
                let! leftResult = asyncLeft
                let! rightResult = asyncRight
                return f leftResult rightResult
            })

    /// Unwrap synchronously (blocking for async case - for backward compatibility)
    let toSync (result : PropertyResult<'a>) : Journal * Outcome<'a> =
        match result with
        | PropertyResult.Sync (journal, outcome) -> (journal, outcome)
        | PropertyResult.Async asyncResult -> 
#if FABLE_COMPILER
            failwith "Synchronous unwrapping of async PropertyResult is not supported in Fable. Use Property.checkAsync or Property.reportAsync instead of Property.check or Property.report."
#else
            Async.RunSynchronously asyncResult
#endif

    /// Unwrap a lazy PropertyResult synchronously
    let unwrapSync (lazyResult : Lazy<PropertyResult<'a>>) : Journal * Outcome<'a> =
        toSync lazyResult.Value

    /// Unwrap a lazy PropertyResult asynchronously without blocking
    let unwrapAsync (lazyResult : Lazy<PropertyResult<'a>>) : Async<Journal * Outcome<'a>> =
        async {
            match lazyResult.Value with
            | PropertyResult.Sync (journal, outcome) -> 
                return (journal, outcome)
            | PropertyResult.Async asyncResult ->
                return! asyncResult
        }
