namespace Hedgehog

type PropertyConfig = internal {
    TestLimit : int<tests>
    ShrinkLimit : int<shrinks> option
}

module PropertyConfig =

    /// The default configuration for a property test.
    let defaultConfig : PropertyConfig =
        { TestLimit = 100<tests>
          ShrinkLimit = None }

    /// Set the number of times a property is allowed to shrink before the test
    /// runner gives up and prints the counterexample.
    let withShrinks (shrinkLimit : int<shrinks>) (config : PropertyConfig) : PropertyConfig =
        { config with ShrinkLimit = Some shrinkLimit }

    /// Restores the default shrinking behavior.
    let withoutShrinks (config : PropertyConfig) : PropertyConfig =
        { config with ShrinkLimit = None }

    /// Set the number of times a property should be executed before it is
    /// considered successful.
    let withTests (testLimit : int<tests>) (config : PropertyConfig) : PropertyConfig =
        { config with TestLimit = testLimit }
