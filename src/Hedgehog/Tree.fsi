namespace Hedgehog

/// A rose tree which represents a root value and its children.
type Tree<'a> =
    | Node of 'a * seq<Tree<'a>>

module Tree =

    /// Get the root of the tree.
    val root : tree: Tree<'a> -> 'a

    /// Get the children of the tree.
    val children : tree: Tree<'a> -> seq<Tree<'a>>

    /// Create a tree with no children.
    val singleton : value: 'a -> Tree<'a>

    /// Add a child to the tree.
    val addChild : child: Tree<'a> -> parent: Tree<'a> -> Tree<'a>

    /// Add a value to the tree, by wrapping it as a singleton.
    val addChildValue : value: 'a -> parent: Tree<'a> -> Tree<'a>

    val cata : f: ('a -> 'b seq -> 'b) -> tree: Tree<'a> -> 'b

    /// Finds the maximum depth of the tree.
    val depth : tree: Tree<'a> -> int32

    /// Converts a tree to an enumerable sequence.
    val toSeq : tree: Tree<'a> -> 'a seq

    /// Map over a tree.
    val map : mapping: ('a -> 'b) -> tree: Tree<'a> -> Tree<'b>

    val mapWithSubtrees : mapping: ('a -> seq<Tree<'b>> -> 'b) -> tree: Tree<'a> -> Tree<'b>

    val bind : mapping: ('a -> Tree<'b>) -> tree: Tree<'a> -> Tree<'b>

    val join : treeOfTrees: Tree<Tree<'a>> -> Tree<'a>

    /// Turns a tree, in to a tree of trees. Useful for testing Hedgehog itself as
    /// it allows you to observe the children for a value inside a property,
    /// while still allowing the property to shrink to a minimal
    /// counterexample.
    val duplicate : tree: Tree<'a> -> Tree<Tree<'a>>

    /// Fold over a tree.
    val fold : ('a -> 'x -> 'b) -> (seq<'b> -> 'x) -> Tree<'a> -> 'b

    /// Fold over a list of trees.
    val foldForest : ('a -> 'x -> 'b) -> (seq<'b> -> 'x) -> seq<Tree<'a>> -> 'x

    /// Build a tree from an unfolding function and a seed value.
    val unfold : rootMapping: ('b -> 'a) -> forestMapping: ('b -> seq<'b>) -> seed: 'b -> Tree<'a>

    /// Build a list of trees from an unfolding function and a seed value.
    val unfoldForest : rootMapping: ('b -> 'a) -> forestMapping: ('b -> seq<'b>) -> seed: 'b -> seq<Tree<'a>>

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
    val expand : mapping: ('a -> seq<'a>) -> tree: Tree<'a> -> Tree<'a>

    /// Recursively discard any children whose root does not pass the predicate.
    /// Note that the root can never be discarded.
    val filter : predicate: ('a -> bool) -> tree: Tree<'a> -> Tree<'a>

    /// Recursively discard any trees whose root does not pass the predicate.
    val filterForest : predicate: ('a -> bool) -> trees: seq<Tree<'a>> -> seq<Tree<'a>>

    /// Generates a formatted string.
    val render : tree: Tree<string> -> string

    /// Generates a formatted sequence of strings.
    val renderList : tree: Tree<string> -> List<string>
