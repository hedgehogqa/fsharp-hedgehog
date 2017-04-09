[![Build Status](https://travis-ci.org/hedgehogqa/fsharp-hedgehog.svg?branch=master)](https://travis-ci.org/hedgehogqa/fsharp-hedgehog)

# fsharp-hedgehog

An alternative property-based testing system for F#, in the spirit of John Hughes & Koen Classen's [QuickCheck](https://web.archive.org/web/20160319204559/http://www.cs.tufts.edu/~nr/cs257/archive/john-hughes/quick.pdf).

The key improvement is that shrinking comes for free — instead of generating a random value and using a shrinking function after the fact, we generate the random value and all the possible shrinks in a rose tree, all at once.

<p align="center">
  <img src="https://github.com/hedgehogqa/fsharp-hedgehog/raw/master/img/hedgehog.png" alt="Hedgehog logo">
</p>

## Table of Contents

* [Highlights](#highlights)
* [Getting Started](#getting-started)
  * [At a glance](#at-a-glance)
  * [Integrated shrinking](#-integrated-shrinking-is-an-important-quality-of-hedgehog)
* [Generators](#generators)
  * [The `gen` expression](#-generators-can-also-be-created-using-the-gen-expression)
* [Properties](#properties)
  * [The `property` expression](#-properties-can-also-be-created-using-the-property-expression)
  * [Custom Operations](#custom-operations)
    * [`counterexample`](#counterexample)
    * [`where`](#where)
* [NuGet](#nuget)
* [Versioning](#versioning)
* [Limitations](#limitations)
* [Integrations](#integrations)
  * [Regex-constrained strings](#regex-constrained-strings)
* [Credits](#credits)
* [License](https://github.com/hedgehogqa/fsharp-hedgehog/blob/master/LICENSE)

## Highlights

* Shrinking is baked into the `Gen` type, so you get it for free. This is not a trivial distinction. Integrating shrinking into generation has two large benefits:
  * Shrinking composes nicely, and you can shrink anything you can generate regardless of whether there is a defined shrinker for the type produced.
  * You can guarantee that shrinking satisfies the same invariants as generation.
* Simplified model; just generators and properties.
* Adequate randomness based on the SplitMix algorithm.
* Convenient syntax for both generators and properties with not only `gen` but also `property` expressions.

## At a glance

Given any generator of type α, Hedgehog not only generates *random values* of type α, but also *shrinks* α into smaller values.

Hedgehog comes with built-in generators for primitive types, so here's how it would generate a couple of integers and shrink them:

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

But Hedgehog can also take on complex types, and shrink them for free:

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

#### 👉 Integrated shrinking is an important quality of Hedgehog

When a property fails (because Hedgehog found a counter-example), the randomly-generated data usually contains "noise". Therefore Hedgehog simplifies counter-examples before reporting them:

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

One way to use Hedgehog to check the above property is to use the `property` computation expression:

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

Hedgehog's `Gen` module exports some basic generators and plenty combinators for making new generators. Here's a generator of alphanumeric chatracters:

```f#
Gen.alphaNum
```

This generator is of type `Gen<char>`, which means that Hedgehog can take this generator and produce characters, like so:

```f#
Gen.alphaNum |> Gen.printSample;;

=== Outcome ===
'3'
=== Shrinks ===
'l'
'L'
'0'
'2'
.
=== Outcome ===
'3'
=== Shrinks ===
'b'
'B'
'0'
'2'
.
=== Outcome ===
'3'
=== Shrinks ===
'x'
'X'
'0'
'2'
.
=== Outcome ===
'4'
=== Shrinks ===
'y'
'Y'
'0'
'2'
'3'
.
=== Outcome ===
't'
=== Shrinks ===
'a'
'j'
'o'
'r'
's'
.
```

Now that we've seen a generator in action, it can be interesting to see how it's created. We'll keep using `Gen.alphaNum` as an example:

```f#
let alphaNum : Gen<char> =
    choice [lower; upper; digit]
```

The `lower`, `upper`, and `digit` functions are also generators. They can be defined as:

```f#
let lower : Gen<char> =
    charRange 'a' 'z'

let upper : Gen<char> =
    charRange 'A' 'Z'

let digit : Gen<char> =
    charRange '0' '9'
```

Note that `charRange` is also a generator, which can be defined as:

```f#
let charRange (lo : char) (hi : char) : Gen<char> =
    range (int lo) (int hi) |> map char
```

So `range` is also a generator, and so on and so forth.

#### 👉 Generators can also be created using the `gen` expression

Hedgehog supports a convenient syntax for working with generators through the `gen` expression. Here's a way to define a generator of type System.Net.IPAddress:

```f#
open System.Net

let ipAddressGen : Gen<IPAddress> =
    gen { let! x = Gen.byte |> Gen.array' 4 4
          return IPAddress x }

ipAddressGen |> Gen.printSample;;

=== Outcome ===
12.6.28.32
=== Shrinks ===
0.6.28.32
6.6.28.32
9.6.28.32
11.6.28.32
12.0.28.32
12.3.28.32
12.5.28.32
12.6.0.32
12.6.14.32
12.6.21.32
12.6.25.32
12.6.27.32
12.6.28.0
12.6.28.16
12.6.28.24
12.6.28.28
12.6.28.30
12.6.28.31
.
=== Outcome ===
21.22.0.32
=== Shrinks ===
0.22.0.32
11.22.0.32
16.22.0.32
19.22.0.32
20.22.0.32
21.0.0.32
21.11.0.32
21.17.0.32
21.20.0.32
21.21.0.32
21.22.0.0
21.22.0.16
21.22.0.24
21.22.0.28
21.22.0.30
21.22.0.31
.
=== Outcome ===
26.1.8.27
=== Shrinks ===
0.1.8.27
13.1.8.27
20.1.8.27
23.1.8.27
25.1.8.27
26.0.8.27
26.1.0.27
26.1.4.27
26.1.6.27
26.1.7.27
26.1.8.0
26.1.8.14
26.1.8.21
26.1.8.24
26.1.8.26
.
=== Outcome ===
16.6.3.13
=== Shrinks ===
0.6.3.13
8.6.3.13
12.6.3.13
14.6.3.13
15.6.3.13
16.0.3.13
16.3.3.13
16.5.3.13
16.6.0.13
16.6.2.13
16.6.3.0
16.6.3.7
16.6.3.10
16.6.3.12
.
=== Outcome ===
27.19.20.17
=== Shrinks ===
0.19.20.17
14.19.20.17
21.19.20.17
24.19.20.17
26.19.20.17
27.0.20.17
27.10.20.17
27.15.20.17
27.17.20.17
27.18.20.17
27.19.0.17
27.19.10.17
27.19.15.17
27.19.18.17
27.19.19.17
27.19.20.0
27.19.20.9
27.19.20.13
27.19.20.15
27.19.20.16
```
The above System.Net.IPAddress generator, without using the `gen` expression, can be defined as:

```f#
open System.Net

let ipAddressGen : Gen<IPAddress> =
    Gen.byte
    |> Gen.array' 4 4
    |> Gen.map IPAddress
```

### Properties

Using Hedgehog, the programmer writes assertions about logical properties that a function should fulfill.

Take [`List.rev`](https://msdn.microsoft.com/visualfsharpdocs/conceptual/list.rev%5b%27t%5d-function-%5bfsharp%5d) as an example, which is a function that returns a new list with the elements in reverse order:

```f#
List.rev [1; 2; 3];;

val it : int list = [3; 2; 1]
```

One logical property of `List.rev` is:

* Calling `List.rev` twice must return the elements in the original order.

Here's an example assertion:

```f#
List.rev (List.rev [1; 2; 3]) = [1; 2; 3];;

val it : bool = true
```

#### A generic assertion

In the previous example `List.rev` was tested against an example value `[1; 2; 3]`. To make the assertion generic, the example value can be parameterized as *any list*:

```f#
fun xs -> List.rev (List.rev xs) = xs;;

val it : xs:'a list -> bool when 'a : equality = <fun:clo@33>
```

Hedgehog will then attempt to generate a test case that *falsifies* the assertion. In order to do that, it needs to know which generator to use, to feed `xs` with random values.

#### A generator for lists of integers

Values for `xs` need to be generated by a generator, as shown in the *Generators* sections. The following one is for lists of type integer:

```f#
let g = Gen.list Gen.int;;

val g : Gen<int list>
```

Every possible value generated by the `g` generator must now be supplied to the assertion, as shown below:

#### A first property

```f#
fun xs ->
    List.rev (List.rev xs) = xs
    |> Property.ofBool
|> Property.forAll g;;

val it : Property<unit>
```

>But what is `forAll`? This comes from [predicate logic](https://en.wikipedia.org/wiki/Universal_quantification) and essentially means that the assertion holds *for all* possible values generated by `g`.

#### 👉 Properties can also be created using the `property` expression

Here's how the previous property can be rewritten:

```f#
property {
    let! xs = g
    return List.rev (List.rev xs) = xs
}
```

#### Try out (see it pass)

```f#
let g = Gen.list Gen.int

property {
    let! xs = g
    return List.rev (List.rev xs) = xs
}
|> Property.print' 500<tests>;;

>
+++ OK, passed 500 tests.

val g : Gen<List<int>> = Gen (Random <fun:sized@79-2>)
val it : unit = ()
>
```

The above property was exercised 500 times. The default is 100, which is what `Property.print` does:

```f#
let g = Gen.list Gen.int

property {
    let! xs = g
    return List.rev (List.rev xs) = xs
}
|> Property.print

>
+++ OK, passed 100 tests.

val g : Gen<List<int>> = Gen (Random <fun:sized@79-2>)
val it : unit = ()
>
```

>Outside of F# Interactive, you might want to use `Property.check` or `Property.check'`, specially if you're using Unquote with xUnit, NUnit, MSTest, or similar.

#### Try out (see it fail)

```f#
let tryAdd a b =
    if a > 100 then None // Nasty bug.
    else Some (a + b)

property { let! a = Gen.int
           let! b = Gen.int
           return tryAdd a b = Some (a + b) }
|> Property.print;;

>
*** Failed! Falsifiable (after 16 tests and 4 shrinks):
101

val tryAdd : a:int -> b:int -> int option
val it : unit = ()
>
```

The test now fails. — Notice how Hedgehog reports back the minimal counter-example. This process is called shrinking.

### Custom Operations

The `Property` module and its `property` expression supports a few [custom operations](https://docs.microsoft.com/en-us/dotnet/articles/fsharp/language-reference/computation-expressions#custom-operations) as well.

#### `counterexample`

Here's how the previous example could be written in order to carry along a friendlier message when it fails:

```f#
let tryAdd a b =
    if a > 100 then None // Nasty bug.
    else Some(a + b)

property { let! a = Gen.int
           let! b = Gen.int
           counterexample (sprintf "The value of a was %d." a)
           return tryAdd a b = Some(a + b) }
|> Property.print;;

>
*** Failed! Falsifiable (after 16 tests and 5 shrinks):
101
The value of a was 101.

val tryAdd : a:int -> b:int -> int option
val it : unit = ()
>
```

#### `where`

Here’s how the previous example could be written so that in never fails:

```f#
let tryAdd a b =
    if a > 100 then None // Nasty bug.
    else Some(a + b)

property { let! a = Gen.int
           let! b = Gen.int
           where (a < 100)
           return tryAdd a b = Some(a + b) }
|> Property.print;;

>
*** Gave up after 100 discards, passed 95 tests.
>
```

Essentially, the `where` custom operation discards test cases which do not satisfy the given condition.

Test case generation continues until 100 cases (the default of `Property.print`) which do satisfy the condition have been found, or until an overall limit on the number of test cases is reached (to avoid looping if the condition never holds).

In this case a message such as

```
Gave up after 100 discards, passed 95 tests.
```

indicates that 95 test cases satisfying the condition were found, and that the property held in those 95 cases.

## NuGet

To install Hedgehog, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console)

```
PM> Install-Package Hedgehog
```

## Versioning

Hedgehog follows [Semantic Versioning 2.0.0](http://semver.org/spec/v2.0.0.html).

According to [semantic versioning specification](http://semver.org/spec/v2.0.0.html#spec-item-4
), until version 1.0.0 is released, major version zero (0.y.z) is for initial development. Anything may change at any time. The public API should not be considered stable.

## Limitations

Some of the features you'd expect from a property-based testing system are still missing, but we'll get there eventually:

* Generating functions
* Model-based testing

Hedgehog doesn't have an Arbitrary type class, by design. The main purpose of the Arbitrary class is to link the generator with a shrink function — this isn't required with Hedgehog so Arbitrary has been eliminated.

This library is still very new, and we wouldn't be surprised if some of the combinators are still a bit buggy.

## Integrations

Use your favorite tools with Hedgehog.

Powerful integrations that help you and your team build properties in an easier way.

### Regex-constrained strings

In Haskell, there's the [quickcheck-regex](https://hackage.haskell.org/package/quickcheck-regex) package, by [Audrey (唐鳳) Tang](https://www.linkedin.com/in/tangaudrey), which allows to write and execute this:

```haskell
generate (matching "[xX][0-9a-z]")
// Prints -> "''UVBw"
```

It exports a `matching` function that turns a Regular Expression into a [DFA](https://en.wikipedia.org/wiki/Deterministic_finite_automaton)/[NFA](https://en.wikipedia.org/wiki/Nondeterministic_finite_automaton) finite-state machine and then into a generator of strings matching that regex:

```haskell
matching :: String -> Gen String
```

A similar generator in F# with Hedgehog can be written as shown below:

```f#
open Hedgehog
open Fare

/// Curried version of Regex.IsMatch, for indicating
/// whether a given regular expression finds a match
/// in the input string.
let matches candidate pattern =
    System.Text.RegularExpressions.Regex.IsMatch (candidate, pattern)

/// Generates a string that is guaranteed to
/// match the regular expression passed in.
let fromRegex (pattern : string) : Gen<string> =
    Gen.sized (fun size ->
        let xeger = Xeger pattern
        [ for i in 1..size -> xeger.Generate () ]
        |> Gen.item)
```

The `fromRegex` function uses the [.NET port](https://www.nuget.org/packages/Fare/) of [dk.brics.automaton](http://www.brics.dk/automaton/) and [xeger](https://code.google.com/p/xeger/).

Here's a way to use it:


```f#
let pattern = "^http\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(/\S*)?$"

Property.print <| property { let! s = fromRegex pattern
                             return matches s pattern }

>
+++ OK, passed 100 tests.
```

## Credits

The idea behind the F# version of Hedgehog originates from [`purescript-jack`](https://github.com/jystic/purescript-jack/) in PureScript and [`disorder-jack`](https://github.com/ambiata/disorder.hs/tree/master/disorder-jack/) in Haskell.
