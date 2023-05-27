# Eval.cs

Mathematical expressions evaluator that supports math and enumerable functions, using only `System`,
from the lexer to the parser with no bizarre regex.

## Install

```console
dotnet add package Eval.cs
```

## Usage

```csharp
using System;
using Eval;

// csharp prefixes can be omitted and lower cased
Console.WriteLine(Evaluator.Evaluate("-2 + pi - Ceiling(3.2)"));

// Everything is called like functions
Console.WriteLine(Evaluator.Evaluate("IEnumerable.Average(2, 3, 5)"));

Console.WriteLine(Evaluator.Evaluate("pow(-average(2, 3, 5), -5)"));
Console.WriteLine(Evaluator.Evaluate("19e-11 /- 12");
Console.WriteLine(Evaluator.Evaluate("last(4, last(1, 2), 5)");
Console.WriteLine(Evaluator.Evaluate("921.315D * -20.93D % 34.567D");
Console.WriteLine(Evaluator.Evaluate(" 9>>3  /+ 1.2");
```

For even weirder examples check my tests
see [./EvalTest/Program.cs](./EvalTest/Program.cs)

## [ExprGen](./ExprGen/Program.cs)

Generates simple math expressions, only binary operations.

## [EvalTest](./EvalTest/Program.cs)

Code to test the lexer, evaluation and exceptions.
