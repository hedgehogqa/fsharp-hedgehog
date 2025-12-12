namespace Hedgehog.Stateful.Linq

/// Dummy type to provide extension methods for C# fluent API.
/// Use StateMachine.Sequential() or StateMachine.Parallel() to create state machine tests.
type StateMachine private () =
    static member val Instance = StateMachine()
