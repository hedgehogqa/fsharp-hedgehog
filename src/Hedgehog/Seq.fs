[<RequireQualifiedAccess>]
module internal Seq

let inline cons (x : 'a) (xs : seq<'a>) : seq<'a> =
    seq {
        yield x
        yield! xs
    }

let inline consNub (x : 'a) (ys0 : seq<'a>) : seq<'a> =
    match Seq.tryHead ys0 with
    | None -> Seq.singleton x
    | Some y ->
        if x = y then
            ys0
        else
            cons x ys0

let inline join (xss: 'a seq seq) : seq<'a> =
    Seq.collect id xss
