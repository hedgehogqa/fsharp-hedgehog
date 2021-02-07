namespace Hedgehog.Linq

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog
open System.Runtime.InteropServices

type private HProperty = Hedgehog.Property

[<Extension>]
[<AbstractClass; Sealed>]
type PropertyConfigExtensions private () =

    /// Set the number of times a property is allowed to shrink before the test runner gives up and prints the counterexample.
    [<Extension>]
    static member WithShrinks (config : PropertyConfig, shrinkLimit: int<shrinks>) : PropertyConfig =
        PropertyConfig.withShrinks shrinkLimit config

    /// Restores the default shrinking behavior.
    [<Extension>]
    static member WithoutShrinks (config : PropertyConfig) : PropertyConfig =
        PropertyConfig.withoutShrinks config

    /// Set the number of times a property should be executed before it is considered successful.
    [<Extension>]
    static member WithTests (config : PropertyConfig, testLimit: int<tests>) : PropertyConfig =
        PropertyConfig.withTests testLimit config


type PropertyConfig =

    /// The default configuration for a property test.
    static member Default : Hedgehog.PropertyConfig =
        PropertyConfig.defaultConfig


type Property =

    static member Failure : HProperty =
        Property.failure

    static member Discard : HProperty =
        Property.discard

    static member Success : HProperty =
        Property.success

    static member FromBool (value : bool) : HProperty =
        Property.ofBool value

    static member FromGen (gen : Gen<Journal * Outcome>) : HProperty =
        Property.ofGen gen

    static member FromOutcome (result : Outcome) : HProperty =
        Property.ofOutcome result

    static member FromThrowing (throwingFunc : Action<'T>, arg : 'T) : HProperty =
        Property.ofThrowing throwingFunc.Invoke arg

    static member Delay (f : Func<HProperty>) : HProperty =
        Property.delay f.Invoke

    static member Using (resource : 'T, action : Func<'T, HProperty>) : HProperty =
        Property.using resource action.Invoke

    static member CounterExample (message : Func<string>) : HProperty =
        Property.counterexample message.Invoke

    static member ForAll (gen : Gen<unit>, k : Func<HProperty>) : HProperty =
        Property.forAll k.Invoke gen

    static member ForAll (gen : Gen<'T>) : HProperty =
        Property.forAll' gen


module internal PropertyConfig =
    let coalesce = function
        | Some x -> x
        | None -> PropertyConfig.defaultConfig


[<Extension>]
[<AbstractClass; Sealed>]
type PropertyExtensions private () =

    [<Extension>]
    static member ToGen (property : HProperty) : Gen<Journal * Outcome> =
        Property.toGen property

    [<Extension>]
    static member TryFinally (property : HProperty, onFinally : Action) : HProperty =
        Property.tryFinally onFinally.Invoke property

    [<Extension>]
    static member TryWith (property : HProperty, onError : Func<exn, HProperty>) : HProperty =
        Property.tryWith onError.Invoke property

    //
    // Runner
    //

    [<Extension>]
    static member Report
        (   property : HProperty,
            [<Optional; DefaultParameterValue null>] ?config : Hedgehog.PropertyConfig
        ) : Report =
        Property.reportWith (PropertyConfig.coalesce config) property

    [<Extension>]
    static member Check
        (   property : HProperty,
            [<Optional; DefaultParameterValue null>] ?config : Hedgehog.PropertyConfig
        ) : unit =
        Property.checkWith (PropertyConfig.coalesce config) property

    [<Extension>]
    static member Recheck
        (   property : HProperty,
            size : Size,
            seed : Seed,
            [<Optional; DefaultParameterValue null>] ?config : Hedgehog.PropertyConfig
        ) : unit =
        Property.recheckWith size seed (PropertyConfig.coalesce config) property

    [<Extension>]
    static member ReportRecheck
        (   property : HProperty,
            size : Size,
            seed : Seed,
            [<Optional; DefaultParameterValue null>] ?config : Hedgehog.PropertyConfig
        ) : Report =
        Property.reportRecheckWith size seed (PropertyConfig.coalesce config) property

#endif
