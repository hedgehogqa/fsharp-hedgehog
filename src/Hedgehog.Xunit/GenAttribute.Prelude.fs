namespace Hedgehog.Xunit

open System
open Hedgehog
open Hedgehog.FSharp

/// Generates an integer within a specified range.
type IntAttribute(min: int, max: int) =
    inherit GenAttribute<int>()
    new() = IntAttribute(Int32.MinValue, Int32.MaxValue)
    override _.Generator =
        Gen.int32 (Range.linear min max)

/// Generates an odd integer.
type OddAttribute(min: int, max: int) =
    inherit GenAttribute<int>()
    new() = OddAttribute(Int32.MinValue, Int32.MaxValue)
    override _.Generator = gen {
        let! n = Gen.int32 (Range.constant min max)
        return n ||| 1
    }

/// Generates an even integer.
type EvenAttribute(min: int, max: int) =
    inherit GenAttribute<int>()
    new() = EvenAttribute(Int32.MinValue, Int32.MaxValue)
    override _.Generator = gen {
        let! n = Gen.int32 (Range.constant min max)
        return n &&& ~~~1
    }

/// Generates a positive integer.
type PositiveIntAttribute(max: int) =
    inherit GenAttribute<int>()
    new() = PositiveIntAttribute(Int32.MaxValue)
    override _.Generator =
        Gen.int32 (Range.linear 1 max)

/// Generates a non-negative integer.
type NonNegativeIntAttribute(max: int) =
    inherit GenAttribute<int>()
    new() = NonNegativeIntAttribute(Int32.MaxValue)
    override _.Generator =
        Gen.int32 (Range.linear 0 max)

/// Generates a non-zero integer.
type NonZeroIntAttribute(min: int, max: int) =
    inherit GenAttribute<int>()
    new() = NonZeroIntAttribute(Int32.MinValue + 1, Int32.MaxValue)
    override _.Generator =
        Gen.choice [
            Gen.int32 (Range.linear min -1)
            Gen.int32 (Range.linear 1 max)
        ]

/// Generates a string that is a valid identifier.
type IdentifierAttribute(maxLen: int) =
    inherit GenAttribute<string>()
    new() = IdentifierAttribute(25)
    override _.Generator =
        Gen.identifier maxLen

/// Generates a string representing a Latin name.
type LatinNameAttribute(maxLength: int) =
    inherit GenAttribute<string>()
    new() = LatinNameAttribute(20)
    override _.Generator =
        Gen.latinName maxLength

/// Generates a string in snake_case.
type SnakeCaseAttribute(maxWordLength: int, maxWordsCount: int) =
    inherit GenAttribute<string>()
    new() = SnakeCaseAttribute(5, 5)
    override _.Generator =
        Gen.snakeCase (Range.constant 1 maxWordLength) (Range.constant 1 maxWordsCount)

/// Generates a string in kebab-case.
type KebabCaseAttribute(maxWordLength: int, maxWordsCount: int) =
    inherit GenAttribute<string>()
    new() = KebabCaseAttribute(5, 5)
    override _.Generator =
        Gen.kebabCase (Range.constant 1 maxWordLength) (Range.constant 1 maxWordsCount)

/// Generates a valid domain name.
type DomainNameAttribute() =
    inherit GenAttribute<string>()
    override _.Generator =
        Gen.domainName

/// Generates a valid email address.
type EmailAttribute() =
    inherit GenAttribute<string>()
    override _.Generator =
        Gen.email

/// Generates a DateTime value.
type DateTimeAttribute(kind: DateTimeKind, from: DateTime, duration: TimeSpan) =
    inherit GenAttribute<DateTime>()
    new() = DateTimeAttribute(DateTimeKind.Utc, DateTime(2000, 1, 1), TimeSpan.FromDays(3650))
    new(from, duration) = DateTimeAttribute(DateTimeKind.Utc, from, duration)
    new(kind) = DateTimeAttribute(kind, DateTime(2000, 1, 1), TimeSpan.FromDays(3650))
    override _.Generator =
        Gen.dateTime (Range.constant from (from + duration))
        |> Gen.map (fun x -> DateTime.SpecifyKind(x, kind))

/// Generates a DateTimeOffset value.
type DateTimeOffsetAttribute(from: DateTimeOffset, duration: TimeSpan) =
    inherit GenAttribute<DateTimeOffset>()
    new() = DateTimeOffsetAttribute(DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), TimeSpan.FromDays(3650))
    new(from) = DateTimeOffsetAttribute(from, TimeSpan.FromDays(3650))
    override _.Generator =
        Gen.dateTimeOffset (Range.constant from (from + duration))

/// Generates a string containing alphanumeric characters.
type AlphaNumStringAttribute(minLength: int, maxLength: int) =
    inherit GenAttribute<string>()
    new() = AlphaNumStringAttribute(0, 256)
    new(minLength) = AlphaNumStringAttribute(minLength, 256)
    override _.Generator =
        Gen.string (Range.constant minLength maxLength) Gen.alphaNum

/// Generates a string containing unicode characters.
type UnicodeStringAttribute(minLength: int, maxLength: int) =
    inherit GenAttribute<string>()
    new() = UnicodeStringAttribute(0, 256)
    new(minLength) = UnicodeStringAttribute(minLength, 256)
    override _.Generator =
        Gen.string (Range.constant minLength maxLength) Gen.unicode

/// Generates an IP address (IPv4).
type IpAddressAttribute() =
    inherit GenAttribute<System.Net.IPAddress>()
    override _.Generator =
        Gen.ipAddress

/// Generates an IPv6 address.
type Ipv6AddressAttribute() =
    inherit GenAttribute<System.Net.IPAddress>()
    override _.Generator =
        Gen.ipv6Address
