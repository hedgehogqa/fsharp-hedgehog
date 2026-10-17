module Hedgehog.Tests.SeedTests

open Hedgehog
open TestDsl

let seedTests = testList "Seed tests" [
    // https://github.com/hedgehogqa/haskell-hedgehog/commit/39b15b9b4d147f6001984c4b7edab00878269da7
    yield! testCases "Seed.from 'fixes' the γ-value"
        [ (0x61c8864680b583ebUL, 15210016002011668638UL, 12297829382473034411UL)
          (0xf8364607e9c949bdUL, 11409286845259996466UL, 12297829382473034411UL)
          (0x88e48f4fcc823718UL,  1931727433621677744UL, 12297829382473034411UL)
          (0x7f83ab8da2e71dd1UL,   307741759840609752UL, 12297829382473034411UL)
          (0x7957d809e827ff4cUL,  8606169619657412120UL, 12297829382473034413UL)
          (0xf8d059aee4c53639UL, 13651108307767328632UL, 12297829382473034413UL)
          (0x9cd9f015db4e58b7UL,   125750466559701114UL, 12297829382473034413UL)
          (0xf4077b0dbebc73c0UL,  6781260234005250507UL, 12297829382473034413UL)
          (0x305cb877109d0686UL, 15306535823716590088UL, 12297829382473034405UL)
          (0x359e58eeafebd527UL,  7344074043290227165UL, 12297829382473034405UL)
          (0xbeb721c511b0da6dUL,  9920554987610416076UL, 12297829382473034405UL)
          (0x86466fd0fcc363a6UL,  3341781972484278810UL, 12297829382473034405UL)
          (0xefee3e7b93db3075UL, 12360157267739240775UL, 12297829382473034421UL)
          (0x79629ee76aa83059UL,   600595566262245170UL, 12297829382473034421UL)
          (0x05d507d05e785673UL,  1471112649570176389UL, 12297829382473034421UL)
          (0x76442b62dddf926cUL,  8100917074368564322UL, 12297829382473034421UL) ] <| fun (x, value, gamma) ->

        let expected =
          { Value = value
            Gamma = gamma }
        Seed.from x =! expected

    yield! testCases "Seed.bitCount is 1 for powers of 2"
        [ 0 .. 63 ] <| fun shift ->
        let value = 1UL <<< shift
        1UL =! Seed.bitCount value

    // The values were recorded with the implementation that took the next seed twice and mixed the two.
    yield! testCases "Seed.split gives the same two seeds as it always did"
        [ (0UL, (14141672759607663454UL, 16294208416658607535UL), (10697439907134545880UL, 15321243935405834545UL))
          (1UL, (15450470250919199918UL, 10451216379200822465UL), (16204969531660614133UL, 5610259966137620355UL))
          (42UL, (18209985878117922550UL, 13679457532755275413UL), (1275548033995301424UL, 10514482549683702313UL))
          (123456789UL, (15273134408196679032UL, 2466975172287755897UL), (4292043419236741472UL, 13924482691834310481UL)) ] <| fun (x, (value1, gamma1), (value2, gamma2)) ->

        let seed1, seed2 = Seed.split (Seed.from x)
        seed1 =! { Value = value1; Gamma = gamma1 }
        seed2 =! { Value = value2; Gamma = gamma2 }

    testCase "Seed.split gives the same seeds five levels down as it always did" <| fun _ ->
        let rec leaves depth (seed : Seed) =
            if depth = 0 then
                [ seed ]
            else
                let seed1, seed2 = Seed.split seed
                leaves (depth - 1) seed1 @ leaves (depth - 1) seed2
        let actual = leaves 5 (Seed.from 7UL)
        List.length actual =! 32
        // The first leaf follows the first seed of every split, the second leaf the second seed of the last one.
        actual.[0] =! { Value = 6484616903009328195UL; Gamma = 7191089600892374487UL }
        actual.[1] =! { Value = 5113354820410970958UL; Gamma = 17709024770181158815UL }

    testCase "the first seed of Seed.split continues the sequence of the seed" <| fun _ ->
        // It is the seed after the next two numbers of the sequence.
        let seed = Seed.from 99UL
        let _, afterOne = Seed.nextUInt64 seed
        let _, afterTwo = Seed.nextUInt64 afterOne
        fst (Seed.split seed) =! afterTwo
]
