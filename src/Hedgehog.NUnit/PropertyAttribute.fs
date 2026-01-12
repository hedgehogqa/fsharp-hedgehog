namespace Hedgehog.NUnit

open System
open Hedgehog
open NUnit.Framework
open NUnit.Framework.Interfaces
open NUnit.Framework.Internal
open NUnit.Framework.Internal.Commands

/// Generates arguments using GenX.auto (or autoWith if you provide an AutoGenConfig), then runs Property.check
[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Property, AllowMultiple = false)>]
type PropertyAttribute(autoGenConfig, autoGenConfigArgs, tests, shrinks, size) =
    inherit TestAttribute()

    let mutable _autoGenConfig: Type option = autoGenConfig
    let mutable _autoGenConfigArgs: obj[] = autoGenConfigArgs
    let mutable _tests: int<tests> option = tests
    let mutable _shrinks: int<shrinks> option = shrinks
    let mutable _size: Size option = size

    member _.AutoGenConfig     with set v = _autoGenConfig     <- Some v and get ():Type         = failwith "this getter only exists to make C# named arguments work"
    member _.AutoGenConfigArgs with set v = _autoGenConfigArgs <-      v and get ():obj array    = failwith "this getter only exists to make C# named arguments work"
    member _.Tests             with set v = _tests             <- Some v and get ():int<tests>   = failwith "this getter only exists to make C# named arguments work"
    member _.Shrinks           with set v = _shrinks           <- Some v and get ():int<shrinks> = failwith "this getter only exists to make C# named arguments work"
    member _.Size              with set v = _size              <- Some v and get ():Size         = failwith "this getter only exists to make C# named arguments work"

    new()                                   = PropertyAttribute(None              , [||], None      , None        , None)
    new(tests)                              = PropertyAttribute(None              , [||], Some tests, None        , None)
    new(tests, shrinks)                     = PropertyAttribute(None              , [||], Some tests, Some shrinks, None)
    new(autoGenConfig)                      = PropertyAttribute(Some autoGenConfig, [||], None      , None        , None)
    new(autoGenConfig:Type, tests)          = PropertyAttribute(Some autoGenConfig, [||], Some tests, None        , None)
    new(autoGenConfig:Type, tests, shrinks) = PropertyAttribute(Some autoGenConfig, [||], Some tests, Some shrinks, None)

    interface IPropertyAttribute with
        member _.AutoGenConfig with get () = _autoGenConfig and set v = _autoGenConfig <- v
        member _.AutoGenConfigArgs with get () = _autoGenConfigArgs and set v = _autoGenConfigArgs <- v
        member _.Tests with get () = _tests and set v = _tests <- v
        member _.Shrinks with get () = _shrinks and set v = _shrinks <- v
        member _.Size with get () = _size and set v = _size <- v

    interface ISimpleTestBuilder with
        member _.BuildFrom(mi: IMethodInfo, suite: Test) =
            let testMethod = HedgehogTestMethod(mi, suite)

            // Apply all IApplyToTest attributes (Category, Description, Ignore, Retry, etc.)
            // but exclude IPropertyAttribute implementations since they're handled separately
            mi.GetCustomAttributes(true)
            |> Seq.cast<obj>
            |> Seq.filter (fun attr -> attr :? IApplyToTest && not (attr :? IPropertyAttribute))
            |> Seq.cast<IApplyToTest>
            |> Seq.iter _.ApplyToTest(testMethod)

            testMethod

    interface IWrapTestMethod with
        member _.Wrap(command: TestCommand) =
            { new TestCommand(command.Test) with
                override _.Execute(context) =
                    match command.Test with
                    | :? HedgehogTestMethod as testMethod -> testMethod.RunTest(context)
                    | _ -> command.Execute(context) }

/// Custom TestMethod that runs Hedgehog property tests
and HedgehogTestMethod(mi: IMethodInfo, parentSuite: Test) =
    inherit TestMethod(mi, parentSuite)

    member x.RunTest(context: TestExecutionContext) =
        let testResult = x.MakeTestResult()
        TestExecutionContext.CurrentContext.CurrentResult <- testResult

        try
            try
                use _ = new TestExecutionContext.IsolatedContext()
                x.RunSetUp()
                x.RunPropertyTest(context, testResult)
            with ex ->
                x.HandleException(ex, testResult, FailureSite.SetUp)
        finally
            x.RunTearDown(testResult)

        testResult

    member private x.RunSetUp() =
        if not (isNull x.SetUpMethods) then
            x.SetUpMethods |> Array.iter (fun mi ->
                mi.Invoke(if mi.IsStatic then null else x.Fixture) |> ignore)

    member private x.RunTearDown(testResult: TestResult) =
        try
            if not (isNull x.TearDownMethods) then
                x.TearDownMethods
                |> Array.rev
                |> Array.iter (fun mi ->
                    mi.Invoke(if mi.IsStatic then null else x.Fixture) |> ignore)
        with ex ->
            testResult.RecordTearDownException(x.FilterException(ex))

    member private _.FilterException(ex: exn) =
        match ex with
        | :? NUnitException as nue when not (isNull nue.InnerException) -> nue.InnerException
        | _ -> ex

    member private x.HandleException(ex: exn, testResult: TestResult, failureSite: FailureSite) =
        testResult.RecordException(x.FilterException(ex), failureSite)

    member private x.RunPropertyTest(context: TestExecutionContext, testResult: TestResult) =
        try
            let testMethod = x.Method.MethodInfo
            let target =
                if not (isNull x.Fixture) then x.Fixture
                elif testMethod.IsStatic then null
                else context.TestObject

            let propertyContext = PropertyContext.fromMethod testMethod

            let report =
                InternalLogic.reportAsync propertyContext testMethod target
                |> Async.RunSynchronously

            match report.Status with
            | Status.OK ->
                testResult.SetResult(ResultState.Success)
            | Status.GaveUp ->
                let message = Report.render report
                testResult.SetResult(ResultState.Failure, message)
            | Status.Failed _ ->
                let message = Report.render report
                testResult.SetResult(ResultState.Failure, message)
        with ex ->
            x.HandleException(ex, testResult, FailureSite.Test)
