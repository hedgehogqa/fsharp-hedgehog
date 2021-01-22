namespace Hedgehog.Linq

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog

type Property = private Property of Property<unit> with

    static member Failure : Property =
        Property.failure
        |> Property

    static member Discard : Property =
        Property.discard
        |> Property

    static member Success (value : 'T) : Property<'T> =
        Property.success value

    static member FromBool (value : bool) : Property =
        Property.ofBool value
        |> Property

    static member FromGen (gen : Gen<Journal * Outcome<'T>>) : Property<'T> =
        Property.ofGen gen

    static member FromOutcome (result : Outcome<'T>) : Property<'T> =
        Property.ofOutcome result

    static member FromThrowing (throwingFunc : Action<'T>, arg : 'T) : Property =
        Property.ofThrowing throwingFunc.Invoke arg
        |> Property

    static member Delay (f : Func<Property<'T>>) : Property<'T> =
        Property.delay f.Invoke

    static member Using (resource : 'T, action : Func<'T, Property<'TResult>>) : Property<'TResult> =
        Property.using resource action.Invoke

    static member CounterExample (message : Func<string>) : Property =
        Property.counterexample message.Invoke
        |> Property

    static member ForAll (gen : Gen<'T>, k : Func<'T, Property<'TResult>>) : Property<'TResult> =
        Property.forAll gen k.Invoke

    static member ForAll (gen : Gen<'T>) : Property<'T> =
        Property.forAll' gen

[<Extension>]
[<AbstractClass; Sealed>]
type PropertyExtensions private () =

    [<Extension>]
    static member ToGen (property : Property<'T>) : Gen<Journal * Outcome<'T>> =
        Property.toGen property

    [<Extension>]
    static member TryFinally (property : Property<'T>, onFinally : Action) : Property<'T> =
        Property.tryFinally property onFinally.Invoke

    [<Extension>]
    static member TryWith (property : Property<'T>, onError : Func<exn, Property<'T>>) : Property<'T> =
        Property.tryWith property onError.Invoke

    //
    // Runner
    //

    [<Extension>]
    static member Report (property : Property) : Report =
        let (Property property) = property
        Property.report property

    [<Extension>]
    static member Report (property : Property, tests : int<tests>) : Report =
        let (Property property) = property
        Property.report' tests property

    [<Extension>]
    static member Report (property : Property<bool>) : Report =
        Property.reportBool property

    [<Extension>]
    static member Report (property : Property<bool>, tests : int<tests>) : Report =
        Property.reportBool' tests property

    [<Extension>]
    static member Check (property : Property) : unit =
        let (Property property) = property
        Property.check property

    [<Extension>]
    static member Check (property : Property, tests : int<tests>) : unit =
        let (Property property) = property
        Property.check' tests property

    [<Extension>]
    static member Check (property : Property<bool>) : unit =
        Property.checkBool property

    [<Extension>]
    static member Check (property : Property<bool>, tests : int<tests>) : unit =
        Property.checkBool' tests property

    [<Extension>]
    static member Recheck (property : Property, size : Size, seed : Seed) : unit =
        let (Property property) = property
        Property.recheck size seed property

    [<Extension>]
    static member Recheck (property : Property, size : Size, seed : Seed, tests : int<tests>) : unit =
        let (Property property) = property
        Property.recheck' size seed tests property

    [<Extension>]
    static member Recheck (property : Property<bool>, size : Size, seed : Seed) : unit =
        Property.recheckBool size seed property

    [<Extension>]
    static member Recheck (property : Property<bool>, size : Size, seed : Seed, tests : int<tests>) : unit =
        Property.recheckBool' size seed tests property

    [<Extension>]
    static member ReportRecheck (property : Property, size : Size, seed : Seed) : Report =
        let (Property property) = property
        Property.reportRecheck size seed property

    [<Extension>]
    static member ReportRecheck (property : Property, size : Size, seed : Seed, tests : int<tests>) : Report =
        let (Property property) = property
        Property.reportRecheck' size seed tests property

    [<Extension>]
    static member ReportRecheck (property : Property<bool>, size : Size, seed : Seed) : Report =
        Property.reportRecheckBool size seed property

    [<Extension>]
    static member ReportRecheck (property : Property<bool>, size : Size, seed : Seed, tests : int<tests>) : Report =
        Property.reportRecheckBool' size seed tests property

    [<Extension>]
    static member Print (property : Property, tests : int<tests>) : unit =
        let (Property property) = property
        Property.print' tests property

    [<Extension>]
    static member Print (property : Property) : unit =
        let (Property property) = property
        Property.print property

    [<Extension>]
    static member Where (property : Property<'T>, filter : Func<'T, bool>) : Property<'T> =
        Property.filter filter.Invoke property

    [<Extension>]
    static member Select (property : Property<'T>, mapper : Func<'T, 'TResult>) : Property<'TResult> =
        Property.map mapper.Invoke property

    [<Extension>]
    static member Select (property : Property<'T>, mapper : Action<'T>) : Property =
        Property.bind property (Property.ofThrowing mapper.Invoke)
        |> Property

    [<Extension>]
    static member SelectMany (property : Property<'T>, binder : Func<'T, Property<'TCollection>>, projection : Func<'T, 'TCollection, 'TResult>) : Property<'TResult> =
        Property.bind property (fun a ->
            Property.map (fun b -> projection.Invoke (a, b)) (binder.Invoke a))

    [<Extension>]
    static member SelectMany (property : Property<'T>, binder : Func<'T, Property<'TCollection>>, projection : Action<'T, 'TCollection>) : Property =
        let result =
            Property.bind property (fun a ->
                Property.bind (binder.Invoke a) (fun b ->
                    Property.ofThrowing projection.Invoke (a, b)))
        Property result

#endif
