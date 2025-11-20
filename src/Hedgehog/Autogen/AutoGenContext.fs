namespace Hedgehog

type internal IAutoGenerator =
    abstract member Generate<'a> : unit -> Gen<'a>

[<Sealed>]
type AutoGenContext internal (
    canRecurse: bool,
    currentRecursionDepth: int,
    collectionRange: Range<int>,
    auto: IAutoGenerator) =
    member _.CanRecurse = canRecurse
    member _.CurrentRecursionDepth = currentRecursionDepth
    member _.CollectionRange = collectionRange
    member _.AutoGenerate<'a>() : Gen<'a> = auto.Generate<'a>()
