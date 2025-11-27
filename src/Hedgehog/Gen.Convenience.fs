namespace Hedgehog.FSharp

open System
open System.Net
open Hedgehog

[<AutoOpen>]
module GenConvenience =

    [<RequireQualifiedAccess>]
    module Gen =

        /// Generates a random identifier string that starts with a lowercase letter,
        /// followed by alphanumeric characters or underscores, up to the specified maximum length.
        let identifier (maxLen: int) : Gen<string> =
            gen {
                let! first = Gen.lower |> Gen.map string

                let! rest =
                    Gen.frequency [ 0, Gen.alphaNum; 2, Gen.constant '_' ]
                    |> Gen.string (Range.linear 0 (maxLen - 1))

                return first + rest
            }

        /// Generates a snake_case string composed of random words of specified lengths and counts.
        let snakeCase (wordLength: Range<int>) (wordsCount: Range<int>) : Gen<string> =
            let word = Gen.frequency [8, Gen.lower; 2, Gen.digit] |> Gen.string wordLength
            word |> Gen.list wordsCount |> Gen.map (fun x -> String.Join("_", x))

        /// Generates a kebab-case string composed of random words of specified lengths and counts.
        let kebabCase (wordLength: Range<int>) (wordsCount: Range<int>) : Gen<string> =
            let word = Gen.frequency [8, Gen.lower; 2, Gen.digit] |> Gen.string wordLength
            word |> Gen.list wordsCount |> Gen.map (fun x -> String.Join("-", x))

        /// Generates a Latin-style name starting with an uppercase letter followed by lowercase letters,
        let latinName (maxLength: int) : Gen<string> =
            gen {
                let! first = Gen.upper
                let! rest = Gen.lower |> Gen.string (Range.linear 0 (maxLength - 1))
                return string first + rest
            }
