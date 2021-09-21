namespace Hedgehog

[<Struct>]
type PropertyState = private {
    Discards : int<discards>
    Shrinks : int<shrinks>
    Tests : int<tests>
    RecheckType : RecheckType
    Seed : Seed
    Size : Size
}

module PropertyState =

    let init = {
        Discards = 0<discards>
        Shrinks = 0<shrinks>
        Tests = 0<tests>
        RecheckType = RecheckType.FSharp
        Seed = Seed.random ()
        Size = 0
    }

    let countDiscard (state : PropertyState) : PropertyState =
        { state with Discards = state.Discards + 1<discards> }

    let countShrink (state : PropertyState) : PropertyState =
        { state with Shrinks = state.Shrinks + 1<shrinks> }

    let countTest (state : PropertyState) : PropertyState =
        { state with Tests = state.Tests + 1<tests> }

    let next (state : PropertyState) : (Seed * PropertyState) =
        let nextSize size =
            if size >= 100 then
                1
            else
                size + 1

        let nextSeed, seed = Seed.split state.Seed
        let nextState = {
            state with
                Seed = seed
                Size = nextSize state.Size
        }

        (nextSeed, nextState)
