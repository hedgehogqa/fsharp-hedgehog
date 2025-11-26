namespace Hedgehog.Xunit.Tests.FSharp

open Xunit
open System
open Hedgehog
open Hedgehog.Xunit
open Hedgehog.FSharp

module Common =
  let [<Literal>] skipReason = "Skipping because it's just here to be the target of a [<Fact>] test"

open Common

type Int13 = static member __ = AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)

type Int5() =
  inherit GenAttribute<int>()
  override _.Generator = Gen.constant 5

type Int6() =
  inherit GenAttribute<int>()
  override _.Generator = Gen.constant 6

type IntConstantRange(max: int, min: int) =
  inherit GenAttribute<int>()
  override _.Generator = Range.constant max min |> Gen.int32

module PropertyTest =
  let runReport methodName (typ: Type) instance =
    let method = typ.GetMethod(methodName)
    let context = PropertyContext.fromMethod method
    InternalLogic.report context method instance

module ``Property module tests`` =

  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  let assertShrunk methodName expected =
    let report = PropertyTest.runReport methodName typeof<Marker>.DeclaringType null
    match report.Status with
    | Status.Failed r ->
      Assert.Equal(expected, r.Journal |> Journal.eval |> Seq.head)
    | _ -> failwith "impossible"

  [<Property(Skip = skipReason)>]
  let ``fails for false, skipped`` (value: int) = false

  [<Fact>]
  let ``fails for false`` () =
    assertShrunk (nameof ``fails for false, skipped``) "value = 0"

  [<Property(Skip = skipReason)>]
  let ``Result with Error shrinks, skipped`` (i: int) =
    if i > 10 then
      Error ()
    else
      Ok ()
  [<Fact>]
  let ``Result with Error shrinks`` () =
    assertShrunk (nameof ``Result with Error shrinks, skipped``) "i = 11"

  [<Property(Skip = skipReason)>]
  let ``Result with Error reports exception with Error value, skipped`` (i: int) =
    if i > 10 then
      Error "Too many digits!"
    else
      Ok ()
  [<Fact>]
  let ``Result with Error reports exception with Error value`` () =
    let report = PropertyTest.runReport (nameof ``Result with Error reports exception with Error value, skipped``) typeof<Marker>.DeclaringType null
    match report.Status with
    | Status.Failed r ->
      let errorMessage = r.Journal |> Journal.eval |> Seq.skip 1 |> Seq.exactlyOne
      Assert.Contains($"System.Exception: Result is in the Error case with the following value:{Environment.NewLine}\"Too many digits!\"", errorMessage)
    | _ -> failwith "impossible"

  [<Property>]
  let ``Can generate an int`` (i: int) =
    printfn $"Test input: %i{i}"

  [<Property(Skip = skipReason)>]
  let ``Can shrink an int, skipped`` (i: int) =
    if i >= 50 then failwith "Some error."
  [<Fact>]
  let ``Can shrink an int`` () =
    assertShrunk (nameof ``Can shrink an int, skipped``) "i = 50"

  [<Property>]
  let ``Can generate two ints`` (i1: int, i2: int) =
    printfn $"Test input: %i{i1}, %i{i2}"

  [<Property(Skip = skipReason)>]
  let ``Can shrink both ints, skipped`` (i1: int, i2: int) =
    if i1 >= 10 &&
       i2 >= 20 then failwith "Some error."
  [<Fact>]
  let ``Can shrink both ints`` () =
    assertShrunk (nameof ``Can shrink both ints, skipped``) "i1 = 10\ni2 = 20"

  [<Property>]
  let ``Can generate an int and string`` (i: int, s: string) =
    printfn $"Test input: %i{i}, %s{s}"

  [<Property(Tests = 1000<tests>, Skip = skipReason)>]
  let ``Can shrink an int and string, skipped`` (i: int, s: string) =
    if i >= 2 && s.Contains "b" then failwith "Some error."
  [<Fact>]
  let ``Can shrink an int and string`` () =
    assertShrunk (nameof ``Can shrink an int and string, skipped``) "i = 2\ns = \"b\""

  [<Property(typeof<Int13>, 1<tests>)>]
  let ``runs with 13 once`` () = ()
  [<Fact>]
  let ``Tests 'runs with 13 once'`` () =
    let context = PropertyContext.fromMethod (nameof ``runs with 13 once`` |> getMethod)
    Assert.Equal(None, context.Shrinks)
    Assert.Equal(Some 1<tests>, context.Tests)
    let generated = Gen.autoWith context.AutoGenConfig |> Gen.sample 1 1 |> Seq.exactlyOne
    Assert.Equal(13, generated)

  type CustomRecord = { Herp: int; Derp: string }
  [<Property>]
  let ``Works up to 26 parameters`` (
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
                                    z: int list list) =
    printfn $"%A{a} %A{b} %A{c} %A{d} %A{e} %A{f} %A{g} %A{h} %A{i} %A{j} %A{k} %A{l} %A{m} %A{n} %A{o} %A{p} %A{q} %A{r} %A{s} %A{t} %A{u} %A{v} %A{w} %A{x} %A{y} %A{z} "

  [<Property>]
  let ``multiple unresolved generics works`` _ _ = ()

  [<Property>]
  let ``mixed unresolved generics works 1`` (_: int) _ = ()

  [<Property>]
  let ``mixed unresolved generics works 2`` _ (_: int) = ()

  [<Property>]
  let ``unresolved nested generics works`` (_: _ list) (_: Result<_, _>) = ()

  [<Property>]
  let ``mixed nested generics works`` (_: int) _ (_: _ list) (_: Result<_, _>) = ()

  [<Property>]
  let ``returning unresolved generic works`` (x: 'a) = x

  [<Property>]
  let ``returning unresolved nested generic works`` () : Result<unit, 'a> = Ok ()

  [<Property>]
  let ``0 parameters passes`` () = ()

type ``Property class tests``(output: ITestOutputHelper) =

  [<Property>]
  let ``Can generate an int`` (i: int) =
    $"Test input: %i{i}" |> output.WriteLine

  [<Property>]
  let ``Can generate two ints`` (i1: int, i2: int) =
    $"Test input: %i{i1}, %i{i2}" |> output.WriteLine

  [<Property>]
  let ``Can generate an int and string`` (i: int, s: string) =
    $"Test input: %i{i}, %s{s}" |> output.WriteLine

type ConfigArg = static member __ a = AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.constant a)

[<Properties(AutoGenConfig = typeof<ConfigArg>, AutoGenConfigArgs = [|'a'|])>]
module ``AutoGenConfigArgs tests`` =

  let [<Property>] ``PropertiesAttribute passes its AutoGenConfigArgs`` a = a = 'a'

  let config a b =
    AutoGenConfig.defaults
    |> AutoGenConfig.addGenerator (Gen.constant a)
    |> AutoGenConfig.addGenerator (Gen.constant b)
  type ConfigGenericArgs = static member __ (a: 'a)     (b: 'b)  = config a b
  type ConfigArgs        = static member __ (a: string) (b: int) = config a b
  type ConfigMixedArgsA  = static member __ (a: 'a)     (b: int) = config a b
  type ConfigMixedArgsB  = static member __ (a: string) (b: 'b)  = config a b

  let test s i = s = "foo" && i = 13
  let [<Property(AutoGenConfig = typeof<ConfigGenericArgs>, AutoGenConfigArgs = [|"foo"; 13|])>] ``all generics``      s i = test s i
  let [<Property(AutoGenConfig = typeof<ConfigArgs>       , AutoGenConfigArgs = [|"foo"; 13|])>] ``all non-generics``  s i = test s i
  let [<Property(AutoGenConfig = typeof<ConfigMixedArgsA> , AutoGenConfigArgs = [|"foo"; 13|])>] ``mixed generics, 1`` s i = test s i
  let [<Property(AutoGenConfig = typeof<ConfigMixedArgsB> , AutoGenConfigArgs = [|"foo"; 13|])>] ``mixed generics, 2`` s i = test s i

module ``Property module with AutoGenConfig tests`` =

  module NormalTests =

    [<Property(                typeof<Int13>)>]
    let ``Uses custom Int gen``                i = i = 13
    [<Property(AutoGenConfig = typeof<Int13>)>]
    let ``Uses custom Int gen with named arg`` i = i = 13

  module FailingTests =
      type private Marker = class end

      type NonstaticProperty = member _.__ = AutoGenConfig.defaults
      [<Property(typeof<NonstaticProperty>, Skip = skipReason)>]
      let ``Instance property fails, skipped`` () = ()
      [<Fact>]
      let ``Instance property fails`` () =
        let testMethod = typeof<Marker>.DeclaringType.GetMethod(nameof ``Instance property fails, skipped``)
        let e = Assert.Throws<Exception>(fun () -> PropertyContext.fromMethod testMethod |> ignore)
        Assert.Equal("Hedgehog.Xunit.Tests.FSharp.Property module with AutoGenConfig tests+FailingTests+NonstaticProperty must have exactly one public static property that returns an AutoGenConfig.

An example type definition:

type NonstaticProperty =
  static member __ =
    AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)
", e.Message)

      type NonAutoGenConfig = static member __ = ()
      [<Property(typeof<NonAutoGenConfig>, Skip = skipReason)>]
      let ``Non AutoGenConfig static property fails, skipped`` () = ()
      [<Fact>]
      let ``Non AutoGenConfig static property fails`` () =
        let testMethod = typeof<Marker>.DeclaringType.GetMethod(nameof ``Non AutoGenConfig static property fails, skipped``)
        let e = Assert.Throws<Exception>(fun () -> PropertyContext.fromMethod testMethod |> ignore)
        Assert.Equal("Hedgehog.Xunit.Tests.FSharp.Property module with AutoGenConfig tests+FailingTests+NonAutoGenConfig must have exactly one public static property that returns an AutoGenConfig.

An example type definition:

type NonAutoGenConfig =
  static member __ =
    AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)
", e.Message)


type Int2718 = static member __ = AutoGenConfig.empty |> AutoGenConfig.addGenerator (Gen.constant 2718)

[<Properties(typeof<Int13>, 200<tests>)>]
module ``Module with <Properties> tests`` =

  [<Property>]
  let ``Module <Properties> works`` (i: int) =
    i = 13

  [<Property(typeof<Int2718>)>]
  let ``Module <Properties> is overriden by Method <Property>`` (i: int) =
    i = 2718

  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Fact>]
  let ``Module <Properties> tests (count) works`` () =
    let testMethod = getMethod (nameof ``Module <Properties> works``)
    let context = PropertyContext.fromMethod testMethod
    Assert.Equal(Some 200<tests>, context.Tests)

  [<Property(300<tests>)>]
  let ``Module <Properties> tests (count) is overriden by Method <Property>, skipped`` (_: int) = ()
  [<Fact>]
  let ``Module <Properties> tests (count) is overriden by Method <Property>`` () =
    let testMethod = getMethod (nameof ``Module <Properties> tests (count) is overriden by Method <Property>, skipped``)
    let context = PropertyContext.fromMethod testMethod
    Assert.Equal(Some 300<tests>, context.Tests)


[<Properties(typeof<Int13>)>]
type ``Class with <Properties> tests``(_output: ITestOutputHelper) =

  [<Property>]
  let ``Class <Properties> works`` (i: int) =
    i = 13

  [<Property(typeof<Int2718>)>]
  let ``Class <Properties> is overriden by Method level <Property>`` (i: int) =
    i = 2718


type PropertyInt13Attribute() = inherit PropertyAttribute(typeof<Int13>)
module ``Property inheritance tests`` =
  [<PropertyInt13>]
  let ``Property inheritance works`` (i: int) =
    i = 13


type PropertiesInt13Attribute() = inherit PropertiesAttribute(typeof<Int13>)
[<PropertiesInt13>]
module ``Properties inheritance tests`` =
  [<Property>]
  let ``Properties inheritance works`` (i: int) =
    i = 13


[<Properties(Tests = 1<tests>, AutoGenConfig = typeof<Int13>)>]
module ``Properties named arg tests`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod
  [<Property>]
  let ``runs once with 13`` () = ()
  [<Fact>]
  let ``Tests 'runs once with 13'`` () =
    let context = PropertyContext.fromMethod (nameof ``runs once with 13`` |> getMethod)
    Assert.Equal(Some 1<tests>, context.Tests)
    let generated = Gen.autoWith context.AutoGenConfig |> Gen.sample 1 1 |> Seq.exactlyOne
    Assert.Equal(13, generated)


[<Properties(1<tests>)>]
module ``Properties (tests count) tests`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod
  [<Property>]
  let ``runs once`` () = ()
  [<Fact>]
  let ``Tests 'runs once'`` () =
    let context = PropertyContext.fromMethod (nameof ``runs once`` |> getMethod)
    Assert.Equal(Some 1<tests>, context.Tests)


module ``Asynchronous tests`` =

  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod
  let assertShrunk methodName expected =
    let report = PropertyTest.runReport methodName typeof<Marker>.DeclaringType null
    printfn "DEBUG: Report status = %A" report.Status
    match report.Status with
    | Status.Failed r ->
      Assert.Equal(expected, r.Journal |> Journal.eval |> Seq.head)
    | _ -> failwithf "impossible - status was: %A" report.Status

  open System.Threading.Tasks
  let FooAsync() =
      Task.Delay 2

  [<Property(Skip = skipReason)>]
  let ``Returning Task with exception fails, skipped`` (i: int) : Task =
    if i > 10 then
      Exception() |> Task.FromException
    else FooAsync()
  [<Fact>]
  let ``Returning Task with exception fails`` () =
    assertShrunk (nameof ``Returning Task with exception fails, skipped``) "i = 11"

  [<Property(Skip = skipReason)>]
  let ``TaskBuilder (returning Task<unit>) with exception shrinks, skipped`` (i: int) : Task<unit> =
    task {
      do! FooAsync()
      if i > 10 then
        raise <| Exception()
    }
  [<Fact>]
  let ``TaskBuilder (returning Task<unit>) with exception shrinks`` () =
    assertShrunk (nameof ``TaskBuilder (returning Task<unit>) with exception shrinks, skipped``) "i = 11"

  [<Property(Skip = skipReason)>]
  let ``Async with exception shrinks, skipped`` (i: int) =
    async {
      do! Async.Sleep 2
      if i > 10 then
        raise <| Exception()
    }
  [<Fact>]
  let ``Async with exception shrinks`` () =
    assertShrunk (nameof ``Async with exception shrinks, skipped``) "i = 11"

  [<Property(Skip = skipReason)>]
  let ``AsyncResult with Error shrinks, skipped`` (i: int) =
    async {
      do! Async.Sleep 2
      if i > 10 then
        return Error ()
      else
        return Ok ()
    }
  [<Fact>]
  let ``AsyncResult with Error shrinks`` () =
    assertShrunk (nameof ``AsyncResult with Error shrinks, skipped``) "i = 11"

  [<Property(Skip = skipReason)>]
  let ``TaskResult with Error shrinks, skipped`` (i: int) =
    task {
      do! FooAsync()
      if i > 10 then
        return Error ()
      else
        return Ok ()
    }
  [<Fact>]
  let ``TaskResult with Error shrinks`` () =
    assertShrunk (nameof ``TaskResult with Error shrinks, skipped``) "i = 11"

  [<Property(Skip = skipReason)>]
  let ``Non-unit TaskResult with Error shrinks, skipped`` (i: int) =
    task {
      do! FooAsync()
      if i > 10 then
        return Error "Test fails"
      else
        return Ok 1
    }
  [<Fact>]
  let ``Non-unit TaskResult with Error shrinks`` () =
    assertShrunk (nameof ``Non-unit TaskResult with Error shrinks, skipped``) "i = 11"

module ``IDisposable test module`` =
  let mutable runs = 0
  let mutable disposes = 0

  type DisposableImplementation() =
    interface IDisposable with
      member _.Dispose() =
        disposes <- disposes + 1
  let getMethod = typeof<DisposableImplementation>.DeclaringType.GetMethod

  [<Property(Skip = skipReason)>]
  let ``IDisposable arg get disposed even if exception thrown, skipped`` (_: DisposableImplementation) (i: int) =
    runs <- runs + 1
    if i > 10 then raise <| Exception()
  [<Fact>]
  let ``IDisposable arg get disposed even if exception thrown`` () =
    let report = PropertyTest.runReport (nameof ``IDisposable arg get disposed even if exception thrown, skipped``) typeof<DisposableImplementation>.DeclaringType null
    match report.Status with
    | Status.Failed _ ->
      Assert.NotEqual(0, runs)
      Assert.Equal(runs, disposes)
    | _ -> failwith "impossible"


module ``The PropertyTestCaseDiscoverer works`` =
  let mutable runs = 0
  [<Property>]
  let ``increment runs`` () =
    runs <- runs + 1

  // This assumes that ``increment runs`` runs before this test runs. The tests *seem* to run in alphabetical order.
  // https://github.com/asherber/Xunit.Priority doesn't seem to work; perhaps modules are treated differently. Ref: https://stackoverflow.com/questions/9210281/
  [<Fact>]
  let ``PropertyAttribute is discovered and run`` () =
    Assert.True(runs > 0)

module TupleTests =
  [<Fact>]
  let ``Non-Hedgehog.Xunit passes`` () =
    Property.check <| property {
      let! a, b =
        AutoGenConfig.defaults
        |> AutoGenConfig.addGenerator (Gen.constant (1, 2))
        |> Gen.autoWith<int*int>
      Assert.Equal(1, a)
      Assert.Equal(2, b)
    }

  type CustomTupleGen = static member __ = AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.constant (1, 2))
  [<Property(typeof<CustomTupleGen>)>]
  let ``Hedgehog.Xunit requires another param to pass`` ((a,b) : int*int, _: bool) =
    Assert.Equal(1, a)
    Assert.Equal(2, b)

module ShrinkTests =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(100<tests>, 0<shrinks>, Skip = skipReason)>]
  let ``0 shrinks, skipped`` i =
    i < 2500
  [<Fact>]
  let ``0 shrinks`` () =
    let context = PropertyContext.fromMethod (nameof ``0 shrinks, skipped`` |> getMethod)
    Assert.Equal(Some 0<shrinks>, context.Shrinks)

  [<Property(Shrinks = 1<shrinks>, Skip = skipReason)>]
  let ``1 shrinks, run, skipped`` i =
    i < 2500
  [<Fact>]
  let ``1 shrinks, run`` () =
    let report = PropertyTest.runReport (nameof ``1 shrinks, run, skipped``) typeof<Marker>.DeclaringType null
    match report.Status with
    | Failed data ->
      Assert.Equal(1<shrinks>, data.Shrinks)
    | _ -> failwith "impossible"

  [<Property(typeof<Int13>, 100<tests>, 0<shrinks>, Skip = skipReason)>]
  let ``0 shrinks, run, skipped`` () : unit =
    failwith "oops"
  [<Fact>]
  let ``0 shrinks, run`` () =
    let report = PropertyTest.runReport (nameof ``0 shrinks, run, skipped``) typeof<Marker>.DeclaringType null
    match report.Status with
    | Failed data ->
      Assert.Equal(0<shrinks>, data.Shrinks)
    | _ -> failwith "impossible"

type Forever = static member __ = AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.constant "...")
[<Properties(typeof<Forever>, 100<tests>, 0<shrinks>)>]
module ``Module with <Properties> tests and 0 shrinks`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(Skip = skipReason)>]
  let ``0 shrinks, skipped`` i =
    i < 2500
  [<Fact>]
  let ``0 shrinks`` () =
    let context = PropertyContext.fromMethod (nameof ``0 shrinks, skipped`` |> getMethod)
    Assert.Equal(Some 0<shrinks>, context.Shrinks)

  [<Property(Shrinks = 1<shrinks>, Skip = skipReason)>]
  let ``1 shrinks, run, skipped`` i =
    i < 2500
  [<Fact>]
  let ``1 shrinks, run`` () =
    let report = PropertyTest.runReport (nameof ``1 shrinks, run, skipped``) typeof<Marker>.DeclaringType null
    match report.Status with
    | Failed data ->
      Assert.Equal(1<shrinks>, data.Shrinks)
    | _ -> failwith "impossible"

[<Properties(Shrinks = 0<shrinks>)>]
module ``Module with <Properties> tests 0 shrinks manual`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(Skip = skipReason)>]
  let ``0 shrinks, skipped`` i =
    i < 2500
  [<Fact>]
  let ``0 shrinks`` () =
    let context = PropertyContext.fromMethod (nameof ``0 shrinks, skipped`` |> getMethod)
    Assert.Equal(Some 0<shrinks>, context.Shrinks)

  [<Property(Shrinks = 1<shrinks>, Skip = skipReason)>]
  let ``1 shrinks, run, skipped`` i =
    i < 2500
  [<Fact>]
  let ``1 shrinks, run`` () =
    let report = PropertyTest.runReport (nameof ``1 shrinks, run, skipped``) typeof<Marker>.DeclaringType null
    match report.Status with
    | Failed data ->
      Assert.Equal(1<shrinks>, data.Shrinks)
    | _ -> failwith "impossible"

[<Properties(100<tests>, 0<shrinks>)>]
module ``Module with <Properties> tests with whatever tests and 0 shrinks`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(Skip = skipReason)>]
  let ``0 shrinks, skipped`` i =
    i < 2500
  [<Fact>]
  let ``0 shrinks`` () =
    let context = PropertyContext.fromMethod (nameof ``0 shrinks, skipped`` |> getMethod)
    Assert.Equal(Some 0<shrinks>, context.Shrinks)

  [<Property(Shrinks = 1<shrinks>, Skip = skipReason)>]
  let ``1 shrinks, run, skipped`` i =
    i < 2500
  [<Fact>]
  let ``1 shrinks, run`` () =
    let report = PropertyTest.runReport (nameof ``1 shrinks, run, skipped``) typeof<Marker>.DeclaringType null
    match report.Status with
    | Failed data ->
      Assert.Equal(1<shrinks>, data.Shrinks)
    | _ -> failwith "impossible"

module RecheckTests =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  let [<Literal>] expectedRecheckData = "0_16700074754810023652_2867022503662193831_"
  [<Property(Skip = skipReason)>]
  [<Recheck(expectedRecheckData)>]
  let ``recheck, skipped`` () = ()
  [<Fact>]
  let recheck () =
    let context = PropertyContext.fromMethod (nameof ``recheck, skipped`` |> getMethod)
    match context.Recheck with
    | None -> failwith "impossible"
    | Some actualRecheckData ->
      Assert.Equal(expectedRecheckData, actualRecheckData)

  let mutable runs = 0
  [<Property>]
  [<Recheck("1_9056294896546497174_14632957226901407867_")>]
  let ``recheck runs once`` (_: int) =
    runs <- runs + 1
    Assert.Equal(1, runs)
    //Assert.True(i < 100) // used to generate the Recheck Data

  [<Recheck("99_9056294896546497174_14632957226901407867_")>]
  let [<Property(Size = 1, Skip = skipReason)>] ``Recheck's Size overrides Property's Size, skipped, 99`` (_: int) = false
  [<Recheck("1_9056294896546497174_14632957226901407867_")>]
  let [<Property(Size = 99, Skip = skipReason)>] ``Recheck's Size overrides Property's Size, skipped, 1`` (_: int) = false
  [<Fact>]
  let ``Recheck's Size overrides Property's Size`` () =
    // Verify that the context reads the size from the Recheck attribute, not the Property attribute
    let getContextSize test =
      let method = getMethod test
      let context = PropertyContext.fromMethod method
      context.Size

    // The Property attribute says Size=1, but Recheck says Size=99
    // PropertyContext should have Size=1 (from Property attribute)
    // But when running, the recheck data (which contains size=99) should override it
    let contextSize99 = getContextSize (nameof ``Recheck's Size overrides Property's Size, skipped, 99``)
    Assert.Equal(Some 1, contextSize99) // Context sees Property's Size

    let contextSize1 = getContextSize (nameof ``Recheck's Size overrides Property's Size, skipped, 1``)
    Assert.Equal(Some 99, contextSize1) // Context sees Property's Size

    // Now verify the recheck strings are parsed correctly
    let getRecheckSize test =
      let method = getMethod test
      let context = PropertyContext.fromMethod method
      match context.Recheck with
      | None -> failwith "Expected Recheck data"
      | Some recheckStr ->
        // Parse the size from "size_seed_gamma_path" format
        recheckStr.Split('_').[0] |> Int32.Parse

    let recheckSize99 = getRecheckSize (nameof ``Recheck's Size overrides Property's Size, skipped, 99``)
    Assert.Equal(99, recheckSize99)

    let recheckSize1 = getRecheckSize (nameof ``Recheck's Size overrides Property's Size, skipped, 1``)
    Assert.Equal(1, recheckSize1)

[<Properties(Size=1)>]
module SizeTests =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(Size = 2)>]
  let ``property size, actual`` () = ()
  [<Fact>]
  let ``property size`` () =
    let context = PropertyContext.fromMethod (nameof ``property size, actual`` |> getMethod)
    match context.Size with
    | None -> failwith "impossible"
    | Some size -> Assert.Equal(2, size)

  [<Property>]
  let ``properites size, actual`` () = ()
  [<Fact>]
  let ``properites size`` () =
    let context = PropertyContext.fromMethod (nameof ``properites size, actual`` |> getMethod)
    match context.Size with
    | None -> failwith "impossible"
    | Some size -> Assert.Equal(1, size)

module ``tryRaise tests`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(Skip = skipReason)>]
  let ``always fails, skipped`` () = false
  [<Fact>]
  let ``always fails`` () =
    let report = PropertyTest.runReport (nameof ``always fails, skipped``) typeof<Marker>.DeclaringType null
    let actual = Assert.Throws<PropertyFailedException>(fun () -> InternalLogic.tryRaise report)
    let expectedMessage = """*** Failed! Falsifiable (after 1 test):"""
    Assert.Contains(expectedMessage, actual.Message)
    let expectedMessage = """Recheck seed: "0_"""
    Assert.Contains(expectedMessage, actual.Message)


module ``returning a property runs it`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(Skip = skipReason)>]
  let ``returning a passing property with internal gen passes, skipped`` () = property {
    let! a = Gen.constant 13
    Assert.Equal(13, a)
  }
  [<Fact>]
  let ``returning a passing property with internal gen passes`` () =
    let report = PropertyTest.runReport (nameof ``returning a passing property with internal gen passes, skipped``) typeof<Marker>.DeclaringType null
    Assert.Equal(100<tests>, report.Tests)

  [<Property(Skip = skipReason)>]
  let ``returning a failing property with internal gen fails and shrinks, skipped`` () = property {
    let! a = Gen.int32 (Range.constant 1 100)
    Assert.True(a <= 50)
  }
  [<Fact>]
  let ``returning a failing property with internal gen fails and shrinks`` () =
    let report = PropertyTest.runReport (nameof ``returning a failing property with internal gen fails and shrinks, skipped``) typeof<Marker>.DeclaringType null
    let actual = Assert.Throws<PropertyFailedException>(fun () -> InternalLogic.tryRaise report)
    actual.Message.Contains("51") |> Assert.True

  [<Property(typeof<Int13>, Skip = skipReason)>]
  let ``returning a passing property with external gen passes, skipped`` i = property {
    Assert.Equal(13, i)
  }
  [<Fact>]
  let ``returning a passing property with external gen passes`` () =
    let report = PropertyTest.runReport (nameof ``returning a passing property with external gen passes, skipped``) typeof<Marker>.DeclaringType null
    Assert.Equal(100<tests>, report.Tests)

  [<Property(Skip = skipReason)>]
  let ``returning a failing property with external gen fails and shrinks, skipped`` i = property {
    let! _50 = Gen.constant 50
    Assert.True(i <= _50)
  }
  [<Fact>]
  let ``returning a failing property with external gen fails and shrinks`` () =
    let report = PropertyTest.runReport (nameof ``returning a failing property with external gen fails and shrinks, skipped``) typeof<Marker>.DeclaringType null
    let actual = Assert.Throws<PropertyFailedException>(fun () -> InternalLogic.tryRaise report)
    actual.Message.Contains("51") |> Assert.True

  [<Property(Skip = skipReason)>]
  let ``returning a passing property<bool> with internal gen passes, skipped`` () = property {
    let! a = Gen.constant 13
    return 13 = a
  }
  [<Fact>]
  let ``returning a passing property<bool> with internal gen passes`` () =
    let report = PropertyTest.runReport (nameof ``returning a passing property<bool> with internal gen passes, skipped``) typeof<Marker>.DeclaringType null
    Assert.Equal(100<tests>, report.Tests)

  [<Property(Skip = skipReason)>]
  let ``returning a failing property<bool> with internal gen fails and shrinks, skipped`` () = property {
    let! a = Gen.int32 (Range.constant 1 100)
    return a <= 50
  }
  [<Fact>]
  let ``returning a failing property<bool> with internal gen fails and shrinks`` () =
    let report = PropertyTest.runReport (nameof ``returning a failing property<bool> with internal gen fails and shrinks, skipped``) typeof<Marker>.DeclaringType null
    let actual = Assert.Throws<PropertyFailedException>(fun () -> InternalLogic.tryRaise report)
    actual.Message.Contains("51") |> Assert.True

  [<Property(typeof<Int13>, Skip = skipReason)>]
  let ``returning a passing property<bool> with external gen passes, skipped`` i = property {
    return 13 = i
  }
  [<Fact>]
  let ``returning a passing property<bool> with external gen passes`` () =
    let report = PropertyTest.runReport (nameof ``returning a passing property<bool> with external gen passes, skipped``) typeof<Marker>.DeclaringType null
    Assert.Equal(100<tests>, report.Tests)

  [<Property(Skip = skipReason)>]
  let ``returning a failing property<bool> with external gen fails and shrinks, skipped`` i = property {
    let! _50 = Gen.constant 50
    return i <= _50
  }
  [<Fact>]
  let ``returning a failing property<bool> with external gen fails and shrinks`` () =
    let report = PropertyTest.runReport (nameof ``returning a failing property<bool> with external gen fails and shrinks, skipped``) typeof<Marker>.DeclaringType null
    let actual = Assert.Throws<PropertyFailedException>(fun () -> InternalLogic.tryRaise report)
    actual.Message.Contains("51") |> Assert.True

module ``GenAttribute Tests`` =

  [<Property>]
  let ``can set parameter as 5`` ([<Int5>] i) =
    Assert.StrictEqual(5, i)

  [<Property(typeof<Int13>)>]
  let ``overrides Property's autoGenConfig`` ([<Int5>] i) =
    Assert.StrictEqual(5, i)

  [<Property>]
  let ``can have different generators for the same parameter type`` ([<Int5>] five) ([<Int6>] six) =
     five = 5 && six = 6

  [<Property>]
  let ``can restrict on range`` ([<IntConstantRange(min = 0, max = 5)>] i) =
    i >= 0 && i <= 5

  type OtherAttribute() = inherit Attribute()

  [<Property>]
  let ``Doesn't error with OtherAttribute`` ([<Other>][<Int5>] i) =
    i = 5

[<Properties(typeof<Int13>)>]
module ``GenAttribute with Properties Tests`` =

  [<Property>]
  let ``overrides Properties' autoGenConfig`` ([<Int5>] i) =
    Assert.StrictEqual(5, i)

  [<Property(typeof<Int13>)>]
  let ``overrides Properties' and Property's autoGenConfig`` ([<Int5>] i) =
    Assert.StrictEqual(5, i)

type Int13A =
  static member __ =
    AutoGenConfig.empty
    |> AutoGenConfig.addGenerator (Gen.constant 13)
    |> AutoGenConfig.addGenerator (Gen.constant "A")

[<Properties(typeof<Int13A>)>]
module ``Module with <Properties(typeof<Int13A>)>`` =

  [<Property>]
  let ``Module's <Properties> works`` (i, s) =
    i = 13 && s = "A"

  [<Property(typeof<Int2718>)>]
  let ``Module's <Properties> merges with Method level <Property>`` (i, s) =
    i = 2718 && s = "A"
