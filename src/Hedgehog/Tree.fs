namespace Hedgehog

type Tree<'a> =
    | Node of 'a * seq<Tree<'a>>

module Tree =

    let root (Node (root, _)) =
        root

    let children (Node (_, children)) =
        children

    let singleton value =
        Node (value, Seq.empty)

    let addChild child parent =
        let (Node (x, xs)) = parent
        Node (x, Seq.cons child xs)

    let addChildValue value tree =
        tree |> addChild (singleton value)

    let rec cata f tree =
        let (Node (x, xs)) = tree
        f x (Seq.map (cata f) xs)

    let depth tree =
        tree |> cata (fun _ -> Seq.fold max -1 >> (+) 1)

    let toSeq tree =
        tree |> cata (fun a -> Seq.join >> Seq.cons a)

    let rec map f tree =
        let (Node (x, xs)) = tree
        Node (f x, Seq.map (map f) xs)

    let mapWithSubtrees f tree =
        tree |> cata (fun a subtrees -> Node (f a subtrees, subtrees))

    let rec bind k tree =
        match k (root tree) with
        | Node (y, ys) ->
            let xs = Seq.map (bind k) (children tree)
            Node (y, Seq.append xs ys)

    let join trees =
        bind id trees

    let rec duplicate tree =
        Node (tree, Seq.map duplicate (children tree))

    let rec fold f g tree =
        let (Node (x, xs)) = tree
        f x (foldForest f g xs)

    and foldForest f g trees =
        Seq.map (fold f g) trees |> g

    let rec unfold (rootSelector : 'b -> 'a) (forestSelector : 'b -> seq<'b>) seed : Tree<'a> =
        let root = seed |> rootSelector
        let children = seed |> unfoldForest rootSelector forestSelector
        Node (root, children)

    and unfoldForest rootSelector forestSelector seed =
        let mapper = unfold rootSelector forestSelector
        seed
        |> forestSelector
        |> Seq.map mapper

    let rec expand mapping tree =
        //
        // Ideally we could put the 'unfoldForest' nodes before the 'map expandTree'
        // nodes, so that we're culling from the top down and we would be able to
        // terminate our search faster, but this prevents minimal shrinking.
        //
        // We'd need some kind of tree transpose to do this properly.
        //
        let root = root tree
        let children = Seq.map (expand mapping) (children tree)
        let forest = unfoldForest id mapping root
        Node (root, Seq.append children forest)

    let rec filter predicate tree =
        Node (root tree, filterForest predicate (children tree))

    and filterForest predicate trees =
        trees
        |> Seq.filter (predicate << root)
        |> Seq.map (filter predicate)

    let rec renderList tree =
        let mapFirstDifferently f g = function
            | [] -> []
            | x :: xs -> (f x) :: (xs |> List.map g)
        let mapLastDifferently f g = List.rev >> mapFirstDifferently g f >> List.rev

        let (Node (x, xs0)) = tree

        let xs =
            xs0
            |> Seq.map renderList
            |> Seq.toList
            |> mapLastDifferently
                (mapFirstDifferently ((+) "├-")
                                     ((+) "| "))
                (mapFirstDifferently ((+) "└-")
                                     ((+) "  "))
            |> List.concat

        x :: xs

    let render tree =
        renderList tree
        |> Seq.reduce (fun a b ->
            a + System.Environment.NewLine + b)
