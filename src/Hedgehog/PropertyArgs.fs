namespace Hedgehog

[<Struct>]
type PropertyArgs = internal {
    RecheckType : RecheckType
    RecheckData : RecheckData
}

module PropertyArgs =

    let init = {
        RecheckType = RecheckType.FSharp
        RecheckData = {
            Size = 0
            Seed = Seed.random ()
        }
    }
