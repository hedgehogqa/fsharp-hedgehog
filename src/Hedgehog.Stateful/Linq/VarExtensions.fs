namespace Hedgehog.Stateful.Linq

open System
open System.Runtime.CompilerServices
open Hedgehog.Stateful
open Hedgehog.Stateful.FSharp


/// <summary>
/// Extension methods for working with <c>Var&lt;T&gt;</c> in C#.
/// </summary>
[<AbstractClass; Sealed>]
type VarExtensions private () =

    /// <summary>
    /// Projects the value of a variable using a selector function.
    /// This allows extracting fields from structured command outputs.
    /// </summary>
    /// <param name="var">The variable to project from.</param>
    /// <param name="selector">A function to apply to the variable's value.</param>
    /// <returns>A new variable with the projection applied.</returns>
    [<Extension>]
    static member Select(var: Var<'T>, selector: Func<'T, 'U>) : Var<'U> =
        Var.map selector.Invoke var
