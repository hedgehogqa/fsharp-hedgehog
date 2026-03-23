namespace Hedgehog

[<Struct>]
type PropertyArgs = internal {
    Language : Language
    RecheckData : RecheckData
}

module PropertyArgs =

    let init (seed : Seed) = {
        Language = Language.FSharp
        RecheckData = {
            Size = 0
            Seed = seed
            ShrinkPath = []
        }
    }
