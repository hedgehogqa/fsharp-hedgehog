namespace Hedgehog.Stateful.Linq

/// Represents the absence of value, that can be used for commands that don't require input parameters.
/// Similar to F#'s unit type, this is a zero-sized struct with a single instance.
[<Struct>]
[<StructuredFormatDisplay("{DisplayText}")>]
type NoValue =
    static member val Value = Unchecked.defaultof<NoValue>
    member private this.DisplayText = ""
