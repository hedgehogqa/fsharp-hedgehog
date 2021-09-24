namespace Hedgehog

type Tree<'a> =
    | Node of 'a * seq<Tree<'a>>

module Tree =

    let root tree =
        let (Node (root, _)) = tree
        root

    let children tree =
        let (Node (_, children)) = tree
        children

    let singleton value =
        Node (value, Seq.empty)

    let addChild child parent =
        Node (root parent, Seq.cons child (children parent))

    let addChildValue value parent =
        parent |> addChild (singleton value)

    let rec cata f tree =
        f (root tree) (Seq.map (cata f) (children tree))

    let depth tree =
        tree |> cata (fun _ -> Seq.fold max -1 >> (+) 1)

    let toSeq tree =
        tree |> cata (fun a -> Seq.join >> Seq.cons a)

    let rec map mapping tree =
        let root = mapping (root tree)
        let children = Seq.map (map mapping) (children tree)
        Node (root, children)

    let mapWithSubtrees mapping tree =
        tree |> cata (fun root subtrees -> Node (mapping root subtrees, subtrees))

    let rec bind mapping tree =
        let newTree = mapping (root tree)
        let newChildren = Seq.map (bind mapping) (children tree)
        Node (root newTree, Seq.append newChildren (children newTree))

    let join treeOfTrees =
        bind id treeOfTrees

    let rec duplicate tree =
        Node (tree, Seq.map duplicate (children tree))

    let rec fold f g tree =
        children tree
        |> foldForest f g
        |> f (root tree)

    and foldForest f g trees =
        trees
        |> Seq.map (fold f g)
        |> g

    let rec unfold rootMapping (forestMapping : 'b -> seq<'b>) seed =
        let root = seed |> rootMapping
        let children = seed |> unfoldForest rootMapping forestMapping
        Node (root, children)

    and unfoldForest rootMapping forestMapping seed =
        let mapper = unfold rootMapping forestMapping
        seed
        |> forestMapping
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

        let xs =
            children tree
            |> Seq.map renderList
            |> Seq.toList
            |> mapLastDifferently
                (mapFirstDifferently ((+) "├-")
                                     ((+) "| "))
                (mapFirstDifferently ((+) "└-")
                                     ((+) "  "))
            |> List.concat

        (root tree) :: xs

    let render tree =
        renderList tree
        |> Seq.reduce (fun a b ->
            a + System.Environment.NewLine + b)
