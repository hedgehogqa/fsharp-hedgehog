module Hedgehog.Tests.ListGenTests

open Hedgehog
open Hedgehog.FSharp
open TestDsl


let listGenTests = testList "ListGen tests" [
    testCase "sequence is applicative" <| fun _ ->
        let gen = Range.constant 0 1 |> Gen.int32
        let listGen = [gen; gen]

        let genList = listGen |> Gen.sequenceList
        let actual =
            seq {
                while true do
                    let treeList = genList |> Gen.sampleTree 0 1 |> Seq.head
                    if Tree.outcome treeList = [1; 1] then
                        yield treeList
            } |> Seq.head

        let expected =
            Node ([1; 1], [
                Node ([0; 1], [
                    Node ([0; 0], [])
                ])
                Node ([1; 0],[
                    Node ([0; 0], [])
                ])
            ])
        (actual      |> Tree.map (sprintf "%A") |> Tree.render)
        =! (expected |> Tree.map (sprintf "%A") |> Tree.render)

]
