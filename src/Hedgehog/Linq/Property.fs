namespace Hedgehog.Linq

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog

type PropertyThrows =
    private
        PropertyThrows of Property<unit>

[<Extension>]
[<AbstractClass; Sealed>]
type Property private () =

    static member Failure : PropertyThrows =
        Property.failure
        |> PropertyThrows

    static member Discard : PropertyThrows =
        Property.discard
        |> PropertyThrows

    static member Success (value : 'T) : Property<'T> =
        Property.success value

    static member FromBool (value : bool) : PropertyThrows =
        Property.ofBool value
        |> PropertyThrows

    static member FromGen (gen : Gen<Journal * Result<'T>>) : Property<'T> =
        Property.ofGen gen

    static member FromResult (result : Result<'T>) : Property<'T> =
        Property.ofResult result

    static member FromThrowing (throwingFunc : Action<'T>, arg : 'T) : PropertyThrows =
        Property.ofThrowing throwingFunc.Invoke arg
        |> PropertyThrows

    static member Delay (f : Func<Property<'T>>) : Property<'T> =
        Property.delay f.Invoke

    static member Using (resource : 'T, action : Func<'T, Property<'TResult>>) : Property<'TResult> =
        Property.using resource action.Invoke

    static member CounterExample (message : Func<string>) : PropertyThrows =
        Property.counterexample(message.Invoke)
        |> PropertyThrows

    static member ForAll (gen : Gen<'T>, k : Func<'T, Property<'TResult>>) : Property<'TResult> =
        Property.forAll gen k.Invoke

    static member ForAll (gen : Gen<'T>) : Property<'T> =
        Property.forAll' gen

    [<Extension>]
    static member ToGen (property : Property<'T>) : Gen<Journal * Result<'T>> =
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
    static member Report (property : PropertyThrows) : Report =
        let (PropertyThrows property) = property
        Property.report property

    [<Extension>]
    static member Report (property : PropertyThrows, tests : int<tests>) : Report =
        let (PropertyThrows property) = property
        Property.report' tests property

    [<Extension>]
    static member Report (property : Property<bool>) : Report =
        Property.reportBool property

    [<Extension>]
    static member Report (property : Property<bool>, tests : int<tests>) : Report =
        Property.reportBool' tests property

    [<Extension>]
    static member Check (property : PropertyThrows) : unit =
        let (PropertyThrows property) = property
        Property.check property

    [<Extension>]
    static member Check (property : PropertyThrows, tests : int<tests>) : unit =
        let (PropertyThrows property) = property
        Property.check' tests property

    [<Extension>]
    static member Check (property : Property<bool>) : unit =
        Property.checkBool property

    [<Extension>]
    static member Check (property : Property<bool>, tests : int<tests>) : unit =
        Property.checkBool' tests property

    [<Extension>]
    static member Recheck (property : PropertyThrows, size : Size, seed : Seed) : unit =
        let (PropertyThrows property) = property
        Property.recheck size seed property

    [<Extension>]
    static member Recheck (property : PropertyThrows, size : Size, seed : Seed, tests : int<tests>) : unit =
        let (PropertyThrows property) = property
        Property.recheck' size seed tests property

    [<Extension>]
    static member Recheck (property : Property<bool>, size : Size, seed : Seed) : unit =
        Property.recheckBool size seed property

    [<Extension>]
    static member Recheck (property : Property<bool>, size : Size, seed : Seed, tests : int<tests>) : unit =
        Property.recheckBool' size seed tests property

    [<Extension>]
    static member ReportRecheck (property : PropertyThrows, size : Size, seed : Seed) : Report =
        let (PropertyThrows property) = property
        Property.reportRecheck size seed property

    [<Extension>]
    static member ReportRecheck (property : PropertyThrows, size : Size, seed : Seed, tests : int<tests>) : Report =
        let (PropertyThrows property) = property
        Property.reportRecheck' size seed tests property

    [<Extension>]
    static member ReportRecheck (property : Property<bool>, size : Size, seed : Seed) : Report =
        Property.reportRecheckBool size seed property

    [<Extension>]
    static member ReportRecheck (property : Property<bool>, size : Size, seed : Seed, tests : int<tests>) : Report =
        Property.reportRecheckBool' size seed tests property

    [<Extension>]
    static member Print (property : PropertyThrows, tests : int<tests>) : unit =
        let (PropertyThrows property) = property
        Property.print' tests property

    [<Extension>]
    static member Print (property : PropertyThrows) : unit =
        let (PropertyThrows property) = property
        Property.print property

    [<Extension>]
    static member Where (property : Property<'T>, filter : Func<'T, bool>) : Property<'T> =
        Property.filter filter.Invoke property

    [<Extension>]
    static member Select (property : Property<'T>, mapper : Func<'T, 'TResult>) : Property<'TResult> =
        Property.map mapper.Invoke property

    [<Extension>]
    static member Select (property : Property<'T>, mapper : Action<'T>) : PropertyThrows =
        Property.bind property (Property.ofThrowing mapper.Invoke)
        |> PropertyThrows

    [<Extension>]
    static member SelectMany (property : Property<'T>, binder : Func<'T, Property<'TCollection>>, projection : Func<'T, 'TCollection, 'TResult>) : Property<'TResult> =
        Property.bind property (fun a ->
            Property.map (fun b -> projection.Invoke(a, b)) (binder.Invoke(a)))

    [<Extension>]
    static member SelectMany (property : Property<'T>, binder : Func<'T, Property<'TCollection>>, projection : Action<'T, 'TCollection>) : PropertyThrows =
        let result =
            Property.bind property (fun a ->
                Property.bind (binder.Invoke a) (fun b ->
                    Property.ofThrowing projection.Invoke (a, b)))
        PropertyThrows result

#endif
