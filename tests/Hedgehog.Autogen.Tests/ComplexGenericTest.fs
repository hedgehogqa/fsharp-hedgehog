module Hedgehog.AutoGen.Tests.ComplexGenericTest

open Xunit
open Swensen.Unquote
open Hedgehog
open Hedgehog.FSharp

// A type with complex generic parameter repetition: <A, A, B, C, A, A, D>.
type ComplexType<'A, 'B, 'C, 'D, 'E, 'F, 'G> = {
    First: 'A
    Second: 'B
    Third: 'C
    Fifth: 'E
    Fourth: 'D
    Sixth: 'F
    Seventh: 'G
}

type ComplexGenerators =
    // Method with pattern: method has <A, B, C, D> but type uses <A, A, B, C, A, A, D>.
    static member Complex<'A, 'B, 'C, 'D>(
        genA: Gen<'A>,
        genC: Gen<'C>,
        genB: Gen<'B>,
        genD: Gen<'D>) : Gen<ComplexType<'A, 'A, 'B, 'C, 'A, 'A, 'D>> =
        gen {
            let! a = genA
            let! b = genB
            let! c = genC
            let! d = genD
            return {
                First = a
                Second = a
                Third = b
                Fourth = c
                Fifth = a
                Sixth = a
                Seventh = d
            }
        }

[<Fact>]
let ``Should handle complex generic parameter repetition pattern``() =
    let config =
        AutoGenConfig.defaults
        |> AutoGenConfig.addGenerators<ComplexGenerators>

    // Generate ComplexType<int, int, string, bool, int, int, float>.
    // Method is Complex<int, string, bool, float>.
    let gen = Gen.autoWith<ComplexType<int, int, string, bool, int, int, float>> config
    let sample = Gen.sample 0 1 gen |> Seq.head

    // Verify the structure is correct.
    test <@ sample.First = sample.Second @>  // Both should be the same 'A value.
    test <@ sample.First = sample.Fifth @>   // All 'A positions should be the same.
    test <@ sample.Second = sample.Sixth @>
    test <@ sample.Third.GetType() = typeof<string> @>
    test <@ sample.Fourth.GetType() = typeof<bool> @>
    test <@ sample.Seventh.GetType() = typeof<float> @>

// Better test with specific verifiable values.
type VerifiableGenerators =
    static member VerifiableComplex<'A, 'B, 'C, 'D>(
        genA: Gen<'A>,
        genC: Gen<'C>,
        genB: Gen<'B>,
        genD: Gen<'D>) : Gen<ComplexType<'A, 'A, 'B, 'C, 'A, 'A, 'D>> =
        gen {
            let! a = genA
            let! b = genB
            let! c = genC
            let! d = genD
            return {
                First = a
                Second = a
                Third = b
                Fourth = c
                Fifth = a
                Sixth = a
                Seventh = d
            }
        }

// Specific constant generators to verify correct parameter mapping.
type SpecificGenerators =
    static member Int() = Gen.constant 42
    static member String() = Gen.constant "test"
    static member Bool() = Gen.constant true
    static member Float() = Gen.constant 3.14

[<Fact>]
let ``Should map parameters correctly with swapped parameter order``() =
    let config =
        AutoGenConfig.defaults
        |> AutoGenConfig.addGenerators<SpecificGenerators>
        |> AutoGenConfig.addGenerators<VerifiableGenerators>

    let gen = Gen.autoWith<ComplexType<int, int, string, bool, int, int, float>> config
    let sample = Gen.sample 0 1 gen |> Seq.head

    // With swapped parameters (genA, genC, genB, genD), the mapping should be:
    // 'A -> int (42) goes to positions: First, Second, Fifth, Sixth
    // 'B -> string ("test") goes to position: Third
    // 'C -> bool (true) goes to position: Fourth
    // 'D -> float (3.14) goes to position: Seventh

    test <@ sample.First = 42 @>
    test <@ sample.Second = 42 @>
    test <@ sample.Third = "test" @>
    test <@ sample.Fourth = true @>
    test <@ sample.Fifth = 42 @>
    test <@ sample.Sixth = 42 @>
    test <@ sample.Seventh = 3.14 @>
