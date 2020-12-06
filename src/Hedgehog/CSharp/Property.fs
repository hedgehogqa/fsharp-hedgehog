namespace Hedgehog.CSharp

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog

[<Extension>]
type PropertyExtensions =

    [<Extension>]
    static member inline Where(property, filter : Func<'T, _>) =
        Property.filter filter.Invoke property

    [<Extension>]
    static member inline Select(property, mapper : Func<'T, 'U>) =
        Property.map mapper.Invoke property

    [<Extension>]
    static member inline Select(property, mapper : Action<'T>) =
        Property.bind property (Property.fromThrowing mapper.Invoke)

    [<Extension>]
    static member inline SelectMany(property, binder : Func<_, _>, projection : Func<'T, 'TCollection, 'TResult>) =
        Property.bind property (fun a ->
            Property.map (fun b -> projection.Invoke (a, b)) (binder.Invoke(a)))

    [<Extension>]
    static member inline SelectMany(property, binder : Func<'T, Property<'U>>, projection : Action<_, _>) =
        Property.bind property (fun a ->
            Property.bind (binder.Invoke a) (fun b ->
                Property.fromThrowing projection.Invoke (a, b)))

#endif
