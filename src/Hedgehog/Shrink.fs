namespace Hedgehog

open FSharpx.Collections

module LazyList =
    let singleton (x : 'a) : LazyList<'a> =
        LazyList.cons x LazyList.empty

    let concatMap (f : 'a -> LazyList<'b>) (xs : LazyList<'a>) : LazyList<'b> =
        LazyList.concat <|
        LazyList.map f xs

    let consNub (x : 'a) (ys0 : LazyList<'a>) : LazyList<'a> =
        match ys0 with
        | LazyList.Nil ->
            singleton x
        | LazyList.Cons (y, ys) ->
            if x = y then
                LazyList.cons y ys
            else
                LazyList.cons x <| LazyList.cons y ys

module Shrink =
    /// Produce all permutations of removing 'k' elements from a list.
    let removes (k0 : int) (xs0 : List<'a>) : LazyList<List<'a>> =
        let rec loop (k : int) (n : int) (xs : List<'a>) : LazyList<List<'a>> =
            let hd = List.take k xs
            let tl = List.skip k xs
            if k > n then
                LazyList.empty
            elif List.isEmpty tl then
                LazyList.singleton List.empty
            else
                LazyList.consDelayed tl <| fun _ ->
                    LazyList.map (fun x -> List.append hd x) (loop k (n - k) tl)
        loop k0 (List.length xs0) xs0

    /// Produce a list containing the progressive halving of an integral.
    let inline halves (n : ^a) : LazyList<'a> =
        let go x =
            let zero : ^a = LanguagePrimitives.GenericZero
            if x = zero then
                None
            else
                let one : ^a = LanguagePrimitives.GenericOne
                let two : ^a = one + one
                let x' = x / two
                Some (x, x')
        LazyList.unfold go n

    /// Shrink a list by edging towards the empty list.
    /// Note we always try the empty list first, as that is the optimal shrink.
    let list (xs : List<'a>) : LazyList<List<'a>> =
        LazyList.concatMap (fun k -> removes k xs) (halves <| List.length xs)

    /// Shrink each of the elements in input list using the supplied shrinking
    /// function.
    let rec elems (shrink : 'a -> LazyList<'a>) (xs00 : List<'a>) : LazyList<List<'a>> =
        match xs00 with
        | [] ->
            LazyList.empty
        | x0 :: xs0 ->
            let ys = LazyList.map (fun x1 -> x1 :: xs0) (shrink x0)
            let zs = LazyList.map (fun xs1 -> x0 :: xs1) (elems shrink xs0)
            LazyList.append ys zs

    /// Turn a list of trees in to a tree of lists, using the supplied function to
    /// merge shrinking options.
    let rec sequence (merge : List<Tree<'a>> -> LazyList<List<Tree<'a>>>) (xs : List<Tree<'a>>) : Tree<List<'a>> =
        let y = List.map Tree.outcome xs
        let ys = LazyList.map (sequence merge) (merge xs)
        Node (y, ys)

    /// Turn a list of trees in to a tree of lists, opting to shrink both the list
    /// itself and the elements in the list during traversal.
    let sequenceList (xs0 : List<Tree<'a>>) : Tree<List<'a>> =
        sequence (fun xs ->
            LazyList.append (list xs) (elems Tree.shrinks xs)) xs0

    /// Turn a list of trees in to a tree of lists, opting to shrink only the
    /// elements of the list (i.e. the size of the list will always be the same).
    let sequenceElems (xs0 : List<Tree<'a>>) : Tree<List<'a>> =
        sequence (fun xs ->
            elems Tree.shrinks xs) xs0

    /// Shrink an integral number by edging towards a destination.
    let inline towards (destination : ^a) (x : ^a) : LazyList<'a> =
        if destination = x then
            LazyList.empty
        else
            let one : ^a = LanguagePrimitives.GenericOne
            let two : ^a = one + one

            /// We need to halve our operands before subtracting them as they may be using
            /// the full range of the type (i.e. 'MinValue' and 'MaxValue' for 'Int32')
            let diff : ^a = (x / two) - (destination / two)

            LazyList.consNub destination <|
            LazyList.map (fun y -> x - y) (halves diff)

    /// Shrink a floating-point number by edging towards a destination.
    /// Note we always try the destination first, as that is the optimal shrink.
    let towardsDouble (destination : double) (x : double) : LazyList<double> =
        if destination = x then
            LazyList.empty
        else
            let diff =
                x - destination

            let go n =
                let x' = x - n
                if  x' <> x then
                    Some (x', n / 2.0)
                else
                    None
            LazyList.unfold go diff

