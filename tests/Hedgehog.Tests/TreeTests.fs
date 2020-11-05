module Hedgehog.Tests.TreeTests

open Hedgehog
open Swensen.Unquote
open Xunit

[<Fact>]
let ``render tree with depth 0`` () =
    Property.check <| property {
        let! x0 = Gen.constant "0"

        let tree =
            Node (x0, [
            ])
            |> Tree.map (sprintf "%A")

        let expected = [
            sprintf "%A" x0
        ]
        test <@ expected = Tree.renderList tree @>
    }

[<Fact>]
let ``render tree with depth 1`` () =
    Property.check <| property {
        let! x0 = Gen.constant "0"
        let! x1 = Gen.constant "1"
        let! x2 = Gen.constant "2"

        let tree =
            Node (x0, [
                Node (x1, [])
                Node (x2, [])
            ])
            |> Tree.map (sprintf "%A")

        let expected = [
            sprintf "%A" x0
            sprintf " ├-%A" x1
            sprintf " └-%A" x2
        ]
        test <@ expected = Tree.renderList tree @>
    }

[<Fact>]
let ``render tree with depth 2`` () =
    Property.check <| property {
        let! x0 = Gen.constant "0"
        let! x1 = Gen.constant "1"
        let! x2 = Gen.constant "2"
        let! x3 = Gen.constant "3"
        let! x4 = Gen.constant "4"
        let! x5 = Gen.constant "5"
        let! x6 = Gen.constant "6"

        let tree =
            Node (x0, [
                Node (x1, [
                    Node (x3, [])
                    Node (x5, [])
                ])
                Node (x2, [
                    Node (x4, [])
                    Node (x6, [])
                ])
            ])
            |> Tree.map (sprintf "%A")

        let expected = [
            sprintf "%A" x0
            sprintf " ├-%A" x1
            sprintf " |  ├-%A" x3
            sprintf " |  └-%A" x5
            sprintf " └-%A" x2
            sprintf "    ├-%A" x4
            sprintf "    └-%A" x6
        ]
        test <@ expected = Tree.renderList tree @>
    }
