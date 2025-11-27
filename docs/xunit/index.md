# xUnit Integration

The `Hedgehog.Xunit` package provides seamless integration with xUnit, making it easy to write property-based tests that fit naturally into your existing xUnit test suite.

> [!NOTE]
> We integrate with [xUnit V3](https://xunit.net/docs/getting-started/v3/getting-started), so please make sure that your test project
> references [xunit.v3](https://www.nuget.org/packages/xunit.v3) nuget package the older `xunit`.

## Installation

Install Hedgehog.Xunit from NuGet:

```bash
dotnet add package Hedgehog.Xunit.v3
```

## Quick Start

The simplest way to write a property test with xUnit is to use the `[<Property>]` attribute:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Xunit

[<Property>]
let ``reverse twice is identity`` (xs: int list) =
    List.rev (List.rev xs) = xs
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;

public class MyTests
{
    [Property]
    public bool ReverseTwiceIsIdentity(List<int> xs)
    {
        return xs.AsEnumerable().Reverse().Reverse().SequenceEqual(xs);
    }
}
```

---

The `Property` attribute automatically:
- Generates test data for all parameters
- Runs the property 100 times by default
- Shrinks failing cases to minimal examples
- Reports results through xUnit's test runner

## Property Return Types

Properties can return various types to indicate success or failure:

**Supported return types:**
- `bool` - `false` indicates failure
- `unit` / `void` - any exception indicates failure  
- `Result<'T, 'Error>` (F#) - `Error` case indicates failure
- `Property<unit>` / `Property<bool>` (F#) - for using the `property` computation expression
- `Task`, `Task<bool>`, `Task<T>` - async versions of the above
- `Async<unit>`, `Async<bool>`, `Async<Result<'T, 'Error>>` (F#) - F# async versions
