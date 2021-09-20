open System
open System.IO
open Hedgehog

[<EntryPoint>]
let main _ =
    use stdout = Console.OpenStandardOutput()
    use writer = new BinaryWriter(stdout)

    let rec loop seed =
        let (n, seed) = Seed.nextUInt64 seed
        writer.Write(n)
        loop seed

    loop (Seed.random ())
    0
