namespace Hedgehog.Linq

#if !FABLE_COMPILER

open System.Runtime.CompilerServices
open Hedgehog

[<AbstractClass; Sealed>]
type PropertyConfigExtensions private () =

    /// Set the number of times a property is allowed to shrink before the test
    /// runner gives up and displays the counterexample.
    [<Extension>]
    static member WithShrinks (config : IPropertyConfig, shrinkLimit: int<shrinks>) : IPropertyConfig =
        PropertyConfig.withShrinks shrinkLimit config

    /// Restores the default shrinking behavior.
    [<Extension>]
    static member WithoutShrinks (config : IPropertyConfig) : IPropertyConfig =
        PropertyConfig.withoutShrinks config

    /// Set the number of times a property should be executed before it is
    /// considered successful.
    [<Extension>]
    static member WithTests (config : IPropertyConfig, testLimit: int<tests>) : IPropertyConfig =
        PropertyConfig.withTests testLimit config

#endif
