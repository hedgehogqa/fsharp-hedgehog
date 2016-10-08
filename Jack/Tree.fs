#light
namespace Jack

open FSharpx.Collections

/// A rose tree which represents a random generated outcome, and all the ways
/// in which it can be made smaller.
type Tree<'a> =
    | Node of 'a * LazyList<Tree<'a>>

module Tree =
    /// The generated outcome.
    let outcome (Node (x, _) : Tree<'a>) : 'a =
        x

    /// All the possible shrinks of this outcome. This should be ordered
    /// smallest to largest as if property still fails with the first shrink in
    /// the list then we will commit to that path and none of the others will
    /// be tried (i.e. there is no backtracking).
    let shrinks (Node (_, xs) : Tree<'a>) : LazyList<Tree<'a>> =
        xs

    /// Create a tree with a single outcome and no shrinks.
    let result (x : 'a) : Tree<'a> =
        Node (x, LazyList.empty)

    /// Map over a tree.
    let rec map (f : 'a -> 'b) (Node (x, xs) : Tree<'a>) : Tree<'b> =
        Node (f x, LazyList.map (map f) xs)

    let rec bind (Node (x, xs0) : Tree<'a>) (k : 'a -> Tree<'b>) : Tree<'b> =
        match k x with
        | Node (y, ys) ->
            let xs = LazyList.map (fun m -> bind m k) xs0
            Node (y, LazyList.append xs ys)

    let join (xss : Tree<Tree<'a>>) : Tree<'a> =
        bind xss id

    /// Fold over a tree.
    let rec fold (f : 'a -> 'x -> 'b) (g : LazyList<'b> -> 'x) (Node (x, xs) : Tree<'a>) : 'b =
        f x (foldForest f g xs)

    /// Fold over a list of trees.
    and foldForest (f : 'a -> 'x -> 'b) (g : LazyList<'b> -> 'x) (xs : LazyList<Tree<'a>>) : 'x =
        LazyList.map (fold f g) xs |> g

    /// Build a tree from an unfolding function and a seed value.
    let rec unfold (f : 'b -> 'a) (g : 'b -> LazyList<'b>) (x : 'b) : Tree<'a> =
        Node (f x, unfoldForest f g x)

    /// Build a list of trees from an unfolding function and a seed value.
    and unfoldForest (f : 'b -> 'a) (g : 'b -> LazyList<'b>) (x : 'b) : LazyList<Tree<'a>> =
        g x |> LazyList.map (unfold f g)

    /// Apply an additional unfolding function to an existing tree.
    ///
    /// The root outcome remains intact, only the shrinks are affected, this
    /// applies recursively, so shrinks can only ever be added using this
    /// function.
    ///
    /// If you want to replace the shrinks altogether, try:
    ///
    /// <c>Tree.unfold f (outcome oldTree)</c>
    ///
    let rec expand (f : 'a -> LazyList<'a>) (Node (x, xs) : Tree<'a>) : Tree<'a> =
        //
        // Ideally we could put the 'unfoldForest' nodes before the 'map expandTree'
        // nodes, so that we're culling from the top down and we would be able to
        // terminate our search faster, but this prevents minimal shrinking.
        //
        // We'd need some kind of tree transpose to do this properly.
        //
        let ys = LazyList.map (expand f) xs
        let zs = unfoldForest id f x
        Node (x, LazyList.append ys zs)

    /// Recursively discard any shrinks whose outcome does not pass the predicate.
    /// <i>Note that the root outcome can never be discarded</i>
    let rec filter (f : 'a -> bool) (Node (x, xs) : Tree<'a>) : Tree<'a> =
        Node (x, filterForest f xs)

    /// Recursively discard any trees whose outcome does not pass the predicate.
    and filterForest (f : 'a -> bool) (xs : LazyList<Tree<'a>>) : LazyList<Tree<'a>> =
        LazyList.filter (f << outcome) xs
        |> LazyList.map (filter f)
