namespace Hedgehog.Stateful.Linq

open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Stateful
open System.Runtime.CompilerServices

[<Sealed>]
type ParallelStateMachine<'TSystem, 'TState> private (
    initial: 'TState,
    setupCommands: ActionGen<'TSystem, 'TState> list,
    commands: ActionGen<'TSystem, 'TState> list,
    cleanupCommands: ActionGen<'TSystem, 'TState> list,
    prefixRangeMin: int,
    prefixRangeMax: int,
    branchRangeMin: int,
    branchRangeMax: int) =

    static member Create(initialState: 'TState) =
        ParallelStateMachine<'TSystem, 'TState>(initialState, [], [], [], 0, 10, 1, 50)

    member this.WithSetupCommand<'TInput, 'TOutput>(cmd: Command<'TSystem, 'TState, 'TInput, 'TOutput>) =
        let wrapped = (cmd :> ICommand<'TSystem, 'TState>).ToActionGen()
        ParallelStateMachine<'TSystem, 'TState>(initial, wrapped :: setupCommands, commands, cleanupCommands, prefixRangeMin, prefixRangeMax, branchRangeMin, branchRangeMax)

    member this.WithCommand<'TInput, 'TOutput>(cmd: Command<'TSystem, 'TState, 'TInput, 'TOutput>) =
        let wrapped = (cmd :> ICommand<'TSystem, 'TState>).ToActionGen()
        ParallelStateMachine<'TSystem, 'TState>(initial, setupCommands, wrapped :: commands, cleanupCommands, prefixRangeMin, prefixRangeMax, branchRangeMin, branchRangeMax)

    member this.WithCleanupCommand<'TInput, 'TOutput>(cmd: Command<'TSystem, 'TState, 'TInput, 'TOutput>) =
        let wrapped = (cmd :> ICommand<'TSystem, 'TState>).ToActionGen()
        ParallelStateMachine<'TSystem, 'TState>(initial, setupCommands, commands, wrapped :: cleanupCommands, prefixRangeMin, prefixRangeMax, branchRangeMin, branchRangeMax)

    member this.WithPrefixRange(min: int, max: int) =
        ParallelStateMachine<'TSystem, 'TState>(initial, setupCommands, commands, cleanupCommands, min, max, branchRangeMin, branchRangeMax)

    member this.WithBranchRange(min: int, max: int) =
        ParallelStateMachine<'TSystem, 'TState>(initial, setupCommands, commands, cleanupCommands, prefixRangeMin, prefixRangeMax, min, max)

    member this.ToProperty(sut: 'TSystem) : Property<unit> =
        let prefixRange = Range.linear prefixRangeMin prefixRangeMax
        let branchRange = Range.linear branchRangeMin branchRangeMax
        let setup = List.rev setupCommands
        let cmds = List.rev commands
        let cleanup = List.rev cleanupCommands
        let gen = Parallel.genActions prefixRange branchRange setup cmds cleanup initial
        property {
            let! actions = gen
            do! Parallel.executeWithSUT sut actions
        }

type ParallelExtensions =
    [<Extension>]
    static member Parallel<'TSystem, 'TState>(initialState: 'TState) =
        ParallelStateMachine<'TSystem, 'TState>.Create(initialState)
