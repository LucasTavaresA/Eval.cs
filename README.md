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
Console.WriteLine(Evaluator.Evaluate("921.315 * -20.93 % 34.567");
Console.WriteLine(Evaluator.Evaluate(" 9>>3  /+ 1.2");
```

## Exceptions

There are two custom exceptions, catch the rest as general exceptions that have a message

### InvalidExpressionException

Means that the passed string is not a valid expression

| Type     | Property  | Description                                                                            |
|:--------:|:---------:|:---------------------------------------------------------------------------------------|
| `string` | `Src`     | the expression                                                                         |
| `int`    | `Offset`  | the offset before the error, specialy useful for pointing where the error has happened |
| `int`    | `Length`  | the length of the wrong value or function                                              |
| `string` | `Message` | the cause of the exception, eg: "Closing unexsistent paren"                            |

### ArgumentAmountException

Means that the wrong of arguments was passed to a function

| Type     | Property   | Description                                                                        |
|:--------:|:----------:|:-----------------------------------------------------------------------------------|
| `string` | `Src`      | same as InvalidExpressionException                                                 |
| `int`    | `Offset`   | same as InvalidExpressionException                                                 |
| `int`    | `Length`   | same as InvalidExpressionException                                                 |
| `string` | `Message`  | the message:  $"`Function`() expects `Expected` arguments but received `Received`" |
| `string` | `Function` | name of the failed function                                                        |
| `int`    | `Expected` | expected amount of arguments                                                       |
| `int`    | `Received` | received amount of arguments                                                       |

### UnexpectedEvaluationException

Should not happen unless there is a logical problem with this evaluator

Throw like an general Exception that has a message

### Example

A text based exception

```console
pow() expects 2 arguments but received 3
Math.Pow(1, 2, 3) - 4
^~~~~~~~~~~~~~~~^
```

```csharp
using Eval;
using System;

static string ErrorMsg(string message, string src, int offset, int length)
{
    if (length < 1)
    {
        return $"{message}";
    }

    var marker = new string(' ', offset);

    if (length == 1)
    {
        marker += "^";
    }
    else if (length == 2)
    {
        marker += "^^";
    }
    else if (length > 2)
    {
        marker += $"^{new('~', length - 2)}^";
    }

    return $"{message}\n{src}\n{marker}";
}

try
{
    var result = Evaluator.Evaluate("Math.Pow(1, 2, 3) - 4");
}
catch (Exception e)
{
    Console.WriteLine(
        e switch
        {
            InvalidExpressionException ie => ErrorMsg(ie.Message, ie.Src, ie.Offset, ie.Length),
            ArgumentAmountException ae => ErrorMsg(ae.Message, ae.Src, ae.Offset, ae.Length),
            _ => e.Message
        }
    );
}
```

## [ExprGen](./ExprGen/Program.cs)

Generates simple math expressions, does not generate functions and parens.

## [EvalTest](./EvalTest/Program.cs)

Code to test the lexer, evaluation and exceptions.
