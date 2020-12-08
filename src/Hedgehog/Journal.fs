namespace Hedgehog

[<Struct>]
type Journal =
    | Journal of seq<unit -> string>

module Journal =
    let ofList (xs : seq<unit -> string>) : Journal =
        Journal xs

    let toList (Journal xs : Journal) : List<string> =
        Seq.toList <| Seq.map (fun f -> f ()) xs

    let empty : Journal =
        Seq.empty |> ofList

    let singleton (x : string) : Journal =
        Seq.singleton (fun () -> x) |> ofList

    let delayedSingleton (x : unit -> string) : Journal =
        Seq.singleton x |> ofList

    let append (Journal xs) (Journal ys) : Journal =
        Seq.append xs ys |> ofList
