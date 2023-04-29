using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Eval.Expression;
using static Eval.Runtime;
using static Eval.Parser;

namespace Eval
{
    public static class Expression
    {
        public class BinaryOperator
        {
            public BinaryOperator(string op, object left, object right)
            {
                Op = op;
                Left = left;
                Right = right;
            }

            public string Op { get; set; }
            public object Left { get; set; }
            public object Right { get; set; }
        }

        public class Funcall
        {
            public Funcall(string name, List<object> args)
            {
                Name = name;
                Args = args;
            }

            public string Name { get; set; }
            public List<object> Args { get; set; }
        }

        /// <summary>
        /// Returns a string representation of the expressions tree
        /// </summary>
        public static string GetExprTree(object expr, string indent = "")
        {
            indent += "  ";

            return expr switch
            {
                BinaryOperator binary
                    => $"\n{indent}({binary.Op}{GetExprTree(binary.Left, indent)}{GetExprTree(binary.Right, indent)})",
                Funcall funcall
                    => $"\n{indent}({funcall.Name}{string.Concat(funcall.Args.Select(arg => GetExprTree(arg, indent)))})",
                string str
                    => $" {str}",
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
        /// Returns a token back in the Src
        /// </summary>
        public void Return(string token)
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
        /// Tokens that are not operators
        /// </summary>
        private static string[] FlowTokens { get; } = { "(", ")", "," };

        /// <summary>
        /// All the tokens
        /// </summary>
        private static string[] Tokens { get; } = BinaryOperators.Keys.Concat(FlowTokens).ToArray();

        /// <summary>
        /// Check if is a token or any of the provided strings
        /// </summary>
        private static bool IsTokenOr(string val, params string[] tokens)
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
        private static object ParsePrimary(Lexer lexer)
        {
            string token = lexer.Next();

            if (token != "")
            {
                if (token == "-")
                {
                    return $"{token}{lexer.Next()}";
                }
                else if (token == "(")
                {
                    object expr = Parse(lexer);
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
                        List<object> args = new();
                        nextToken = lexer.Next();

                        if (nextToken == ")")
                        {
                            return new Funcall(token, args);
                        }

                        if (nextToken == "")
                        {
                            throw new FormatException("Unexpected end of input");
                        }

                        lexer.Return(nextToken);
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
                            lexer.Return(nextToken);
                        }

                        return token;
                    }
                }
            }
            else
            {
                throw new FormatException("Expected primary expression but reached the end of the input!");
            }
        }

        public static object Parse(Lexer lexer, int precedence = 0)
        {
            if (precedence >= 2)
            {
                return ParsePrimary(lexer);
            }

            object left = Parse(lexer, precedence + 1);
            string op = lexer.Next();

            if (op != "")
            {
                if (BinaryOperators.TryGetValue(op, out _) && Precedence[op] == precedence)
                {
                    object right = Parse(lexer, precedence);
                    return new BinaryOperator(op, left, right);
                }
                else
                {
                    lexer.Return(op);
                }
            }

            return left;
        }

        public static object Parse(string expression)
        {
            return Parse(new Lexer(expression));
        }
    }

    public static class Evaluator
    {
        private static string RemovePrefix(string str)
        {
            // false advertising ;-;
            // for csharp functions support
            return System.Text.RegularExpressions.Regex.Replace(str, "^(Math|IEnumerable)\\.", "");
        }

        public static double Evaluate(object expr)
        {
            return expr switch
            {
                string str =>
                    double.TryParse(str, NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out double number)
                        ? number
                        : Variables.TryGetValue(RemovePrefix(str), out number)
                        ? number
                        : throw new ArgumentException($"Unknown variable '{str}'"),

#nullable enable
                BinaryOperator binary =>
                    BinaryOperators.TryGetValue(binary.Op, out Func<double, double, double>? bfunc)
                        ? bfunc(Evaluate(binary.Left),
                                Evaluate(binary.Right))
                        : throw new ArgumentException($"Unknown binary operator '{binary.Op}'"),

                Funcall funcall =>
                    Functions.TryGetValue(RemovePrefix(funcall.Name), out Delegate? func)
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
                            Func<double[], double> vfunc =>
                                vfunc(funcall.Args.Select(Evaluate).ToArray()),
                            _ => throw new InvalidOperationException($"Function '{funcall.Name}' expects {func.Method.GetParameters().Length} but received {funcall.Args.Count}"),
                        }
                    : throw new InvalidOperationException($"Unknown function '{funcall.Name}'"),
#nullable disable

                _ => throw new ArgumentException($"Unknown expression type '{expr.GetType()}'"),
            };
        }

        public static double Evaluate(string expr)
        {
            return Evaluate(Parse(new Lexer(expr)));
        }
    }
}
