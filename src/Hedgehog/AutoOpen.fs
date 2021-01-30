[<AutoOpen>]
module internal AutoOpen

let inline always (a : 'a) (_ : 'b) : 'a =
    a

let inline flip (f : 'a -> 'b -> 'c) (b : 'b) (a : 'a) : 'c =
    f a b

let inline uncurry f (a, b) =
    f a b
