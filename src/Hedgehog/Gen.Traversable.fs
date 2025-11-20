namespace Hedgehog

[<AutoOpen>]
module GenTraversable =

  [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
  module Gen =

    let private traverse' (f: 'a -> Gen<'b>) (ma: #seq<'a>) : Gen<List<'b>> =
      List.empty
      |> Gen.constant
      |> Seq.fold (fun glb a ->
        (fun list b -> List.append list [b])
        |> Gen.constant
        |> Gen.apply glb
        |> Gen.apply (f a))
      <| ma

    let traverse (f: 'a -> Gen<'b>) (ma: #seq<'a>) : Gen<seq<'b>> =
      traverse' f ma |> Gen.map Seq.ofList

    let sequence (gens : #seq<Gen<'a>>) : Gen<seq<'a>> =
        gens |> traverse id

    let traverseList (f: 'a -> Gen<'b>) (ma: List<'a>) : Gen<List<'b>> =
      traverse' f ma

    let sequenceList (gens : List<Gen<'a>>) : Gen<List<'a>> =
        gens |> traverseList id
