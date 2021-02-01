// Workaround for a fable issue: https://github.com/fable-compiler/Fable/issues/2069
#if FABLE_COMPILER
module Hedgehog.Tuple
#else
module private Hedgehog.Tuple
#endif

let mapFst (f : 'a -> 'c) (x : 'a, y : 'b) : 'c * 'b =
    f x, y

let mapSnd (f : 'b -> 'c) (x : 'a, y : 'b) : 'a * 'c =
    x, f y
