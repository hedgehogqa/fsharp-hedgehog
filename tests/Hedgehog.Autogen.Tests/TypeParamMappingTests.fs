module Hedgehog.Autogen.Tests.TypeParamMappingTests

open Xunit
open Swensen.Unquote
open Hedgehog

type Or<'A, 'B> = Left of 'A | Right of 'B

type And<'A> = And of 'A * 'A

type OutOfOrder<'a, 'b, 'c> = OutOfOrder of 'c * 'a * 'b * 'a

type Generators =
    static member OrSame<'A>(genA: Gen<'A>) : Gen<Or<'A, 'A>> =
        Gen.choice [
            genA |> Gen.map Left
            genA |> Gen.map Right
        ]

    static member AndSame<'A>(one: Gen<'A>, two: Gen<'A>) : Gen<And<'A>> =
        Gen.map2 (fun x y -> And(x, y)) one two

    static member OutOfOrderGen<'a, 'b, 'c>(genB: Gen<'b>, genC: Gen<'c>, genA: Gen<'a>, genA2: Gen<'a>) : Gen<OutOfOrder<'a, 'b, 'c>> =
        Gen.map4 (fun a a1 b c -> OutOfOrder(c, a, b, a1)) genA genA2 genB genC

[<Fact>]
let ``Should generate Or with same type for both parameters``() =
    property {
        let! i = Gen.auto<int>
        let config =
            AutoGenConfig.defaults
            |> AutoGenConfig.addGenerator (Gen.constant i)
            |> AutoGenConfig.addGenerators<Generators>

        let! result = Gen.autoWith<Or<int, int>> config

        match result with
        | Left x -> test <@ x = i @>
        | Right x -> test <@ x = i @>
    }
    |> Property.check

[<Fact>]
let ``Should generate And with same type for both parameters``() =
    property {
        let! i = Gen.auto<int>
        let config =
            AutoGenConfig.defaults
            |> AutoGenConfig.addGenerator (Gen.constant i)
            |> AutoGenConfig.addGenerators<Generators>

        let! result = Gen.autoWith<And<int>> config

        test <@ result = And(i, i) @>
    }
    |> Property.check

[<Fact>]
let ``Should generate OutOfOrder with parameters in different order``() =
    property {
        let! i, s, f = Gen.auto<int * string * float>
        let config =
            AutoGenConfig.defaults
            |> AutoGenConfig.addGenerator (Gen.constant i)
            |> AutoGenConfig.addGenerator (Gen.constant s)
            |> AutoGenConfig.addGenerator (Gen.constant f)
            |> AutoGenConfig.addGenerators<Generators>

        let! result = Gen.autoWith<OutOfOrder<int, string, float>> config

        test <@ result = OutOfOrder(f, i, s, i) @>
    }
    |> Property.check
