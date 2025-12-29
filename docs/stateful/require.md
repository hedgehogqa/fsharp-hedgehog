# Runtime Preconditions with Require

When you generate a test sequence, Hedgehog creates a plan of actions to execute. But what happens when that plan includes references to values that might not exist when execution time arrives?

This is where the `Require` method becomes essential.

## The Problem: Symbolic Variables as Input

Consider a user registry system where commands can look up previously registered users. Your state might track registered user IDs:

# [F#](#tab/fsharp)

```fsharp
type RegistryState = {
    RegisteredIds: Var<int> list  // List of IDs from previous Register commands
}
```

# [C#](#tab/csharp)

```csharp
public record RegistryState
{
    public List<Var<int>> RegisteredIds { get; init; } = new();
}
```

---

When generating a `LookupUser` command, you need to pick an ID from this list:

# [F#](#tab/fsharp)

```fsharp
type LookupUserCommand() =
    inherit Command<UserRegistry, RegistryState, Var<int>, UserInfo option>()
    
    override _.Precondition(state) =
        not (List.isEmpty state.RegisteredIds)
    
    override _.Generate(state) =
        Gen.item state.RegisteredIds  // Returns a Var<int> from the list
```

# [C#](#tab/csharp)

```csharp
public class LookupUserCommand : Command<UserRegistry, RegistryState, Var<int>, UserInfo?>
{
    public override bool Precondition(RegistryState state) =>
        state.RegisteredIds.Count > 0;
    
    public override Gen<Var<int>> Generate(RegistryState state) =>
        Gen.Element(state.RegisteredIds);  // Returns a Var<int> from the list
}
```

---

**Notice:** `Generate` returns a `Var<int>` - a symbolic reference to an ID that some previous command produced.

### The Hidden Danger: Shrinking Can Leave Variables Unbound

Here's the subtle problem. Imagine Hedgehog generates this sequence (which passes):

```text
1. RegisterUser("Alice") → Var₁
2. RegisterUser("Bob")   → Var₂  
3. RegisterUser("Charlie") → Var₃
4. LookupUser(Var₂)      ← succeeds
5. DeleteUser(Var₃)      
6. SomeOtherOperation()  ← THIS FAILS!
```

The test fails at step 6. Now Hedgehog tries to **shrink** the sequence to find the minimal failing case. It might try removing step 2:

```text
1. RegisterUser("Alice") → Var₁
   [step 2 removed - "Bob" never registered]
3. RegisterUser("Charlie") → Var₃
4. LookupUser(Var₂)      ← CRASH! Var₂ is unbound
5. DeleteUser(Var₃)      
6. SomeOtherOperation()
```

**What happened?**

- When step 4 was originally generated, the state had `[Var₁, Var₂]` in `RegisteredIds`
- `Generate` picked `Var₂` randomly from that list
- During shrinking, step 2 (which produced `Var₂`) was removed
- Step 4 still has `Var₂` as its input, but `Var₂` was never bound to a concrete value
- Without `Require`, calling `Var₂.Resolve(env)` will throw an exception

This isn't a problem with the test logic—it's a fundamental aspect of how shrinking works. Actions are removed from sequences, but the inputs to remaining actions are fixed when they're generated.

**The fix:** Override `Require` to check `idVar.TryResolve(env, out var _)`. During shrinking, if the var can't be resolved, `Require` returns `false` and the action is skipped (not failed). The shrinking process continues and finds a valid minimal sequence.

## The Solution: Override Require

The `Require` method is your runtime safety check. Unlike `Precondition` (which only sees the structural state during generation), `Require` receives the `Env` parameter and can resolve symbolic variables to check if they're actually valid.

Here's the safe version:

# [F#](#tab/fsharp)

```fsharp
type LookupUserCommand() =
    inherit Command<UserRegistry, RegistryState, Var<int>, UserInfo option>()
    
    override _.Name = "LookupUser"
    
    // Structural precondition: we need at least one ID in our list
    override _.Precondition(state) =
        not (List.isEmpty state.RegisteredIds)
    
    // Pick a random ID from the list
    override _.Generate(state) =
        Gen.item state.RegisteredIds
    
    // Runtime check: can we actually resolve this ID?
    override _.Require(env, state, idVar) =
        idVar |> Var.tryResolve env |> Result.isOk
    
    override _.Execute(registry, env, state, idVar) =
        let actualId = idVar.Resolve(env)
        Task.FromResult(registry.Lookup(actualId))
    
    override _.Ensure(env, oldState, newState, idVar, result) =
        result.IsSome  // Should always find the user
```

# [C#](#tab/csharp)

```csharp
public class LookupUserCommand : Command<UserRegistry, RegistryState, Var<int>, UserInfo?>
{
    public override string Name => "LookupUser";
    
    // Structural precondition: we need at least one ID in our list
    public override bool Precondition(RegistryState state) =>
        state.RegisteredIds.Count > 0;
    
    // Pick a random ID from the list
    public override Gen<Var<int>> Generate(RegistryState state) =>
        Gen.Element(state.RegisteredIds);
    
    // Runtime check: can we actually resolve this ID?
    public override bool Require(Env env, RegistryState state, Var<int> idVar) =>
        idVar.TryResolve(env, out var _);  // Returns false if not bound yet
    
    public override Task<UserInfo?> Execute(UserRegistry sut, Env env, RegistryState state, Var<int> idVar)
    {
        var actualId = idVar.Resolve(env);
        return Task.FromResult(sut.Lookup(actualId));
    }
    
    public override bool Ensure(Env env, RegistryState oldState, RegistryState newState, Var<int> idVar, UserInfo? result) =>
        result is not null;  // Should always find the user
}
```

---

**What changes:**

1. We override `Require` to check if `idVar` can actually be resolved
2. If `TryResolve` returns `false`, the command is **skipped** (not failed!)
3. The test continues with the remaining actions

This prevents trying to lookup a user ID that was never actually created.

## When to Override Require

**Override `Require` when:**

1. **Your `Generate` returns `Var<T>` as part of the input** - You're selecting from a list of symbolic variables
2. **Commands can be skipped** - Previous commands might not execute, leaving vars unbound
3. **You need runtime value checks** - Validate concrete values that weren't available during generation

**You can skip `Require` when:**

1. **`Generate` returns concrete values only** - No symbolic variables in the input
2. **Simple preconditions** - Structural checks that `Precondition` already handles
3. **All commands always execute** - No risk of skipped actions (rare in practice)

## Best Practices

### 1. Use TryResolve for Safety

Don't use `Resolve` directly in `Require` - it throws if the variable isn't bound. 

In F#, use the idiomatic `Var.tryResolve` which returns a `Result`:

# [F#](#tab/fsharp)

```fsharp
override _.Require(env, state, input) =
    input.SomeVarField |> Var.tryResolve env |> Result.isOk
```

# [C#](#tab/csharp)

In C#, use `TryResolve` with an out parameter:

```csharp
public override bool Require(Env env, MyState state, MyInput input) =>
    input.SomeVarField.TryResolve(env, out var _);
```

---

### 2. Check the Symbolic Inputs You Use

If your command's input contains `Var<T>` fields that come from previous command outputs, and your `Execute` method will resolve them, you should check those in `Require`:

# [F#](#tab/fsharp)

```fsharp
type TransferInput = {
    FromAccount: Var<AccountId>  // From a previous CreateAccount command
    ToAccount: Var<AccountId>    // From a previous CreateAccount command
    Amount: decimal
}

override _.Require(env, state, input) =
    // Check both vars since Execute will resolve both
    (input.FromAccount |> Var.tryResolve env |> Result.isOk) &&
    (input.ToAccount |> Var.tryResolve env |> Result.isOk)
```

# [C#](#tab/csharp)

```csharp
public record TransferInput
{
    // From previous CreateAccount commands
    public required Var<AccountId> FromAccount { get; init; }
    public required Var<AccountId> ToAccount { get; init; }
    public decimal Amount { get; init; }
}

public override bool Require(Env env, BankState state, TransferInput input) =>
    // Check both vars since Execute will resolve both
    input.FromAccount.TryResolve(env, out var _) &&
    input.ToAccount.TryResolve(env, out var _);
```

---

**Important distinctions:**

- **Vars from command inputs** (produced by `Generate` picking from state lists): These can become unbound during shrinking and need to be checked in `Require`
- **Vars in the state** (created with `Var.symbolic` in `InitialState`): These always have default values and are always resolvable, so they don't need `Require` checks
- **Only check what you use**: Only check the `Var<T>` fields that your `Execute` method actually resolves. If your command doesn't use a particular var from the input, you don't need to check it in `Require`

## Require vs Precondition

It's important to understand the difference between these two methods:

| Aspect | Precondition | Require |
|--------|-------------|---------|
| **When called** | During generation & execution | Only during execution |
| **Has Env?** | ❌ No - cannot resolve vars | ✅ Yes - can resolve vars |
| **Purpose** | Structural check ("do we have IDs?") | Runtime check ("can we resolve this ID?") |
| **Return false means** | Don't generate this command | Skip this command (continue test) |
| **Check frequency** | Every candidate command | Only generated commands |

**In practice:** Use `Precondition` for structural state checks, and `Require` for validating symbolic variable inputs.

## Summary

**The Rule:** If your `Generate` method returns a `Var<T>` (or contains one as a part of a generated input), 
always override `Require` to check if that variable can be resolved.

**Why:** Commands can be skipped during execution, leaving symbolic variables unbound. 
Without `Require`, you'll crash trying to resolve non-existent values.

**How:** Use `TryResolve` to safely check if a variable is bound before attempting to use it in `Execute`.

This pattern keeps your stateful tests robust and prevents mysterious failures from unresolved symbolic variables.
