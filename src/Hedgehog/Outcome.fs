namespace Hedgehog

type Outcome =
    | Failure
    | Discard
    | Success

module Outcome =

    let private cata
        (outcome : Outcome)
        (failure : unit -> 'b)
        (discard : unit -> 'b)
        (success : unit -> 'b) : 'b =
        match outcome with
        | Failure ->
            failure ()
        | Discard ->
            discard ()
        | Success ->
            success ()

    let isFailure (result : Outcome) : bool =
        cata result
            (always true)
            (always false)
            (always false)
