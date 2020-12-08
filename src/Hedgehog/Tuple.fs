module private Hedgehog.Tuple

let mapFirst (f : 'a -> 'c) (x : 'a, y : 'b) : 'c * 'b =
    f x, y

let mapSecond (f : 'b -> 'c) (x : 'a, y : 'b) : 'a * 'c =
    x, f y
