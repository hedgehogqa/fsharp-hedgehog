namespace Hedgehog.FSharp

open System

[<AutoOpen>]
module GenUri =

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    [<RequireQualifiedAccess>]
    module Gen =

        let private uriSchemeNonFirst = Gen.item (['a' .. 'z'] @ ['+'; '.'; '-'])
        let private uriScheme = gen {
            let! first = Gen.lower |> Gen.map string
            // It seems that length must be at least 2, becuase otherwise we might get
            // an implicit file:// scheme with the generated scheme as part of the path.
            let! rest = Gen.string (Range.linear 1 9) uriSchemeNonFirst
            return first + rest + ":"
        }

        let private uriUserInfo = gen {
            let! username = Gen.string (Range.linear 1 10) Gen.alphaNum
            let! password = Gen.frequency [
                5, Gen.constant None
                1, Gen.string (Range.linear 0 10) Gen.alphaNum |> Gen.map Some
            ]
            match password with
            | None -> return username + "@"
            | Some pwd -> return username + ":" + pwd + "@"
        }

        let private uriPort =
            Gen.int32 (Range.constant 1 65535)
            |> Gen.map (fun i -> ":" + string i)

        /// Generates a random valid domain name.
        let domainName =
            gen {
                let! tld = Gen.item ["io"; "com"; "gov.au"; "school"; "education"; "m1xed-c0mplex"]
                let! subdomain =
                    Gen.choice [
                        // Simple alphanumeric
                        Gen.alphaNum |> Gen.string (Range.linear 1 63)

                        // With hyphens (not at start/end)
                        gen {
                            let! start = Gen.alphaNum
                            let! middle = Gen.choice [Gen.alphaNum; Gen.constant '-'] |> Gen.string (Range.linear 0 61)
                            let! end' = Gen.alphaNum
                            return $"%c{start}%s{middle}%c{end'}"
                        }

                        // Multi-level subdomain
                        gen {
                            let! first = Gen.alphaNum |> Gen.string (Range.linear 1 20)
                            let! second = Gen.alphaNum |> Gen.string (Range.linear 1 20)
                            return $"%s{first}.%s{second}"
                        }
                    ]
                return $"%s{subdomain}.%s{tld}"
            }

        let private uriAuthority = gen {
            let! userinfo = Gen.frequency [
                3, Gen.constant None
                1, uriUserInfo |> Gen.map Some
            ]
            let! host = domainName
            let! port = Gen.frequency [
                3, Gen.constant None
                1, uriPort |> Gen.map Some
            ]
            return "//" + (userinfo |> Option.defaultValue "") + host + (port |> Option.defaultValue "")
        }

        let private uriPath =
            Gen.alphaNum
            |> Gen.string (Range.exponential 0 10)
            |> Gen.list (Range.linear 0 5)
            |> Gen.map (String.concat "/")

        let private uriQuery =
            Gen.alphaNum
            |> Gen.string (Range.exponential 1 10)
            |> Gen.tuple
            |> Gen.list (Range.linear 1 5)
            |> Gen.map (List.map (fun (k, v) -> k + "=" + v) >> String.concat "&")
            |> Gen.map (fun s -> "?" + s)

        let private uriFragment =
            Gen.alphaNum
            |> Gen.string (Range.exponential 1 10)
            |> Gen.map (fun s -> "#" + s)

        /// Generates a random URI.
        let uri = gen {
            let! scheme = uriScheme
            let! authority = uriAuthority
            let! path = uriPath
            let! query = uriQuery |> Gen.option
            let! fragment = uriFragment |> Gen.option
            let path = if path = "" then path else "/" + path
            return Uri(scheme + authority + path + (query |> Option.defaultValue "") + (fragment |> Option.defaultValue ""))
        }
