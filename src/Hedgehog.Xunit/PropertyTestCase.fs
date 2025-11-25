namespace Hedgehog.Xunit

open System
open System.Collections.Generic
open System.ComponentModel
open System.Threading.Tasks
open Xunit.v3


type internal PropertyTestCase =
  inherit XunitTestCase

  new (
    testMethod: IXunitTestMethod,
    testCaseDisplayName: string,
    uniqueID: string,
    explicit: bool,
    skipExceptions: Type array option,
    skipReason: string option,
    skipType: Type option,
    skipUnless: string option,
    skipWhen: string option,
    traits: Dictionary<string, HashSet<string>>,
    testMethodArguments: obj array,
    sourceFilePath: string option,
    sourceLineNumber: int option,
    timeout: Nullable<int>
  ) =
    { inherit XunitTestCase(
        testMethod,
        testCaseDisplayName,
        uniqueID,
        explicit,
        skipExceptions |> Option.toObj,
        skipReason |> Option.toObj,
        skipType |> Option.toObj,
        skipUnless |> Option.toObj,
        skipWhen |> Option.toObj,
        traits,
        testMethodArguments,
        sourceFilePath |> Option.toObj,
        sourceLineNumber |> Option.toNullable,
        timeout
      )
    }

  [<EditorBrowsable(EditorBrowsableState.Never)>]
  [<Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")>]
  [<System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage>]
  new() =
    { inherit XunitTestCase() }

  interface ISelfExecutingXunitTestCase with
    member this.Run(explicitOption, messageBus, constructorArguments, aggregator, cancellationTokenSource) =
      task {
        let! tests = aggregator.RunAsync((fun _ -> this.CreateTests()), [])

        return! PropertyTestCaseRunner.Instance.Run(
          this,
          tests,
          messageBus,
          aggregator,
          cancellationTokenSource,
          this.TestCaseDisplayName,
          this.SkipReason,
          explicitOption,
          constructorArguments
        )
      }
      |> ValueTask<RunSummary>
