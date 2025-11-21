module Hedgehog.Autogen.Tests.AutoGenConfigTests

open Xunit
open Swensen.Unquote
open Hedgehog

[<Fact>]
let ``merging AutoGenConfig preserves set values``() =
  let expectedRange = Range.exponential 2 6
  let expectedDepth = 2
  let config1 =
    AutoGenConfig.defaults
    |> AutoGenConfig.setSeqRange expectedRange
    |> AutoGenConfig.setRecursionDepth expectedDepth
    |> AutoGenConfig.addGenerator (Gen.int32 (Range.exponentialBounded()))
  let config2 = AutoGenConfig.defaults |> AutoGenConfig.addGenerator Gen.bool
  let merged = AutoGenConfig.merge config1 config2
  test <@ AutoGenConfig.recursionDepth merged = expectedDepth @>

  let property = property {
    let! array = merged |> Gen.autoWith<(int * bool)[]>
    test <@ Array.length array >= 2 && Array.length array <= 6 @>
  }

  Property.check property

[<Fact>]
let ``merging AutoGenConfig overrides values``() =
  let previousRange = Range.exponential 10 15
  let expectedRange = Range.exponential 2 6
  let expectedDepth = 2
  let config1 = AutoGenConfig.defaults |> AutoGenConfig.setSeqRange previousRange |> AutoGenConfig.setRecursionDepth 1
  let config2 =
    AutoGenConfig.defaults
    |> AutoGenConfig.setSeqRange expectedRange
    |> AutoGenConfig.setRecursionDepth expectedDepth
    |> AutoGenConfig.addGenerator (Gen.int32 (Range.exponentialBounded()))

  let merged = AutoGenConfig.merge config1 config2
  test <@ AutoGenConfig.recursionDepth merged = expectedDepth @>

  let property = property {
    let! array = merged |> Gen.autoWith<int[]>
    test <@ Array.length array >= 2 && Array.length array <= 6 @>
  }

  Property.check property

type CustomType = { Value: int; Items: string list }

type CustomGenerators =
  // Generator that uses AutoGenConfig to access seqRange
  static member CustomTypeGen(config: IAutoGenConfig) : Gen<CustomType> = gen {
    let! value = Gen.int32 (Range.exponentialBounded())
    let! items = Gen.string (Range.linear 1 5) Gen.alpha |> Gen.list (AutoGenConfig.seqRange config)
    return { Value = value; Items = items }
  }

  // Generator that takes both AutoGenConfig and Gen<_> parameters
  static member CustomTypeWithGen(config: IAutoGenConfig, genValue: Gen<int>) : Gen<CustomType> = gen {
    let! value = genValue
    let! items = Gen.string (Range.linear 1 5) Gen.alpha |> Gen.list (AutoGenConfig.seqRange config)
    return { Value = value; Items = items }
  }

[<Fact>]
let ``addGenerators supports methods with AutoGenConfig parameter``() =
  let customConfig =
    AutoGenConfig.defaults
    |> AutoGenConfig.setSeqRange (Range.constant 3 3)
    |> AutoGenConfig.addGenerators<CustomGenerators>

  // Create the generator and sample a value to ensure it works
  let gen = customConfig |> Gen.autoWith<CustomType>
  let sample = Gen.sample 0 1 gen |> Seq.head
  test <@ sample.Items.Length = 3 @>

open System.Collections.Immutable

type ImmutableListGenerators =
  static member ImmutableListGen<'T>(config: IAutoGenConfig, genItem: Gen<'T>) : Gen<ImmutableList<'T>> =
    genItem |> Gen.list (AutoGenConfig.seqRange config) |> Gen.map ImmutableList.CreateRange


[<Fact>]
let ``addGenerators supports generic methods with AutoGenConfig and Gen parameters``() =
  let customConfig =
    AutoGenConfig.defaults
    |> AutoGenConfig.setSeqRange (Range.constant 5 5)
    |> AutoGenConfig.addGenerators<ImmutableListGenerators>

  // The ImmutableListGen<int> will be called with config and Gen<int>
  // This demonstrates that generic generators work with both AutoGenConfig and Gen<T> parameters
  let gen = customConfig |> Gen.autoWith<ImmutableList<int>>
  let sample = Gen.sample 0 1 gen |> Seq.head
  test <@ sample.Count = 5 @>

  // Also test with a different type to verify generics work
  let genString = customConfig |> Gen.autoWith<ImmutableList<string>>
  let sampleString = Gen.sample 0 1 genString |> Seq.head
  test <@ sampleString.Count = 5 @>
