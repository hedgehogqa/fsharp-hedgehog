[<RequireQualifiedAccess>]
module Hedgehog.ListRandom

let traverse (f: 'a -> Random<'b>) (list: List<'a>) : Random<List<'b>> =
    let rec loop input output =
        match input with
        | [] -> output |> List.rev |> Random.constant
        | a :: input ->
            random {
                let! b = f a
                return! loop input (b :: output)
            }
    loop list []

let sequence (randoms : List<Random<'a>>) : Random<List<'a>> =
    randoms |> traverse id
