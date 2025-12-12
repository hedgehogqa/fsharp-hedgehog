namespace Hedgehog.Stateful.Linq

open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Stateful
open System.Runtime.CompilerServices

[<Sealed>]
type SequentialStateMachine<'TSystem, 'TState> private (
    initial: 'TState,
    commands: ActionGen<'TSystem, 'TState> list,
    rangeMin: int,
    rangeMax: int) =
    
    static member Create(initialState: 'TState) =
        SequentialStateMachine<'TSystem, 'TState>(initialState, [], 1, 100)

    member this.WithCommand<'TInput, 'TOutput>(cmd: Command<'TSystem, 'TState, 'TInput, 'TOutput>) =
        let wrapped = (cmd :> ICommand<'TSystem, 'TState>).ToActionGen()
        SequentialStateMachine<'TSystem, 'TState>(initial, wrapped :: commands, rangeMin, rangeMax)

    member this.WithRange(min: int, max: int) =
        SequentialStateMachine<'TSystem, 'TState>(initial, commands, min, max)

    member this.ToProperty(sut: 'TSystem) : Property<unit> =
        let range = Range.linear rangeMin rangeMax
        let cmds = List.rev commands
        let gen = Sequential.genActions range [] cmds [] initial
        property {
            let! actions = gen
            do! Sequential.executeWithSUT sut actions
        }

type SequentialExtensions =
    [<Extension>]
    static member Sequential<'TSystem, 'TState>(initialState: 'TState) =
        SequentialStateMachine<'TSystem, 'TState>.Create(initialState)
