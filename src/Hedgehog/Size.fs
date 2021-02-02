namespace Hedgehog

/// Tests are parameterized by the `Size` of the randomly-generated data,
/// the meaning of which depends on the particular generator used.
[<Struct>]
type Size = private Size of (int * int)

module Size =

    let create (current : int) (maximum : int) : Size =
        Size (current, int maximum)

    let constant (n : int) : Size =
        create n n

    let current (Size (current, _)) : int =
        current

    let maximum (Size (_, maximum)) : int =
        maximum

    let private mapCurrent (f : int -> int) (Size(pair)) : Size =
        uncurry create (Pair.mapFst f pair)

    let rewind (n : int) (size : Size) : Size =
        size |> mapCurrent (fun k -> k * 2 + n)

    let half (size : Size) : Size =
        size |> mapCurrent (fun n -> n / 2)

    let next (size : Size) : Size =
        size |> mapCurrent (fun n -> n + 1)

    let prev (size : Size) : Size =
        size |> mapCurrent (fun n -> n - 1)

    module Double =

        let normalized (size : Size) : float =
            let current = float (current size)
            let maximum = float (maximum size)
            current / maximum

    module BigInt =

        let lerp (min : bigint) (max : bigint) (size : Size) : bigint =
            let current = bigint (current size)
            let maximum = bigint (maximum size)
            min + (((max - min) * current) / maximum)
