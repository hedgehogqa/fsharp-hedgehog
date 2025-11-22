namespace Hedgehog.Linq

open System.Runtime.CompilerServices
open Hedgehog
open Hedgehog.FSharp

[<AbstractClass; Sealed;>]
type GenListExtensions() =

      /// Generates a value that is not contained in the specified list.
    [<Extension>]
    static member NotIn(self : Gen<'T>, values : ResizeArray<'T>) =
        Gen.notIn (List.ofSeq values) self

    /// Generates a list that does not contain the specified element.
    /// Shortcut for Gen.filter (not << List.contains x).
    [<Extension>]
    static member NotContains(self : Gen<ResizeArray<'T>>, value: 'T) =
        Gen.notContains value (self |> Gen.map(List.ofSeq)) |> Gen.map ResizeArray

    // Inserts the given element at a random place in the list.
    // Does not guarantee that the element is unique in the list.
    [<Extension>]
    static member AddElement(self : Gen<ResizeArray<'T>>, x : 'T) =
        self |> Gen.map List.ofSeq |> Gen.addElement x |> Gen.map ResizeArray
