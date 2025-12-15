namespace Hedgehog.Stateful

open Hedgehog

/// Interface for types that helps with converting commands to executable actions.
/// This allows seamless mixing of different commands and action commands.
[<Interface>]
type ICommand<'TSystem, 'TState> =
    abstract member ToActionGen: unit -> ActionGen<'TSystem, 'TState>

/// <b>Used internally.</b>
/// Existential wrapper to allow heterogeneous command lists.
/// This erases the input/output types so different commands can be in the same list.
and ActionGen<'TSystem, 'TState> =
    internal
        {
            /// Attempt to generate an action from this command
            TryGen: ActionCategory -> 'TState -> Env -> Gen<Action<'TSystem, 'TState> * Env> option
        }

    interface ICommand<'TSystem, 'TState> with
        member this.ToActionGen() = this
