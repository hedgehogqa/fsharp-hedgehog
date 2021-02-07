[<RequireQualifiedAccess>]
module internal Seq

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
