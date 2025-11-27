# Generator Attributes

`Hedgehog.Xunit` provides a collection of built-in attributes that make it easy to generate test data for your property-based tests. These attributes inherit from `GenAttribute<'T>` and can be applied directly to test method parameters.

## Why Use Generator Attributes?

Generator attributes provide a declarative, reusable way to specify test data generation. They offer several key benefits:

**Readability and Intent**: Attributes make your test's data requirements immediately visible in the method signature. Instead of scrolling through generator setup code, you can see at a glance that a parameter needs to be a positive integer or a valid email address.

**Reusability**: Define a generator once as an attribute, then reuse it across multiple tests. This eliminates duplication and ensures consistency in how you generate specific types of data throughout your test suite.

**Composability**: Combine multiple attributes in a single test to generate complex test scenarios. Each parameter can have its own specialized generator without cluttering your test logic.

**Discoverability**: Built-in attributes serve as documentation, showing what kinds of constrained data generation are available. When you need a specific type of data, you can often find an existing attribute rather than writing a custom generator.

**Separation of Concerns**: Keep your test logic focused on the property being tested, while data generation concerns are handled declaratively through attributes.

## When to Create Custom Attributes

Consider creating your own `GenAttribute` when you:
- Need to generate domain-specific types (e.g., valid credit card numbers, postal codes, product SKUs)
- Have generation logic that's reused across multiple tests
- Want to make test signatures more self-documenting
- Need parameterized generators with sensible defaults

## Basic Usage

Instead of manually creating generators, you can use attributes to declaratively specify what kind of data you want:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog.Xunit
open Xunit

[<Property>]
let ``reversing a list twice returns the original`` ([<PositiveInt>] length: int) =
    let list = List.init length id
    let reversed = List.rev (List.rev list)
    list = reversed
```

# [C#](#tab/csharp)

```csharp
using Hedgehog.Xunit;
using Xunit;

public class ListTests
{
    [Property]
    public bool ReversingListTwiceReturnsOriginal([PositiveInt] int length)
    {
        var list = Enumerable.Range(0, length).ToList();
        var reversed = list.AsEnumerable().Reverse().Reverse().ToList();
        return list.SequenceEqual(reversed);
    }
}
```

---

## Available Attributes

### Numeric Attributes

#### `Odd` and `Even` Attributes
Generate odd or even integers within a specified range.

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``odd numbers are not divisible by 2`` ([<Odd>] n: int) =
    n % 2 <> 0

[<Property>]
let ``even numbers are divisible by 2`` ([<Even>] n: int) =
    n % 2 = 0

// With custom range
[<Property>]
let ``small odd numbers`` ([<Odd(1, 100)>] n: int) =
    n >= 1 && n <= 100 && n % 2 <> 0
```

# [C#](#tab/csharp)

```csharp
[Property]
public bool OddNumbersAreNotDivisibleBy2([Odd] int n)
{
    return n % 2 != 0;
}

[Property]
public bool EvenNumbersAreDivisibleBy2([Even] int n)
{
    return n % 2 == 0;
}

// With custom range
[Property]
public bool SmallOddNumbers([Odd(1, 100)] int n)
{
    return n >= 1 && n <= 100 && n % 2 != 0;
}
```

---

#### `PositiveInt` Attribute
Generates positive integers (greater than 0).

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``positive integers are greater than zero`` ([<PositiveInt>] n: int) =
    n > 0

// With custom maximum
[<Property>]
let ``small positive integers`` ([<PositiveInt(100)>] n: int) =
    n > 0 && n <= 100
```

# [C#](#tab/csharp)

```csharp
[Property]
public bool PositiveIntegersAreGreaterThanZero([PositiveInt] int n)
{
    return n > 0;
}

// With custom maximum
[Property]
public bool SmallPositiveIntegers([PositiveInt(100)] int n)
{
    return n > 0 && n <= 100;
}
```

---

#### `NonNegativeInt` Attribute
Generates non-negative integers (greater than or equal to 0).

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``array length is non-negative`` ([<NonNegativeInt>] length: int) =
    let arr = Array.zeroCreate length
    arr.Length >= 0
```

# [C#](#tab/csharp)

```csharp
[Property]
public bool ArrayLengthIsNonNegative([NonNegativeInt] int length)
{
    var arr = new int[length];
    return arr.Length >= 0;
}
```

---

#### `NonZeroInt` Attribute
Generates integers that are not zero.

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``division by non-zero is safe`` ([<NonZeroInt>] divisor: int) =
    let result = 100 / divisor
    true // Won't throw DivideByZeroException
```

# [C#](#tab/csharp)

```csharp
[Property]
public bool DivisionByNonZeroIsSafe([NonZeroInt] int divisor)
{
    var result = 100 / divisor;
    return true; // Won't throw DivideByZeroException
}
```

---

### String Attributes

#### `Identifier` Attribute
Generates valid programming identifiers.

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``identifiers start with letter or underscore`` ([<Identifier>] id: string) =
    let firstChar = id.[0]
    Char.IsLetter(firstChar) || firstChar = '_'

// With custom max length
[<Property>]
let ``short identifiers`` ([<Identifier(10)>] id: string) =
    id.Length <= 10
```

# [C#](#tab/csharp)

```csharp
[Property]
public bool IdentifiersStartWithLetterOrUnderscore([Identifier] string id)
{
    var firstChar = id[0];
    return char.IsLetter(firstChar) || firstChar == '_';
}

// With custom max length
[Property]
public bool ShortIdentifiers([Identifier(10)] string id)
{
    return id.Length <= 10;
}
```

---

#### `LatinName` Attribute
Generates human-readable Latin names.

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``names are capitalized`` ([<LatinName>] name: string) =
    Char.IsUpper(name.[0])
```

# [C#](#tab/csharp)

```csharp
[Property]
public bool NamesAreCapitalized([LatinName] string name)
{
    return char.IsUpper(name[0]);
}
```

---

#### `SnakeCase` and `KebabCase` Attributes
Generate strings in snake_case or kebab-case format.

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``snake case uses underscores`` ([<SnakeCase>] s: string) =
    not (s.Contains("-"))

[<Property>]
let ``kebab case uses hyphens`` ([<KebabCase>] s: string) =
    not (s.Contains("_"))

// With custom parameters (maxWordLength, maxWordsCount)
[<Property>]
let ``short snake case`` ([<SnakeCase(3, 2)>] s: string) =
    true
```

# [C#](#tab/csharp)

```csharp
[Property]
public bool SnakeCaseUsesUnderscores([SnakeCase] string s)
{
    return !s.Contains("-");
}

[Property]
public bool KebabCaseUsesHyphens([KebabCase] string s)
{
    return !s.Contains("_");
}

// With custom parameters (maxWordLength, maxWordsCount)
[Property]
public bool ShortSnakeCase([SnakeCase(3, 2)] string s)
{
    return true;
}
```

---

#### `AlphaNumString` Attribute
Generates strings containing only alphanumeric characters.

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``alphanumeric strings contain no special chars`` ([<AlphaNumString>] s: string) =
    s |> Seq.forall Char.IsLetterOrDigit

// With length constraints (minLength, maxLength)
[<Property>]
let ``bounded alphanumeric`` ([<AlphaNumString(5, 20)>] s: string) =
    s.Length >= 5 && s.Length <= 20
```

# [C#](#tab/csharp)

```csharp
[Property]
public bool AlphanumericStringsContainNoSpecialChars([AlphaNumString] string s)
{
    return s.All(char.IsLetterOrDigit);
}

// With length constraints (minLength, maxLength)
[Property]
public bool BoundedAlphanumeric([AlphaNumString(5, 20)] string s)
{
    return s.Length >= 5 && s.Length <= 20;
}
```

---

#### `UnicodeString` Attribute
Generates strings with Unicode characters.

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``unicode strings can contain emoji`` ([<UnicodeString(1, 50)>] s: string) =
    s.Length >= 1 && s.Length <= 50
```

# [C#](#tab/csharp)

```csharp
[Property]
public bool UnicodeStringsCanContainEmoji([UnicodeString(1, 50)] string s)
{
    return s.Length >= 1 && s.Length <= 50;
}
```

---

### Network Attributes

#### `DomainName` Attribute
Generates valid domain names.

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``domain names contain dots`` ([<DomainName>] domain: string) =
    domain.Contains(".")
```

# [C#](#tab/csharp)

```csharp
[Property]
public bool DomainNamesContainDots([DomainName] string domain)
{
    return domain.Contains(".");
}
```

---

#### `Email` Attribute
Generates valid email addresses.

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``emails contain @ symbol`` ([<Email>] email: string) =
    email.Contains("@")
```

# [C#](#tab/csharp)

```csharp
[Property]
public bool EmailsContainAtSymbol([Email] string email)
{
    return email.Contains("@");
}
```

---

#### `IpAddress` and `Ipv6Address` Attributes
Generate IP addresses (IPv4 or IPv6).

# [F#](#tab/fsharp)

```fsharp
open System.Net

[<Property>]
let ``IPv4 addresses are valid`` ([<IpAddress>] ip: IPAddress) =
    ip.AddressFamily = Sockets.AddressFamily.InterNetwork

[<Property>]
let ``IPv6 addresses are valid`` ([<Ipv6Address>] ip: IPAddress) =
    ip.AddressFamily = Sockets.AddressFamily.InterNetworkV6
```

# [C#](#tab/csharp)

```csharp
using System.Net;
using System.Net.Sockets;

[Property]
public bool IPv4AddressesAreValid([IpAddress] IPAddress ip)
{
    return ip.AddressFamily == AddressFamily.InterNetwork;
}

[Property]
public bool IPv6AddressesAreValid([Ipv6Address] IPAddress ip)
{
    return ip.AddressFamily == AddressFamily.InterNetworkV6;
}
```

---

### Date and Time Attributes

#### `DateTime` Attribute
Generates DateTime values.

# [F#](#tab/fsharp)

```fsharp
open System

[<Property>]
let ``dates are in default range`` ([<DateTime>] dt: DateTime) =
    dt >= DateTime(2000, 1, 1) && dt < DateTime(2010, 1, 1)

// With custom range (from, duration)
[<Property>]
let ``recent dates`` ([<DateTime(DateTime(2020, 1, 1), TimeSpan.FromDays(365))>] dt: DateTime) =
    dt.Year >= 2020

// With specific DateTimeKind
[<Property>]
let ``UTC dates`` ([<DateTime(DateTimeKind.Utc)>] dt: DateTime) =
    dt.Kind = DateTimeKind.Utc
```

# [C#](#tab/csharp)

```csharp
using System;

[Property]
public bool DatesAreInDefaultRange([DateTime] DateTime dt)
{
    return dt >= new DateTime(2000, 1, 1) && dt < new DateTime(2010, 1, 1);
}

// With custom range (from, duration)
[Property]
public bool RecentDates([DateTime(typeof(DateTime), "2020-01-01", "365.00:00:00")] DateTime dt)
{
    return dt.Year >= 2020;
}

// With specific DateTimeKind
[Property]
public bool UtcDates([DateTime(DateTimeKind.Utc)] DateTime dt)
{
    return dt.Kind == DateTimeKind.Utc;
}
```

---

#### `DateTimeOffset` Attribute
Generates DateTimeOffset values.

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``date offsets preserve timezone`` ([<DateTimeOffset>] dto: DateTimeOffset) =
    dto.Offset.TotalHours >= -14.0 && dto.Offset.TotalHours <= 14.0
```

# [C#](#tab/csharp)

```csharp
[Property]
public bool DateOffsetsPreserveTimezone([DateTimeOffset] DateTimeOffset dto)
{
    return dto.Offset.TotalHours >= -14.0 && dto.Offset.TotalHours <= 14.0;
}
```

---

## Combining Multiple Attributes

You can use multiple attributes in a single test to generate different types of data:

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``user registration with generated data`` 
    ([<Identifier>] username: string)
    ([<Email>] email: string)
    ([<PositiveInt(150)>] age: int)
    ([<LatinName>] firstName: string)
    ([<LatinName>] lastName: string) =
    
    // Test user registration logic
    let user = createUser username email age firstName lastName
    user.Username = username &&
    user.Email = email &&
    user.Age > 0
```

# [C#](#tab/csharp)

```csharp
[Property]
public bool UserRegistrationWithGeneratedData(
    [Identifier] string username,
    [Email] string email,
    [PositiveInt(150)] int age,
    [LatinName] string firstName,
    [LatinName] string lastName)
{
    // Test user registration logic
    var user = CreateUser(username, email, age, firstName, lastName);
    return user.Username == username &&
           user.Email == email &&
           user.Age > 0;
}
```

---

## Creating Custom Attributes

You can create your own generator attributes by inheriting from `GenAttribute<'T>`:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

/// Generates a valid US phone number
type UsPhoneNumberAttribute() =
    inherit GenAttribute<string>()
    override _.Generator = gen {
        let! areaCode = Gen.int32 (Range.constant 200 999)
        let! exchange = Gen.int32 (Range.constant 200 999)
        let! number = Gen.int32 (Range.constant 0 9999)
        return sprintf "(%03d) %03d-%04d" areaCode exchange number
    }

[<Property>]
let ``phone numbers are formatted correctly`` ([<UsPhoneNumber>] phone: string) =
    phone.Length = 14 && phone.[0] = '(' && phone.[4] = ')'
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;
using Range = Hedgehog.Linq.Range;

/// <summary>Generates a valid US phone number</summary>
public class UsPhoneNumberAttribute : GenAttribute<string>
{
    public override Gen<string> Generator =>
        from areaCode in Gen.Int32(Range.Constant(200, 999))
        from exchange in Gen.Int32(Range.Constant(200, 999))
        from number in Gen.Int32(Range.Constant(0, 9999))
        select $"({areaCode:000}) {exchange:000}-{number:0000}";
}

[Property]
public bool PhoneNumbersAreFormattedCorrectly([UsPhoneNumber] string phone)
{
    return phone.Length == 14 && phone[0] == '(' && phone[4] == ')';
}
```

---

## Best Practices

1. **Use Specific Attributes**: Choose the most specific attribute for your needs (e.g., `[<PositiveInt>]` instead of `[<NonNegativeInt>]` when you need values > 0)

2. **Set Reasonable Ranges**: Use constructor parameters to limit ranges to realistic values for your domain

3. **Combine with Property Attribute**: Always use generator attributes with `[<Property>]` or `[<Properties>]` attributes

4. **Document Custom Attributes**: When creating custom attributes, add XML documentation to explain what they generate
