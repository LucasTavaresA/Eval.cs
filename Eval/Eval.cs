// Licensed under the LGPL3 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using static Eval.Globals;

namespace Eval;

public unsafe struct Lexer
{
    private readonly char* SrcPtr;
    public readonly string Src { get; }
    public int Index { get; private set; }

    public Lexer(string src)
    {
        if (src == "")
        {
            throw new InvalidOperationException($"Expression cannot be empty");
        }
        else if (src == null)
        {
            throw new InvalidOperationException($"Expression cannot be null");
        }

        Src = src;

        fixed (char* ptr = src)
        {
            SrcPtr = ptr;
        }
    }

    private static int SkipSpace(char* src, int index = 0)
    {
        while (src[index] == ' ')
        {
            index++;
        }

        return index;
    }

    /// <summary>
    /// Get the next token and move to the next index
    /// </summary>
    public Token NextToken()
    {
        var index = SkipSpace(SrcPtr, Index);
        var nextIndex = SkipSpace(SrcPtr, index + 1);

        var src = SrcPtr + index;

        if (SrcPtr[index] == '\0')
        {
            return new(TokenKind.End, "");
        }

        var next = SrcPtr[nextIndex];

        Token? token = SrcPtr[index] switch
        {
            '\0' => new(TokenKind.End, ""),
            '(' => new(TokenKind.OpenParen, "("),
            ')' => new(TokenKind.CloseParen, ")"),
            ',' => new(TokenKind.Comma, ","),
            '+'
                => next switch
                {
                    '-' => new(TokenKind.Minus, "-"),
                    '+' => new(TokenKind.Plus, "++"),
                    _ => new(TokenKind.Plus, "+"),
                },
            '-'
                => next switch
                {
                    '+' => new(TokenKind.Minus, "-+"),
                    _ => new(TokenKind.Minus, "-"),
                },
            '*'
                => next switch
                {
                    '+' => new(TokenKind.Multiply, "*+"),
                    _ => new(TokenKind.Multiply, "*"),
                },
            '/'
                => next switch
                {
                    '+' => new(TokenKind.Divide, "/+"),
                    _ => new(TokenKind.Divide, "/"),
                },
            '%'
                => next switch
                {
                    '+' => new(TokenKind.Modulo, "%+"),
                    _ => new(TokenKind.Modulo, "%"),
                },
            '^' => new(TokenKind.Exponent, "^"),
            '<'
                => next switch
                {
                    '<' => new(TokenKind.ShiftLeft, "<<"),
                    _
                        => throw new InvalidExpressionException(
                            $"Invalid operator '<{next}'",
                            Src,
                            index,
                            2
                        ),
                },
            '>'
                => next switch
                {
                    '>' => new(TokenKind.ShiftRight, ">>"),
                    _
                        => throw new InvalidExpressionException(
                            $"Invalid operator '>{next}'",
                            Src,
                            index,
                            2
                        ),
                },
            _ => null,
        };

        if (token is Token t)
        {
            Index = t.Literal.Length + index;
            return t;
        }

        var count = 0;

        while (src[count] != '\0' && (char.IsAsciiLetterOrDigit(src[count]) || src[count] == '.'))
        {
            count++;
        }

        if (count == 0)
        {
            throw new InvalidExpressionException(
                $"Invalid character '{src[count]}'",
                Src,
                index,
                1
            );
        }

        // Handle scientific notation
        if (src[count - 1] is 'E' or 'e' && src[count] is '+' or '-')
        {
            count++;

            while (src[count] != '\0' && char.IsAsciiDigit(src[count]))
            {
                count++;
            }
        }

        Index = index + count;
        return new(TokenKind.Symbol, new string(SrcPtr, index, count));
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

    private record struct Paren(string Kind, int Pos);

    internal static List<object> Parse(ref Lexer lexer)
    {
        var operators = new Stack<object>();
        var output = new List<object>();
        var args = new Stack<int>();
        var token = lexer.NextToken();

        // expression starts with additive
        if (token.Kind == TokenKind.Minus)
        {
            operators.Push(Negative);
            token = lexer.NextToken();
        }
        else if (token.Kind == TokenKind.Plus)
        {
            token = lexer.NextToken();
        }

        while (token.Kind != TokenKind.End)
        {
            if (token.Kind < TokenKind.Symbol)
            {
                var binaryOperator = BinaryOperators[token.Kind];

                while (
                    operators.Count > 0
                    && (
                        // additive
                        operators.Peek() is Delegate
                        || (
                            operators.Peek() is BinaryOperator bop
                            && binaryOperator.Precedence <= bop.Precedence
                        )
                    )
                )
                {
                    output.Add(operators.Pop());
                }

                operators.Push(binaryOperator);

                token = lexer.NextToken();

                // additive in front of a binary operator
                if (token.Kind == TokenKind.Minus)
                {
                    operators.Push(Negative);
                    token = lexer.NextToken();
                }
                else if (token.Kind == TokenKind.Plus)
                {
                    token = lexer.NextToken();
                }
            }
            else if (token.Kind == TokenKind.Symbol)
            {
                var symbol = CleanSymbol(token.Literal);

                if (
                    double.TryParse(
                        token.Literal,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var number
                    ) || Variables.TryGetValue(symbol, out number)
                )
                {
                    output.Add(number);
                    token = lexer.NextToken();
                }
                else if (Functions.TryGetValue(symbol, out var funcall))
                {
                    args.Push(1);
                    funcall.Offset = lexer.Index - token.Literal.Length;
                    operators.Push(funcall);
                    token = lexer.NextToken();
                }
                else
                {
                    throw new InvalidExpressionException(
                        $"Invalid variable or function: '{token}'",
                        lexer.Src,
                        lexer.Index - token.Literal.Length,
                        token.Literal.Length
                    );
                }
            }
            else if (token.Kind == TokenKind.OpenParen)
            {
                // used to check if inside parens later
                operators.Push(new Paren(token.Literal, lexer.Index));
                token = lexer.NextToken();

                // expression inside parens starts with additive
                if (token.Kind == TokenKind.Minus)
                {
                    operators.Push(Negative);
                    token = lexer.NextToken();
                }
                else if (token.Kind == TokenKind.Plus)
                {
                    token = lexer.NextToken();
                }
                else if (token.Kind == TokenKind.CloseParen)
                {
                    throw new InvalidExpressionException(
                        "Empty parens",
                        lexer.Src,
                        lexer.Index - 2,
                        2
                    );
                }
            }
            else if (token.Kind is TokenKind.Comma)
            {
                while (operators.Count > 0 && operators.Peek() is not Paren)
                {
                    output.Add(operators.Pop());
                }

                args.Push(args.Pop() + 1);

                token = lexer.NextToken();

                // argument starts with additive
                if (token.Kind == TokenKind.Minus)
                {
                    operators.Push(Negative);
                    token = lexer.NextToken();
                }
                else if (token.Kind == TokenKind.Plus)
                {
                    token = lexer.NextToken();
                }
            }
            else if (token.Kind is TokenKind.CloseParen)
            {
                while (operators.Count > 0 && operators.Peek() is not Paren)
                {
                    output.Add(operators.Pop());
                }

                // discard (
                if (!operators.TryPop(out _))
                {
                    throw new InvalidExpressionException(
                        "Closing unexsistent paren",
                        lexer.Src,
                        lexer.Index - 1,
                        1
                    );
                }

                if (operators.TryPeek(out var op) && op is Funcall function)
                {
                    _ = operators.Pop();
                    var received = args.Pop();

                    if (function.ArgAmount == 0)
                    {
                        function.ArgAmount = received;
                    }
                    else if (function.ArgAmount != received)
                    {
                        throw new ArgumentAmountException(
                            lexer.Src,
                            function.Name,
                            function.ArgAmount,
                            received,
                            function.Offset,
                            lexer.Index
                        );
                    }

                    output.Add(function);
                }

                token = lexer.NextToken();
            }
            else
            {
                throw new InvalidExpressionException(
                    $"Invalid token: '{token}'",
                    lexer.Src,
                    lexer.Index - token.Literal.Length,
                    token.Literal.Length
                );
            }
        }

        foreach (var op in operators)
        {
            if (op is Paren paren)
            {
                throw new InvalidExpressionException(
                    "Opened paren is not closed",
                    lexer.Src,
                    paren.Pos - 1,
                    1
                );
            }
            else
            {
                output.Add(op);
            }
        }

        return output;
    }
}

public struct Evaluator
{
    private static double[] PopArgs(Stack<double> args, int argAmount)
    {
        var outArgs = new double[argAmount];

        for (var i = 0; i < argAmount; i++)
        {
            outArgs[i] = args.TryPop(out var arg)
                ? arg
                : throw new UnexpectedEvaluationException($"Lack of operands");
        }

        return outArgs;
    }

    internal static double Evaluate(List<object> expr)
    {
        var operands = new Stack<double>();
        double[] args;

        for (var i = 0; i < expr.Count; i++)
        {
            switch (expr[i])
            {
                case double number:
                    operands.Push(number);
                    break;
                case Func<double, double> negative:
                    args = PopArgs(operands, 1);
                    operands.Push(negative(args[0]));
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
                            args = PopArgs(operands, func.ArgAmount);
                            Array.Reverse(args);
                            operands.Push(vfunc(args));
                            break;
                        default:
                            throw new UnexpectedEvaluationException(
                                $"Unknown type of function '{expr[i].GetType()}'"
                            );
                    }
                    break;
                default:
                    throw new UnexpectedEvaluationException($"Unknown expression '{expr[i]}'");
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
        var lexer = new Lexer(expr);
        return Evaluate(Parser.Parse(ref lexer));
    }
}
