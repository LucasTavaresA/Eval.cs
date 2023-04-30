using System;
using System.Collections.Generic;
using System.Linq;

namespace Eval
{
    public static class Runtime
    {
        public static Dictionary<string, Delegate> Functions { get; } = new()
        {
            { "Average", (Func<double[], double>)((args) => args.Average()) },
            { "Max", (Func<double[], double>)((args) => args.Max()) },
            { "Min", (Func<double[], double>)((args) => args.Min()) },
            { "Sum", (Func<double[], double>)((args) => args.Sum()) },
            { "Last", (Func<double[], double>)((args) => args.Last()) },
            { "Length", (Func<double[], double>)((args) => args.Length) },
            { "Count", (Func<double[], double>)((args) => args.Length) },
            { "First", (Func<double[], double>)((args) => args.First()) },
            { "Single", (Func<double[], double>)((args) => args.Single()) },
            { "GetHashCode", (Func<double[], double>)((args) => args.GetHashCode()) },
            { "SingleOrDefault", (Func<double[], double>)((args) => args.SingleOrDefault()) },
            { "FirstOrDefault", (Func<double[], double>)((args) => args.FirstOrDefault()) },
            { "LastOrDefault", (Func<double[], double>)((args) => args.LastOrDefault()) },
            { "Abs", (Func<double, double>)Math.Abs },
            { "Acos", (Func<double, double>)Math.Acos },
            { "Acosh", (Func<double, double>)Math.Acosh },
            { "Asin", (Func<double, double>)Math.Asin },
            { "Asinh", (Func<double, double>)Math.Asinh },
            { "Atan", (Func<double, double>)Math.Atan },
            { "Atan2", (Func<double, double, double>)Math.Atan2 },
            { "Atanh", (Func<double, double>)Math.Atanh },
            { "BitDecrement", (Func<double, double>)Math.BitDecrement },
            { "BitIncrement", (Func<double, double>)Math.BitIncrement },
            { "Cbrt", (Func<double, double>)Math.Cbrt },
            { "Ceiling", (Func<double, double>)Math.Ceiling },
            { "CopySign", (Func<double, double, double>)Math.CopySign },
            { "Cos", (Func<double, double>)Math.Cos },
            { "Cosh", (Func<double, double>)Math.Cosh },
            { "Exp", (Func<double, double>)Math.Exp },
            { "Floor", (Func<double, double>)Math.Floor },
            { "FusedMultiplyAdd", (Func<double, double, double, double>)Math.FusedMultiplyAdd },
            { "IEEERemainder", (Func<double, double, double>)Math.IEEERemainder },
            { "Log", (Func<double, double>)Math.Log },
            { "Log10", (Func<double, double>)Math.Log10 },
            { "Log2", (Func<double, double>)Math.Log2 },
            { "MaxMagnitude", (Func<double, double, double>)Math.MaxMagnitude },
            { "MinMagnitude", (Func<double, double, double>)Math.MinMagnitude },
            { "Pow", (Func<double, double, double>)Math.Pow },
            { "Round", (Func<double, double>)Math.Round },
            { "Sin", (Func<double, double>)Math.Sin },
            { "Sinh", (Func<double, double>)Math.Sinh },
            { "Sqrt", (Func<double, double>)Math.Sqrt },
            { "Tan", (Func<double, double>)Math.Tan },
            { "Tanh", (Func<double, double>)Math.Tanh },
            { "Truncate", (Func<double, double>)Math.Truncate },
        };

        public static Dictionary<string, double> Variables { get; } = new()
        {
            { "PI", Math.PI },
            { "E", Math.E },
            { "Tau", Math.Tau },
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
            { "-", 1 }, // minus is not really a binary operator so it need to be evaluated first
            { "*", 2 },
            { "/", 2 },
            { "%", 2 },
        };
    }
}
