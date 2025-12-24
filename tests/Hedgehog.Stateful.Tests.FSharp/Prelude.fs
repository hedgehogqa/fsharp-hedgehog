[<AutoOpen>]
module Hedgehog.Stateful.Tests.Prelude

open VerifyTests
open VerifyXunit

let settings =
    let set = VerifySettings()
    set.UseDirectory("__snapshots__")
    set

type Verifier with
    static member VerifyFormatted<'T>(value: 'T) =
         Verifier.Verify($"%A{value}", settings).ToTask()
