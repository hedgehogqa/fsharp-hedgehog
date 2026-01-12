namespace Hedgehog.NUnit

open System
open Hedgehog

// Represents the interface for property attributes used in property-based testing.
// This interface is shared between Property and Properties attributes.
[<Interface>]
type internal IPropertyAttribute =
    abstract member AutoGenConfig: Type option with get, set
    abstract member AutoGenConfigArgs: obj array with get, set
    abstract member Tests: int<tests> option with get, set
    abstract member Shrinks: int<shrinks> option with get, set
    abstract member Size: Size option with get, set
