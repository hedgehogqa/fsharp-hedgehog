namespace Hedgehog.Linq

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog
open System.Runtime.InteropServices

module internal Build =
    let config tests shrinkLimit =
        PropertyConfig.defaultConfig
        |> PropertyConfig.withTestCount (tests |> Option.defaultValue PropertyConfig.defaultConfig.TestCount)
        |> fun config ->
            match shrinkLimit with
            | Some shrinkLimit -> config |> PropertyConfig.withShrinkLimit shrinkLimit
            | None -> config


[<Extension>]
[<AbstractClass; Sealed>]
type PropertyConfigExtensions private () =

    /// The number of shrinks to try before giving up on shrinking.
    [<Extension>]
    static member WithShrinkLimit (config : PropertyConfig, shrinkLimit: int<shrinks>) : PropertyConfig =
        PropertyConfig.withShrinkLimit shrinkLimit config

    /// The number of successful tests that need to be run before a property test is considered successful.
    [<Extension>]
    static member WithTestCount (config : PropertyConfig, tests: int<tests>) : PropertyConfig =
        PropertyConfig.withTestCount tests config


type PropertyConfig =

    /// The default configuration for a property test.
    static member DefaultConfig : Hedgehog.PropertyConfig =
        PropertyConfig.defaultConfig

        
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
    static member Report (property : Property<bool>) : Report =
        Property.reportBool property

    [<Extension>]
    static member Check
        (   property : Property,
            [<Optional; DefaultParameterValue null>] ?tests       : int<tests>,
            [<Optional; DefaultParameterValue null>] ?shrinkLimit : int<shrinks>
        ) : unit =
        let (Property property) = property
        Property.checkWith (Build.config tests shrinkLimit) property

    [<Extension>]
    static member Check
        (   property : Property<bool>,
            [<Optional; DefaultParameterValue null>] ?tests       : int<tests>,
            [<Optional; DefaultParameterValue null>] ?shrinkLimit : int<shrinks>
        ) : unit =
        Property.checkBoolWith (Build.config tests shrinkLimit) property

    [<Extension>]
    static member Check (property : Property, config : Hedgehog.PropertyConfig) : unit =
        let (Property property) = property
        Property.checkWith config property

    [<Extension>]
    static member Check (property : Property<bool>, config : Hedgehog.PropertyConfig) : unit =
        Property.checkBoolWith config property

    [<Extension>]
    static member Recheck
        (   property : Property,
            size : Size,
            seed : Seed,
            [<Optional; DefaultParameterValue null>] ?tests       : int<tests>,
            [<Optional; DefaultParameterValue null>] ?shrinkLimit : int<shrinks>
        ) : unit =
        let (Property property) = property
        Property.recheckWith size seed (Build.config tests shrinkLimit) property

    [<Extension>]
    static member Recheck
        (   property : Property<bool>,
            size : Size,
            seed : Seed,
            [<Optional; DefaultParameterValue null>] ?tests       : int<tests>,
            [<Optional; DefaultParameterValue null>] ?shrinkLimit : int<shrinks>
        ) : unit =
        Property.recheckBoolWith size seed (Build.config tests shrinkLimit) property

    [<Extension>]
    static member Recheck (property : Property, size : Size, seed : Seed, config : Hedgehog.PropertyConfig) : unit =
        let (Property property) = property
        Property.recheckWith size seed config property

    [<Extension>]
    static member Recheck (property : Property<bool>, size : Size, seed : Seed, config : Hedgehog.PropertyConfig) : unit =
        Property.recheckBoolWith size seed config property

    [<Extension>]
    static member ReportRecheck
        (   property : Property,
            size : Size,
            seed : Seed,
            [<Optional; DefaultParameterValue null>] ?tests       : int<tests>,
            [<Optional; DefaultParameterValue null>] ?shrinkLimit : int<shrinks>
        ) : Report =
        let (Property property) = property
        Property.reportRecheckWith size seed (Build.config tests shrinkLimit) property

    [<Extension>]
    static member ReportRecheck
        (   property : Property<bool>,
            size : Size,
            seed : Seed,
            [<Optional; DefaultParameterValue null>] ?tests       : int<tests>,
            [<Optional; DefaultParameterValue null>] ?shrinkLimit : int<shrinks>
        ) : Report =
        Property.reportRecheckBoolWith size seed (Build.config tests shrinkLimit) property

    [<Extension>]
    static member ReportRecheck (property : Property, size : Size, seed : Seed, config : Hedgehog.PropertyConfig) : Report =
        let (Property property) = property
        Property.reportRecheckWith size seed config property

    [<Extension>]
    static member ReportRecheck (property : Property<bool>, size : Size, seed : Seed, config : Hedgehog.PropertyConfig) : Report =
        Property.reportRecheckBoolWith size seed config property

    [<Extension>]
    static member Print
        (   property : Property,
            [<Optional; DefaultParameterValue null>] ?tests       : int<tests>,
            [<Optional; DefaultParameterValue null>] ?shrinkLimit : int<shrinks>
        ) : unit =
        let (Property property) = property
        Property.printWith (Build.config tests shrinkLimit) property

    [<Extension>]
    static member Print
        (   property : Property<bool>,
            [<Optional; DefaultParameterValue null>] ?tests       : int<tests>,
            [<Optional; DefaultParameterValue null>] ?shrinkLimit : int<shrinks>
        ) : unit =
        Property.printBoolWith (Build.config tests shrinkLimit) property

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
