module Tests

open Hedgehog

[<EntryPoint>]
let main args =
    // For now just test if everything compiles
    property {
        let! x = Gen.int (Range.linearBounded())
        return x = x
    } |> Property.check
    0
