namespace Hedgehog.Xunit

open System

/// Runs Property.reportRecheck
[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Property, AllowMultiple = false)>]
type RecheckAttribute(recheckData) =
  inherit Attribute()

  let _recheckData : string = recheckData

  member internal _.GetRecheckData = _recheckData
