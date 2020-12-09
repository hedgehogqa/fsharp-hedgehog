namespace Hedgehog

[<Struct>]
type Journal =
    | Journal of seq<unit -> string>

module Journal =

    /// Creates a journal from a sequence of entries.
    let ofSeq (entries : seq<unit -> string>) : Journal =
        Journal entries

    /// Evaluates a single entry, returning it's message.
    let private evalEntry (f : unit -> string) : string =
        f()

    /// Evaluates all entries in the journal, returning their messages.
    let eval (Journal entries : Journal) : seq<string> =
        Seq.map evalEntry entries

    /// Represents a journal with no entries.
    let empty : Journal =
        ofSeq []

    /// Creates a single entry journal from a given message.
    let singletonMessage (message : string) : Journal =
        ofSeq [ fun () -> message ]

    /// Creates a single entry journal from a given entry.
    let singleton (entry : unit -> string) : Journal =
        ofSeq [ entry ]

    /// Creates a journal composed of entries from two journals.
    let append (Journal xs) (Journal ys) : Journal =
        ofSeq <| Seq.append xs ys
