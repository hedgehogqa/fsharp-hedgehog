fsharp-hedgehog [![NuGet][nuget-shield]][nuget] [![Travis][travis-shield]][travis]
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

More examples can be found in the [tutorial](doc/tutorial.md).

## Building from source

To build Hedgehog from source, you will need either the
[.NET Core SDK or Visual Studio][net-core-sdk].

### Linux-specific

If you are using Linux you will also need Mono installed
(in order to run Paket). The full install sequence (for Ubuntu)
will be something like [this][ubuntu-steps].

### Building & running tests

With Visual Studio you can build Hedgehog and run the tests
from inside the IDE, otherwise with the `dotnet` command-line
tool you can execute:

```sh
dotnet build
```

The first time you run it, this will use Paket to restore all
the packages, and then build the code.

To run the tests, you can execute:

```sh
dotnet test tests/Hedgehog.Tests/Hedgehog.Tests.fsproj
dotnet test tests/Hedgehog.CSharp.Tests/Hedgehog.CSharp.Tests.csproj
```

### Building the NuGet package

After building the source (for *release* configuration, i.e.
`dotnet build -c Release`), you can produce the NuGet package with
Paket:

```sh
.paket/paket.exe pack src/Hedgehog
```

This will produce `Hedgehog-x.y.z.w.nupkg` in `src/Hedgehog`.

 [nuget]: https://www.nuget.org/packages/Hedgehog/
 [nuget-shield]: https://img.shields.io/nuget/dt/Hedgehog.svg?style=flat

 [travis]: https://travis-ci.org/hedgehogqa/fsharp-hedgehog
 [travis-shield]: https://travis-ci.org/hedgehogqa/fsharp-hedgehog.svg?branch=master

 [net-core-sdk]: https://www.microsoft.com/net/download/
 [ubuntu-steps]: https://github.com/hedgehogqa/fsharp-hedgehog/pull/153#issuecomment-364325504
