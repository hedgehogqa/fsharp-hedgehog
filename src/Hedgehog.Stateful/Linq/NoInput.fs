namespace Hedgehog.Stateful.Linq

/// Represents the absence of input for commands that don't require input parameters.
/// Similar to F#'s unit type, this is a zero-sized struct with a single instance.
[<Struct>]
[<StructuredFormatDisplay("{DisplayText}")>]
type NoInput =
    static member val Value = Unchecked.defaultof<NoInput>
    member private this.DisplayText = ""
