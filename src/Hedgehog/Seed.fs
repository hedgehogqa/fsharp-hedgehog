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
// generator in dotnet-jack [3] – QuickCheck with shrinking for free.
//
// Other than the choice of initial seed for 'ofRandomSeed' this port
// should be faithful. Currently, we have not rerun the DieHarder, or
// BigCrush tests on this implementation.
//
// 1. Guy L. Steele, Jr., Doug Lea, Christine H. Flood
//    Fast splittable pseudorandom number generators
//    Comm ACM, 49(10), Oct 2014, pp453-472.
//
// 2. https://github.com/fscheck/FsCheck/issues/198
// 3. https://github.com/jystic/dotnet-jack/issues/26
//

namespace Hedgehog

/// Splittable random number generator.
type Seed =
    { Value : uint64
      Gamma : uint64 }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Seed =
    open System

    /// A predefined gamma value's needed for initializing the "root"
    /// instances of SplittableRandom that is, instances not produced
    /// by splitting an already existing instance. We choose: the odd
    /// integer closest to 2^64/φ, where φ = (1 + √5)/2 is the golden
    /// ratio, and call it GOLDEN_GAMMA.
    let [<Literal>] private goldenGamma : uint64 =
        0x9e3779b97f4a7c15UL

    let private mix64 (s0 : uint64) : uint64 =
        let s = s0
        let s = (s ^^^ (s >>> 33)) * 0xff51afd7ed558ccdUL
        let s = (s ^^^ (s >>> 33)) * 0xc4ceb9fe1a85ec53UL
        s ^^^ (s >>> 33)

    let private mix64variant13 (s0 : uint64) : uint64 =
        let s = s0
        let s = (s ^^^ (s >>> 30)) * 0xbf58476d1ce4e5b9UL
        let s = (s ^^^ (s >>> 27)) * 0x94d049bb133111ebUL
        s ^^^ (s >>> 31)

    let private bitCount (s0 : uint64) : uint64 =
        let s = s0 - ((s0 >>> 1) &&& 0x5555555555555555UL)
        let s = (s &&& 0x3333333333333333UL) + ((s >>> 2) &&& 0x3333333333333333UL)
        let s = (s + (s >>> 4)) &&& 0x0f0f0f0f0f0f0f0fUL
        let s = s + (s >>> 8)
        let s = s + (s >>> 16)
        let s = s + (s >>> 32)
        s &&& 0x7fUL

    let private mixGamma (g0 : uint64) : uint64 =
        let g = mix64variant13 g0 ||| 1UL
        let n = bitCount (g ^^^ (g >>> 1))
        if n < 24UL then g ^^^ 0xaaaaaaaaaaaaaaaaUL
        else g

    let private nextSeed (s0 : Seed) : Seed =
        { s0 with Value = s0.Value + s0.Gamma }

    /// Create a new 'Seed'.
    let from (s : uint64) : Seed =
        { Value = mix64 s
          Gamma = mixGamma (s + goldenGamma) }

    /// Create a new random 'Seed'.
    let random () : Seed =
        from (uint64 DateTimeOffset.UtcNow.Ticks + 2UL * goldenGamma)

    /// The possible range of values returned from 'next'.
    let range : int64 * int64 =
        -9223372036854775808L, 9223372036854775807L

    /// Returns the next pseudo-random number in the sequence, and a new seed.
    let private next (s : Seed) : uint64 * Seed =
        mix64 s.Value, nextSeed s

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
                    let x, seed1 = next seed0
                    let v1 = v0 * b + (bigint x - bigint genlo)
                    loop (mag * b) v1 seed1

            let v, seedN = loop 1I 0I seed

            // TODO remove crashUnless
            crashUnless (v >= 0I) "v >= 0I"
            crashUnless (k >= 0I) "k >= 0I"
            lo + v % k, seedN

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

    /// Splits a random number generator in to two.
    let split (s0 : Seed) : Seed * Seed =
        let s1 = nextSeed s0
        let s2 = nextSeed s1
        { s0 with Value = mix64 s1.Value },
        { s1 with Value = mix64 s2.Value }
