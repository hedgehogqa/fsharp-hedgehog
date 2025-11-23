// Workaround for a Fable issue: https://github.com/fable-compiler/Fable/issues/2069
#if FABLE_COMPILER
module Hedgehog.FSharp.GenLazy
#else
[<RequireQualifiedAccess>]
module internal Hedgehog.FSharp.GenLazy
#endif

open Hedgehog

let constant a = a |> Lazy.constant |> Gen.constant

let map f = f |> Lazy.map |> Gen.map

let join glgla = glgla |> Gen.bind Lazy.value

let bind f gla = gla |> map f |> join
