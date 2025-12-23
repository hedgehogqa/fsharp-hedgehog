namespace Hedgehog.Stateful

open System.Threading.Tasks
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Stateful.FSharp

/// <summary>
/// Base class for commands that perform stateful operations and return values.
/// Commands are the building blocks of state machine tests - they represent operations
/// on a system under test (SUT) that can be generated, executed, and verified.
/// The SUT is passed as a typed parameter to Execute and Ensure.
/// Execute returns a Task to support both synchronous and asynchronous operations.
/// </summary>
/// <typeparam name="TSystem">The type of the system under test.</typeparam>
/// <typeparam name="TState">The type of the model state.</typeparam>
/// <typeparam name="TInput">The type of input required to execute the command.</typeparam>
/// <typeparam name="TOutput">The type of output produced by the command.</typeparam>
[<AbstractClass>]
type Command<'TSystem, 'TState, 'TInput, 'TOutput>() =

    /// Human-readable name of the command (e.g., "Push", "Pop")
    abstract member Name : string

    /// <summary>
    /// Structural precondition that determines if this command can be generated/executed in the given state.
    /// Return true if the command can run, false to skip generation.
    /// This is automatically used by both generation and Require (unless Require is overridden).
    /// </summary>
    /// <param name="state">The current model state.</param>
    abstract member Precondition : state:'TState -> bool

    /// <summary>
    /// Generate random input for this command given the current model state.
    /// This is only called when Precondition returns true.
    /// </summary>
    /// <param name="state">The current model state.</param>
    abstract member Generate: state:'TState -> Gen<'TInput>

    /// <summary>
    /// Check if the command can be executed in the given state with concrete values.
    /// Return false if the command should not be executed in this state.
    /// The env parameter allows resolving symbolic variables from the state.
    /// By default, returns true. Override only when you need to check concrete values with env.
    /// Note: Precondition is always checked separately, so you don't need to duplicate those checks here.
    /// </summary>
    /// <param name="env">The environment containing resolved values from previous commands.</param>
    /// <param name="state">The current model state.</param>
    /// <param name="input">The concrete input value for this command.</param>
    abstract member Require : env:Env * state:'TState * input:'TInput -> bool
    default this.Require(_, _, _) = true

    /// <summary>
    /// Execute the command against the real system, returning a Task.
    /// For synchronous operations, use Task.FromResult(value).
    /// The sut parameter is the typed System Under Test.
    /// The state parameter is the current model state.
    /// The env contains resolved values from previous commands.
    /// </summary>
    /// <param name="sut">The system under test.</param>
    /// <param name="env">The environment containing resolved values from previous commands.</param>
    /// <param name="state">The current model state.</param>
    /// <param name="input">The input value for this command.</param>
    abstract member Execute: sut:'TSystem * env:Env * state:'TState * input:'TInput -> Task<'TOutput>

    /// <summary>
    /// Update the model state after the command executes.
    /// The Var represents the symbolic output that can be referenced by future commands.
    /// </summary>
    /// <param name="state">The current model state.</param>
    /// <param name="input">The input value for this command.</param>
    /// <param name="output">The symbolic output variable that can be referenced by future commands.</param>
    abstract member Update : state:'TState * input:'TInput * output:Var<'TOutput> -> 'TState
    default this.Update(state, _, _) = state

    /// <summary>
    /// Verify the command's postcondition after execution.
    /// Compare the model state transitions (s0 -> s1) with the actual output.
    /// Should only verify based on states, input, and output - not inspect the SUT directly.
    /// Returns true if postcondition is satisfied, false otherwise.
    /// May throw exceptions which will be caught and treated as failures.
    /// </summary>
    /// <param name="env">The environment containing resolved values from previous commands.</param>
    /// <param name="oldState">The model state before the command executed.</param>
    /// <param name="newState">The model state after the command executed.</param>
    /// <param name="input">The input value for this command.</param>
    /// <param name="output">The actual output value produced by the command.</param>
    abstract member Ensure : env:Env * oldState:'TState * newState:'TState * input:'TInput * output:'TOutput -> bool
    default this.Ensure(_, _, _, _, _) = true

    interface ICommand<'TSystem, 'TState> with
        member this.ToActionGen() = {
            TryGen = fun category state env ->
                if this.Precondition(state) then
                    Some (this.Generate(state) |> Gen.map (fun input ->
                        let actionId, env' = Env.freshName env
                        let action : Action<'TSystem, 'TState> = {
                            Id = actionId
                            Name = $"%s{this.Name} %A{input}"
                            Input = box input
                            Category = category
                            Precondition = this.Precondition
                            Require = fun e s -> this.Require(e, s, input)
                            Execute = fun sut environment state ->
                                task {
                                    try
                                        let! result = this.Execute(sut, environment, state, input)
                                        return ActionResult.Success (box result)
                                    with ex ->
                                        return ActionResult.Failure ex
                                }
                            Update = fun s v ->
                                // Convert Var<obj> to Var<'TOutput> - they have different runtime types
                                this.Update(s, input, Var.convertFrom v)
                            Ensure = fun environment s0 s1 o ->
                                this.Ensure(environment, s0, s1, input, unbox<'TOutput> o)
                        }
                        action, env'
                    ))
                else
                    None
        }


/// <summary>
/// Base class for commands that perform side-effects without returning meaningful values.
/// Examples: closing a connection, clearing a cache, deleting a file.
/// Simpler than Command because no output variable is needed.
/// Execute returns a Task to support both synchronous and asynchronous operations.
/// The SUT is passed as a typed parameter to Execute and Ensure.
/// </summary>
/// <typeparam name="TSystem">The type of the system under test.</typeparam>
/// <typeparam name="TState">The type of the model state.</typeparam>
/// <typeparam name="TInput">The type of input required to execute the action.</typeparam>
[<AbstractClass>]
type ActionCommand<'TSystem, 'TState, 'TInput>() =

    /// Human-readable name of the action (e.g., "Close", "Clear", "Delete")
    abstract member Name : string

    /// <summary>
    /// Structural precondition that determines if this action can be generated/executed in the given state.
    /// Return true if the action can run, false to skip generation.
    /// This is automatically used by both generation and Require (unless Require is overridden).
    /// </summary>
    /// <param name="state">The current model state.</param>
    abstract member Precondition : state:'TState -> bool

    /// <summary>
    /// Generate random input for this action given the current model state.
    /// This is only called when Precondition returns true.
    /// </summary>
    /// <param name="state">The current model state.</param>
    abstract member Generate: state:'TState -> Gen<'TInput>

    /// <summary>
    /// Check if the action can be executed in the given state with concrete values.
    /// Return false if the action should not be executed in this state.
    /// The env parameter allows resolving symbolic variables from the state.
    /// By default, returns true. Override only when you need to check concrete values with env.
    /// Note: Precondition is always checked separately, so you don't need to duplicate those checks here.
    /// </summary>
    /// <param name="env">The environment containing resolved values from previous commands.</param>
    /// <param name="state">The current model state.</param>
    /// <param name="input">The concrete input value for this action.</param>
    abstract member Require : env:Env * state:'TState * input:'TInput -> bool
    default this.Require(_, _, _) = true

    /// <summary>
    /// Execute the action against the real system without returning a value.
    /// For synchronous operations, use Task.CompletedTask.
    /// The sut parameter is the typed System Under Test.
    /// The state parameter is the current model state.
    /// The env contains resolved values from previous commands.
    /// </summary>
    /// <param name="sut">The system under test.</param>
    /// <param name="env">The environment containing resolved values from previous commands.</param>
    /// <param name="state">The current model state.</param>
    /// <param name="input">The input value for this action.</param>
    abstract member Execute: sut:'TSystem * env:Env * state:'TState * input:'TInput -> Task

    /// <summary>
    /// Update the model state after the action executes.
    /// No output variable since actions don't produce values to reference later.
    /// </summary>
    /// <param name="state">The current model state.</param>
    /// <param name="input">The input value for this action.</param>
    abstract member Update : state:'TState * input:'TInput -> 'TState
    default this.Update(state, _) = state

    /// <summary>
    /// Verify the action's postcondition after execution.
    /// Compare the model state transitions (s0 -> s1) with the input.
    /// Should only verify based on states and input - not inspect the SUT directly.
    /// No output parameter since actions don't produce values.
    /// Returns true if postcondition is satisfied, false otherwise.
    /// May throw exceptions which will be caught and treated as failures.
    /// </summary>
    /// <param name="env">The environment containing resolved values from previous commands.</param>
    /// <param name="oldState">The model state before the action executed.</param>
    /// <param name="newState">The model state after the action executed.</param>
    /// <param name="input">The input value for this action.</param>
    abstract member Ensure : env:Env * oldState:'TState * newState:'TState * input:'TInput -> bool
    default this.Ensure(_, _, _, _) = true

    interface ICommand<'TSystem, 'TState> with
        member this.ToActionGen() = {
            TryGen = fun category state env ->
                if this.Precondition(state) then
                    Some (this.Generate(state) |> Gen.map (fun input ->
                        let actionId, env' = Env.freshName env
                        let action : Action<'TSystem, 'TState> = {
                            Id = actionId
                            Name = $"%s{this.Name} %A{input}"
                            Input = box input
                            Category = category
                            Precondition = this.Precondition
                            Require = fun e s -> this.Require(e, s, input)
                            Execute = fun sut environment state ->
                                task {
                                    try
                                        do! this.Execute(sut, environment, state, input)
                                        return ActionResult.Success (box ())
                                    with ex ->
                                        return ActionResult.Failure ex
                                }
                            Update = fun s _ -> this.Update(s, input)  // Ignore Var parameter
                            Ensure = fun environment s0 s1 _ -> this.Ensure(environment, s0, s1, input)  // Ignore output parameter
                        }
                        action, env'  // Return updated env with fresh name
                    ))
                else
                    None
        }
