// Workaround for a Fable issue: https://github.com/fable-compiler/Fable/issues/2069
#if FABLE_COMPILER
module Hedgehog.GenTuple
#else
module internal Hedgehog.GenTuple
#endif

let mapFst (f : 'a -> 'c) (gen : Gen<'a * 'b>) : Gen<'c * 'b> =
    Gen.map (Tuple.mapFst f) gen

let mapSnd (f : 'b -> 'c) (gen : Gen<'a * 'b>) : Gen<'a * 'c> =
    Gen.map (Tuple.mapSnd f) gen
