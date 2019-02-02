namespace Hedgehog

module SeqExtra =
    let cons (x : 'a) (xs : seq<'a>) : seq<'a> = 
        seq {
            yield x
            yield! xs
        }

    let consNub (x : 'a) (ys0 : seq<'a>) : seq<'a> =
        match Seq.tryHead ys0 with
        | None -> Seq.singleton x
        | Some y ->
            if x = y then
                ys0
            else
                cons x ys0


module Shrink =
    /// Produce all permutations of removing 'k' elements from a list.
    let removes (k0 : int) (xs0 : List<'a>) : seq<List<'a>> =
        let rec loop (k : int) (n : int) (xs : List<'a>) : seq<List<'a>> =
            let hd = List.take k xs
            let tl = List.skip k xs
            if k > n then
                Seq.empty
            elif List.isEmpty tl then
                Seq.singleton List.empty
            else
                SeqExtra.cons tl (Seq.map (fun x -> List.append hd x) (loop k (n - k) tl))
        loop k0 (List.length xs0) xs0

    /// Produce a list containing the progressive halving of an integral.
    let inline halves (n : ^a) : seq<'a> =
        let go x =
            let zero : ^a = LanguagePrimitives.GenericZero
            if x = zero then
                None
            else
                let one : ^a = LanguagePrimitives.GenericOne
                let two : ^a = one + one
                let x' = x / two
                Some (x, x')
        Seq.unfold go n

    /// Shrink a list by edging towards the empty list.
    /// Note we always try the empty list first, as that is the optimal shrink.
    let list (xs : List<'a>) : seq<List<'a>> =
        Seq.concat <| Seq.map (fun k -> removes k xs) (halves <| List.length xs)

    /// Shrink each of the elements in input list using the supplied shrinking
    /// function.
    let rec elems (shrink : 'a -> seq<'a>) (xs00 : List<'a>) : seq<List<'a>> =
        match xs00 with
        | [] ->
            Seq.empty
        | x0 :: xs0 ->
            let ys = Seq.map (fun x1 -> x1 :: xs0) (shrink x0)
            let zs = Seq.map (fun xs1 -> x0 :: xs1) (elems shrink xs0)
            Seq.append ys zs

    /// Turn a list of trees in to a tree of lists, using the supplied function to
    /// merge shrinking options.
    let rec sequence (merge : List<Tree<'a>> -> seq<List<Tree<'a>>>) (xs : List<Tree<'a>>) : Tree<List<'a>> =
        let y = List.map Tree.outcome xs
        let ys = Seq.map (sequence merge) (merge xs)
        Node (y, ys)

    /// Turn a list of trees in to a tree of lists, opting to shrink both the list
    /// itself and the elements in the list during traversal.
    let sequenceList (xs0 : List<Tree<'a>>) : Tree<List<'a>> =
        sequence (fun xs ->
            Seq.append (list xs) (elems Tree.shrinks xs)) xs0

    /// Turn a list of trees in to a tree of lists, opting to shrink only the
    /// elements of the list (i.e. the size of the list will always be the same).
    let sequenceElems (xs0 : List<Tree<'a>>) : Tree<List<'a>> =
        sequence (fun xs ->
            elems Tree.shrinks xs) xs0

    /// Shrink an integral number by edging towards a destination.
    let inline towards (destination : ^a) (x : ^a) : seq<'a> =
        if destination = x then
            Seq.empty
        else
            let one : ^a = LanguagePrimitives.GenericOne
            let two : ^a = one + one

            /// We need to halve our operands before subtracting them as they may be using
            /// the full range of the type (i.e. 'MinValue' and 'MaxValue' for 'Int32')
            let diff : ^a = (x / two) - (destination / two)

            SeqExtra.consNub destination <| Seq.map (fun y -> x - y) (halves diff)

    /// Shrink a floating-point number by edging towards a destination.
    /// Note we always try the destination first, as that is the optimal shrink.
    let towardsDouble (destination : double) (x : double) : seq<double> =
        if destination = x then
            Seq.empty
        else
            let diff =
                x - destination

            let go n =
                let x' = x - n
                if  x' <> x then
                    Some (x', n / 2.0)
                else
                    None

            Seq.unfold go diff

