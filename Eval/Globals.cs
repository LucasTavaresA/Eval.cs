// Licensed under the LGPL3 license.
// See the LICENSE file in the project root for more information.
#pragma warning disable CA1716

using System;
using System.Collections.Generic;
using System.Linq;

namespace Eval
{
    public readonly struct Globals
    {
        public static readonly Func<double, double> Negative = (double val) => -val;

        public readonly struct Token
        {
            public TokenKind Kind { get; }
            public string Literal { get; }

            public Token(TokenKind kind, string literal)
            {
                Kind = kind;
                Literal = literal;
            }

            public override string ToString() => Literal;
        }

        public readonly struct Operator
        {
            public int Precedence { get; }
            public Func<double, double, double> Operation { get; }

            public Operator(int precedence, Func<double, double, double> operation)
            {
                Precedence = precedence;
                Operation = operation;
            }
        }

        public struct Function
        {
            public string Name { get; set; }
            public Delegate Funcall { get; set; }
            public int Args { get; set; }
            public int Offset { get; set; }

            public Function(string name, int argAmount, Delegate funcall)
            {
                Name = name;
                Funcall = funcall;
                Args = argAmount;
                Offset = 0;
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

        // NOTE: this is ridiculous but get net5.0 working
        public static readonly Dictionary<string, Function> Functions =
            new()
            {
                { "average",          new("average",          0, (Func<double[], double>)((double[] args) => args.Average())) },
                { "max",              new("max",              0, (Func<double[], double>)((double[] args) => args.Max())) },
                { "min",              new("min",              0, (Func<double[], double>)((double[] args) => args.Min())) },
                { "sum",              new("sum",              0, (Func<double[], double>)((double[] args) => args.Sum())) },
                { "last",             new("last",             0, (Func<double[], double>)((double[] args) => args.Last())) },
                { "length",           new("length",           0, (Func<double[], double>)((double[] args) => args.Length)) },
                { "count",            new("count",            0, (Func<double[], double>)((double[] args) => args.Length)) },
                { "first",            new("first",            0, (Func<double[], double>)((double[] args) => args.First())) },
                { "single",           new("single",           0, (Func<double[], double>)((double[] args) => args.Single())) },
                { "gethashcode",      new("gethashcode",      0, (Func<double[], double>)((double[] args) => args.GetHashCode())) },
                { "singleordefault",  new("singleordefault",  0, (Func<double[], double>)((double[] args) => args.SingleOrDefault())) },
                { "firstordefault",   new("firstordefault",   0, (Func<double[], double>)((double[] args) => args.FirstOrDefault())) },
                { "lastordefault",    new("lastordefault",    0, (Func<double[], double>)((double[] args) => args.LastOrDefault())) },
                { "abs",              new("abs",              1, (Func<double, double>)((double arg) => Math.Abs(arg))) },
                { "ceiling",          new("ceiling",          1, (Func<double, double>)((double arg) => Math.Ceiling(arg))) },
                { "floor",            new("floor",            1, (Func<double, double>)((double arg) => Math.Floor(arg))) },
                { "log",              new("log",              1, (Func<double, double>)((double arg) => Math.Log(arg))) },
                { "round",            new("round",            1, (Func<double, double>)((double arg) => Math.Round(arg))) },
                { "truncate",         new("truncate",         1, (Func<double, double>)((double arg) => Math.Truncate(arg))) },
                { "mod",              new("mod",              2, (Func<double, double, double>)((double x, double y) => x % y)) },
                { "acos",             new("acos",             1, (Func<double, double>)((double x) => Math.Acos(x))) },
                { "acosh",            new("acosh",            1, (Func<double, double>)((double x) => Math.Acosh(x))) },
                { "asin",             new("asin",             1, (Func<double, double>)((double x) => Math.Asin(x))) },
                { "asinh",            new("asinh",            1, (Func<double, double>)((double x) => Math.Asinh(x))) },
                { "atan",             new("atan",             1, (Func<double, double>)((double x) => Math.Atan(x))) },
                { "atan2",            new("atan2",            2, (Func<double, double, double>)((double x, double y) => Math.Atan2(x, y))) },
                { "atanh",            new("atanh",            1, (Func<double, double>)((double x) => Math.Atanh(x))) },
                { "bitdecrement",     new("bitdecrement",     1, (Func<double, double>)((double x) => Math.BitDecrement(x))) },
                { "bitincrement",     new("bitincrement",     1, (Func<double, double>)((double x) => Math.BitIncrement(x))) },
                { "cbrt",             new("cbrt",             1, (Func<double, double>)((double x) => Math.Cbrt(x))) },
                { "copysign",         new("copysign",         2, (Func<double, double, double>)((double x, double y) => Math.CopySign(x, y))) },
                { "cos",              new("cos",              1, (Func<double, double>)((double x) => Math.Cos(x))) },
                { "cosh",             new("cosh",             1, (Func<double, double>)((double x) => Math.Cosh(x))) },
                { "exp",              new("exp",              1, (Func<double, double>)((double x) => Math.Exp(x))) },
                { "fusedmultiplyadd", new("fusedmultiplyadd", 3, (Func<double, double, double, double>)((double x, double y, double z) => Math.FusedMultiplyAdd(x, y, z))) },
                { "ieeeremainder",    new("ieeeremainder",    2, (Func<double, double, double>)((double x, double y) => Math.IEEERemainder(x, y))) },
                { "log10",            new("log10",            1, (Func<double, double>)((double x) => Math.Log10(x))) },
                { "log2",             new("log2",             1, (Func<double, double>)((double x) => Math.Log2(x))) },
                { "maxmagnitude",     new("maxmagnitude",     2, (Func<double, double, double>)((double x, double y) => Math.MaxMagnitude(x, y))) },
                { "minmagnitude",     new("minmagnitude",     2, (Func<double, double, double>)((double x, double y) => Math.MinMagnitude(x, y))) },
                { "pow",              new("pow",              2, (Func<double, double, double>)((double x, double y) => Math.Pow(x, y))) },
                { "sin",              new("sin",              1, (Func<double, double>)((double x) => Math.Sin(x))) },
                { "sinh",             new("sinh",             1, (Func<double, double>)((double x) => Math.Sinh(x))) },
                { "sqrt",             new("sqrt",             1, (Func<double, double>)((double x) => Math.Sqrt(x))) },
                { "tan",              new("tan",              1, (Func<double, double>)((double x) => Math.Tan(x))) },
                { "tanh",             new("tanh",             1, (Func<double, double>)((double x) => Math.Tanh(x))) },
            };
        // csharpier-ignore-end
    }
}
