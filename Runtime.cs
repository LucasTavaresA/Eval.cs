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
            { "Math.Acos", Math.Acos },
            { "Math.Acosh", Math.Acosh },
            { "Math.Asin", Math.Asin },
            { "Math.Asinh", Math.Asinh },
            { "Math.Atan", Math.Atan },
            { "Math.Atan2", Math.Atan2 },
            { "Math.Atanh", Math.Atanh },
            { "Math.BitDecrement", Math.BitDecrement },
            { "Math.BitIncrement", Math.BitIncrement },
            { "Math.Cbrt", Math.Cbrt },
            { "Math.Ceiling", (Func<double, double>)Math.Ceiling },
            { "Math.CopySign", Math.CopySign },
            { "Math.Cos", Math.Cos },
            { "Math.Cosh", Math.Cosh },
            { "Math.Exp", Math.Exp },
            { "Math.Floor", (Func<double, double>)Math.Floor },
            { "Math.FusedMultiplyAdd", Math.FusedMultiplyAdd },
            { "Math.IEEERemainder", Math.IEEERemainder },
            { "Math.Log", (Func<double, double>)Math.Log },
            { "Math.Log10", Math.Log10 },
            { "Math.Log2", Math.Log2 },
            { "Math.MaxMagnitude", Math.MaxMagnitude },
            { "Math.MinMagnitude", Math.MinMagnitude },
            { "Math.Pow", Math.Pow },
            { "Math.ReciprocalEstimate", Math.ReciprocalEstimate },
            { "Math.ReciprocalSqrtEstimate", Math.ReciprocalSqrtEstimate },
            { "Math.Round", (Func<double, double>)Math.Round },
            { "Math.Sin", Math.Sin },
            { "Math.Sinh", Math.Sinh },
            { "Math.Sqrt", Math.Sqrt },
            { "Math.Tan", Math.Tan },
            { "Math.Tanh", Math.Tanh },
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
