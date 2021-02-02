// Workaround for a fable issue: https://github.com/fable-compiler/Fable/issues/2069
#if FABLE_COMPILER
module Hedgehog.ListGen
#else
module private Hedgehog.ListGen
#endif

let traverse (f: 'a -> Gen<'b>) (ma: list<'a>) : Gen<list<'b>> =
    let rec loop input output =
        match input with
        | [] -> output |> List.rev |> Gen.constant
        | a :: input ->
            gen {
                let! b = f a
                return! loop input (b :: output)
            }
    loop ma []

let sequence (gens : List<Gen<'a>>) : Gen<List<'a>> =
    gens |> traverse id
