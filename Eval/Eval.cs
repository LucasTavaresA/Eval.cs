using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using static Eval.Globals;

namespace Eval
{
    public struct Lexer
    {
        private string Src { get; set; }

        public Lexer(string src)
        {
            Src = src;
        }

        /// <summary>
        /// Get the next token and remove it from src
        /// </summary>
        public string Pop()
        {
            string token = Peek();
            Drop(token.Length);
            return token;
        }

        /// <summary>
        /// Removes an amount of chars from source
        /// </summary>
        public void Drop(int amount)
        {
            Src = Src.TrimStart();
            Src = amount < Src.Length ? Src[amount..] : "";
        }

        /// <summary>
        /// Get the next token
        /// </summary>
        public string Peek()
        {
            string src = Src.TrimStart();

            if (src == "")
            {
                return "";
            }

            int index =
                Tokens
                    .FirstOrDefault((t) => src.StartsWith(t, StringComparison.OrdinalIgnoreCase))
                    ?.Length ?? src.TakeWhile((c) => char.IsLetterOrDigit(c) || c == '.').Count();

            if (index < 1)
            {
                throw new InvalidOperationException($"Invalid Character: '{src[0]}'");
            }

            string token = src[..Math.Max(1, index)];

            // Handle scientific notation
            if (
                token.EndsWith("E", StringComparison.OrdinalIgnoreCase)
                && token.Length > 1
                && index + 1 < src.Length
                && AdditiveOperators.ContainsKey(src[index].ToString())
            )
            {
                index = src[(token.Length + 1)..].TakeWhile(char.IsDigit).Count();
                return src[..(index + token.Length + 1)];
            }

            return token;
        }
    }

    public struct Parser
    {
        private static string CleanNumber(string number)
        {
            return number.TrimEnd('d', 'D');
        }

        private static string CleanSymbol(string symbol)
        {
            return (
                symbol.StartsWith("Math.", StringComparison.OrdinalIgnoreCase)
                    ? symbol[5..]
                    : symbol.StartsWith("IEnumerable.", StringComparison.OrdinalIgnoreCase)
                        ? symbol[12..]
                        : symbol
            ).ToLowerInvariant();
        }

        private static void SetArgs(ref Funcall f, int received)
        {
            if (f.ArgAmount == 0)
            {
                f.ArgAmount = received;
            }
            else if (f.ArgAmount > received)
            {
                throw new ArgumentException(
                    $"Lacking arguments: {f.Name}() expects {f.ArgAmount} arguments but received {received}"
                );
            }
            else if (f.ArgAmount < received)
            {
                throw new ArgumentException(
                    $"Too many arguments: {f.Name}() expects {f.ArgAmount} arguments but received {received}"
                );
            }
        }

        internal static List<object> Parse(ref Lexer lexer)
        {
            Stack<object> operators = new();
            List<object> output = new();
            Stack<int> args = new();
            string token = lexer.Pop();

            // expression starts with additive
            if (AdditiveOperators.TryGetValue(token, out AdditiveOperator additive))
            {
                operators.Push(additive);
                token = lexer.Pop();
            }

            while (token != "")
            {
                if (
                    double.TryParse(
                        CleanNumber(token),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double number
                    ) || Variables.TryGetValue(CleanSymbol(token), out number)
                )
                {
                    output.Add(number);
                    token = lexer.Pop();
                }
                else if (Functions.TryGetValue(CleanSymbol(token), out Funcall func))
                {
                    args.Push(1);
                    operators.Push(func);
                    token = lexer.Pop();
                }
                else if (BinaryOperators.TryGetValue(token, out BinaryOperator cbop))
                {
                    while (
                        operators.Count > 0
                        && (
                            operators.Peek() is AdditiveOperator or Funcall
                            || (
                                operators.Peek() is BinaryOperator bop
                                && cbop.Precedence <= bop.Precedence
                            )
                        )
                    )
                    {
                        if (operators.Peek() is Funcall f)
                        {
                            SetArgs(ref f, args.Pop());
                            output.Add(f);
                            _ = operators.Pop();
                        }
                        else
                        {
                            output.Add(operators.Pop());
                        }
                    }

                    operators.Push(cbop);

                    token = lexer.Pop();

                    // additive operator in front of a binary operator
                    if (AdditiveOperators.TryGetValue(token, out additive))
                    {
                        operators.Push(additive);
                        token = lexer.Pop();
                    }
                }
                else if (token == "(")
                {
                    operators.Push(token);

                    token = lexer.Pop();

                    // expression inside parens starts with additive
                    if (AdditiveOperators.TryGetValue(token, out additive))
                    {
                        operators.Push(additive);
                        token = lexer.Pop();
                    }
                }
                else if (token is ")" or ",")
                {
                    while (operators.Count > 0 && operators.Peek().ToString() != "(")
                    {
                        if (operators.Peek() is Funcall f)
                        {
                            SetArgs(ref f, args.Pop());
                            output.Add(f);
                            _ = operators.Pop();
                        }
                        else
                        {
                            output.Add(operators.Pop());
                        }
                    }

                    if (token == ",")
                    {
                        args.Push(args.Pop() + 1);

                        token = lexer.Pop();

                        // argument starts with additive
                        if (AdditiveOperators.TryGetValue(token, out additive))
                        {
                            operators.Push(additive);
                            token = lexer.Pop();
                        }
                    }
                    else if (token == ")")
                    {
                        // discard (
                        _ =
                            operators.Count == 0
                                ? throw new InvalidOperationException("Closing unexsistent paren")
                                : operators.Pop();
                        token = lexer.Pop();
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Invalid variable or function: '{token}'");
                }
            }

            foreach (object o in operators)
            {
                if (o is "(")
                {
                    throw new InvalidOperationException("Opened paren is not closed");
                }
                else if (o is Funcall f)
                {
                    SetArgs(ref f, args.Pop());
                    output.Add(f);
                }
                else
                {
                    output.Add(o);
                }
            }

            return output;
        }
    }

    internal class ArgumentAmountException : ArgumentException
    {
        internal int Received { get; }

        internal ArgumentAmountException(int received)
        {
            Received = received;
        }
    }

    public struct Evaluator
    {
        private static double[] PopArgs(Stack<double> args, int argAmount)
        {
            double[] outArgs = new double[argAmount];

            for (int i = 0; i < argAmount; i++)
            {
                outArgs[i] = args.TryPop(out double arg)
                    ? arg
                    : throw new ArgumentAmountException(i);
            }

            return outArgs;
        }

        internal static double Evaluate(List<object> expr)
        {
            Stack<double> operands = new();
            double[] args;

            for (int i = 0; i < expr.Count; i++)
            {
                try
                {
                    switch (expr[i])
                    {
                        case double number:
                            operands.Push(number);
                            break;
                        case AdditiveOperator additive:
                            args = PopArgs(operands, 1);
                            operands.Push(additive.Operation(args[0]));
                            break;
                        case BinaryOperator bop:
                            args = PopArgs(operands, 2);
                            operands.Push(bop.Operation(args[1], args[0]));
                            break;
                        case Funcall func:
                            switch (func.Func)
                            {
                                case Func<double, double> func1:
                                    args = PopArgs(operands, 1);
                                    operands.Push(func1(args[0]));
                                    break;
                                case Func<double, double, double> func2:
                                    args = PopArgs(operands, 2);
                                    operands.Push(func2(args[1], args[0]));
                                    break;
                                case Func<double, double, double, double> func3:
                                    args = PopArgs(operands, 3);
                                    operands.Push(func3(args[2], args[1], args[0]));
                                    break;
                                case Func<double[], double> vfunc:
                                    args = PopArgs(operands, func.ArgAmount).Reverse().ToArray();
                                    operands.Push(vfunc(args));
                                    break;
                                default:
                                    throw new UnreachableException(
                                        $"Unknown type of function '{expr[i].GetType()}'"
                                    );
                            }
                            break;
                        default:
                            throw new UnreachableException($"Unknown expression '{expr[i]}'");
                    }
                }
                catch (ArgumentAmountException e)
                {
                    throw expr[i] switch
                    {
                        Funcall func
                            => new ArgumentException(
                                $"{func.Name}() is missing arguments, expected {func.ArgAmount} received {e.Received}"
                            ),
                        AdditiveOperator additive
                            => new ArgumentException(
                                $"Additive operator '{additive.Op}' is missing its operand"
                            ),
                        BinaryOperator bop
                            => e.Received == 1
                                ? new ArgumentException(
                                    $"binary operator '{bop.Op}' is missing a right operand"
                                )
                                : new ArgumentException(
                                    $"binary operator '{bop.Op}' is missing operands"
                                ),
                        _
                            => new UnreachableException(
                                $"Unknow expression '{e.GetType()}' is missing operands"
                            ),
                    };
                }
            }

            return operands.Pop();
        }

        public static double Evaluate(string expr)
        {
            Lexer lexer = new(expr);
            return Evaluate(Parser.Parse(ref lexer));
        }
    }
}
