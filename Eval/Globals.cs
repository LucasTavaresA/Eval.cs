// Licensed under the LGPL3 license.
// See the LICENSE file in the project root for more information.
#pragma warning disable CA1716

using System;
using System.Collections.Generic;
using System.Linq;

namespace Eval;

public readonly struct Globals
{
    public static readonly Delegate Negative = (double val) => -val;

    public readonly record struct Token(TokenKind Kind, string Literal)
    {
        public override string ToString() => Literal;
    };

    public readonly record struct Operator(
        int Precedence,
        Func<double, double, double> Operation
    );

    public struct Function
    {
        public string Name { get; set; }
        public Delegate Funcall { get; set; }
        public int Args { get; set; }
        public int Offset { get; set; }

        public Function(string name, int argAmount, Delegate funcall)
        {
            Name = name;
            Args = argAmount;
            Funcall = funcall;
        }

        public override readonly string ToString()
        {
            return Name;
        }
    }

    public enum TokenKind
    {
        // operators
        Plus,
        Minus,
        Multiply,
        Divide,
        Percentage,
        Exponent,
        ShiftLeft,
        ShiftRight,

        Number = 8,
        Variable,
        Function,

        // control flow
        OpenParen,
        CloseParen,
        Comma,
        End,
        Illegal,
    }

    // csharpier-ignore-start
    public static readonly Dictionary<TokenKind, Operator> Operators =
        new()
        {
            { TokenKind.Plus,       new(1, (double left, double right) => left + right) },
            { TokenKind.Minus,      new(1, (double left, double right) => left - right) },
            { TokenKind.Multiply,   new(2, (double left, double right) => left * right) },
            { TokenKind.Divide,     new(2, (double left, double right) => left / right) },
            { TokenKind.Percentage, new(2, (double left, double right) => left * (right / 100)) },
            { TokenKind.Exponent,   new(3, Math.Pow) },
            { TokenKind.ShiftLeft,  new(0, (double left, double right) => (int)left << (int)right) },
            { TokenKind.ShiftRight, new(0, (double left, double right) => (int)left >> (int)right) },
        };

    public static readonly Dictionary<string, TokenKind> Tokens =
        new()
        {
            { "+",  TokenKind.Plus },
            { "++", TokenKind.Plus },
            { "-",  TokenKind.Minus },
            { "+-", TokenKind.Minus },
            { "--", TokenKind.Minus },
            { "-+", TokenKind.Minus },
            { "*",  TokenKind.Multiply },
            { "*+", TokenKind.Multiply },
            { "/",  TokenKind.Divide },
            { "/+", TokenKind.Divide },
            { "%",  TokenKind.Percentage },
            { "%+", TokenKind.Percentage },
            { "^",  TokenKind.Exponent },
            { "<<", TokenKind.ShiftLeft },
            { ">>", TokenKind.ShiftRight },
            { "(",  TokenKind.OpenParen },
            { ")",  TokenKind.CloseParen },
            { ",",  TokenKind.Comma },
            { "\0", TokenKind.End },
        };

    public static readonly Dictionary<string, double> Variables =
        new()
        {
            { "pi",  Math.PI },
            { "e",   Math.E },
            { "tau", Math.Tau },
        };

    public static readonly Dictionary<string, Function> Functions =
        new()
        {
            { "average",          new("average",          0, (double[] args) => args.Average()) },
            { "max",              new("max",              0, (double[] args) => args.Max()) },
            { "min",              new("min",              0, (double[] args) => args.Min()) },
            { "sum",              new("sum",              0, (double[] args) => args.Sum()) },
            { "last",             new("last",             0, (double[] args) => args.Last()) },
            { "length",           new("length",           0, (double[] args) => args.Length) },
            { "count",            new("count",            0, (double[] args) => args.Length) },
            { "first",            new("first",            0, (double[] args) => args.First()) },
            { "single",           new("single",           0, (double[] args) => args.Single()) },
            { "gethashcode",      new("gethashcode",      0, (double[] args) => args.GetHashCode()) },
            { "singleordefault",  new("singleordefault",  0, (double[] args) => args.SingleOrDefault()) },
            { "firstordefault",   new("firstordefault",   0, (double[] args) => args.FirstOrDefault()) },
            { "lastordefault",    new("lastordefault",    0, (double[] args) => args.LastOrDefault()) },
            { "abs",              new("abs",              1, (double arg) => Math.Abs(arg)) },
            { "ceiling",          new("ceiling",          1, (double arg) => Math.Ceiling(arg)) },
            { "floor",            new("floor",            1, (double arg) => Math.Floor(arg)) },
            { "log",              new("log",              1, (double arg) => Math.Log(arg)) },
            { "round",            new("round",            1, (double arg) => Math.Round(arg)) },
            { "truncate",         new("truncate",         1, (double arg) => Math.Truncate(arg)) },
            { "mod",              new("mod",              2, (double x, double y) => x % y) },
            { "acos",             new("acos",             1, Math.Acos) },
            { "acosh",            new("acosh",            1, Math.Acosh) },
            { "asin",             new("asin",             1, Math.Asin) },
            { "asinh",            new("asinh",            1, Math.Asinh) },
            { "atan",             new("atan",             1, Math.Atan) },
            { "atan2",            new("atan2",            2, Math.Atan2) },
            { "atanh",            new("atanh",            1, Math.Atanh) },
            { "bitdecrement",     new("bitdecrement",     1, Math.BitDecrement) },
            { "bitincrement",     new("bitincrement",     1, Math.BitIncrement) },
            { "cbrt",             new("cbrt",             1, Math.Cbrt) },
            { "copysign",         new("copysign",         2, Math.CopySign) },
            { "cos",              new("cos",              1, Math.Cos) },
            { "cosh",             new("cosh",             1, Math.Cosh) },
            { "exp",              new("exp",              1, Math.Exp) },
            { "fusedmultiplyadd", new("fusedmultiplyadd", 3, Math.FusedMultiplyAdd) },
            { "ieeeremainder",    new("ieeeremainder",    2, Math.IEEERemainder) },
            { "log10",            new("log10",            1, Math.Log10) },
            { "log2",             new("log2",             1, Math.Log2) },
            { "maxmagnitude",     new("maxmagnitude",     2, Math.MaxMagnitude) },
            { "minmagnitude",     new("minmagnitude",     2, Math.MinMagnitude) },
            { "pow",              new("pow",              2, Math.Pow) },
            { "sin",              new("sin",              1, Math.Sin) },
            { "sinh",             new("sinh",             1, Math.Sinh) },
            { "sqrt",             new("sqrt",             1, Math.Sqrt) },
            { "tan",              new("tan",              1, Math.Tan) },
            { "tanh",             new("tanh",             1, Math.Tanh) },
        };
    // csharpier-ignore-end
}
