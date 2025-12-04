[<RequireQualifiedAccess>]
module Hedgehog.Exceptions

open System

/// Recursively unwraps wrapper exceptions to get to the actual meaningful exception.
/// Unwraps single-inner AggregateException (from async/tasks).
let rec unwrap (e : exn) : exn =
#if FABLE_COMPILER
    e
#else
    match e with
    | :? AggregateException as ae when ae.InnerExceptions.Count = 1 ->
        unwrap ae.InnerExceptions[0]
    | _ -> e
#endif
