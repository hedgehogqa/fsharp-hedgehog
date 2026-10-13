namespace Hedgehog.MSTest

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading.Tasks
open Hedgehog
open Microsoft.VisualStudio.TestTools.UnitTesting

/// <summary>
/// Marks an MSTest test method as a Hedgehog property: Hedgehog generates its arguments, MSTest invokes it once for
/// every generated case and every shrink step, and the property reports a single
/// <see cref="T:Microsoft.VisualStudio.TestTools.UnitTesting.TestResult"/>.
/// <para>
/// A parameter gets its value from the <see cref="T:Hedgehog.MSTest.GenAttribute`1"/> on it or, without one, from
/// <see cref="M:Hedgehog.FSharp.AutoGenExtensions.Gen.autoWith``1(Hedgehog.IAutoGenConfig)"/> with the
/// <see cref="P:Hedgehog.MSTest.PropertyAttribute.AutoGenConfig"/> of this attribute merged over those of the
/// <see cref="T:Hedgehog.MSTest.PropertiesAttribute"/> of the test class and its base classes. The values of a
/// <see cref="T:Microsoft.VisualStudio.TestTools.UnitTesting.DataRowAttribute"/> or
/// <see cref="T:Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute"/> row are the leading arguments, and
/// only the parameters after them are generated. A setting of this attribute wins over the same setting of
/// <see cref="T:Hedgehog.MSTest.PropertiesAttribute"/>, and <see cref="T:Hedgehog.MSTest.RecheckAttribute"/> replays a
/// reported failure.
/// </para>
/// <para>
/// The method is a non-generic instance member that returns <see cref="T:Microsoft.FSharp.Core.Unit"/>,
/// <see cref="T:System.Threading.Tasks.Task"/> or <see cref="T:System.Threading.Tasks.ValueTask"/> and fails by
/// throwing, such as through an <see cref="T:Microsoft.VisualStudio.TestTools.UnitTesting.Assert"/> call; MSTest does not
/// discover a test method that returns anything else. MSTest creates the test class, injects its
/// <see cref="T:Microsoft.VisualStudio.TestTools.UnitTesting.TestContext"/> and runs its
/// <see cref="T:Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute"/> and
/// <see cref="T:Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute"/> methods for every invocation, and a
/// <see cref="T:Microsoft.VisualStudio.TestTools.UnitTesting.TimeoutAttribute"/> applies to each invocation. A generated
/// argument that is <see cref="T:System.IDisposable"/> is disposed after its invocation.
/// </para>
/// <para>
/// An invocation that calls <see cref="M:Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Inconclusive(System.String)"/>
/// is discarded, and the property fails when Hedgehog gives up after 100 discarded cases. A failed property reports
/// Hedgehog's report with the shrunk arguments, the exception of the counterexample as inner exception, the output of its
/// invocation and a <see cref="T:Hedgehog.MSTest.RecheckAttribute"/> ready to paste. Once the cancellation token of the
/// <see cref="T:Microsoft.VisualStudio.TestTools.UnitTesting.TestContext"/> is cancelled, by a timeout or by the test
/// run, the property stops without shrinking. An invocation that ends with any other outcome, such as
/// <see cref="F:Microsoft.VisualStudio.TestTools.UnitTesting.UnitTestOutcome.Error"/>, says that MSTest could not run the
/// test method as a test: the property stops at it without shrinking and reports the outcome and the exception of that
/// invocation.
/// </para>
/// </summary>
/// <example>
/// <code lang="fsharp">
/// [&lt;TestClass&gt;]
/// type ReverseTests () =
///
///     [&lt;Property(200&lt;tests&gt;)&gt;]
///     member _.``reversing a list twice gives the list back`` (xs : int list) =
///         Assert.AreEqual (xs, List.rev (List.rev xs), "reversing twice must give the list back")
/// </code>
/// </example>
[<AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)>]
type PropertyAttribute
    private
    (
        autoGenConfig: Type option,
        autoGenConfigArgs: obj[],
        tests: int<tests> option,
        shrinks: int<shrinks> option,
        size: Size option,
        callerFilePath: string,
        callerLineNumber: int
    ) =
    // TestMethodAttribute records the declaring file and line of the test method from the caller information of its
    // constructor, as STATestMethodAttribute passes it on; F# fills it in for attribute constructors too
    inherit TestMethodAttribute(callerFilePath, callerLineNumber)

    let mutable _autoGenConfig: Type option = autoGenConfig
    let mutable _autoGenConfigArgs: obj[] = autoGenConfigArgs
    let mutable _tests: int<tests> option = tests
    let mutable _shrinks: int<shrinks> option = shrinks
    let mutable _size: Size option = size
    let mutable _seed: uint64 option = None

    /// <summary>
    /// A type with exactly one public static member that returns <see cref="T:Hedgehog.IAutoGenConfig"/>, optionally
    /// taking <see cref="P:Hedgehog.MSTest.PropertyAttribute.AutoGenConfigArgs"/>; its configuration is merged over that of
    /// <see cref="T:Hedgehog.MSTest.PropertiesAttribute"/>. Only for setting: reading it throws.
    /// </summary>
    member _.AutoGenConfig     with set v = _autoGenConfig     <- Some v and get ():Type         = failwith "this getter only exists to make C# named arguments work"

    /// <summary>
    /// The arguments of the member of <see cref="P:Hedgehog.MSTest.PropertyAttribute.AutoGenConfig"/>. Only for setting:
    /// reading it throws.
    /// </summary>
    member _.AutoGenConfigArgs with set v = _autoGenConfigArgs <-      v and get ():obj array    = failwith "this getter only exists to make C# named arguments work"

    /// <summary>
    /// The number of cases that must pass, 100 unless set here or on <see cref="T:Hedgehog.MSTest.PropertiesAttribute"/>.
    /// Only for setting: reading it throws.
    /// </summary>
    member _.Tests             with set v = _tests             <- Some v and get ():int<tests>   = failwith "this getter only exists to make C# named arguments work"

    /// <summary>
    /// The largest number of shrink steps of a failure, unlimited unless set here or on
    /// <see cref="T:Hedgehog.MSTest.PropertiesAttribute"/>. Only for setting: reading it throws.
    /// </summary>
    member _.Shrinks           with set v = _shrinks           <- Some v and get ():int<shrinks> = failwith "this getter only exists to make C# named arguments work"

    /// <summary>
    /// The size of every generated case; without it the size grows from case to case. Only for setting: reading it
    /// throws.
    /// </summary>
    member _.Size              with set v = _size              <- Some v and get ():Size         = failwith "this getter only exists to make C# named arguments work"

    /// <summary>
    /// The seed of the run, so that every run generates the same cases; without it every run draws a random seed.
    /// Only for setting: reading it throws.
    /// </summary>
    member _.Seed              with set v = _seed              <- Some v and get ():uint64       = failwith "this getter only exists to make C# named arguments work"

    /// <summary>
    /// Runs the property with the settings of <see cref="T:Hedgehog.MSTest.PropertiesAttribute"/> or Hedgehog's
    /// defaults.
    /// </summary>
    /// <param name="callerFilePath">The file that declares the test method; the compiler fills it in.</param>
    /// <param name="callerLineNumber">The line that declares the test method; the compiler fills it in.</param>
    new([<CallerFilePath; Optional; DefaultParameterValue("")>] callerFilePath: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>] callerLineNumber: int) =
        PropertyAttribute(None, [||], None, None, None, callerFilePath, callerLineNumber)

    /// <summary>
    /// Runs the property until <paramref name="tests"/> cases pass.
    /// </summary>
    /// <param name="tests">The number of cases that must pass.</param>
    /// <param name="callerFilePath">The file that declares the test method; the compiler fills it in.</param>
    /// <param name="callerLineNumber">The line that declares the test method; the compiler fills it in.</param>
    new(tests, [<CallerFilePath; Optional; DefaultParameterValue("")>] callerFilePath: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>] callerLineNumber: int) =
        PropertyAttribute(None, [||], Some tests, None, None, callerFilePath, callerLineNumber)

    /// <summary>
    /// Runs the property until <paramref name="tests"/> cases pass and shrinks a failure in at most
    /// <paramref name="shrinks"/> steps.
    /// </summary>
    /// <param name="tests">The number of cases that must pass.</param>
    /// <param name="shrinks">The largest number of shrink steps.</param>
    /// <param name="callerFilePath">The file that declares the test method; the compiler fills it in.</param>
    /// <param name="callerLineNumber">The line that declares the test method; the compiler fills it in.</param>
    new(tests, shrinks, [<CallerFilePath; Optional; DefaultParameterValue("")>] callerFilePath: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>] callerLineNumber: int) =
        PropertyAttribute(None, [||], Some tests, Some shrinks, None, callerFilePath, callerLineNumber)

    /// <summary>
    /// Generates the arguments with the configuration of <paramref name="autoGenConfig"/>.
    /// </summary>
    /// <param name="autoGenConfig">
    /// A type with exactly one public static member that returns <see cref="T:Hedgehog.IAutoGenConfig"/>.
    /// </param>
    /// <param name="callerFilePath">The file that declares the test method; the compiler fills it in.</param>
    /// <param name="callerLineNumber">The line that declares the test method; the compiler fills it in.</param>
    new(autoGenConfig, [<CallerFilePath; Optional; DefaultParameterValue("")>] callerFilePath: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>] callerLineNumber: int) =
        PropertyAttribute(Some autoGenConfig, [||], None, None, None, callerFilePath, callerLineNumber)

    /// <summary>
    /// Generates the arguments with the configuration of <paramref name="autoGenConfig"/> until <paramref name="tests"/>
    /// cases pass.
    /// </summary>
    /// <param name="autoGenConfig">
    /// A type with exactly one public static member that returns <see cref="T:Hedgehog.IAutoGenConfig"/>.
    /// </param>
    /// <param name="tests">The number of cases that must pass.</param>
    /// <param name="callerFilePath">The file that declares the test method; the compiler fills it in.</param>
    /// <param name="callerLineNumber">The line that declares the test method; the compiler fills it in.</param>
    new(autoGenConfig:Type, tests, [<CallerFilePath; Optional; DefaultParameterValue("")>] callerFilePath: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>] callerLineNumber: int) =
        PropertyAttribute(Some autoGenConfig, [||], Some tests, None, None, callerFilePath, callerLineNumber)

    /// <summary>
    /// Generates the arguments with the configuration of <paramref name="autoGenConfig"/> until <paramref name="tests"/>
    /// cases pass and shrinks a failure in at most <paramref name="shrinks"/> steps.
    /// </summary>
    /// <param name="autoGenConfig">
    /// A type with exactly one public static member that returns <see cref="T:Hedgehog.IAutoGenConfig"/>.
    /// </param>
    /// <param name="tests">The number of cases that must pass.</param>
    /// <param name="shrinks">The largest number of shrink steps.</param>
    /// <param name="callerFilePath">The file that declares the test method; the compiler fills it in.</param>
    /// <param name="callerLineNumber">The line that declares the test method; the compiler fills it in.</param>
    new(autoGenConfig:Type, tests, shrinks, [<CallerFilePath; Optional; DefaultParameterValue("")>] callerFilePath: string, [<CallerLineNumber; Optional; DefaultParameterValue(-1)>] callerLineNumber: int) =
        PropertyAttribute(Some autoGenConfig, [||], Some tests, Some shrinks, None, callerFilePath, callerLineNumber)

    interface IPropertyAttribute with
        member _.AutoGenConfig with get () = _autoGenConfig and set v = _autoGenConfig <- v
        member _.AutoGenConfigArgs with get () = _autoGenConfigArgs and set v = _autoGenConfigArgs <- v
        member _.Tests with get () = _tests and set v = _tests <- v
        member _.Shrinks with get () = _shrinks and set v = _shrinks <- v
        member _.Size with get () = _size and set v = _size <- v
        member _.Seed with get () = _seed and set v = _seed <- v

    /// <summary>
    /// Runs the test method as a property instead of invoking it once, and returns the single result of the property.
    /// <para>
    /// Each case is one call of <see cref="M:Microsoft.VisualStudio.TestTools.UnitTesting.ITestMethod.InvokeAsync(System.Object[])"/>
    /// with the values of the data row in <see cref="P:Microsoft.VisualStudio.TestTools.UnitTesting.ITestMethod.Arguments"/>
    /// followed by the generated ones. A property that cannot run, because the method is generic or returns a type MSTest
    /// cannot run, because an <see cref="T:Hedgehog.IAutoGenConfig"/> type is invalid or the recheck data cannot be read,
    /// gives a result with the outcome <see cref="F:Microsoft.VisualStudio.TestTools.UnitTesting.UnitTestOutcome.Error"/>
    /// instead of an exception.
    /// </para>
    /// </summary>
    /// <param name="testMethod">The test method that MSTest runs.</param>
    /// <returns>An array with the one result of the property.</returns>
    override _.ExecuteAsync(testMethod: ITestMethod) : Task<TestResult[]> =
        task {
            let! result =
                InternalLogic.executeAsync
                    testMethod.MethodInfo
                    testMethod.Arguments
                    (fun arguments -> testMethod.InvokeAsync arguments)
                    InternalLogic.currentCancellationToken

            // Several results from one ExecuteAsync call appear as separate results of one test and inflate the totals
            return [| result |]
        }
