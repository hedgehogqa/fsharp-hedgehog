# Tutorial

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
* [Integrations](#integrations)
  * [Regex-constrained strings](#regex-constrained-strings)

### Getting Started

The standard "hello-world" property shown in most property-based testing systems is:

```
reverse (reverse xs) = xs, ∀xs :: [α]
```

which means that "the reverse of the reverse of a list, is the list itself - for all lists of type α".

One way to use Hedgehog to check the above property is to use the `property` computation expression:

```fs
property {
    let! xs = Gen.list (Range.linear 0 100) <| Gen.int (Range.constant 0 1000)
    return List.rev (List.rev xs) = xs
    }
```

and to test the above property on 100 random lists of integers, pipe it into `Property.print`:

```fs
property {
    let! xs = Gen.list (Range.linear 0 100) <| Gen.int (Range.constant 0 1000)
    return List.rev (List.rev xs) = xs
    }
|> Property.print

+++ OK, passed 100 tests.
```

## At a glance

Given any generator of type α, Hedgehog not only generates *random values* of type α, but also *shrinks* α into smaller values.

Hedgehog comes with built-in generators for primitive types, so here's how it would generate a couple of integers and shrink them:

```fs
Range.constant 0 100
|> Gen.int
|> Gen.printSample;;

=== Outcome ===
77
=== Shrinks ===
0
39
58
68
73
75
76
.
=== Outcome ===
39
=== Shrinks ===
0
20
30
35
37
38
.
=== Outcome ===
2
=== Shrinks ===
0
1
.
=== Outcome ===
34
=== Shrinks ===
0
17
26
30
32
33
.
=== Outcome ===
28
=== Shrinks ===
0
14
21
25
27
.
```

But Hedgehog can also take on complex types, and shrink them for free:

```fs
Range.constantBounded ()
|> Gen.byte
|> Gen.map int
|> Gen.tuple3
|> Gen.map (fun (ma, mi, bu) -> Version (ma, mi, bu))
|> Gen.printSample;;

=== Outcome ===
60.8.252
=== Shrinks ===
0.8.252
30.8.252
45.8.252
53.8.252
57.8.252
59.8.252
60.0.252
60.4.252
60.6.252
60.7.252
60.8.0
60.8.126
60.8.189
60.8.221
60.8.237
60.8.245
60.8.249
60.8.251
.
=== Outcome ===
238.151.174
=== Shrinks ===
0.151.174
119.151.174
179.151.174
209.151.174
224.151.174
231.151.174
235.151.174
237.151.174
238.0.174
238.76.174
238.114.174
238.133.174
238.142.174
238.147.174
238.149.174
238.150.174
238.151.0
238.151.87
238.151.131
238.151.153
238.151.164
238.151.169
238.151.172
238.151.173
.
=== Outcome ===
122.72.39
=== Shrinks ===
0.72.39
61.72.39
92.72.39
107.72.39
115.72.39
119.72.39
121.72.39
122.0.39
122.36.39
122.54.39
122.63.39
122.68.39
122.70.39
122.71.39
122.72.0
122.72.20
122.72.30
122.72.35
122.72.37
122.72.38
.
=== Outcome ===
9.176.80
=== Shrinks ===
0.176.80
5.176.80
7.176.80
8.176.80
9.0.80
9.88.80
9.132.80
9.154.80
9.165.80
9.171.80
9.174.80
9.175.80
9.176.0
9.176.40
9.176.60
9.176.70
9.176.75
9.176.78
9.176.79
.
=== Outcome ===
233.193.86
=== Shrinks ===
0.193.86
117.193.86
175.193.86
204.193.86
219.193.86
226.193.86
230.193.86
232.193.86
233.0.86
233.97.86
233.145.86
233.169.86
233.181.86
233.187.86
233.190.86
233.192.86
233.193.0
233.193.43
233.193.65
233.193.76
233.193.81
233.193.84
233.193.85
.
```

#### 👉 Integrated shrinking is an important quality of Hedgehog

When a property fails (because Hedgehog found a counter-example), the randomly-generated data usually contains "noise". Therefore Hedgehog simplifies counter-examples before reporting them:

```fs
let version =
    Range.constantBounded ()
    |> Gen.byte
    |> Gen.map int
    |> Gen.tuple3
    |> Gen.map (fun (ma, mi, bu) -> Version (ma, mi, bu))

Property.print <| property {
    let! xs = Gen.list (Range.linear 0 100) version
    return xs |> List.rev = xs
    }

>
*** Failed! Falsifiable (after 3 tests and 6 shrinks):
[0.0.0; 0.0.1]
```

The above example, is the standard "hello-world" property, but instead of the classic list of integers, we're using a list of type System.Version, demonstrating that integrated shrinking works with 'foreign' types too.

---

*As a matter of fact, here's the above example written using another property-based testing system, FsCheck:*

```fs
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

### Generators

Hedgehog's `Gen` module exports some basic generators and plenty combinators for making new generators. Here's a generator of alphanumeric chatracters:

```fs
Gen.alphaNum
```

This generator is of type `Gen<char>`, which means that Hedgehog can take this generator and produce characters, like so:

```fs
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

#### 👉 Generators can also be created using the `gen` expression

Hedgehog supports a convenient syntax for working with generators through the `gen` expression. Here's a way to define a generator of type System.Net.IPAddress:

```fs
open System.Net

let ipAddressGen : Gen<IPAddress> =
    gen {
        let! addr = Gen.array (Range.constant 4 4) (Gen.byte <| Range.constantBounded())
        return System.Net.IPAddress addr
    }

ipAddressGen |> Gen.printSample;;

=== Outcome ===
45.230.61.78
=== Shrinks ===
0.230.61.78
23.230.61.78
34.230.61.78
40.230.61.78
43.230.61.78
44.230.61.78
45.0.61.78
45.115.61.78
45.173.61.78
45.202.61.78
45.216.61.78
45.223.61.78
45.227.61.78
45.229.61.78
45.230.0.78
45.230.31.78
45.230.46.78
45.230.54.78
45.230.58.78
45.230.60.78
45.230.61.0
45.230.61.39
45.230.61.59
45.230.61.69
45.230.61.74
45.230.61.76
45.230.61.77
.
=== Outcome ===
203.224.13.253
=== Shrinks ===
0.224.13.253
102.224.13.253
153.224.13.253
178.224.13.253
191.224.13.253
197.224.13.253
200.224.13.253
202.224.13.253
203.0.13.253
203.112.13.253
203.168.13.253
203.196.13.253
203.210.13.253
203.217.13.253
203.221.13.253
203.223.13.253
203.224.0.253
203.224.7.253
203.224.10.253
203.224.12.253
203.224.13.0
203.224.13.127
203.224.13.190
203.224.13.222
203.224.13.238
203.224.13.246
203.224.13.250
203.224.13.252
.
=== Outcome ===
73.112.249.182
=== Shrinks ===
0.112.249.182
37.112.249.182
55.112.249.182
64.112.249.182
69.112.249.182
71.112.249.182
72.112.249.182
73.0.249.182
73.56.249.182
73.84.249.182
73.98.249.182
73.105.249.182
73.109.249.182
73.111.249.182
73.112.0.182
73.112.125.182
73.112.187.182
73.112.218.182
73.112.234.182
73.112.242.182
73.112.246.182
73.112.248.182
73.112.249.0
73.112.249.91
73.112.249.137
73.112.249.160
73.112.249.171
73.112.249.177
73.112.249.180
73.112.249.181
.
=== Outcome ===
202.71.39.27
=== Shrinks ===
0.71.39.27
101.71.39.27
152.71.39.27
177.71.39.27
190.71.39.27
196.71.39.27
199.71.39.27
201.71.39.27
202.0.39.27
202.36.39.27
202.54.39.27
202.63.39.27
202.67.39.27
202.69.39.27
202.70.39.27
202.71.0.27
202.71.20.27
202.71.30.27
202.71.35.27
202.71.37.27
202.71.38.27
202.71.39.0
202.71.39.14
202.71.39.21
202.71.39.24
202.71.39.26
.
=== Outcome ===
244.251.46.14
=== Shrinks ===
0.251.46.14
122.251.46.14
183.251.46.14
214.251.46.14
229.251.46.14
237.251.46.14
241.251.46.14
243.251.46.14
244.0.46.14
244.126.46.14
244.189.46.14
244.220.46.14
244.236.46.14
244.244.46.14
244.248.46.14
244.250.46.14
244.251.0.14
244.251.23.14
244.251.35.14
244.251.41.14
244.251.44.14
244.251.45.14
244.251.46.0
244.251.46.7
244.251.46.11
244.251.46.13
.
```

### Properties

Using Hedgehog, the programmer writes assertions about logical properties that a function should fulfill.

Take [`List.rev`](https://msdn.microsoft.com/visualfsharpdocs/conceptual/list.rev%5b%27t%5d-function-%5bfsharp%5d) as an example, which is a function that returns a new list with the elements in reverse order:

```fs
List.rev [1; 2; 3];;

val it : int list = [3; 2; 1]
```

One logical property of `List.rev` is:

* Calling `List.rev` twice must return the elements in the original order.

Here's an example assertion:

```fs
List.rev (List.rev [1; 2; 3]) = [1; 2; 3];;

val it : bool = true
```

#### A generic assertion

In the previous example `List.rev` was tested against an example value `[1; 2; 3]`. To make the assertion generic, the example value can be parameterized as *any list*:

```fs
fun xs -> List.rev (List.rev xs) = xs;;

val it : xs:'a list -> bool when 'a : equality = <fun:clo@33>
```

Hedgehog will then attempt to generate a test case that *falsifies* the assertion. In order to do that, it needs to know which generator to use, to feed `xs` with random values.

#### A generator for lists of integers

Values for `xs` need to be generated by a generator, as shown in the *Generators* sections. The following one is for lists of type integer:

```fs
let g = Gen.list (Range.linear 0 100) Gen.alpha;;

val g : Gen<char list>
```

Every possible value generated by the `g` generator must now be supplied to the assertion, as shown below:

#### A first property

```fs
fun xs ->
    List.rev (List.rev xs) = xs
    |> Property.ofBool
|> Property.forAll g;;

val it : Property<unit>
```

>But what is `forAll`? This comes from [predicate logic](https://en.wikipedia.org/wiki/Universal_quantification) and essentially means that the assertion holds *for all* possible values generated by `g`.

#### 👉 Properties can also be created using the `property` expression

Here's how the previous property can be rewritten:

```fs
property {
    let! xs = g
    return List.rev (List.rev xs) = xs
}
```

#### Try out (see it pass)

```fs
let g = Gen.list (Range.linear 0 100) Gen.alpha

property {
    let! xs = g
    return List.rev (List.rev xs) = xs
}
|> Property.print' 500<tests>;;

>
+++ OK, passed 500 tests.

```

The above property was exercised 500 times. The default is 100, which is what `Property.print` does:

```fs
let g = Gen.list (Range.linear 0 100) Gen.alpha

property {
    let! xs = g
    return List.rev (List.rev xs) = xs
}
|> Property.print

>
+++ OK, passed 100 tests.

```

>Outside of F# Interactive, you might want to use `Property.check` or `Property.check'`, specially if you're using Unquote with xUnit, NUnit, MSTest, or similar.

#### Try out (see it fail)

```fs
let tryAdd a b =
    if a > 100 then None // Nasty bug.
    else Some (a + b)

property { let! a = Gen.int <| Range.constantBounded ()
           let! b = Gen.int <| Range.constantBounded ()
           return tryAdd a b = Some (a + b) }
|> Property.print;;

>
*** Failed! Falsifiable (after 3 tests and 24 shrinks):
101

```

The test now fails. — Notice how Hedgehog reports back the minimal counter-example. This process is called shrinking.

### Custom Operations

The `Property` module and its `property` expression supports a few [custom operations](https://docs.microsoft.com/en-us/dotnet/articles/fsharp/language-reference/computation-expressions#custom-operations) as well.

#### `counterexample`

Here's how the previous example could be written in order to carry along a friendlier message when it fails:

```fs
let tryAdd a b =
    if a > 100 then None // Nasty bug.
    else Some(a + b)

property { let! a = Gen.int <| Range.constantBounded ()
           let! b = Gen.int <| Range.constantBounded ()
           counterexample (sprintf "The value of a was %d." a)
           return tryAdd a b = Some(a + b) }
|> Property.print;;

>
*** Failed! Falsifiable (after 16 tests and 5 shrinks):
101
The value of a was 101.

```

#### `where`

Here’s how the previous example could be written so that in never fails:

```fs
let tryAdd a b =
    if a > 100 then None // Nasty bug.
    else Some(a + b)

property { let! a = Gen.int <| Range.constantBounded ()
           let! b = Gen.int <| Range.constantBounded ()
           where (a < 100)
           return tryAdd a b = Some(a + b) }
|> Property.print;;

>
*** Gave up after 100 discards, passed 95 tests.

```

Essentially, the `where` custom operation discards test cases which do not satisfy the given condition.

Test case generation continues until 100 cases (the default of `Property.print`) which do satisfy the condition have been found, or until an overall limit on the number of test cases is reached (to avoid looping if the condition never holds).

In this case a message such as

```
Gave up after 100 discards, passed 95 tests.
```

indicates that 95 test cases satisfying the condition were found, and that the property held in those 95 cases.

## Integrations

Use your favorite tools with Hedgehog.

Powerful integrations that help you and your team build properties in an easier way.

### Regex-constrained strings

In Haskell, there's the [quickcheck-regex](https://hackage.haskell.org/package/quickcheck-regex) package, by [Audrey (唐鳳) Tang](https://www.linkedin.com/in/tangaudrey), which allows to write and execute this:

```hs
generate (matching "[xX][0-9a-z]")
// Prints -> "''UVBw"
```

It exports a `matching` function that turns a Regular Expression into a [DFA](https://en.wikipedia.org/wiki/Deterministic_finite_automaton)/[NFA](https://en.wikipedia.org/wiki/Nondeterministic_finite_automaton) finite-state machine and then into a generator of strings matching that regex:

```hs
matching :: String -> Gen String
```

A similar generator in F# with Hedgehog can be written as shown below:

```fs
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


```fs
let pattern = "^http\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(/\S*)?$"

Property.print <| property { let! s = fromRegex pattern
                             return matches s pattern }

+++ OK, passed 100 tests.

```
