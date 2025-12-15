# What is stateful testing?

Most property-based tests check individual operations in isolation: "if I sort this list, is it actually sorted?" But real systems have state that changes over time. A user logs in, makes changes, logs out. A door locks and unlocks. A shopping cart adds and removes items.

**Stateful testing** lets you test sequences of actions on systems that maintain state.

Instead of testing one operation at a time, you define each possible action as a **Command**. Hedgehog then generates random sequences of these commands - essentially, complete scenarios of how your system might be used. When something goes wrong, Hedgehog automatically shrinks both the sequence of actions *and* their parameters to find you the minimal failing case.

Imagine testing a login system. Instead of manually writing test cases like "login, then logout" or "login, change password, logout," Hedgehog generates hundreds of different sequences and finds edge cases you never thought of—like what happens when you try to change a password twice in a row, or logout when already logged out.

## Building blocks: Commands

Let's build up the idea step by step. We'll use pseudocode to focus on concepts rather than specific syntax.

#### Step 1: Naming your command

Every command needs a name for clarity:

```pseudocode
Command {
    Name: "LogIn"
}
```

#### Step 2: Defining preconditions

Here's where stateful testing gets interesting. Not all commands make sense at all times. You can't log out if you're not logged in. You can't lock a door that's already locked.

We track this with a **model state**—a simple state representing what we know from interacting with the system. Commands can check this state and decide whether they're allowed to run:

```pseudocode
Command {
    Name: "LogOut"

    Precondition(currentState): 
        currentState.isLoggedIn  // Only generate when logged in
}
```

This way, Hedgehog only generates valid sequences. The `LogOut` command appears only when the user is logged in (when `Precondition` returns true). The `LockDoor` command appears only when the door is unlocked.

#### Step 3: Generating command inputs

Just like regular property tests, commands need test data. Each command defines how to generate its input:

```pseudocode
Command {
    Name: "LogIn"
    Precondition(currentState): currentState.isLoggedIn

    Gen: generateRandomUsername()
}
```

#### Step 4: Executing the command

Now we need to actually *run* the command against the real system:

```pseudocode
Command {
    Name: "LogIn"
    Precondition(currentState): currentState.isLoggedIn
    Gen(currentState): generateRandomUsername()
    
    Execute(input):
        actualSystem.login(input.username)
}
```

The `Execute` method takes the generated input and performs the actual operation, returning whatever the system returns.

#### Step 5: Updating the model state

After executing a command, we need to update our model state to reflect what happened:

```pseudocode
Command {
    Name: "LogIn"
    Precondition(currentState): currentState.isLoggedIn
    Gen(currentState): generateRandomUsername()
    Execute(input): actualSystem.login(input.username)
    
    UpdateState(oldState, input, output):
        oldState with { isLoggedIn: true, username: input.username }
}
```

This method takes the old state, the input we used, and the output we got, and produces the new state.

#### Step 6: Making assertions

Finally, we need to verify that the system behaved correctly. The `Ensure` method checks whether everything went as expected:

```pseudocode
Command {
    Name: "LogIn"
    Precondition(currentState): currentState.isLoggedIn
    Gen(currentState): generateRandomUsername()
    Execute(input): actualSystem.login(input.username)
    UpdateState(oldState, input, output): 
        oldState with { isLoggedIn: true, username: input.username }
    
    Ensure(oldState, newState, input, output):
        assert output.success == true
        assert output.username == input.username
}
```

With both states available, you can make rich assertions: "after logging in, the user should be logged in," "after adding an item, the cart count should increase by one," and so on.

## Putting it all together

Define all your commands (LogIn, LogOut, ChangePassword, etc.), and Hedgehog will:

1. Generate random sequences of commands
2. Check preconditions to ensure valid sequences
3. Execute each command against your real system
4. Update the model state after each command
5. Assert that everything worked correctly
6. If something fails, shrink the sequence to find the minimal reproduction case

You get comprehensive testing of complex stateful scenarios with minimal effort, and when bugs appear, you get a short, clear sequence showing exactly how to reproduce them.