---
_tocRel: articles/toc.yml
---

# Hedgehog .NET

**Hedgehog will eat all your bugs.**

Hedgehog is a modern property-based testing system for .NET, designed to help you release with confidence by automatically finding edge cases you didn't think of.

## What is Property-Based Testing?

Instead of writing individual test cases with specific inputs and expected outputs, property-based testing lets you describe *properties* that should hold true for all inputs:

```
reverse (reverse xs) = xs, ∀xs :: [α]
```

This reads as: "the reverse of the reverse of a list equals the original list — for all lists of any type."

In Hedgehog, you express this property and the framework generates hundreds of random test cases automatically:

# [F#](#tab/fsharp)

```fsharp
property {
    let! xs = Gen.list (Range.linear 0 100) Gen.alpha
    return List.rev (List.rev xs) = xs
}
|> Property.render
```

# [C#](#tab/csharp)

```csharp
var property =
    from xs in Gen.Alpha.List(Range.LinearInt32(0, 100)).ForAll()
    select xs.Reverse().Reverse().SequenceEqual(xs);

property.Check();
```

---

```
+++ OK, passed 100 tests.
```

## Why Hedgehog?

### Integrated Shrinking

When a property fails, Hedgehog automatically simplifies the failing input to find the smallest counterexample. Unlike older property-based testing libraries that shrink values separately from generation, Hedgehog uses **integrated shrinking** — shrinking is built into generators, guaranteeing that shrunk values obey the same invariants as the generated values.

This means you get minimal, actionable error reports:

# [F#](#tab/fsharp)

```fsharp
property {
    let! xs = Gen.list (Range.linear 0 100) version
    return xs |> List.rev = xs
}
|> Property.render
```

# [C#](#tab/csharp)

```csharp
var property =
    from xs in versionGen.List(Range.LinearInt32(0, 100)).ForAll()
    select xs.Reverse().SequenceEqual(xs);

property.Check();
```

---

```
*** Failed! Falsifiable (after 3 tests and 6 shrinks):
[0.0.0; 0.0.1]
```

Even complex types like `System.Version` shrink automatically to their simplest failing case.

### Expressive Syntax

# [F#](#tab/fsharp)

Hedgehog provides `gen` and `property` computation expressions that feel natural in F#:

```fsharp
let ipAddressGen = gen {
    let! addr = Gen.array (Range.singleton 4) Gen.byte
    return System.Net.IPAddress addr
}
```

# [C#](#tab/csharp)

Hedgehog provides support for LINQ syntax that allows writing generators and properties easier:

```csharp
var ipAddressGen = 
    from addr in Gen.Byte.Array(Range.Singleton(4))
    select new System.Net.IPAddress(addr);
```

---

### Precise Control

Range combinators let you control the scope of generated values:

```fsharp
Gen.int32 (Range.constant 0 100)      // Always between 0-100
Gen.int32 (Range.linear 0 100)        // Scales with test size
Gen.list (Range.exponential 1 1000) g // Exponential growth
```

## Core Concepts

**Properties** are invariants that should hold for all inputs. They're expressed as functions that return `bool` or `Property<bool>`.

**Generators** produce random values of a given type. Hedgehog includes generators for primitives and combinators to build generators for complex types. Generators are composable using `map`, `bind`, and other familiar operations.

**Shrinking** happens automatically when a property fails. Hedgehog finds progressively smaller inputs that still cause the failure, giving you the minimal reproduction case.

