# Auto-Generation

> [!WARNING]
> Auto-generation is not available when using Fable. You must write custom generators manually for Fable projects.

Hedgehog can automatically generate test data for your types without manually writing generators. This feature works with F# records, discriminated unions, tuples, and C# classes.

## Basic Usage

# [F#](#tab/fsharp)

For simple F# types, use `Gen.auto<'T>`:

```fsharp
open Hedgehog
open Hedgehog.FSharp

type User = {
    Name: string
    Age: int
    Email: string
}

type Status =
    | Active
    | Inactive
    | Pending of reason: string

property {
    let! user = Gen.auto<User>
    let! status = Gen.auto<Status>
    return user.Age >= 0 && user.Name.Length > 0
}
|> Property.check
```

# [C#](#tab/csharp)

Auto-generation works with C# classes, records, and structs:

```csharp
using Hedgehog;
using Hedgehog.Linq;

public record User(string Name, int Age, string Email);

public enum Status { Active, Inactive, Pending }

var property =
    from user in Gen.Auto<User>().ForAll()
    from status in Gen.Auto<Status>().ForAll()
    select user.Age >= 0 && user.Name.Length > 0;

property.Check();
```

---

## Supported Types

Auto-generation works with:

- **Primitives**: `int`, `string`, `bool`, `decimal`, `DateTime`, etc.
- **Records**: F# records and C# record types
- **Discriminated Unions**: F# union types
- **Tuples**: Both F# and C# tuples
- **Collections**: `List`, `Array`, `Set`, `Map`, `Dictionary`, `IEnumerable`
- **Options**: `Option<'T>`, `ValueOption<'T>`, `Nullable<'T>`
- **Results**: `Result<'T, 'TError>`
- **Classes**: C# classes with constructors or mutable properties
- **Nested Types**: Any combination of the above

## Customizing Auto-Generation

### Using AutoGenConfig

When you need to customize how types are generated, use `Gen.autoWith` with an `AutoGenConfig`:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

type User = {
    Name: string
    Age: int
    Email: string
}

// Create a custom config
let config =
    AutoGenConfig.defaults
    |> AutoGenConfig.addGenerator (Gen.string (Range.linear 5 20) Gen.alpha)  // Custom string generator
    |> AutoGenConfig.addGenerator (Gen.int32 (Range.linear 18 100))           // Ages between 18-100

property {
    let! user = Gen.autoWith<User> config
    return user.Age >= 18 && user.Name.Length >= 5
}
|> Property.check
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

public record User(string Name, int Age, string Email);

// Create a custom config
var config = AutoGenConfig.Defaults
    .AddGenerator(Gen.String(Range.LinearInt32(5, 20), Gen.Alpha))  // Custom string generator
    .AddGenerator(Gen.Int32(Range.LinearInt32(18, 100)));            // Ages between 18-100

var property =
    from user in Gen.AutoWith<User>(config).ForAll()
    select user.Age >= 18 && user.Name.Length >= 5;

property.Check();
```

---

### Registering Custom Generators

For more control, create a custom generator class with static methods that match the names of your types. Hedgehog will automatically discover and use these generators when you register the class.

#### How Generator Classes Work

When you call `AutoGenConfig.addGenerators<T>`, Hedgehog:
1. Scans the class `T` for public static methods
2. Finds methods that return `Gen<T>` (for any type `T`)
3. Uses these methods when generating values of type `T`

**Method Requirements:**
- Must be `public static`
- Must return `Gen<T>` where `T` is the type you want to generate
- Can optionally accept parameters of type `IAutoGenContext` or `Gen<TValue>` for generic types

**Examples:**
- A method returning `Gen<Email>` will be used to generate `Email` values
- A method returning `Gen<User>` will be used to generate `User` values
- For generic types like `Gen<List<T>>`, the method can accept a `Gen<T>` parameter

#### Example

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open System

type Email = Email of string

type User = {
    Name: string
    Email: Email
    RegisteredAt: DateTime
}

// Custom generators class
type MyGenerators =
    // Generator for Email type - return type is Gen<Email>
    static member Email() : Gen<Email> =
        gen {
            let! name = Gen.string (Range.linear 3 10) Gen.alphaNum
            let! domain = Gen.item ["com"; "net"; "org"]
            return Email $"{name}@example.{domain}"
        }
    
    // Generator for DateTime - return type is Gen<DateTime>
    static member DateTime() : Gen<DateTime> =
        gen {
            let! days = Gen.int32 (Range.linear 0 365)
            return DateTime.Now.AddDays(-float days)
        }

// Register the custom generators
let config =
    AutoGenConfig.defaults
    |> AutoGenConfig.addGenerators<MyGenerators>

// Now Gen.autoWith will use MyGenerators.Email() for Email types
// and MyGenerators.DateTime() for DateTime types
property {
    let! user = Gen.autoWith<User> config
    let (Email email) = user.Email
    return email.Contains("@") && user.RegisteredAt <= DateTime.Now
}
|> Property.check
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;
using System;

public record Email(string Value);

public record User(string Name, Email Email, DateTime RegisteredAt);

// Custom generators class
public class MyGenerators
{
    // Generator for Email type - return type is Gen<Email>
    public static Gen<Email> Email() =>
        from name in Gen.AlphaNum.String(Range.LinearInt32(3, 10))
        from domain in Gen.Item("com", "net", "org")
        select new Email($"{name}@example.{domain}");
    
    // Generator for DateTime - return type is Gen<DateTime>
    public static Gen<DateTime> DateTime() =>
        from days in Gen.Int32(Range.LinearInt32(0, 365))
        select System.DateTime.Now.AddDays(-days);
}

// Register the custom generators
var config = AutoGenConfig.Defaults
    .AddGenerators<MyGenerators>();

// Now Gen.AutoWith will use MyGenerators.Email() for Email types
// and MyGenerators.DateTime() for DateTime types
var property =
    from user in Gen.AutoWith<User>(config).ForAll()
    select user.Email.Value.Contains("@") && user.RegisteredAt <= System.DateTime.Now;

property.Check();
```

---

## Generator Method Signatures

Custom generator methods must follow specific patterns:

### Parameterless Generators

For types without generic parameters:

# [F#](#tab/fsharp)

```fsharp
static member TypeName() : Gen<TypeName> = ...
```

# [C#](#tab/csharp)

```csharp
public static Gen<TypeName> TypeName() => ...
```

---

### Generic Type Generators

For generic types, accept `Gen<T>` parameters for each type parameter:

# [F#](#tab/fsharp)

```fsharp
type GenericGenerators =
    // Simple generic type
    static member MyGenericType<'a>(valueGen: Gen<'a>) : Gen<MyGenericType<'a>> =
        valueGen |> Gen.map (fun x -> MyGenericType(x))
```

# [C#](#tab/csharp)

```csharp
public class GenericGenerators
{
    // Simple generic type
    public static Gen<MyGenericType<A>> MyGenericType<A>(Gen<A> valueGen) =>
        valueGen.Select(x => new MyGenericType<A>(x));
}
```

---

### Using AutoGenContext

For recursive types or when you need access to collection range and recursion depth, use `AutoGenContext`:

# [F#](#tab/fsharp)

```fsharp
type GenericGenerators =
    // Access to recursion control via AutoGenContext
    static member ImmutableList<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<ImmutableList<'a>> =
        if context.CanRecurse then
            valueGen |> Gen.list context.CollectionRange |> Gen.map ImmutableList.CreateRange
        else
            Gen.constant ImmutableList<'a>.Empty

let config =
    AutoGenConfig.defaults
    |> AutoGenConfig.addGenerators<GenericGenerators>
```

# [C#](#tab/csharp)

```csharp
public class GenericGenerators
{
    // Access to recursion control via AutoGenContext
    public static Gen<ImmutableList<A>> ImmutableList<A>(AutoGenContext context, Gen<A> valueGen) =>
        context.CanRecurse
            ? valueGen.List(context.CollectionRange).Select(System.Collections.Immutable.ImmutableList.CreateRange)
            : Gen.Constant(System.Collections.Immutable.ImmutableList<A>.Empty);
}

var config = AutoGenConfig.Defaults
    .AddGenerators<GenericGenerators>();
```

---

## Advanced Scenarios

### Multiple Configurations

You can create different configurations for different test scenarios:

# [F#](#tab/fsharp)

```fsharp
let smallDataConfig =
    AutoGenConfig.defaults
    |> AutoGenConfig.addGenerator (Gen.string (Range.linear 0 10) Gen.alpha)

let largeDataConfig =
    AutoGenConfig.defaults
    |> AutoGenConfig.addGenerator (Gen.string (Range.linear 0 1000) Gen.unicode)
```

# [C#](#tab/csharp)

```csharp
var smallDataConfig = AutoGenConfig.Defaults
    .AddGenerator(Gen.String(Range.LinearInt32(0, 10), Gen.Alpha));

var largeDataConfig = AutoGenConfig.Defaults
    .AddGenerator(Gen.String(Range.LinearInt32(0, 1000), Gen.Unicode));
```

---

## Best Practices

1. **Start with `Gen.auto`** - Use the default auto-generation first, only customize when needed
2. **Register generators once** - Create a shared `AutoGenConfig` for your test suite
3. **Use type safety** - Let the type system guide generator creation
4. **Test your generators** - Verify custom generators produce valid data
5. **Leverage AutoGenContext** - For recursive types, use `AutoGenContext` to control depth and collection sizes

