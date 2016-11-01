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

namespace Jack

/// Splittable random number generator.
type Seed =
    internal { Value : int64
               Gamma : int64 }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Seed =
    open System

    /// A predefined gamma value's needed for initializing the "root"
    /// instances of SplittableRandom that is, instances not produced
    /// by splitting an already existing instance. We choose: the odd
    /// integer closest to 2^64/φ, where φ = (1 + √5)/2 is the golden
    /// ratio, and call it GOLDEN_GAMMA.
    let [<Literal>] private goldenGamma : int64 = 0x9e3779b97f4a7c15L

    let private crashUnless (cond : bool) (msg : string) : unit =
        if cond then
            ()
        else
            failwith msg

    /// Create a new 'Seed' from a 32-bit integer.
    let ofInt32 (s0 : int) : Seed =
        // We want a non-negative number, but we can't just take the abs
        // of s0 as -Int32.MinValue == Int32.MinValue.
        let s = s0 &&& Int32.MaxValue

        // TODO remove crashUnless
        crashUnless (s >= 0) "s >= 0"

        { Value = int64 s
          Gamma = goldenGamma }

    /// Mix the bits of a 64-bit arg to produce a result, computing a
    /// bijective function on 64-bit values.
    let private mix64 (s0 : int64) : int64 =
        let s = s0
        let s = (s ^^^ (s >>> 33)) * 0xff51afd7ed558ccdL
        let s = (s ^^^ (s >>> 33)) * 0xc4ceb9fe1a85ec53L
        s ^^^ (s >>> 33)

    /// Mix the bits of a 64-bit arg to produce a result, computing a
    /// bijective function on 64-bit values.
    let private mix64variant13 (s0 : int64) : int64 =
        let s = s0
        let s = (s ^^^ (s >>> 30)) * 0xbf58476d1ce4e5b9L
        let s = (s ^^^ (s >>> 27)) * 0x94d049bb133111ebL
        s ^^^ (s >>> 31)

    let private bitCount (s0 : int64) : int =
        let s = s0 - ((s0 >>> 1) &&& 0x5555555555555555L)
        let s = (s &&& 0x3333333333333333L) + ((s >>> 2) &&& 0x3333333333333333L)
        let s = (s + (s >>> 4)) &&& 0x0f0f0f0f0f0f0f0fL
        let s = s + (s >>> 8)
        let s = s + (s >>> 16)
        let s = s + (s >>> 32)
        (int s) &&& 0x7f

    /// Mix the bits of a 64-bit arg to produce a result, computing a
    /// bijective function on 64-bit values.
    let private mixGamma (g0 : int64) : int64 =
        let g = mix64variant13 g0 ||| 1L
        let n = bitCount (g ^^^ (g >>> 1))
        if n < 24 then g ^^^ 0xaaaaaaaaaaaaaaaaL
        else g
    
    /// Create a new random 'Seed'.
    let random () : Seed =
        let s = System.DateTimeOffset.UtcNow.Ticks + 2L * goldenGamma
        { Value = mix64 s
          Gamma = mixGamma s + goldenGamma }
    
    /// Mix the bits of a 64-bit arg to produce a result, computing a
    /// bijective function on 64-bit values.
    let private mix32 (s0 : int64) : int =
        let s = s0
        let s = (s ^^^ (s >>> 33)) * 0xff51afd7ed558ccdL
        let s = (s ^^^ (s >>> 33)) * 0xc4ceb9fe1a85ec53L
        (int) (s >>> 32)

    let private nextSeed (s0 : Seed) : Seed =
        { s0 with Value = s0.Value + s0.Gamma }

    /// Returns the next pseudo-random number in the sequence, and a new seed.
    let next (s0 : Seed) : int * Seed =
        let s = nextSeed s0
        let n = mix32 s.Value
        n, nextSeed s
    
    /// Generate a random bigint in the specified range.
    let rec nextBigInt (lo : bigint) (hi : bigint) (seed : Seed) : bigint * Seed =
        if lo > hi then
            nextBigInt hi lo seed
        else
            let s1 = nextSeed seed
            let mutable r = bigint (mix32 s1.Value)
            if (lo < hi) then
                let n = hi - lo
                let m = n - (bigint 1)
                if (n &&& m) = (bigint 0) then (r &&& m) + lo, s1
                elif n > (bigint 0) then
                    let mutable u = r >>> 1
                    let mutable s = nextSeed s1
                    while u + m - (r <- u % n
                                   r)
                          < bigint 0 do
                        u <- bigint (mix32 s.Value >>> 1)
                        r <- r + lo
                        s <- nextSeed s
                    r, s
                else
                    let mutable s = nextSeed s1
                    while (r < lo || r >= hi) do
                        r <- bigint (mix32 s.Value)
                        s <- nextSeed s
                    r, s
            else r, s1

    /// Splits a random number generator in to two.
    let split (s0 : Seed) : Seed * Seed =
        let s1 = nextSeed s0
        let s2 = nextSeed s1
        { s0 with Value = mix64 s1.Value },
        { s1 with Value = mix64 s2.Value }
