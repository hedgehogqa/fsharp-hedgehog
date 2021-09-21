namespace Hedgehog

type PropertyConfig = internal {
    DiscardLimit : int<discards>
    ShrinkLimit : int<shrinks> option
    TestLimit : int<tests>
}

module PropertyConfig =

    /// The default configuration for a property test.
    let defaultConfig : PropertyConfig =
        { DiscardLimit = 100<discards>
          ShrinkLimit = None
          TestLimit = 100<tests> }

    /// Set the number of times a property is allowed to shrink before the test
    /// runner gives up and displays the counterexample.
    let withShrinks (shrinkLimit : int<shrinks>) (config : PropertyConfig) : PropertyConfig =
        { config with ShrinkLimit = Some shrinkLimit }

    /// Restores the default shrinking behavior.
    let withoutShrinks (config : PropertyConfig) : PropertyConfig =
        { config with ShrinkLimit = None }

    /// Set the number of times a property should be executed before it is
    /// considered successful.
    let withTests (testLimit : int<tests>) (config : PropertyConfig) : PropertyConfig =
        { config with TestLimit = testLimit }
