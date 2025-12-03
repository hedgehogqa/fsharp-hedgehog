namespace Hedgehog

[<Struct>]
type PropertyArgs = internal {
    Language : Language
    RecheckData : RecheckData
}

module PropertyArgs =

    let init () = {
        Language = Language.FSharp
        RecheckData = {
            Size = 0
            Seed = Seed.random ()
            ShrinkPath = []
        }
    }
