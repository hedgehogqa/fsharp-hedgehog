# Understanding Ranges

Ranges control how Hedgehog generates values and are crucial for effective property-based testing. Choosing the right range type can mean the difference between tests that find bugs and tests that miss edge cases.

## What Are Ranges?

A **Range** specifies the bounds within which a generator produces values. But ranges do more than just set minimum and maximum values—they control *how* the generator explores the value space as tests progress.

Hedgehog runs tests with a **Size** parameter that starts at 1 and increases to 99, then cycles back. Different range types use this Size parameter differently to control value generation.

## The Three Core Range Types

### Constant Ranges

**What they do**: Generate values uniformly across the entire range, unaffected by the Size parameter.

**When to use**:
- Small ranges where you want full coverage (e.g., 0-10)
- Critical edge cases that must be tested at all sizes
- When the range is already appropriately scoped

**Characteristics**:
- ✅ Tests boundary values at all sizes
- ✅ Uniform distribution across the range
- ❌ Can miss patterns in large ranges
- ❌ No gradual exploration of the value space

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

// Always generates values between 0-100, regardless of test size
Gen.int32 (Range.constant 0 100)

// Always generates lists with 5-10 elements
Gen.list (Range.constant 5 10) Gen.alpha
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

// Always generates values between 0-100, regardless of test size
Gen.Int32(Range.Constant(0, 100));

// Always generates lists with 5-10 elements
Gen.Alpha.List(Range.Constant(5, 10));
```

---

**Example Use Case**: Testing a function that works with percentages (0-100) or small counts.

### Linear Ranges

**What they do**: Scale the range bounds *linearly* with the Size parameter. At Size 1, generates values near the origin; at Size 99, generates values near the bounds.

**When to use**:
- When you want gradual, predictable exploration of the value space
- When the range is small enough that skipping the first ~1% won't miss critical values
- When you control the origin point with `linearFrom`

**Characteristics**:
- ⚠️ **At Size 1, bounds are only ~1% of the full range** (e.g., bounds 0-10 for a range of 0-1000)
- ⚠️ **Origin affects the bounds** - use `linearFrom` to control where small sizes start
- ✅ Gradual, predictable scaling
- ❌ May miss critical edge cases in large ranges (takes many iterations to reach upper bound)
- ❌ Poor for ranges where you need to test boundary values early

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

// Scales linearly from 0 to 1000
// Size 1:  bounds (0, ~10) - generates random values like 0, 4, 7, 10
// Size 50: bounds (0, ~505)
// Size 99: bounds (0, 1000)
Gen.int32 (Range.linear 0 1000)

// Scales from origin=0, range 0-1000
// Size 1: near 0
// Size 99: ~1000
Gen.int32 (Range.linearFrom 0 0 1000)
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

// Scales linearly from 0 to 1000
// Size 1:  bounds (0, ~10) - generates random values like 0, 4, 7, 10
// Size 50: bounds (0, ~505)
// Size 99: bounds (0, 1000)
Gen.Int32(Range.LinearInt32(0, 1000));

// Scales from origin=0, range 0-1000
// Size 1: near 0
// Size 99: ~1000
Gen.Int32(Range.LinearFromInt32(0, 0, 1000));
```

---

**Example Use Case**: Testing array indices (0-1000), moderately-sized counts, or when you control the origin with `linearFrom`.

### Exponential Ranges

**What they do**: Scale the range bounds *exponentially* with the Size parameter. Start very close to the origin and grow exponentially toward the bounds.

**When to use**:
- **Large ranges** (Int32.MaxValue, Int64.MaxValue, etc.)
- When boundary values (especially small ones) are critical
- When you need both edge cases and large values

**Characteristics**:
- ✅ **Tests boundary values** at small sizes
- ✅ Perfect for huge ranges like Int32.MaxValue
- ✅ Explores both edge cases and large values effectively
- ✅ Reaches the maximum bound at Size 99

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

// Exponential scaling from 0 to Int32.MaxValue
// Size 0:  0
// Size 1:  ~0 (very small)
// Size 10: ~2
// Size 50: ~46,340
// Size 99: 2,147,483,647 (Int32.MaxValue)
Gen.int32 (Range.exponential 0 System.Int32.MaxValue)

// From origin=1
Gen.int32 (Range.exponentialFrom 1 1 System.Int32.MaxValue)
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

// Exponential scaling from 0 to Int32.MaxValue
// Size 0:  0
// Size 1:  ~0 (very small)
// Size 10: ~2
// Size 50: ~46,340
// Size 99: 2,147,483,647 (Int32.MaxValue)
Gen.Int32(Range.ExponentialInt32(0, int.MaxValue));

// From origin=1
Gen.Int32(Range.ExponentialFromInt32(1, 1, int.MaxValue));
```

---

**Example Use Case**: Testing with any integer, testing with very large collections, performance testing.

## The Boundary Value Problem

**Critical Insight**: When using `linear` over very large ranges, **boundary values are only tested at very small sizes** where they're less likely to be generated.

### The Problem

Consider this common pattern:

# [F#](#tab/fsharp)

```fsharp
// This looks reasonable but has a problem!
Gen.int32 (Range.linear 0 System.Int32.MaxValue)
```

# [C#](#tab/csharp)

```csharp
// This looks reasonable but has a problem!
Gen.Int32(Range.LinearInt32(0, int.MaxValue))
```

---

**What actually happens**:

| Size | Bounds | Issue |
|------|--------|-------|
| 1 | (0, ~21,691,854) | Upper bound already in millions! |
| 10 | (0, ~216,918,546) | |
| 50 | (0, ~1,084,592,732) | |
| 99 | (0, 2,147,483,647) | Only reaches max at size 99 |

**At Size 1, while bounds include 0-21M, small values like 0-1000 are rare** - only a ~0.005% chance of being picked!

### The Solution: Use Exponential for Large Ranges

# [F#](#tab/fsharp)

```fsharp
// ✅ Correctly tests boundary values
Gen.int32 (Range.exponential 0 System.Int32.MaxValue)
```

# [C#](#tab/csharp)

```csharp
// ✅ Correctly tests boundary values
Gen.Int32(Range.ExponentialInt32(0, int.MaxValue))
```

---

**What actually happens**:

| Size | Bounds | Coverage |
|------|--------|----------|
| 0 | (0, 0) | ✅ Tests exact boundary |
| 1 | (0, ~1) | ✅ Tests small values |
| 10 | (0, ~2) | ✅ Tests edge cases |
| 50 | (0, ~46,340) | ✅ Tests medium values |
| 99 | (0, 2,147,483,647) | ✅ Tests maximum |

## Quick Reference Guide

Choose based on **what matters for your domain**, not just the size of the range:

| Scenario | Recommended Range | Why |
|----------|------------------|-----|
| Small range (0-100) | `constant` or `linear` | Range is small enough that linear won't skip important values |
| Edge cases are critical | **`exponential`** or `constant` | Tests boundary values (0, 1, -1, etc.) at small sizes |
| Uniform distribution needed | `linear` (with appropriate size) | Spreads values evenly across the range |
| Very large range (Int32, Int64) | **`exponential`** | Linear will skip millions of values near the origin |
| Need specific start point | `linearFrom` or `exponentialFrom` | Control where shrinking targets and small sizes start |
| Fixed size collections | `constant` or `singleton` | Size shouldn't vary with test iterations |

**Key Insight**: The question isn't "how big is my range?" but "**will linear skip values that could expose bugs?**"

For a range like 0-10,000:
- `linear` at Size 1 has bounds (0, ~101) - small values possible but less likely as range grows
- `exponential` at Size 1 has bounds (0, ~1) - heavily focused on small values
- If bugs are likely in 0-10 (empty, minimal values, off-by-one): use **exponential**
- If testing the full range gradually is more important: **linear is fine**

## Common Patterns and Examples

### Testing Positive Integers

When you need positive integers (> 0), use `linearFrom` or `exponentialFrom` to set the origin:

# [F#](#tab/fsharp)

```fsharp
// ✅ Starts at 1, scales to Int32.MaxValue
Gen.int32 (Range.linearFrom 1 1 System.Int32.MaxValue)

// ✅ Better for very large ranges
Gen.int32 (Range.exponentialFrom 1 1 System.Int32.MaxValue)
```

# [C#](#tab/csharp)

```csharp
// ✅ Starts at 1, scales to int.MaxValue
Gen.Int32(Range.LinearFromInt32(1, 1, int.MaxValue));

// ✅ Better for very large ranges
Gen.Int32(Range.ExponentialFromInt32(1, 1, int.MaxValue));
```

---

This is exactly what the `[PositiveInt]` attribute does in Hedgehog.Xunit.

### Testing Collection Sizes

# [F#](#tab/fsharp)

```fsharp
// Small lists with gradual growth
Gen.list (Range.linear 0 20) Gen.alpha

// Can test up to very large lists
Gen.list (Range.exponential 0 10000) Gen.int32

// Fixed small size for focused testing
Gen.list (Range.constant 5 10) Gen.bool
```

# [C#](#tab/csharp)

```csharp
// Small lists with gradual growth
Gen.Alpha.List(Range.LinearInt32(0, 20));

// Can test up to very large lists
Gen.Int32(Range.ConstantBoundedInt32()).List(Range.ExponentialInt32(0, 10000));

// Fixed small size for focused testing
Gen.Bool.List(Range.Constant(5, 10));
```

---

### Testing with Bounded Types

For types with natural bounds (bytes, shorts, etc.), use `exponentialBounded`:

# [F#](#tab/fsharp)

```fsharp
// Automatically uses 0 to Byte.MaxValue (255)
Gen.byte (Range.exponentialBounded ())

// Automatically uses Int16.MinValue to Int16.MaxValue
Gen.int16 (Range.exponentialBounded ())
```

# [C#](#tab/csharp)

```csharp
// Automatically uses 0 to byte.MaxValue (255)
Gen.Byte(Range.ExponentialBoundedByte());

// Automatically uses short.MinValue to short.MaxValue
Gen.Int16(Range.ExponentialBoundedInt16());
```

---

## How Size Affects Test Execution

Understanding how the Size parameter works helps you choose the right range:

```
Test 1:  Size = 1
Test 2:  Size = 2
Test 3:  Size = 3
...
Test 99: Size = 99
Test 100: Size = 100
Test 101: Size = 1   (cycles back)
Test 102: Size = 2
...
```

**Note**: Size 0 is used internally for shrinking but is **not used** in the main test loop, which starts at Size 1.

### Scaling Formulas

**Linear**:
```
value = origin + ((bound - origin) × size) / 99
```

**Exponential**:
```
diff = (((|bound - origin| + 1) ^ (size / 99)) - 1) × sign(bound - origin)
value = origin + diff
```

**Constant**:
```
value = random between lowerBound and upperBound (ignores size)
```

## Best Practices

### ✅ Do

- **Use exponential for large ranges** (Int32, Int64)
- **Use linearFrom to control the starting point** when the origin matters
- **Use constant for small ranges** where full coverage is needed
- **Test your assumptions** about what values are being generated
- **Consider boundary values** when choosing ranges

### ❌ Don't

- **Don't use linear over huge ranges** (you'll miss edge cases)
- **Don't assume Size 0 is tested** (it starts at 1)
- **Don't use unbounded ranges** without understanding the implications
- **Don't forget that linear doesn't start at the lower bound** (use linearFrom instead)

## Debugging Range Behavior

If you're unsure what values a range is producing, you can sample it:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

// See what values are generated
Range.exponential 0 System.Int32.MaxValue
|> Gen.int32
|> Gen.sampleFrom 1 20  // 20 samples statring from size 1
// Output: [0; 0; 1; 1; 2; 1; 4; 5; 4; 0; 8; 1; 5; 17; 12; 20; 18; 48; 60; 22] - tests edge cases!

// Compare linear vs exponential
Range.linear 0 1000
|> Gen.int32
|> Gen.sampleFrom 1 20 
// Output: [4; 12; 6; 20; 39; 20; 27; 3; 0; 52; 70; 77; 100; 137; 140; 66; 42; 27; 3; 194]

Range.exponential 0 1000
|> Gen.int32
|> Gen.sampleFrom 1 20 
// Output: [0; 0; 0; 0; 0; 0; 0; 0; 0; 1; 0; 0; 1; 1; 1; 1; 2; 3; 3; 0] - tests edge cases!
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

// See what values are generated
Gen.Int32(Range.ExponentialInt32(0, int.MaxValue)) 
   .SampleFrom(size: 1, count: 20);
// Output: [0, 0, 1, 1, 2, 1, 4, 5, 4, 0, 8, 1, 5, 17, 12, 20, 18, 48, 60, 22] - tests edge cases!

// Compare linear vs exponential
Gen.Int32(Range.LinearInt32(0, 1000))
   .SampleFrom(size: 1, count: 20);
// Output: [4, 12, 6, 20, 39, 20, 27, 3, 0, 52, 70, 77, 100, 137, 140, 66, 42, 27, 3, 194]

```

---

## Summary

Choosing the right range is crucial for effective property-based testing:

- **Constant**: Full range at all sizes, best when uniform distribution matters more than gradual scaling
- **Linear**: Gradual scaling, good when ~1% skip at small sizes won't miss bugs
- **Exponential**: Exponential scaling, **best when edge cases near the origin are critical**

**The decision isn't about the range size—it's about your domain**:
- Are bugs likely near 0, 1, empty, minimal values? → **Exponential**
- Does skipping the first 1% of your range matter? → **Exponential** or **Constant**
- Is uniform distribution more important than edge cases? → **Linear** or **Constant**

When in doubt for large ranges (like Int32.MaxValue), **use exponential**—it's safer because it ensures boundary value coverage.
