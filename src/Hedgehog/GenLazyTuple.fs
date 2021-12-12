// Workaround for a Fable issue: https://github.com/fable-compiler/Fable/issues/2069
#if FABLE_COMPILER
module Hedgehog.GenLazyTuple
#else
[<RequireQualifiedAccess>]
module internal Hedgehog.GenLazyTuple
#endif

let mapFst f = f |> Tuple.mapFst |> GenLazy.map
let mapSnd f = f |> Tuple.mapSnd |> GenLazy.map
