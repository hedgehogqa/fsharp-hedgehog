[![Build Status](https://travis-ci.org/hedgehogqa/dotnet-hedgehog.svg?branch=master)](https://travis-ci.org/hedgehogqa/dotnet-hedgehog)

# dotnet-hedgehog

[Hedgehog](http://hedgehog.qa/) is a modern property-based testing system, in the spirit of QuickCheck. Hedgehog offers a simplified model for writing properies with not only `gen` but also `property` expressions. Hedgehog does shrinking automatically, which means it can shrink anything it can generate. Hedgehog has adequate randomness based on the SplitMix algorithm.

![](https://github.com/hedgehogqa/dotnet-hedgehog/raw/master/img/dice.jpg)

## Installing

To install Hedgehog, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console)

```
PM> Install-Package Hedgehog
```

## Contribute

There are many ways to [contribute](CONTRIBUTING.md) to Hedgehog.
* [Submit bugs](https://github.com/hedgehogqa/dotnet-hedgehog/issues) and help us verify fixes as they are checked in.
* Review the [source code changes](https://github.com/hedgehogqa/dotnet-hedgehog/pulls).
* Engage with other Hedgehog users and developers on [StackOverflow](http://stackoverflow.com/questions/tagged/hedgehogqa).
* Join the [#hedgehogqa](http://twitter.com/#!/search/realtime/%23hedgehogqa) discussion on Twitter.
* [Contribute bug fixes](CONTRIBUTING.md).

This project has adopted the [Code of Conduct for Open Source Projects](http://contributor-covenant.org/). You can view and download the latest version of the Contributor Covenant [here](http://contributor-covenant.org/version/1/4/code_of_conduct.txt).

## Documentation

*  [Quick tutorial](doc/TUTORIAL.md)

## Building

In order to build dotnet-hedgehog, ensure that you have [Git](http://git-scm.com/downloads) and [F#](http://fsharp.org/) installed.

Clone a copy of the repo:

```
git clone https://github.com/hedgehogqa/dotnet-hedgehog.git
```

Change to the dotnet-hedgehog directory:

```
cd dotnet-hedgehog
```

Use one of the following to build and test:

```
./build.sh Build      # Build Hedgehog into src/Hedgehog/bin/Release
./build.sh Test       # Run tests using the xUnit.net console runner
./build.sh NuGet      # Create a NuGet Package into .nuget directory
```


## Usage

```f#
property {
    let! xs = g
    return List.rev (List.rev xs) = xs
}
```

where `g` is either a built-in generator or a custom one. See the [quick tutorial](doc/TUTORIAL.md) for some examples.

## Versioning

Hedgehog follows [Semantic Versioning 2.0.0](http://semver.org/spec/v2.0.0.html).

According to [semantic versioning specification](http://semver.org/spec/v2.0.0.html#spec-item-4
), until version 1.0.0 is released, major version zero (0.y.z) is for initial development. Anything may change at any time. The public API should not be considered stable.

## Limitations

Some of the features you'd expect from a property-based testing system are still missing, but we'll get there eventually:

* Generating functions
* Model-based testing

Hedgehog doesn't have an Arbitrary type class, by design. The main purpose of the Arbitrary class is to link the generator with a shrink function — this isn't required with Hedgehog so Arbitrary has been eliminated.