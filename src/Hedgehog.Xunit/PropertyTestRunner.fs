namespace Hedgehog.Xunit

open Hedgehog
open System.Threading.Tasks
open Xunit.v3

type internal PropertyTestRunner() =
  inherit XunitTestRunner()

   static member val Instance = PropertyTestRunner() with get

   override this.InvokeTestMethod(ctx, testClassInstance) =
     // Process the Hedgehog report asynchronously and raise exceptions for failed tests
     let context = PropertyContext.fromMethod ctx.TestMethod
     task {
       let! report = InternalLogic.reportAsync context ctx.TestMethod testClassInstance |> Async.StartAsTask
       ReportFormatter.tryRaise report
     }
