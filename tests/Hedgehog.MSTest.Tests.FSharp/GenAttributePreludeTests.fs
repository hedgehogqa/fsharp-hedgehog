namespace Hedgehog.MSTest.Tests

open System
open System.Net
open System.Net.Sockets
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
