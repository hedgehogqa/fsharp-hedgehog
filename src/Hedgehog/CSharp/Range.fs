namespace Hedgehog.CSharp

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog

[<Extension>]
type RangeExtensions =

    [<Extension>]
    static member inline Select(range : Range<'T>, mapper : Func<'T, 'TResult>) : Range<'TResult> =
        Range.map mapper.Invoke range

#endif
