module Hedgehog.ListGen


let traverse (f: 'a -> Gen<'b>) (ma: list<'a>) : Gen<list<'b>> =
  List.empty
  |> Gen.constant
  |> List.fold (fun glb a ->
    (fun list b -> List.append list [b])
    |> Gen.constant
    |> Gen.apply glb
    |> Gen.apply (f a))
  <| ma

let sequence (gens : List<Gen<'a>>) : Gen<List<'a>> =
    gens |> traverse id
