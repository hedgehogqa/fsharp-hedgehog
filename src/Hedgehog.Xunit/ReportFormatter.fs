/// Report formatting for xUnit integration
module internal ReportFormatter

open Hedgehog
open Hedgehog.Xunit
open System
open System.Text

// ========================================
// Report Formatting
// ========================================

/// Filters exception string to show only user code stack trace.
/// When we rethrow using ExceptionDispatchInfo.Capture().Throw() to preserve the original stack trace,
/// it adds a "--- End of stack trace from previous location ---" marker and appends Hedgehog's
/// internal frames as the exception propagates. We remove everything from that marker onwards
/// to show only the user's code in the test failure report.
let private filterExceptionStackTrace (exceptionEntry: string) : string =
    match exceptionEntry.IndexOf("--- End of inner exception stack trace ---") with
    | -1 -> exceptionEntry  // No marker found, return as-is
    | idx -> exceptionEntry.Substring(0, idx).TrimEnd()

let private formatFailureForXunit (failure: FailureData) (report: Report) : string =
    let sb = StringBuilder()
    let indent = "  " // 2 spaces to align with xUnit's output format

    let renderTests (tests: int<tests>) =
        sprintf "%d test%s" (int tests) (if int tests = 1 then "" else "s")

    let renderAndShrinks (shrinks: int<shrinks>) =
        if int shrinks = 0 then ""
        else sprintf " and %d shrink%s" (int shrinks) (if int shrinks = 1 then "" else "s")

    let renderAndDiscards (discards: int<discards>) =
        if int discards = 0 then ""
        else sprintf " and %d discard%s" (int discards) (if int discards = 1 then "" else "s")

    // Header
    sb.AppendIndentedLine(
        indent,
        sprintf "*** Failed! Falsifiable (after %s%s%s):"
            (renderTests report.Tests)
            (renderAndShrinks failure.Shrinks)
            (renderAndDiscards report.Discards)
    ) |> ignore

    // Journal structure: first=parameters, middle=entries (optional), last=exception (always present on failure)
    let journalEntries = Journal.eval failure.Journal |> Array.ofSeq
    let parametersEntry, entries, exceptionEntryOpt = Array.splitFirstMiddleLast journalEntries

    // Parameters section
    sb.AppendLine() |> ignore
    if String.IsNullOrWhiteSpace(parametersEntry) then
        sb.AppendLine("Test doesn't take parameters") |> ignore
    else
        sb.AppendLine("Input parameters:")
          .AppendIndentedLine(indent, parametersEntry) |> ignore

    // Middle entries section (user's debug info from Property.counterexample, etc.)
    if entries.Length > 0 then
        sb.AppendLine()
          .AppendLines(entries) |> ignore

    // Recheck seed (if available)
    match failure.RecheckInfo with
    | Some recheckInfo ->
        let serialized = RecheckData.serialize recheckInfo.Data
        sb.AppendLine()
          .AppendLine($"Recheck seed: \"%s{serialized}\"") |> ignore
    | None -> ()

    // Exception section (filtered to show only user code)
    match exceptionEntryOpt with
    | Some exceptionEntry ->
        let filteredEntry = filterExceptionStackTrace exceptionEntry
        sb.AppendLine()
          .AppendLine("Actual exception:")
          .AppendLine(filteredEntry) |> ignore
    | None -> ()

    sb.ToStringTrimmed()

let private formatReportForXunit (report: Report) : string =
    match report.Status with
    | Failed failure -> formatFailureForXunit failure report
    | _ -> Report.render report

let tryRaise (report: Report) : unit =
    match report.Status with
    | Failed _ ->
        report |> formatReportForXunit |> PropertyFailedException |> raise
    | _ ->
        Report.tryRaise report
