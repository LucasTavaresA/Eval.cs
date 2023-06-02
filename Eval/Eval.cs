// Licensed under the LGPL3 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using static Eval.Globals;

namespace Eval;

public struct Lexer
{
    public string Src { get; private set; }
    public int Index { get; private set; } = 0;

    public Lexer(string src)
    {
        if (src == "")
        {
            throw new InvalidOperationException($"Expression cannot be empty");
        }

        Src = src ?? throw new InvalidOperationException($"Expression cannot be null");
    }

    /// <summary>
    /// Get the next token and move to the next index
    /// </summary>
    public string Pop()
    {
        var nextIndex = Peek();
        var token = Src[Index..nextIndex].TrimStart();
        Index = nextIndex;
        return token;
    }

    /// <summary>
    /// Get the next token index
    /// </summary>
    public int Peek()
    {
        var index = Index + Src[Index..].TakeWhile(c => c == ' ').Count();
        var src = Src[index..];

        if (src == "")
        {
            return Src.Length;
        }

        var tokenIndex =
            Tokens
                .FirstOrDefault((t) => src.StartsWith(t, StringComparison.OrdinalIgnoreCase))
                ?.Length ?? src.TakeWhile((c) => char.IsLetterOrDigit(c) || c == '.').Count();

        if (tokenIndex < 1)
        {
            throw new InvalidOperationException($"Invalid Character: '{src[0]}'");
        }

        src = src[..tokenIndex];
        index += tokenIndex;

        // Handle scientific notation
        if (
            src.EndsWith("E", StringComparison.OrdinalIgnoreCase)
            && src.Length > 1
            && index + 1 < Src.Length
            && AdditiveOperators.ContainsKey(Src[index].ToString())
        )
        {
            index += Src[(index + 1)..].TakeWhile(char.IsDigit).Count() + 1;
        }

        return index;
    }
}

internal struct Parser
{
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
        var token = lexer.Pop();

        // expression starts with additive
        if (AdditiveOperators.TryGetValue(token, out var additive))
        {
            operators.Push(additive);
            token = lexer.Pop();
        }

        while (token != "")
        {
            if (
                double.TryParse(
                    token,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var number
                ) || Variables.TryGetValue(CleanSymbol(token), out number)
            )
            {
                output.Add(number);
                token = lexer.Pop();
            }
            else if (Functions.TryGetValue(CleanSymbol(token), out var func))
            {
                args.Push(1);
                operators.Push(func);
                token = lexer.Pop();
            }
            else if (BinaryOperators.TryGetValue(token, out var cbop))
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

        foreach (var o in operators)
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
        var outArgs = new double[argAmount];

        for (var i = 0; i < argAmount; i++)
        {
            outArgs[i] = args.TryPop(out var arg) ? arg : throw new ArgumentAmountException(i);
        }

        return outArgs;
    }

    internal static double Evaluate(List<object> expr)
    {
        Stack<double> operands = new();
        double[] args;

        for (var i = 0; i < expr.Count; i++)
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

        return operands.TryPop(out var result)
            ? result
            : throw new UnexpectedEvaluationException(
                $"Lack of operands when returning evaluation result"
            );
    }

    public static double Evaluate(string expr)
    {
        Lexer lexer = new(expr);
        return Evaluate(Parser.Parse(ref lexer));
    }
}
