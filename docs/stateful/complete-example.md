# Complete Example: Testing a Counter

This walkthrough will guide you through building a complete stateful test from scratch. We'll test a simple `Counter` class with increment, decrement, and reset operations.

## The System Under Test (SUT)

Let's start with a simple counter implementation:

# [F#](#tab/fsharp)

```fsharp
type Counter() =
    let mutable value = 0
    
    member _.Increment() = value <- value + 1
    member _.Decrement() = value <- value - 1
    member _.Reset() = value <- 0
    member _.Get() = value
```

# [C#](#tab/csharp)

```csharp
public sealed class Counter
{
    private int _value;

    public void Increment() => _value++;
    public void Decrement() => _value--;
    public void Reset() => _value = 0;
    public int Get() => _value;
}
```

---

This is our **System Under Test** (SUT). It maintains state (the current count) and provides operations that modify that state.

## The Model State

To test our counter, we need a model that represents what we *expect* the counter's state to be. Since our counter has a single integer value, our model is simple:

# [F#](#tab/fsharp)

```fsharp
type CounterState = {
    CurrentCount: Var<int>  // Symbolic reference to the counter's value
}
```

# [C#](#tab/csharp)

```csharp
public record CounterState
{
    public required Var<int> CurrentCount { get; init; }
}
```

---

The `Var<int>` is a **symbolic variable**—it represents "the value that operation X returned." This lets us track relationships between operations even before we know their concrete values.

## Defining Commands

Now we'll define each operation as a **Command**. 

Each command inherits from `Command<TSystem, TState, TInput, TOutput>` where:
- `TSystem` is our system under test type (`Counter`)
- `TState` is our model state type (`CounterState`)
- `TInput` is the input parameter type for this command
- `TOutput` is what the command returns

### The Increment Command

# [F#](#tab/fsharp)

```fsharp
type IncrementCommand() =
    inherit Command<Counter, CounterState, unit, int>()

    // Name for debugging/shrinking output
    override _.Name = "Increment"
    
    // Always allow this command
    override _.Precondition(state) = true
    
    // Execute the real operation on the SUT
    override _.Execute(counter, env, state, input) =
        counter.Increment()
        let result = counter.Get()
        Task.FromResult(result)
    
    // Generate inputs (none needed for increment)
    override _.Gen(state) = Gen.constant ()
    
    // Update our model state with the new value
    override _.Update(state, input, outputVar) = { CurrentCount = outputVar }
    
    // Assert the result is correct
    override _.Ensure(env, oldState, newState, input, result) =
        let oldCount = oldState.CurrentCount.Resolve(env)
        result = oldCount + 1
```

# [C#](#tab/csharp)

```csharp
public class IncrementCommand : Command<Counter, CounterState, NoInput, int>
{
    // Name for debugging/shrinking output
    public override string Name => "Increment";

    // Always allow this command
    public override bool Precondition(CounterState state) => true;

    // Execute the real operation on the SUT
    public override Task<int> Execute(Counter sut, Env env, CounterState state, NoInput input)
    {
        sut.Increment();
        var result = sut.Get();
        return Task.FromResult(result);
    }

    // Generate inputs (none needed for increment)
    public override Gen<NoInput> Generate(CounterState state) =>
        Gen.Constant(NoInput.Value);

    // Update our model state with the new value
    public override CounterState Update(CounterState state, NoInput input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    // Assert the result is correct
    public override bool Ensure(Env env, CounterState oldState, CounterState newState, NoInput input, int result)
    {
        var oldCount = oldState.CurrentCount.Resolve(env);
        return result == oldCount + 1;
    }
}
```

---

Let's break down each method:

1. **Name**: Used in test output to show which command failed
2. **Precondition**: Determines if this command can be generated in the current state (always `true` for simple commands)
3. **Execute**: Runs the actual operation and returns the new count
4. **Gen**: Generates random inputs (we use `unit` in F# or `NoInput` in C# since increment needs no meaningful input)
5. **Update**: Takes the output and creates the new model state
6. **Ensure**: Verifies the result is what we expected (old count + 1)

### The Decrement Command

The decrement command is almost identical:

# [F#](#tab/fsharp)

```fsharp
type DecrementCommand() =
    inherit Command<Counter, CounterState, unit, int>()

    override _.Name = "Decrement"
    
    override _.Precondition(state) = true
    
    override _.Execute(counter, env, state, input) =
        counter.Decrement()
        let result = counter.Get()
        Task.FromResult(result)
    
    override _.Gen _ = Gen.constant ()
    
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }
    
    override _.Ensure(env, oldState, newState, input, result) =
        let oldCount = oldState.CurrentCount.Resolve(env)
        result = oldCount - 1  // Should decrease by 1
```

# [C#](#tab/csharp)

```csharp
public class DecrementCommand : Command<Counter, CounterState, NoInput, int>
{
    public override string Name => "Decrement";

    public override bool Precondition(CounterState state) => true;

    public override Task<int> Execute(Counter sut, Env env, CounterState state, NoInput input)
    {
        sut.Decrement();
        var result = sut.Get();
        return Task.FromResult(result);
    }

    public override Gen<NoInput> Generate(CounterState state) =>
        Gen.Constant(NoInput.Value);

    public override CounterState Update(CounterState state, NoInput input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, CounterState oldState, CounterState newState, NoInput input, int result)
    {
        var oldCount = oldState.CurrentCount.Resolve(env);
        return result == oldCount - 1;  // Should decrease by 1
    }
}
```

---

### The Reset Command

Reset is interesting because it doesn't depend on the previous state:

# [F#](#tab/fsharp)

```fsharp
type ResetCommand() =
    inherit Command<Counter, CounterState, unit, int>()

    override _.Name = "Reset"
    
    override _.Precondition(state) = true
    
    override _.Execute(counter, env, state, input) =
        counter.Reset()
        let result = counter.Get()
        Task.FromResult(result)
    
    override _.Gen(state) = Gen.constant ()
    override _.Update(state, input, outputVar) = { CurrentCount = outputVar }
    override _.Ensure(env, oldState, newState, input, result) = result = 0
```

# [C#](#tab/csharp)

```csharp
public class ResetCommand : Command<Counter, CounterState, NoInput, int>
{
    public override string Name => "Reset";

    public override bool Precondition(CounterState state) => true;

    public override Task<int> Execute(Counter sut, Env env, CounterState state, NoInput input)
    {
        sut.Reset();
        var result = sut.Get();
        return Task.FromResult(result);
    }

    public override Gen<NoInput> Generate(CounterState state) =>
        Gen.Constant(NoInput.Value);

    public override CounterState Update(CounterState state, NoInput input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, CounterState oldState, CounterState newState, NoInput input, int result) =>
        result == 0; // Reset always returns 0
}
```

---

### A Get Command (for completeness)

Let's add a read-only operation to verify our model stays in sync:

# [F#](#tab/fsharp)

```fsharp
type GetCommand() =
    inherit Command<Counter, CounterState, unit, int>()

    override _.Name = "Get"
    
    override _.Precondition(state) = true
    
    override _.Execute(counter, env, state, input) = 
        Task.FromResult(counter.Get())
    
    override _.Gen(state) = Gen.constant ()
    
    override _.Update(state, input, outputVar) = { CurrentCount = outputVar }
    
    override _.Ensure(env, oldState, newState, input, result) =
        // Get should return exactly what's in our model
        result = oldState.CurrentCount.Resolve(env)
```

# [C#](#tab/csharp)

```csharp
public class GetCommand : Command<Counter, CounterState, NoInput, int>
{
    public override string Name => "Get";

    public override bool Precondition(CounterState state) => true;

    public override Task<int> Execute(Counter sut, Env env, CounterState state, NoInput input) =>
        Task.FromResult(sut.Get());

    public override Gen<NoInput> Generate(CounterState state) =>
        Gen.Constant(NoInput.Value);

    public override CounterState Update(CounterState state, NoInput input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, CounterState oldState, CounterState newState, NoInput input, int result)
    {
        // Get should return exactly what's in our model
        return result == oldState.CurrentCount.Resolve(env);
    }
}
```

---

## Creating the Specification

Now we tie it all together with a `SequentialSpecification`:

# [F#](#tab/fsharp)

```fsharp
type CounterSpec() =
    inherit SequentialSpecification<Counter, CounterState>()

    // Start each test from 0
    override _.SetupCommands = [| ResetCommand() |]

    // The initial model state
    override _.InitialState = { CurrentCount = Var.symbolic 0 }

    // How many commands to generate (1-50)
    override _.Range = Range.linear 1 50

    // All available commands
    override _.Commands = [|
        IncrementCommand()
        DecrementCommand()
        ResetCommand()
        GetCommand()
    |]
```

# [C#](#tab/csharp)

```csharp
public class CounterSpec : SequentialSpecification<Counter, CounterState>
{
    // Start each test from 0
    public override ICommand<Counter, CounterState>[] SetupCommands => 
        [new ResetCommand()];  

    // The initial model state
    public override CounterState InitialState => 
        new() { CurrentCount = Var.Symbolic(0) };

    // How many commands to generate (1-50)
    public override Range<int> Range => 
        Hedgehog.Linq.Range.LinearInt32(1, 50);

    // All available commands
    public override ICommand<Counter, CounterState>[] Commands =>
        [
            new IncrementCommand(),
            new DecrementCommand(),
            new ResetCommand(),
            new GetCommand()
        ];
}
```

---

## Running the Test

Finally, we write our test:

# [F#](#tab/fsharp)

```fsharp
[<Fact>]
let ``Counter behaves correctly under random operations``() =
    let sut = Counter()
    CounterSpec().ToProperty(sut).Check()
```

# [C#](#tab/csharp)

```csharp
[Fact]
public void CounterBehavesCorrectlyUnderRandomOperations()
{
    var sut = new Counter();
    new CounterSpec().ToProperty(sut).Check();
}
```

---

That's it! When you run this test, Hedgehog will:

1. Generate random sequences of commands (e.g., "Increment, Increment, Reset, Decrement, Get")
2. Execute each sequence against your actual counter
3. Verify each operation produces the expected result
4. If any assertion fails, shrink the sequence to find the minimal failing case

### Example Test Scenarios

Here are some sequences Hedgehog might generate:

**Simple sequence:**
```
1. Reset → expect 0
2. Increment → expect 1
3. Increment → expect 2
4. Get → expect 2
```

**Edge case:**
```
1. Reset → expect 0
2. Decrement → expect -1
3. Decrement → expect -2
4. Reset → expect 0
5. Get → expect 0
```

**Complex sequence:**
```
1. Increment → expect 1
2. Increment → expect 2
3. Decrement → expect 1
4. Increment → expect 2
5. Reset → expect 0
6. Increment → expect 1
```

If any command returns an unexpected value, Hedgehog will report exactly which sequence caused the failure and shrink it to the smallest reproduction case.

## Testing new behaviours

Let's assume that now `Counter` adds a new API method `Set`:

# [F#](#tab/fsharp)

```fsharp
type Counter() =
    member _.Set(n: int) = value <- n
    // the rest of the members
```

# [C#](#tab/csharp)

```csharp
public sealed class Counter
{
    public void Set(int n) => _value = n;
    // the rest of the members
}

```

---

To test it we need to define some `SetCommand`:

# [F#](#tab/fsharp)

```fsharp
type SetCommand() =
    inherit Command<Counter, CounterState, int, int>()

    override _.Name = "Set"
    
    override _.Precondition(state) = true
    
    override _.Execute(counter, _, value) =
        counter.Set(value)
        let result = counter.Get()
        Task.FromResult(result)
    
    // Generate random integers between -10 and 100
    override _.Gen _ = Gen.int32 (Range.linearFrom 0 -10 100)
    override _.Update(_, _, outputVar) = { CurrentCount = outputVar }
    override _.Ensure(_, _, _, input, result) = result = input
```

# [C#](#tab/csharp)

```csharp
public class SetCommand : Command<Counter, CounterState, int, int>
{
    public override string Name => "Set";

    public override bool Precondition(CounterState state) => true;

    public override Task<int> Execute(Counter sut, Env env, int value)
    {
        sut.Set(value);
        var result = sut.Get();
        return Task.FromResult(result);
    }

    // Generate random integers between -10 and 100
    public override Gen<int> Generate(CounterState state) =>
        Hedgehog.Linq.Gen.Int32(Range.LinearFromInt32(0, -10, 100));

    public override CounterState Update(CounterState state, int input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };

    public override bool Ensure(Env env, CounterState oldState, CounterState newState, int input, int result)
    {
        return result == input;  // Set should set the exact value
    }
}
```

---

Then add `SetCommand()` to your `Commands` array in the specification.

Now Hedgehog will use it in generating sequence and will generate sequences like:
```
1. Set(42) → expect 42
2. Increment → expect 43
3. Set(-5) → expect -5
4. Get → expect -5
```

## What About Bugs?

Let's introduce a bug in our counter to see what happens:

# [F#](#tab/fsharp)

```fsharp
member _.Decrement() = 
    if value > 10 then value <- value - 1
    else value <- value - 2  // SIMULATE BUG: decrements by 2 when <= 10
```

# [C#](#tab/csharp)

```csharp
public void Decrement()
{
    if (_value > 10) _value--;
    else _value -= 2;  // SIMULATE BUG: decrements by 2 when <= 10
}
```

---

When you run the test, Hedgehog will find this bug and report something like:

```
*** Failed! Falsifiable (after 8 tests and 1 shrink):

You can reproduce this failure with the following Recheck Seed:
  "7_13402950062986702852_14277380902303697685_0"

Generated values:
  { Initial = CounterState { CurrentCount = 0 (symbolic) }
    Steps = [Reset ; Decrement ] }

Counterexamples:
  Final state: CounterState { CurrentCount = 0 (symbolic) }
  + Reset 
  Decrement 
```

Hedgehog automatically found the minimal reproduction case: decrement once 
(you can see "`+ Reset`" in the list because `Reset` is defined as a Setup step and is always executed first).



## Summary

You've learned how to:

1. **Define a model state** that represents what you expect
2. **Create commands** for each operation on your SUT
3. **Implement the key methods** for each command:
   - `Name`: for debugging
   - `Precondition`: to control when the command can be generated
   - `Gen`: to generate inputs
   - `Execute`: to run the real operation
   - `Update`: to update the model state
   - `Ensure`: to verify correctness
4. **Create a specification** that ties everything together
5. **Run the test** and let Hedgehog find bugs automatically

The beauty of this approach is that Hedgehog explores hundreds or thousands of different sequences automatically, finding edge cases you might never think to test manually.
