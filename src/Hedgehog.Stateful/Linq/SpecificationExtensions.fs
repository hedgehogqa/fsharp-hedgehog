namespace Hedgehog.Stateful.Linq

open System
open System.Runtime.CompilerServices
open Hedgehog
open Hedgehog.Stateful


/// <summary>
/// Extension methods for working with specifications in C#.
/// </summary>
[<AbstractClass; Sealed>]
type SpecificationExtensions private () =

    /// <summary>
    /// Convert a sequential specification to a property using a SUT factory.
    /// The factory is called once per property test run to create a fresh SUT.
    /// This is the recommended approach to ensure test isolation.
    /// </summary>
    /// <param name="spec">The sequential specification.</param>
    /// <param name="createSut">A Func delegate that creates a new SUT instance for each test run.</param>
    /// <returns>A property representing the sequential specification test.</returns>
    [<Extension>]
    static member ToPropertyWith(spec: SequentialSpecification<'TSystem, 'TState>, createSut: Func<'TSystem>) : Property<unit> =
        spec.ToPropertyWith(fun () -> createSut.Invoke())

    /// <summary>
    /// Convert a parallel specification to a property using a SUT factory.
    /// The factory is called once per property test run to create a fresh SUT.
    /// This is the recommended approach to ensure test isolation.
    /// </summary>
    /// <param name="spec">The parallel specification.</param>
    /// <param name="createSut">A Func delegate that creates a new SUT instance for each test run.</param>
    /// <returns>A property representing the parallel specification test.</returns>
    [<Extension>]
    static member ToPropertyWith(spec: ParallelSpecification<'TSystem, 'TState>, createSut: Func<'TSystem>) : Property<unit> =
        spec.ToPropertyWith(fun () -> createSut.Invoke())
