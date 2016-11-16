[![Build Status](https://travis-ci.org/jystic/dotnet-jack.svg?branch=master)](https://travis-ci.org/jystic/dotnet-jack)

# dotnet-jack

*Jack's love of dice has brought him here, where he has taken on the form of a Haskell library, in order to help you gamble with your properties.*

![](img/dice.jpg)

Jack is a modern testing library, in the spirit of John Hughes & Koen Classen's Haskell [QuickCheck](https://web.archive.org/web/20160319204559/http://www.cs.tufts.edu/~nr/cs257/archive/john-hughes/quick.pdf).

What makes Jack different from Haskell QuickCheck is that instead of generating a random value and using a shrinking function after the fact, Jack generates the random value and all the possible shrinks in a rose tree, all at once. Shrinking is baked in the `Gen` computation expression, so you get it for free.

There isn't much here yet, but Jack can be published on NuGet once it's in a usable state.
