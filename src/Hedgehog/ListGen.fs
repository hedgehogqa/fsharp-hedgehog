module Hedgehog.ListGen

let traverse (f: 'a -> Gen<'b>) (list: List<'a>) : Gen<List<'b>> =
    let rec loop input output =
        match input with
        | [] -> output |> List.rev |> Gen.constant
        | a :: input ->
            gen {
                let! b = f a
                return! loop input (b :: output)
            }
    loop list []

let sequence (gens : List<Gen<'a>>) : Gen<List<'a>> =
    gens |> traverse id
