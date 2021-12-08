// Workaround for a Fable issue: https://github.com/fable-compiler/Fable/issues/2069
#if FABLE_COMPILER
module Hedgehog.Lazy
#else
[<RequireQualifiedAccess>]
module internal Hedgehog.Lazy
#endif

let func (f: unit -> 'a) = Lazy<'a>(valueFactory = fun () -> f ())

let constant (a: 'a) = Lazy<'a>(valueFactory = fun () -> a)

let value (ma: Lazy<'a>) = ma.Value

let map (f: 'a -> 'b) (ma: Lazy<'a>) : Lazy<'b> =
    (fun () -> ma.Value |> f)
    |> func

let join (mma: Lazy<Lazy<'a>>) =
    (fun () -> mma.Value.Value)
    |> func

let bind (f: 'a -> Lazy<'b>) =
    f |> map >> join
