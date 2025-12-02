<p align="center">
  <img width="320"
       src="https://github.com/hedgehogqa/haskell-hedgehog/raw/master/img/hedgehog-text-logo.png">
</p>

# Hedgehog for .NET

[![NuGet][nuget-shield]][nuget] [![Build][github-shield]][github-ci]

Hedgehog explores a wide range of inputs and shrinks failures to minimal,
valid examples. It works with any .NET test framework and can be used
from F#, C#, or other .NET languages.

## Features

- Shrinking that preserves invariants by construction.
- `gen` and `property` expressions for concise generators and properties.
- Range combinators for controlled numeric and collection generation.
- Composable generators for structured and recursive data types.
- Deterministic runs via explicit seed control.
- Shrink trees available for debugging.
- and additional helpers for complex data generation.

---

Docs: https://hedgehogqa.github.io/fsharp-hedgehog/

[nuget-shield]: https://img.shields.io/nuget/v/Hedgehog.svg
[nuget]: https://www.nuget.org/packages/Hedgehog

[github-shield]: https://github.com/hedgehogqa/fsharp-hedgehog/actions/workflows/master.yml/badge.svg
[github-ci]: https://github.com/hed
