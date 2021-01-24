module private Hedgehog.GenTuple

let mapFst (f : 'a -> 'c) (gen : Gen<'a * 'b>) : Gen<'c * 'b> =
    Gen.map (Tuple.mapFst f) gen

let mapSnd (f : 'b -> 'c) (gen : Gen<'a * 'b>) : Gen<'a * 'c> =
    Gen.map (Tuple.mapSnd f) gen
