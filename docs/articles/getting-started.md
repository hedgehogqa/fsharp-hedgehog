# Getting Started

This guide will help you write your first property-based tests with Hedgehog in just a few minutes.

## Installation

Install Hedgehog from NuGet:

```bash
dotnet add package Hedgehog
```

## Your First Property Test

Let's test a simple property: reversing a list twice should give you the original list.

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

let propReverse =
    property {
        let! xs = Gen.list (Range.linear 0 100) Gen.alpha
        return List.rev (List.rev xs) = xs
    }

// Run it
Property.check propReverse
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

var propReverse =
    from xs in Gen.Alpha.List(Range.LinearInt32(0, 100)).ForAll()
    select xs.Reverse().Reverse().SequenceEqual(xs);

// Run it
propReverse.Check();
```

---

Run this and you'll see:

```
+++ OK, passed 100 tests.
```

## Understanding What Happened

1. **Generator**: `Gen.list` (or `Gen.Alpha.List`) created random lists of characters
2. **Range**: `Range.linear 0 100` specified lists can have 0 to 100 elements
3. **Property**: The `return` statement (or `select`) defines what should be true for all inputs
4. **Testing**: Hedgehog generated 100 random lists and verified the property held for each

## Writing Better Properties

Good properties describe **invariants** - things that should always be true. Here are some patterns:

**Inverse operations**: `f(g(x)) = x`
```fsharp
encode(decode(x)) = x
serialize(deserialize(x)) = x
```

**Commutative operations**: `f(a, b) = f(b, a)`
```fsharp
x + y = y + x
min(a, b) = min(b, a)
```

**Idempotence**: `f(f(x)) = f(x)`
```fsharp
sort(sort(xs)) = sort(xs)
distinct(distinct(xs)) = distinct(xs)
```

**Preservation**: Properties that don't change
```fsharp
length(reverse(xs)) = length(xs)
sum(map(f, xs)) = map(f, sum(xs))  // if f is linear
```

## Seeing Shrinking in Action

Let's introduce a bug and see how Hedgehog finds the minimal failing case:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

// Buggy function - fails for numbers > 100
let tryAdd a b =
    if a > 100 then None
    else Some(a + b)

let propAdd =
    property {
        let! a = Gen.int32 (Range.constantBounded ())
        let! b = Gen.int32 (Range.constantBounded ())
        return tryAdd a b = Some(a + b)
    }

Property.check propAdd
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

// Buggy function - fails for numbers > 100
int? TryAdd(int a, int b)
{
    if (a > 100) return null;
    return a + b;
}

var propAdd =
    from a in Gen.Int32(Range.ConstantBoundedInt32()).ForAll()
    from b in Gen.Int32(Range.ConstantBoundedInt32()).ForAll()
    select TryAdd(a, b) == a + b;

propAdd.Check();
```

---

Output:

```
*** Failed! Falsifiable (after 16 tests and 5 shrinks):
101
0
```

Notice Hedgehog didn't just find *a* failing case - it found the **smallest** one: `a = 101, b = 0`. This is automatic shrinking in action.

## Integration with Test Frameworks

### xUnit

> [!TIP]
> Hedgehog provides integrated support for xUnit v3 in [Hedgehog.Xunit](../xunit/) package.

# [F#](#tab/fsharp)

```fsharp
open Xunit
open Hedgehog
open Hedgehog.FSharp

[<Fact>]
let ``reverse twice is identity`` () =
    property {
        let! xs = Gen.list (Range.linear 0 100) Gen.alpha
        return List.rev (List.rev xs) = xs
    }
    |> Property.check
```

# [C#](#tab/csharp)

```csharp
using Xunit;
using Hedgehog;
using Hedgehog.Linq;

[Fact]
public void ReverseTwiceIsIdentity()
{
    var property =
        from xs in Gen.Alpha.List(Range.LinearInt32(0, 100)).ForAll()
        select xs.Reverse().Reverse().SequenceEqual(xs);
    
    property.Check();
}
```

---

### NUnit

# [F#](#tab/fsharp)

```fsharp
open NUnit.Framework
open Hedgehog
open Hedgehog.FSharp

[<Test>]
let ``reverse twice is identity`` () =
    property {
        let! xs = Gen.list (Range.linear 0 100) Gen.alpha
        return List.rev (List.rev xs) = xs
    }
    |> Property.check
```

# [C#](#tab/csharp)

```csharp
using NUnit.Framework;
using Hedgehog;
using Hedgehog.Linq;

[Test]
public void ReverseTwiceIsIdentity()
{
    var property =
        from xs in Gen.Alpha.List(Range.LinearInt32(0, 100)).ForAll()
        select xs.Reverse().Reverse().SequenceEqual(xs);
    
    property.Check();
}
```

---

### Expecto (F# only)

```fsharp
open Expecto
open Hedgehog
open Hedgehog.FSharp

let tests = testList "properties" [
    testCase "reverse twice is identity" <| fun _ ->
        property {
            let! xs = Gen.list (Range.linear 0 100) Gen.alpha
            return List.rev (List.rev xs) = xs
        }
        |> Property.check
]

[<EntryPoint>]
let main args = runTestsWithArgs defaultConfig args tests
```

## Building Custom Generators

You can compose generators to create complex test data:

# [F#](#tab/fsharp)

```fsharp
open System.Net
open Hedgehog
open Hedgehog.FSharp

// Generator for IP addresses
let ipAddressGen = gen {
    let! bytes = Gen.array (Range.singleton 4) Gen.byte
    return IPAddress bytes
}

// Generator for valid email-like strings
let emailGen = gen {
    let! name = Gen.string (Range.linear 1 20) Gen.alphaNum
    let! domain = Gen.item ["com"; "net"; "org"]
    return $"{name}@example.{domain}"
}

// Use them in properties
property {
    let! ip = ipAddressGen
    let! email = emailGen
    return ip.ToString().Length > 0 && email.Contains("@")
}
|> Property.check
```

# [C#](#tab/csharp)

```csharp
using System.Net;
using Hedgehog;
using Hedgehog.Linq;

// Generator for IP addresses
var ipAddressGen =
    from bytes in Gen.Byte.Array(Range.Singleton(4))
    select new IPAddress(bytes);

// Generator for valid email-like strings
var emailGen =
    from name in Gen.AlphaNum.String(Range.LinearInt32(1, 20))
    from domain in Gen.Item("com", "net", "org")
    select $"{name}@example.{domain}";

// Use them in properties
var property =
    from ip in ipAddressGen.ForAll()
    from email in emailGen.ForAll()
    select ip.ToString().Length > 0 && email.Contains("@");

property.Check();
```

---

## Auto-Generating Test Data

Hedgehog can automatically generate test data for your types:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

type User = {
    Name: string
    Age: int
}

property {
    let! user = Gen.auto<User>
    return user.Age >= 0
}
|> Property.check
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

public record User(string Name, int Age);

var property =
    from user in Gen.Auto<User>().ForAll()
    select user.Age >= 0;

property.Check();
```

---

Auto-generation works with records, unions, tuples, classes, and collections. For advanced customization and registering custom generators, see the [Auto-Generation Guide](auto-generation.md).

## Testing Asynchronous Functions

Hedgehog provides first-class support for testing asynchronous code using F# `async` computations and C# `Task`/`Task<T>`. You can bind async operations directly in the `property` computation expression or LINQ query syntax.

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

property {
    let! x = Gen.int32 (Range.constant 0 100)
    let! y = async { return x + 1 }
    return y = x + 1
}
|> Property.checkBoolAsync
```

You can mix `async` and `task`:

```fsharp
open System.Threading.Tasks

property {
    let! x = Gen.int32 (Range.constant 0 100)
    let! y = async { return x + 1 }
    let! z = task { return y * 2 }
    return z = (x + 1) * 2
}
|> Property.checkBoolAsync
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;
using System.Threading.Tasks;

var property =
    from x in Gen.Int32(Range.Constant(0, 100)).ForAll()
    from y in Task.FromResult(x + 1)
    select y == x + 1;

property.CheckAsync();
```

---

## Controlling Test Runs

You can customize how many tests to run and other settings:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

let config =
    PropertyConfig.defaults
    |> PropertyConfig.withTests 1000<tests>

property {
    let! xs = Gen.list (Range.linear 0 100) Gen.alpha
    return List.rev (List.rev xs) = xs
}
|> Property.checkWith config
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

var config = PropertyConfig.Defaults
    .WithTests(PropertyConfig.Test.Create(1000));

var property =
    from xs in Gen.Alpha.List(Range.LinearInt32(0, 100)).ForAll()
    select xs.Reverse().Reverse().SequenceEqual(xs);

property.Check(config);
```

---

## Common Patterns

### Re-running Failing Tests

When a property fails, Hedgehog provides a seed that you can use to reproduce the exact failure:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

let prop = property {
    let! x = Gen.int32 (Range.constant 0 1000)
    return x < 500  // Will fail eventually
}

// First run - will fail and show a seed
Property.check prop

// Example output:
// *** Failed! Falsifiable (after 6 tests and 1 shrink):
// 500
// This failure can be reproduced by running:
// > Property.recheck "1a2b3c4d" prop

// Recheck with the specific seed
Property.recheck "1a2b3c4d" prop
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

var prop =
    from x in Gen.Int32(Range.Constant(0, 1000)).ForAll()
    select x < 500;  // Will fail eventually

// First run - will fail and show a seed
prop.Check();

// Example output:
// *** Failed! Falsifiable (after 6 tests and 1 shrink):
// 500
// This failure can be reproduced by running:
// > prop.Recheck("1a2b3c4d")

// Recheck with the specific seed
prop.Recheck("1a2b3c4d");
```

---

This is especially useful when:
- Debugging intermittent failures
- Sharing reproducible test cases with teammates
- Verifying a bug fix works for the specific failing case


### Adding Debug Information

Use `counterexample` to add context when tests fail:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

property {
    let! x = Gen.int32 (Range.constant 0 1000)
    let! y = Gen.int32 (Range.constant -100 1000)
    counterexample $"x = {x}, y = {y}, x * y = {x * y}"
    return x * y >= 0
}
|> Property.check
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

var property =
    from x in Gen.Int32(Range.Constant(0, 1000)).ForAll()
    from y in Gen.Int32(Range.Constant(-100, 1000)).ForAll()
    from _ in Property.CounterExample(() => $"x = {x}, y = {y}, x * y = {x * y}")
    select x * y >= 0;

property.Check();
```

---

```
*** Failed! Falsifiable (after 20 tests and 11 shrinks):
1
-100
x = 1, y = -100, x * y = -100
```

---

### Additional Resources

- **[Hedgehog.Xunit](https://github.com/dharmaturtle/fsharp-hedgehog-xunit)** - Enhanced xUnit integration with `[<Property>]` attributes

You're now ready to start finding bugs automatically with property-based testing!
