[<RequireQualifiedAccess>]
module internal Seq

let inline cons (x : 'a) (xs : seq<'a>) : seq<'a> =
    seq {
        yield x
        yield! xs
    }

let inline consNub (x : 'a) (ys0 : seq<'a>) : seq<'a> =
    seq {
        match Seq.tryHead ys0 with
        | None -> yield x
        | Some y ->
            if x = y then
                yield! ys0
            else
                yield x
                yield! ys0
    }

let inline join (xss: 'a seq seq) : seq<'a> =
    Seq.collect id xss
