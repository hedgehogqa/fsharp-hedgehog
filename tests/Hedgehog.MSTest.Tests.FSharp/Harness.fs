namespace Hedgehog.MSTest.Tests

open System
open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open Hedgehog
open Hedgehog.MSTest
open Microsoft.VisualStudio.TestTools.UnitTesting

/// <summary>
/// Runs properties outside the MSTest pipeline through the adapter's entry point
/// <see cref="M:InternalLogic.executeAsync(System.Reflection.MethodInfo,System.Object[],Microsoft.FSharp.Core.FSharpFunc{System.Object[],System.Threading.Tasks.Task{Microsoft.VisualStudio.TestTools.UnitTesting.TestResult}},Microsoft.FSharp.Core.FSharpFunc{Microsoft.FSharp.Core.Unit,System.Threading.CancellationToken})"/>,
/// so that a property that fails by design is checked through its result instead of failing the suite. The targets
/// live in classes without <see cref="T:Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute"/>, which
/// MSTest does not run.
/// </summary>
module internal Harness =

    /// <summary>
    /// Invokes a test method for one case the way <see cref="T:Microsoft.VisualStudio.TestTools.UnitTesting.ITestMethod"/>
    /// does: on a new instance of the class, awaiting the returned task, with the thrown exception in the result and
    /// <see cref="T:Microsoft.VisualStudio.TestTools.UnitTesting.AssertInconclusiveException"/> as an inconclusive result.
    /// </summary>
    let invoker (testMethod: MethodInfo) : obj array -> Task<TestResult> =
        fun arguments ->
            task {
                let instance =
                    if testMethod.IsStatic then null else Activator.CreateInstance testMethod.ReflectedType

                try
                    try
                        match testMethod.Invoke(instance, arguments) with
                        | :? Task as pending -> do! pending
                        | :? ValueTask as pending -> do! pending
                        | _ -> ()

                        return TestResult(Outcome = UnitTestOutcome.Passed)
                    with error ->
                        let error =
                            match error with
                            | :? TargetInvocationException as e when not (isNull e.InnerException) -> e.InnerException
                            | e -> e

                        let outcome =
                            match error with
                            | :? AssertInconclusiveException -> UnitTestOutcome.Inconclusive
                            | _ -> UnitTestOutcome.Failed

                        return TestResult(Outcome = outcome, TestFailureException = error)
                finally
                    InternalLogic.dispose instance
            }

    let private noCancellation () = CancellationToken.None

    /// The public method of the class with the given name.
    let methodOf<'Class> (name: string) : MethodInfo =
        match typeof<'Class>.GetMethod name with
        | null -> invalidArg (nameof name) $"%s{typeof<'Class>.Name} has no public method %s{name}."
        | testMethod -> testMethod

    /// The context that the attributes of the method and its class give.
    let contextOf<'Class> (name: string) = PropertyContext.fromMethod (methodOf<'Class> name)

    /// Runs the method as PropertyAttribute does and returns its one result.
    let executeAsync<'Class> (name: string) : Task<TestResult> =
        let testMethod = methodOf<'Class> name
        InternalLogic.executeAsync testMethod [||] (invoker testMethod) noCancellation

    /// Runs the method with the given context, with a data row and a cancellation token of the caller's choice.
    let runWithAsync
        (context: PropertyContext)
        (testMethod: MethodInfo)
        (invoke: obj array -> Task<TestResult>)
        (cancellationToken: unit -> CancellationToken)
        : Task<InternalLogic.PropertyRun> =
        InternalLogic.runAsync context testMethod [||] invoke cancellationToken

    /// Runs the method with the context its attributes give and returns the run with its report.
    let runAsync<'Class> (name: string) : Task<InternalLogic.PropertyRun> =
        let testMethod = methodOf<'Class> name
        runWithAsync (PropertyContext.fromMethod testMethod) testMethod (invoker testMethod) noCancellation

    /// The parameters that the journal of a failed report names, as name=value.
    let parametersOf (report: Report) : string array =
        match report.Status with
        | Failed failure ->
            failure.Journal
            |> Journal.eval
            |> Seq.choose (function
                | TestParameter(name, value) -> Some $"%s{name}=%O{value}"
                | _ -> None)
            |> Seq.toArray
        | status -> raise (AssertFailedException $"The property must fail, but its status is %A{status}.")

    /// The message of the exception of a result, or an empty string.
    let messageOf (result: TestResult) =
        match result.TestFailureException with
        | null -> ""
        | error -> error.Message

/// <summary>
/// Runs a property that fails by design through the real MSTest pipeline and turns the expected failure into a passed
/// result, so that the suite pins what <see cref="T:Hedgehog.MSTest.PropertyAttribute"/> reports through MSTest itself:
/// exactly one result, its outcome, its message, its output and the type of its inner exception. Any other result fails
/// the test and shows the property's own result.
/// </summary>
type FailingPropertyAttribute
    /// <summary>
    /// Expects the property to fail.
    /// </summary>
    /// <param name="callerFilePath">The file that declares the test method; the compiler fills it in.</param>
    /// <param name="callerLineNumber">The line that declares the test method; the compiler fills it in.</param>
    (
        [<CallerFilePath; Optional; DefaultParameterValue("")>] callerFilePath: string,
        [<CallerLineNumber; Optional; DefaultParameterValue(-1)>] callerLineNumber: int
    ) =
    inherit PropertyAttribute(callerFilePath, callerLineNumber)

    /// The expected outcome; Failed unless set.
    member val Outcome = UnitTestOutcome.Failed with get, set

    /// Texts that the failure message contains.
    member val MessageContains: string array = [||] with get, set

    /// Texts that the output of the result contains: its standard output or its TestContext messages.
    member val OutputContains: string array = [||] with get, set

    /// The type of the inner exception of the failure, or null for no check.
    member val InnerException: Type = null with get, set

    /// Runs the property and checks its result.
    override this.ExecuteAsync(testMethod: ITestMethod) : Task<TestResult array> =
        // base cannot be used inside the task expression, which is a closure
        let run = base.ExecuteAsync testMethod

        task {
            let! results = run

            let describe () =
                results
                |> Seq.map (fun result ->
                    $"Outcome: %O{result.Outcome}%s{Environment.NewLine}%s{Harness.messageOf result}%s{Environment.NewLine}%s{result.LogOutput}%s{result.TestContextMessages}")
                |> String.concat Environment.NewLine

            let verdict =
                try
                    Assert.HasCount(1, results, "A property must report exactly one result.")
                    let result = results[0]
                    Assert.AreEqual(this.Outcome, result.Outcome, "The property must end with the expected outcome.")
                    Assert.IsNotNull(result.TestFailureException, "A property that does not pass must report an exception.")
                    let message = result.TestFailureException.Message

                    for expected in this.MessageContains do
                        Assert.Contains(expected, message, StringComparison.Ordinal, "The failure message must contain the expected text.")

                    // TestContext.WriteLine goes to the TestContext messages, Console output to the standard output
                    let output = result.LogOutput + Environment.NewLine + result.TestContextMessages

                    for expected in this.OutputContains do
                        Assert.Contains(expected, output, StringComparison.Ordinal, "The output must contain the expected text.")

                    match this.InnerException with
                    | null -> ()
                    | expectedType ->
                        Assert.IsInstanceOfType(
                            result.TestFailureException.InnerException,
                            expectedType,
                            "The failure must carry the exception of the counterexample as its inner exception."
                        )

                    TestResult(Outcome = UnitTestOutcome.Passed, LogOutput = "The property failed as expected." + Environment.NewLine + describe ())
                with :? AssertFailedException as error ->
                    TestResult(Outcome = UnitTestOutcome.Failed, TestFailureException = error, LogOutput = describe ())

            return [| verdict |]
        }
