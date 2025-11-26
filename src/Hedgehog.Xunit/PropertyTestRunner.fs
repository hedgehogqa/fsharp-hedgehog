namespace Hedgehog.Xunit

open Hedgehog
open Xunit.v3

type internal PropertyTestRunner() =
  inherit XunitTestRunner()

   static member val Instance = PropertyTestRunner() with get

   override this.InvokeTestMethod(ctx, testClassInstance) =
     // Process the Hedgehog report and raise exceptions for failed tests
     let context = PropertyContext.fromMethod ctx.TestMethod
     let report = InternalLogic.report context ctx.TestMethod testClassInstance
     ReportFormatter.tryRaise report
