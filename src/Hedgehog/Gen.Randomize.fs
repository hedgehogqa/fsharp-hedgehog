namespace Hedgehog.FSharp

open System
open Hedgehog

[<AutoOpen>]
module GenRandomize =

    [<RequireQualifiedAccess>]
    module Gen =

        /// Generates a permutation of the given list.
        // "Inside-out" algorithm of Fisher-Yates shuffle from https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle#The_%22inside-out%22_algorithm
        let shuffle (xs: 'a list) =
            xs
            |> List.mapi (fun i _ -> Gen.integral (Range.constant 0 i))
            |> Gen.sequenceList
            |> Gen.map (fun list ->
                let shuffled = Array.zeroCreate<'a>(xs.Length)
                list
                |> List.zip xs
                |> List.iteri (fun i (a, j) ->
                    shuffled[i] <- shuffled[j]
                    shuffled[j] <- a)
                shuffled |> Array.toList)

        /// Shuffles the case of the given string.
        let shuffleCase (s: string) =
            Gen.bool
            |> List.replicate s.Length
            |> Gen.sequenceList
            |> Gen.map (fun bs ->
                let sb = Text.StringBuilder ()
                bs |> List.iteri (fun i b ->
                    let f = if b then Char.ToUpperInvariant else Char.ToLowerInvariant
                    sb.Append (f s[i]) |> ignore)
                sb.ToString())

        /// Generates the subset of the provided items.
        /// The generated subset will be in the same order as the input items.
        let subsetOf (items: #seq<'T>) : Gen<'T seq> =
            let xs = items |> List.ofSeq
            gen {
                let! bs = Gen.bool |> Gen.seq (Range.singleton xs.Length)
                return
                    Seq.zip bs xs
                    |> Seq.filter fst
                    |> Seq.map snd
            }
