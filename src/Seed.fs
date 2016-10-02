//
// This is a port of GHC's System.Random implementation.
//
// This implementation uses the Portable Combined Generator of L'Ecuyer for
// 32-bit computers [1], transliterated by Lennart Augustsson.
//
// 1. Pierre L'Ecuyer
//    Efficient and portable combined random number generators
//    Comm ACM, 31(6), Jun 1988, pp742-749.
//

#light
namespace Jack

/// Splittable random number generator.
type Seed =
    | Seed of int * int

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Seed =
    open System

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

        // The integer variables s1 and s2 must be initialized to values
        // in the range [1, 2147483562] and [1, 2147483398] respectively. [1]
        crashUnless (s >= 0) "s >= 0"
        let q, s1 = Math.DivRem(s, 2147483562)
        crashUnless (q >= 0) "q >= 0"
        let s2 = q % 2147483398
    
        Seed (s1 + 1, s2 + 1)
    
    /// Create a new 'Seed' using 'System.Random' to seed the generator.
    let random () : Seed =
        Random().Next() |> ofInt32

    /// The possible range of values returned from 'next'.
    let range : int * int =
        1, 2147483562
    
    /// Returns the next pseudo-random number in the sequence, and a new seed.
    let next (Seed (s1, s2)) : int * Seed =
        // TODO remove crashUnless
        crashUnless (s1 >= 0) "s1 >= 0"
        let k    = s1 / 53668
        let s1'  = 40014 * (s1 - k * 53668) - k * 12211
        let s1'' = if s1' < 0 then s1' + 2147483563 else s1'
    
        crashUnless (s2 >= 0) "s2 >= 0"
        let k'   = s2 / 52774
        let s2'  = 40692 * (s2 - k' * 52774) - k' * 3791
        let s2'' = if s2' < 0 then s2' + 2147483399 else s2'
    
        let z    = s1'' - s2''
        let z'   = if z < 1 then z + 2147483562 else z
    
        z', Seed (s1'', s2'')
    
    /// Generate a random bigint in the specified range.
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

    /// Splits a random number generator in to two.
    let split (Seed (s1, s2) as seed) : Seed * Seed =
        let (Seed (t1, t2)) = snd (next seed)
    
        // no statistical foundation for this!
        let new_s1 = if s1 = 2147483562 then 1 else s1 + 1
        let new_s2 = if s2 = 1 then 2147483398 else s2 - 1
        let left   = Seed (new_s1, t2)
        let right  = Seed (t1, new_s2)
    
        left, right
