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

    let private renderTests : int<tests> -> string = function
        | 1<tests> ->
            "1 test"
        | n ->
            sprintf "%d tests" n

    let private renderDiscards : int<discards> -> string = function
        | 1<discards> ->
            "1 discard"
        | n ->
            sprintf "%d discards" n

    let private renderAndDiscards : int<discards> -> string = function
        | 0<discards> ->
            ""
        | 1<discards> ->
            " and 1 discard"
        | n ->
            sprintf " and %d discards" n

    let private renderAndShrinks : int<shrinks> -> string = function
        | 0<shrinks> ->
            ""
        | 1<shrinks> ->
            " and 1 shrink"
        | n ->
            sprintf " and %d shrinks" n

    let private appendLine (sb : StringBuilder) (msg : string) : unit =
        sb.AppendLine msg |> ignore

    let private appendLinef (sb : StringBuilder) (fmt : Printf.StringFormat<'a, unit>) : 'a =
        Printf.ksprintf (appendLine sb) fmt

    let private renderOK (report : Report) : string =
        sprintf "+++ OK, passed %s." (renderTests report.Tests)

    let private renderGaveUp (report : Report) : string =
        sprintf "*** Gave up after %s, passed %s."
            (renderDiscards report.Discards)
            (renderTests report.Tests)

    let private renderFailed (failure : FailureData) (report : Report) : string =
        let sb = StringBuilder ()

        appendLinef sb "*** Failed! Falsifiable (after %s%s%s):"
            (renderTests report.Tests)
            (renderAndShrinks failure.Shrinks)
            (renderAndDiscards report.Discards)

        // Split journal entries into parameters and exceptions
        let journalEntries = Journal.eval failure.Journal |> List.ofSeq
        let parameters, exceptions = 
            journalEntries |> List.partition (fun entry -> 
                not (entry.Contains("Exception") || entry.Contains("   at ")))
        
        // Render parameters
        Seq.iter (appendLine sb) parameters

        // Then render recheck info
        match failure.RecheckInfo with
        | None ->
            ()
        | Some { Data = recheckData } ->
            appendLinef sb ""
            appendLinef sb "Recheck seed: \"%s\"" (RecheckData.serialize recheckData)

        // Finally render exceptions
        if not (List.isEmpty exceptions) then
            appendLinef sb ""
            appendLinef sb "Actual error:"
            appendLinef sb ""
            Seq.iter (appendLine sb) exceptions

        sb.ToString().Trim() // Exclude extra newline.

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
