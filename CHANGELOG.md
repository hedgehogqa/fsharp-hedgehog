## Version 0.8.3 (2019-07-09)

- Improve Int64 value generation (avoid overflows) ([#186][186], [@marklam][marklam])

## Version 0.8.2 (2018-08-06)

- Improve UInt32 and UInt64 value generation (avoid overflows) ([#173][173], [@jwChung][jwChung])
- Improve DateTime value generation (include milliseconds) ([#165][165], [@jwChung][jwChung])

## Version 0.8.1 (2018-06-27)

- Unicode generator no longer generates noncharacters '\65534' and '\65535' ([#163][163], [@jwChung][jwChung])

## Version 0.8.0 (2018-06-04)

- Verify that Seed.from 'fixes' the γ-value ([#161][161], [@moodmosaic][moodmosaic])
- Sync F# version of Seed with Haskell ([#160][160], [@moodmosaic][moodmosaic])

## Version 0.7.0 (2018-02-12)

- Convert to .NET Standard ([#153][153], [@porges][porges])

## Version 0.6.0 (2017-11-07)

- Make Journal store delayed strings, so they are computed on-demand ([#147][147], [@porges][porges])

## Version 0.5.0 (2017-11-06)

- Support for LINQ queries ([#113][113], [@porges][porges])

## Version 0.4.4 (2017-10-31)

- Correct mixGamma oddness check ([#142][142], [@moodmosaic][moodmosaic])

## Version 0.4.3 (2017-10-12)

- Add ToBigInt overload for System.Double ([#136][136], [@moodmosaic][moodmosaic])

## Version 0.4.2 (2017-10-10)

- Increase size in Discard case ([#129][129], [@moodmosaic][moodmosaic])

## Version 0.4.1 (2017-10-10)

- XML-documentation improvements ([#122][122], [@moodmosaic][moodmosaic])
- Typo fix in doc/tutorial.md ([#121][121], [@frankshearar][frankshearar])

## Version 0.4 (2017-09-25)

- Render exceptions so they get added to the journal ([#119][119], [@moodmosaic][moodmosaic])
- Exclude FSharp.Core NuGet dependency ([#109][109], [@ploeh][ploeh])

## Version 0.3 (2017-07-24)

- Add Range exponential combinators ([#105][105], [@moodmosaic][moodmosaic])
- Change Gen.double and Gen.float so that they take a Range type ([#104][104], [@moodmosaic][moodmosaic])
- Shrink floating binary point types similar to the Haskell version ([#103][103], [@moodmosaic][moodmosaic])
- Add F# interactive examples to the Shrink module ([#102][102], [@moodmosaic][moodmosaic])
- Test F# interactive examples with Doctest ([#99][99], [@moodmosaic][moodmosaic])

## Version 0.2.1 (2017-05-16)

- Add ASCII, Latin-1, and Unicode character generators ([#96][96], [@moodmosaic][moodmosaic])

## Version 0.2.0 (2017-05-06)

- Add doc/tutorial, make smaller README ([#94][94], [@moodmosaic][moodmosaic])
- Modify Gen combinators so that they take a Range ([#92][92], [@moodmosaic][moodmosaic])
- Add Range type and combinators ([#91][91], [@moodmosaic][moodmosaic])
- Improve the Visual Studio dev experience ([#90][90], [@porges][porges])

## Version 0.1 (2017-05-06)

- First release of Hedgehog ([@jystic][jystic], [@moodmosaic][moodmosaic])

[frankshearar]:
  https://github.com/frankshearar
[jystic]:
  https://github.com/jystic
[jwChung]:
  https://github.com/jwChung
[marklam]:
  https://github.com/marklam
[moodmosaic]:
  https://github.com/moodmosaic
[ploeh]:
  https://github.com/ploeh
[porges]:
  https://github.com/porges

[186]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/186
[173]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/173
[165]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/165
[163]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/163
[161]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/161
[160]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/160
[153]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/153
[147]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/147
[142]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/142
[136]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/136
[129]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/129
[122]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/122
[121]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/121
[119]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/119
[113]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/113
[109]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/109
[105]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/105
[104]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/104
[103]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/103
[102]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/102
[99]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/99
[96]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/96
[94]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/94
[92]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/92
[91]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/91
[90]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/90
