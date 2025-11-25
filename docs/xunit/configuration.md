# Configuration

This page covers how to configure property tests using the `Property` and `Properties` attributes.

## Test Parameters

### Number of Tests

Control how many test cases to run:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Xunit

[<Property(1000<tests>)>]
let ``run 1000 times`` (x: int) =
    x + 0 = x
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;

[Property(Tests = 1000)]
public bool Run1000Times(int x)
{
    return x + 0 == x;
}
```

---

### Number of Shrinks

Limit the number of shrinking attempts:

# [F#](#tab/fsharp)

```fsharp
[<Property(100<tests>, 50<shrinks>)>]
let ``limited shrinking`` (x: int) =
    x < 1000
```

# [C#](#tab/csharp)

```csharp
[Property(Tests = 100, Shrinks = 50)]
public bool LimitedShrinking(int x)
{
    return x < 1000;
}
```

---

### Size Parameter

Control the size of generated data:

# [F#](#tab/fsharp)

```fsharp
[<Property(Size = 50)>]
let ``with specific size`` (xs: int list) =
    xs.Length <= 50
```

# [C#](#tab/csharp)

```csharp
[Property(Size = 50)]
public bool WithSpecificSize(List<int> xs)
{
    return xs.Count <= 50;
}
```

---

## Class and Module Level Configuration

Use `[<Properties>]` to set defaults for all properties in a class or module:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Xunit

[<Properties(1000<tests>)>]
module ``Fast running tests`` =
    
    [<Property>]
    let ``uses 1000 tests`` (x: int) =
        x + 0 = x
    
    [<Property(500<tests>)>]
    let ``overrides with 500 tests`` (x: int) =
        x * 1 = x
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;

[Properties(Tests = 1000)]
public class FastRunningTests
{
    [Property]
    public bool Uses1000Tests(int x)
    {
        return x + 0 == x;
    }
    
    [Property(Tests = 500)]
    public bool OverridesWith500Tests(int x)
    {
        return x * 1 == x;
    }
}
```

---

Individual `[<Property>]` attributes can override settings from `[<Properties>]`.
