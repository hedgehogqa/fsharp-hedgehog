# Best Practices

This page provides tips for effective property-based testing with xUnit integration.

## Start Simple

Begin with simple `[<Property>]` attributes and auto-generated data. Only add custom configuration when you need it.

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Xunit

[<Property>]
let ``basic property`` (x: int) (y: int) =
    x + y = y + x
```

# [C#](#tab/csharp)

```csharp
using Hedgehog.Xunit;

public class BasicTests
{
    [Property]
    public bool BasicProperty(int x, int y)
    {
        return x + y == y + x;
    }
}
```

---

## Use Descriptive Test Names

Good test names describe the property being tested:

# [F#](#tab/fsharp)

```fsharp
[<Property>]
let ``addition is commutative`` (x: int) (y: int) =
    x + y = y + x

[<Property>]
let ``reversing twice gives original list`` (xs: int list) =
    List.rev (List.rev xs) = xs
```

# [C#](#tab/csharp)

```csharp
using Hedgehog.Xunit;
using System.Linq;

public class PropertyTests
{
    [Property]
    public bool AdditionIsCommutative(int x, int y)
    {
        return x + y == y + x;
    }

    [Property]
    public bool ReversingTwiceGivesOriginalList(int[] xs)
    {
        return xs.Reverse().Reverse().SequenceEqual(xs);
    }
}
```

---

## Use Module/Class Level Config for Related Tests

Group related tests with similar configuration needs. This is particularly useful for sharing `AutoGenConfig` settings or test counts across multiple related properties:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.Xunit

[<Properties(1000<tests>, AutoGenConfig = "autoGenConfig")>]
module ``Performance critical properties`` =
    
    let autoGenConfig = 
        AutoGenConfig.defaults
        |> AutoGenConfig.addGenerator (Gen.int32 (Range.constant 1 1000))
    
    [<Property>]
    let ``property 1`` (x: int) = x + 1 > x
    
    [<Property>]
    let ``property 2`` (x: int) = x * 2 >= x
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;

[Properties(Tests = 1000, AutoGenConfig = nameof(AutoGenConfig))]
public class PerformanceCriticalProperties
{
    public static IAutoGenConfig AutoGenConfig { get; } =
        Hedgehog.AutoGenConfig.defaults
            .AddGenerator(Gen.Int32(Range.Constant(1, 1000)));
    
    [Property]
    public bool Property1(int x) => x + 1 > x;
    
    [Property]
    public bool Property2(int x) => x * 2 >= x;
}
```

---

## Comparison with Manual Integration

# [F#](#tab/fsharp)

**Without Hedgehog.Xunit:**

```fsharp
open Xunit
open Hedgehog
open Hedgehog.FSharp

[<Fact>]
let ``manual property test`` () =
    property {
        let! xs = Gen.list (Range.linear 0 100) Gen.int32
        return List.rev (List.rev xs) = xs
    }
    |> Property.check
```

**With Hedgehog.Xunit:**

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Xunit

[<Property>]
let ``automatic property test`` (xs: int list) =
    List.rev (List.rev xs) = xs
```

# [C#](#tab/csharp)

**Without Hedgehog.Xunit:**

```csharp
using Xunit;
using Hedgehog;
using Hedgehog.Linq;
using System.Linq;

public class ManualTests
{
    [Fact]
    public void ManualPropertyTest()
    {
        var property = from xs in Gen.Int32().List(Range.Linear(0, 100))
                       select xs.Reverse().Reverse().SequenceEqual(xs);
        
        property.Check();
    }
}
```

**With Hedgehog.Xunit:**

```csharp
using Hedgehog.Xunit;
using System.Linq;

public class AutomaticTests
{
    [Property]
    public bool AutomaticPropertyTest(int[] xs)
    {
        return xs.Reverse().Reverse().SequenceEqual(xs);
    }
}
```

---

The `[<Property>]` attribute eliminates boilerplate while providing more features like rechecking, custom generators per parameter, and class/module-level configuration.
