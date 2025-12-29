# Commands

Commands are the heart of stateful testing. Each command represents a single operation you can perform on your system - like opening a door, adding an item to a cart, or incrementing a counter.

Hedgehog then uses these individual actions (commands) to build up valid sequences that simulate client's behaviour.

This page dives deep into the `Command` interface, explaining how commands work, when they execute, and how to use state to chain operations together.

## Understanding State

Before diving into commands, you need to understand how state works in stateful testing. 

State is shared throughout the lifecycle of testing: first during **sequence generation** (when Hedgehog builds up a series of commands to execute), and then during **sequence execution** (when those commands actually run against your system).

The key insight is that some parts of the state can be known during generation, while others only become known during execution:

- **Known at generation time**: When generating a sequence that includes `LockDoorCommand`, we know the door will be "Locked" for the next action in the sequence - even before executing anything. This is **concrete state**.
  
- **Known only at execution time**: If the system returns a lock code when the door is locked, we know *that a code exists* during generation, but the actual value is only available after execution. This is **symbolic state**.

We represent concrete state using regular properties (like `bool IsLocked`), and symbolic state using the `Var<T>` type (like `Var<string> LockCode`).

Here's an example of a door state that combines both:

# [F#](#tab/fsharp)

```fsharp
type DoorState = {
    IsLocked: bool           // Concrete: we know immediately after LockDoor
    LockCode: Var<string>    // Symbolic: actual code only known after execution
}
```

# [C#](#tab/csharp)

```csharp
public record DoorState
{
    public bool IsLocked { get; init; }      // Concrete: we know immediately after LockDoor
    public Var<string> LockCode { get; init; } // Symbolic: actual code only known after execution
}
```

---

**The key rule:** At generation time, you can only access the **concrete parts** of the state. You cannot resolve symbolic variables because execution hasn't happened yet!

## The Command Interface

Every command inherits from `Command<TSystem, TState, TInput, TOutput>` and implements these methods:

# [F#](#tab/fsharp)

```fsharp
type MyCommand() =
    inherit Command<MySystem, MyState, MyInput, MyOutput>()
    
    // 1. Name for reporting
    override _.Name = "MyCommand"
    
    // 2. Should this command be generated in this state?
    override _.Precondition(state) = true
    
    // 3. Generate input for this command
    override _.Generate(state) = 
        Gen.int32 (Range.linear 0 100)
    
    // 4. Should we execute this command with these inputs?
    override _.Require(env, state, input) = true
    
    // 5. Execute the real operation
    override _.Execute(sut, env, state, input) =
        let result = sut.DoSomething(input)
        Task.FromResult(result)
    
    // 6. Update the model state
    override _.Update(state, input, outputVar) =
        { state with Value = outputVar }
    
    // 7. Verify the result is correct
    override _.Ensure(env, oldState, newState, input, output) =
        // Check output matches expectations
        true
```

# [C#](#tab/csharp)

```csharp
public class MyCommand : Command<MySystem, MyState, MyInput, MyOutput>
{
    // 1. Name for reporting
    public override string Name => "MyCommand";
    
    // 2. Should this command be generated in this state?
    public override bool Precondition(MyState state) => true;
    
    // 3. Generate input for this command
    public override Gen<MyInput> Generate(MyState state) =>
        Gen.Int32(Range.LinearInt32(0, 100));
    
    // 4. Should we execute this command with these inputs?
    public override bool Require(Env env, MyState state, MyInput input) => true;
    
    // 5. Execute the real operation
    public override Task<MyOutput> Execute(MySystem sut, Env env, MyState state, MyInput input)
    {
        var result = sut.DoSomething(input);
        return Task.FromResult(result);
    }
    
    // 6. Update the model state
    public override MyState Update(MyState state, MyInput input, Var<MyOutput> outputVar) =>
        state with { Value = outputVar };
    
    // 7. Verify the result is correct
    public override bool Ensure(Env env, MyState oldState, MyState newState, MyInput input, MyOutput output) =>
        // Check output matches expectations
        true;
}
```

---

Let's understand each piece and how they work together.

### Understanding Env

The `Env` (environment) is a key concept that allows you to resolve symbolic state values into concrete values, giving you access to the real execution-time values.

**Methods with Env** (can resolve symbolic values):
- `Require` - Check preconditions using actual runtime values
- `Execute` - Perform operations using resolved values
- `Ensure` - Verify results using actual values

**Methods without Env** (can only access concrete state):
- `Generate` - Only has access to structural/concrete state
- `Update` - Works with symbolic variables, doesn't resolve them

During sequence generation, symbolic values don't have concrete runtime values yet, so `Generate` and `Update` cannot receive an `Env`. However, during execution, `Require`, `Execute`, and `Ensure` all receive an `Env` that lets you resolve any `Var<T>` to its actual runtime value.

## The Command Lifecycle

When Hedgehog builds and executes a test sequence, each command goes through these stages:

**During Generation:**
```text
1. Precondition → Should we include this command? (check concrete state only - no Env)
2. Generate     → Generate input for this command (only called if Precondition returns true)
3. Update       → Update model state symbolically (enables next command's Precondition check)
```

**During Execution:**
```text
1. Require      → Can we execute it now? (has Env - can resolve symbolic values)  
2. Execute      → Run the operation on the real system
3. Update       → Update model state symbolically (same call as generation, now env has real values)
4. Ensure       → Verify the result matches our expectations
```

### 1. Precondition: Deciding Whether to Generate

**When it runs:** During test *generation*, before execution

**Purpose:** Decide whether this command makes sense based on the **concrete structure** of the state

**Returns:** `true` to include the command (and call `Generate`), `false` to skip this command

**Critical:** You can only access concrete parts of the state here. No `Env` means no resolving symbolic variables!

For example, checking a simple boolean:

# [F#](#tab/fsharp)

```fsharp
type DoorState = {
    IsLocked: bool  // Concrete value we can check
}

type KnockKnockDoorCommand() =
    inherit Command<Door, DoorState, int, bool>()
    
    override _.Name = "KnockKnock"
    
    override _.Precondition(state) =
        // Check concrete state directly - no Env needed!
        state.IsLocked  // Only generate when door is locked
    
    override _.Generate(state) =
        // Generate the number of knocks
        Gen.int32 (Range.linear 1 5)
```

# [C#](#tab/csharp)

```csharp
public record DoorState
{
    public bool IsLocked { get; init; }  // Concrete value we can check
}

public class KnockKnockDoorCommand : Command<Door, DoorState, int, bool>
{
    public override string Name => "KnockKnock";
    
    public override bool Precondition(DoorState state)
    {
        // Check concrete state directly - no Env needed!
        return state.IsLocked;  // Only generate when door is locked
    }
    
    public override Gen<int> Generate(DoorState state)
    {
        // Generate the number of knocks
        return Gen.Int32(Range.LinearInt32(1, 5));
    }
}
```

---

**Key insight:** At generation time, you only know the *structure* - "the door is locked", "the stack has 3 items" - but not the actual values like "the lock code is 1234" or "the top item is 42". Those require execution.

### 2. Generate: Creating Command Input

**When it runs:** During test *generation*, after `Precondition` returns `true`

**Purpose:** Generate random input for this command

**Returns:** A `Gen<TInput>` generator that produces input values

**Important:** This is only called when `Precondition` returns `true`, so you can assume the structural preconditions are already satisfied.

### 3. Require: Runtime Precondition Check

**When it runs:** Just before execution, after the sequence is generated

**Purpose:** Verify that the command can still be executed with these specific inputs.

**Returns:** `true` to proceed with execution, `false` to skip this command (the test continues with remaining commands)

**Important:** Returning `false` does NOT fail the test - it simply skips the command. This is different from `Ensure`, which validates correctness.

**Key difference from Precondition:** `Require` DOES receive an `Env` parameter, so it CAN resolve symbolic variables! This is where you check concrete runtime values that weren't known during generation.

**When you need it:** The most common case is when your `Generate` method returns `Var<T>` as part of the input (e.g., picking from a list of symbolic IDs). Since previous commands can be skipped, those symbolic variables might not be bound at execution time. Override `Require` to check if such variables can actually be resolved before attempting to use them.

> **💡 Tip:** For a detailed explanation of when and how to use `Require`, including complete examples with symbolic variables in inputs, see [Runtime Preconditions](require.md).

**In practice:** The default `Require` implementation returns `true`, which works for most commands that don't use symbolic variables as input.

### 4. Execute: Running the Real Operation

**When it runs:** During test execution

**Purpose:** Perform the actual operation on your system under test

**Returns:** A `Task<TOutput>` with the result

This is straightforward - call your system's method and return the result:

# [F#](#tab/fsharp)

```fsharp
override _.Execute(sut, env, state, input) =
    let result = sut.IncrementCounter()
    Task.FromResult(result)
```

# [C#](#tab/csharp)

```csharp
public override Task<int> Execute(Counter sut, Env env, CounterState state, bool input)
{
    var result = sut.Increment();
    return Task.FromResult(result);
}
```

---

**Important:** The `Execute` method receives:
- `sut`: Your system under test (the real object)
- `env`: The environment with resolved values from previous commands
- `state`: The current model state
- `input`: The generated input for this command

Use `env` to resolve any symbolic variables you need from the state:

# [F#](#tab/fsharp)

```fsharp
override _.Execute(sut, env, state, input) =
    let code = state.LockCode.Resolve(env)
    sut.UnlockDoorAsync(code, input)
```

# [C#](#tab/csharp)

```csharp
public override Task<bool> Execute(DoorState sut, Env env, CartState state, string input)
{
    var code = state.LockCode.Resolve(env);
    return sut.UnlockDoorAsync(code, input);
}
```

---

### 5. Update: Tracking Symbolic State

**When it runs:** During *both* generation **and** execution phases - after each command in the sequence

**Purpose:** Update your model state with the new symbolic output to evolve the structural state

**Returns:** The new state

**Critical insight:** `Update` is called during *generation* (before any real execution happens) to maintain the structural state. 
This allows subsequent commands in the sequence to evaluate their `Precondition` based on the evolved state. 
During *execution*, these variables are bound to actual runtime values in the `Env`.

This is where you store the command's output as a **symbolic variable** (`Var<TOutput>`) that future commands can reference:

# [F#](#tab/fsharp)

```fsharp
override _.Update(state, input, outputVar) =
    { state with CurrentCount = outputVar }
```

# [C#](#tab/csharp)

```csharp
public override CounterState Update(CounterState state, bool input, Var<int> outputVar) =>
    state with { CurrentCount = outputVar };
```

---

The `outputVar` is a symbolic reference to this command's output. Later commands can resolve it to get the actual value.

#### Projecting Fields from Structured Outputs

When a command returns a structured type (like a record or class), you often want to store individual fields in your state rather than the entire object. Use `Var.map` (F#) or `.Select()` (C#) to project fields from the output:

# [F#](#tab/fsharp)

```fsharp
// Command that returns a structured Person type
type Person = {
    Name: string
    Age: int
}

type RegistryState = {
    LastPersonName: Var<string>
    LastPersonAge: Var<int>
}

type AddPersonCommand() =
    inherit Command<PersonRegistry, RegistryState, string * int, Person>()
    
    override _.Execute(sut, env, state, (name, age)) =
        let person = sut.AddPerson(name, age)
        Task.FromResult(person)
    
    // Project individual fields from the Person output
    override _.Update(state, input, personVar) =
        { LastPersonName = Var.map (fun p -> p.Name) personVar
          LastPersonAge = Var.map (fun p -> p.Age) personVar }
```

# [C#](#tab/csharp)

```csharp
// Command that returns a structured Person type
public record Person(string Name, int Age);

public record RegistryState
{
    public Var<string> LastPersonName { get; init; }
    public Var<int> LastPersonAge { get; init; }
}

public class AddPersonCommand : Command<PersonRegistry, RegistryState, (string, int), Person>
{
    public override Task<Person> Execute(PersonRegistry sut, Env env, RegistryState state, (string, int) input)
    {
        var (name, age) = input;
        var person = sut.AddPerson(name, age);
        return Task.FromResult(person);
    }
    
    // Project individual fields from the Person output
    public override RegistryState Update(RegistryState state, (string, int) input, Var<Person> personVar) =>
        state with 
        { 
            LastPersonName = personVar.Select(p => p.Name),
            LastPersonAge = personVar.Select(p => p.Age)
        };
}
```

---

**How it works:** Both projected variables (`LastPersonName` and `LastPersonAge`) share the same underlying variable name - they point to the same `Person` object in the environment. When you resolve them, the projection function is applied to extract the specific field.

**You can chain projections:**

# [F#](#tab/fsharp)

```fsharp
override _.Update(state, input, personVar) =
    let nameVar = Var.map (fun p -> p.Name) personVar
    let nameLengthVar = Var.map String.length nameVar
    { state with NameLength = nameLengthVar }
```

# [C#](#tab/csharp)

```csharp
public override RegistryState Update(RegistryState state, (string, int) input, Var<Person> personVar)
{
    var nameVar = personVar.Select(p => p.Name);
    var nameLengthVar = nameVar.Select(name => name.Length);
    return state with { NameLength = nameLengthVar };
}
```

---

This is particularly useful when you need to pass different parts of a command's output to different subsequent commands.

### 6. Ensure: Verifying Correctness

**When it runs:** After execution and state update

**Purpose:** Assert that the output matches expectations

**Returns:** `true` if the assertion passes, `false` or throw an exception if it fails

This is your postcondition check:

# [F#](#tab/fsharp)

```fsharp
override _.Ensure(env, oldState, newState, input, output) =
    let oldCount = oldState.CurrentCount.Resolve(env)
    output = oldCount + 1  // Increment should increase by exactly 1
```

# [C#](#tab/csharp)

```csharp
public override bool Ensure(Env env, CounterState oldState, CounterState newState, bool input, int output)
{
    var oldCount = oldState.CurrentCount.Resolve(env);
    return output == oldCount + 1;  // Increment should increase by exactly 1
}
```

---

You can also throw exceptions for more detailed error messages:

# [F#](#tab/fsharp)

```fsharp
override _.Ensure(env, oldState, newState, input, output) =
    let oldCount = oldState.CurrentCount.Resolve(env)
    if output <> oldCount + 1 then
        failwith $"Expected {oldCount + 1}, got {output}"
    true
```

# [C#](#tab/csharp)

```csharp
public override bool Ensure(Env env, CounterState oldState, CounterState newState, bool input, int output)
{
    var oldCount = oldState.CurrentCount.Resolve(env);
    Assert.Equal(output, oldCount + 1);
    return true;
}
```

---


## Commands Without Outputs

Sometimes operations don't return meaningful values - they just perform side effects. For these, use `ActionCommand<TSystem, TState, TInput>`:

# [F#](#tab/fsharp)

```fsharp
type CloseConnectionCommand() =
    inherit ActionCommand<Database, DbState, unit>()
    
    override _.Name = "CloseConnection"
    
    override _.Precondition(state) = true
    
    override _.Generate(state) = 
        Gen.constant ()
    
    override _.Execute(sut, env, state, input) =
        sut.CloseConnection()
        Task.CompletedTask  // No return value
    
    override _.Update(state, input) =
        { state with IsConnected = false }  // No output var
    
    override _.Ensure(env, oldState, newState, input) =
        // Verify side effect without checking output
        true
```

# [C#](#tab/csharp)

```csharp
public class CloseConnectionCommand : ActionCommand<Database, DbState, NoInput>
{
    public override string Name => "CloseConnection";
    
    public override bool Precondition(DbState state) => true;
    
    public override Gen<NoInput> Generate(DbState state) =>
        Gen.Constant(NoInput.Value);
    
    public override Task Execute(Database sut, Env env, DbState state, NoInput input)
    {
        sut.CloseConnection();
        return Task.CompletedTask;  // No return value
    }
    
    public override DbState Update(DbState state, NoInput input) =>
        state with { IsConnected = false };  // No output var
    
    public override bool Ensure(Env env, DbState oldState, DbState newState, NoInput input) =>
        true;
}
```

---
