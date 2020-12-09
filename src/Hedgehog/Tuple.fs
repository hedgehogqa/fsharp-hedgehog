namespace Hedgehog

module private Tuple =

    let mapFirst (f : 'a -> 'c) (x : 'a, y : 'b) : 'c * 'b =
        f x, y

    let mapSecond (f : 'b -> 'c) (x : 'a, y : 'b) : 'a * 'c =
        x, f y


module private GenTuple =

    let mapFirst (f : 'a -> 'c) (gen : Gen<'a * 'b>) : Gen<'c * 'b> =
        Gen.map (Tuple.mapFirst f) gen


    let mapSecond (f : 'b -> 'c) (gen : Gen<'a * 'b>) : Gen<'a * 'c> =
        Gen.map (Tuple.mapSecond f) gen
