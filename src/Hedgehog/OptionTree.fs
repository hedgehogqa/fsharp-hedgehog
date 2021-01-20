[<RequireQualifiedAccess>]
module internal Hedgehog.OptionTree

let traverse (f: 'a -> Tree<'b>) (maybeTree: 'a option) : Tree<'b option> =
  match maybeTree with
  | None -> None |> Tree.singleton
  | Some t -> t |> f |> Tree.map Some

let sequence (maybeTree: Tree<'a> option) : Tree<'a option> =
  traverse id maybeTree
