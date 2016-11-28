[![Build Status](https://travis-ci.org/jystic/dotnet-jack.svg?branch=master)](https://travis-ci.org/jystic/dotnet-jack)

# dotnet-jack

A modern property-based testing tool, in the spirit of John Hughes & Koen Classen's [QuickCheck](https://web.archive.org/web/20160319204559/http://www.cs.tufts.edu/~nr/cs257/archive/john-hughes/quick.pdf). The key improvement is that shrinking comes for free.

![](https://github.com/moodmosaic/dotnet-jack/raw/master/img/dice.jpg)

*Jack's love of dice has brought him here, where he has taken on the form of an F# library, in order to help you gamble with your properties.*

## Highlights

* TODO
* TODO
* TODO

## At a glance

`dotnet-jack` is a lightweight, but powerful, property-based testing tool. The key improvement being that shrinking is baked in to the `Gen` monad, so you get it for free.

### Getting Started

The standard "hello-world" property shown in most property-based testing tools is:

```
reverse (reverse xs) = xs, ∀xs :: [α]
```

which means that "the reverse of the reverse of a list, is the list itself - for all lists of type `a`".

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
val it : unit = ()
>
```

### Generators

TODO

### Properties

TODO

## Limitations

TODO

## NuGet

There isn't much here yet, but `dotnet-jack` can be published on NuGet once it's in a usable state.

## Versioning

`dotnet-jack` follows [Semantic Versioning 2.0.0](http://semver.org/spec/v2.0.0.html) once it’s in a usable state.

## Credits

The idea behind `dotnet-jack` originates from [`purescript-jack`](https://github.com/jystic/purescript-jack/) in PureScript and [`disorder-jack`](https://github.com/ambiata/disorder.hs/tree/master/disorder-jack/) in Haskell.
