module private Hedgehog.Pair

let mapFst (f : 'a -> 'c) (x : 'a, y : 'b) : ('c * 'b) =
    (f x, y)

let mapSnd (f : 'b -> 'c) (x : 'a, y : 'b) : ('a * 'c) =
    (x, f y)
