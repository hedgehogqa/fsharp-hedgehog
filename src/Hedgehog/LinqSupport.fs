namespace Hedgehog

open System
open System.Runtime.CompilerServices

open Hedgehog

[<Extension>]
module GenLinqSupport =

    // Support for `where`:
    [<Extension>]
    [<CompiledName("Where")>]
    let where (g : Gen<'a>) (f : Func<'a, bool>) : Gen<'a> =
        Gen.filter (fun x -> f.Invoke x) g

    [<Extension>]
    [<CompiledName("Select")>]
    let select (g : Gen<'a>) (f : Func<'a, 'b>) : Gen<'b> =
        Gen.map (fun x -> f.Invoke x) g

    [<Extension>]
    [<CompiledName("SelectMany")>]
    let bind2 (g : Gen<'a>) (f : Func<'a, Gen<'b>>) (proj : Func<'a, 'b, 'c>) : Gen<'c> =
        gen {
            let! a = g
            let! b = f.Invoke a
            return proj.Invoke (a, b)
        }

[<Extension>]
module PropertyLinqSupport =

    [<Extension>]
    [<CompiledName("Where")>]
    let where (p : Property<'a>) (f : Func<'a, bool>) : Property<'a> =
        Property.filter (fun x -> f.Invoke x) p

    [<Extension>]
    [<CompiledName("Select")>]
    let select (p : Property<'a>) (f : Func<'a, 'b>) : Property<'b> =
        Property.map (fun x -> f.Invoke x) p

    [<Extension>]
    [<CompiledName("SelectMany")>]
    let bind2 (pa : Property<'a>) (f : Func<'a, Property<'b>>) (proj : Func<'a, 'b, 'c>) : Property<'c> =
        Property.bind pa (fun a -> Property.bind (f.Invoke a) (fun b -> Property.success (proj.Invoke (a, b))))

    // This supports simple assertions in `select`:
    [<Extension>]
#if !FABLE_COMPILER
    [<CompiledName("Select")>]
#endif    
    let selectUnit (p : Property<'a>) (f : Action<'a>) : Property<unit> =
        Property.bind p (Property.fromThrowing f.Invoke)

    // This supports assertions as `select`:
    [<Extension>]
#if !FABLE_COMPILER
    [<CompiledName("SelectMany")>]
#endif    
    let bind2Unit (pa : Property<'a>) (f : Func<'a, Property<'b>>) (proj : Action<'a, 'b>) : Property<unit> =
        Property.bind pa (fun a ->
            Property.bind (f.Invoke a) (fun b -> Property.fromThrowing proj.Invoke (a, b)))

[<Extension>]
module RangeLinqSupport =
    
    [<Extension>]
    [<CompiledName("Select")>]
    let select (r : Range<'a>) (f : Func<'a, 'b>) : Range<'b> =
        Range.map (fun x -> f.Invoke x) r
