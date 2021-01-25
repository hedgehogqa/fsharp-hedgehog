namespace Hedgehog

type FailureData = {
    Size : Size
    Seed : Seed
    Shrinks : int<shrinks>
    Journal : Journal
    RenderRecheck : bool
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

        if failure.RenderRecheck then
            appendLinef sb "This failure can be reproduced by running:"
            appendLinef sb "> Property.recheck (Size.create %d %d) ({ Value = %A; Gamma = %A }) <property>"
                (Size.current failure.Size)
                (Size.maximum failure.Size)
                failure.Seed.Value
                failure.Seed.Gamma

        sb.ToString (0, sb.Length - 1) // Exclude extra newline.

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
