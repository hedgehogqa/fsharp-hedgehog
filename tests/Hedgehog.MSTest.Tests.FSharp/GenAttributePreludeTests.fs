namespace Hedgehog.MSTest.Tests

open System
open System.Net
open System.Net.Sockets
open Hedgehog.FSharp
open Hedgehog.MSTest
open Microsoft.VisualStudio.TestTools.UnitTesting

/// Ported from "GenAttribute Prelude tests" of tests/Hedgehog.NUnit.Tests.FSharp.
[<TestClass>]
type ``GenAttribute Prelude tests``() =

    [<Property>]
    member _.``Int attribute generates integers`` ([<Int(-10, 10)>] i: int) =
        Assert.IsInRange(-10, 10, i, "The value lies in the range.")

    [<Property>]
    member _.``PositiveInt generates positive integers`` ([<PositiveInt>] i: int) =
        Assert.IsGreaterThan(0, i, "The value is positive.")

    [<Property>]
    member _.``NonNegativeInt generates non-negative integers`` ([<NonNegativeInt>] i: int) =
        Assert.IsGreaterThanOrEqualTo(0, i, "The value is not negative.")

    [<Property>]
    member _.``NonZeroInt generates non-zero integers`` ([<NonZeroInt>] i: int) =
        Assert.AreNotEqual(0, i, "The value is not zero.")

    [<Property>]
    member _.``NonZeroInt from 0 generates the positive part of its range`` ([<NonZeroInt(0, 5)>] i: int) =
        Assert.IsInRange(1, 5, i, "The value lies in the range and is not zero.")

    [<Property>]
    member _.``NonZeroInt up to 0 generates the negative part of its range`` ([<NonZeroInt(-5, 0)>] i: int) =
        Assert.IsInRange(-5, -1, i, "The value lies in the range and is not zero.")

    [<TestMethod>]
    member _.``NonZeroInt rejects the range from 0 to 0`` () =
        Assert.ThrowsExactly<ArgumentException>(
            Action(fun () -> NonZeroIntAttribute(0, 0).Generator |> ignore),
            "The range from 0 to 0 holds no non-zero value."
        )
        |> ignore

    [<Property>]
    member _.``OddAttribute generates odd integers`` ([<Odd>] i: int) =
        Assert.AreNotEqual(0, i % 2, "The value is odd.")

    [<Property>]
    member _.``EvenAttribute generates even integers`` ([<Even>] i: int) =
        Assert.AreEqual(0, i % 2, "The value is even.")

    [<Property>]
    member _.``Odd stays inside a range that ends with an even value`` ([<Odd(1, 10)>] i: int) =
        Assert.IsInRange(1, 9, i, "The value lies in the range.")
        Assert.AreNotEqual(0, i % 2, "The value is odd.")

    [<Property>]
    member _.``Odd generates the only odd value of its range`` ([<Odd(-1, 0)>] i: int) =
        Assert.AreEqual(-1, i, "-1 is the only odd value from -1 to 0.")

    [<Property>]
    member _.``Odd reaches Int32.MaxValue without overflow`` ([<Odd(Int32.MaxValue - 1, Int32.MaxValue)>] i: int) =
        Assert.AreEqual(Int32.MaxValue, i, "Int32.MaxValue is the only odd value of the range.")

    [<Property>]
    member _.``Even stays inside a range that starts with an odd value`` ([<Even(1, 10)>] i: int) =
        Assert.IsInRange(2, 10, i, "The value lies in the range.")
        Assert.AreEqual(0, i % 2, "The value is even.")

    [<Property>]
    member _.``Even generates the only even value of its range`` ([<Even(-1, 0)>] i: int) =
        Assert.AreEqual(0, i, "0 is the only even value from -1 to 0.")

    [<Property>]
    member _.``Even reaches Int32.MinValue without overflow`` ([<Even(Int32.MinValue, Int32.MinValue + 1)>] i: int) =
        Assert.AreEqual(Int32.MinValue, i, "Int32.MinValue is the only even value of the range.")

    [<TestMethod>]
    member _.``Odd rejects a range without an odd value`` () =
        Assert.ThrowsExactly<ArgumentException>(
            Action(fun () -> OddAttribute(0, 0).Generator |> ignore),
            "The range from 0 to 0 holds no odd value."
        )
        |> ignore

    [<TestMethod>]
    member _.``Even rejects a range without an even value`` () =
        Assert.ThrowsExactly<ArgumentException>(
            Action(fun () -> EvenAttribute(1, 1).Generator |> ignore),
            "The range from 1 to 1 holds no even value."
        )
        |> ignore

    [<Property>]
    member _.``Email generates valid email addresses`` ([<Email>] email: string) =
        Assert.Contains("@", email, StringComparison.Ordinal, "An email address contains @.")
        Assert.Contains(".", email, StringComparison.Ordinal, "An email address contains a dot.")

    [<Property>]
    member _.``DomainName generates valid domain names`` ([<DomainName>] domain: string) =
        Assert.Contains(".", domain, StringComparison.Ordinal, "A domain name contains a dot.")

    [<Property>]
    member _.``AlphaNumString generates alphanumeric strings`` ([<AlphaNumString(5, 20)>] s: string) =
        Assert.IsInRange(5, 20, s.Length, "The length lies in the range.")
        Assert.MatchesRegex("^[a-zA-Z0-9]*$", s, "The string holds letters and digits only.")

    [<Property>]
    member _.``UnicodeString generates strings of the requested length`` ([<UnicodeString(3, 8)>] s: string) =
        Assert.IsInRange(3, 8, s.Length, "The length lies in the range.")

    [<Property>]
    member _.``Identifier generates valid identifiers`` ([<Identifier>] id: string) =
        Assert.IsNotEmpty(id, "An identifier is not empty.")
        Assert.IsTrue(Char.IsLetter id[0] || id[0] = '_', "An identifier starts with a letter or an underscore.")

    [<Property>]
    member _.``LatinName generates Latin names`` ([<LatinName>] name: string) =
        Assert.IsNotEmpty(name, "A Latin name is not empty.")
        Assert.IsTrue(Char.IsUpper name[0], "A Latin name starts with an uppercase letter.")

    [<Property>]
    member _.``SnakeCase generates snake_case strings`` ([<SnakeCase>] s: string) =
        if s.Contains("_", StringComparison.Ordinal) then
            Assert.MatchesRegex("^[a-z0-9]+(_[a-z0-9]+)*$", s, "Snake case joins lowercase words with underscores.")

    [<Property>]
    member _.``KebabCase generates kebab-case strings`` ([<KebabCase>] s: string) =
        if s.Contains("-", StringComparison.Ordinal) then
            Assert.MatchesRegex("^[a-z0-9]+(-[a-z0-9]+)*$", s, "Kebab case joins lowercase words with hyphens.")

    [<Property>]
    member _.``Ipv4Address generates valid IPv4 addresses`` ([<Ipv4Address>] ip: IPAddress) =
        Assert.AreEqual(AddressFamily.InterNetwork, ip.AddressFamily, "The address is an IPv4 address.")

    [<Property>]
    member _.``Ipv6Address generates valid IPv6 addresses`` ([<Ipv6Address>] ip: IPAddress) =
        Assert.AreEqual(AddressFamily.InterNetworkV6, ip.AddressFamily, "The address is an IPv6 address.")

    [<Property>]
    member _.``DateTime generates valid dates`` ([<DateTime>] dt: DateTime) =
        Assert.IsInRange(DateTime(2000, 1, 1), DateTime(2010, 1, 1), dt, "The date lies in the default range.")
        Assert.AreEqual(DateTimeKind.Utc, dt.Kind, "The date is in UTC.")

    [<Property>]
    member _.``DateTimeOffset generates valid dates`` ([<DateTimeOffset>] dto: DateTimeOffset) =
        Assert.IsGreaterThanOrEqualTo(DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), dto, "The date lies in the default range.")

    [<Property>]
    member _.``DateOnly generates dates of the default range`` ([<DateOnly>] date: DateOnly) =
        Assert.IsInRange(DateOnly(2000, 1, 1), DateOnly(2009, 12, 29), date, "The date lies in the 3650 days from 2000-01-01.")

    [<Property>]
    member _.``DateOnly generates dates of the given range`` ([<DateOnly(2024, 2, 28, 2024, 3, 1)>] date: DateOnly) =
        Assert.IsInRange(DateOnly(2024, 2, 28), DateOnly(2024, 3, 1), date, "The date lies in the range.")

    [<TestMethod>]
    member _.``DateOnly generates every date of its range, both ends included`` () =
        let generated =
            DateOnlyAttribute(2024, 2, 28, 2024, 3, 1).Generator
            |> Gen.sample 10 200
            |> Seq.distinct
            |> Seq.sort
            |> Seq.toArray

        CollectionAssert.AreEqual(
            [| DateOnly(2024, 2, 28); DateOnly(2024, 2, 29); DateOnly(2024, 3, 1) |],
            generated,
            "200 values of a range of three dates hold each of them and nothing else."
        )

    [<Property>]
    member _.``TimeOnly generates times of the given range`` ([<TimeOnly(9, 0, 17, 30)>] time: TimeOnly) =
        Assert.IsInRange(TimeOnly(9, 0), TimeOnly(17, 30), time, "The time lies in the range.")

    [<TestMethod>]
    member _.``TimeOnly generates every time of its range, both ends included`` () =
        // The constructor that derived attributes call: three values, 100 nanoseconds apart
        let generated =
            TimeOnlyAttribute(TimeOnly(0L), TimeOnly(2L)).Generator
            |> Gen.sample 10 200
            |> Seq.distinct
            |> Seq.sort
            |> Seq.toArray

        CollectionAssert.AreEqual(
            [| TimeOnly(0L); TimeOnly(1L); TimeOnly(2L) |],
            generated,
            "200 values of a range of three times hold each of them and nothing else."
        )

    [<TestMethod>]
    member _.``TimeOnly without a range generates times of the whole day at full precision`` () =
        let generated = TimeOnlyAttribute().Generator |> Gen.sample 99 200 |> Seq.toArray
        Assert.IsTrue(generated |> Array.exists (fun time -> time.Hour < 12), "Some of 200 times lie before noon.")
        Assert.IsTrue(generated |> Array.exists (fun time -> time.Hour >= 12), "Some of 200 times lie after noon.")

        Assert.IsTrue(
            generated |> Array.exists (fun time -> time.Ticks % TimeSpan.TicksPerSecond <> 0L),
            "A time has the precision of TimeOnly, not whole seconds."
        )
