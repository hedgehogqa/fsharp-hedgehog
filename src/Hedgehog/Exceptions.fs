[<RequireQualifiedAccess>]
module Hedgehog.Exceptions

open System
open System.Reflection

/// Recursively unwraps wrapper exceptions to get to the actual meaningful exception.
/// Unwraps TargetInvocationException (from reflection) and single-inner AggregateException (from async/tasks).
let rec unwrap (e : exn) : exn =
#if FABLE_COMPILER
    e
#else
    match e with
    | :? TargetInvocationException as tie when not (isNull tie.InnerException) ->
        unwrap tie.InnerException
    | :? AggregateException as ae when ae.InnerExceptions.Count = 1 ->
        unwrap ae.InnerExceptions.[0]
    | _ -> e
#endif
