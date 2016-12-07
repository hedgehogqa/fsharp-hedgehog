[![Build Status](https://travis-ci.org/jystic/dotnet-jack.svg?branch=master)](https://travis-ci.org/jystic/dotnet-jack)

# dotnet-jack

An alternative property-based testing system for F#, in the spirit of John Hughes & Koen Classen's [QuickCheck](https://web.archive.org/web/20160319204559/http://www.cs.tufts.edu/~nr/cs257/archive/john-hughes/quick.pdf). The key improvement is that shrinking comes for free.

![](https://github.com/moodmosaic/dotnet-jack/raw/master/img/dice.jpg)

*Jack's love of dice has brought him here, where he has taken on the form of an F# library, in order to help you gamble with your properties.*

## Highlights

* Shrinking is baked into the `Gen` type, so you get it for free. This is not a trivial distinction. Integrating shrinking into generation has two large benefits:
  * Shrinking composes nicely, and you can shrink anything you can generate regardless of whether there is a defined shrinker for the type produced.
  * You can guarantee that shrinking satisfies the same invariants as generation.
* Simplified model; just generators and properties.
* Adequate randomness based on the SplitMix algorithm.
* Convenient syntax for both generators and properties with not only `gen` but also `property` expressions.

## At a glance

Given any generator of type α, Jack not only generates *random values* of type α, but also *shrinks* α into smaller values.

Jack comes with built-in generators for primitive types, so here's how it would generate a couple of integers and shrink them:

```f#
Gen.int
|> Gen.printSample;;

=== Outcome ===
0
=== Shrinks ===
.
=== Outcome ===
-12
=== Shrinks ===
0
-6
-9
-11
.
=== Outcome ===
-3
=== Shrinks ===
0
-2
.
=== Outcome ===
-29
=== Shrinks ===
0
-15
-22
-26
-28
.
=== Outcome ===
4
=== Shrinks ===
0
2
3
.
```

But Jack can also take on complex types, and shrink them for free:

```f#
Gen.byte
|> Gen.map int
|> Gen.tuple3
|> Gen.map (fun (ma, mi, bu) -> Version (ma, mi, bu)) // Major, Minor, Build
|> Gen.printSample;;

=== Outcome ===
19.15.23
=== Shrinks ===
0.15.23
10.15.23
15.15.23
17.15.23
18.15.23
19.0.23
19.8.23
19.12.23
19.14.23
19.15.0
19.15.12
19.15.18
19.15.21
19.15.22
.
=== Outcome ===
14.14.26
=== Shrinks ===
0.14.26
7.14.26
11.14.26
13.14.26
14.0.26
14.7.26
14.11.26
14.13.26
14.14.0
14.14.13
14.14.20
14.14.23
14.14.25
.
=== Outcome ===
5.11.10
=== Shrinks ===
0.11.10
3.11.10
4.11.10
5.0.10
5.6.10
5.9.10
5.10.10
5.11.0
5.11.5
5.11.8
5.11.9
.
=== Outcome ===
14.15.4
=== Shrinks ===
0.15.4
7.15.4
11.15.4
13.15.4
14.0.4
14.8.4
14.12.4
14.14.4
14.15.0
14.15.2
14.15.3
.
=== Outcome ===
2.28.32
=== Shrinks ===
0.28.32
1.28.32
2.0.32
2.14.32
2.21.32
2.25.32
2.27.32
2.28.0
2.28.16
2.28.24
2.28.28
2.28.30
2.28.31
.
```

#### 👉 Automatic shrinking is an important quality of Jack

When a property fails (because Jack found a counter-example), the randomly-generated data usually contains "noise". Therefore Jack simplifies counter-examples before reporting them:

```f#
let version =
    Gen.byte
    |> Gen.map int
    |> Gen.tuple3
    |> Gen.map (fun (ma, mi, bu) -> Version (ma, mi, bu))

property { let! xs = Gen.list version
           return xs |> List.rev = xs } |> Property.print

>
*** Failed! Falsifiable (after 3 tests and 6 shrinks):
[0.0.0; 0.0.1]
```

The above example, is the standard "hello-world" property, but instead of the classic list of integers, we're using a list of type System.Version, demonstrating that integrated shrikning works with 'foreign' types too.

---

*As a matter of fact, here's the above example written using another property-based testing system, FsCheck:*

```f#
let version =
    Arb.generate<byte>
    |> Gen.map int
    |> Gen.three
    |> Gen.map (fun (ma, mi, bu) -> Version (ma, mi, bu))
    |> Gen.listOf
    |> Arb.fromGen

version
|> Prop.forAll <| fun xs -> xs |> List.rev = xs
|> Check.Quick

>
Falsifiable, after 2 tests (0 shrinks) (StdGen (783880299,296237326)):
Original:
[183.211.153; 129.237.113; 242.27.80]
```
You can find out more about integrated vs type-based shrinking in [this](http://hypothesis.works/articles/integrated-shrinking/) blog post.

### Getting Started

The standard "hello-world" property shown in most property-based testing systems is:

```
reverse (reverse xs) = xs, ∀xs :: [α]
```

which means that "the reverse of the reverse of a list, is the list itself - for all lists of type α".

One way to use `dotnet-jack` to check the above property is to use the `property` computation expression:

```f#
property { let! xs = Gen.list Gen.int
           return List.rev (List.rev xs) = xs }
```

and to test the above property on 100 random lists of integers, pipe it into `Property.print`:

```f#
property { let! xs = Gen.list Gen.int
           return List.rev (List.rev xs) = xs }
|> Property.print

+++ OK, passed 100 tests.
```

### Generators

TODO

### Properties

TODO

## NuGet

There isn't much here yet, but `dotnet-jack` can be published on NuGet once it's in a usable state.

## Versioning

`dotnet-jack` follows [Semantic Versioning 2.0.0](http://semver.org/spec/v2.0.0.html) once it’s in a usable state.

## Credits

The idea behind `dotnet-jack` originates from [`purescript-jack`](https://github.com/jystic/purescript-jack/) in PureScript and [`disorder-jack`](https://github.com/ambiata/disorder.hs/tree/master/disorder-jack/) in Haskell.
