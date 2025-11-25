# Property-Based Testing Best Practices

## The Mental Shift: From Examples to Properties

Property-based testing requires a fundamental shift in how you think about testing. Instead of asking "what specific examples should I test?", you ask "what is **always true** about my code?"

### Example-Based vs Property-Based Thinking

| Example-Based | Property-Based |
|---------------|----------------|
| "Test with values 1, 5, and 100" | "Test with any positive integer" |
| "Check this specific case" | "What's always true?" |
| "This input gives this output" | "What relationship holds between input and output?" |
| "Cover branches" | "Cover invariants and properties" |

### The Core Philosophy

**Properties over Examples**: Seek universal truths that hold for all valid inputs, not just carefully chosen examples.

**Generative Thinking**: Think about the *space of all possible inputs*. What happens with empty collections? Maximum values? Negative numbers? Unicode characters?

**Shrinking for Clarity**: When tests fail, the framework automatically finds the minimal failing case, revealing the true cause.

**Specification by Properties**: Properties serve as executable specifications that document how your code should behave.

**Essential over Exhaustive**: Focus on minimal, non-overlapping properties that provide unique value. Quality over quantity.

## Discovering Properties: A Systematic Approach

### Step 1: Understand the Code's Contracts

Before writing any test, ask these fundamental questions:

- **What are the preconditions?** (What inputs are valid?)
- **What are the postconditions?** (What does the code guarantee about outputs?)
- **What invariants must always hold?** (What never changes?)
- **What relationships exist between inputs and outputs?**
- **What business rules must never be violated?**

### Step 2: Use Property Pattern Recognition

Start with the easiest patterns to identify:

1. **Type Properties**: Does it return the expected type?
2. **Boundary Properties**: What happens at edges (empty, zero, max, null)?
3. **Idempotent Properties**: Does doing it twice = doing it once?
4. **Inverse Properties**: Can you undo the operation?
5. **Invariant Properties**: What never changes?
6. **Commutative Properties**: Does order matter?
7. **Business Rule Properties**: What domain rules must hold?

## The Seven Core Property Patterns

### 1. Invariants: What Never Changes

**Pattern**: Properties that always hold about the result, regardless of input.

**When to use**: When something about the output must always be true.

**Questions to ask**:
- What properties are preserved by this operation?
- What must always be true about the result?
- What can't possibly change?

**Examples**:
- Sorting preserves all elements (count and contents)
- Mapping over a list preserves its length
- Filtering never increases collection size
- String trimming never increases length

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Swensen.Unquote

[<Fact>]
let ``Sort should preserve all elements`` () =
    property {
        let! list = Gen.list (Range.linear 0 100) Gen.int32
        let sorted = List.sort list
        test <@ List.length sorted = List.length list @>
        test <@ List.forall (fun x -> List.contains x sorted) list @>
    }
    |> Property.check
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

[Fact]
public void Sort_Should_Preserve_All_Elements()
{
    var property =
        from list in Gen.Int32(Range.ConstantBoundedInt32()).List(Range.LinearInt32(0, 100)).ForAll()
        let sorted = list.OrderBy(x => x).ToList()
        select sorted.Count == list.Count && list.All(x => sorted.Contains(x));

    property.Check();
}
```

---

### 2. Business Rules: Domain Constraints

**Pattern**: Domain-specific rules that must never be violated.

**When to use**: When you have business logic that defines what's valid.

**Questions to ask**:
- What business rules must never be broken?
- What would make the output invalid in the business domain?
- What constraints does the domain impose?

**Examples**:
- Discounts never exceed the original price
- Age must be within valid range (0-120)
- Account balance can't go negative (unless overdraft allowed)
- Percentages must be between 0 and 100

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Swensen.Unquote

let priceGen = Gen.int32 (Range.constant 1 10000) |> Gen.map (fun x -> decimal x / 100m)
let discountGen = Gen.int32 (Range.constant 0 100) |> Gen.map (fun x -> decimal x / 100m)

[<Fact>]
let ``Apply discount should never exceed original price`` () =
    property {
        let! price = priceGen
        let! discountPercent = discountGen
        let discounted = applyDiscount price discountPercent
        test <@ discounted <= price @>
        test <@ discounted >= 0m @>
    }
    |> Property.check
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

var priceGen = Gen.Int32(Range.Constant(1, 10000)).Select(x => (decimal)x / 100);
var discountGen = Gen.Int32(Range.Constant(0, 100)).Select(x => (decimal)x / 100);

[Fact]
public void ApplyDiscount_Should_Never_Exceed_Original_Price()
{
    var property =
        from price in priceGen.ForAll()
        from discountPercent in discountGen.ForAll()
        let discounted = ApplyDiscount(price, discountPercent)
        select discounted <= price && discounted >= 0;

    property.Check();
}
```

---

### 3. Inverse/Roundtrip: Reversible Operations

**Pattern**: Operations that undo each other should return to the original state.

**When to use**: When you have encode/decode, serialize/deserialize, compress/decompress, encrypt/decrypt, or any reversible transformation.

**Questions to ask**:
- Can I undo this operation?
- Is there a reverse operation?
- Should encode→decode return the original?

**Examples**:
- Serialize→Deserialize = identity
- Encode→Decode = identity
- Compress→Decompress = identity
- Add→Subtract = identity

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open System.Text.Json
open Swensen.Unquote

[<Fact>]
let ``Serialize should roundtrip when deserializing`` () =
    property {
        let! person = Gen.auto<Person>
        let json = JsonSerializer.Serialize(person)
        let restored = JsonSerializer.Deserialize<Person>(json)
        test <@ restored = person @>
    }
    |> Property.check
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

var personGen = /* define your Person generator */;

[Fact]
public void Serialize_Should_RoundTrip_When_Deserializing()
{
    var property =
        from person in personGen.ForAll()
        let json = JsonSerializer.Serialize(person)
        let restored = JsonSerializer.Deserialize<Person>(json)
        select restored.Equals(person);

    property.Check();
}
```

---

### 4. Idempotence: Applying N Times = Applying Once

**Pattern**: Applying an operation multiple times has the same effect as applying it once.

**When to use**: When operations normalize, clean, or reach a stable state.

**Questions to ask**:
- Does applying this twice change the result?
- Does the operation stabilize?
- Is this a normalization?

**Examples**:
- Normalize(Normalize(x)) = Normalize(x)
- ToUpper(ToUpper(s)) = ToUpper(s)
- Trim(Trim(s)) = Trim(s)
- Absolute value is idempotent: Abs(Abs(x)) = Abs(x)

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Swensen.Unquote

[<Fact>]
let ``Normalize should be idempotent`` () =
    property {
        let! text = Gen.string (Range.linear 0 100) Gen.unicode
        let once = normalize text
        let twice = normalize once
        test <@ twice = once @>
    }
    |> Property.check
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

[Fact]
public void Normalize_Should_Be_Idempotent()
{
    var property =
        from text in Gen.Unicode.String(Range.LinearInt32(0, 100)).ForAll()
        let once = Normalize(text)
        let twice = Normalize(once)
        select twice == once;

    property.Check();
}
```

---

### 5. Oracle: Comparing Against Known Truth

**Pattern**: Compare your implementation against a reference implementation or mathematical truth.

**When to use**: When you have a trusted implementation (standard library, mathematical formula, legacy system).

**Questions to ask**:
- Is there a reference implementation?
- Can I use a mathematical formula?
- Is there a simpler (but slower) correct implementation?

**Examples**:
- Custom sort should match standard library sort
- Square root: sqrt(x)² ≈ x
- Custom parser should match standard parser
- Optimized algorithm should match naive implementation

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Swensen.Unquote

[<Fact>]
let ``Custom sort should match standard sort`` () =
    property {
        let! list = Gen.list (Range.linear 0 100) Gen.int32
        let custom = myCustomSort list
        let standard = List.sort list
        test <@ custom = standard @>
    }
    |> Property.check
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

[Fact]
public void CustomSort_Should_Match_StandardSort()
{
    var property =
        from list in Gen.Int32(Range.ConstantBoundedInt32()).List(Range.LinearInt32(0, 100)).ForAll()
        let custom = MyCustomSort(list)
        let standard = list.OrderBy(x => x).ToList()
        select custom.SequenceEqual(standard);

    property.Check();
}
```

---

### 6. Metamorphic: How Input Changes Affect Output

**Pattern**: How transforming the input should transform the output.

**When to use**: When you understand the mathematical or logical relationship between input and output transformations.

**Questions to ask**:
- If I double the input, what happens to the output?
- If I reverse the input, how does the output change?
- What transformations have predictable effects?

**Examples**:
- Doubling all elements doubles the sum
- Reversing input twice returns original
- Adding constant to all elements adds constant × count to sum
- Multiplying prices by 2 multiplies total by 2

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Swensen.Unquote

[<Fact>]
let ``Doubling elements should double the sum`` () =
    property {
        let! list = Gen.list (Range.linear 0 100) Gen.int32
        let originalSum = List.sum list
        let doubledSum = list |> List.map ((*) 2) |> List.sum
        test <@ doubledSum = originalSum * 2 @>
    }
    |> Property.check
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

[Fact]
public void DoublingElements_Should_Double_The_Sum()
{
    var property =
        from list in Gen.Int32(Range.ConstantBoundedInt32()).List(Range.LinearInt32(0, 100)).ForAll()
        let originalSum = list.Sum()
        let doubledSum = list.Select(x => x * 2).Sum()
        select doubledSum == originalSum * 2;

    property.Check();
}
```

---

### 7. Model-Based: Generate Valid Output First

**Pattern**: Generate the expected output first, then derive the input that should produce it.

**When to use**: When it's easier to generate valid output than to generate valid input, or when you want to avoid complex filtering.

**Questions to ask**:
- What does valid output look like?
- Can I work backwards from output to input?
- Is it easier to generate the result than the cause?

**Examples**:
- Generate event → derive command that should produce it
- Generate normalized form → derive denormalized input
- Generate valid parse result → derive string that should parse to it

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Swensen.Unquote

[<Fact>]
let ``Create account command should produce expected event`` () =
    property {
        let! expectedEvent = Gen.auto<AccountCreatedEvent>
        // Work backwards: derive command from event
        let command = 
            { AccountId = expectedEvent.AccountId
              Name = expectedEvent.Name
              InitialBalance = expectedEvent.InitialBalance }
        let actualEvent = accountService.Handle command
        test <@ actualEvent = expectedEvent @>
    }
    |> Property.check
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

var eventGen = /* define your AccountCreatedEvent generator */;

[Fact]
public void CreateAccountCommand_Should_Produce_Expected_Event()
{
    var property =
        from expectedEvent in eventGen.ForAll()
        // Work backwards: derive command from event
        let command = new CreateAccountCommand(
            expectedEvent.AccountId,
            expectedEvent.Name,
            expectedEvent.InitialBalance)
        let actualEvent = accountService.Handle(command)
        select actualEvent.Equals(expectedEvent);

    property.Check();
}
```

---

## Avoiding Overlapping Properties

**Critical Rule**: Before implementing tests, ensure your properties are distinct and non-overlapping.

### The Problem with Overlapping Properties

Multiple properties that verify the same underlying truth waste effort and create maintenance burden without adding value.

### The Elimination Workflow

1. **Discovery**: List ALL candidate properties you can think of
2. **Analysis**: Examine relationships between properties
3. **Elimination**: Remove redundancy using these criteria:
   - If property A failing → property B must fail: Keep the stronger one only
   - If property A is a special case of B: Keep B only
   - If same invariant, different wording: Keep the clearest one
4. **Proposal**: Present minimal set with justification

### Example: Sorting Function

**Initial candidates**:
1. Sorted list contains all original elements
2. Sorted list has same count as original
3. Sorted list has same elements with same frequencies
4. Sorted list is in ascending order
5. Sorting twice gives same result as sorting once

**Analysis**:
- Property 2 is weaker than property 1 (1 failing → 2 fails)
- Property 3 is weaker than property 1 (1 failing → 3 fails)
- Property 4 tests a different aspect (ordering, not preservation)
- Property 5 tests a different aspect (idempotence)

**Final minimal set**:
1. Sorted list contains all original elements (covers count and frequencies)
4. Sorted list is in ascending order
5. Sorting twice gives same result as sorting once

## Designing Effective Generators

### When to Use Auto-Generation

**Use auto-generation (default) when**:
- Testing with primitive types (int, string, bool, etc.)
- Working with simple domain objects with public constructors
- Testing collections of simple types
- You want to explore the full input space without constraints

### When to Create Custom Generators

**Create custom generators when**:
- You need constrained values (positive numbers, valid emails, etc.)
- Testing complex domain objects with invariants
- Values must follow specific business rules
- Working with recursive data structures
- You need specific distributions (e.g., mostly small values, rare large values)

### Generator Design Principles

**Coverage**: Generate the full valid input space, including edge cases (empty, zero, max, min, null, etc.)

**Relevance**: Focus on valid inputs. Test invalid cases separately with explicit invalid generators.

**Composition**: Build complex generators from simple ones using LINQ.

**Realistic**: Generate data that resembles production scenarios, not just random noise.

**Avoid Over-Filtering**: Don't use `where` clauses that reject most generated values.

### The Filtering Anti-Pattern

❌ **Bad** - Filtering rejects many values:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

// Bad: Rejects ~50% of generated values
gen {
    let! x = Gen.int32 (Range.constant -1000 1000)
    where (x > 0)
    return x
}

// Bad: Rejects 50% of values
gen {
    let! x = Gen.int32 (Range.constant -100 100)
    where (x % 2 = 0)
    return x
}
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

// Bad: Rejects ~50% of generated values
from x in Gen.Int32(Range.Constant(-1000, 1000)) 
where x > 0 
select x

// Bad: Rejects 50% of values
from x in Gen.Int32(Range.Constant(-100, 100)) 
where x % 2 == 0 
select x
```

---

✅ **Good** - Generate only valid values:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

// Good: Only generates positive values
Gen.int32 (Range.constant 1 1000)

// Good: Only generates even values
Gen.int32 (Range.constantBounded ()) |> Gen.map (fun x -> x &&& ~~~1)
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;

// Good: Only generates positive values
Gen.Int32(Range.Constant(1, 1000))

// Good: Only generates even values
Gen.Int32(Range.ConstantBoundedInt32()).Select(x => x & ~1)
```

---

**Rule of Thumb**: If your generator rejects more than 10-20% of values, redesign it to generate valid values directly.

## Common Anti-Patterns to Avoid

### Testing Anti-Patterns

❌ **Testing Implementation Details**: Don't test *how* code works, test *what* it does.
- Bad: Checking internal state or private fields
- Good: Checking observable behavior and guarantees

❌ **Over-Constraining Generators**: Don't make generators so specific they only produce passing values.
- Bad: Only generating sorted lists when testing a sort function
- Good: Generating any list and asserting it becomes sorted

❌ **Hidden Assumptions**: Don't assume specific generated values in your assertions.
- Bad: `result.Should().Be(42)` in a property test
- Good: `result.Should().BeGreaterThan(input)`

❌ **Example-Based Thinking in Property Tests**: Don't test specific values; test relationships.
- Bad: `[Property] void Test(int x) => Foo(5).Should().Be(10);`
- Good: `[Property] void Test(int x) => Foo(x).Should().Be(x * 2);`

❌ **Ignoring Shrinking Output**: The minimal failing case is the key to understanding the bug.
- Don't just see "test failed"
- Examine the shrunk input to understand why

❌ **Too Many Assertions in One Test**: Keep property tests focused on a single property.
- Bad: Testing 5 different invariants in one test
- Good: One property per test, 5 focused tests

❌ **Redundant Properties**: Don't create multiple tests that verify the same underlying property.
- Analyze relationships between properties
- Keep only distinct, non-overlapping tests

❌ **Weak Properties Instead of Strong Ones**: Don't write several weak properties when one stronger property would suffice.
- Bad: Separate tests for count, for element presence, for no duplicates
- Good: One test asserting "contains all original elements" (implies count, presence, and frequency)

### Generator Anti-Patterns

❌ **Insufficient Coverage**: Generators that miss important edge cases.
- Consider: empty, zero, negative, max, min, null, whitespace, Unicode

❌ **Unrealistic Data**: Generating values that would never occur in production.
- Bad: Random strings that don't resemble real data
- Good: Strings that look like actual names, emails, addresses

❌ **Over-Filtering**: Using `where` that rejects most generated values (>50% rejection).
- See "The Filtering Anti-Pattern" section above

## The Property Discovery Checklist

When analyzing code to test, systematically ask:

**Universal Truths**:
- ☐ What's **always true** about the output?
- ☐ What **can't possibly happen** if the code is correct?
- ☐ What would it mean for this to be **correct**?

**Reversibility**:
- ☐ Can I **undo** this operation?
- ☐ Is there an inverse operation?

**Stability**:
- ☐ Does doing this **twice** differ from **once**?
- ☐ Does it reach a stable state?

**Relationships**:
- ☐ What **relationships** must hold between input and output?
- ☐ How do input transformations affect output?

**Boundaries**:
- ☐ What happens at the **edges** (empty, zero, max, null)?
- ☐ What separates valid from invalid?

**Domain Rules**:
- ☐ What **business rules** must never be violated?
- ☐ What domain constraints exist?

**Comparison**:
- ☐ Is there a **reference implementation** to compare against?
- ☐ Is there a mathematical truth to verify?

## Practical Workflow

### 1. Start Simple
Begin with the most obvious property and auto-generated parameters.

### 2. Run and Observe
Let the generator explore the input space. Pay attention to failures.

### 3. Analyze Shrinking
When tests fail, examine the minimal failing case. What does it reveal?

### 4. Refine Generators
If auto-generation is too broad, add constraints with custom generators.

### 5. Iterate
Add more properties one at a time, ensuring each adds unique value.

### 6. Review for Overlap
Before finalizing, eliminate redundant properties.

## The Power of Property-Based Testing

**Coverage**: One property test exercises your code with hundreds of different inputs.

**Edge Cases**: Generators automatically explore boundaries you might not think of.

**Documentation**: Properties serve as executable specifications.

**Regression Protection**: Properties continue to verify behavior as code evolves.

**Bug Finding**: Random exploration often finds bugs that example-based tests miss.

**Minimal Failures**: Shrinking reveals the simplest case that breaks your code.

---

**Remember**: The goal is not to test *examples*, but to discover and verify *universal truths* about your code. Think in properties, not examples.
