namespace Hedgehog

[<AutoOpen>]
module GenPrimitives =

    open System

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    [<RequireQualifiedAccess>]
    module Gen =

        /// Generates a random character in the given range.
        let char (lo : char) (hi : char) : Gen<char> =
            Range.constant (int lo) (int hi)
            |> Gen.integral
            |> Gen.map char

        /// Generates a Unicode character, including invalid standalone surrogates,
        /// i.e. from '\000' to '\65535'.
        let unicodeAll : Gen<char> =
            let lo = Char.MinValue
            let hi = Char.MaxValue
            char lo hi

        /// <summary>
        /// Generates a random numerical character, i.e. from '0' to '9'.
        /// </summary>
        /// <example>
        /// Combine with <see cref="T:Hedgehog.Gen.string"/> to create strings of a desired length.
        /// <code>
        /// Gen.digit |> Gen.string (Range.constant 5 10)
        /// </code>
        /// </example>
        let digit : Gen<char> =
            char '0' '9'


        /// <summary>
        /// Generates a random lowercase character, i.e. from 'a' to 'z'.
        /// </summary>
        /// <example>
        /// Combine with <see cref="T:Hedgehog.Gen.string"/> to create strings of a desired length.
        /// <code>
        /// Gen.lower |> Gen.string (Range.constant 5 10)
        /// </code>
        /// </example>
        let lower : Gen<char> =
            char 'a' 'z'

        /// <summary>
        /// Generates a random uppercase character, i.e. from 'A' to 'Z'.
        /// </summary>
        /// <example>
        /// Combine with <see cref="T:Hedgehog.Gen.string"/> to create strings of a desired length.
        /// <code>
        /// Gen.upper |> Gen.string (Range.constant 5 10)
        /// </code>
        /// </example>
        let upper : Gen<char> =
            char 'A' 'Z'

        /// <summary>
        /// Generates a random ASCII character, i.e. from '\000' to '\127', i.e. any 7 bit character.
        /// </summary>
        /// <remarks>
        /// Non-printable and control characters can be generated, e.g. NULL and BEL.
        /// </remarks>
        /// <example>
        /// Combine with <see cref="T:Hedgehog.Gen.string"/> to create strings of a desired length.
        /// <code>
        /// Gen.ascii |> Gen.string (Range.constant 5 10)
        /// </code>
        /// </example>
        let ascii : Gen<char> =
            char '\000' '\127'

        /// <summary>
        /// Generates a random Latin-1 character, i.e. from '\000' to '\255', i.e. any 8 bit character.
        /// </summary>
        /// <remarks>
        /// Non-printable and control characters can be generated, e.g. NULL and BEL.
        /// </remarks>
        /// <example>
        /// Combine with <see cref="T:Hedgehog.Gen.string"/> to create strings of a desired length.
        /// <code>
        /// Gen.latin1 |> Gen.string (Range.constant 5 10)
        /// </code>
        /// </example>
        let latin1 : Gen<char> =
            char '\000' '\255'

        /// <summary>
        /// Generates a Unicode character, excluding non-characters ('\65534' and '\65535') and invalid standalone surrogates (from '\55296' to '\57343').
        /// </summary>
        /// <example>
        /// Combine with <see cref="T:Hedgehog.Gen.string"/> to create strings of a desired length.
        /// <code>
        /// Gen.unicode |> Gen.string (Range.constant 5 10)
        /// </code>
        /// </example>
        let unicode : Gen<char> =
            let isNoncharacter x =
                   x = Operators.char 65534
                || x = Operators.char 65535
            unicodeAll
            |> Gen.filter (not << isNoncharacter)
            |> Gen.filter (not << Char.IsSurrogate)

        /// <summary>
        /// Generates an alphabetic character, i.e. 'a' to 'z' or 'A' to 'Z'.
        /// </summary>
        /// <example>
        /// Combine with <see cref="T:Hedgehog.Gen.string"/> to create strings of a desired length.
        /// <code>
        /// Gen.alpha |> Gen.string (Range.constant 5 10)
        /// </code>
        /// This generates strings such as <c>Ldklk</c> or <c>aFDG</c>
        /// </example>
        let alpha : Gen<char> =
            Gen.choice [lower; upper]

        /// <summary>
        /// Generates an alphanumeric character, i.e. 'a' to 'z', 'A' to 'Z', or '0' to '9'.
        /// </summary>
        /// <example>
        /// Combine with <see cref="T:Hedgehog.Gen.string"/> to create strings of a desired length.
        /// <code>
        /// Gen.alphaNum |> Gen.string (Range.constant 5 10)
        /// </code>
        /// This generates strings such as <c>Ld5lk</c> or <c>4dFDG</c>
        /// </example>
        let alphaNum : Gen<char> =
            Gen.choice [lower; upper; digit]

        /// Generates a random string using 'Range' to determine the length and the
        /// given character generator.
        let string (range : Range<int>) (g : Gen<char>) : Gen<string> =
            Gen.array range g
            |> Gen.map String

        //
        // Combinators - Primitives
        //

        /// Generates a random boolean.
        let bool : Gen<bool> =
            Gen.item [false; true]

        /// Generates a random byte.
        let byte (range : Range<byte>) : Gen<byte> =
            Gen.integral range

        /// Generates a random signed byte.
        let sbyte (range : Range<sbyte>) : Gen<sbyte> =
            Gen.integral range

        /// Generates a random signed 16-bit integer.
        let int16 (range : Range<int16>) : Gen<int16> =
            Gen.integral range

        /// Generates a random unsigned 16-bit integer.
        let uint16 (range : Range<uint16>) : Gen<uint16> =
            Gen.integral range

        /// Generates a random signed 32-bit integer.
        let int32 (range : Range<int32>) : Gen<int32> =
            Gen.integral range

        /// Generates a random unsigned 32-bit integer.
        let uint32 (range : Range<uint32>) : Gen<uint32> =
            Gen.integral range

        /// Generates a random signed 64-bit integer.
        let int64 (range : Range<int64>) : Gen<int64> =
            Gen.integral range

        /// Generates a random unsigned 64-bit integer.
        let uint64 (range : Range<uint64>) : Gen<uint64> =
            Gen.integral range

        /// Generates a random 64-bit floating point number.
        let double (range : Range<double>) : Gen<double> =
            Random.double range
            |> Gen.create (Shrink.towardsDouble (Range.origin range))

        /// Generates a random 32-bit floating point number.
        let single (range : Range<single>) : Gen<single> =
            double (Range.map ExtraTopLevelOperators.double range) |> Gen.map single

        /// Generates a random decimal floating-point number.
        let decimal (range : Range<decimal>) : Gen<decimal> =
            double (Range.map ExtraTopLevelOperators.double range) |> Gen.map decimal

        /// Generates a random big integer.
        let bigint (range : Range<bigint>) : Gen<bigint> =
            Gen.integral range

        /// Generates a random globally unique identifier.
        let guid : Gen<Guid> = gen {
            let! bs = Range.constantBounded () |> byte |> Gen.array (Range.singleton 16)
            return Guid bs
        }
