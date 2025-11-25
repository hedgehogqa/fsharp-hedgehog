# Custom Generators

This page covers how to customize data generation using `AutoGenConfig` and `GenAttribute`.

## Using AutoGenConfig

For complete control over generation, provide a custom `AutoGenConfig`:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Xunit

type PositiveInts = 
    static member __ = 
        AutoGenConfig.empty 
        |> AutoGenConfig.addGenerator (Gen.int32 (Range.constant 1 100))

[<Property(typeof<PositiveInts>)>]
let ``only positive integers`` (x: int) =
    x > 0
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;
using Range = Hedgehog.Linq.Range;

public class PositiveInts
{
    public static AutoGenConfig Config =>
        AutoGenConfig.Empty
            .AddGenerator(Gen.Int32(Range.Constant(1, 100)));
}

public class MyTests
{
    [Property(AutoGenConfig = typeof(PositiveInts))]
    public bool OnlyPositiveIntegers(int x)
    {
        return x > 0;
    }
}
```

---

## Using GenAttribute

For parameter-level control, inherit from `GenAttribute`:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Xunit

type SmallPositiveInt() =
    inherit GenAttribute<int>()
    override _.Generator = Gen.int32 (Range.constant 1 20)

[<Property>]
let ``custom parameter generator`` ([<SmallPositiveInt>] x) =
    x >= 1 && x <= 20
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;
using Range = Hedgehog.Linq.Range;

public class SmallPositiveIntAttribute : GenAttribute<int>
{
    public override Gen<int> Generator =>
        Gen.Int32(Range.Constant(1, 20));
}

[Property]
public bool CustomParameterGenerator([SmallPositiveInt] int x)
{
    return x >= 1 && x <= 20;
}
```

---

Parameter-level generators override any `AutoGenConfig` settings:

# [F#](#tab/fsharp)

```fsharp
type AlwaysThirteen = 
    static member __ = 
        AutoGenConfig.empty 
        |> AutoGenConfig.addGenerator (Gen.constant 13)

type AlwaysFive() =
    inherit GenAttribute<int>()
    override _.Generator = Gen.constant 5

[<Property(typeof<AlwaysThirteen>)>]
let ``GenAttribute overrides AutoGenConfig`` ([<AlwaysFive>] i) =
    i = 5
```

# [C#](#tab/csharp)

```csharp
public class AlwaysThirteen
{
    public static AutoGenConfig Config =>
        AutoGenConfig.Empty.AddGenerator(Gen.Constant(13));
}

public class AlwaysFiveAttribute : GenAttribute<int>
{
    public override Gen<int> Generator => Gen.Constant(5);
}

[Property(AutoGenConfig = typeof(AlwaysThirteen))]
public bool GenAttributeOverridesAutoGenConfig([AlwaysFive] int i)
{
    return i == 5;
}
```

---

## Understanding AutoGenConfig Layering

Hedgehog merges `AutoGenConfig` settings from multiple levels, creating a layered configuration system:

1. **Default generators** - Hedgehog's built-in generators for common types (base layer)
2. **`PropertiesAttribute` level** - Configurations applied to all properties in a class/module
3. **`PropertyAttribute` level** - Configurations for individual property tests
4. **`GenAttribute` level** - Generators for specific parameters (highest priority)

When merging layers, generators at more specific levels override those from outer levels. For example, if you configure an `int` generator at the `Properties` level and another `int` generator at the `Property` level, the `Property` one wins.

### Why Use `AutoGenConfig.empty`?

When creating custom `AutoGenConfig` instances, always start with `AutoGenConfig.empty` rather than `AutoGenConfig.defaults`:

# [F#](#tab/fsharp)

```fsharp
// ✓ Recommended: Add only what you need
type CustomConfig = 
    static member __ = 
        AutoGenConfig.empty 
        |> AutoGenConfig.addGenerator (Gen.int32 (Range.constant 1 100))

// ✗ Avoid: Contains all default generators
type ProblematicConfig = 
    static member __ = 
        AutoGenConfig.defaults  // Includes default generators for all types!
        |> AutoGenConfig.addGenerator (Gen.string (Range.constant 5 10) Gen.alpha)
```

# [C#](#tab/csharp)

```csharp
// ✓ Recommended: Add only what you need
public class CustomConfig
{
    public static AutoGenConfig Config =>
        AutoGenConfig.Empty
            .AddGenerator(Gen.Int32(Range.Constant(1, 100)));
}

// ✗ Avoid: Contains all default generators
public class ProblematicConfig
{
    public static AutoGenConfig Config =>
        AutoGenConfig.Defaults  // Includes default generators for all types!
            .AddGenerator(Gen.String(Range.Constant(5, 10), Gen.Alpha));
}
```

---

**The problem:** When you start with `AutoGenConfig.defaults`, it includes generators for all built-in types. During merging, these will override any generators configured at outer layers.

For example:
- `Properties` level configures a custom `int` generator (1-10)
- `Property` level uses `AutoGenConfig.defaults.AddGenerator(...)` 
- Result: The default `int` generator (from `defaults`) overrides your custom one!

By starting with `AutoGenConfig.empty`, you only include the generators you explicitly add. Since `defaults` are already used as the base layer, there's rarely a reason to start from `AutoGenConfig.defaults`.

### Layering Example

Here's how configurations merge across levels:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Xunit

// Class-level config: integers 1-10
type SmallInts = 
    static member __ = 
        AutoGenConfig.empty 
        |> AutoGenConfig.addGenerator (Gen.int32 (Range.constant 1 10))

// Method-level config: add string generator
type WithStrings = 
    static member __ = 
        AutoGenConfig.empty 
        |> AutoGenConfig.addGenerator (Gen.string (Range.constant 5 10) Gen.alpha)

[<Properties(typeof<SmallInts>)>]
module ``Layering example`` =
    
    [<Property>]
    let ``uses class-level config`` (x: int) =
        // x will be 1-10 from SmallInts
        x >= 1 && x <= 10
    
    [<Property(typeof<WithStrings>)>]
    let ``merges class and method config`` (x: int) (s: string) =
        // x still 1-10 from SmallInts (class level)
        // s is 5-10 chars from WithStrings (method level)
        x >= 1 && x <= 10 && s.Length >= 5 && s.Length <= 10
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;
using Range = Hedgehog.Linq.Range;

// Class-level config: integers 1-10
public class SmallInts
{
    public static AutoGenConfig Config =>
        AutoGenConfig.Empty
            .AddGenerator(Gen.Int32(Range.Constant(1, 10)));
}

// Method-level config: add string generator
public class WithStrings
{
    public static AutoGenConfig Config =>
        AutoGenConfig.Empty
            .AddGenerator(Gen.String(Range.Constant(5, 10), Gen.Alpha));
}

[Properties(AutoGenConfig = typeof(SmallInts))]
public class LayeringExample
{
    [Property]
    public bool UsesClassLevelConfig(int x)
    {
        // x will be 1-10 from SmallInts
        return x >= 1 && x <= 10;
    }
    
    [Property(AutoGenConfig = typeof(WithStrings))]
    public bool MergesClassAndMethodConfig(int x, string s)
    {
        // x still 1-10 from SmallInts (class level)
        // s is 5-10 chars from WithStrings (method level)
        return x >= 1 && x <= 10 && s.Length >= 5 && s.Length <= 10;
    }
}
```

---

## Working with AutoGenConfig Arguments

For more dynamic configurations, you can pass arguments to your `AutoGenConfig`:

# [F#](#tab/fsharp)

```fsharp
type ConfigWithArgs = 
    static member __ (minValue: int) (maxValue: int) = 
        AutoGenConfig.empty 
        |> AutoGenConfig.addGenerator (Gen.int32 (Range.constant minValue maxValue))

[<Property(
    AutoGenConfig = typeof<ConfigWithArgs>, 
    AutoGenConfigArgs = [|10; 20|])>]
let ``uses config arguments`` (x: int) =
    x >= 10 && x <= 20
```

# [C#](#tab/csharp)

```csharp
public class ConfigWithArgs
{
    public static AutoGenConfig Config(int minValue, int maxValue) =>
        AutoGenConfig.Empty
            .AddGenerator(Gen.Int32(Range.Constant(minValue, maxValue)));
}

[Property(
    AutoGenConfig = typeof(ConfigWithArgs), 
    AutoGenConfigArgs = new object[] { 10, 20 })]
public bool UsesConfigArguments(int x)
{
    return x >= 10 && x <= 20;
}
```

---
