fsharp-hedgehog [![NuGet][nuget-shield]][nuget] ![](https://github.com/hedgehogqa/fsharp-hedgehog/workflows/master/badge.svg)
========

> Hedgehog will eat all your bugs.

<img src="https://github.com/hedgehogqa/fsharp-hedgehog/raw/master/img/hedgehog-logo.png" width="307" align="right"/>

[Hedgehog](http://hedgehog.qa/) is a modern property-based testing
system, in the spirit of QuickCheck. Hedgehog uses integrated shrinking,
so shrinks obey the invariants of generated values by construction.

## Features

- Integrated shrinking, shrinks obey invariants by construction.
- Convenient syntax for generators and properties with `gen` and `property` expressions.
- Range combinators for full control over the scope of generated numbers and collections.

## Documentation

- [Why Hedgehog?!](https://hedgehogqa.github.io/fsharp-hedgehog)
- [Getting Started](https://hedgehogqa.github.io/fsharp-hedgehog/articles/getting-started.html)
- [Best Practices](https://hedgehogqa.github.io/fsharp-hedgehog/articles/best-practices.html)