/// Report formatting for xUnit integration
[<RequireQualifiedAccess>]
module internal ReportFormatter

open Hedgehog
open Hedgehog.Xunit
open System.Text

// ========================================
// Constants and Formatting
// ========================================

let private indent = "  " // 2 spaces to align with xUnit's output format
let private printValue = Hedgehog.FSharp.ValueFormatting.printValue

// ========================================
// Report Formatting
// ========================================

/// Filters exception string to show only user code stack trace.
/// When we rethrow using ExceptionDispatchInfo.Capture().Throw() to preserve the original stack trace,
/// it adds a "--- End of stack trace from previous location ---" marker and appends Hedgehog's
/// internal frames as the exception propagates. We remove everything from that marker onwards
/// to show only the user's code in the test failure report.
let private filterExceptionStackTrace (exceptionEntry: string) : string =
    match exceptionEntry.IndexOf("--- End of stack trace from previous location ---") with
    | -1 -> exceptionEntry  // No marker found, return as-is
    | idx -> exceptionEntry.Substring(0, idx).TrimEnd()

// ========================================
// Journal Entry Groups
// ========================================

type private JournalEntryGroup =
    | ParametersGroup of (string * obj) list
    | GeneratedGroup of obj list
    | CounterexamplesGroup of string list
    | TextsGroup of string list
    | CancellationsGroup of string list
    | ExceptionsGroup of exn list

let private classifyJournalLine (line: JournalLine) : JournalEntryGroup =
    match line with
    | TestParameter (name, value) -> ParametersGroup [(name, value)]
    | GeneratedValue value -> GeneratedGroup [value]
    | Counterexample msg -> CounterexamplesGroup [msg]
    | Text msg -> TextsGroup [msg]
    | Cancellation msg -> CancellationsGroup [msg]
    | Exception exn -> ExceptionsGroup [exn]

let private groupKey (group: JournalEntryGroup) : int =
    match group with
    | ParametersGroup _ -> 0
    | GeneratedGroup _ -> 1
    | CounterexamplesGroup _ -> 2
    | TextsGroup _ -> 3
    | CancellationsGroup _ -> 4
    | ExceptionsGroup _ -> 5

let private mergeGroups (groups: JournalEntryGroup list) : JournalEntryGroup =
    match groups with
    | [] -> failwith "Cannot merge empty group list"
    | ParametersGroup _ :: _ ->
        groups |> List.collect (function ParametersGroup items -> items | _ -> []) |> ParametersGroup
    | GeneratedGroup _ :: _ ->
        groups |> List.collect (function GeneratedGroup items -> items | _ -> []) |> GeneratedGroup
    | CounterexamplesGroup _ :: _ ->
        groups |> List.collect (function CounterexamplesGroup items -> items | _ -> []) |> CounterexamplesGroup
    | TextsGroup _ :: _ ->
        groups |> List.collect (function TextsGroup items -> items | _ -> []) |> TextsGroup
    | CancellationsGroup _ :: _ ->
        groups |> List.collect (function CancellationsGroup items -> items | _ -> []) |> CancellationsGroup
    | ExceptionsGroup _ :: _ ->
        groups |> List.collect (function ExceptionsGroup items -> items | _ -> []) |> ExceptionsGroup

// ========================================
// Group Rendering Functions
// ========================================

let private renderParameters (sb: StringBuilder) (parameters: (string * obj) list) : unit =
    sb.AppendLine().AppendLine("Test parameters:") |> ignore
    parameters |> List.iter (fun (name, value) ->
        sb.AppendIndentedLine(indent, $"%s{name} = %s{printValue value}") |> ignore)

let private renderGenerated (sb: StringBuilder) (values: obj list) : unit =
    sb.AppendLine().AppendLine("Generated values:") |> ignore
    values |> List.iter (fun value ->
        sb.AppendIndentedLine(indent, printValue value) |> ignore)

let private renderCounterexamples (sb: StringBuilder) (messages: string list) : unit =
    sb.AppendLine().AppendLine("Counterexamples:") |> ignore
    messages |> List.iter (fun msg -> sb.AppendIndentedLine(indent, msg) |> ignore)

let private renderTexts (sb: StringBuilder) (messages: string list) : unit =
    sb.AppendLine() |> ignore
    messages |> List.iter (fun msg -> sb.AppendLine(msg) |> ignore)

let private renderCancellations (sb: StringBuilder) (messages: string list) : unit =
    sb.AppendLine() |> ignore
    messages |> List.iter (fun msg -> sb.AppendLine(msg) |> ignore)

let private renderExceptions (sb: StringBuilder) (exceptions: exn list) : unit =
    exceptions |> List.iter (fun exn ->
        let exceptionString = string (Exceptions.unwrap exn)
        let filteredEntry = filterExceptionStackTrace exceptionString
        sb.AppendLine().AppendLine("Actual exception:").AppendLine(filteredEntry) |> ignore)

let private formatFailureForXunit (failure: FailureData) (report: Report) : string =
    let sb = StringBuilder()

    let renderTests (tests: int<tests>) =
        sprintf "%d test%s" (int tests) (if int tests = 1 then "" else "s")

    let renderAndShrinks (shrinks: int<shrinks>) =
        if int shrinks = 0 then
            ""
        else
            sprintf " and %d shrink%s" (int shrinks) (if int shrinks = 1 then "" else "s")

    let renderAndDiscards (discards: int<discards>) =
        if int discards = 0 then
            ""
        else
            sprintf " and %d discard%s" (int discards) (if int discards = 1 then "" else "s")

    // Header
    sb.AppendIndentedLine(
        indent,
        sprintf
            "*** Failed! Falsifiable (after %s%s%s):"
            (renderTests report.Tests)
            (renderAndShrinks failure.Shrinks)
            (renderAndDiscards report.Discards)
    )
    |> ignore

    // Recheck seed (if available)
    match failure.RecheckInfo with
    | Some recheckInfo ->
        let serialized = RecheckData.serialize recheckInfo.Data
        sb.AppendLine()
          .AppendLine("You can reproduce this failure with the following Recheck Seed:")
          .AppendIndentedLine(indent, $"\"%s{serialized}\"") |> ignore
    | None -> ()

    // Evaluate journal entries and group consecutively by type
    let journalLines = Journal.eval failure.Journal
    
    // Classify each journal line and group consecutive entries of the same type
    let groups =
        journalLines
        |> Seq.map classifyJournalLine
        |> Seq.groupConsecutiveBy groupKey
        |> List.map (fun (_, groupList) -> mergeGroups groupList)
    
    // Render each group in order
    groups |> List.iter (fun group ->
        match group with
        | ParametersGroup parameters -> renderParameters sb parameters
        | GeneratedGroup values -> renderGenerated sb values
        | CounterexamplesGroup messages -> renderCounterexamples sb messages
        | TextsGroup messages -> renderTexts sb messages
        | CancellationsGroup messages -> renderCancellations sb messages
        | ExceptionsGroup exceptions -> renderExceptions sb exceptions)

    sb.ToString()

let private formatReportForXunit (report: Report) : string =
    match report.Status with
    | Failed failure -> formatFailureForXunit failure report
    | _ -> Report.render report

let tryRaise (report: Report) : unit =
    match report.Status with
    | Failed _ -> report |> formatReportForXunit |> PropertyFailedException |> raise
    | _ -> Report.tryRaise report
