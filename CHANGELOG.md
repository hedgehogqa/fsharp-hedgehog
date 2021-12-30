## Version ?.?.?

- Add `Tree.apply`. Change `Gen.apply` from monadic to applicative. Revert runtime optimization of `Gen.integral`. ([#398][398], [@TysonMN][TysonMN])
- Change `ListGen.traverse` from monadic to applicative. ([#399][399], [@TysonMN][TysonMN])
- Fix bug in the `BindReturn` method of the `property` CE where the generated value is not added to the Journal. ([#401][401], [@TysonMN][TysonMN])
- Add `BindReturn` to the `gen` CE. This essentially changes the last call to `let!` to use `Gen.map` instead of `Gen.bind`. Add `MergeSources` to the `gen` and `property` CEs.  This change enables the `and!` syntax. ([#400][400], [@TysonMN][TysonMN])

## Version 0.12.0 (2021-12-12)

- Rename `Property.failOnFalse` to `Property.falseToFailure` ([#384][384], [@TysonMN][TysonMN])
- Add `BindReturn` to the `property` CE ([#364][364], [@TysonMN][TysonMN])
  - A breaking change.  Previously, returning a `bool` from a `property` CE (after using `let!`) caused the CE to have return type `Property<unit>`.  Now this results in a return type of `Property<bool>`.  The previous behavior can now be expressed by piping the `Property<bool>` instance into `Property.falseToFailure`.
- Change recheck API to accept recheck data encoded as `string` ([#385][385], [@TysonMN][TysonMN])
- Add `RecheckInfo` to simplify recheck reporting ([#386][386], [@TysonMN][TysonMN])
- Optimize rechecking by only executing the end of the `property` CE with the shrunken input ([#336][336], [@TysonMN][TysonMN])

## Version 0.11.1 (2021-11-19)

- Add Property.failOnFalse ([#380][380], [@TysonMN][TysonMN])
- Fix bug [#381][381] that prevents rendering of reports containing `None` ([#382][382], [@TysonMN][TysonMN])

## Version 0.11.0 (2021-09-22)

- Improved integral shrink trees to match behavior of binary search ([#239][239], [@TysonMN][TysonMN])
- Add render functions ([#274][274], [@adam-becker][adam-becker])
- fix link to tutorial in nuget package ([#305][305], [@ThisFunctionalTom][ThisFunctionalTom])
- made Seq module internal and moved to own file ([#307][307], [@TysonMN][TysonMN])
- Sort .editorconfig properties ([#308][308], [@adam-becker][adam-becker])
- Remove PropertyConfig.coalesce ([#309][309], [@adam-becker][adam-becker])
- Move property config to its own file. ([#311][311], [@adam-becker][adam-becker])
- Add property args structure. ([#312][312], [@adam-becker][adam-becker])
- Rename the parameters of Range.exponentialFrom (and friends) ([#315][315], [@dharmaturtle][dharmaturtle])
- Implement Property.select via bind to avoid bug ([#318][318], [@TysonMN][TysonMN])
  - simplify fix for #317 ([#356][356], [@TysonMN][TysonMN])
- Remove duplicates in frequency shrink tree ([#321][321], [@TysonMN][TysonMN])
- Special processing for printing ResizeArray<_> in Property.forAll #323 ([#324][324], [@altxt][altxt])
- Fix for counterexample crashing bug #327 ([#328][328], [@TysonMN][TysonMN])
- Refactors to simplify code mostly for takeSmallest ([#334][334], [@TysonMN][TysonMN])
- Remove extra sized call ([#337][337], [@adam-becker][adam-becker])
- Implement `Gen.mapN` variants with `Gen.apply` ([#338][338], [@adam-becker][adam-becker])
- allow tests to access internal scope ([#344][344], [@TysonMN][TysonMN])
- change access scope for two top-level modules from private to internal ([#345][345], [@TysonMN][TysonMN])
- improve function name takeSmallest to shrinkInput in Property ([#347][347], [@TysonMN][TysonMN])
- Add error handling to Property.map ([#348][348], [@TysonMN][TysonMN])
- Add contributing guidelines. ([#349][349], [@adam-becker][adam-becker])
- Update benchmark project ([#350][350], [@adam-becker][adam-becker])
- Deprecate gen functions ([#351][351], [@adam-becker][adam-becker])
- Remove 'Random.Builder' ([#352][352], [@adam-becker][adam-becker])
- make Gen.{sample, sampleTree} and Random.replicate lazy ([#354][354], [@TysonMN][TysonMN])
- Better C# example in README.md. ([#355][355], [@adam-becker][adam-becker])
- remove Property.ofThrowing ([#357][357], [@TysonMN][TysonMN])
- Fixed import namespace in cshrp sample ([#358][358], [@lupin-de-mid][lupin-de-mid])
- Fix order of arguments to Expect.equal ([#361][361], [@TysonMN][TysonMN])
- Fix a few things in our Seed implementation ([#362][362], [@adam-becker][adam-becker])
- Utilize Property.set internally ([#363][363], [@TysonMN][TysonMN])

## Version 0.10.0 (2021-02-05)

- Add `PropertyConfig` ([#288][288], [@dharmaturtle][dharmaturtle])
- Add `OptionTree.traverse` ([#282][282], [@TysonMN][TysonMN])
- Use fsdocs ([#277][277], [@adam-becker][adam-becker])
- Rearrange parameters for better chaining ([#266][266], [@adam-becker][adam-becker])
  - Rearrange `Tree.bind` parameters ([#300][300], [@adam-becker][adam-becker])
- Add `ListGen.traverse` ([#260][260], [@TysonMN][TysonMN])
- Improve and extend DateTime/DateTimeOffset generation ([#252][252], [@cmeeren][cmeeren])
- Split Property.fs across multiple files ([#247][247], [@adam-becker][adam-becker])
- Add support for LINQ via Hedgehog.Linq namespace ([#244][244], [@adam-becker][adam-becker])
- Switch to Fable.Mocha ([#196][196], [@ThisFunctionalTom][ThisFunctionalTom])

## Version 0.9.0 (2020-12-17)

- Add Gen.single and Gen.decimal ([#250][250], [@cmeeren][cmeeren])
- Add Property.recheck ([#233][233], [@adam-becker][adam-becker])
- Remove additional space in Tree.render ([#240][240], [@TysonMN][TysonMN])
- Improve Range documentation ([#237][237], [#235][235], [@TysonMN][TysonMN])
- Improve Gen.list ([#231][231], [@adam-becker][adam-becker])
- Add Tree.render ([#229][229], [@TysonMN][TysonMN])
- Improve Gen.frequency documentation ([#230][230], [@TysonMN][TysonMN])
- Mangle compiled name for functions that cannot be called from langs other than F# ([#219][219], [#255][255], [@mausch][mausch] / [@dharmaturtle][dharmaturtle])
  - Explicitly instantiate static inline functions for C# callers ([#222][222], [@mausch][mausch])
- Add missing internal conversions to bigint in Numeric.fs ([#221][221], [@mausch][mausch])
- Add LINQ support for Range ([#220][220], [@mausch][mausch])

## Version 0.8.4 (2020-07-26)

- Add target .NET Framework 4.5 ([#210][210], [@TysonMN][TysonMN])
- Add target .NET Standard 2.0 ([#209][209], [@TysonMN][TysonMN])

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

[altxt]:
  https://github.com/altxt
[lupin-de-mid]:
  https://github.com/lupin-de-mid
[ThisFunctionalTom]:
  https://github.com/ThisFunctionalTom
[dharmaturtle]:
  https://github.com/dharmaturtle
[cmeeren]:
  https://github.com/cmeeren
[adam-becker]:
  https://github.com/adam-becker
[TysonMN]:
  https://github.com/TysonMN
[mausch]:
  https://github.com/mausch
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

[401]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/401
[400]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/400
[399]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/399
[398]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/398
[386]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/386
[385]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/385
[384]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/384
[382]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/382
[381]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/381
[380]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/380
[364]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/364
[363]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/363
[362]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/362
[361]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/361
[358]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/358
[357]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/357
[356]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/356
[355]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/355
[354]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/354
[352]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/352
[351]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/351
[350]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/350
[349]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/349
[348]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/348
[347]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/347
[345]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/345
[344]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/344
[338]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/338
[337]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/337
[336]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/336
[334]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/334
[328]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/328
[324]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/324
[321]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/321
[318]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/318
[315]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/315
[312]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/312
[311]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/311
[309]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/309
[308]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/308
[307]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/307
[305]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/305
[300]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/300
[288]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/288
[282]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/282
[277]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/277
[274]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/274
[269]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/269
[266]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/266
[260]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/260
[255]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/255
[252]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/252
[250]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/250
[247]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/247
[244]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/244
[240]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/240
[239]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/239
[237]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/237
[235]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/235
[233]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/233
[231]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/231
[230]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/230
[229]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/229
[222]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/222
[221]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/221
[220]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/220
[219]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/219
[210]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/210
[209]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/209
[196]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/196
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
