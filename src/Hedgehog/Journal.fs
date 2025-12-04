namespace Hedgehog

/// Represents a single line in a property test journal with semantic meaning
type JournalLine =
    | TestParameter of name: string * value: obj    // Individual xUnit test method parameter
    | GeneratedValue of value: obj                   // forAll generated values (no name)
    | Counterexample of message: string              // Property.counterexample user messages
    | Exception of exn: exn                          // Original exception, unwrap at render
    | Cancellation of message: string                // OperationCanceledException messages
    | Text of message: string                        // Future-proof escape hatch

[<Struct>]
type Journal =
    | Journal of seq<unit -> JournalLine>

module Journal =

    /// Creates a journal from a sequence of entries.
    let ofSeq (entries : seq<unit -> JournalLine>) : Journal =
        Journal entries

    /// Evaluates a single entry, returning the journal line.
    let private evalEntry (f : unit -> JournalLine) : JournalLine =
        f()

    /// Evaluates all entries in the journal, returning their journal lines.
    let eval (Journal entries : Journal) : seq<JournalLine> =
        Seq.map evalEntry entries

    /// Represents a journal with no entries.
    let empty : Journal =
        ofSeq []

    /// Creates a single entry journal from a given message as Text.
    let singletonMessage (message : string) : Journal =
        ofSeq [ fun () -> Text message ]

    /// Adds exception to the journal as a single entry.
    let exn (error: exn): Journal =
        ofSeq [ fun () -> Exception error ]

    /// Creates a single entry journal from a given entry.
    let singleton (entry : unit -> JournalLine) : Journal =
        ofSeq [ entry ]

    /// Creates a journal composed of entries from two journals.
    let append (Journal xs) (Journal ys) : Journal =
        Seq.append xs ys
        |> ofSeq
