namespace Hedgehog.CSharp

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog

[<Extension>]
type PropertyExtensions =

    [<Extension>]
    member _.Where(prop, filter : Func<'T, _>) =
        Property.filter filter.Invoke prop

    [<Extension>]
    member _.Select(property, mapper : Func<'T, 'U>) =
        Property.map mapper.Invoke property

    [<Extension>]
    member _.Select(property, mapper : Action<'T>) =
        Property.bind property (Property.fromThrowing mapper.Invoke)

    [<Extension>]
    member _.SelectMany(property, binder : Func<_, _>, projection : Func<'T, 'TCollection, 'TResult>) =
        Property.bind property (fun a ->
            Property.map (fun b -> projection.Invoke (a, b)) (binder.Invoke(a)))

    [<Extension>]
    member _.SelectMany(property, binder : Func<'T, Property<'U>>, projection : Action<_, _>) =
        Property.bind property (fun a ->
            Property.bind (binder.Invoke a) (fun b ->
                Property.fromThrowing projection.Invoke (a, b)))

#endif
