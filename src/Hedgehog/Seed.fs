//
// This is a port of "Fast Splittable Pseudorandom Number Generators"
// by Steele et. al. [1].
//
// The paper's algorithm provides decent randomness for most purposes
// but sacrifices cryptographic-quality randomness in favor of speed.
// The original implementation is tested with DieHarder and BigCrush;
// see the paper for details.
//
// This implementation is a port from the paper, and also taking into
// account the SplittableRandom.java source code in OpenJDK v8u40-b25
// as well as splittable_random.ml in Jane Street's standard library
// overlay (kernel) v113.33.03, and Random.fs in FsCheck v3, which is
// the initial motivation of doing this port [2] although the idea of
// doing this port is for having it as the default, splittable random
// generator in fsharp-hedgehog [3] - QuickCheck with shrinking for
// free.
//
// Other than the choice of initial seed for 'ofRandomSeed' this port
// should be faithful. Currently, we have not rerun the DieHarder, or
// BigCrush tests on this implementation.
//
// 1. Guy L. Steele, Jr., Doug Lea, Christine H. Flood
//    Fast splittable pseudorandom number generators
//    Comm ACM, 49(10), Oct 2014, pp453-472.
//
// 2. Nikos Baxevanis
//    https://github.com/moodmosaic/SplitMix/blob/master/SplitMix.hs
//
// 3. F# Hedgehog
//    https://github.com/hedgehogqa/fsharp-hedgehog/issues/26
//

namespace Hedgehog

/// Splittable random number generator.
type Seed =
    { Value : uint64
      /// Must be an odd number.
      Gamma : uint64 }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Seed =
    open System

    /// A predefined gamma value's needed for initializing the "root"
    /// instances of 'Seed'. That is, instances not produced
    /// by splitting an already existing instance. We choose: the odd
    /// integer closest to 2^64/φ, where φ = (1 + √5)/2 is the golden
    /// ratio.
    let [<Literal>] private GoldenGamma : uint64 =
        0x9e3779b97f4a7c15UL

    let private mix64 (x : uint64) : uint64 =
        let y = (x ^^^ (x >>> 33)) * 0xff51afd7ed558ccdUL
        let z = (y ^^^ (y >>> 33)) * 0xc4ceb9fe1a85ec53UL
        z ^^^ (z >>> 33)

    let private mix64variant13 (x : uint64) : uint64 =
        let y = (x ^^^ (x >>> 30)) * 0xbf58476d1ce4e5b9UL
        let z = (y ^^^ (y >>> 27)) * 0x94d049bb133111ebUL
        z ^^^ (z >>> 31)

    let internal bitCount (s0 : uint64) : uint64 =
        let s = s0 - ((s0 >>> 1) &&& 0x5555555555555555UL)
        let s = (s &&& 0x3333333333333333UL) + ((s >>> 2) &&& 0x3333333333333333UL)
        let s = (s + (s >>> 4)) &&& 0x0f0f0f0f0f0f0f0fUL
        let s = s + (s >>> 8)
        let s = s + (s >>> 16)
        let s = s + (s >>> 32)
        s &&& 0x7fUL

    let private mixGamma (x : uint64) : uint64 =
        let y = mix64variant13 x ||| 1UL
        let n = bitCount (y ^^^ (y >>> 1))
        if n < 24UL then
            y ^^^ 0xaaaaaaaaaaaaaaaaUL
        else
            y

    /// Create a new 'Seed' from the supplied values.
    let private newSeed value gamma =
        { Value = value
          Gamma = gamma }

    /// Create a new 'Seed' by mixing the supplied values.
    let private mixSeed value gamma : Seed =
        newSeed (mix64 value) (mixGamma gamma)

    /// Create a new 'Seed'.
    let from (x : uint64) : Seed =
        mixSeed x (x + GoldenGamma)

    /// Create a new random 'Seed'.
    let random () : Seed =
        from (uint64 DateTimeOffset.UtcNow.Ticks + 2UL * GoldenGamma)

    /// The possible range of values returned from 'next'.
    let range : int64 * int64 =
        Int64.MinValue, Int64.MaxValue

    /// Returns the next pseudo-random number in the sequence, and a new seed.
    let private next (seed : Seed) : uint64 * Seed =
        let g = seed.Gamma
        let v = seed.Value + g
        (v, newSeed v g)

    /// Generates a random 'System.UInt64'.
    let nextUInt64 (seed : Seed) : uint64 * Seed =
        let (v, s) = next seed
        (mix64 v, s)

    let private crashUnless (cond : bool) (msg : string) : unit =
        if cond then
            ()
        else
            failwith msg

    /// Generates a random bigint in the specified range.
    let rec nextBigInt (lo : bigint) (hi : bigint) (seed : Seed) : bigint * Seed =
        if lo > hi then
            nextBigInt hi lo seed
        else
            //
            // Probabilities of the most likely and least likely result will differ
            // at most by a factor of (1 +- 1/q). Assuming Seed is uniform, of
            // course.
            //
            // On average, log q / log b more random values will be generated than
            // the minimum.
            //

            let genlo, genhi = range
            let b = bigint genhi - bigint genlo + 1I

            let q = 1000I
            let k = hi - lo + 1I
            let magtgt = k * q

            // Generate random values until we exceed the target magnitude.
            let rec loop mag v0 seed0 =
                if mag >= magtgt then
                    v0, seed0
                else
                    let x, seed1 = nextUInt64 seed0
                    let v1 = v0 * b + (bigint x - bigint genlo)
                    loop (mag * b) v1 seed1

            let v, seedN = loop 1I 0I seed

            // TODO remove crashUnless
            crashUnless (v >= 0I) "v >= 0I"
            crashUnless (k >= 0I) "k >= 0I"
            lo + v % k, seedN

    /// Generates a random int32 in the specified range.
    let rec nextInt32 (lo : int32) (hi : int32) (seed : Seed) : int32 * Seed =
        if lo > hi then
            nextInt32 hi lo seed
        else
            let range = uint32 (hi - lo)
            // If range is UInt32.MaxValue, we can just use the full 32 bits from nextUInt64
            if range = UInt32.MaxValue then
                let x, seed1 = nextUInt64 seed
                int32 x, seed1
            else
                let limit = UInt32.MaxValue - (UInt32.MaxValue % (range + 1u))
                let rec loop seed0 =
                    let x, seed1 = nextUInt64 seed0
                    let v = uint32 x
                    if v <= limit then
                        int32 (v % (range + 1u)) + lo, seed1
                    else
                        loop seed1
                loop seed

    /// Generates a random int64 in the specified range.
    let rec nextInt64 (lo : int64) (hi : int64) (seed : Seed) : int64 * Seed =
        if lo > hi then
            nextInt64 hi lo seed
        else
            let range = uint64 (hi - lo)
            // If range is UInt64.MaxValue, we can just use the full 64 bits from nextUInt64
            if range = UInt64.MaxValue then
                let x, seed1 = nextUInt64 seed
                int64 x, seed1
            else
                let limit = UInt64.MaxValue - (UInt64.MaxValue % (range + 1UL))
                let rec loop seed0 =
                    let x, seed1 = nextUInt64 seed0
                    if x <= limit then
                        int64 (x % (range + 1UL)) + lo, seed1
                    else
                        loop seed1
                loop seed

    /// Generates a random double in the specified range.
    let rec nextDouble (lo : double) (hi : double) (seed : Seed) : double * Seed =
        if lo > hi then
            nextDouble hi lo seed
        else
            let x, seed' =
                seed |> nextBigInt
                    (bigint Int32.MinValue)
                    (bigint Int32.MaxValue)
            let scaledX =
                  (0.5 * lo + 0.5 * hi) + ((0.5 * hi - 0.5 * lo) / (0.5 * 4294967296.0)) * float x

            scaledX, seed'

    /// Splits a 'Seed' in to two.
    let split (seed : Seed) : Seed * Seed =
        let (value, seed1) = next seed
        let (gamma, seed2) = next seed1
        (seed2, mixSeed value gamma)
