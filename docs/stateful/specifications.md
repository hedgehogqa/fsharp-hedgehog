# Specifications

Once you've defined your commands, you need a **specification** that ties everything together. A specification declares:

- The initial state of your model
- Which commands are available
- How many actions to generate
- Whether to test sequentially or in parallel

Hedgehog provides two types of specifications:

- **SequentialSpecification**: Tests sequences of operations running one after another
- **ParallelSpecification**: Tests concurrent operations to verify linearizability

## Sequential Specifications

Sequential specifications test your system by generating and executing sequences of commands one at a time. This is the most common type of stateful test.

### Basic Structure

# [F#](#tab/fsharp)

```fsharp
type CounterSpec() =
    inherit SequentialSpecification<Counter, CounterState>()
    
    // The starting state of your model
    override _.InitialState = { CurrentCount = Var.symbolic 0 }
    
    // How many actions to generate (1 to 10)
    override _.Range = Range.linear 1 10
    
    // Available commands
    override _.Commands = [|
        IncrementCommand()
        DecrementCommand()
        ResetCommand()
    |]
```

# [C#](#tab/csharp)

```csharp
public class CounterSpec : SequentialSpecification<Counter, CounterState>
{
    // The starting state of your model
    public override CounterState InitialState => 
        new() { CurrentCount = Var.Symbolic(0) };
    
    // How many actions to generate (1 to 10)
    public override Range<int> Range => Range.Linear(1, 10);
    
    // Available commands
    public override ICommand<Counter, CounterState>[] Commands => 
        [
            new IncrementCommand(),
            new DecrementCommand(),
            new ResetCommand()
        ];
}
```

---

### Running the Test

Convert the specification to a property and check it as a regular property:

# [F#](#tab/fsharp)

```fsharp
[<Fact>]
let ``Counter behaves correctly``() =
    // obtain sut somehow
    let sut = Counter()
    CounterSpec().ToProperty(sut).Check()
```

# [C#](#tab/csharp)

```csharp
[Fact]
public void Counter_BehavesCorrectly()
{
    // obtain sut somehow
    var sut = new Counter();
    new CounterSpec().ToProperty(sut).Check();
}
```

---

### Setup and Cleanup

Sequential specifications support setup and cleanup commands that run before and after each test sequence:

# [F#](#tab/fsharp)

```fsharp
type CounterSpec() =
    inherit SequentialSpecification<Counter, CounterState>()
    
    override _.InitialState = { CurrentCount = Var.symbolic 0 }
    override _.Range = Range.linear 1 10
    
    // Setup runs BEFORE the test sequence
    override _.SetupCommands = [|
        InitializeCommand()  // e.g., set counter to a random value
    |]
    
    // Main test commands
    override _.Commands = [|
        IncrementCommand()
        DecrementCommand()
    |]
    
    // Cleanup runs AFTER the test sequence
    override _.CleanupCommands = [|
        ResetCommand()  // e.g., reset counter to 0
    |]
```

# [C#](#tab/csharp)

```csharp
public class CounterSpec : SequentialSpecification<Counter, CounterState>
{
    public override CounterState InitialState => 
        new() { CurrentCount = Var.Symbolic(0) };
    
    public override Range<int> Range => Range.Linear(1, 10);
    
    // Setup runs BEFORE the test sequence
    public override ICommand<Counter, CounterState>[] SetupCommands => 
        [ 
            InitializeCommand()  // e.g., set counter to a random value
        ];
    
    // Main test commands
    public override ICommand<Counter, CounterState>[] Commands => 
        [
            new IncrementCommand(),
            new DecrementCommand()
        ];
    
    // Cleanup runs AFTER the test sequence
    public override ICommand<Counter, CounterState>[] CleanupCommands => 
        [
            new ResetCommand()  // e.g., reset counter to 0
        ];
}
```

---

**Key points about setup and cleanup:**

- Setup commands execute **in order** before test commands.
- Cleanup commands execute **in order** after test commands.
- Cleanup runs **even if the test fails**
- Both setup and cleanup commands can have their **parameters shrink**, but they cannot be removed from the sequence during shrinking.

## Parallel Specifications

Parallel specifications test whether your system is **linearizable**—meaning concurrent operations behave as if they executed in *some* sequential order.

### What is Linearizability?

When operations run concurrently, they can interfere with each other. A linearizable system guarantees that despite this interference, the results match what *could have happened* if the operations ran sequentially.

For example, if two threads both increment a counter, linearizability ensures the counter increases by 2—not 1 (which would indicate a lost update).

### How Parallel Testing Works

Parallel testing has three phases:

1. **Sequential Prefix**: Run some operations sequentially to set up initial state
2. **Parallel Branches**: Run two sequences of operations **in parallel** on the same system
3. **Linearizability Check**: Verify the results match *some* interleaving of the operations

### Basic Structure

# [F#](#tab/fsharp)

```fsharp
type ParallelCounterSpec() =
    inherit ParallelSpecification<ThreadSafeCounter, CounterState>()
    
    // Starting state
    override _.InitialState = { CurrentCount = Var.symbolic 0 }
    
    // Length of sequential prefix (0-3 operations)
    override _.PrefixRange = Range.linear 0 3
    
    // Length of each parallel branch (1-5 operations each)
    override _.BranchRange = Range.linear 1 5
    
    // Commands that can run in parallel
    override _.Commands = [|
        IncrementCommand()
        DecrementCommand()
        GetCommand()
    |]
```

# [C#](#tab/csharp)

```csharp
public class ParallelCounterSpec : ParallelSpecification<ThreadSafeCounter, CounterState>
{
    // Starting state
    public override CounterState InitialState => 
        new() { CurrentCount = Var.Symbolic(0) };
    
    // Length of sequential prefix (0-3 operations)
    public override Range<int> PrefixRange => Range.Linear(0, 3);
    
    // Length of each parallel branch (1-5 operations each)
    public override Range<int> BranchRange => Range.Linear(1, 5);
    
    // Commands that can run in parallel
    public override ICommand<ThreadSafeCounter, CounterState>[] Commands => 
        [
            new IncrementCommand(),
            new DecrementCommand(),
            new GetCommand()
        ];
}
```

---

### Running Parallel Tests

# [F#](#tab/fsharp)

```fsharp
[<Fact>]
let ``Counter is thread-safe``() =
    let sut = ThreadSafeCounter()
    ParallelCounterSpec().ToProperty(sut).Check()
```

# [C#](#tab/csharp)

```csharp
[Fact]
public void Counter_IsThreadSafe()
{
    var sut = new ThreadSafeCounter();
    new ParallelCounterSpec().ToProperty(sut).Check();
}
```

---

### Important: Sharing the SUT

**The same SUT instance is intentionally shared between parallel branches.** This is not a bug - it's the whole point! 

We *want* operations to interfere with each other because we're testing whether the system handles concurrent access correctly. If the system is properly thread-safe, all results will be linearizable. If it has concurrency bugs, some test runs will produce results that can't be explained by any sequential ordering.

### Writing Commands for Parallel Testing

When writing commands for parallel testing, remember:

1. **Preconditions and Require** still check the model state
2. **Ensure** should verify weak postconditions—linearizability checking handles the strong guarantees
3. Return values matter—the framework uses them to verify linearizability

Example of a weak postcondition in parallel context:

# [F#](#tab/fsharp)

```fsharp
type IncrementCommand() =
    inherit Command<ThreadSafeCounter, CounterState, unit, int>()
    
    override _.Name = "Increment"
    override _.Precondition(state) = true
    override _.Execute(counter, env, state, input) = 
        Task.FromResult(counter.Increment())
    override _.Generate(state) = Gen.constant ()
    override _.Update(state, input, outputVar) = { CurrentCount = outputVar }
    
    // Weak postcondition: we can't assert the exact value
    // Linearizability checking verifies the sequence is valid
    override _.Ensure(env, oldState, newState, input, result) =
        result > 0  // After increment, should be positive
```

# [C#](#tab/csharp)

```csharp
public class IncrementCommand : Command<ThreadSafeCounter, CounterState, NoInput, int>
{
    public override string Name => "Increment";
    public override bool Precondition(CounterState state) => true;
    
    public override Task<int> Execute(ThreadSafeCounter sut, Env env, CounterState state, NoInput input) =>
        Task.FromResult(sut.Increment());
    
    public override Gen<NoInput> Generate(CounterState state) => 
        Gen.Constant(NoInput.Value);
    
    public override CounterState Update(CounterState state, NoInput input, Var<int> outputVar) =>
        state with { CurrentCount = outputVar };
    
    // Weak postcondition: we can't assert the exact value
    // Linearizability checking verifies the sequence is valid
    public override bool Ensure(Env env, CounterState oldState, CounterState newState, NoInput input, int result) =>
        result > 0;  // After increment, should be positive
}
```

---

### Setup and Cleanup in Parallel Tests

Parallel specifications also support setup and cleanup:

# [F#](#tab/fsharp)

```fsharp
type ParallelCounterSpec() =
    inherit ParallelSpecification<ThreadSafeCounter, CounterState>()
    
    override _.InitialState = { CurrentCount = Var.symbolic 0 }
    override _.PrefixRange = Range.linear 0 3
    override _.BranchRange = Range.linear 1 5
    
    // Setup runs BEFORE prefix and parallel branches
    override _.SetupCommands = [|
        InitializeCommand()
    |]
    
    override _.Commands = [|
        IncrementCommand()
        DecrementCommand()
    |]
    
    // Cleanup runs AFTER parallel branches complete
    override _.CleanupCommands = [|
        ResetCommand()
    |]
```

# [C#](#tab/csharp)

```csharp
public class ParallelCounterSpec : ParallelSpecification<ThreadSafeCounter, CounterState>
{
    public override CounterState InitialState => 
        new() { CurrentCount = Var.Symbolic(0) };
    
    public override Range<int> PrefixRange => Range.Linear(0, 3);
    public override Range<int> BranchRange => Range.Linear(1, 5);
    
    // Setup runs BEFORE prefix and parallel branches
    public override ICommand<ThreadSafeCounter, CounterState>[] SetupCommands =>
        [ new InitializeCommand() ];
    
    public override ICommand<ThreadSafeCounter, CounterState>[] Commands => 
        [
            new IncrementCommand(),
            new DecrementCommand()
        ];
    
    // Cleanup runs AFTER parallel branches complete
    public override ICommand<ThreadSafeCounter, CounterState>[] CleanupCommands => 
        [ new ResetCommand() ];
}
```

---

**Note:** Cleanup is generated using the state **after the prefix** (before parallel execution), since the parallel branches can't be used to predict a single final state. But it is **executed** at the end of the whole sequence.

## Using Fresh SUTs

Instead of creating the SUT yourself and passing it to the specification, you can provide a factory function that creates a fresh SUT for each test run:

# [F#](#tab/fsharp)

```fsharp
[<Fact>]
let ``Counter test with fresh SUT``() =
    // Factory creates a new Counter for each test
    let createSut() = Counter()
    
    CounterSpec().ToPropertyWith(createSut).Check()
```

# [C#](#tab/csharp)

```csharp
[Fact]
public void Counter_TestWithFreshSUT()
{
    // Factory creates a new Counter for each test
    Func<Counter> createSut = () => new Counter();
    
    new CounterSpec().ToPropertyWith(createSut).Check();
}
```

---

This approach ensures test isolation—each test run gets a completely fresh system.

## Summary

**Sequential Specifications:**
- Test operations running one after another
- Use `SequentialSpecification<TSystem, TState>`
- Define `InitialState`, `Range`, and `Commands`
- Optionally add `SetupCommands` and `CleanupCommands`

**Parallel Specifications:**
- Test concurrent operations for linearizability
- Use `ParallelSpecification<TSystem, TState>`
- Define `InitialState`, `PrefixRange`, `BranchRange`, and `Commands`
- The same SUT is intentionally shared between parallel branches
- Linearizability checking verifies results match some sequential interleaving
- Optionally add `SetupCommands` and `CleanupCommands`

Both specifications provide:
- Automatic shrinking of test sequences
- Setup/cleanup command support
- Clear failure reporting
- Integration with property-based testing frameworks
