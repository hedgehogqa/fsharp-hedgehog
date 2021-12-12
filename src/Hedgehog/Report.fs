namespace Hedgehog

[<Measure>] type tests
[<Measure>] type discards
[<Measure>] type shrinks

[<RequireQualifiedAccess>]
type ShrinkOutcome =
    | Pass
    | Fail
    
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


module internal RecheckData =
    open System

    let private separator = "_"

    let serialize data =
        [ string data.Size
          string data.Seed.Value
          string data.Seed.Gamma
          data.ShrinkPath
          |> List.map (function ShrinkOutcome.Fail -> "0" | ShrinkOutcome.Pass -> "1" )
          |> String.concat "" ]
        |> String.concat separator

    let deserialize (s: string) =
        try
            let parts = s.Split([|separator|], StringSplitOptions.None)
            let size = parts.[0] |> Int32.Parse
            let seed =
                { Value = parts.[1] |> UInt64.Parse
                  Gamma = parts.[2] |> UInt64.Parse }
            let path =
                parts.[3]
                |> Seq.map (function '0' -> ShrinkOutcome.Fail
                                   | '1' -> ShrinkOutcome.Pass
                                   |  c  -> failwithf "Unexpected character %c in shrink path" c)
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

        Seq.iter (appendLine sb) (Journal.eval failure.Journal)

        match failure.RecheckInfo with
        | None ->
            ()

        | Some { Language = Language.FSharp; Data = recheckData } ->
            appendLinef sb "This failure can be reproduced by running:"
            appendLinef sb "> Property.recheck \"%s\" <property>"
                (RecheckData.serialize recheckData)
                
        | Some { Language = Language.CSharp; Data = recheckData } ->
            appendLinef sb "This failure can be reproduced by running:"
            appendLinef sb "> property.Recheck(\"%s\")"
                (RecheckData.serialize recheckData)

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
