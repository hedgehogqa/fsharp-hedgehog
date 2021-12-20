module Hedgehog.Tests.TreeTests

open Hedgehog
open TestDsl

let treeTests = testList "Tree tests" [
    testCase "children during bind are concatenated in the superior order" <| fun _ ->
        // Tree.bind can be defined in two ways corresponding to the order in
        // which the old and new children are concatenated.  This test ensures
        // that the superior of these two orderings is being used.  For more
        // information, see
        // https://well-typed.com/blog/2019/05/integrated-shrinking/#:~:text=Although%20this%20version%20of%20join%20still%20satisfies%20the%20monad%20laws%2C%20it%20is%20strictly%20worse.
        let iTree = 1 |> Tree.singleton |> Tree.addChildValue 0

        let actual =
            "b"
            |> Tree.singleton
            |> Tree.addChildValue "a"
            |> Tree.bind (fun s ->
                iTree
                |> Tree.map (fun i -> s, i))

        let expected =
            Tree.create
                ("b", 1)
                [ Tree.create
                      ("a", 1)
                      [ Tree.singleton
                            ("a", 0)
                      ]
                  Tree.singleton
                      ("b", 0)
                ]

        true =! Tree.equals actual expected


    testCase "depth of tree with no subtrees is 0" <| fun _ ->
        let actual =
            Tree.singleton "a"
            |> Tree.depth
        0 =! actual

    testCase "depth of tree with only one subtree of depth 0 is 1" <| fun _ ->
        let actual =
            Tree.singleton "a"
            |> Tree.addChildValue "b"
            |> Tree.depth
        1 =! actual

    testCase "render tree with depth 0" <| fun _ ->
        property {
            let! x0 = Gen.constant "0"
            let tree =
                Node (x0, [
                ])
                |> Tree.map (sprintf "%A")

            let expected = [
                sprintf "%A" x0
            ]
            Expect.isTrue (expected = Tree.renderList tree)
        }
        |> Property.check

    testCase "render tree with depth 1" <| fun _ ->
        property {
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
        }
        |> Property.check

    testCase "render tree with depth 2" <| fun _ ->
        property {
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
        }
        |> Property.check
]
