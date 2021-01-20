namespace Hedgehog

module private Tuple =

    let mapFst (f : 'a -> 'c) (x : 'a, y : 'b) : 'c * 'b =
        f x, y

    let mapSnd (f : 'b -> 'c) (x : 'a, y : 'b) : 'a * 'c =
        x, f y


module private GenTuple =

    let mapFst (f : 'a -> 'c) (gen : Gen<'a * 'b>) : Gen<'c * 'b> =
        Gen.map (Tuple.mapFst f) gen


    let mapSnd (f : 'b -> 'c) (gen : Gen<'a * 'b>) : Gen<'a * 'c> =
        Gen.map (Tuple.mapSnd f) gen
