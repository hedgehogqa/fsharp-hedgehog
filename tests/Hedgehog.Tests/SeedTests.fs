module Hedgehog.Tests.SeedTests

open Hedgehog
open TestHelpers

[<Tests>]
let seedTests = testList "Seed tests" [

    theory "Seed.from 'fixes' the γ-value"
        // https://github.com/hedgehogqa/haskell-hedgehog/commit/39b15b9b4d147f6001984c4b7edab00878269da7
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

        { Value = value
          Gamma = gamma }
        =! Seed.from x

]
