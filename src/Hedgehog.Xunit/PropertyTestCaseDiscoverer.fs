namespace Hedgehog.Xunit

open System
open System.Collections.Generic
open System.Threading.Tasks
open Hedgehog
open Xunit.v3
open Xunit.Internal

type internal PropertyTestCaseDiscoverer() =

  interface IXunitTestCaseDiscoverer with
    override _.Discover(discoveryOptions, testMethod, attribute) =

      let struct (TestCaseDisplayName, Explicit, SkipExceptions, SkipReason,
                  SkipType, SkipUnless, SkipWhen, SourceFilePath,
                  SourceLineNumber, Timeout, UniqueID, ResolvedTestMethod) =
        TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, attribute, null, Nullable<int>(), null, null)

      let args = Array.create testMethod.Parameters.Count null

      let testCase =
        PropertyTestCase(
          ResolvedTestMethod,
          TestCaseDisplayName,
          UniqueID,
          Explicit,
          skipExceptions = Option.ofObj SkipExceptions,
          skipReason = Option.ofObj SkipReason,
          skipType = Option.ofObj SkipType,
          skipUnless = Option.ofObj SkipUnless,
          skipWhen = Option.ofObj SkipWhen,
          traits = testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase),
          testMethodArguments = args,
          sourceFilePath = None,
          sourceLineNumber = None,
          timeout = Timeout
        )

      let result = [ testCase :> IXunitTestCase ] :> IReadOnlyCollection<IXunitTestCase>
      ValueTask<IReadOnlyCollection<IXunitTestCase>>(result)
