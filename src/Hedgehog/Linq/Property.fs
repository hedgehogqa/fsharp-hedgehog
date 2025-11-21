namespace Hedgehog.Linq

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog


type Property = private Property of Property<unit> with

    static member Failure : Property =
        Property.failure |> Property

    static member Discard : Property =
        Property.discard |> Property

    static member Success (value : 'T) : Property<'T> =
        Property.success value

    static member FromBool (value : bool) : Property =
        value |> Property.ofBool |> Property

    static member FromGen (gen : Gen<Lazy<Journal * Outcome<'T>>>) : Property<'T> =
        Property.ofGen gen

    static member FromOutcome (result : Outcome<'T>) : Property<'T> =
        Property.ofOutcome result

    static member Delay (f : Func<Property<'T>>) : Property<'T> =
        Property.delay f.Invoke

    static member Using (resource : 'T, action : Func<'T, Property<'TResult>>) : Property<'TResult> =
        Property.using resource action.Invoke

    static member CounterExample (message : Func<string>) : Property =
        Property.counterexample message.Invoke
        |> Property

    [<Obsolete("Use .ForAll() extension method")>]
    static member ForAll (gen : Gen<'T>, k : Func<'T, Property<'TResult>>) : Property<'TResult> =
        Property.forAll k.Invoke gen

    [<Obsolete("Use .ForAll() extension method")>]
    static member ForAll (gen : Gen<'T>) : Property<'T> =
        Property.forAll' gen


[<Extension>]
[<AbstractClass; Sealed>]
type PropertyExtensions private () =

    [<Extension>]
    static member ForAll(gen : Gen<'T>): Property<'T> =
        Property.forAll' gen

    [<Extension>]
    static member ForAll(gen : Gen<'T>, bind : Func<'T, Property<'TResult>>): Property<'TResult> =
        Property.forAll bind.Invoke gen

    [<Extension>]
    static member ToGen (property : Property<'T>) : Gen<Lazy<Journal * Outcome<'T>>> =
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
    static member Report (property : Property, config : IPropertyConfig) : Report =
        let (Property property) = property
        Property.reportWith config property

    [<Extension>]
    static member Report (property : Property<bool>) : Report =
        property |> Property.falseToFailure |> Property.report

    [<Extension>]
    static member Report (property : Property<bool>, config : IPropertyConfig) : Report =
        property |> Property.falseToFailure |> Property.reportWith config

    [<Extension>]
    static member Check (property : Property) : unit =
        let (Property property) = property
        Property.check property

    [<Extension>]
    static member Check (property : Property, config : IPropertyConfig) : unit =
        let (Property property) = property
        Property.checkWith config property

    [<Extension>]
    static member Check (property : Property<bool>) : unit =
        property |> Property.falseToFailure |> Property.check

    [<Extension>]
    static member Check (property : Property<bool>, config : IPropertyConfig) : unit =
        property |> Property.falseToFailure |> Property.checkWith config

    [<Extension>]
    static member Recheck (property : Property, recheckData: string) : unit =
        let (Property property) = property
        Property.recheck recheckData property

    [<Extension>]
    static member Recheck (property : Property, recheckData: string, config : IPropertyConfig) : unit =
        let (Property property) = property
        Property.recheckWith recheckData config property

    [<Extension>]
    static member Recheck (property : Property<bool>, recheckData: string) : unit =
        property |> Property.falseToFailure |> Property.recheck recheckData

    [<Extension>]
    static member Recheck (property : Property<bool>, recheckData: string, config : IPropertyConfig) : unit =
        property |> Property.falseToFailure |> Property.recheckWith recheckData config

    [<Extension>]
    static member ReportRecheck (property : Property, recheckData: string) : Report =
        let (Property property) = property
        Property.reportRecheck recheckData property

    [<Extension>]
    static member internal ReportRecheck (property : Property, recheckData: RecheckData) : Report =
        let (Property property) = property
        Property.reportRecheck (RecheckData.serialize recheckData) property

    [<Extension>]
    static member ReportRecheck (property : Property, recheckData: string, config : IPropertyConfig) : Report =
        let (Property property) = property
        Property.reportRecheckWith recheckData config property

    [<Extension>]
    static member ReportRecheck (property : Property<bool>, recheckData: string) : Report =
        property |> Property.falseToFailure |> Property.reportRecheck recheckData

    [<Extension>]
    static member ReportRecheck (property : Property<bool>, recheckData: RecheckData) : Report =
        property |> Property.falseToFailure |> Property.reportRecheck (RecheckData.serialize recheckData)

    [<Extension>]
    static member ReportRecheck (property : Property<bool>, recheckData: string, config : IPropertyConfig) : Report =
        property |> Property.falseToFailure |> Property.reportRecheckWith recheckData config

    [<Extension>]
    static member Render (property : Property) : string =
        let (Property property) = property
        Property.render property

    [<Extension>]
    static member Render (property : Property, config : IPropertyConfig) : string =
        let (Property property) = property
        Property.renderWith config property

    [<Extension>]
    static member Render (property : Property<bool>) : string =
        property |> Property.falseToFailure |> Property.render

    [<Extension>]
    static member Render (property : Property<bool>, config : IPropertyConfig) : string =
        property |> Property.falseToFailure |> Property.renderWith config

    [<Extension>]
    static member Where (property : Property<'T>, filter : Func<'T, bool>) : Property<'T> =
        Property.filter filter.Invoke property

    [<Extension>]
    static member Select (property : Property<'T>, mapper : Func<'T, 'TResult>) : Property<'TResult> =
        property
        |> Property.map mapper.Invoke

    [<Extension>]
    static member Select (property : Property<'T>, mapper : Action<'T>) : Property =
        property
        |> Property.map mapper.Invoke
        |> Property

    [<Extension>]
    static member SelectMany (property : Property<'T>, binder : Func<'T, Property<'TCollection>>, projection : Func<'T, 'TCollection, 'TResult>) : Property<'TResult> =
        property |> Property.bind (fun a ->
            binder.Invoke a |> Property.map (fun b -> projection.Invoke (a, b)))

    [<Extension>]
    static member SelectMany (property : Property<'T>, binder : Func<'T, Property<'TCollection>>, projection : Action<'T, 'TCollection>) : Property =
        let result =
            property |> Property.bind (fun a ->
                binder.Invoke a |> Property.map (fun b ->
                    projection.Invoke (a, b)))
        Property result

#endif
