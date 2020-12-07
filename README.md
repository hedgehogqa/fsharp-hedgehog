fsharp-hedgehog [![NuGet][nuget-shield]][nuget] ![](https://github.com/hedgehogqa/fsharp-hedgehog/workflows/.NET%20Core/badge.svg)
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

👉 For auto-generators (à la AutoFixture) and other convenience generators, check out [fsharp-hedgehog-experimental](https://github.com/cmeeren/fsharp-hedgehog-experimental/).

## Building from source

To build Hedgehog from source, you will need either the
[.NET Core SDK or Visual Studio][net-core-sdk].

### Building & running tests

With Visual Studio you can build Hedgehog and run the tests
from inside the IDE, otherwise with the `dotnet` command-line
tool you can execute:

```sh
dotnet build
```

To run the tests, you can execute:

```sh
dotnet test tests/Hedgehog.Tests/Hedgehog.Tests.fsproj
dotnet test tests/Hedgehog.CSharp.Tests/Hedgehog.CSharp.Tests.csproj
```

### Building the NuGet package

```sh
dotnet pack src/Hedgehog/Hedgehog.fsproj -c Release
```

This will produce `Hedgehog-x.y.z.nupkg` in `src/Hedgehog/bin/Release`.

 [nuget]: https://www.nuget.org/packages/Hedgehog/
 [nuget-shield]: https://img.shields.io/nuget/dt/Hedgehog.svg?style=flat

 [travis]: https://travis-ci.org/hedgehogqa/fsharp-hedgehog
 [travis-shield]: https://travis-ci.org/hedgehogqa/fsharp-hedgehog.svg?branch=master

 [net-core-sdk]: https://www.microsoft.com/net/download/
 [ubuntu-steps]: https://github.com/hedgehogqa/fsharp-hedgehog/pull/153#issuecomment-364325504


## Development Environments

### Visual Studio

Hedgehog can be developed in Visual Studio, once these requirements are met:

- .NET Core (2.1+) or .NET Framework (4.5.1+) are installed.

### Visual Studio Code

Hedgehog can be developed in Visual Studio Code, with a couple of scenarios supported.

#### Bare Metal

Developing on bare metal is essentially the same as developing in Visual Studio, install the appropriate .NET SDK then open the project folder.

#### Remote Containers

Development can also be done using [Remote Development][remote-containers-doc].

The steps to get this up and running are as follows:

1. Install [Docker][docker-install].
2. Install the [Remote Containers][remote-containers-ext] extension.
3. Open the project folder, then run the `Remote-Containers: Reopen in Container` command.

[remote-containers-doc]: https://code.visualstudio.com/docs/remote/containers
[remote-containers-ext]: https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers
[docker-install]: https://docs.docker.com/get-docker/
