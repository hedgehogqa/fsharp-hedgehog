namespace Hedgehog

type SeedConfig =
    internal
    | FixedSeed of Seed
    | RandomSeed

type IPropertyConfig = internal {
    TestLimit : int<tests>
    ShrinkLimit : int<shrinks> option
    SeedConfig : SeedConfig
}

[<RequireQualifiedAccess>]
module SeedConfig =

    let init (seedConfig : SeedConfig) : Seed =
        match seedConfig with
        | FixedSeed seed -> seed
        | RandomSeed -> Seed.random ()

module PropertyConfig =

    /// The default configuration for a property test.
    [<CompiledName("Default")>]
    let defaults: IPropertyConfig =
        { TestLimit = 100<tests>
          ShrinkLimit = None
          SeedConfig = FixedSeed (Seed.from 0UL) }

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

    /// Set the seed to a random value for each run.
    let withRandomSeed (config : IPropertyConfig) : IPropertyConfig =
        { config with SeedConfig = RandomSeed }

    /// Set the seed to a fixed value for all runs.
    let withSeed (seed : Seed) (config : IPropertyConfig) : IPropertyConfig =
        { config with SeedConfig = FixedSeed seed }
