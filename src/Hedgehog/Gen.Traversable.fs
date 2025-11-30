namespace Hedgehog.FSharp

open Hedgehog

[<AutoOpen>]
module GenTraversable =

  [<RequireQualifiedAccess>]
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

    /// Apply a generator-producing function to each element and collect the results.
    let traverse (f: 'a -> Gen<'b>) (ma: #seq<'a>) : Gen<seq<'b>> =
      traverse' f ma |> Gen.map Seq.ofList

    /// Turn a sequence of generators into a generator of a sequence.
    let sequence (gens : #seq<Gen<'a>>) : Gen<seq<'a>> =
        gens |> traverse id

    /// Apply a generator-producing function to each list element and collect the results.
    let traverseList (f: 'a -> Gen<'b>) (ma: List<'a>) : Gen<List<'b>> =
      traverse' f ma

    /// Turn a list of generators into a generator of a list.
    let sequenceList (gens : List<Gen<'a>>) : Gen<List<'a>> =
        gens |> traverseList id
