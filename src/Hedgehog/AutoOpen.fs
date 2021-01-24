[<AutoOpen>]
module internal AutoOpen

[<Measure>] type tests
[<Measure>] type discards
[<Measure>] type shrinks

let inline always (a : 'a) (_ : 'b) : 'a =
    a

let inline flip (f : 'a -> 'b -> 'c) (b : 'b) (a : 'a) : 'c =
    f a b
