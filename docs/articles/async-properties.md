# Async Properties

Hedgehog supports testing asynchronous code through async properties. This guide explains how async support works internally and what to expect when writing async property tests.

## Basic Usage

You can bind async computations and tasks directly in property computation expressions:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

property {
    let! x = Gen.int32 (Range.linear 1 100)
    let! result = async {
        do! Async.Sleep 10
        return x * 2
    }
    return result > 0
}
|> Property.checkAsync
|> Async.RunSynchronously
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

var prop =
    from x in Gen.Int32(Range.LinearInt32(1, 100)).ForAll()
    from result in Task.Run(async () => 
    {
        await Task.Delay(10);
        return x * 2;
    })
    select result > 0;

await prop.CheckAsync();
```

---

## How Async Support Works

Understanding the internal mechanics helps you write efficient async property tests.

### Two Phases: Generation and Execution

Property testing in Hedgehog happens in two distinct phases:

1. **Generation Phase**: Building the tree of test values with shrinks
2. **Execution Phase**: Running the property test with generated values

### Synchronous Generation, Async Execution

The key design decision is:

- **Generator trees are built synchronously** - This ensures proper seed threading for reproducible random generation
- **Property evaluation happens asynchronously** - When you use `checkAsync`/`reportAsync`, the actual test execution is fully async without blocking

### When Blocking Occurs

Blocking can occur during the **generation phase** when you use the result of an async operation to determine what generator to use next:

# [F#](#tab/fsharp)

```fsharp
// This WILL block during generation:
property {
    let! x = Gen.int32 (Range.linear 1 100)
    let! asyncValue = async { return 42 }  // Creates async value
    let! y = Gen.int32 (Range.linear asyncValue 100)  // BLOCKS to get asyncValue
    return y > 0
}
```

# [C#](#tab/csharp)

```csharp
// This WILL block during generation:
var prop =
    from x in Gen.Int32(Range.LinearInt32(1, 100)).ForAll()
    from asyncValue in Task.FromResult(42)
    from y in Gen.Int32(Range.LinearInt32(asyncValue, 100)).ForAll()  // BLOCKS!
    select y > 0;
```

---

Why? Because to create the generator `Gen.int32 (Range.linear asyncValue 100)`, Hedgehog needs to know what `asyncValue` is. 
The only way to get it is to run the async computation and get the value.

### When Blocking Does NOT Occur

If your async operations are only used for the final assertion or return value, no blocking occurs during generation:

# [F#](#tab/fsharp)

```fsharp
// This will NOT block during generation:
property {
    let! x = Gen.int32 (Range.linear 1 100)
    let! y = Gen.string (Range.linear 1 10) Gen.alpha
    let! result = async {
        // Async work here
        do! Async.Sleep 10
        return x + String.length y
    }
    return result > 0  // Just checking the result
}
|> Property.checkAsync
```

# [C#](#tab/csharp)

```csharp
// This will NOT block during generation:
var prop =
    from x in Gen.Int32(Range.LinearInt32(1, 100)).ForAll()
    from y in Gen.String(Range.LinearInt32(1, 10), Gen.Alpha).ForAll()
    from result in Task.Run(async () =>
    {
        await Task.Delay(10);
        return x + y.Length;
    })
    select result > 0;  // Just checking the result

await prop.CheckAsync();
```

---

When you run this with `checkAsync` or `CheckAsync`, the entire async chain executes without blocking threads.

> [!NOTE]
> Cancellation tokens are not currently supported in Hedgehog's async APIs. The `checkAsync` and `reportAsync` methods do not accept `CancellationToken` parameters, and async operations within properties cannot be cancelled externally.

## Best Practices

### ✅ Do: Use async for test logic

# [F#](#tab/fsharp)

```fsharp
property {
    let! userId = Gen.int32 (Range.linear 1 1000)
    let! userName = Gen.string (Range.linear 1 50) Gen.alpha
    
    // Async operations in test body - no blocking
    let! user = async {
        let! created = createUserAsync userId userName
        let! fetched = getUserAsync userId
        return fetched
    }
    
    return user.Name = userName
}
|> Property.checkAsync
```

# [C#](#tab/csharp)

```csharp
var prop =
    from userId in Gen.Int32(Range.LinearInt32(1, 1000)).ForAll()
    from userName in Gen.String(Range.LinearInt32(1, 50), Gen.Alpha).ForAll()
    from user in Task.Run(async () =>
    {
        // Async operations in test body - no blocking
        var created = await CreateUserAsync(userId, userName);
        var fetched = await GetUserAsync(userId);
        return fetched;
    })
    select user.Name == userName;

await prop.CheckAsync();
```

---

### ✅ Do: Generate all values first, then run async

# [F#](#tab/fsharp)

```fsharp
property {
    // Generate all test data synchronously
    let! count = Gen.int32 (Range.linear 1 100)
    let! items = Gen.list (Range.singleton count) Gen.alpha
    
    // Then run async operations
    let! result = async {
        do! processItemsAsync items
        return! verifyAsync count
    }
    
    return result
}
|> Property.checkAsync
```

# [C#](#tab/csharp)

```csharp
var prop =
    from count in Gen.Int32(Range.LinearInt32(1, 100)).ForAll()
    from items in Gen.Alpha.List(Range.Singleton(count)).ForAll()
    from result in Task.Run(async () =>
    {
        // Then run async operations
        await ProcessItemsAsync(items);
        return await VerifyAsync(count);
    })
    select result;

await prop.CheckAsync();
```

---

### ⚠️ Avoid: Using async results to determine generators

# [F#](#tab/fsharp)

```fsharp
// This blocks during generation!
property {
    let! config = async { return! loadConfigAsync() }
    let! value = Gen.int32 (Range.linear 1 config.MaxValue)  // Blocks!
    // ...
}
```

Instead, generate the range synchronously:

```fsharp
// Better approach
property {
    let! maxValue = Gen.int32 (Range.linear 1 1000)  // Generate synchronously
    let! value = Gen.int32 (Range.linear 1 maxValue)
    let! result = async {
        let! config = loadConfigAsync()
        return value <= config.MaxValue
    }
    return result
}
|> Property.checkAsync
```

# [C#](#tab/csharp)

```csharp
// This blocks during generation!
var badProp =
    from config in LoadConfigAsync()
    from value in Gen.Int32(Range.LinearInt32(1, config.MaxValue)).ForAll()  // Blocks!
    select true;
```

Instead, generate the range synchronously:

```csharp
// Better approach
var goodProp =
    from maxValue in Gen.Int32(Range.LinearInt32(1, 1000)).ForAll()  // Generate synchronously
    from value in Gen.Int32(Range.LinearInt32(1, maxValue)).ForAll()
    from result in Task.Run(async () =>
    {
        var config = await LoadConfigAsync();
        return value <= config.MaxValue;
    })
    select result;

await goodProp.CheckAsync();
```

---

## Synchronous vs Asynchronous Execution

Hedgehog provides both synchronous and asynchronous APIs:

### Synchronous API (blocks on async properties)

# [F#](#tab/fsharp)

```fsharp
property { ... }
|> Property.check  // Blocks if property contains async

property { ... }
|> Property.checkBool  // Blocks if property contains async
```

# [C#](#tab/csharp)

```csharp
prop.Check();  // Blocks if property contains async

boolProp.CheckBool();  // Blocks if property contains async
```

---

Use this for:
- Pure synchronous properties
- When blocking is acceptable
- Simple test scenarios

### Asynchronous API (non-blocking)

# [F#](#tab/fsharp)

```fsharp
property { ... }
|> Property.checkAsync  // Non-blocking F# Async
|> Async.RunSynchronously

property { ... }
|> Property.checkTask  // Non-blocking C# Task
|> Async.AwaitTask
|> Async.RunSynchronously
```

# [C#](#tab/csharp)

```csharp
await prop.CheckAsync();  // Non-blocking returns F# Async

// Or if you have the Task-returning version:
await prop.CheckAsync();  // Non-blocking C# Task
```

---

Use this for:
- Properties containing async/task operations
- Integration tests with I/O
- When you need non-blocking execution

## Summary

- **Generation is synchronous**: Building the tree of test cases with proper seed threading
- **Execution can be async**: Use `checkAsync`/`reportAsync` for non-blocking test execution
- **Interleaving matters**: Using async results to determine what to generate next will block during generation
- **Pattern**: Generate values first (sync), then test them (async) for best performance
