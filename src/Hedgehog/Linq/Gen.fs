namespace Hedgehog.Linq

open System
open System.Net
open Hedgehog
open Hedgehog.FSharp

type Gen private () =

    /// <summary>
    /// Create a generator that automatically generates values of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of values to generate.</typeparam>
    static member Auto<'T>() : Gen<'T> = Gen.auto<'T>

    /// <summary>
    /// Create a generator that automatically generates values of the specified type,
    /// using the provided configuration.
    /// </summary>
    /// <typeparam name="T">The type of values to generate.</typeparam>
    /// <param name="config">The configuration to use for automatic generation.</param>
    static member AutoWith<'T>(config: IAutoGenConfig) : Gen<'T> = Gen.autoWith<'T> config

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
    static member Frequency ([<ParamArray>] values : array<struct (int * Gen<'T>)>) : Gen<'T> =
        values |> Seq.map (fun struct (weight, gen) -> (weight, gen)) |> Gen.frequency

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

    /// Generates the subset of the provided items.
    /// The generated subset will be in the same order as the input items.
    static member SubsetOf<'T>(items: 'T seq): Gen<'T seq> =
        Gen.subsetOf items

    /// Generates a permutation of the given items.
    static member Shuffle(items : 'T seq) : Gen<'T seq> =
        items |> List.ofSeq |> Gen.shuffle |> Gen.map Seq.ofList

    /// Shuffles the case of the given string.
    static member ShuffleCase(value: string) : Gen<string> =
        Gen.shuffleCase value

    /// Generates a random IPv4 address.
    static member Ipv4Address : Gen<IPAddress> =
        Gen.ipv4Address

    /// Generates a random IPv6 address.
    static member Ipv6Address : Gen<IPAddress> =
        Gen.ipv6Address

    /// Generates a random IP address (either IPv4 or IPv6)
    /// with a higher chance of generating an IPv4 address.
    static member IpAddress : Gen<IPAddress> =
        Gen.ipAddress

    /// Generates a random URI
    static member Uri : Gen<Uri> =
        Gen.uri

    /// Generates a random valid domain name.
    static member DomainName : Gen<string> =
        Gen.domainName

    /// Generates a random identifier string (starts with a lowercase letter,
    /// followed by lowercase letters, digits, or underscores) of up to the specified maximum length
    static member Identifier (maxLen: int) : Gen<string> =
        Gen.identifier maxLen

    /// Generates a random snake_case string composed of words with the specified
    /// word length and words count ranges.
    static member SnakeCase (wordLength: Range<int>, wordsCount: Range<int>) : Gen<string> =
        Gen.snakeCase wordLength wordsCount

    /// Generates a random snake_case string composed of words with the specified
    /// maximum word length and maximum words count.
    static member SnakeCase(maxWordLength: int, maxWordsCount: int) : Gen<string> =
        Gen.snakeCase (Range.linear 1 maxWordLength) (Range.linear 1 maxWordsCount)

    /// Generates a random kebab-case string composed of words with the specified
    /// word length and words count ranges.
    static member KebabCase (wordLength: Range<int>, wordsCount: Range<int>) : Gen<string> =
        Gen.kebabCase wordLength wordsCount

    /// Generates a random kebab-case string composed of words with the specified
    /// maximum word length and maximum words count.
    static member KebabCase(maxWordLength: int, maxWordsCount: int) : Gen<string> =
        Gen.kebabCase (Range.linear 1 maxWordLength) (Range.linear 1 maxWordsCount)

    /// Generates a random Latin-style name starting with an uppercase letter,
    /// followed by lowercase letters, of up to the specified maximum length.
    static member LatinName (maxLength: int) : Gen<string> =
        Gen.latinName maxLength

    /// Generates a random email address.
    static member Email : Gen<string> =
        Gen.email
