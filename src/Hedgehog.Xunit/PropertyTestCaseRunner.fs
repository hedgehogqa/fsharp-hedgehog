namespace Hedgehog.Xunit

open Hedgehog
open Hedgehog.Xunit
open Xunit.v3

type internal PropertyTestCaseRunner() =
  inherit XunitTestCaseRunner()

  static member val Instance = PropertyTestCaseRunner() with get

  member private this.BaseRun(ctx) = base.Run(ctx)

  override this.RunTest(ctx, test: IXunitTest) =
    PropertyTestRunner.Instance.Run(
      test,
      ctx.MessageBus,
      ctx.ConstructorArguments,
      ctx.ExplicitOption,
      ctx.Aggregator.Clone(),
      ctx.CancellationTokenSource,
      ctx.BeforeAfterTestAttributes
    )
