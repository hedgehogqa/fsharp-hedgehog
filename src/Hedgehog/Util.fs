[<AutoOpen>]
module private Hedgehog.Util

let inline always (a : 'a) (_ : 'b) : 'a =
    a
