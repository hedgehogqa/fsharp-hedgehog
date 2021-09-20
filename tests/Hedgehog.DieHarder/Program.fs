open Hedgehog

[<EntryPoint>]
let main _ =
    let rec loop seed =
        let (n, seed) = Seed.nextUInt64 seed
        printfn "%d" n
        loop seed
    loop (Seed.random ())
    0
