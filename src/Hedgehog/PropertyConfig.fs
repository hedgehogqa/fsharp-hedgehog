namespace Hedgehog

type IPropertyConfig = internal {
    TestLimit : int<tests>
    ShrinkLimit : int<shrinks> option
}

module PropertyConfig =

    /// The default configuration for a property test.
    [<CompiledName("Default")>]
    let defaults: IPropertyConfig =
        { TestLimit = 100<tests>
          ShrinkLimit = None }

    /// Set the number of times a property is allowed to shrink before the test
    /// runner gives up and displays the counterexample.
    let withShrinks (shrinkLimit : int<shrinks>) (config : IPropertyConfig) : IPropertyConfig =
        { config with ShrinkLimit = Some shrinkLimit }

    /// Restores the default shrinking behavior.
    let withoutShrinks (config : IPropertyConfig) : IPropertyConfig =
        { config with ShrinkLimit = None }

    /// Set the number of times a property should be executed before it is
    /// considered successful.
    let withTests (testLimit : int<tests>) (config : IPropertyConfig) : IPropertyConfig =
        { config with TestLimit = testLimit }
