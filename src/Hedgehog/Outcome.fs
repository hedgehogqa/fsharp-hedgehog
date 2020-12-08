namespace Hedgehog

type Outcome<'a> =
    | Failure
    | Discard
    | Success of 'a

module Outcome =
    [<CompiledName("Map")>]
    let map (f : 'a -> 'b) (result : Outcome<'a>) : Outcome<'b> =
        match result with
        | Failure ->
            Failure
        | Discard ->
            Discard
        | Success x ->
            Success (f x)

    [<CompiledName("Filter")>]
    let filter (f : 'a -> bool) (result : Outcome<'a>) : Outcome<'a> =
        match result with
        | Failure ->
            Failure
        | Discard ->
            Discard
        | Success x ->
            if f x then
              Success x
            else
              Discard

    [<CompiledName("IsFailure")>]
    let isFailure (result : Outcome<'a>) : bool =
        match result with
        | Failure ->
            true
        | Discard ->
            false
        | Success _ ->
            false
