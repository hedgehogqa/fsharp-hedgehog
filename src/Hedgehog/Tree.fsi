namespace Hedgehog

/// A rose tree which represents a root value and its children.
type Tree<'a> =
    | Node of 'a * seq<Tree<'a>>

module Tree =

    /// Get the root of the tree.
    val root : Tree<'a> -> 'a

    /// Get the children of the tree.
    val children : Tree<'a> -> seq<Tree<'a>>

    /// Create a tree with no children.
    val singleton : 'a -> Tree<'a>

    /// Add a child to the tree.
    val addChild : Tree<'a> -> Tree<'a> -> Tree<'a>

    /// Add a value to the tree, by wrapping it as a singleton.
    val addChildValue : 'a -> Tree<'a> -> Tree<'a>

    val cata : ('a -> 'b seq -> 'b) -> Tree<'a> -> 'b

    /// Finds the maximum depth of the tree.
    val depth : Tree<'a> -> int32

    /// Converts a tree to an enumerable sequence.
    val toSeq : Tree<'a> -> 'a seq

    /// Map over a tree.
    val map : ('a -> 'b) -> Tree<'a> -> Tree<'b>

    val mapWithSubtrees : ('a -> seq<Tree<'b>> -> 'b) -> Tree<'a> -> Tree<'b>

    val bind : ('a -> Tree<'b>) -> Tree<'a> -> Tree<'b>

    val join : Tree<Tree<'a>> -> Tree<'a>

    /// Turns a tree, in to a tree of trees. Useful for testing Hedgehog itself as
    /// it allows you to observe the children for a value inside a property,
    /// while still allowing the property to shrink to a minimal
    /// counterexample.
    val duplicate : Tree<'a> -> Tree<Tree<'a>>

    /// Fold over a tree.
    val fold : ('a -> 'x -> 'b) -> (seq<'b> -> 'x) -> Tree<'a> -> 'b

    /// Fold over a list of trees.
    val foldForest : ('a -> 'x -> 'b) -> (seq<'b> -> 'x) -> seq<Tree<'a>> -> 'x

    /// Build a tree from an unfolding function and a seed value.
    val unfold : ('b -> 'a) -> ('b -> seq<'b>) -> 'b -> Tree<'a>

    /// Build a list of trees from an unfolding function and a seed value.
    val unfoldForest : ('b -> 'a) -> ('b -> seq<'b>) -> 'b -> seq<Tree<'a>>

    /// Apply an additional unfolding function to an existing tree.
    ///
    /// The root remains intact, only the children are affected, this
    /// applies recursively, so children can only ever be added using this
    /// function.
    ///
    /// If you want to replace the children altogether, try:
    ///
    /// Tree.unfold f (root oldTree)
    ///
    val expand : ('a -> seq<'a>) -> Tree<'a> -> Tree<'a>

    /// Recursively discard any children whose root does not pass the predicate.
    /// Note that the root can never be discarded.
    val filter : ('a -> bool) -> Tree<'a> -> Tree<'a>

    /// Recursively discard any trees whose root does not pass the predicate.
    val filterForest : ('a -> bool) -> seq<Tree<'a>> -> seq<Tree<'a>>

    /// Generates a formatted string.
    val render : Tree<string> -> string

    /// Generates a formatted sequence of strings.
    val renderList : Tree<string> -> List<string>
