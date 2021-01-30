// Workaround for a fable issue: https://github.com/fable-compiler/Fable/issues/2069
#if FABLE_COMPILER
module Hedgehog.GenTuple
#else
module private Hedgehog.GenTuple
#endif

let mapFst (f : 'a -> 'c) (gen : Gen<'a * 'b>) : Gen<'c * 'b> =
    Gen.map (Pair.mapFst f) gen

let mapSnd (f : 'b -> 'c) (gen : Gen<'a * 'b>) : Gen<'a * 'c> =
    Gen.map (Pair.mapSnd f) gen
