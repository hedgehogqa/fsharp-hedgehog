namespace Hedgehog.Stateful

open System.Threading.Tasks

/// Result of executing a command
type internal ActionResult<'TOutput> =
    | Success of 'TOutput
    | Failure of exn

/// Category of action in a test sequence
type internal ActionCategory =
    | Setup     // Setup actions that run before the main test sequence
    | Test      // Main test actions
    | Cleanup   // Cleanup actions that run after the main test sequence

/// An action is a command instance with concrete input.
/// Actions are generated with symbolic inputs and executed against the SUT.
/// The SUT is passed as a typed parameter, separate from Env.
[<StructuredFormatDisplay("{DisplayText}")>]
type internal Action<'TSystem, 'TState> = private {
    /// Unique identifier for this action instance (generated from Env.freshName)
    Id: Name
    /// Human-readable description
    Name: string
    /// The input to the command (boxed)
    Input: obj
    /// Category of this action (Setup, Test, or Cleanup)
    Category: ActionCategory
    /// Execute the action against the real system (SUT passed as typed parameter)
    /// Returns a Task that completes with ExecutionResult - sync commands return completed tasks
    Execute: 'TSystem -> Env -> 'TState -> Task<ActionResult<obj>>
    /// Structural precondition check (on model state only, no env)
    Precondition: 'TState -> bool
    /// Check precondition with concrete values (env allows resolving symbolic variables in state)
    Require: Env -> 'TState -> bool
    /// Update the model state
    Update: 'TState -> Var<obj> -> 'TState
    /// Verify postcondition (SUT not passed - verify based on state transitions only)
    /// Returns true if postcondition is satisfied, false otherwise.
    /// May throw exceptions which will be caught and treated as failures.
    Ensure: Env -> 'TState -> 'TState -> obj -> bool
}
with
    member private this.DisplayText = this.Name

/// A sequence of actions to execute
type internal Actions<'TSystem, 'TState> = {
    Initial: 'TState
    /// Setup actions (executed first, in order, stops on first failure)
    Setup: Action<'TSystem, 'TState> list
    /// Test actions (executed after setup, in order, stops on first failure)
    Test: Action<'TSystem, 'TState> list
    /// Cleanup actions (always executed, even if Setup/Test fail; all are attempted)
    Cleanup: Action<'TSystem, 'TState> list
}
