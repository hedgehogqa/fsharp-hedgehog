[<AutoOpen>]
module internal Hedgehog.Xunit.Prelude

open System

module Option =
  let requireSome msg =
    function
    | Some x -> x
    | None   -> failwith msg

module Array =
  /// Splits an array into first element, middle elements, and last element
  /// Returns (first, middle, last option) where last is None if array has only one element
  let splitFirstMiddleLast (arr: 'a[]) : 'a * 'a[] * 'a option =
    match arr with
    | [||] -> failwith "Cannot split empty array"
    | [| single |] -> (single, [||], None)
    | _ ->
        let first = arr[0]
        let middle = arr[1 .. arr.Length - 2]
        let last = arr[arr.Length - 1]
        (first, middle, Some last)

module Seq =
  let inline tryMin xs =
    if Seq.isEmpty xs then None else Some (Seq.min xs)

  // https://github.com/dotnet/fsharp/blob/b9942004e8ba19bf73862b69b2d71151a98975ba/src/FSharp.Core/seqcore.fs#L172-L174
  let inline private checkNonNull argName arg =
    if isNull arg then
      nullArg argName

  // https://github.com/dotnet/fsharp/blob/b9942004e8ba19bf73862b69b2d71151a98975ba/src/FSharp.Core/seq.fs#L1710-L1719
  let seqTryExactlyOne (source: seq<_>) =
    checkNonNull "source" source
    use e = source.GetEnumerator()

    if e.MoveNext() then
      let v = e.Current
      if e.MoveNext() then None else Some v
    else
      None

module internal Type =
  let getAllAttributes<'T> (t: Type) : 'T list =
    t
    |> Seq.unfold (fun ty -> if ty = null then None else Some(ty, ty.BaseType))
    |> Seq.choose (fun ty ->
        ty.GetCustomAttributes(false)
        |> Seq.tryFind (fun attr -> attr :? 'T)
        |> Option.map (fun attr -> attr :?> 'T))
    |> Seq.toList

[<AutoOpen>]
module StringBuilder =
  open System.Text

  type StringBuilder with
    /// Appends each string in the sequence with indentation
    member this.AppendIndentedLine(indent: string, lines: #seq<string>) =
      lines |> Seq.iter (fun line -> this.Append(indent).AppendLine(line) |> ignore)
      this

    /// Splits text into lines and appends each with indentation
    member this.AppendIndentedLine(indent: string, text: string) =
      let lines = text.Split([|'\n'; '\r'|], StringSplitOptions.None)
      this.AppendIndentedLine(indent, lines)

    member this.AppendLines(lines: #seq<string>) =
      this.AppendJoin(Environment.NewLine, lines)

    /// Returns the string content with trailing whitespace removed
    member this.ToStringTrimmed() =
      this.ToString().TrimEnd()
