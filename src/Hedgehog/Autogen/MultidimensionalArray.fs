namespace Hedgehog.AutoGen

open System

module internal MultidimensionalArray =

  let createWithDefaultEntries<'a> (lengths: int list) =
    let array = lengths |> Array.ofList
    Array.CreateInstance (typeof<'a>, array)

  let createWithGivenEntries<'a> (data: 'a seq) lengths =
    let array = createWithDefaultEntries<'a> lengths
    let currentIndices = Array.create (List.length lengths) 0
    use en = data.GetEnumerator ()
    let rec loop currentDimensionIndex = function
      | [] ->
          en.MoveNext () |> ignore
          array.SetValue(en.Current, currentIndices)
      | currentLength :: remainingLengths ->
          for i in 0..currentLength - 1 do
            currentIndices[currentDimensionIndex] <- i
            loop (currentDimensionIndex + 1) remainingLengths
    loop 0 lengths
    array
