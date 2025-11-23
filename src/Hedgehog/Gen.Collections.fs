namespace Hedgehog.FSharp

open Hedgehog

[<AutoOpen>]
module GenCollections =

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    [<RequireQualifiedAccess>]
    module Gen =

        let notIn (list: 'a list) (g : Gen<'a>) : Gen<'a> =
            g |> Gen.filter (fun x -> not <| List.contains x list)

        /// Generates a list that does not contain the specified element.
        /// Shortcut for Gen.filter (not << List.contains x).
        let notContains (x: 'a) : Gen<'a list> -> Gen<'a list> =
           Gen.filter (not << List.contains x)

        /// Inserts the given element at a random place in the list.
        let addElement (x : 'a) (g : Gen<'a list>) : Gen<'a list> =
            gen {
                let! xs = g
                let! i = Gen.integral (Range.constant 0 xs.Length)
                let l1, l2 = xs |> List.splitAt i
                return List.concat [l1; [x]; l2]
            }
