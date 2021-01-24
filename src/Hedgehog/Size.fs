namespace Hedgehog

/// Tests are parameterized by the `Size` of the randomly-generated data,
/// the meaning of which depends on the particular generator used.
[<Struct>]
type Size = private Size of int

module Size =

    let [<Literal>] private MIN_VALUE = 0
    let [<Literal>] private MAX_VALUE = 99

    let maxValue : Size =
        Size MAX_VALUE

    let minValue : Size =
        Size MIN_VALUE

    let modify (f : int -> int) (Size n) : Size =
        Size (f n)

    /// Converts a simple `int` to a `Size`.
    let ofInt32 (n : int) : Size =
        n
        |> max MIN_VALUE
        |> min MAX_VALUE
        |> Size

    /// Converts a `Size` to a simple `int`.
    let toInt32 (Size n) : int =
        n

    let toNormalized (size : Size) : float =
        let n = toInt32 size
        float n / float MAX_VALUE

    let next (size : Size) : Size =
        let modifier (n : int) : int =
            (n + 1) % 100

        modify modifier size

    module BigInt =

        let scale (n : bigint) (size : Size) : bigint =
            let sz = toInt32 size
            (n * bigint sz) / (bigint MAX_VALUE)
