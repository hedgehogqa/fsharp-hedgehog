namespace Hedgehog.AutoGen

open System
open System.Collections.Immutable
open Hedgehog

type internal RecursionState = {
    CurrentLevel: int
    CanRecurse: bool
    Depths: ImmutableDictionary<Type, int>
  }

module internal RecursionState =
  let empty = {
    CurrentLevel = 0
    CanRecurse = true
    Depths = ImmutableDictionary.Empty
  }

  let reconcileFor<'a> (config: AutoGenConfig) (current: RecursionState) =
    let currentLevel = current.Depths.GetValueOrDefault(typeof<'a>, 0)
    let maxDepth = AutoGenConfig.recursionDepth config
    if (currentLevel > maxDepth) then None
    else Some
          {
            CurrentLevel = currentLevel
            CanRecurse = currentLevel < maxDepth
            Depths = current.Depths.SetItem(typeof<'a>, currentLevel + 1)
          }
