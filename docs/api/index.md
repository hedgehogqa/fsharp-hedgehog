# API Documentation

### For F# Users

To use Hedgehog in F#, import the following namespaces:

```fsharp
open Hedgehog
open Hedgehog.FSharp
```

- [Hedgehog.FSharp.Gen](xref:Hedgehog.FSharp.Gen)
- [Hedgehog.FSharp.Property](xref:Hedgehog.FSharp.Property)
- [Hedgehog.FSharp.Range](xref:Hedgehog.FSharp.Range)

### For C# Users

To use Hedgehog in C#, import the following namespaces:

```csharp
using Hedgehog;
using Hedgehog.Linq;
```

- [Hedgehog.Linq.Gen](xref:Hedgehog.Linq.Gen)
- [Hedgehog.Linq.Property](xref:Hedgehog.Linq.Property)
- [Hedgehog.Linq.Range](xref:Hedgehog.Linq.Range)

## Core Types

The most important types in Hedgehog are:

- [Gen&lt;T&gt;](xref:Hedgehog.Gen_a_) - A generator for random values of type `T`. Generators can be composed and transformed to create complex data generators.

- [Range&lt;T&gt;](xref:Hedgehog.Range_a_) - Defines the range from which values are generated and controls shrinking behavior during property testing.

- [Property&lt;T&gt;](xref:Hedgehog.Property_a_) - Represents a property to be tested. Properties combine generators with assertions to verify that certain conditions hold for all generated test cases.
