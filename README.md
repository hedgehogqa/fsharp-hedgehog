fsharp-hedgehog [![NuGet][nuget-shield]][nuget] [![Travis][travis-shield]][travis]
========

> Hedgehog will eat all your bugs.

<img src="https://github.com/hedgehogqa/fsharp-hedgehog/raw/master/img/SQUARE_hedgehog_615x615.png" width="307" align="right"/>

[Hedgehog](http://hedgehog.qa/) is a modern property-based testing
system, in the spirit of QuickCheck. Hedgehog uses integrated shrinking,
so shrinks obey the invariants of generated values by construction.

## Features

- Integrated shrinking, shrinks obey invariants by construction.
- Convenient syntax for generators and properties with `gen` and `property` expressions.
- Range combinators for full control over the scope of generated numbers and collections.

## Example

The root namespace, `Hedgehog`, includes almost
everything you need to get started writing property tests with Hedgehog.

```fs
open Hedgehog
```

Once you have your import declaration set up, you can write a simple property:

```fs
let propReverse : Property<Unit> =
    property {
        let! xs = Gen.list (Range.linear 0 100) Gen.alpha
        return xs |> List.rev |> List.rev = xs
        }
```

You can then load the module in F# Interactive, and run it:

```
> Property.print propReverse
+++ OK, passed 100 tests.

```

 [nuget]: https://www.nuget.org/packages/Hedgehog/
 [nuget-shield]: https://img.shields.io/nuget/dt/Hedgehog.svg?style=flat

 [travis]: https://travis-ci.org/hedgehogqa/fsharp-hedgehog
 [travis-shield]: https://travis-ci.org/hedgehogqa/fsharp-hedgehog.svg?branch=master
