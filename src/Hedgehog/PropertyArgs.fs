namespace Hedgehog

[<Struct>]
type PropertyArgs = internal {
    Language : Language option
    RecheckData : RecheckData
}

module PropertyArgs =

    let init = {
        Language = Some Language.FSharp
        RecheckData = {
            Size = 0
            Seed = Seed.random ()
        }
    }
