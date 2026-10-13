// Tests of what the MSTest adapter adds to Hedgehog.NUnit and Hedgehog.Xunit or does differently: one result per
// property, the lifecycle of every case, failure reports and their replay, Seed, give-ups, Assert.Inconclusive, rejected
// methods, Task and ValueTask bodies, data rows, settings of inherited properties, generator attributes found through
// the inheritance chain, memoised invocations, cancellation, timeouts and invocations that MSTest could not run. Each
// pins MSTest behaviour that the adapter relies on, so the suite shows what an MSTest update changes.

namespace Hedgehog.MSTest.Tests

open System
open System.Threading
open System.Threading.Tasks
open Hedgehog
open Hedgehog.MSTest
open Microsoft.VisualStudio.TestTools.UnitTesting

/// The function under test of the failure report tests: it loses the last item of a list of three or more items.
module Buggy =
    let reverse (xs: int list) =
        let reversed = List.rev xs
        if reversed.Length >= 3 then List.truncate (reversed.Length - 1) reversed else reversed

/// The recheck data that the seed-42 run of the buggy reverse property reports.
module BuggyRecheck =
    [<Literal>]
    let Data = "33_7178138842248401496_3704456211830240771_4-4-4"

/// Records what the lifecycle test class does, one instance number per case.
module LifecycleRecord =
    let mutable constructed = 0
    let mutable lastBodyInstance = 0
    let mutable lastCleanedUp = 0
    let mutable lastDisposed = 0

/// MSTest runs the constructor, the TestContext injection, TestInitialize, TestCleanup and Dispose for every case.
[<TestClass>]
type ``Lifecycle per case tests``() =
    let instance = Interlocked.Increment &LifecycleRecord.constructed
    let mutable initialized = false
    let mutable bodies = 0

    member val TestContext: TestContext = null with get, set

    [<TestInitialize>]
    member _.Initialize() = initialized <- true

    [<TestCleanup>]
    member _.Cleanup() = LifecycleRecord.lastCleanedUp <- instance

    interface IDisposable with
        member _.Dispose() = LifecycleRecord.lastDisposed <- instance

    [<Property(30<tests>)>]
    member this.``every case gets its own instance, TestContext, TestInitialize, TestCleanup and Dispose`` (_: int) =
        bodies <- bodies + 1
        Assert.AreEqual(1, bodies, "An instance runs a single case.")
        Assert.IsTrue(initialized, "TestInitialize runs before every case.")
        Assert.IsNotNull(this.TestContext, "MSTest injects the TestContext for every case.")

        Assert.AreEqual(
            "every case gets its own instance, TestContext, TestInitialize, TestCleanup and Dispose",
            this.TestContext.TestName,
            "The TestContext names the property."
        )

        match LifecycleRecord.lastBodyInstance with
        | 0 -> ()
        | previous ->
            Assert.AreNotEqual(previous, instance, "Every case runs on a new instance.")
            Assert.AreEqual(previous, LifecycleRecord.lastCleanedUp, "TestCleanup ran after the previous case.")
            Assert.AreEqual(previous, LifecycleRecord.lastDisposed, "Dispose ran after the previous case.")

        LifecycleRecord.lastBodyInstance <- instance

/// MSTest passes the TestContext to the constructor of every case as well.
[<TestClass>]
type ``TestContext constructor injection tests``(testContext: TestContext) =

    [<Property(5<tests>)>]
    member _.``MSTest passes the TestContext to the constructor of every case`` (_: bool) =
        Assert.IsNotNull(testContext, "MSTest passes the TestContext to the constructor.")

/// A failed property reports Hedgehog's report, its counterexample, recheck data ready to paste, the exception of the
/// counterexample and the output of its invocation, and RecheckAttribute replays it.
[<TestClass>]
type ``Failure report tests``() =
    static let mutable fixedRuns = 0

    member val TestContext: TestContext = null with get, set

    [<FailingProperty(Seed = 42UL,
                      MessageContains =
                          [| "*** Failed! Falsifiable"
                             "You can reproduce this failure with the following Recheck Seed:"
                             "i = 50"
                             "Reproduce by adding next to the Property attribute: [<Recheck(\"" |],
                      OutputContains = [| "Hedgehog: "; "invocation output for 50" |],
                      InnerException = typeof<AssertFailedException>)>]
    member this.``a failed property reports its counterexample, its recheck data and the output of its invocation`` (i: int) =
        this.TestContext.WriteLine $"invocation output for %d{i}"
        Assert.IsLessThan(50, i, "fails by design from 50")

    [<FailingProperty(Seed = 42UL,
                      MessageContains = [| "Reproduce by adding next to the Property attribute: [<Recheck(\"" + BuggyRecheck.Data + "\")>]" |])>]
    member _.``the buggy reverse reports the recheck data that the tests below replay`` (xs: int list) =
        CollectionAssert.AreEqual(List.toArray xs, List.toArray (Buggy.reverse (Buggy.reverse xs)), "Reversing twice must give the list back.")

    [<FailingProperty(MessageContains = [| "after 1 test" |], InnerException = typeof<AssertFailedException>)>]
    [<Recheck(BuggyRecheck.Data)>]
    member _.``Recheck replays the counterexample`` (xs: int list) =
        CollectionAssert.AreEqual(List.toArray xs, List.toArray (Buggy.reverse (Buggy.reverse xs)), "Reversing twice must give the list back.")

    [<Property>]
    [<Recheck(BuggyRecheck.Data)>]
    member _.``Recheck passes with a single case once the bug is fixed`` (xs: int list) =
        Assert.AreEqual(1, Interlocked.Increment &fixedRuns, "A recheck replays exactly one case.")
        CollectionAssert.AreEqual(List.toArray xs, List.toArray (List.rev (List.rev xs)), "Reversing twice must give the list back.")

    [<FailingProperty(MessageContains = [| "row = 7"; "i = 10" |])>]
    [<DataRow(7)>]
    member _.``a failed data row names its row values among the parameters`` (row: int, i: int) =
        Assert.IsLessThan(10, i, "fails by design from 10")

    [<FailingProperty(Outcome = UnitTestOutcome.Error, MessageContains = [| "Hedgehog could not run the property"; "RecheckData" |])>]
    [<Recheck("not-recheck-data")>]
    member _.``malformed recheck data gives an error result`` (i: int) = Assert.AreEqual(i, i, "never reached")

    [<FailingProperty(MessageContains = [| "Gave up after 100 discards" |])>]
    member _.``a property that gives up fails`` (n: int) : unit = Assert.Inconclusive $"never applicable (%d{n})"

/// Counts the instances of CancelledCaseArgument and their disposals.
module CancelledCaseCounters =
    let mutable created = 0
    let mutable disposed = 0

/// A generated argument of the cancellation tests, which counts its instances and their disposals.
type CancelledCaseArgument() =
    do Interlocked.Increment &CancelledCaseCounters.created |> ignore

    interface IDisposable with
        member _.Dispose() = Interlocked.Increment &CancelledCaseCounters.disposed |> ignore

/// Targets of the harness tests, which fail by design or cannot run.
type HarnessTargets() =

    [<Property(Tests = 5<tests>)>]
    member _.``takes a disposable argument`` (_: CancelledCaseArgument) = ()

    [<Property(Seed = 42UL)>]
    member _.``buggy reverse`` (xs: int list) =
        CollectionAssert.AreEqual(List.toArray xs, List.toArray (Buggy.reverse (Buggy.reverse xs)), "Reversing twice must give the list back.")

    [<Property(Seed = 42UL, Tests = 20<tests>)>]
    member _.``seed 42`` (_: int, _: string) = ()

    [<Property(Seed = 7UL, Tests = 20<tests>)>]
    member _.``seed 7`` (_: int, _: string) = ()

    [<Property(Seed = 2026UL)>]
    member _.``fails from 50`` (i: int) : unit =
        if i >= 50 then
            failwith "fails by design"

    [<Property>]
    member _.``always inconclusive`` (n: int) : unit = Assert.Inconclusive $"never applicable (%d{n})"

    [<Property(Tests = 20<tests>)>]
    member _.``odd values are discarded`` (n: int) : unit =
        if n % 2 <> 0 then
            Assert.Inconclusive "odd values do not apply"

    [<Property>]
    [<Recheck("1_9056294896546497174_14632957226901407867_")>]
    member _.``inconclusive in a recheck`` (_: int) : unit = Assert.Inconclusive "a recheck cannot discard its case"

    [<Property>]
    member _.``fails for any value`` (_: int) : unit = Assert.Fail "fails by design"

    [<Property>]
    member _.``generic property`` (value: 'a) : unit = ignore value

    [<Property>]
    member _.``returns bool`` (_: int) = true

    [<Property>]
    member _.``returns Task of unit`` (_: int) : Task<unit> = task { return () }

    [<Property>]
    member _.``returns Async`` (_: int) = async { return () }

    [<Property>]
    member _.``returns Result`` (_: int) : Result<unit, string> = Ok()

/// Targets of the caller information tests, one for each constructor of PropertyAttribute.
type CallerInformationTargets() =

    [<Property>]
    member _.``no arguments`` () = ()

    [<Property(5<tests>)>]
    member _.``tests`` () = ()

    [<Property(5<tests>, 0<shrinks>)>]
    member _.``tests and shrinks`` () = ()

    [<Property(typeof<Int13>)>]
    member _.``AutoGenConfig`` () = ()

    [<Property(typeof<Int13>, 5<tests>)>]
    member _.``AutoGenConfig and tests`` () = ()

    [<Property(typeof<Int13>, 5<tests>, 0<shrinks>)>]
    member _.``AutoGenConfig, tests and shrinks`` () = ()

/// PropertyAttribute passes the caller information of each of its constructors on to TestMethodAttribute, which records
/// where the test method is declared.
[<TestClass>]
type ``Caller information tests``() =

    [<TestMethod>]
    [<DataRow("no arguments")>]
    [<DataRow("tests")>]
    [<DataRow("tests and shrinks")>]
    [<DataRow("AutoGenConfig")>]
    [<DataRow("AutoGenConfig and tests")>]
    [<DataRow("AutoGenConfig, tests and shrinks")>]
    member _.``a property records the file and the line that declare it`` (name: string) =
        let attribute =
            Harness.methodOf<CallerInformationTargets> name
            |> fun testMethod -> testMethod.GetCustomAttributes(typeof<PropertyAttribute>, false)
            |> Seq.exactlyOne
            :?> PropertyAttribute

        StringAssert.EndsWith(attribute.DeclaringFilePath, "MSTestTests.fs", StringComparison.Ordinal, "The attribute records the declaring file.")
        Assert.IsTrue(attribute.DeclaringLineNumber.HasValue, "The attribute records the declaring line.")
        Assert.IsGreaterThan(0, attribute.DeclaringLineNumber.Value, "The declaring line is a line of the file.")

/// The behaviour of the runner, driven through the adapter's entry point with an invoker of the suite's own.
[<TestClass>]
type ``Runner tests``() =

    /// Runs a target with an invoker that records the arguments of every invocation.
    let recordAsync name : Task<struct (InternalLogic.PropertyRun * string array)> =
        task {
            let testMethod = Harness.methodOf<HarnessTargets> name
            let invoke = Harness.invoker testMethod
            let recorded = ResizeArray<string>()

            let recording (arguments: obj array) =
                recorded.Add(arguments |> Array.map (fun argument -> $"%A{argument}") |> String.concat ", ")
                invoke arguments

            let! run = Harness.runWithAsync (PropertyContext.fromMethod testMethod) testMethod recording (fun () -> CancellationToken.None)
            return struct (run, recorded.ToArray())
        }

    [<TestMethod>]
    member _.``the same Seed generates the same cases, another Seed others`` () : Task =
        task {
            let! struct (_, first) = recordAsync "seed 42"
            let! struct (_, second) = recordAsync "seed 42"
            let! struct (_, other) = recordAsync "seed 7"
            Assert.HasCount(20, first, "The property runs its 20 cases.")
            CollectionAssert.AreEqual(first, second, "The same seed generates the same cases.")
            CollectionAssert.AreNotEqual(first, other, "Another seed generates other cases.")
        }

    [<TestMethod>]
    member _.``the recheck data of a failed run replays its counterexample in one invocation`` () : Task =
        task {
            let! run = Harness.runAsync<HarnessTargets> "buggy reverse"

            let recheckData =
                match run.Report.Status with
                | Failed { RecheckInfo = Some info } -> RecheckData.serialize info.Data
                | status -> raise (AssertFailedException $"The property must fail with recheck data, but its status is %A{status}.")

            Assert.AreEqual(BuggyRecheck.Data, recheckData, "The seed fixes the recheck data that the failure report tests replay.")
            let testMethod = Harness.methodOf<HarnessTargets> "buggy reverse"
            let context = { PropertyContext.fromMethod testMethod with Recheck = Some recheckData }
            let! replay = Harness.runWithAsync context testMethod (Harness.invoker testMethod) (fun () -> CancellationToken.None)
            Assert.AreEqual(1, replay.Invocations, "A recheck replays exactly one case.")

            CollectionAssert.AreEqual(
                Harness.parametersOf run.Report,
                Harness.parametersOf replay.Report,
                "The recheck replays the counterexample of the run."
            )
        }

    [<TestMethod>]
    member _.``the counterexample is invoked once`` () : Task =
        task {
            let! struct (run, recorded) = recordAsync "fails from 50"
            CollectionAssert.AreEqual([| "i=50" |], Harness.parametersOf run.Report, "The property shrinks to 50.")
            let invocationsOfCounterexample = recorded |> Array.filter (fun arguments -> arguments = "50")
            Assert.HasCount(1, invocationsOfCounterexample, "Hedgehog must not invoke the counterexample again to read its journal.")
            Assert.HasCount(run.Invocations, recorded, "The run counts every invocation.")
        }

    [<TestMethod>]
    member _.``a property that gives up fails`` () : Task =
        task {
            let! result = Harness.executeAsync<HarnessTargets> "always inconclusive"
            Assert.AreEqual(UnitTestOutcome.Failed, result.Outcome, "A property that gives up fails, as in Hedgehog.NUnit and Hedgehog.Xunit.")
            Assert.Contains("Gave up after 100 discards", Harness.messageOf result, StringComparison.Ordinal, "The message is Hedgehog's report.")
        }

    [<TestMethod>]
    member _.``Assert.Inconclusive discards the case`` () : Task =
        task {
            let! run = Harness.runAsync<HarnessTargets> "odd values are discarded"
            Assert.AreEqual(Status.OK, run.Report.Status, "Discarded cases do not fail the property.")
            Assert.AreEqual(20<tests>, run.Report.Tests, "The property runs its 20 cases that apply.")
            Assert.IsGreaterThan(0<discards>, run.Report.Discards, "The odd values are discarded.")
        }

    [<TestMethod>]
    member _.``Assert.Inconclusive in a recheck fails`` () : Task =
        task {
            let! result = Harness.executeAsync<HarnessTargets> "inconclusive in a recheck"
            Assert.AreEqual(UnitTestOutcome.Failed, result.Outcome, "A recheck cannot discard its only case.")

            Assert.IsInstanceOfType<AssertInconclusiveException>(
                result.TestFailureException.InnerException,
                "The failure carries the exception of Assert.Inconclusive."
            )
            |> ignore
        }

    [<TestMethod>]
    member _.``a cancelled token stops the run without shrinking`` () : Task =
        task {
            use cancellation = new CancellationTokenSource()
            // Fails from 50 only, so its first failing case has smaller values to shrink to
            let testMethod = Harness.methodOf<HarnessTargets> "fails from 50"
            let invoke = Harness.invoker testMethod

            // Cancels the token when an invocation fails, as a [<Timeout>] does when it ends one
            let cancelling (arguments: obj array) =
                task {
                    let! result = invoke arguments

                    if result.Outcome <> UnitTestOutcome.Passed then
                        cancellation.Cancel()

                    return result
                }

            let! run = Harness.runWithAsync (PropertyContext.fromMethod testMethod) testMethod cancelling (fun () -> cancellation.Token)
            Assert.IsTrue(run.Cancelled, "The run notices the cancelled token.")

            match run.Report.Status with
            | Failed failure -> Assert.AreEqual(0<shrinks>, failure.Shrinks, "No shrink step runs once the token is cancelled.")
            | status -> Assert.Fail $"The property must fail, but its status is %A{status}."

            Assert.AreEqual(int run.Report.Tests, run.Invocations, "Only the generated cases are invoked, no shrink step.")
            let result = InternalLogic.toTestResult run TimeSpan.Zero
            Assert.AreEqual(UnitTestOutcome.Failed, result.Outcome, "A stopped property fails.")

            Assert.Contains(
                $"stopped after %d{run.Invocations} invocations without shrinking",
                Harness.messageOf result,
                StringComparison.Ordinal,
                "The message explains why the property stopped."
            )
        }

    [<TestMethod>]
    [<DataRow(3, DisplayName = "an earlier case")>]
    [<DataRow(20, DisplayName = "the last case")>]
    member _.``a token cancelled during a passing invocation stops the run`` (cancelledAt: int) : Task =
        task {
            use cancellation = new CancellationTokenSource()
            // Runs 20 cases
            let testMethod = Harness.methodOf<HarnessTargets> "seed 42"
            let invoked = ResizeArray<obj array>()

            // Passes every case and cancels the token while one of them runs, as a cooperative [<Timeout>] does for a
            // test method that returns without observing its token: MSTest reports such an invocation as passed
            let cancelling (arguments: obj array) =
                task {
                    invoked.Add arguments

                    if invoked.Count = cancelledAt then
                        cancellation.Cancel()

                    return TestResult(Outcome = UnitTestOutcome.Passed)
                }

            let! run = Harness.runWithAsync (PropertyContext.fromMethod testMethod) testMethod cancelling (fun () -> cancellation.Token)
            Assert.IsTrue(run.Cancelled, "The run notices the token that was cancelled during a passing invocation.")
            Assert.HasCount(cancelledAt, invoked, "No case runs after the one whose token was cancelled.")
            let result = InternalLogic.toTestResult run TimeSpan.Zero
            Assert.AreEqual(UnitTestOutcome.Failed, result.Outcome, "A stopped property fails, wherever the cancellation reached it.")

            Assert.Contains(
                $"stopped after %d{cancelledAt} invocations without shrinking",
                Harness.messageOf result,
                StringComparison.Ordinal,
                "The message explains why the property stopped."
            )
        }

    [<TestMethod>]
    member _.``a case that a cancelled token keeps from running still disposes its arguments`` () : Task =
        task {
            use cancellation = new CancellationTokenSource()
            cancellation.Cancel()
            let testMethod = Harness.methodOf<HarnessTargets> "takes a disposable argument"

            let! run =
                Harness.runWithAsync (PropertyContext.fromMethod testMethod) testMethod (Harness.invoker testMethod) (fun () -> cancellation.Token)

            Assert.IsTrue(run.Cancelled, "The run notices the cancelled token.")
            Assert.AreEqual(0, run.Invocations, "No case runs once the token is cancelled.")
            Assert.AreNotEqual(0, CancelledCaseCounters.created, "Arguments are generated for the cases that do not run.")
            Assert.AreEqual(CancelledCaseCounters.created, CancelledCaseCounters.disposed, "Every generated argument is disposed.")
        }

    [<TestMethod>]
    [<DataRow(UnitTestOutcome.Error)>]
    [<DataRow(UnitTestOutcome.NotFound)>]
    member _.``an invocation that MSTest could not run stops the run and gives its result to the property`` (outcome: UnitTestOutcome) : Task =
        task {
            let testMethod = Harness.methodOf<HarnessTargets> "seed 42"
            let error: exn = InvalidOperationException "The runner fails by design."
            let invoked = ResizeArray<obj array>()

            // The third invocation ends as MSTest ends one whose test method it could not run; the earlier ones pass
            let failingAtThird (arguments: obj array) =
                task {
                    invoked.Add arguments

                    if invoked.Count = 3 then
                        return TestResult(Outcome = outcome, TestFailureException = error, LogOutput = "output of the third invocation")
                    else
                        return TestResult(Outcome = UnitTestOutcome.Passed)
                }

            let! run = Harness.runWithAsync (PropertyContext.fromMethod testMethod) testMethod failingAtThird (fun () -> CancellationToken.None)
            Assert.HasCount(3, invoked, "Neither a further case nor a shrink step is invoked after the one that MSTest could not run.")
            Assert.AreEqual(3, run.Invocations, "The run counts the invocations that happened.")
            Assert.IsTrue(run.RunnerFailure.IsSome, "The run keeps the result of the invocation that MSTest could not run.")
            Assert.IsFalse(run.Cancelled, "The run did not stop because of the cancellation token.")
            let result = InternalLogic.toTestResult run TimeSpan.Zero
            Assert.AreEqual(outcome, result.Outcome, "The property ends with the outcome of that invocation instead of Failed.")
            Assert.AreSame(error, result.TestFailureException, "The property reports the exception of that invocation as it is.")

            Assert.Contains(
                "stopped after 3 invocations without shrinking",
                result.LogOutput,
                StringComparison.Ordinal,
                "The output explains why the property stopped."
            )

            Assert.Contains(
                "output of the third invocation",
                result.LogOutput,
                StringComparison.Ordinal,
                "The output of that invocation is kept."
            )
        }

    [<TestMethod>]
    member _.``an exception of the invoker stops the run with an error result`` () : Task =
        task {
            let testMethod = Harness.methodOf<HarnessTargets> "seed 42"
            let error: exn = InvalidOperationException "The invoker fails by design."
            let invoked = ResizeArray<obj array>()

            // MSTest reports what a test method throws in the result, so an exception of InvokeAsync comes from MSTest
            let throwing (arguments: obj array) : Task<TestResult> =
                invoked.Add arguments
                raise error

            let! run = Harness.runWithAsync (PropertyContext.fromMethod testMethod) testMethod throwing (fun () -> CancellationToken.None)
            Assert.HasCount(1, invoked, "Neither a further case nor a shrink step is invoked after the exception.")
            let result = InternalLogic.toTestResult run TimeSpan.Zero
            Assert.AreEqual(UnitTestOutcome.Error, result.Outcome, "An exception of the invoker is an error, not a failed case.")
            Assert.AreSame(error, result.TestFailureException, "The property reports the exception of the invoker as it is.")
        }

    [<TestMethod>]
    member _.``a generic property method gives an error result`` () : Task =
        task {
            let! result = Harness.executeAsync<HarnessTargets> "generic property"
            Assert.AreEqual(UnitTestOutcome.Error, result.Outcome, "A generic method cannot run as a property.")
            Assert.Contains("is generic", Harness.messageOf result, StringComparison.Ordinal, "The message names the rule.")
        }

    [<TestMethod>]
    [<DataRow("returns bool", "return unit, Task or ValueTask")>]
    [<DataRow("returns Task of unit", "without a result")>]
    [<DataRow("returns Async", "instead of an Async")>]
    [<DataRow("returns Result", "instead of returning Error")>]
    member _.``a return type that MSTest cannot run gives an error result`` (name: string, advice: string) : Task =
        task {
            let! result = Harness.executeAsync<HarnessTargets> name
            Assert.AreEqual(UnitTestOutcome.Error, result.Outcome, "MSTest cannot run such a method.")
            Assert.Contains(advice, Harness.messageOf result, StringComparison.Ordinal, "The message tells what to return instead.")
        }

/// MSTest reports Assert.Inconclusive as an inconclusive invocation, which the adapter turns into a discarded case.
[<TestClass>]
type ``Inconclusive tests``() =

    [<Property(20<tests>)>]
    member _.``odd values are discarded`` (n: int) =
        if n % 2 <> 0 then
            Assert.Inconclusive "odd values do not apply"

        Assert.AreEqual(0, n % 2, "Only even values get past the precondition.")

/// Task and ValueTask bodies run through MSTest, which awaits them; the shrinking tests cover their failures.
[<TestClass>]
type ``Task and ValueTask tests``() =

    [<Property(20<tests>)>]
    member _.``a Task property runs`` (i: int) : Task =
        task {
            do! Task.Yield()
            Assert.AreEqual(i, i + 0, "Adding zero keeps the value.")
        }

    [<Property(20<tests>)>]
    member _.``a ValueTask property runs`` (i: int) : ValueTask =
        Assert.AreEqual(i, i * 1, "Multiplying by one keeps the value.")
        ValueTask.CompletedTask

/// The rows of the data row tests.
module DataRows =
    let labels: obj array seq = seq { [| box "first" |]; [| box "second" |] }

    let typed: TestDataRow<struct (string * int)> seq =
        seq {
            TestDataRow(struct ("one", 1))
            TestDataRow(struct ("two", 2))
        }

/// The values of an MSTest data row are the leading arguments, and the parameters after them are generated.
[<TestClass>]
type ``Data row tests``() =

    static member Labels = DataRows.labels

    static member Typed = DataRows.typed

    [<Property(10<tests>)>]
    [<DataRow(3)>]
    [<DataRow(5)>]
    member _.``DataRow values are the leading arguments`` (fixedValue: int, _: int list) =
        Assert.IsTrue((fixedValue = 3 || fixedValue = 5), "The leading argument comes from the data row.")

    [<Property(10<tests>)>]
    [<DynamicData(nameof ``Data row tests``.Labels)>]
    member _.``DynamicData values are the leading arguments`` (label: string, _: bool) =
        Assert.IsTrue((label = "first" || label = "second"), "The leading argument comes from the data row.")

    [<Property(10<tests>)>]
    [<DynamicData(nameof ``Data row tests``.Typed)>]
    member _.``a TestDataRow of a struct tuple fills two leading parameters`` (name: string, count: int, _: int) =
        Assert.IsTrue((name = "one" && count = 1) || (name = "two" && count = 2), "The two leading arguments come from the row.")

/// The base of two test classes whose class-level settings differ; the property is declared here once.
[<AbstractClass>]
type InheritedPropertySuite() =
    abstract Expected: int

    [<Property(5<tests>)>]
    member this.``an inherited property gets the settings of the class that MSTest runs`` (i: int) =
        Assert.AreEqual(this.Expected, i, "The config of the derived test class sets every int.")

/// Runs the inherited property with Int13.
[<TestClass>]
[<Properties(typeof<Int13>)>]
type ``Inherited property with Int13``() =
    inherit InheritedPropertySuite()
    override _.Expected = 13

/// Runs the inherited property with Int2718.
[<TestClass>]
[<Properties(typeof<Int2718>)>]
type ``Inherited property with Int2718``() =
    inherit InheritedPropertySuite()
    override _.Expected = 2718

/// The base class of the settings precedence tests.
[<Properties(Tests = 3<tests>, AutoGenConfig = typeof<Int13A>, Seed = 1UL)>]
type SettingsBase() =

    [<Property>]
    member _.``declared in the base class`` (i: int, s: string) =
        Assert.AreEqual(2718, i, "The derived class's config wins for ints.")
        Assert.AreEqual("A", s, "The base class's config still sets every string.")

/// The derived class of the settings precedence tests.
[<Properties(Tests = 7<tests>, AutoGenConfig = typeof<Int2718>)>]
type SettingsDerived() =
    inherit SettingsBase()

/// A derived class's Properties win over its base class's, and the configurations merge.
[<TestClass>]
type ``Class-level settings precedence tests``() =

    [<TestMethod>]
    member _.``a base class alone sets its own settings`` () =
        let context = Harness.contextOf<SettingsBase> "declared in the base class"
        Assert.AreEqual(Some 3<tests>, context.Tests, "The base class sets the number of tests.")
        Assert.AreEqual(Some 1UL, context.Seed, "The base class sets the seed.")

    [<TestMethod>]
    member _.``a derived class's settings win over its base class's`` () =
        let context = Harness.contextOf<SettingsDerived> "declared in the base class"
        Assert.AreEqual(Some 7<tests>, context.Tests, "The derived class's number of tests wins.")
        Assert.AreEqual(Some 1UL, context.Seed, "A setting the derived class leaves out comes from the base class.")

    [<TestMethod>]
    member _.``a derived class's configuration merges over its base class's`` () : Task =
        task {
            let! run = Harness.runAsync<SettingsDerived> "declared in the base class"
            Assert.AreEqual(Status.OK, run.Report.Status, "The merged configuration generates 2718 and \"A\".")
            Assert.AreEqual(7<tests>, run.Report.Tests, "The derived class's number of tests applies.")
        }

/// A generator attribute derived from the built-in IntAttribute, which Hedgehog.NUnit and Hedgehog.Xunit overlook.
type OneToThreeAttribute() =
    inherit IntAttribute(1, 3)

/// A GenAttribute is found wherever it sits in the inheritance chain.
[<TestClass>]
type ``GenAttribute inheritance tests``() =

    [<Property>]
    member _.``an attribute derived from a built-in one sets the value`` ([<OneToThree>] i: int) =
        Assert.IsInRange(1, 3, i, "The derived attribute bounds the value.")

/// A [<Timeout>] applies to each invocation, and a timed-out invocation stops the property without shrinking. The
/// TestCleanup method makes MSTest replace the token source of the TestContext before it, which the adapter must survive.
[<TestClass>]
type ``Timeout tests``() =

    member val TestContext: TestContext = null with get, set

    [<TestCleanup>]
    member _.Cleanup() = ()

    [<Property(12<tests>)>]
    [<Timeout(1000, CooperativeCancellation = true)>]
    member this.``a timeout applies to each invocation, not to the property`` (_: int) : Task =
        Task.Delay(100, this.TestContext.CancellationToken)

    [<FailingProperty(Tests = 5<tests>, MessageContains = [| "stopped after 1 invocations without shrinking" |])>]
    [<Timeout(200, CooperativeCancellation = true)>]
    member this.``a timed-out invocation stops the property without shrinking`` (_: int) : Task =
        Task.Delay(TimeSpan.FromSeconds 30.0, this.TestContext.CancellationToken)
