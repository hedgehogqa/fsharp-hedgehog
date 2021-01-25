namespace Hedgehog

/// Tests are parameterized by the `Size` of the randomly-generated data,
/// the meaning of which depends on the particular generator used.
[<Struct>]
type Size = private Size of (int * int)

module Size =

    /// Initializes a `Size`, where `current` specifies the numerator while `maximum` specifies the denominator.
    ///
    /// The value for `maximum` is inclusive.
    let init (current : int) (maximum : int<tests>) : Size =
        Size (current, int maximum)

    let current (Size (current, _)) =
        int current

    let maximum (Size (_, maximum)) =
        int maximum

    let private modify (f : int -> int) (size : Size) : Size =
        let current = current size
        let maximum = maximum size
        Size (f current, maximum)

    let rewind (n : int) (size : Size) =
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
