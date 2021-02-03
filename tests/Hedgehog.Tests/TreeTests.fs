module Hedgehog.Tests.TreeTests

open Hedgehog
open TestDsl

let treeTests = testList "Tree tests" [
    testCase "render tree with depth 0" <| fun _ ->
        Property.check (property {
            let! x0 = Gen.constant "0"
            let tree =
                Node (x0, [
                ])
                |> Tree.map (sprintf "%A")

            let expected = [
                sprintf "%A" x0
            ]
            Expect.isTrue (expected = Tree.renderList tree)
        })

    testCase "render tree with depth 1" <| fun _ ->
        Property.check (property {
            let! x0 = Gen.constant "0"
            let! x1 = Gen.constant "1"
            let! x2 = Gen.constant "2"
            let! x3 = Gen.constant "3"

            let tree =
                Node (x0, [
                    Node (x1, [])
                    Node (x2, [])
                    Node (x3, [])
                ])
                |> Tree.map (sprintf "%A")

            let expected = [
                sprintf "%A" x0
                sprintf "├-%A" x1
                sprintf "├-%A" x2
                sprintf "└-%A" x3
            ]
            Expect.isTrue (expected = Tree.renderList tree)
        })

    testCase "render tree with depth 2" <| fun _ ->
        Property.check (property {
            let! x0 = Gen.constant "0"
            let! x1 = Gen.constant "1"
            let! x2 = Gen.constant "2"
            let! x3 = Gen.constant "3"
            let! x4 = Gen.constant "4"
            let! x5 = Gen.constant "5"
            let! x6 = Gen.constant "6"
            let! x7 = Gen.constant "7"
            let! x8 = Gen.constant "8"
            let! x9 = Gen.constant "9"
            let! x10 = Gen.constant "10"
            let! x11 = Gen.constant "11"
            let! x12 = Gen.constant "12"

            let tree =
                Node (x0, [
                    Node (x1, [
                        Node (x4, [])
                        Node (x5, [])
                        Node (x6, [])
                    ])
                    Node (x2, [
                        Node (x7, [])
                        Node (x8, [])
                        Node (x9, [])
                    ])
                    Node (x3, [
                        Node (x10, [])
                        Node (x11, [])
                        Node (x12, [])
                    ])
                ])
                |> Tree.map (sprintf "%A")

            let expected = [
                sprintf "%A" x0
                sprintf "├-%A" x1
                sprintf "| ├-%A" x4
                sprintf "| ├-%A" x5
                sprintf "| └-%A" x6
                sprintf "├-%A" x2
                sprintf "| ├-%A" x7
                sprintf "| ├-%A" x8
                sprintf "| └-%A" x9
                sprintf "└-%A" x3
                sprintf "  ├-%A" x10
                sprintf "  ├-%A" x11
                sprintf "  └-%A" x12
            ]
            Expect.isTrue (expected = Tree.renderList tree)
        })
]
