using System;
using System.Collections.Generic;
using System.Globalization;

namespace Eval
{
    public static class Utilities
    {
        public static Dictionary<string, Func<double, double, double>> Operators { get; } = new()
        {
            { "+", (x, y) => x + y },
            { "-", (x, y) => x - y },
            { "*", (x, y) => x * y },
            { "/", (x, y) => x / y },
            { "%", (x, y) => x % y }
        };

        public static Dictionary<string, double> Variables { get; } = new()
        {
            { "Math.PI", Math.PI },
            { "Math.E", Math.E },
        };

        public static Dictionary<string, Func<double, double>> Functions { get; } = new()
        {
            { "Math.Sin", Math.Sin },
        };
    }

    public class ASTNode
    {
        public string Op { get; set; } = "";
        public string Lhs { get; set; } = "";
        public ASTNode? Rhs { get; set; }

        public ASTNode(string op, string lhs, ASTNode rhs)
        {
            Op = op;
            Lhs = lhs;
            Rhs = rhs;
        }

        public ASTNode(string end)
        {
            Lhs = end;
        }

        public static void PrintAST(ASTNode node, string indent = "    ")
        {
            if (node == null)
            {
                return;
            }

            if (node.Op != "")
            {
                Console.WriteLine();
                Console.Write($"{indent}({node.Op}");
                indent += "  ";
            }

            Console.Write(node.Rhs != null ? $" {node.Lhs} " : $"{node.Lhs}");
            PrintAST(node.Rhs!, indent);

            if (node.Op != "")
            {
                Console.Write(")");
            }
        }
    }

    public class Lexer
    {
        private string Expr;

        public Lexer(string expr) { Expr = expr; }

        /// <summary>
        /// Loops through the expression token by token.
        /// </summary>
        public string Next()
        {
            string token;

            Expr = Expr.TrimStart();

            if (Expr == "")
            {
                return "";
            }

            if (Utilities.Operators.ContainsKey(Expr[..1]))
            {
                token = Expr[..1];
                Expr = Expr[1..];
                return token;
            }

            for (int i = 0; i < Expr.Length; i++)
            {
                if (Utilities.Operators.ContainsKey(Expr[i..(i + 1)]) ||
                    Expr[i..(i + 1)] == " ")
                {
                    token = Expr[0..i];
                    Expr = Expr[i..];
                    return token;
                }
            }

            token = Expr;
            Expr = "";
            return token;
        }

        public static void Test()
        {
            string expr = "  2.0 * Math.PI/32.0";
            Lexer lexer = new(expr);
            List<string> tokens = new() { "{ \"" };

            Console.WriteLine();

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
            Console.WriteLine(string.Join("", tokens));
        }
    }

    public class Parser
    {
        public static string ParsePrimary(Lexer lexer)
        {
            string lhs = lexer.Next();

            if (lhs == "")
            {
                throw new FormatException("Expected primary expression but reached the end of expression!");
            }

            return lhs;
        }

        public static ASTNode Parse(Lexer lexer)
        {
            string lhs = ParsePrimary(lexer);
            string op = lexer.Next();

            return op switch
            {
                "" => new(lhs),
                _ => Utilities.Operators.ContainsKey(op)
                    ? new(op, lhs, Parse(lexer))
                    : throw new FormatException($"Unknow binary operator `{op}` found while parsing!"),
            };
        }
    }

    public class Program
    {
        public static void Main()
        {
            string[] exprs = {
                " 17% 2.0 * Math.PI/32.0 + 6",
                "1",
                "Math.PI*100-9",
                "10%  9 ",
                "Math.E",
                // TODO: Support these
                // "Math.sin(2.0 * Math.PI)",
                // "-10",
                // "(10/4)+2",
            };

            Console.WriteLine();

            foreach (string expr in exprs)
            {
                Console.Write($"Result: {Eval(expr)}\n\nAST: ");
                ASTNode.PrintAST(Parser.Parse(new Lexer(expr)));
                Console.WriteLine();
                Console.WriteLine("============================");
            }
        }

        public static double Eval(string expr)
        {
            return Evaluate(Parser.Parse(new Lexer(expr)));
        }

        public static double Evaluate(ASTNode node)
        {
            return node.Op switch
            {
                "" => Evaluate(node.Lhs),
                _ => Utilities.Operators[node.Op](Evaluate(node.Lhs), Evaluate(node.Rhs!)),
            };
        }

        public static double Evaluate(string value)
        {
            return double.TryParse(value, NumberStyles.Float,
                                   CultureInfo.InvariantCulture, out double number)
                ? number
                : Utilities.Variables.TryGetValue(value, out double var)
                ? var :
                    throw new FormatException($"Unknown variable {value}!");
        }
    }
}
