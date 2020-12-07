namespace Hedgehog.CSharp

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog

[<Extension>]
type PropertyExtensions =

    [<Extension>]
    static member inline Where(property : Property<'T>, filter : Func<'T, bool>) : Property<'T> =
        Property.filter filter.Invoke property

    [<Extension>]
    static member inline Select(property : Property<'T>, mapper : Func<'T, 'TResult>) : Property<'TResult> =
        Property.map mapper.Invoke property

    [<Extension>]
    static member inline Select(property : Property<'T>, mapper : Action<'T>) : Property<unit> =
        Property.bind property (Property.fromThrowing mapper.Invoke)

    [<Extension>]
    static member inline SelectMany(property : Property<'T>, binder : Func<'T, Property<'TCollection>>, projection : Func<'T, 'TCollection, 'TResult>) : Property<'TResult> =
        Property.bind property (fun a ->
            Property.map (fun b -> projection.Invoke(a, b)) (binder.Invoke(a)))

    [<Extension>]
    static member inline SelectMany(property : Property<'T>, binder : Func<'T, Property<'TCollection>>, projection : Action<'T, 'TCollection>) : Property<unit> =
        Property.bind property (fun a ->
            Property.bind (binder.Invoke a) (fun b ->
                Property.fromThrowing projection.Invoke (a, b)))

#endif
