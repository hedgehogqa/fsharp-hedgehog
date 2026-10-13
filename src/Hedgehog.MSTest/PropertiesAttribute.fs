namespace Hedgehog.MSTest

open System
open Hedgehog

/// <summary>
/// Sets the defaults of every <see cref="T:Hedgehog.MSTest.PropertyAttribute"/> method of a test class.
/// <para>
/// The settings come from the class that MSTest runs, so a property inherited from an abstract base class gets the
/// settings of each derived test class. Along the inheritance chain the settings of a derived class win over those of
/// its base classes, and the <see cref="T:Hedgehog.IAutoGenConfig"/> configurations are merged in the same order; a
/// setting of <see cref="T:Hedgehog.MSTest.PropertyAttribute"/> wins over all of them.
/// </para>
/// </summary>
[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type PropertiesAttribute private (autoGenConfig, autoGenConfigArgs, tests, shrinks, size) =
    inherit Attribute()

    let mutable _autoGenConfig: Type option = autoGenConfig
    let mutable _autoGenConfigArgs: obj[] = autoGenConfigArgs
    let mutable _tests: int<tests> option = tests
    let mutable _shrinks: int<shrinks> option = shrinks
    let mutable _size: Size option = size
    let mutable _seed: uint64 option = None

    /// <summary>
    /// A type with exactly one public static member that returns <see cref="T:Hedgehog.IAutoGenConfig"/>, optionally
    /// taking <see cref="P:Hedgehog.MSTest.PropertiesAttribute.AutoGenConfigArgs"/>. Only for setting: reading it throws.
    /// </summary>
    member _.AutoGenConfig     with set v = _autoGenConfig     <- Some v and get ():Type         = failwith "this getter only exists to make C# named arguments work"

    /// <summary>
    /// The arguments of the member of <see cref="P:Hedgehog.MSTest.PropertiesAttribute.AutoGenConfig"/>. Only for
    /// setting: reading it throws.
    /// </summary>
    member _.AutoGenConfigArgs with set v = _autoGenConfigArgs <-      v and get ():obj array    = failwith "this getter only exists to make C# named arguments work"

    /// <summary>
    /// The number of cases that must pass. Only for setting: reading it throws.
    /// </summary>
    member _.Tests             with set v = _tests             <- Some v and get ():int<tests>   = failwith "this getter only exists to make C# named arguments work"

    /// <summary>
    /// The largest number of shrink steps of a failure. Only for setting: reading it throws.
    /// </summary>
    member _.Shrinks           with set v = _shrinks           <- Some v and get ():int<shrinks> = failwith "this getter only exists to make C# named arguments work"

    /// <summary>
    /// The size of every generated case. Only for setting: reading it throws.
    /// </summary>
    member _.Size              with set v = _size              <- Some v and get ():Size         = failwith "this getter only exists to make C# named arguments work"

    /// <summary>
    /// The seed of every run, such as one seed per suite. Only for setting: reading it throws.
    /// </summary>
    member _.Seed              with set v = _seed              <- Some v and get ():uint64       = failwith "this getter only exists to make C# named arguments work"

    /// <summary>
    /// Sets no defaults; named arguments set them.
    /// </summary>
    new()                                   = PropertiesAttribute(None              , [||], None      , None        , None)

    /// <summary>
    /// Sets the number of cases that must pass.
    /// </summary>
    /// <param name="tests">The number of cases that must pass.</param>
    new(tests)                              = PropertiesAttribute(None              , [||], Some tests, None        , None)

    /// <summary>
    /// Sets the number of cases that must pass and the largest number of shrink steps.
    /// </summary>
    /// <param name="tests">The number of cases that must pass.</param>
    /// <param name="shrinks">The largest number of shrink steps.</param>
    new(tests, shrinks)                     = PropertiesAttribute(None              , [||], Some tests, Some shrinks, None)

    /// <summary>
    /// Sets the configuration of the generated arguments.
    /// </summary>
    /// <param name="autoGenConfig">
    /// A type with exactly one public static member that returns <see cref="T:Hedgehog.IAutoGenConfig"/>.
    /// </param>
    new(autoGenConfig)                      = PropertiesAttribute(Some autoGenConfig, [||], None      , None        , None)

    /// <summary>
    /// Sets the configuration of the generated arguments and the number of cases that must pass.
    /// </summary>
    /// <param name="autoGenConfig">
    /// A type with exactly one public static member that returns <see cref="T:Hedgehog.IAutoGenConfig"/>.
    /// </param>
    /// <param name="tests">The number of cases that must pass.</param>
    new(autoGenConfig:Type, tests)          = PropertiesAttribute(Some autoGenConfig, [||], Some tests, None        , None)

    /// <summary>
    /// Sets the configuration of the generated arguments, the number of cases that must pass and the largest number of
    /// shrink steps.
    /// </summary>
    /// <param name="autoGenConfig">
    /// A type with exactly one public static member that returns <see cref="T:Hedgehog.IAutoGenConfig"/>.
    /// </param>
    /// <param name="tests">The number of cases that must pass.</param>
    /// <param name="shrinks">The largest number of shrink steps.</param>
    new(autoGenConfig:Type, tests, shrinks) = PropertiesAttribute(Some autoGenConfig, [||], Some tests, Some shrinks, None)

    interface IPropertyAttribute with
        member _.AutoGenConfig with get () = _autoGenConfig and set v = _autoGenConfig <- v
        member _.AutoGenConfigArgs with get () = _autoGenConfigArgs and set v = _autoGenConfigArgs <- v
        member _.Tests with get () = _tests and set v = _tests <- v
        member _.Shrinks with get () = _shrinks and set v = _shrinks <- v
        member _.Size with get () = _size and set v = _size <- v
        member _.Seed with get () = _seed and set v = _seed <- v
