/// Report formatting for xUnit integration
[<RequireQualifiedAccess>]
module internal ReportFormatter

open Hedgehog
open Hedgehog.Xunit

let tryRaise (report: Report) : unit =
    match report.Status with
    | Failed _ -> report |> Report.render |> PropertyFailedException |> raise
    | _ -> Report.tryRaise report
