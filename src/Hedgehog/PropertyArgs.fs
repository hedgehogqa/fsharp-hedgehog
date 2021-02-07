namespace Hedgehog

[<Struct>]
type PropertyArgs = private {
    RecheckType : RecheckType
    Size : Size
    Seed : Seed
}

module PropertyArgs =

    let init = {
        RecheckType = RecheckType.FSharp
        Size = 0
        Seed = Seed.random ()
    }
