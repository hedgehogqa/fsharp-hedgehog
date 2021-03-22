namespace Hedgehog.Linq

#if !FABLE_COMPILER

open System.Runtime.CompilerServices
open Hedgehog

[<Extension>]
[<AbstractClass; Sealed>]
type ReportExtensions private () =

    [<Extension>]
    static member Render (report: Report) : string =
        Report.render report

#endif
