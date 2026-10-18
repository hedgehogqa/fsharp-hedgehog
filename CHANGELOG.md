## Unreleased

Changes on `master` since the latest versions on NuGet: `Hedgehog` 2.0.4, `Hedgehog.Xunit.v3` 2.0.0 and `Hedgehog.NUnit` 1.0.0.

- Add `Hedgehog.MSTest`, the adapter for MSTest 4, published as 1.0.0 on 2026-10-13 ([#487][487], [@xperiandri][xperiandri])
  - It has the same attributes and settings as `Hedgehog.Xunit.v3` and `Hedgehog.NUnit`: `Property`, `Properties`, `Recheck`, `GenAttribute<'a>`, the generator attributes, `AutoGenConfig`, `AutoGenConfigArgs`, `Tests`, `Shrinks` and `Size`.
  - It adds the `Seed` setting of `Property` and `Properties`, data rows (`DataRow` and `DynamicData`), `Assert.Inconclusive` to discard a case, and finding a `GenAttribute` anywhere in an inheritance chain.
  - MSTest 4 runs only test methods that return `void`, `Task` or `ValueTask`, so a property fails by throwing. Results like `bool`, `Property<_>` and `Result` are not handled.
- Add `Gen.dateOnly` and `Gen.timeOnly` (`Gen.DateOnly` and `Gen.TimeOnly` in LINQ) and default generators for `DateOnly` and `TimeOnly`. Previously, `Gen.auto` threw an exception for parameters of these types. ([#488][488], [@xperiandri][xperiandri])
  - Add the `DateOnly` and `TimeOnly` attributes to `Hedgehog.Xunit.v3` and `Hedgehog.NUnit`.

## Version 2.0.4 (2026-06-29)

- Add a generator of `DateTimeKind` as a parameter of `Gen.dateTime` ([#480][480], [@njlr][njlr])
  - Add `Gen.dateTimeKind`, `Gen.dateTimeUtc`, `Gen.dateTimeLocal` and `Gen.dateTimeUnspecified`. The LINQ `Gen.DateTime` gains a generator of `DateTimeKind` too.
  - A breaking change for F# and C#. `Gen.dateTime` takes a second argument now; pass `Gen.constant DateTimeKind.Unspecified` to keep the previous behavior. C# callers must change `Gen.DateTime(range)` to `Gen.DateTime(range, null)` to keep the previous behavior.
- Fix a typo in the comment of `JournalLine.Text` ([#481][481], [@njlr][njlr])
- Make the seed part of the property config ([#482][482], [@njlr][njlr])
  - Add `SeedConfig`, `PropertyConfig.withSeed` to fix the seed of all runs and `PropertyConfig.withRandomSeed` to draw a random seed for each run, which is the default.
  - The seed of the config is the starting seed of a run, and so it is in the recheck data.
- Add SourceLink to the `Hedgehog` package by embedding its sources in the PDB. Change the `rollForward` policy in `global.json` so that the repository can be opened with a later .NET SDK. ([#485][485], [@jwosty][jwosty])

## Version 2.0.3 (2026-01-13)

- Add `Hedgehog.NUnit`, the adapter for NUnit. It is published as 1.0.0 and has the same attributes as `Hedgehog.Xunit.v3`: `Property`, `Properties`, `Recheck`, `GenAttribute<'a>` and the generator attributes. ([#478][478], [@AlexeyRaga][AlexeyRaga])
- Declare the license of the `Hedgehog` package as an SPDX expression instead of a file. The license file is still included in the package. ([#479][479], [@kchenery][kchenery])

## Version 2.0.2 (2026-01-09)

- Add `Property.ofAsyncWithJournal` to create a property from an async computation that produces a journal and an outcome ([#477][477], [@AlexeyRaga][AlexeyRaga])
- Remove `Hedgehog.Stateful` from the repository ([#477][477], [@AlexeyRaga][AlexeyRaga])
  - The last version that was published from this repository is 0.7.0-beta3.
  - Before that, `Hedgehog.Stateful` was released as 0.6.0-beta1, 0.7.0-beta1, 0.7.0-beta2 and 0.7.0-beta3 with these changes:
    - Fix bugs (0.6.0-beta1) ([#472][472], [@AlexeyRaga][AlexeyRaga])
    - Allow to project the state with `Var.map` and show the resolved values of `Var`s in reports (0.6.0-beta1) ([#473][473], [@AlexeyRaga][AlexeyRaga])
    - Simplify `Var` and `Env`: a `Var` holds its concrete value instead of looking it up in the `Env`, so that the state can be printed without reflection (0.6.0-beta1) ([#474][474], [@AlexeyRaga][AlexeyRaga])
    - Allow commands to take symbolic variables as input: `Generate` can return a `Var<T>`, `Require` checks that it can be resolved, and `Resolve`, `ResolveOr` and `TryResolve` resolve it (0.7.0-beta1) ([#475][475], [@AlexeyRaga][AlexeyRaga])
    - Add custom equality and comparison for `Var<'T>` (0.7.0-beta2) ([5a6f144][5a6f144], [@AlexeyRaga][AlexeyRaga])
    - Add the C# extension method `ToPropertyWith` (0.7.0-beta3) ([a9fac48][a9fac48], [@AlexeyRaga][AlexeyRaga])

## Version 2.0.1 (2025-12-15)

- Add `Hedgehog.Stateful`, a library for state machine property-based testing, published as 0.5.0-beta1 ([#471][471], [@AlexeyRaga][AlexeyRaga])
- Add the missing extension methods `Check`, `CheckAsync`, `Recheck`, `RecheckAsync`, `Render`, `Report`, `ReportAsync`, `ReportRecheck` and `ReportRecheckAsync` for `Property<unit>` to the LINQ API ([#471][471], [@AlexeyRaga][AlexeyRaga])
- Add the chatmode of GitHub Copilot for writing C# tests with `Hedgehog.Xunit.v3` to the documentation ([aa3ced9][aa3ced9], [@AlexeyRaga][AlexeyRaga])

## Version 2.0.0 (2025-12-08)

- Use a different seed for each property. Previously, all properties of a test run used the same random seed, so different properties generated the same values. ([#468][468], [@AlexeyRaga][AlexeyRaga])
- Add `Property.ignoreResult` (`IgnoreResult` in LINQ) to discard the result of a property, for example, the result of a fluent assertion ([#469][469], [@AlexeyRaga][AlexeyRaga])
- Add tagged journal entries, so that writing to the journal is not concerned with turning values into strings and the report formatter knows the meaning of each entry ([#470][470], [@AlexeyRaga][AlexeyRaga])
  - A breaking change. `Journal` holds entries of the new type `JournalLine` (`TestParameter`, `GeneratedValue`, `Counterexample`, `Exception`, `Cancellation` and `Text`) instead of strings.

## Version 1.1.0 (2025-12-03)

- Support async operations in properties ([#464][464], [@AlexeyRaga][AlexeyRaga])
  - `Async<'T>` and `Task<'T>` can be bound with `let!` in the `property` CE; LINQ queries can bind `Task<'T>` and `Task` directly.
  - Add `Property.ofAsync`, `Property.ofTask` and `Property.ofTaskUnit`, and `Property.reportAsync`, `Property.checkAsync` and their variants to run a property asynchronously.
- Revise the README ([#466][466], [@moodmosaic][moodmosaic])
- Fix small signature issues: move the date and time generators to the `Hedgehog.FSharp` namespace like the other F# generators, and make the LINQ `Sequence` and `Traverse` take `seq<_>` instead of a flexible type ([#467][467], [@AlexeyRaga][AlexeyRaga])

## Version 1.0.2 (2025-12-01)

- Unwrap `TargetInvocationException` and `AggregateException` with a single inner exception in failure reports, so that they show the exception that was actually thrown ([8cc514d][8cc514d], [@AlexeyRaga][AlexeyRaga])

## Version 1.0.1 (2025-11-30)

- Fix stack overflow in `Shrink.elems` for large lists ([#463][463], [@moodmosaic][moodmosaic])
- Add missing LINQ combinators, such as `Sequence`, `Traverse`, `WithMapTo` and `WithDistinctMapTo`, and document the existing `TryFinally` and `TryWith` combinators ([#465][465], [@AlexeyRaga][AlexeyRaga])

## Version 1.0.0 (2025-11-27)

Highlights:

- .NET 8 is the baseline now
- `Hedgehog.Experimental` is merged in, so `Hedgehog` provides `Gen.auto` and `Gen.autoWith`
- `Hedgehog.Xunit` is added to the repository. It uses xUnit v3 and reports failures better. It is published as `Hedgehog.Xunit.v3` with the same versions as `Hedgehog` up to 2.0.0.
- The names of generators and combinators are unified between C# and F# (except the ones that are required for LINQ), and the support of C# is polished
- Performance optimizations and generated documentation

The pre-releases 1.0.0-beta1 (2025-11-25), 1.0.0-beta2 (2025-11-26), 1.0.0-beta3 and 1.0.0-rc1 (2025-11-27) are included.

Detailed changes:

- No longer targeting .NET Framework ([#417][417], [@LyndonGingerich][LyndonGingerich])
- Fix the introductory examples in the documentation so that they compile ([#421][421], [@bmitc][bmitc])
- The runtime of `Property.recheck` is now about the same as `Property.check`.  Previously, it was about ten times slower. ([#433][433], [@TysonMN][TysonMN])
- Update dependencies ([#435][435], [@dharmaturtle][dharmaturtle])
- The error message given when a deadend is reached during rechecking is improved to say that the cause is a change in generators. ([#436][436], [@TysonMN][TysonMN])
- The implementation of `Property.falseToFailure` is now better.  As a result, `Property.recheckBool` now only tests the shrunken input. ([#437][437], [@TysonMN][TysonMN])
- Fix the conversion of `bigint` to `decimal`, which went through `int32` ([#441][441], [@njlr][njlr])
- Add `Gen.timeSpan` ([#442][442], [@njlr][njlr])
- Add `Gen.bigint` ([#443][443], [@njlr][njlr])
- Use Vite and the new Fable compiler for the Fable tests ([#444][444], [@ThisFunctionalTom][ThisFunctionalTom])
- Work around a Fable bug with `decimal` ([#445][445], [@ThisFunctionalTom][ThisFunctionalTom])
- Fix the links in the table of contents of the website, which did not work because of a mismatch in case ([#446][446], [@arialdomartini][arialdomartini])
- Add XML documentation to `Hedgehog.Linq` ([#447][447], [@JohnEffo][JohnEffo])
- Pin the version of the `YoloDev.Expecto.TestSdk` package of the tests to 0.14.3, which supports .NET 6 ([#452][452], [@jwChung][jwChung])
- Clean up the C# API ([#453][453], [@AlexeyRaga][AlexeyRaga])
  - Deconflict the C# and F# `Gen` and `Range`.
  - Add `Gen.Constant` as the counterpart of the F# `Gen.constant` and make `Gen.FromValue` obsolete.
  - Make the C# extensions that receive collections receive `params`.
- Merge `Hedgehog.Experimental` into `Hedgehog` ([#454][454], [@AlexeyRaga][AlexeyRaga])
  - Add `Gen.auto` and `Gen.autoWith` for auto-generation.
  - Standardize the names of generators and combinators.
  - A breaking change. The API that is specific to F# is in the `Hedgehog.FSharp` namespace now, so that the API does not look clunky from C#. F# code needs `open Hedgehog.FSharp`.
- Make generating values about 2.5 times faster in most cases ([#456][456], [@AlexeyRaga][AlexeyRaga])
- Add documentation with examples in C# and F#, generated with DocFX and published from CI ([#457][457], [@AlexeyRaga][AlexeyRaga])
- Install .NET 8 in CI ([#458][458], [@AlexeyRaga][AlexeyRaga])
- Merge `Hedgehog.Xunit` and its tests into this repository ([#459][459], [@AlexeyRaga][AlexeyRaga])
- Remove the quick start link from the description of the `Hedgehog` package ([#460][460], [@moodmosaic][moodmosaic])
- Report the failures of `Hedgehog.Xunit.v3` tests better ([#461][461], [@AlexeyRaga][AlexeyRaga])
  - The report clearly specifies the names and values of the parameters, makes the recheck seed easy to find and shows the exception that caused the failure, without the noise of xUnit and Hedgehog internals and multiple stack traces.
- Enrich and polish ([#462][462], [@AlexeyRaga][AlexeyRaga])
  - Add the documentation of `Hedgehog.Xunit.v3`.
  - Add more basic generators and improve the parity of generators between F# and C#.
  - Add useful generator attributes for `Hedgehog.Xunit.v3`.

## Version 0.13.0 (2022-07-23)

- Fix bug in `Property.recheck` where the result is always `Failed`. ([#415][415], [@TysonMN][TysonMN])
- Runtime targets are now .NET Standard 2.0, .NET 4.8. and .NET 6.0. ([#416][416], [@LyndonGingerich][LyndonGingerich])

## Version 0.12.1 (2021-12-31)

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

[xperiandri]:
  https://github.com/xperiandri
[jwosty]:
  https://github.com/jwosty
[kchenery]:
  https://github.com/kchenery
[AlexeyRaga]:
  https://github.com/AlexeyRaga
[JohnEffo]:
  https://github.com/JohnEffo
[arialdomartini]:
  https://github.com/arialdomartini
[njlr]:
  https://github.com/njlr
[bmitc]:
  https://github.com/bmitc
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
[LyndonGingerich]:
  https://github.com/LyndonGingerich

[488]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/488
[487]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/487
[485]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/485
[482]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/482
[481]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/481
[480]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/480
[479]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/479
[478]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/478
[477]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/477
[475]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/475
[474]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/474
[473]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/473
[472]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/472
[471]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/471
[470]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/470
[469]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/469
[468]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/468
[467]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/467
[466]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/466
[465]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/465
[464]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/464
[463]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/463
[462]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/462
[461]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/461
[460]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/460
[459]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/459
[458]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/458
[457]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/457
[456]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/456
[454]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/454
[453]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/453
[452]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/452
[447]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/447
[446]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/446
[445]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/445
[444]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/444
[443]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/443
[442]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/442
[441]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/441
[437]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/437
[436]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/436
[435]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/435
[433]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/433
[421]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/421
[417]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/417
[416]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/416
[415]:
  https://github.com/hedgehogqa/fsharp-hedgehog/pull/415
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

[a9fac48]:
  https://github.com/hedgehogqa/fsharp-hedgehog/commit/a9fac4852ecdc32df9851c94eea708babaa50b2a
[5a6f144]:
  https://github.com/hedgehogqa/fsharp-hedgehog/commit/5a6f14462574d0e4514463312eb59ad1cf18f9b7
[aa3ced9]:
  https://github.com/hedgehogqa/fsharp-hedgehog/commit/aa3ced992ef8db3b6362c1ad4e484de4ab6228f2
[8cc514d]:
  https://github.com/hedgehogqa/fsharp-hedgehog/commit/8cc514d877e1d2739914de9e2a649095930900e9
