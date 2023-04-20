# Eval.cs

**Work in progress**

Evaluates mathematical expressions

Using only `System`, from the lexer to the parser with no bizarre regex

Receives only strings, but supports `Math` variables like `Math.PI`

## Usage

```csharp
using Eval;

Eval("2 + Math.PI");
```
