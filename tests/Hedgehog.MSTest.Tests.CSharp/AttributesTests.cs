using System.Net;
using AwesomeAssertions;

namespace Hedgehog.MSTest.Tests.CSharp;

[TestClass]
public sealed class AttributesTests
{
    [Property]
    public void Odd_Attribute_Should_Generate_Odd_Numbers([Odd] int oddValue) =>
        Math.Abs(oddValue % 2).Should().Be(1);

    [Property]
    public void Even_Attribute_Should_Generate_Even_Numbers([Even] int evenValue) =>
        (evenValue % 2).Should().Be(0);

    [Property]
    public void PositiveInt_Attribute_Should_Generate_Positive_Numbers([PositiveInt] int value) =>
        value.Should().BeGreaterThan(0);

    [Property]
    public void NonNegativeInt_Attribute_Should_Generate_NonNegative_Numbers([NonNegativeInt] int value) =>
        value.Should().BeGreaterThanOrEqualTo(0);

    [Property]
    public void NonZeroInt_Attribute_Should_Never_Generate_Zero([NonZeroInt] int value) =>
        value.Should().NotBe(0);

    [Property]
    public void IntInRange_Attribute_Should_Generate_Values_Within_Range([Int(-50, 50)] int value) =>
        value.Should().BeInRange(-50, 50);

    [Property]
    public void AlphaNumericString_Attribute_Should_Generate_AlphaNumeric_Only([AlphaNumString] string value) =>
        value.Should().MatchRegex("^[a-zA-Z0-9]*$");

    [Property]
    public void AlphaNumericString_Attribute_Should_Respect_Length_Constraints(
        [AlphaNumString(minLength: 5, maxLength: 10)]
        string value) =>
        value.Length.Should().BeInRange(5, 10);

    [Property]
    public void UnicodeString_Attribute_Should_Respect_Length_Constraints(
        [UnicodeString(minLength: 3, maxLength: 8)]
        string value) =>
        value.Length.Should().BeInRange(3, 8);

    [Property]
    public void Identifier_Attribute_Should_Generate_Valid_Identifiers([Identifier] string identifier) =>
        identifier.Should().NotBeEmpty()
            .And
            .MatchRegex("^[a-z][a-zA-Z0-9_]*$",
                "identifier must start with lowercase letter and contain only alphanumeric or underscore");

    [Property]
    public void Email_Attribute_Should_Generate_Valid_Email_Format([Email] string email)
    {
        email.Should().Contain("@");
        var parts = email.Split('@');
        parts.Should().HaveCount(2);
        parts[0].Should().NotBeEmpty();
        parts[1].Should().NotBeEmpty();
        parts[1].Should().Contain(".");
    }

    [Property]
    public void LatinName_Attribute_Should_Generate_Valid_Names([LatinName] string name) =>
        name.Should().NotBeEmpty()
            .And.MatchRegex("^[A-Z][a-z]*$",
                "name must start with uppercase letter and contain only lowercase letters");

    [Property]
    public void DomainName_Attribute_Should_Generate_Valid_Domains([DomainName] string domain)
    {
        domain.Should().Contain(".");
        domain.Should().NotStartWith(".");
        domain.Should().NotEndWith(".");
        domain.Should().NotContain("..");
    }

    [Property]
    public void Ipv4Address_Attribute_Should_Generate_Valid_Ipv4([Ipv4Address] IPAddress ipv4) =>
        ipv4.AddressFamily.Should().Be(System.Net.Sockets.AddressFamily.InterNetwork);

    [Property]
    public void Ipv6Address_Attribute_Should_Generate_Valid_Ipv6([Ipv6Address] IPAddress ipv6) =>
        ipv6.AddressFamily.Should().Be(System.Net.Sockets.AddressFamily.InterNetworkV6);

    [Property]
    public void SnakeCase_Attribute_Should_Generate_Valid_SnakeCase([SnakeCase] string value) =>
        value.All(c => char.IsLower(c) || char.IsDigit(c) || c == '_').Should().BeTrue();

    [Property]
    public void KebabCase_Attribute_Should_Generate_Valid_KebabCase([KebabCase] string value) =>
        value.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-').Should().BeTrue();

    [Property]
    public void DateOnly_Attribute_Should_Generate_Dates_In_Default_Range([DateOnly] DateOnly date) =>
        (date >= new DateOnly(2000, 1, 1) && date <= new DateOnly(2009, 12, 29)).Should().BeTrue();

    [Property]
    public void DateOnly_Attribute_Should_Respect_Its_Range([DateOnly(2024, 2, 28, 2024, 3, 1)] DateOnly date) =>
        (date >= new DateOnly(2024, 2, 28) && date <= new DateOnly(2024, 3, 1)).Should().BeTrue();

    [Property]
    public void TimeOnly_Attribute_Should_Respect_Its_Range([TimeOnly(9, 0, 17, 30)] TimeOnly time) =>
        (time >= new TimeOnly(9, 0) && time <= new TimeOnly(17, 30)).Should().BeTrue();

}
