namespace Hedgehog.NUnit.Tests.FSharp

open NUnit.Framework
open System
open Hedgehog
open Hedgehog.NUnit
open Hedgehog.FSharp

module Common =
    [<Literal>]
    let skipReason = "Skipping because it's just here to be the target of a test"

open Common

type Int13 =
    static member __ = AutoGenConfig.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)

type Int5() =
    inherit GenAttribute<int>()
    override _.Generator = Gen.constant 5

type Int6() =
    inherit GenAttribute<int>()
    override _.Generator = Gen.constant 6

type IntConstantRange(min: int, max: int) =
    inherit GenAttribute<int>()
    override _.Generator = Range.constant min max |> Gen.int32

module PropertyTest =
    let runReport methodName (typ: Type) instance =
        let method = typ.GetMethod(methodName)
        let context = PropertyContext.fromMethod method
        InternalLogic.reportAsync context method instance |> Async.RunSynchronously

module ``Property module tests`` =

    type private Marker = class end
    let getMethod = typeof<Marker>.DeclaringType.GetMethod

    let assertShrunk methodName (expectedParams: (string * obj) list) =
        let report = PropertyTest.runReport methodName typeof<Marker>.DeclaringType null

        match report.Status with
        | Status.Failed r ->
            let actualParams =
                r.Journal
                |> Journal.eval
                |> Seq.choose (function
                    | TestParameter(name, value) -> Some(name, value)
                    | _ -> None)
                |> Seq.toList

            Assert.That(actualParams, Is.EqualTo(expectedParams :> obj))
        | _ -> failwith "impossible"

    [<Property>]
    [<Ignore(skipReason)>]
    let ``fails for false, skipped`` (value: int) = false

    [<Test>]
    let ``fails for false`` () =
        assertShrunk (nameof ``fails for false, skipped``) [ ("value", box 0) ]

    [<Property>]
    [<Ignore(skipReason)>]
    let ``Result with Error shrinks, skipped`` (i: int) = if i > 10 then Error() else Ok()

    [<Test>]
    let ``Result with Error shrinks`` () =
        assertShrunk (nameof ``Result with Error shrinks, skipped``) [ ("i", box 11) ]

    [<Property>]
    let ``Can generate an int`` (i: int) = printfn $"Test input: %i{i}"

    [<Property>]
    [<Ignore(skipReason)>]
    let ``Can shrink an int, skipped`` (i: int) =
        if i >= 50 then
            failwith "Some error."

    [<Test>]
    let ``Can shrink an int`` () =
        assertShrunk (nameof ``Can shrink an int, skipped``) [ ("i", box 50) ]

    [<Property>]
    let ``Can generate two ints`` (i1: int, i2: int) = printfn $"Test input: %i{i1}, %i{i2}"

    [<Property>]
    [<Ignore(skipReason)>]
    let ``Can shrink both ints, skipped`` (i1: int, i2: int) =
        if i1 >= 10 && i2 >= 20 then
            failwith "Some error."

    [<Test>]
    let ``Can shrink both ints`` () =
        assertShrunk (nameof ``Can shrink both ints, skipped``) [ ("i1", box 10); ("i2", box 20) ]

    [<Property>]
    let ``Can generate an int and string`` (i: int, s: string) = printfn $"Test input: %i{i}, %s{s}"

    [<Property(1000<tests>)>]
    [<Ignore(skipReason)>]
    let ``Can shrink an int and string, skipped`` (i: int, s: string) =
        if i >= 2 && s.Contains "b" then
            failwith "Some error."

    [<Test>]
    let ``Can shrink an int and string`` () =
        assertShrunk (nameof ``Can shrink an int and string, skipped``) [ ("i", box 2); ("s", box "b") ]

    [<Property(typeof<Int13>, 1<tests>)>]
    let ``runs with 13 once`` () = ()

    [<Test>]
    let ``Tests 'runs with 13 once'`` () =
        let method =
            typeof<Marker>.DeclaringType.GetMethods()
            |> Array.find (fun m -> m.Name.Contains("runs with 13 once"))

        let context = PropertyContext.fromMethod method
        // Just check the basic configuration is right
        Assert.That(context.Tests, Is.EqualTo(Some 1<tests>))
        Assert.That(context.AutoGenConfig, Is.Not.Null)

    type CustomRecord = { Herp: int; Derp: string }

    [<Property>]
    let ``Works up to 26 parameters``
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
        printfn
            $"%A{a} %A{b} %A{c} %A{d} %A{e} %A{f} %A{g} %A{h} %A{i} %A{j} %A{k} %A{l} %A{m} %A{n} %A{o} %A{p} %A{q} %A{r} %A{s} %A{t} %A{u} %A{v} %A{w} %A{x} %A{y} %A{z} "

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
    let ``returning unresolved nested generic works`` () : Result<unit, 'a> = Ok()

    [<Property>]
    let ``0 parameters passes`` () = ()

[<TestFixture>]
type ``Property class tests``() =

    [<Property>]
    member _.``Can generate an int``(i: int) = printfn $"Test input: %i{i}"

    [<Property>]
    member _.``Can generate two ints``(i1: int, i2: int) = printfn $"Test input: %i{i1}, %i{i2}"

    [<Property>]
    member _.``Can generate an int and string``(i: int, s: string) = printfn $"Test input: %i{i}, %s{s}"

    [<Property>]
    member _.``GenAttribute works``([<Int5>] i: int) = Assert.That(i, Is.EqualTo(5))

    [<Property>]
    member _.``GenAttribute works with parameters``([<IntConstantRange(5, 7)>] i: int) =
        Assert.That(i, Is.GreaterThanOrEqualTo(5))
        Assert.That(i, Is.LessThanOrEqualTo(7))

module ``Async and Task tests`` =

    open System.Threading.Tasks

    [<Property>]
    let ``async property works`` (i: int) =
        async {
            do! Async.Sleep 1
            return i > 0 || i <= 0 // always true
        }

    [<Property>]
    let ``task property works`` (i: int) =
        task {
            do! Task.Delay 1
            return i > 0 || i <= 0 // always true
        }

    [<Property>]
    let ``async unit property works`` (i: int) =
        async {
            do! Async.Sleep 1
            printfn $"%i{i}"
        }

    [<Property>]
    let ``task unit property works`` (i: int) =
        task {
            do! Task.Delay 1
            printfn $"%i{i}"
        }

[<TestFixture>]
[<Properties(typeof<Int13>)>]
type ``PropertiesAttribute works``() =

    [<Property>]
    member _.``inherits AutoGenConfig from class``(i: int) = Assert.That(i, Is.EqualTo(13))

    [<Property(1<tests>)>]
    member _.``can override tests``(i: int) = Assert.That(i, Is.EqualTo(13))
