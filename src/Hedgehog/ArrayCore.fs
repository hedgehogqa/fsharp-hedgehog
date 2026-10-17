namespace Hedgehog.FSharp

open Hedgehog

/// The core of the generators of collections: 'Gen.list', 'Gen.array' and 'Gen.seq'.
///
/// It keeps the trees of the elements in one array, where 'Shrink.sequenceList' keeps them in a list, and builds
/// a collection from the array of their outcomes. It generates the same values and the same shrinks, in the same
/// order, as the generator on lists did, so that the recorded rechecks keep working:
///
/// - The length is drawn first, and then the elements, with the same splitting of the seed.
/// - A collection shrinks to the empty one first, then by removing blocks of n/2, n/4, ... 1 elements, as
///   'Shrink.list' does, and then by shrinking its elements one by one, as 'Shrink.elems' does.
/// - No shrink is shorter than the lower bound of the range, as 'Tree.filter' made sure.
///
/// The tests compare it with the generator on lists, so that the two do not drift apart.
module internal ArrayCore =

    /// The array without the block of 'count' elements that starts at 'start'.
    let private removeBlock (trees : Tree<'a>[]) (start : int) (count : int) : Tree<'a>[] =
        let rest = Array.zeroCreate (trees.Length - count)
        Array.blit trees 0 rest 0 start
        Array.blit trees (start + count) rest start (trees.Length - start - count)
        rest

    /// A copy of the array with the element at 'index' replaced.
    let private replaceAt (trees : Tree<'a>[]) (index : int) (tree : Tree<'a>) : Tree<'a>[] =
        let copy = Array.copy trees
        copy.[index] <- tree
        copy

    /// A new array of the outcomes of the trees.
    let private outcomes (trees : Tree<'a>[]) : 'a[] =
        let xs = Array.zeroCreate trees.Length
        for i in 0 .. trees.Length - 1 do
            let (Node (x, _)) = trees.[i]
            xs.[i] <- x
        xs

    /// Turn an array of trees in to a tree of collections, opting to shrink both the collection itself and its
    /// elements, like 'Shrink.sequenceList' does for a list, and not to go below the minimum length.
    ///
    /// There are at least 'minLength' trees: the first collection is not shorter than that, and none of its shrinks is.
    ///
    /// 'build' gets a new array of the outcomes for every node of the tree. The array is never used again by this
    /// function, so a collection is free to keep it.
    let rec sequence (minLength : int) (build : 'a[] -> 'c) (trees : Tree<'a>[]) : Tree<'c> =
        Node (build (outcomes trees), shrinks minLength build trees)

    and private shrinks (minLength : int) (build : 'a[] -> 'c) (trees : Tree<'a>[]) : seq<Tree<'c>> =
        let length = trees.Length
        if length = 0 then
            Seq.empty
        else
            seq {
                // Remove blocks of n, n/2, ... 1 elements. The first block is the whole collection, so the empty
                // collection is tried first, as it is the optimal shrink.
                let mutable blockSize = length
                while blockSize > 0 do
                    // A collection that would be shorter than the range allows is not built at all.
                    if length - blockSize >= minLength then
                        for block in 0 .. length / blockSize - 1 do
                            yield sequence minLength build (removeBlock trees (block * blockSize) blockSize)
                    blockSize <- blockSize / 2

                // Shrink the elements one by one.
                for i in 0 .. length - 1 do
                    for shrunk in Tree.shrinks trees.[i] do
                        yield sequence minLength build (replaceAt trees i shrunk)
            }

    /// Generate a collection, using a 'Range' to determine its length and a 'Random' of the trees of the elements.
    let generate (build : 'a[] -> 'c) (range : Range<int>) (element : Random<Tree<'a>>) : Random<Tree<'c>> =
        // The elements are drawn like 'Random.replicate' draws them: the size is passed on as it is.
        let (Hedgehog.Random drawElement) = element
        Hedgehog.Random (fun seed size ->
            let lower, upper = Range.bounds size range
            let lengthSeed, elementsSeed = Seed.split seed
            let length, _ = Seed.nextInt32 lower upper lengthSeed
            if length < 0 then
                invalidArg "range" (sprintf "The range gave the length of %d, but a collection cannot have a negative length." length)
            let trees = Array.zeroCreate length
            let mutable restSeed = elementsSeed
            for i in 0 .. length - 1 do
                let elementSeed, nextSeed = Seed.split restSeed
                trees.[i] <- drawElement elementSeed size
                restSeed <- nextSeed
            sequence (min lower upper) build trees)
