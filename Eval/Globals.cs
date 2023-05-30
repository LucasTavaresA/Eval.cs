// Licensed under the LGPL3 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Eval;

public struct KeyLengthComparer : IComparer<string>
{
    public int Compare(string x, string y)
    {
        var lengthComparison = y.Length.CompareTo(x.Length);
        return lengthComparison == 0
            ? string.Compare(y, x, StringComparison.Ordinal)
            : lengthComparison;
    }
}

public struct Globals
{
    // csharpier-ignore-start
    public static readonly Dictionary<string, Funcall> Functions =
        new()
        {
            { "average",          new("average",          0, (Func<double[], double>)((args) => args.Average())) },
            { "max",              new("max",              0, (Func<double[], double>)((args) => args.Max())) },
            { "min",              new("min",              0, (Func<double[], double>)((args) => args.Min())) },
            { "sum",              new("sum",              0, (Func<double[], double>)((args) => args.Sum())) },
            { "last",             new("last",             0, (Func<double[], double>)((args) => args.Last())) },
            { "length",           new("length",           0, (Func<double[], double>)((args) => args.Length)) },
            { "count",            new("count",            0, (Func<double[], double>)((args) => args.Length)) },
            { "first",            new("first",            0, (Func<double[], double>)((args) => args.First())) },
            { "single",           new("single",           0, (Func<double[], double>)((args) => args.Single())) },
            { "gethashcode",      new("gethashcode",      0, (Func<double[], double>)((args) => args.GetHashCode())) },
            { "singleordefault",  new("singleordefault",  0, (Func<double[], double>)((args) => args.SingleOrDefault())) },
            { "firstordefault",   new("firstordefault",   0, (Func<double[], double>)((args) => args.FirstOrDefault())) },
            { "lastordefault",    new("lastordefault",    0, (Func<double[], double>)((args) => args.LastOrDefault())) },
            { "abs",              new("abs",              1, (Func<double, double>)Math.Abs) },
            { "ceiling",          new("ceiling",          1, (Func<double, double>)Math.Ceiling) },
            { "floor",            new("floor",            1, (Func<double, double>)Math.Floor) },
            { "log",              new("log",              1, (Func<double, double>)Math.Log) },
            { "round",            new("round",            1, (Func<double, double>)Math.Round) },
            { "truncate",         new("truncate",         1, (Func<double, double>)Math.Truncate) },
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

    public static readonly Dictionary<string, double> Variables =
        new()
        {
            { "pi",  Math.PI },
            { "e",   Math.E },
            { "tau", Math.Tau },
        };

    public static readonly SortedDictionary<string, BinaryOperator> BinaryOperators =
        new(new KeyLengthComparer())
        {
            { "+",  new(1, "+",  (left, right) => left + right) },
            { "-",  new(1, "-",  (left, right) => left - right) },
            { "*",  new(2, "*",  (left, right) => left * right) },
            { "/",  new(2, "/",  (left, right) => left / right) },
            { "%",  new(2, "%",  (left, right) => left % right) },
            { "^",  new(3, "^",  Math.Pow) },
            { "+-", new(1, "+-", (left, right) => left + -right) },
            { "-+", new(1, "-+", (left, right) => left - +right) },
            { "*-", new(2, "*-", (left, right) => left * -right) },
            { "*+", new(2, "*+", (left, right) => left * +right) },
            { "/-", new(2, "/-", (left, right) => left / -right) },
            { "/+", new(2, "/+", (left, right) => left / +right) },
            { "%-", new(2, "%-", (left, right) => left % -right) },
            { "%+", new(2, "%+", (left, right) => left % +right) },
            { "<<", new(0, "<<", (left, right) => (int)left << (int)right) },
            { ">>", new(0, ">>", (left, right) => (int)left >> (int)right) },
        };

    public static readonly Dictionary<string, AdditiveOperator> AdditiveOperators =
        new()
        {
            { "+", new("+", (val) => +val) },
            { "-", new("-", (val) => -val) },
        };
    // csharpier-ignore-end

    /// <summary>
    /// Tokens that are not operators
    /// </summary>
    public static readonly string[] FlowTokens = { "(", ")", " ", "," };

    /// <summary>
    /// All the tokens sorted by length
    /// </summary>
    public static readonly string[] Tokens = BinaryOperators.Keys.Concat(FlowTokens).ToArray();

    public struct BinaryOperator
    {
        public int Precedence { get; set; }
        public string Op { get; set; }
        public Func<double, double, double> Operation { get; set; }

        public BinaryOperator(int precedence, string op, Func<double, double, double> operation)
        {
            Precedence = precedence;
            Op = op;
            Operation = operation;
        }

        public override string ToString()
        {
            return Op;
        }
    }

    public struct AdditiveOperator
    {
        public string Op { get; set; }
        public Func<double, double> Operation { get; set; }

        public AdditiveOperator(string op, Func<double, double> operation)
        {
            Op = op;
            Operation = operation;
        }

        public override string ToString()
        {
            return Op;
        }
    }

    public struct Funcall
    {
        public string Name { get; set; }
        public Delegate Func { get; set; }
        public int ArgAmount { get; set; }

        public Funcall(string name, int argAmount, Delegate func)
        {
            Name = name;
            ArgAmount = argAmount;
            Func = func;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
