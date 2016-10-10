module Jack.Tests.ShrinkTests

open FSharpx.Collections
open Jack
open Swensen.Unquote
open Xunit

[<Fact>]
let ``removes permutes a list by removing 'k' consecutive elements from it``() =
    let actual = Shrink.removes 2 [ 1; 2; 3; 4; 5; 6 ]

    let expected =
        LazyList.ofList [ [ 3; 4; 5; 6 ]
                          [ 1; 2; 5; 6 ]
                          [ 1; 2; 3; 4 ] ]
    // http://stackoverflow.com/a/17101488
    test <@ Seq.fold (&&) true (Seq.zip expected actual |> Seq.map (fun (a, b) -> a = b)) @>
