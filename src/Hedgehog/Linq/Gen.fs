namespace Hedgehog.Linq

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog

type Gen private () =

    /// <summary>
    /// Create a generator that always yields a constant value.
    /// </summary>
    /// <param name="value">The constant value the generator always returns.</param>
    [<Obsolete("Use Gen.Constant instead.")>]
    static member FromValue (value : 'T) : Gen<'T> =
        Gen.constant value

    /// <summary>
    /// Create a generator that always yields a constant value.
    /// </summary>
    /// <param name="value">The constant value the generator always returns.</param>
    static member Constant (value : 'T) : Gen<'T> =

        Gen.constant value
    static member FromRandom (random : Random<Tree<'T>>) : Gen<'T> =
        Gen.ofRandom random

    static member Delay (func : Func<Gen<'T>>) : Gen<'T> =
        Gen.delay func.Invoke

    static member Create (shrink : Func<'T, seq<'T>>, random : Random<'T>) : Gen<'T> =
        Gen.create shrink.Invoke random

    /// Used to construct generators that depend on the size parameter.
    static member Sized (scaler : Func<Size, Gen<'T>>) : Gen<'T> =
        Gen.sized scaler.Invoke

    /// Generates a random number in the given inclusive range.
    static member Integral (range : Range<byte>) : Gen<byte> =
        Gen.integral range

    /// Generates a random number in the given inclusive range.
    static member Integral (range : Range<sbyte>) : Gen<sbyte> =
        Gen.integral range

     /// Generates a random number in the given inclusive range.
    static member Integral (range : Range<int16>) : Gen<int16> =
        Gen.integral range

    /// Generates a random number in the given inclusive range.
    static member Integral (range : Range<uint16>) : Gen<uint16> =
        Gen.integral range

    /// Generates a random number in the given inclusive range.
    static member Integral (range : Range<int32>) : Gen<int32> =
        Gen.integral range

    /// Generates a random number in the given inclusive range.
    static member Integral (range : Range<uint32>) : Gen<uint32> =
        Gen.integral range

    /// Generates a random number in the given inclusive range.
    static member Integral (range : Range<int64>) : Gen<int64> =
        Gen.integral range

    /// Generates a random number in the given inclusive range.
    static member Integral (range : Range<uint64>) : Gen<uint64> =
        Gen.integral range

    /// Generates a random number in the given inclusive range.
    static member Integral (range : Range<double>) : Gen<double> =
        Gen.integral range

    /// Generates a random number in the given inclusive range.
    static member Integral (range : Range<decimal>) : Gen<decimal> =
        Gen.integral range

    /// <summary>
    /// Randomly selects one of the values in the list.
    /// <i>The input list must be non-empty.</i>
    /// </summary>
    /// <param name="items">A non-empty IEnumerable of the Gen's possible values</param>
    static member Item ([<ParamArray>] items : array<'T>) : Gen<'T> =
        Gen.item items

    /// Uses a weighted distribution to randomly select one of the gens in the list.
    /// This generator shrinks towards the first generator in the list.
    /// <i>The input list must be non-empty.</i>
    static member Frequency ([<ParamArray>] gens : array<int * Gen<'T>>) : Gen<'T> =
        Gen.frequency gens

    /// Uses a weighted distribution to randomly select one of the gens in the list.
    /// This generator shrinks towards the first generator in the list.
    /// <i>The input list must be non-empty.</i>
    static member Frequency ([<ParamArray>] values : array<struct (int * 'T)>) : Gen<'T> =
        values
        |> Seq.map (fun struct (weight, value) -> (weight, Gen.constant value))
        |> Gen.frequency


    /// Randomly selects one of the gens in the list.
    /// <i>The input list must be non-empty.</i>
    static member Choice ([<ParamArray>] gens : array<Gen<'T>>) : Gen<'T> =
        Gen.choice gens

    /// Randomly selects from one of the gens in either the non-recursive or the
    /// recursive list. When a selection is made from the recursive list, the size
    /// is halved. When the size gets to one or less, selections are no longer made
    /// from the recursive list.
    /// <i>The first argument (i.e. the non-recursive input list) must be non-empty.</i>
    static member ChoiceRecursive (nonrecs : seq<Gen<'T>>, recs : seq<Gen<'T>>) : Gen<'T> =
        Gen.choiceRec nonrecs recs

    /// Generates a random character in the given range.
    static member Char (lo : char, hi : char) : Gen<char> =
        Gen.char lo hi

    /// Generates a Unicode character, including invalid standalone surrogates,
    /// i.e. from '\000' to '\65535'.
    static member UnicodeAll : Gen<char> =
        Gen.unicodeAll

    /// <summary>
    /// Generates a random numerical character, i.e. from '0' to '9'.
    /// </summary>
    /// <example>
    /// Combine with <see cref="T:Hedgehog.Linq.GenExtensions.String"/> to create strings of a desired length.
    /// <code>
    /// Gen.Digit.String(Range.Constant(5, 10))
    /// </code>
    /// </example>
    static member Digit : Gen<char> =
        Gen.digit

    /// <summary>
    /// Generates a random lowercase character, i.e. from 'a' to 'z'.
    /// </summary>
    /// <example>
    /// Combine with <see cref="T:Hedgehog.Linq.GenExtensions.String"/> to create strings of a desired length.
    /// <code>
    /// Gen.Lower.String(Range.Constant(5, 10))
    /// </code>
    /// </example>
    static member Lower : Gen<char> =
        Gen.lower

    /// <summary>
    /// Generates a random uppercase character, i.e. from 'A' to 'Z'.
    /// </summary>
    /// <example>
    /// Combine with <see cref="T:Hedgehog.Linq.GenExtensions.String"/> to create strings of a desired length.
    /// <code>
    /// Gen.Upper.String(Range.Constant(5, 10))
    /// </code>
    /// </example>
    static member Upper : Gen<char> =
        Gen.upper

    /// <summary>
    /// Generates a random ASCII character, i.e. from '\000' to '\127', i.e. any 7 bit character.
    /// </summary>
    /// <remarks>
    /// Non-printable and control characters can be generated, e.g. NULL and BEL.
    /// </remarks>
    /// <example>
    /// Combine with <see cref="T:Hedgehog.Linq.GenExtensions.String"/> to create strings of a desired length.
    /// <code>
    /// Gen.Ascii.String(Range.Constant(5, 10))
    /// </code>
    /// </example>
    static member Ascii : Gen<char> =
        Gen.ascii

    /// <summary>
    /// Generates a random Latin-1 character, i.e. from '\000' to '\255', i.e. any 8 bit character.
    /// </summary>
    /// <remarks>
    /// Non-printable and control characters can be generated, e.g. NULL and BEL.
    /// </remarks>
    /// <example>
    /// Combine with <see cref="T:Hedgehog.Linq.GenExtensions.String"/> to create strings of a desired length.
    /// <code>
    /// Gen.Latin1.String(Range.Constant(5, 10))
    /// </code>
    /// </example>
    static member Latin1 : Gen<char> =
        Gen.latin1

    /// <summary>
    /// Generates a Unicode character, excluding non-characters ('\65534' and '\65535') and invalid standalone surrogates (from '\55296' to '\57343').
    /// </summary>
    /// <example>
    /// Combine with <see cref="T:Hedgehog.Linq.GenExtensions.String"/> to create strings of a desired length.
    /// <code>
    /// Gen.Unicode.String(Range.Constant(5, 10))
    /// </code>
    /// </example>
    static member Unicode : Gen<char> =
        Gen.unicode

    /// <summary>
    /// Generates an alphabetic character, i.e. 'a' to 'z' or 'A' to 'Z'.
    /// </summary>
    /// <example>
    /// Combine with <see cref="T:Hedgehog.Linq.GenExtensions.String"/> to create strings of a desired length.
    /// <code>
    /// Gen.Alpha.String(Range.Constant(5, 10))
    /// </code>
    /// This generates strings such as <c>Ldklk</c> or <c>aFDG</c>
    /// </example>
    static member Alpha : Gen<char> =
        Gen.alpha

    /// <summary>
    /// Generates an alphanumeric character, i.e. 'a' to 'z', 'A' to 'Z', or '0' to '9'.
    /// </summary>
    /// <example>
    /// Combine with <see cref="T:Hedgehog.Linq.GenExtensions.String"/> to create strings of a desired length.
    /// <code>
    /// Gen.AlphaNumeric.String(Range.Constant(5, 10))
    /// </code>
    /// This generates strings such as <c>Ld5lk</c> or <c>4dFDG</c>
    /// </example>
    static member AlphaNumeric : Gen<char> =
        Gen.alphaNum

    /// Generates a random boolean.
    static member Bool : Gen<bool> =
        Gen.bool

    /// Generates a random signed byte.
    static member SByte (range : Range<sbyte>) : Gen<sbyte> =
        Gen.sbyte range

     /// Generates a random byte.
    static member Byte (range : Range<byte>) : Gen<byte> =
        Gen.byte range

    /// Generates a random signed 16-bit integer.
    static member Int16 (range : Range<int16>) : Gen<int16> =
        Gen.int16 range

    /// Generates a random unsigned 16-bit integer.
    static member UInt16 (range : Range<uint16>) : Gen<uint16> =
        Gen.uint16 range

    /// Generates a random signed 32-bit integer.
    static member Int32 (range : Range<int32>) : Gen<int32> =
        Gen.int32 range

    /// Generates a random unsigned 32-bit integer.
    static member UInt32 (range : Range<uint32>) : Gen<uint32> =
        Gen.uint32 range

    /// Generates a random signed 64-bit integer.
    static member Int64 (range : Range<int64>) : Gen<int64> =
        Gen.int64 range

    /// Generates a random unsigned 64-bit integer.
    static member UInt64 (range : Range<uint64>) : Gen<uint64> =
        Gen.uint64 range

    /// Generates a random 32-bit floating point number.
    static member Single (range : Range<single>) : Gen<single> =
        Gen.single range

    /// Generates a random 64-bit floating point number.
    static member Double (range : Range<double>) : Gen<double> =
        Gen.double range

    /// Generates a random decimal floating-point number.
    static member Decimal (range : Range<decimal>) : Gen<decimal> =
        Gen.decimal range

    /// Generates a random globally unique identifier.
    static member Guid : Gen<Guid> =
        Gen.guid

    /// <summary>
    /// Generates a random DateTime using the given range.
    /// </summary>
    /// <example>
    /// <code>
    /// var TwentiethCentury = Gen.DateTime(
    ///     Range.Constant(
    ///         new DateTime(1900,  1,  1),
    ///         new DateTime(1999, 12, 31)));
    /// </code>
    /// </example>
    /// <param name="range">Range determining the bounds of the <c>DateTime</c> that can be generated.</param>
    static member DateTime (range : Range<DateTime>) : Gen<DateTime> =
        Gen.dateTime range

    /// Generates a random DateTimeOffset using the given range.
    static member DateTimeOffset (range : Range<DateTimeOffset>) : Gen<DateTimeOffset> =
        Gen.dateTimeOffset range

[<AbstractClass; Sealed>]
type GenExtensions private () =

    [<Extension>]
    static member Apply (genFunc : Gen<Func<'T, 'TResult>>, genArg : Gen<'T>) : Gen<'TResult> =
        Gen.apply genArg (genFunc |> Gen.map (fun f -> f.Invoke))

    /// <summary>
    /// Generates an array using a 'Range' to determine the length.
    /// </summary>
    /// <param name="gen">Item generator.</param>
    /// <param name="range">Range determining the length of the array.</param>
    [<Extension>]
    static member Array (gen : Gen<'T>, range : Range<int>) : Gen<'T []> =
        Gen.array range gen

    /// <summary>
    /// Generates an enumerable using a 'Range' to determine the length.
    /// </summary>
    /// <param name="gen">Item generator.</param>
    /// <param name="range">Range determining the length of the enumerable.</param>
    [<Extension>]
    static member Enumerable (gen : Gen<'T>, range : Range<int>) : Gen<seq<'T>> =
        Gen.seq range gen

    /// Run a generator. The size passed to the generator is always 30;
    /// if you want another size then you should explicitly use 'resize'.
    [<Extension>]
    static member GenerateTree (gen : Gen<'T>) : Tree<'T> =
        Gen.generateTree gen

    /// <summary>
    /// Generates a List using a 'Range' to determine the length and a 'Gen' to produce the elements.
    /// </summary>
    /// <param name="gen">Generates the items in the List.</param>
    /// <param name="range">Range determining the length of the List.</param>
    [<Extension>]
    static member List (gen : Gen<'T>, range : Range<int>) : Gen<ResizeArray<'T>> =
        Gen.list range gen
        |> Gen.map ResizeArray

    /// Prevent a 'Gen' from shrinking.
    [<Extension>]
    static member NoShrink (gen : Gen<'T>) : Gen<'T> =
        Gen.noShrink gen

    /// Generates a <c>null</c> or a value from gen. Null becomes less common with larger Sizes.
    [<Extension>]
    static member NullReference (gen : Gen<'T>) : Gen<'T> =
        Gen.option gen |> Gen.map (Option.defaultValue null)

    /// Generates a <c>null</c> or a value from gen. Null becomes less common with larger Sizes.
    [<Extension>]
    static member NullValue (gen : Gen<'T>) : Gen<Nullable<'T>> =
        Gen.option gen |> Gen.map (Option.defaultWith Nullable << Option.map Nullable)

    /// Samples the gen 5 times with a Size of 10, called the "Outcome" in the returned string.
    /// Then the shrink path to each Outcome is produced. This may be useful in debugging
    /// shrink paths in complex Gens.
    [<Extension>]
    static member RenderSample (gen : Gen<'T>) : string =
        Gen.renderSample gen

    /// Overrides the size parameter. Returns a generator which uses the
    /// given size instead of the runtime-size parameter.
    [<Extension>]
    static member Resize (gen : Gen<'T>, size : Size) : Gen<'T> =
        Gen.resize size gen

    /// <summary>Returns a List of values produced by the generator.</summary>
    /// <param name="gen">Value generator.</param>
    /// <param name="size">The size parameter for the generator.</param>
    /// <param name="count">The number of samples to produce, i.e. the length of the List.</param>
    [<Extension>]
    static member Sample (gen : Gen<'T>, size : Size, count : int) : ResizeArray<'T> =
        Gen.sample size count gen
        |> ResizeArray

    [<Extension>]
    static member SampleTree (gen : Gen<'T>, size : Size, count : int) : ResizeArray<Tree<'T>> =
        Gen.sampleTree size count gen
        |> ResizeArray

    /// Adjust the size parameter, by transforming it with the given
    /// function.
    [<Extension>]
    static member Scale (gen : Gen<'T>, scaler : Func<int, int>) : Gen<'T> =
        Gen.scale scaler.Invoke gen

    [<Extension>]
    static member SelectMany (gen : Gen<'T>, binder : Func<'T, Gen<'U>>) : Gen<'U> =
        Gen.bind binder.Invoke gen

    [<Extension>]
    static member SelectMany (gen : Gen<'T>, binder : Func<'T, Gen<'TCollection>>, projection : Func<'T, 'TCollection, 'TResult>) : Gen<'TResult> =
        GenBuilder.gen {
            let! a = gen
            let! b = binder.Invoke a
            return projection.Invoke (a, b)
        }

    [<Extension>]
    static member SelectRandom (gen : Gen<'T>, binder : Func<Random<Tree<'T>>, Random<Tree<'TResult>>>) : Gen<'TResult> =
        Gen.mapRandom binder.Invoke gen

    [<Extension>]
    static member SelectTree (gen : Gen<'T>, binder : Func<Tree<'T>, Tree<'TResult>>) : Gen<'TResult> =
        Gen.mapTree binder.Invoke gen
    
    /// <summary>
    /// Projects each value of a generator into a new form. Similar to <c>Enumerable.Select</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// Gen&lt;Point&gt; pointGen = Gen.Int32(Range.Constant(0,200))
    ///     .Tuple2()
    ///     .Select(tuple => new Point(tuple.Item1, tuple.Item2));
    /// </code>
    /// </example>
    [<Extension>]
    static member Select (gen : Gen<'T>, mapper : Func<'T, 'TResult>) : Gen<'TResult> =
        Gen.map mapper.Invoke gen

    /// <summary>
    /// Projects each value of a generator into a new form. Similar to <c>Enumerable.Select</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// Gen&lt;Point&gt; pointGen = Gen.Int32(Range.Constant(0,200))
    ///     .Tuple2()
    ///     .Select(tuple => new Point(tuple.Item1, tuple.Item2));
    /// </code>
    /// </example>
    [<Extension>]
    static member Select (genA : Gen<'T>, mapper : Func<'T, 'U, 'TResult>, genB : Gen<'U>) : Gen<'TResult> =
        Gen.map2 (fun a b -> mapper.Invoke (a, b))
            genA
            genB

    /// <summary>
    /// Projects each value of a generator into a new form. Similar to <c>Enumerable.Select</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// Gen&lt;Point&gt; pointGen = Gen.Int32(Range.Constant(0,200))
    ///     .Tuple2()
    ///     .Select(tuple => new Point(tuple.Item1, tuple.Item2));
    /// </code>
    /// </example>
    [<Extension>]
    static member Select (genA : Gen<'T>, mapper : Func<'T, 'U, 'V, 'TResult>, genB : Gen<'U>, genC : Gen<'V>) : Gen<'TResult> =
        Gen.map3 (fun a b c -> mapper.Invoke (a, b, c))
            genA
            genB
            genC

    /// <summary>
    /// Projects each value of a generator into a new form. Similar to <c>Enumerable.Select</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// Gen&lt;Point&gt; pointGen = Gen.Int32(Range.Constant(0,200))
    ///     .Tuple2()
    ///     .Select(tuple => new Point(tuple.Item1, tuple.Item2));
    /// </code>
    /// </example>
    [<Extension>]
    static member Select (genA : Gen<'T>, mapper : Func<'T, 'U, 'V, 'W, 'TResult>, genB : Gen<'U>, genC : Gen<'V>, genD : Gen<'W>) : Gen<'TResult> =
        Gen.map4 (fun a b c d -> mapper.Invoke (a, b, c, d))
            genA
            genB
            genC
            genD

    /// Apply an additional shrinker to all generated trees.
    [<Extension>]
    static member Shrink (gen : Gen<'T>, shrinker : Func<'T, ResizeArray<'T>>) : Gen<'T> =
        Gen.shrink (shrinker.Invoke >> Seq.toList) gen

    /// Apply an additional shrinker to all generated trees.
    [<Extension>]
    static member ShrinkLazy (gen : Gen<'T>, shrinker : Func<'T, seq<'T>>) : Gen<'T> =
        Gen.shrinkLazy shrinker.Invoke gen

    /// Runs an option generator until it produces a 'Some'.
    [<Extension>]
    static member Some (gen : Gen<Option<'T>>) : Gen<'T> =
        Gen.some gen

    /// Generates a random string using 'Range' to determine the length and the
    /// given character generator.
    [<Extension>]
    static member String (gen : Gen<char>, range : Range<int>) : Gen<string> =
        Gen.string range gen

    [<Extension>]
    static member ToGen (random : Random<Tree<'T>>) : Gen<'T> =
        Gen.ofRandom random

    [<Extension>]
    static member ToRandom (gen : Gen<'T>) : Random<Tree<'T>> =
        Gen.toRandom gen

    [<Extension>]
    static member TryFinally (gen : Gen<'T>, after : Action) : Gen<'T> =
        Gen.tryFinally after.Invoke gen

    [<Extension>]
    static member TryWith (gen : Gen<'T>, after : Func<exn, Gen<'T>>) : Gen<'T> =
        Gen.tryWith after.Invoke gen

    [<Extension>]
    static member Tuple2 (gen : Gen<'T>) : Gen<'T * 'T> =
        Gen.tuple gen

    [<Extension>]
    static member Tuple3 (gen : Gen<'T>) : Gen<'T * 'T * 'T> =
        Gen.tuple3 gen

    [<Extension>]
    static member Tuple4 (gen : Gen<'T>) : Gen<'T * 'T * 'T * 'T> =
        Gen.tuple4 gen

    /// Generates a value that satisfies a predicate.
    [<Extension>]
    static member Where (gen : Gen<'T>, predicate : Func<'T, bool>) : Gen<'T> =
        Gen.filter predicate.Invoke gen

    [<Extension>]
    static member Zip (genA : Gen<'T>, genB : Gen<'U>) : Gen<'T * 'U> =
        Gen.zip genA genB

    [<Extension>]
    static member Zip (genA : Gen<'T>, genB : Gen<'U>, genC : Gen<'V>) : Gen<'T * 'U * 'V> =
        Gen.zip3 genA genB genC

    [<Extension>]
    static member Zip (genA : Gen<'T>, genB : Gen<'U>, genC : Gen<'V>, genD : Gen<'W>) : Gen<'T * 'U * 'V * 'W> =
        Gen.zip4 genA genB genC genD

#endif
