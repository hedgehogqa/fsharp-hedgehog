namespace Hedgehog

type Outcome<'a> =
    | Failure
    | Discard
    | Success of 'a

module Outcome =

    let cata result failure discard success =
        match result with
        | Failure ->
            failure()
        | Discard ->
            discard()
        | Success(x) ->
            success(x)

    [<CompiledName("Map")>]
    let map (f : 'a -> 'b) (result : Outcome<'a>) : Outcome<'b> =
        cata result
            (always Failure)
            (always Discard)
            (f >> Success)

    [<CompiledName("Filter")>]
    let filter (f : 'a -> bool) (result : Outcome<'a>) : Outcome<'a> =
        let successOrDiscard x =
            if f x then
                Success(x)
            else
                Discard

        cata result
            (always Failure)
            (always Discard)
            successOrDiscard

    [<CompiledName("IsFailure")>]
    let isFailure (result : Outcome<'a>) : bool =
        cata result
            (always true)
            (always false)
            (always false)
