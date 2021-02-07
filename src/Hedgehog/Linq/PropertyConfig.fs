namespace Hedgehog.Linq

#if !FABLE_COMPILER

open System.Runtime.CompilerServices
open Hedgehog


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

#endif
