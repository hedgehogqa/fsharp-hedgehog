namespace Hedgehog.Xunit

open System
open Hedgehog

// Represents the interface for property attributes used in property-based testing.
// This interface is shared between Property and Properties attributes,
//
// It is also needed to work around the limitations of F# "no cyclic dependencies" rule
// where Property attribute needs to reference Discoverer type (and therefore be defined after it),
// But the Discoverer is the entrypoint for the whole testing pipeline.
[<Interface>]
type internal IPropertyAttribute =
  abstract member AutoGenConfig: Type option with get, set
  abstract member AutoGenConfigArgs: obj array with get, set
  abstract member Tests: int<tests> option with get, set
  abstract member Shrinks: int<shrinks> option with get, set
  abstract member Size: Size option with get, set
