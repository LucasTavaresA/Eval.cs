using System;
using System.Collections.Generic;
using System.Linq;

namespace Eval
{
    public static class Runtime
    {
        public static Dictionary<string, double> Variables { get; } = new()
        {
            { "Math.PI", Math.PI },
            { "Math.E", Math.E },
            { "Math.Tau", Math.Tau },
        };

        public static Dictionary<string, Delegate> Functions { get; } = new()
        {
            { "IEnumerable.Average", (Func<double[], double>)((args) => args.Average()) },
            { "IEnumerable.Max", (Func<double[], double>)((args) => args.Max()) },
            { "IEnumerable.Min", (Func<double[], double>)((args) => args.Min()) },
            { "IEnumerable.Sum", (Func<double[], double>)((args) => args.Sum()) },
            { "IEnumerable.Last", (Func<double[], double>)((args) => args.Last()) },
            { "IEnumerable.Length", (Func<double[], double>)((args) => args.Length) },
            { "IEnumerable.Count", (Func<double[], double>)((args) => args.Length) },
            { "IEnumerable.First", (Func<double[], double>)((args) => args.First()) },
            { "IEnumerable.Single", (Func<double[], double>)((args) => args.Single()) },
            { "IEnumerable.GetHashCode", (Func<double[], double>)((args) => args.GetHashCode()) },
            { "IEnumerable.SingleOrDefault", (Func<double[], double>)((args) => args.SingleOrDefault()) },
            { "IEnumerable.FirstOrDefault", (Func<double[], double>)((args) => args.FirstOrDefault()) },
            { "IEnumerable.LastOrDefault", (Func<double[], double>)((args) => args.LastOrDefault()) },
            { "Math.Abs", (Func<double, double>)Math.Abs },
            { "Math.Acos", (Func<double, double>)Math.Acos },
            { "Math.Acosh", (Func<double, double>)Math.Acosh },
            { "Math.Asin", (Func<double, double>)Math.Asin },
            { "Math.Asinh", (Func<double, double>)Math.Asinh },
            { "Math.Atan", (Func<double, double>)Math.Atan },
            { "Math.Atan2", (Func<double, double, double>)Math.Atan2 },
            { "Math.Atanh", (Func<double, double>)Math.Atanh },
            { "Math.BitDecrement", (Func<double, double>)Math.BitDecrement },
            { "Math.BitIncrement", (Func<double, double>)Math.BitIncrement },
            { "Math.Cbrt", (Func<double, double>)Math.Cbrt },
            { "Math.Ceiling", (Func<double, double>)Math.Ceiling },
            { "Math.CopySign", (Func<double, double, double>)Math.CopySign },
            { "Math.Cos", (Func<double, double>)Math.Cos },
            { "Math.Cosh", (Func<double, double>)Math.Cosh },
            { "Math.Exp", (Func<double, double>)Math.Exp },
            { "Math.Floor", (Func<double, double>)Math.Floor },
            { "Math.FusedMultiplyAdd", (Func<double, double, double, double>)Math.FusedMultiplyAdd },
            { "Math.IEEERemainder", (Func<double, double, double>)Math.IEEERemainder },
            { "Math.Log", (Func<double, double>)Math.Log },
            { "Math.Log10", (Func<double, double>)Math.Log10 },
            { "Math.Log2", (Func<double, double>)Math.Log2 },
            { "Math.MaxMagnitude", (Func<double, double, double>)Math.MaxMagnitude },
            { "Math.MinMagnitude", (Func<double, double, double>)Math.MinMagnitude },
            { "Math.Pow", (Func<double, double, double>)Math.Pow },
            { "Math.Round", (Func<double, double>)Math.Round },
            { "Math.Sin", (Func<double, double>)Math.Sin },
            { "Math.Sinh", (Func<double, double>)Math.Sinh },
            { "Math.Sqrt", (Func<double, double>)Math.Sqrt },
            { "Math.Tan", (Func<double, double>)Math.Tan },
            { "Math.Tanh", (Func<double, double>)Math.Tanh },
            { "Math.Truncate", (Func<double, double>)Math.Truncate },
        };

        public static Dictionary<string, Func<double, double, double>> BinaryOperators { get; } = new()
        {
            { "+", (left, right) => left + right },
            { "-", (left, right) => left - right },
            { "*", (left, right) => left * right },
            { "/", (left, right) => left / right },
            { "%", (left, right) => left % right }
        };

        public static Dictionary<string, int> Precedence { get; } = new()
        {
            { "+", 0 },
            { "-", 0 },
            { "*", 1 },
            { "/", 1 },
            { "%", 1 },
        };
    }
}
