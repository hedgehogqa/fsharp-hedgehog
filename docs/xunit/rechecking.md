# Rechecking Failures

When a property fails, Hedgehog provides recheck data that allows you to reproduce the exact failure. This is invaluable for debugging and creating regression tests.

## Using the Recheck Attribute

When a property test fails, Hedgehog outputs recheck data that you can use to reproduce the failure:

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Xunit

[<Property>]
let ``might fail`` (x: int) =
    x < 1000

// When the above fails, Hedgehog outputs:
// *** Failed! Falsifiable (after 45 tests and 3 shrinks):
// 1000
// This failure can be reproduced by running:
// > Property.recheck "0_16700074754810023652_2867022503662193831_"

// Use the recheck string to reproduce the exact failure:
[<Property>]
[<Recheck("0_16700074754810023652_2867022503662193831_")>]
let ``reproduce the failure`` (x: int) =
    x < 1000
```

# [C#](#tab/csharp)

```csharp
using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;

[Property]
public bool MightFail(int x)
{
    return x < 1000;
}

// When the above fails, Hedgehog outputs:
// *** Failed! Falsifiable (after 45 tests and 3 shrinks):
// 1000
// This failure can be reproduced by running:
// > Property.recheck "0_16700074754810023652_2867022503662193831_"

// Use the recheck string to reproduce the exact failure:
[Property]
[Recheck("0_16700074754810023652_2867022503662193831_")]
public bool ReproduceTheFailure(int x)
{
    return x < 1000;
}
```

---

The `Recheck` attribute runs the test exactly once with the specific seed and shrink path that caused the original failure, making it a perfect regression test.
