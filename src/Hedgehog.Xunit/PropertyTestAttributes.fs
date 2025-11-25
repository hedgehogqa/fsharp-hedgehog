namespace Hedgehog.Xunit

open System
open Xunit
open Hedgehog
open Xunit.v3

/// Generates arguments using GenX.auto (or autoWith if you provide an AutoGenConfig), then runs Property.check
[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Property, AllowMultiple = false)>]
[<XunitTestCaseDiscoverer(typeof<PropertyTestCaseDiscoverer>)>]
type PropertyAttribute(autoGenConfig, autoGenConfigArgs, tests, shrinks, size) =
  inherit FactAttribute()

  let mutable _autoGenConfig: Type option = autoGenConfig
  let mutable _autoGenConfigArgs: obj[] = autoGenConfigArgs
  let mutable _tests: int<tests> option = tests
  let mutable _shrinks: int<shrinks> option = shrinks
  let mutable _size: Size option = size

  member _.AutoGenConfig     with set v = _autoGenConfig     <- Some v and get ():Type         = failwith "this getter only exists to make C# named arguments work"
  member _.AutoGenConfigArgs with set v = _autoGenConfigArgs <-      v and get ():obj array    = failwith "this getter only exists to make C# named arguments work"
  member _.Tests             with set v = _tests             <- Some v and get ():int<tests>   = failwith "this getter only exists to make C# named arguments work"
  member _.Shrinks           with set v = _shrinks           <- Some v and get ():int<shrinks> = failwith "this getter only exists to make C# named arguments work"
  member _.Size              with set v = _size              <- Some v and get ():Size         = failwith "this getter only exists to make C# named arguments work"

  new()                                   = PropertyAttribute(None              , [||], None      , None        , None)
  new(tests)                              = PropertyAttribute(None              , [||], Some tests, None        , None)
  new(tests, shrinks)                     = PropertyAttribute(None              , [||], Some tests, Some shrinks, None)
  new(autoGenConfig)                      = PropertyAttribute(Some autoGenConfig, [||], None      , None        , None)
  new(autoGenConfig:Type, tests)          = PropertyAttribute(Some autoGenConfig, [||], Some tests, None        , None)
  new(autoGenConfig:Type, tests, shrinks) = PropertyAttribute(Some autoGenConfig, [||], Some tests, Some shrinks, None)

  interface IPropertyAttribute with
    member _.AutoGenConfig with get () = _autoGenConfig and set v = _autoGenConfig <- v
    member _.AutoGenConfigArgs with get () = _autoGenConfigArgs and set v = _autoGenConfigArgs <- v
    member _.Tests with get () = _tests and set v = _tests <- v
    member _.Shrinks with get () = _shrinks and set v = _shrinks <- v
    member _.Size with get () = _size and set v = _size <- v


/// Set a default AutoGenConfig or <tests> for all [<Property>] attributed methods in this class/module
[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type PropertiesAttribute(autoGenConfig, autoGenConfigArgs, tests, shrinks, size) =
  inherit Attribute()

  let mutable _autoGenConfig: Type option = autoGenConfig
  let mutable _autoGenConfigArgs: obj[] = autoGenConfigArgs
  let mutable _tests: int<tests> option = tests
  let mutable _shrinks: int<shrinks> option = shrinks
  let mutable _size: Size option = size

  member _.AutoGenConfig     with set v = _autoGenConfig     <- Some v and get ():Type         = failwith "this getter only exists to make C# named arguments work"
  member _.AutoGenConfigArgs with set v = _autoGenConfigArgs <-      v and get ():obj array    = failwith "this getter only exists to make C# named arguments work"
  member _.Tests             with set v = _tests             <- Some v and get ():int<tests>   = failwith "this getter only exists to make C# named arguments work"
  member _.Shrinks           with set v = _shrinks           <- Some v and get ():int<shrinks> = failwith "this getter only exists to make C# named arguments work"
  member _.Size              with set v = _size              <- Some v and get ():Size         = failwith "this getter only exists to make C# named arguments work"

  new()                                   = PropertiesAttribute(None              , [||], None      , None        , None)
  new(tests)                              = PropertiesAttribute(None              , [||], Some tests, None        , None)
  new(tests, shrinks)                     = PropertiesAttribute(None              , [||], Some tests, Some shrinks, None)
  new(autoGenConfig)                      = PropertiesAttribute(Some autoGenConfig, [||], None      , None        , None)
  new(autoGenConfig:Type, tests)          = PropertiesAttribute(Some autoGenConfig, [||], Some tests, None        , None)
  new(autoGenConfig:Type, tests, shrinks) = PropertiesAttribute(Some autoGenConfig, [||], Some tests, Some shrinks, None)

  interface IPropertyAttribute with
    member _.AutoGenConfig with get () = _autoGenConfig and set v = _autoGenConfig <- v
    member _.AutoGenConfigArgs with get () = _autoGenConfigArgs and set v = _autoGenConfigArgs <- v
    member _.Tests with get () = _tests and set v = _tests <- v
    member _.Shrinks with get () = _shrinks and set v = _shrinks <- v
    member _.Size with get () = _size and set v = _size <- v
