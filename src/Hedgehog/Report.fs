namespace Hedgehog

[<Measure>] type tests
[<Measure>] type discards
[<Measure>] type shrinks

[<RequireQualifiedAccess>]
type ShrinkOutcome =
    | Pass of int
    
[<Struct>]
type RecheckData = internal {
    Size : Size
    Seed : Seed
    ShrinkPath : ShrinkOutcome list
}

[<RequireQualifiedAccess>]
type Language =
    | CSharp
    | FSharp

[<RequireQualifiedAccess>]
type RecheckInfo = {
    Language : Language
    Data : RecheckData
}

type FailureData = {
    Shrinks : int<shrinks>
    Journal : Journal
    RecheckInfo : RecheckInfo option
}

type Status =
    | Failed of FailureData
    | GaveUp
    | OK

type Report = {
    Tests : int<tests>
    Discards : int<discards>
    Status : Status
}


module RecheckData =
    open System

    let private separator = "_"
    let private pathSeparator = "-"

    let serialize data =
        [ string data.Size
          string data.Seed.Value
          string data.Seed.Gamma
          data.ShrinkPath
          |> List.map (function ShrinkOutcome.Pass i -> i.ToString() )
          |> String.concat pathSeparator ]
        |> String.concat separator

    let deserialize (s: string) =
        try
            let parts = s.Split([|separator|], StringSplitOptions.None)
            let size = parts[0] |> Int32.Parse
            let seed =
                { Value = parts[1] |> UInt64.Parse
                  Gamma = parts[2] |> UInt64.Parse }
            let path =
                if parts[3] = ""
                then []
                else parts[3].Split([|pathSeparator|], StringSplitOptions.None)
                    |> Seq.map (Int32.Parse >> ShrinkOutcome.Pass)
                    |> Seq.toList
            { Size = size
              Seed = seed
              ShrinkPath = path }
        with e ->
            raise (ArgumentException("Failed to deserialize RecheckData", e))


module Report =

    open System
    open System.Text

    let private printValue = Hedgehog.FSharp.ValueFormatting.printValue
    let private indent = "  " // 2 spaces for indentation

    // ========================================
    // StringBuilder Extensions
    // ========================================

    type private StringBuilder with
        /// Appends each string in the sequence with indentation
        member this.AppendIndentedLine(indent: string, lines: #seq<string>) =
            lines |> Seq.iter (fun line -> this.Append(indent).AppendLine(line) |> ignore)
            this

        /// Splits text into lines and appends each with indentation
        member this.AppendIndentedLine(indent: string, text: string) =
            let lines = text.Split([|'\n'; '\r'|], StringSplitOptions.None)
            this.AppendIndentedLine(indent, lines)

    // ========================================
    // Consecutive Grouping
    // ========================================

    /// Groups consecutive elements by a classifier function, preserving order.
    /// Returns a list of (key * items list) tuples where items with the same consecutive key are grouped together.
    let private groupConsecutiveBy (classifier: 'T -> 'Key) (source: 'T seq) : ('Key * 'T list) list =
        let folder (groups, currentKey, currentGroup) item =
            let key = classifier item
            match currentKey with
            | None -> (groups, Some key, [item])
            | Some prevKey when key = prevKey -> (groups, currentKey, item :: currentGroup)
            | Some prevKey -> ((prevKey, List.rev currentGroup) :: groups, Some key, [item])
        
        let groups, finalKey, finalGroup =
            source |> Seq.fold folder ([], None, [])
        
        match finalKey with
        | None -> []
        | Some key -> (key, List.rev finalGroup) :: groups
        |> List.rev

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
            sb.AppendLine().AppendLine("Actual exception:").AppendLine(exceptionString) |> ignore)

    let private renderTests : int<tests> -> string = function
        | 1<tests> ->
            "1 test"
        | n ->
            $"%d{n} tests"

    let private renderDiscards : int<discards> -> string = function
        | 1<discards> ->
            "1 discard"
        | n ->
            $"%d{n} discards"

    let private renderAndDiscards : int<discards> -> string = function
        | 0<discards> ->
            ""
        | 1<discards> ->
            " and 1 discard"
        | n ->
            $" and %d{n} discards"

    let private renderAndShrinks : int<shrinks> -> string = function
        | 0<shrinks> ->
            ""
        | 1<shrinks> ->
            " and 1 shrink"
        | n ->
            $" and %d{n} shrinks"

    let private renderOK (report : Report) : string =
        sprintf "+++ OK, passed %s." (renderTests report.Tests)

    let private renderGaveUp (report : Report) : string =
        sprintf "*** Gave up after %s, passed %s."
            (renderDiscards report.Discards)
            (renderTests report.Tests)

    let private renderFailed (failure : FailureData) (report : Report) : string =
        let sb = StringBuilder ()

        sb.AppendIndentedLine(
            indent,
            sprintf
                "*** Failed! Falsifiable (after %s%s%s):"
                (renderTests report.Tests)
                (renderAndShrinks failure.Shrinks)
                (renderAndDiscards report.Discards)
        )
        |> ignore

        // Recheck seed (if available) - render after journal entries
        match failure.RecheckInfo with
        | Some { Data = recheckData } ->
            let serialized = RecheckData.serialize recheckData
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
            |> groupConsecutiveBy groupKey
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

    let render (report : Report) : string =
        match report.Status with
        | OK ->
            renderOK report
        | GaveUp ->
            renderGaveUp report
        | Failed failure ->
            renderFailed failure report

    let tryRaise (report : Report) : unit =
        match report.Status with
        | OK ->
            ()
        | GaveUp
        | Failed _ ->
            raise (Exception (render report))
