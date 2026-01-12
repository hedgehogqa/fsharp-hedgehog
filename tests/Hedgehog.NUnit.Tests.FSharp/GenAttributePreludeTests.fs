namespace Hedgehog.NUnit.Tests.FSharp

open NUnit.Framework
open Hedgehog.NUnit

[<TestFixture>]
type ``GenAttribute Prelude tests``() =

    [<Property>]
    member _.``Int attribute generates integers`` ([<Int(-10, 10)>] i: int) =
        Assert.That(i, Is.GreaterThanOrEqualTo(-10))
        Assert.That(i, Is.LessThanOrEqualTo(10))

    [<Property>]
    member _.``PositiveInt generates positive integers`` ([<PositiveInt>] i: int) =
        Assert.That(i, Is.GreaterThan(0))

    [<Property>]
    member _.``NonNegativeInt generates non-negative integers`` ([<NonNegativeInt>] i: int) =
        Assert.That(i, Is.GreaterThanOrEqualTo(0))

    [<Property>]
    member _.``NonZeroInt generates non-zero integers`` ([<NonZeroInt>] i: int) =
        Assert.That(i, Is.Not.EqualTo(0))

    [<Property>]
    member _.``OddAttribute generates odd integers`` ([<Odd>] i: int) =
        Assert.That(i % 2, Is.EqualTo(1).Or.EqualTo(-1))

    [<Property>]
    member _.``EvenAttribute generates even integers`` ([<Even>] i: int) =
        Assert.That(i % 2, Is.EqualTo(0))

    [<Property>]
    member _.``Email generates valid email addresses`` ([<Email>] email: string) =
        Assert.That(email, Does.Contain("@"))
        Assert.That(email, Does.Contain("."))

    [<Property>]
    member _.``DomainName generates valid domain names`` ([<DomainName>] domain: string) =
        Assert.That(domain, Does.Contain("."))
        Assert.That(domain, Is.Not.Empty)

    [<Property>]
    member _.``AlphaNumString generates alphanumeric strings`` ([<AlphaNumString(5, 20)>] s: string) =
        Assert.That(s.Length, Is.GreaterThanOrEqualTo(5))
        Assert.That(s.Length, Is.LessThanOrEqualTo(20))
        Assert.That(s, Does.Match("^[a-zA-Z0-9]*$"))

    [<Property>]
    member _.``Identifier generates valid identifiers`` ([<Identifier>] id: string) =
        Assert.That(id, Is.Not.Empty)
        // Identifiers should start with a letter or underscore
        Assert.That(System.Char.IsLetter(id.[0]) || id.[0] = '_', Is.True)

    [<Property>]
    member _.``LatinName generates Latin names`` ([<LatinName>] name: string) =
        Assert.That(name, Is.Not.Empty)
        Assert.That(System.Char.IsUpper(name.[0]), Is.True, "Latin names should start with uppercase")

    [<Property>]
    member _.``SnakeCase generates snake_case strings`` ([<SnakeCase>] s: string) =
        if s.Contains("_") then
            // Snake case can contain lowercase letters and numbers
            Assert.That(s, Does.Match("^[a-z0-9]+(_[a-z0-9]+)*$"))

    [<Property>]
    member _.``KebabCase generates kebab-case strings`` ([<KebabCase>] s: string) =
        if s.Contains("-") then
            // Kebab case can contain lowercase letters and numbers
            Assert.That(s, Does.Match("^[a-z0-9]+(-[a-z0-9]+)*$"))

    [<Property>]
    member _.``Ipv4Address generates valid IPv4 addresses`` ([<Ipv4Address>] ip: System.Net.IPAddress) =
        Assert.That(ip.AddressFamily, Is.EqualTo(System.Net.Sockets.AddressFamily.InterNetwork))

    [<Property>]
    member _.``Ipv6Address generates valid IPv6 addresses`` ([<Ipv6Address>] ip: System.Net.IPAddress) =
        Assert.That(ip.AddressFamily, Is.EqualTo(System.Net.Sockets.AddressFamily.InterNetworkV6))

    [<Property>]
    member _.``DateTime generates valid dates`` ([<DateTime>] dt: System.DateTime) =
        Assert.That(dt, Is.GreaterThanOrEqualTo(System.DateTime(2000, 1, 1)))
        Assert.That(dt, Is.LessThanOrEqualTo(System.DateTime(2010, 1, 1)))
        Assert.That(dt.Kind, Is.EqualTo(System.DateTimeKind.Utc))

    [<Property>]
    member _.``DateTimeOffset generates valid dates`` ([<DateTimeOffset>] dto: System.DateTimeOffset) =
        Assert.That(dto, Is.GreaterThanOrEqualTo(System.DateTimeOffset(2000, 1, 1, 0, 0, 0, System.TimeSpan.Zero)))
