// The tests of tests/Hedgehog.Xunit.Tests.FSharp and tests/Hedgehog.NUnit.Tests.FSharp, rewritten for MSTest; the doc
// comment of each class names the modules it is ported from. Properties are instance members of [<TestClass>] types and
// fail by throwing; a target that fails by design lives in a class without [<TestClass>] and runs through Harness.
// Left out are the cases MSTest 4 cannot discover (bool, Result, Async, Task<'T> and Property return values, unresolved
// generic parameters, module-level properties), the xUnit discoverer and tryRaise tests, and the in-body Property.check
// test. The targets that must shrink once fix their seed, because a random first failure of exactly 2500 cannot shrink.

namespace Hedgehog.MSTest.Tests

open System
open System.Threading
open System.Threading.Tasks
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.MSTest
open Microsoft.VisualStudio.TestTools.UnitTesting

/// Targets of the shrinking tests, which fail by design.
type ShrinkTargets() =

    [<Property>]
    member _.``fails for any value`` (value: int) : unit = Assert.Fail "fails by design"

    [<Property>]
    member _.``fails from 50`` (i: int) : unit =
        if i >= 50 then
            failwith "Some error."

    [<Property>]
    member _.``fails when both ints are large`` (i1: int, i2: int) : unit =
        if i1 >= 10 && i2 >= 20 then
            failwith "Some error."

    [<Property(1000<tests>)>]
    member _.``fails for an int and a string`` (i: int, s: string) : unit =
        if i >= 2 && s.Contains("b", StringComparison.Ordinal) then
            failwith "Some error."

    [<Property>]
    member _.``Task fails above 10`` (i: int) : Task =
        if i > 10 then
            raise (System.Exception())
        else
            Task.Delay 2

    [<Property>]
    member _.``task expression fails above 10`` (i: int) : Task =
        task {
            do! Task.Delay 2

            if i > 10 then
                raise (System.Exception())
        }

    [<Property>]
    member _.``ValueTask fails above 10`` (i: int) : ValueTask =
        if i > 10 then
            ValueTask.FromException(System.Exception())
        else
            ValueTask.CompletedTask

/// Ported from the shrinking tests of "Property module tests" and "Asynchronous tests".
[<TestClass>]
type ``Shrinking tests``() =

    let assertShrunk name (expected: string array) : Task =
        task {
            let! run = Harness.runAsync<ShrinkTargets> name
            CollectionAssert.AreEqual(expected, Harness.parametersOf run.Report, "The property must shrink to the smallest counterexample.")
        }

    [<TestMethod>]
    member _.``fails and shrinks an int to 0`` () : Task =
        assertShrunk (nameof Unchecked.defaultof<ShrinkTargets>.``fails for any value``) [| "value=0" |]

    [<TestMethod>]
    member _.``Can shrink an int`` () : Task =
        assertShrunk (nameof Unchecked.defaultof<ShrinkTargets>.``fails from 50``) [| "i=50" |]

    [<TestMethod>]
    member _.``Can shrink both ints`` () : Task =
        assertShrunk (nameof Unchecked.defaultof<ShrinkTargets>.``fails when both ints are large``) [| "i1=10"; "i2=20" |]

    [<TestMethod>]
    member _.``Can shrink an int and string`` () : Task =
        assertShrunk (nameof Unchecked.defaultof<ShrinkTargets>.``fails for an int and a string``) [| "i=2"; "s=b" |]

    [<TestMethod>]
    member _.``Returning Task with exception fails`` () : Task =
        assertShrunk (nameof Unchecked.defaultof<ShrinkTargets>.``Task fails above 10``) [| "i=11" |]

    [<TestMethod>]
    member _.``task expression with exception shrinks`` () : Task =
        assertShrunk (nameof Unchecked.defaultof<ShrinkTargets>.``task expression fails above 10``) [| "i=11" |]

    [<TestMethod>]
    member _.``ValueTask with exception shrinks`` () : Task =
        assertShrunk (nameof Unchecked.defaultof<ShrinkTargets>.``ValueTask fails above 10``) [| "i=11" |]

/// The record of the 26-parameter property.
type CustomRecord = { Herp: int; Derp: string }

/// Ported from the generating tests of "Property module tests" and "Property class tests".
[<TestClass>]
type ``Property class tests``() =

    member val TestContext: TestContext = null with get, set

    [<Property>]
    member this.``Can generate an int`` (i: int) = this.TestContext.WriteLine $"Test input: %i{i}"

    [<Property>]
    member this.``Can generate two ints`` (i1: int, i2: int) =
        this.TestContext.WriteLine $"Test input: %i{i1}, %i{i2}"

    [<Property>]
    member this.``Can generate an int and string`` (i: int, s: string) =
        this.TestContext.WriteLine $"Test input: %i{i}, %s{s}"

    [<Property>]
    member this.``Works up to 26 parameters``
        (
            a: string,
            b: char,
            c: double,
            d: bool,
            e: DateTime,
            f: DateTimeOffset,
            g: string list,
            h: char list,
            i: int,
            j: int array,
            k: char array,
            l: DateTime array,
            m: DateTimeOffset array,
            n: CustomRecord option,
            o: DateTime option,
            p: Result<string, string>,
            q: Result<string, int>,
            r: Result<int, int>,
            s: Result<int, string>,
            t: Result<DateTime, string>,
            u: Result<CustomRecord, string>,
            v: Result<DateTimeOffset, DateTimeOffset>,
            w: Result<double, DateTimeOffset>,
            x: Result<double, bool>,
            y: CustomRecord,
            z: int list list
        ) =
        this.TestContext.WriteLine
            $"%A{a} %A{b} %A{c} %A{d} %A{e} %A{f} %A{g} %A{h} %A{i} %A{j} %A{k} %A{l} %A{m} %A{n} %A{o} %A{p} %A{q} %A{r} %A{s} %A{t} %A{u} %A{v} %A{w} %A{x} %A{y} %A{z} "

    [<Property>]
    member _.``0 parameters passes`` () = ()

/// Targets of the AutoGenConfig and context tests, which these tests inspect without running them.
type ConfigTargets() =

    [<Property(typeof<Int13>, 1<tests>)>]
    member _.``runs with 13 once`` () = ()

    [<Property(typeof<NonstaticProperty>)>]
    member _.``Instance property fails`` () = ()

    [<Property(typeof<NonAutoGenConfig>)>]
    member _.``Non AutoGenConfig static property fails`` () = ()

/// An AutoGenConfig type whose member is not static, which the adapter rejects.
and NonstaticProperty =
    member _.__ = AutoGenConfig.defaults

/// An AutoGenConfig type whose static member does not return an AutoGenConfig, which the adapter rejects.
and NonAutoGenConfig =
    static member __ = ()

/// Ported from "Property module with AutoGenConfig tests".
[<TestClass>]
type ``Property AutoGenConfig tests``() =

    let expectedMessage (configType: Type) =
        $"%s{configType.FullName} must have exactly one public static property that returns an AutoGenConfig."
        + """

An example type definition:

"""
        + $"type %s{configType.Name} ="
        + """
  static member __ =
    AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)
"""

    [<Property(typeof<Int13>)>]
    member _.``Uses custom Int gen`` (i: int) = Assert.AreEqual(13, i, "Int13 sets every int to 13.")

    [<Property(AutoGenConfig = typeof<Int13>)>]
    member _.``Uses custom Int gen with named arg`` (i: int) = Assert.AreEqual(13, i, "Int13 sets every int to 13.")

    [<TestMethod>]
    member _.``Tests 'runs with 13 once'`` () =
        let context = Harness.contextOf<ConfigTargets> (nameof Unchecked.defaultof<ConfigTargets>.``runs with 13 once``)
        Assert.AreEqual(Some 1<tests>, context.Tests, "The attribute sets the number of tests.")
        Assert.AreEqual(None, context.Shrinks, "The attribute sets no shrink limit.")
        let generated = Gen.autoWith<int> context.AutoGenConfig |> Gen.sample 1 1 |> Seq.exactlyOne
        Assert.AreEqual(13, generated, "The attribute's config generates 13.")

    [<TestMethod>]
    member _.``Instance property fails`` () =
        let error =
            Assert.Throws<Exception>(
                (fun () -> Harness.contextOf<ConfigTargets> (nameof Unchecked.defaultof<ConfigTargets>.``Instance property fails``) |> ignore),
                "An AutoGenConfig type without a static member is rejected."
            )

        Assert.AreEqual(expectedMessage typeof<NonstaticProperty>, error.Message, "The message explains the rule.")

    [<TestMethod>]
    member _.``Non AutoGenConfig static property fails`` () =
        let error =
            Assert.Throws<Exception>(
                (fun () ->
                    Harness.contextOf<ConfigTargets> (nameof Unchecked.defaultof<ConfigTargets>.``Non AutoGenConfig static property fails``)
                    |> ignore),
                "An AutoGenConfig type whose member returns something else is rejected."
            )

        Assert.AreEqual(expectedMessage typeof<NonAutoGenConfig>, error.Message, "The message explains the rule.")

    [<TestMethod>]
    member _.``an invalid AutoGenConfig type gives an error result`` () : Task =
        task {
            let! result = Harness.executeAsync<ConfigTargets> (nameof Unchecked.defaultof<ConfigTargets>.``Instance property fails``)
            Assert.AreEqual(UnitTestOutcome.Error, result.Outcome, "A property that cannot run is an error.")
            Assert.Contains("must have exactly one public static property", Harness.messageOf result, StringComparison.Ordinal, "The message explains the rule.")
        }

/// An AutoGenConfig type that takes the value of its char generator as an argument.
type ConfigArg =
    static member __ a = AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.constant a)

/// AutoGenConfig types whose members take generic, concrete and mixed arguments.
module ConfigArgs =
    let config a b =
        AutoGenConfig.defaults
        |> AutoGenConfig.addGenerator (Gen.constant a)
        |> AutoGenConfig.addGenerator (Gen.constant b)

    type ConfigGenericArgs = static member __ (a: 'a)     (b: 'b)  = config a b
    type ConfigArgs        = static member __ (a: string) (b: int) = config a b
    type ConfigMixedArgsA  = static member __ (a: 'a)     (b: int) = config a b
    type ConfigMixedArgsB  = static member __ (a: string) (b: 'b)  = config a b

    /// One generic parameter for two arguments: each generated value is one of them.
    type ConfigRepeatedGeneric =
        static member __ (a: 'a) (b: 'a) = AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.item [ a; b ])

    /// A generic parameter nested in the array type of the argument: each generated value is one of its items.
    type ConfigNestedGeneric =
        static member __ (items: 'a[]) = AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.item items)

/// Ported from "AutoGenConfigArgs tests".
[<TestClass>]
[<Properties(AutoGenConfig = typeof<ConfigArg>, AutoGenConfigArgs = [| 'a' |])>]
type ``AutoGenConfigArgs tests``() =

    let test (s: string) (i: int) =
        Assert.AreEqual("foo", s, "The config arguments set every string.")
        Assert.AreEqual(13, i, "The config arguments set every int.")

    [<Property>]
    member _.``PropertiesAttribute passes its AutoGenConfigArgs`` (a: char) =
        Assert.AreEqual('a', a, "The class-level config arguments set every char.")

    [<Property(AutoGenConfig = typeof<ConfigArgs.ConfigGenericArgs>, AutoGenConfigArgs = [| "foo"; 13 |])>]
    member _.``all generics`` (s: string, i: int) = test s i

    [<Property(AutoGenConfig = typeof<ConfigArgs.ConfigArgs>, AutoGenConfigArgs = [| "foo"; 13 |])>]
    member _.``all non-generics`` (s: string, i: int) = test s i

    [<Property(AutoGenConfig = typeof<ConfigArgs.ConfigMixedArgsA>, AutoGenConfigArgs = [| "foo"; 13 |])>]
    member _.``mixed generics, 1`` (s: string, i: int) = test s i

    [<Property(AutoGenConfig = typeof<ConfigArgs.ConfigMixedArgsB>, AutoGenConfigArgs = [| "foo"; 13 |])>]
    member _.``mixed generics, 2`` (s: string, i: int) = test s i

    [<Property(AutoGenConfig = typeof<ConfigArgs.ConfigRepeatedGeneric>, AutoGenConfigArgs = [| "foo"; "bar" |])>]
    member _.``one generic parameter for two arguments`` (s: string) =
        CollectionAssert.Contains([| "foo"; "bar" |], s, "Both arguments of the one generic parameter supply the strings.")

    [<TestMethod>]
    member _.``a generic parameter nested in an array type`` () =
        let config = Hedgehog.MSTest.AutoGenConfig.instantiate typeof<ConfigArgs.ConfigNestedGeneric> [| box [| 13; 14 |] |]
        let values = Gen.autoWith<int> config |> Gen.sample 10 20 |> Array.ofSeq
        // IsSubsetOf counts repeated items, so the distinct values are compared
        CollectionAssert.IsSubsetOf(Array.distinct values, [| 13; 14 |], "The items of the array argument supply every int.")

    [<TestMethod>]
    member _.``a generic parameter must get one type from all of its arguments`` () =
        let error =
            Assert.Throws<Exception>(
                Action(fun () ->
                    Hedgehog.MSTest.AutoGenConfig.instantiate typeof<ConfigArgs.ConfigRepeatedGeneric> [| box "foo"; box 13 |]
                    |> ignore),
                "Two types for one generic parameter cannot instantiate the config."
            )

        Assert.Contains("gets both System.String and System.Int32", error.Message, StringComparison.Ordinal, "The message names both types.")

/// Targets of the class-level settings tests, which these tests inspect without running them.
[<Properties(typeof<Int13>, 200<tests>)>]
type ClassPropertiesTargets() =

    [<Property>]
    member _.``Class Properties works`` (_: int) = ()

    [<Property(300<tests>)>]
    member _.``Class Properties tests (count) is overridden by Method Property`` (_: int) = ()

/// Targets of the named-argument tests.
[<Properties(Tests = 1<tests>, AutoGenConfig = typeof<Int13>)>]
type NamedArgumentTargets() =

    [<Property>]
    member _.``runs once with 13`` () = ()

/// Targets of the tests-count tests.
[<Properties(1<tests>)>]
type TestsCountTargets() =

    [<Property>]
    member _.``runs once`` () = ()

/// Ported from "Module with <Properties> tests", "Properties named arg tests" and "Properties (tests count)
/// tests".
[<TestClass>]
type ``Class-level settings tests``() =

    [<TestMethod>]
    member _.``Class Properties tests (count) works`` () =
        let context = Harness.contextOf<ClassPropertiesTargets> (nameof Unchecked.defaultof<ClassPropertiesTargets>.``Class Properties works``)
        Assert.AreEqual(Some 200<tests>, context.Tests, "The class sets the number of tests.")

    [<TestMethod>]
    member _.``Class Properties tests (count) is overridden by Method Property`` () =
        let context =
            Harness.contextOf<ClassPropertiesTargets>
                (nameof Unchecked.defaultof<ClassPropertiesTargets>.``Class Properties tests (count) is overridden by Method Property``)

        Assert.AreEqual(Some 300<tests>, context.Tests, "The method's number of tests wins over the class's.")

    [<TestMethod>]
    member _.``Tests 'runs once with 13'`` () =
        let context = Harness.contextOf<NamedArgumentTargets> (nameof Unchecked.defaultof<NamedArgumentTargets>.``runs once with 13``)
        Assert.AreEqual(Some 1<tests>, context.Tests, "The named argument sets the number of tests.")
        let generated = Gen.autoWith<int> context.AutoGenConfig |> Gen.sample 1 1 |> Seq.exactlyOne
        Assert.AreEqual(13, generated, "The named argument's config generates 13.")

    [<TestMethod>]
    member _.``Tests 'runs once'`` () =
        let context = Harness.contextOf<TestsCountTargets> (nameof Unchecked.defaultof<TestsCountTargets>.``runs once``)
        Assert.AreEqual(Some 1<tests>, context.Tests, "The constructor argument sets the number of tests.")

/// Ported from "Class with <Properties> tests".
[<TestClass>]
[<Properties(typeof<Int13>)>]
type ``Class with Properties tests``() =

    [<Property>]
    member _.``Class Properties works`` (i: int) = Assert.AreEqual(13, i, "The class's config sets every int.")

    [<Property(typeof<Int2718>)>]
    member _.``Class Properties is overridden by Method level Property`` (i: int) =
        Assert.AreEqual(2718, i, "The method's config wins over the class's.")

/// Ported from "Property inheritance tests".
[<TestClass>]
type ``Property inheritance tests``() =

    [<PropertyInt13>]
    member _.``Property inheritance works`` (i: int) = Assert.AreEqual(13, i, "The derived attribute's config sets every int.")

/// Ported from "Properties inheritance tests".
[<TestClass>]
[<PropertiesInt13>]
type ``Properties inheritance tests``() =

    [<Property>]
    member _.``Properties inheritance works`` (i: int) = Assert.AreEqual(13, i, "The derived attribute's config sets every int.")

/// Ported from "Module with <Properties(typeof<Int13A>)>".
[<TestClass>]
[<Properties(typeof<Int13A>)>]
type ``Class with Properties of Int13A``() =

    [<Property>]
    member _.``Class's Properties works`` (i: int, s: string) =
        Assert.AreEqual(13, i, "The class's config sets every int.")
        Assert.AreEqual("A", s, "The class's config sets every string.")

    [<Property(typeof<Int2718>)>]
    member _.``Class's Properties merges with Method level Property`` (i: int, s: string) =
        Assert.AreEqual(2718, i, "The method's config wins for ints.")
        Assert.AreEqual("A", s, "The class's config still sets every string.")

/// An AutoGenConfig type whose every int * int tuple is (1, 2).
type CustomTupleGen =
    static member __ = AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.constant (1, 2))

/// Ported from "TupleTests".
[<TestClass>]
type ``Tuple tests``() =

    [<Property(typeof<CustomTupleGen>)>]
    member _.``a tuple parameter takes its generator from the config`` ((a, b): int * int, _: bool) =
        Assert.AreEqual(1, a, "The config sets the first item.")
        Assert.AreEqual(2, b, "The config sets the second item.")

/// The seed of the targets that must shrink once. With a random seed, a run whose first failing value is exactly 2500 has
/// no smaller failing value to shrink to and ends after zero shrinks, about one run in 2500.
module ShrinkSeed =
    [<Literal>]
    let Value = 1UL

/// Targets of the shrink-limit tests, which fail by design.
type ShrinksTargets() =

    [<Property(100<tests>, 0<shrinks>)>]
    member _.``0 shrinks`` (i: int) = Assert.IsLessThan(2500, i, "fails by design for large values")

    [<Property(Shrinks = 1<shrinks>, Seed = ShrinkSeed.Value)>]
    member _.``1 shrinks, run`` (i: int) = Assert.IsLessThan(2500, i, "fails by design for large values")

    [<Property(typeof<Int13>, 100<tests>, 0<shrinks>)>]
    member _.``0 shrinks, run`` () : unit = failwith "oops"

/// An AutoGenConfig type whose every string is "...".
type Forever =
    static member __ = AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.constant "...")

/// Targets of the class-level shrink-limit tests, set through the (config, tests, shrinks) constructor.
[<Properties(typeof<Forever>, 100<tests>, 0<shrinks>)>]
type ClassShrinksTargets() =

    [<Property>]
    member _.``0 shrinks`` (i: int) = Assert.IsLessThan(2500, i, "fails by design for large values")

    [<Property(Shrinks = 1<shrinks>, Seed = ShrinkSeed.Value)>]
    member _.``1 shrinks, run`` (i: int) = Assert.IsLessThan(2500, i, "fails by design for large values")

/// Targets of the class-level shrink-limit tests, set through the named argument.
[<Properties(Shrinks = 0<shrinks>)>]
type ClassNamedShrinksTargets() =

    [<Property>]
    member _.``0 shrinks`` (i: int) = Assert.IsLessThan(2500, i, "fails by design for large values")

    [<Property(Shrinks = 1<shrinks>, Seed = ShrinkSeed.Value)>]
    member _.``1 shrinks, run`` (i: int) = Assert.IsLessThan(2500, i, "fails by design for large values")

/// Targets of the class-level shrink-limit tests, set through the (tests, shrinks) constructor.
[<Properties(100<tests>, 0<shrinks>)>]
type ClassTestsShrinksTargets() =

    [<Property>]
    member _.``0 shrinks`` (i: int) = Assert.IsLessThan(2500, i, "fails by design for large values")

    [<Property(Shrinks = 1<shrinks>, Seed = ShrinkSeed.Value)>]
    member _.``1 shrinks, run`` (i: int) = Assert.IsLessThan(2500, i, "fails by design for large values")

/// Ported from "ShrinkTests" and the three "Module with <Properties> tests ... shrinks" modules.
[<TestClass>]
type ``Shrink limit tests``() =

    let assertShrinks (expected: int<shrinks>) (run: InternalLogic.PropertyRun) =
        match run.Report.Status with
        | Failed data -> Assert.AreEqual(expected, data.Shrinks, "The shrink limit bounds the shrink steps.")
        | status -> Assert.Fail $"The property must fail, but its status is %A{status}."

    [<TestMethod>]
    member _.``0 shrinks`` () =
        let context = Harness.contextOf<ShrinksTargets> (nameof Unchecked.defaultof<ShrinksTargets>.``0 shrinks``)
        Assert.AreEqual(Some 0<shrinks>, context.Shrinks, "The attribute sets the shrink limit.")

    [<TestMethod>]
    member _.``1 shrinks, run`` () : Task =
        task {
            let! run = Harness.runAsync<ShrinksTargets> (nameof Unchecked.defaultof<ShrinksTargets>.``1 shrinks, run``)
            assertShrinks 1<shrinks> run
        }

    [<TestMethod>]
    member _.``0 shrinks, run`` () : Task =
        task {
            let! run = Harness.runAsync<ShrinksTargets> (nameof Unchecked.defaultof<ShrinksTargets>.``0 shrinks, run``)
            assertShrinks 0<shrinks> run
        }

    [<TestMethod>]
    [<DataRow("config, tests and shrinks")>]
    [<DataRow("named argument")>]
    [<DataRow("tests and shrinks")>]
    member _.``Class Properties sets 0 shrinks`` (constructor: string) =
        let context =
            match constructor with
            | "config, tests and shrinks" -> Harness.contextOf<ClassShrinksTargets> "0 shrinks"
            | "named argument" -> Harness.contextOf<ClassNamedShrinksTargets> "0 shrinks"
            | _ -> Harness.contextOf<ClassTestsShrinksTargets> "0 shrinks"

        Assert.AreEqual(Some 0<shrinks>, context.Shrinks, "The class sets the shrink limit.")

    [<TestMethod>]
    member _.``Method Property overrides the class's 0 shrinks`` () : Task =
        task {
            let! configRun = Harness.runAsync<ClassShrinksTargets> "1 shrinks, run"
            assertShrinks 1<shrinks> configRun
            let! namedRun = Harness.runAsync<ClassNamedShrinksTargets> "1 shrinks, run"
            assertShrinks 1<shrinks> namedRun
            let! testsRun = Harness.runAsync<ClassTestsShrinksTargets> "1 shrinks, run"
            assertShrinks 1<shrinks> testsRun
        }

/// The recheck data of the recheck tests.
module RecheckData =
    [<Literal>]
    let Expected = "0_16700074754810023652_2867022503662193831_"

/// Targets of the recheck context tests.
type RecheckTargets() =

    [<Property>]
    [<Recheck(RecheckData.Expected)>]
    member _.recheck () = ()

    [<Property(Size = 1)>]
    [<Recheck("99_9056294896546497174_14632957226901407867_")>]
    member _.``Recheck's Size overrides Property's Size, 99`` (_: int) = ()

    [<Property(Size = 99)>]
    [<Recheck("1_9056294896546497174_14632957226901407867_")>]
    member _.``Recheck's Size overrides Property's Size, 1`` (_: int) = ()

/// Ported from "RecheckTests".
[<TestClass>]
type ``Recheck tests``() =
    static let mutable runs = 0

    [<TestMethod>]
    member _.recheck () =
        let context = Harness.contextOf<RecheckTargets> (nameof Unchecked.defaultof<RecheckTargets>.recheck)
        Assert.AreEqual(Some RecheckData.Expected, context.Recheck, "The attribute supplies the recheck data.")

    [<Property>]
    [<Recheck("1_9056294896546497174_14632957226901407867_")>]
    member _.``recheck runs once`` (_: int) =
        Assert.AreEqual(1, Interlocked.Increment &runs, "A recheck replays exactly one case.")

    [<TestMethod>]
    member _.``Recheck's Size overrides Property's Size`` () =
        // The context keeps the Size of the attribute; the recheck data carries its own size, which the run uses
        let size99 = Harness.contextOf<RecheckTargets> "Recheck's Size overrides Property's Size, 99"
        let size1 = Harness.contextOf<RecheckTargets> "Recheck's Size overrides Property's Size, 1"
        Assert.AreEqual(Some 1, size99.Size, "The context sees the attribute's Size.")
        Assert.AreEqual(Some 99, size1.Size, "The context sees the attribute's Size.")
        Assert.AreEqual("99", size99.Recheck.Value.Split('_')[0], "The recheck data carries size 99.")
        Assert.AreEqual("1", size1.Recheck.Value.Split('_')[0], "The recheck data carries size 1.")

/// Targets of the size tests.
[<Properties(Size = 1)>]
type SizeTargets() =

    [<Property(Size = 2)>]
    member _.``property size, actual`` () = ()

    [<Property>]
    member _.``properties size, actual`` () = ()

/// Ported from "SizeTests".
[<TestClass>]
type ``Size tests``() =

    [<TestMethod>]
    member _.``property size`` () =
        let context = Harness.contextOf<SizeTargets> (nameof Unchecked.defaultof<SizeTargets>.``property size, actual``)
        Assert.AreEqual(Some 2, context.Size, "The method's Size wins over the class's.")

    [<TestMethod>]
    member _.``properties size`` () =
        let context = Harness.contextOf<SizeTargets> (nameof Unchecked.defaultof<SizeTargets>.``properties size, actual``)
        Assert.AreEqual(Some 1, context.Size, "The class's Size applies.")

/// Counts the instances of DisposableImplementation and their disposals.
module DisposableCounters =
    let mutable runs = 0
    let mutable disposes = 0

/// A generated argument that counts its disposal.
type DisposableImplementation() =
    interface IDisposable with
        member _.Dispose() = Interlocked.Increment &DisposableCounters.disposes |> ignore

/// Targets of the IDisposable test, which fails by design.
type DisposableTargets() =

    [<Property>]
    member _.``IDisposable arg gets disposed even if exception thrown`` (_: DisposableImplementation, i: int) =
        Interlocked.Increment &DisposableCounters.runs |> ignore

        if i > 10 then
            raise (System.Exception())

/// Ported from "IDisposable test module".
[<TestClass>]
type ``IDisposable tests``() =

    [<TestMethod>]
    member _.``IDisposable arg gets disposed even if exception thrown`` () : Task =
        task {
            let! run =
                Harness.runAsync<DisposableTargets>
                    (nameof Unchecked.defaultof<DisposableTargets>.``IDisposable arg gets disposed even if exception thrown``)

            Assert.IsTrue((match run.Report.Status with Failed _ -> true | _ -> false), "The property fails by design.")
            Assert.AreNotEqual(0, DisposableCounters.runs, "The property runs.")
            Assert.AreEqual(DisposableCounters.runs, DisposableCounters.disposes, "Every generated argument is disposed.")
        }

/// An attribute on a parameter that is not a generator.
type OtherAttribute() =
    inherit Attribute()

/// Ported from "GenAttribute Tests".
[<TestClass>]
type ``GenAttribute tests``() =

    [<Property>]
    member _.``can set parameter as 5`` ([<Int5>] i: int) = Assert.AreEqual(5, i, "The attribute sets the value.")

    [<Property(typeof<Int13>)>]
    member _.``overrides Property's autoGenConfig`` ([<Int5>] i: int) =
        Assert.AreEqual(5, i, "The attribute wins over the method's config.")

    [<Property>]
    member _.``can have different generators for the same parameter type`` ([<Int5>] five: int, [<Int6>] six: int) =
        Assert.AreEqual(5, five, "The first attribute sets the first value.")
        Assert.AreEqual(6, six, "The second attribute sets the second value.")

    [<Property>]
    member _.``can restrict on range`` ([<IntConstantRange(0, 5)>] i: int) =
        Assert.IsInRange(0, 5, i, "The attribute bounds the value.")

    [<Property>]
    member _.``Doesn't error with OtherAttribute`` ([<Other; Int5>] i: int) =
        Assert.AreEqual(5, i, "Another attribute on the parameter does not hide the generator.")

/// Ported from "GenAttribute with Properties Tests".
[<TestClass>]
[<Properties(typeof<Int13>)>]
type ``GenAttribute with Properties tests``() =

    [<Property>]
    member _.``overrides Properties' autoGenConfig`` ([<Int5>] i: int) =
        Assert.AreEqual(5, i, "The attribute wins over the class's config.")

    [<Property(typeof<Int13>)>]
    member _.``overrides Properties' and Property's autoGenConfig`` ([<Int5>] i: int) =
        Assert.AreEqual(5, i, "The attribute wins over both configs.")
