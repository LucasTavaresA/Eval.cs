# Eval.cs

**Work in progress**

Mathematical expressions evaluator that supports `Math.*` and `IEnumerable.*()`, using only `System.*`,
from the lexer to the parser with no bizarre regex.

## Install

```console
dotnet add package Eval.cs
```

## Usage

```csharp
using System;
using Eval;

// you do variables and Math functions
Console.WriteLine(Evaluator.Evaluate("-2 + Math.PI - Math.Ceiling(3.2)"));
// -2.85840734641021                                 ^ 4

// Everything is called like functions
Console.WriteLine(Evaluator.Evaluate("IEnumerable.Average(2, 3, 5)"));
// 3.33333333333333

// Can even do weird things like this
Console.WriteLine(Evaluator.Evaluate("IEnumerable.Max(2, 35, 5)"));
// 35
```
