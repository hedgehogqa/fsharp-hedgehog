namespace Hedgehog

open Hedgehog.FSharp

[<AutoOpen>]
module GenDateTime =

    open System

    [<RequireQualifiedAccess>]
    module Gen =

        /// <summary>
        /// Generates a random DateTime using the given range.
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
        let dateTime (range : Range<DateTime>) : Gen<DateTime> =
            gen {
                let! ticks = range |> Range.map _.Ticks |> Gen.integral
                return DateTime ticks
            }

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
