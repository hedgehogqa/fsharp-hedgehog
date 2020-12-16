namespace Hedgehog.CSharp

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog

[<Extension>]
type Property =

    static member Failure : Property<unit> =
        Property.failure

    static member Discard : Property<unit> =
        Property.discard

    static member Success (value : 'T) : Property<'T> =
        Property.Success(value)

    static member FromBool (value : bool) : Property<unit> =
        Property.ofBool(value)

    static member FromGen (gen : Gen<Journal * Result<'T>>) : Property<'T> =
        Property.ofGen gen

    static member FromThrowing (f : Action<'T>) (arg : 'T) : Property<unit> =
        Property.fromThrowing f.Invoke arg

    static member Delay (f : Func<Property<'T>>) : Property<'T> =
        Property.delay f.Invoke

    static member Using (resource : 'T) (action : Func<'T, Property<'TResult>>) : Property<'TResult> =
        Property.using resource action.Invoke

    static member FromOutcome (result : Result<'T>) : Property<'T> =
        Property.ofResult(result)

    static member CounterExample (message : Func<string>) : Property<unit> =
        Property.counterexample(message.Invoke)

    static member ForAll (gen : Gen<'T>, k : Func<'T, Property<'TResult>>) : Property<'TResult> =
        Property.forAll gen k.Invoke

    static member ForAll (gen : Gen<'T>) : Property<'T> =
        Property.forAll' gen

    [<Extension>]
    static member ToGen (property : Property<'T>) : Gen<Journal * Result<'T>> =
        Property.toGen property

    [<Extension>]
    static member TryFinally (property : Property<'T>) (onFinally : Action) : Property<'T> =
        Property.tryFinally property onFinally.Invoke

    [<Extension>]
    static member TryWith (property : Property<'T>) (onError : Func<exn, Property<'T>>) : Property<'T> =
        Property.tryWith property onError.Invoke

    //
    // Runner
    //

    [<Extension>]
    static member inline Report (property : Property<unit>) : Report =
        Property.report property

    [<Extension>]
    static member inline Report (property : Property<unit>, tests : int<tests>) : Report =
        Property.report' tests property

    [<Extension>]
    static member inline Check (property : Property<unit>) : unit =
        Property.check property

    [<Extension>]
    static member Check (property : Property<unit>, tests : int<tests>) : unit =
        Property.check' tests property

    [<Extension>]
    static member Check (property : Property<bool>) : unit =
        Property.checkBool property

    [<Extension>]
    static member Check (property : Property<bool>, tests : int<tests>) : unit =
        Property.checkBool' tests property

    [<Extension>]
    static member Recheck (property : Property<unit>, size : Size, seed : Seed) : unit =
        Property.recheck size seed property

    [<Extension>]
    static member Recheck (property : Property<unit>, size : Size, seed : Seed, tests : int<tests>) : unit =
        Property.recheck' size seed tests property

    [<Extension>]
    static member Recheck (property : Property<bool>, size : Size, seed : Seed) : unit =
        Property.recheckBool size seed property

    [<Extension>]
    static member Recheck (property : Property<bool>, size : Size, seed : Seed, tests : int<tests>) : unit =
        Property.recheckBool' size seed tests property

    [<Extension>]
    static member Print (property : Property<unit>, tests : int<tests>) : unit =
        Property.print' tests property

    [<Extension>]
    static member Print (property : Property<unit>) : unit =
        Property.print property

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
