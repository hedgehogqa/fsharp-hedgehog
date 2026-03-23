namespace Hedgehog.FSharp

open Hedgehog

[<AutoOpen>]
module GenDateTime =

    open System

    [<RequireQualifiedAccess>]
    module Gen =

        /// <summary>
        /// Generates a random DateTimeKind uniformly.
        /// </summary>
        let dateTimeKind : Gen<DateTimeKind> =
            Gen.item
                [|
                    DateTimeKind.Utc
                    DateTimeKind.Local
                    DateTimeKind.Unspecified
                |]

        /// <summary>
        /// Generates a random DateTime using the given range and `DateTimeKind` generator.
        /// </summary>
        /// <example>
        /// <code>
        /// let range =
        ///    Range.constantFrom
        ///        (DateTime (2000, 1, 1)) DateTime.MinValue DateTime.MaxValue
        /// Gen.dateTime range
        /// </code>
        /// </example>
        /// <param name="range">Range determining the bounds of the <c>DateTime</c> that can be generated.</param>
        /// <param name="kind">Generator determining the <c>DateTimeKind</c>.</param>
        let dateTime (range : Range<DateTime>) (kind : Gen<DateTimeKind>) : Gen<DateTime> =
            gen {
                let! ticks = range |> Range.map _.Ticks |> Gen.integral
                and! kind = kind
                return DateTime(ticks, kind)
            }

        /// Generates a DateTime using the given range with UTC kind.
        let dateTimeUtc (range : Range<DateTime>) : Gen<DateTime> =
            dateTime range (Gen.constant DateTimeKind.Utc)

        /// Generates a DateTime using the given range with Local kind.
        let dateTimeLocal (range : Range<DateTime>) : Gen<DateTime> =
            dateTime range (Gen.constant DateTimeKind.Local)

        /// Generates a DateTime using the given range with Unspecified kind.
        let dateTimeUnspecified (range : Range<DateTime>) : Gen<DateTime> =
            dateTime range (Gen.constant DateTimeKind.Unspecified)

        /// Generates a random DateTimeOffset using the given range.
        let dateTimeOffset (range : Range<DateTimeOffset>) : Gen<DateTimeOffset> =
            gen {
                let! ticks = range |> Range.map _.Ticks |> Gen.integral
                // Ensure there is no overflow near the edges when adding the offset
                let minOffsetMinutes =
                  max
                    (-14L * 60L)
                    ((DateTimeOffset.MaxValue.Ticks - ticks) / TimeSpan.TicksPerMinute * -1L)
                let maxOffsetMinutes =
                  min
                    (14L * 60L)
                    ((ticks - DateTimeOffset.MinValue.Ticks) / TimeSpan.TicksPerMinute)
                let! offsetMinutes = Gen.int32 (Range.linearFrom 0 (int minOffsetMinutes) (int maxOffsetMinutes))
                return DateTimeOffset(ticks, TimeSpan.FromMinutes (float offsetMinutes))
            }

#if !FABLE_COMPILER
        /// Generates a random TimeSpan using the specified range.
        let timeSpan (range : Range<TimeSpan>) : Gen<TimeSpan> =
            range
            |> Range.map (fun x -> x.Ticks (* Fable can't do this *))
            |> Gen.int64
            |> Gen.map TimeSpan.FromTicks
#endif
