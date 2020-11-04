module Hedgehog.Tests.TreeTests

open Hedgehog
open Swensen.Unquote
open Xunit

[<Fact>]
let ``render of singleton tree`` () =
    Property.check <| property {
        let! v = Gen.uint64 (Range.exponentialBounded ())
        let tree = Tree.singleton v |> Tree.map (sprintf "%A")
        let expected = sprintf "%A" v
        test <@ expected = Tree.render tree @>
    }

[<Fact>]
let ``renderLines of binary tree with depth 1`` () =
    Property.check <| property {
        let! v = Gen.uint64 (Range.exponentialBounded ())
        let! v0 = Gen.uint64 (Range.exponentialBounded ())
        let! v1 = Gen.uint64 (Range.exponentialBounded ())
        let tree =
            Node (v, [ v0; v1 ] |> Seq.map Tree.singleton)
            |> Tree.map (sprintf "%A")
        let expected = [
            sprintf "%A" v
            sprintf " ├-%A" v0
            sprintf " └-%A" v1
        ]
        test <@ expected = Tree.renderLines tree @>
    }

[<Fact>]
let ``renderLines of binary tree with depth 2`` () =
    Property.check <| property {
        let! v = Gen.uint64 (Range.exponentialBounded ())
        let! v0 = Gen.uint64 (Range.exponentialBounded ())
        let! v1 = Gen.uint64 (Range.exponentialBounded ())
        let! v00 = Gen.uint64 (Range.exponentialBounded ())
        let! v01 = Gen.uint64 (Range.exponentialBounded ())
        let! v10 = Gen.uint64 (Range.exponentialBounded ())
        let! v11 = Gen.uint64 (Range.exponentialBounded ())
        let v0Children = Node (v0, [ v00; v01 ] |> Seq.map Tree.singleton)
        let v1Children = Node (v1, [ v10; v11 ] |> Seq.map Tree.singleton)
        let tree =
            Node (v, [ v0Children; v1Children ])
            |> Tree.map (sprintf "%A")
        let expected = [
            sprintf "%A" v
            sprintf " ├-%A" v0
            sprintf " |  ├-%A" v00
            sprintf " |  └-%A" v01
            sprintf " └-%A" v1
            sprintf "    ├-%A" v10
            sprintf "    └-%A" v11
        ]
        test <@ expected = Tree.renderLines tree @>
    }