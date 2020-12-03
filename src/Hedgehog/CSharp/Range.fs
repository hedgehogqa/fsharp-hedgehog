namespace Hedgehog.CSharp

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog

[<Extension>]
type RangeExtensions =
    
    [<Extension>]
    member _.Select(range, mapper : Func<'T, 'U>) =
        Range.map mapper.Invoke range

#endif
