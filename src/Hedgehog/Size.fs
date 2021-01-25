namespace Hedgehog

/// Tests are parameterized by the `Size` of the randomly-generated data,
/// the meaning of which depends on the particular generator used.
[<Struct>]
type Size = private Size of struct(int * int)

module Size =

    let create (current : int) (maximum : int) : Size =
        Size (current, int maximum)

    let constant (n : int) : Size =
        create n n

    let current (size : Size) : int =
        let (Size (current, _)) = size
        int current

    let maximum (size : Size) : int =
        let (Size (_, maximum)) = size
        int maximum

    let private modify (f : int -> int) (size : Size) : Size =
        let current = current size
        let maximum = maximum size
        create (f current) maximum

    let rewind (n : int) (size : Size) : Size =
        size |> modify (fun k -> k * 2 + n)

    let half (size : Size) : Size =
        size |> modify (fun n -> n / 2)

    let next (size : Size) : Size =
        size |> modify (fun n -> n + 1)

    let prev (size : Size) : Size =
        size |> modify (fun n -> n - 1)

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
