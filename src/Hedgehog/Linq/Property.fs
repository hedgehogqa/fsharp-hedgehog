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
        Property.forAll k.Invoke gen

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
        Property.tryFinally onFinally.Invoke property

    [<Extension>]
    static member TryWith (property : Property<'T>, onError : Func<exn, Property<'T>>) : Property<'T> =
        Property.tryWith onError.Invoke property

    [<Extension>]
    static member Report (property : Property) : Report =
        let (Property property) = property
        Property.report property

    [<Extension>]
    static member Report (property : Property, config : Hedgehog.PropertyConfig) : Report =
        let (Property property) = property
        Property.reportWith config property

    [<Extension>]
    static member Report (property : Property<bool>) : Report =
        Property.reportBool property

    [<Extension>]
    static member Report (property : Property<bool>, config : Hedgehog.PropertyConfig) : Report =
        Property.reportBoolWith config property

    [<Extension>]
    static member Check (property : Property) : unit =
        let (Property property) = property
        Property.check property

    [<Extension>]
    static member Check (property : Property, config : Hedgehog.PropertyConfig) : unit =
        let (Property property) = property
        Property.checkWith config property

    [<Extension>]
    static member Check (property : Property<bool>) : unit =
        Property.checkBool property

    [<Extension>]
    static member Check (property : Property<bool>, config : Hedgehog.PropertyConfig) : unit =
        Property.checkBoolWith config property

    [<Extension>]
    static member Recheck (property : Property, size : Size, seed : Seed) : unit =
        let (Property property) = property
        Property.recheck size seed property

    [<Extension>]
    static member Recheck (property : Property, size : Size, seed : Seed, config : Hedgehog.PropertyConfig) : unit =
        let (Property property) = property
        Property.recheckWith size seed config property

    [<Extension>]
    static member Recheck (property : Property<bool>, size : Size, seed : Seed) : unit =
        Property.recheckBool size seed property

    [<Extension>]
    static member Recheck (property : Property<bool>, size : Size, seed : Seed, config : Hedgehog.PropertyConfig) : unit =
        Property.recheckBoolWith size seed config property

    [<Extension>]
    static member ReportRecheck (property : Property, size : Size, seed : Seed) : Report =
        let (Property property) = property
        Property.reportRecheck size seed property

    [<Extension>]
    static member ReportRecheck (property : Property, size : Size, seed : Seed, config : Hedgehog.PropertyConfig) : Report =
        let (Property property) = property
        Property.reportRecheckWith size seed config property

    [<Extension>]
    static member ReportRecheck (property : Property<bool>, size : Size, seed : Seed) : Report =
        Property.reportRecheckBool size seed property

    [<Extension>]
    static member ReportRecheck (property : Property<bool>, size : Size, seed : Seed, config : Hedgehog.PropertyConfig) : Report =
        Property.reportRecheckBoolWith size seed config property

    [<Extension>]
    static member Render (property : Property) : string =
        let (Property property) = property
        Property.render property

    [<Extension>]
    static member Render (property : Property, config : Hedgehog.PropertyConfig) : string =
        let (Property property) = property
        Property.renderWith config property

    [<Extension>]
    static member Render (property : Property<bool>) : string =
        Property.renderBool property

    [<Extension>]
    static member Render (property : Property<bool>, config : Hedgehog.PropertyConfig) : string =
        Property.renderBoolWith config property

    [<Extension>]
    static member Where (property : Property<'T>, filter : Func<'T, bool>) : Property<'T> =
        Property.filter filter.Invoke property

    [<Extension>]
    static member Select (property : Property<'T>, mapper : Func<'T, 'TResult>) : Property<'TResult> =
        property
        |> Property.bind (Property.ofThrowing mapper.Invoke)

    [<Extension>]
    static member Select (property : Property<'T>, mapper : Action<'T>) : Property =
        property
        |> Property.bind (Property.ofThrowing mapper.Invoke)
        |> Property

    [<Extension>]
    static member SelectMany (property : Property<'T>, binder : Func<'T, Property<'TCollection>>, projection : Func<'T, 'TCollection, 'TResult>) : Property<'TResult> =
        property |> Property.bind (fun a ->
            binder.Invoke a |> Property.map (fun b -> projection.Invoke (a, b)))

    [<Extension>]
    static member SelectMany (property : Property<'T>, binder : Func<'T, Property<'TCollection>>, projection : Action<'T, 'TCollection>) : Property =
        let result =
            property |> Property.bind (fun a ->
                binder.Invoke a |> Property.bind (fun b ->
                    Property.ofThrowing projection.Invoke (a, b)))
        Property result

#endif
