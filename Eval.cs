using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Eval.Expression;
using static Eval.Runtime;
using static Eval.Parser;

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

        public static Dictionary<string, Func<double[], double>> VariadicFunctions { get; } = new()
        {
            { "IEnumerable.Average", (args) => args.Average() },
            { "IEnumerable.Max", (args) => args.Max() },
            { "IEnumerable.Min", (args) => args.Min() },
            { "IEnumerable.Sum", (args) => args.Sum() },
            { "IEnumerable.Last", (args) => args.Last() },
            { "IEnumerable.Length", (args) => args.Length },
            { "IEnumerable.Count", (args) => args.Length },
            { "IEnumerable.First", (args) => args.First() },
            { "IEnumerable.Single", (args) => args.Single() },
            { "IEnumerable.Last", (args) => args.Last() },
            { "IEnumerable.GetHashCode", (args) => args.GetHashCode() },
            { "IEnumerable.SingleOrDefault", (args) => args.SingleOrDefault() },
            { "IEnumerable.FirstOrDefault", (args) => args.FirstOrDefault() },
            { "IEnumerable.LastOrDefault", (args) => args.LastOrDefault() },
        };

        public static Dictionary<string, Delegate> Functions { get; } = new()
        {
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

        public static Dictionary<string, Func<double, double>> UnaryOperators { get; } = new()
        {
            { "-", (arg) => -arg }
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

    public abstract class Expression
    {
        public class UnaryOperator : Expression
        {
            public UnaryOperator(string op, Expression operand)
            {
                Op = op;
                Operand = operand;
            }

            public string Op { get; set; }
            public Expression Operand { get; set; }
        }

        public class BinaryOperator : Expression
        {
            public BinaryOperator(string op, Expression left, Expression right)
            {
                Op = op;
                Left = left;
                Right = right;
            }

            public string Op { get; set; }
            public Expression Left { get; set; }
            public Expression Right { get; set; }
        }

        public class Funcall : Expression
        {
            public Funcall(string name, List<Expression> args)
            {
                Name = name;
                Args = args;
            }

            public string Name { get; set; }
            public List<Expression> Args { get; set; }
        }

        public class Symbol : Expression
        {
            public Symbol(string value)
            {
                Value = value;
            }

            public string Value { get; set; }
        }

        /// <summary>
        /// Returns a string representation of the expressions tree
        /// </summary>
        public static string GetExprTree(Expression expr, string indent = "")
        {
            indent += "  ";

            return expr switch
            {
                UnaryOperator unary => $"{indent}UnaryOperator({unary.Op})\n{GetExprTree(unary.Operand, indent)}",
                BinaryOperator binary => $"{indent}BinaryOperator({binary.Op})\n{GetExprTree(binary.Left, indent)}{GetExprTree(binary.Right, indent)}",
                Funcall funcall => $"{indent}Funcall({funcall.Name})\n{string.Concat(funcall.Args.Select(arg => GetExprTree(arg, indent)))}",
                Symbol symbol => $"{indent}Symbol({symbol.Value})\n",
                _ => throw new ArgumentException($"Unexpected expression type '{expr.GetType()}'"),
            };
        }

        public static string GetExprTree(string expr)
        {
            return GetExprTree(Parse(expr));
        }
    }

    public class Lexer
    {
        private string Src { get; set; } = "";

        public Lexer(string src)
        {
            Src = src;
        }

        /// <summary>
        /// Tokens that are not operators
        /// </summary>
        private static string[] FlowTokens { get; } = { "(", ")", "," };

        /// <summary>
        /// All the tokens
        /// </summary>
        private static string[] Tokens { get; } =
            BinaryOperators.Keys.Concat(UnaryOperators.Keys).Concat(FlowTokens).ToArray();

        /// <summary>
        /// Check if is a token or any of the provided strings
        /// </summary>
        public static bool IsTokenOr(string val, params string[] tokens)
        {
            foreach (string token in Tokens.Concat(tokens))
            {
                if (token == val)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Places the previous token back in the Src
        /// </summary>
        public void Prev(string token)
        {
            Src = token + Src;
        }

        /// <summary>
        /// Gets the next token
        /// </summary>
        public string Next()
        {
            string token;
            Src = Src.TrimStart();

            if (Src == "")
            {
                return "";
            }

            if (IsTokenOr(Src[..1]))
            {
                token = Src[..1];
                Src = Src[1..];
                return token;
            }

            for (int i = 0; i < Src.Length; ++i)
            {
                if (IsTokenOr(Src[i..(i + 1)], " "))
                {
                    token = Src[0..i];
                    Src = Src[i..];
                    return token;
                }
            }

            token = Src;
            Src = "";
            return token;
        }

        /// <summary>
        /// Returns a string representation of the tokens separated by the
        /// lexer
        /// </summary>
        public static string GetTokens(string expr)
        {
            Lexer lexer = new(expr);
            List<string> tokens = new() { "{ \"" };

            string token = lexer.Next();
            while (token != "")
            {
                tokens.Add(token);
                token = lexer.Next();

                if (token != "")
                {
                    tokens.Add("\", \"");
                }
            }

            tokens.Add("\" }");
            return string.Join("", tokens);
        }
    }

    public static class Parser
    {
        private static Expression ParsePrimary(Lexer lexer)
        {
            string token = lexer.Next();

            if (token != "")
            {
                if (UnaryOperators.ContainsKey(token))
                {
                    return new UnaryOperator(token, Parse(lexer));
                }
                else if (token == "(")
                {
                    Expression expr = Parse(lexer);
                    token = lexer.Next();

                    return token != ")"
                        ? throw new FormatException($"Expected ')' but got '{token}'")
                        : expr;
                }
                else if (token == ")")
                {
                    throw new FormatException("Unbalanced parenthesis");
                }
                else
                {
                    string nextToken = lexer.Next();

                    if (nextToken == "(")
                    {
                        List<Expression> args = new();
                        nextToken = lexer.Next();

                        if (nextToken == ")")
                        {
                            return new Funcall(token, args);
                        }

                        if (nextToken == "")
                        {
                            throw new FormatException("Unexpected end of input");
                        }

                        lexer.Prev(nextToken);
                        args.Add(Parse(lexer));

                        nextToken = lexer.Next();
                        while (nextToken == ",")
                        {
                            args.Add(Parse(lexer));
                            nextToken = lexer.Next();
                        }

                        return nextToken != ")"
                            ? throw new FormatException($"Expected ')' but got '{nextToken}'")
                            : new Funcall(token, args);
                    }
                    else
                    {
                        if (nextToken != "")
                        {
                            lexer.Prev(nextToken);
                        }

                        return new Symbol(token);
                    }
                }
            }
            else
            {
                throw new FormatException("Expected primary expression but reached the end of the input!");
            }
        }

        public static Expression Parse(Lexer lexer, int precedence = 0)
        {
            if (precedence >= 2)
            {
                return ParsePrimary(lexer);
            }

            Expression left = Parse(lexer, precedence + 1);
            string op = lexer.Next();

            if (op != "")
            {
                if (BinaryOperators.TryGetValue(op, out _) && Precedence[op] == precedence)
                {
                    Expression right = Parse(lexer, precedence);
                    return new BinaryOperator(op, left, right);
                }
                else
                {
                    lexer.Prev(op);
                }
            }

            return left;
        }

        public static Expression Parse(string expression)
        {
            return Parse(new Lexer(expression));
        }
    }

    public static class Evaluator
    {
        public static double Evaluate(Expression expr)
        {
            return expr switch
            {
                Symbol symbol =>
                    double.TryParse(symbol.Value, NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out double number)
                        ? number
                        : Variables.TryGetValue(symbol.Value, out number)
                          || Variables.TryGetValue(symbol.Value, out number)
                        ? number
                        : throw new ArgumentException($"Unknown variable '{symbol.Value}'"),

                UnaryOperator unary =>
                    UnaryOperators.TryGetValue(unary.Op, out Func<double, double>? ufunc)
                       ? ufunc(Evaluate(unary.Operand))
                       : throw new ArgumentException($"Unknown unary operator '{unary.Op}'"),

                BinaryOperator binary =>
                    BinaryOperators.TryGetValue(binary.Op, out Func<double, double, double>? bfunc)
                        ? bfunc(Evaluate(binary.Left),
                                Evaluate(binary.Right))
                        : throw new ArgumentException($"Unknown binary operator '{binary.Op}'"),

                Funcall funcall =>
                    VariadicFunctions.TryGetValue(funcall.Name, out Func<double[], double>? vfunc)
                        ? vfunc(funcall.Args.Select(Evaluate).ToArray())
                        : Functions.TryGetValue(funcall.Name, out Delegate? func)
                        ? func switch
                        {
                            Func<double, double> func1 =>
                                func1(Evaluate(funcall.Args[0])),
                            Func<double, double, double> func2 =>
                                func2(Evaluate(funcall.Args[0]),
                                      Evaluate(funcall.Args[1])),
                            Func<double, double, double, double> func3 =>
                                func3(Evaluate(funcall.Args[0]),
                                      Evaluate(funcall.Args[1]),
                                      Evaluate(funcall.Args[2])),
                            _ => throw new InvalidOperationException($"Function '{funcall.Name}' expects {func.Method.GetParameters().Length} but received {funcall.Args.Count}"),
                        }
                        : throw new ArgumentException($"Unknown function '{funcall.Name}'"),

                _ => throw new ArgumentException($"Unknown expression type '{expr.GetType()}'"),
            };
        }

        public static double Evaluate(string expr)
        {
            return Evaluate(Parse(new Lexer(expr)));
        }
    }
}
