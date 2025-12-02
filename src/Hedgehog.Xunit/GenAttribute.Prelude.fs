namespace Hedgehog.Xunit

open System
open Hedgehog
open Hedgehog.FSharp

[<AutoOpen>]
module private RangeHelpers =
    /// Choose between constant, linear, and exponential range based on the range size.
    /// - For ranges > 1000: use exponential to ensure boundary values are tested
    /// - For ranges > 100: use linear for balanced shrinking
    /// - For ranges <= 100: use constant (no shrinking needed for small ranges)
    let inline chooseRangeInt32 (min: int) (max: int) : Range<int> =
        let rangeSize = int64 max - int64 min
        let origin = if min <= 0 && 0 <= max then 0 else min
        match rangeSize with
        | size when size > 1000L -> Range.exponentialFrom origin min max
        | size when size > 100L -> Range.linearFrom origin min max
        | _ -> Range.constantFrom origin min max

/// <summary>Generates an integer within a specified range.</summary>
/// <remarks>Range strategy: exponential (>1000), linear (>100), or constant (≤100).</remarks>
type IntAttribute(min: int, max: int) =
    inherit GenAttribute<int>()
    /// <summary>Generates an integer from Int32.MinValue to Int32.MaxValue.</summary>
    /// <remarks>Range strategy: exponential (>1000), linear (>100), or constant (≤100).</remarks>
    new() = IntAttribute(Int32.MinValue, Int32.MaxValue)
    override _.Generator =
        Gen.int32 (chooseRangeInt32 min max)

/// <summary>Generates an odd integer.</summary>
/// <remarks>Range strategy: exponential (>1000), linear (>100), or constant (≤100).</remarks>
type OddAttribute(min: int, max: int) =
    inherit GenAttribute<int>()
    /// <summary>Generates an odd integer from Int32.MinValue to Int32.MaxValue.</summary>
    /// <remarks>Range strategy: exponential (>1000), linear (>100), or constant (≤100).</remarks>
    new() = OddAttribute(Int32.MinValue, Int32.MaxValue)
    override _.Generator = gen {
        let! n = Gen.int32 (chooseRangeInt32 min max)
        return n ||| 1
    }

/// <summary>Generates an even integer.</summary>
/// <remarks>Range strategy: exponential (>1000), linear (>100), or constant (≤100).</remarks>
type EvenAttribute(min: int, max: int) =
    inherit GenAttribute<int>()
    /// <summary>Generates an even integer from Int32.MinValue to Int32.MaxValue.</summary>
    /// <remarks>Range strategy: exponential (>1000), linear (>100), or constant (≤100).</remarks>
    new() = EvenAttribute(Int32.MinValue, Int32.MaxValue)
    override _.Generator = gen {
        let! n = Gen.int32 (chooseRangeInt32 min max)
        return n &&& ~~~1
    }

/// <summary>Generates a positive integer.</summary>
/// <remarks>Range strategy: exponential (>1000), linear (>100), or constant (≤100).</remarks>
type PositiveIntAttribute(max: int) =
    inherit GenAttribute<int>()
    /// <summary>Generates a positive integer from 1 to Int32.MaxValue.</summary>
    /// <remarks>Range strategy: exponential (>1000), linear (>100), or constant (≤100).</remarks>
    new() = PositiveIntAttribute(Int32.MaxValue)
    override _.Generator =
        Gen.int32 (chooseRangeInt32 1 max)

/// <summary>Generates a non-negative integer.</summary>
/// <remarks>Range strategy: exponential (>1000), linear (>100), or constant (≤100).</remarks>
type NonNegativeIntAttribute(max: int) =
    inherit GenAttribute<int>()
    /// <summary>Generates a non-negative integer from 0 to Int32.MaxValue.</summary>
    /// <remarks>Range strategy: exponential (>1000), linear (>100), or constant (≤100).</remarks>
    new() = NonNegativeIntAttribute(Int32.MaxValue)
    override _.Generator =
        Gen.int32 (chooseRangeInt32 0 max)

/// <summary>Generates a non-zero integer.</summary>
/// <remarks>Range strategy: exponential (>1000), linear (>100), or constant (≤100).</remarks>
type NonZeroIntAttribute(min: int, max: int) =
    inherit GenAttribute<int>()
    /// <summary>Generates a non-zero integer from Int32.MinValue+1 to Int32.MaxValue.</summary>
    /// <remarks>Range strategy: exponential (>1000), linear (>100), or constant (≤100).</remarks>
    new() = NonZeroIntAttribute(Int32.MinValue + 1, Int32.MaxValue)
    override _.Generator =
        match min, max with
        | _, m when m < 0 -> Gen.int32 (chooseRangeInt32 min max)  // Range entirely negative
        | n, _ when n > 0 -> Gen.int32 (chooseRangeInt32 min max)  // Range entirely positive
        | n, m ->                                                   // 0 is in range, split it
            Gen.choice [
                Gen.int32 (chooseRangeInt32 n -1)
                Gen.int32 (chooseRangeInt32 1 m)
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
type Ipv4AddressAttribute() =
    inherit GenAttribute<System.Net.IPAddress>()
    override _.Generator =
        Gen.ipv4Address

/// Generates an IPv6 address.
type Ipv6AddressAttribute() =
    inherit GenAttribute<System.Net.IPAddress>()
    override _.Generator =
        Gen.ipv6Address
