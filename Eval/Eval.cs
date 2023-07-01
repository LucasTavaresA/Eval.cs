﻿// Licensed under the LGPL3 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using static Eval.Globals;

namespace Eval;

public ref struct Lexer
{
    public readonly ReadOnlySpan<char> Input { get; }
    public int Index { get; private set; }
    public int NextIndex { get; private set; }
    private char Char { get; set; }

    public Lexer(ReadOnlySpan<char> input)
    {
        Input =
            input == null
                ? throw new InvalidOperationException($"Expression cannot be null")
                : input.ToString() == ""
                    ? throw new InvalidOperationException($"Expression cannot be empty")
                    : input;

        NextChar();
    }

    public void NextChar()
    {
        Char = NextIndex >= Input.Length ? '\0' : Input[NextIndex];
        Index = NextIndex;
        NextIndex += 1;
    }

    private readonly char PeekChar()
    {
        return NextIndex >= Input.Length ? '\0' : Input[NextIndex];
    }

    private void SkipSpace()
    {
        while (char.IsWhiteSpace(Char))
        {
            NextChar();
        }
    }

    public ReadOnlySpan<char> ReadSymbol()
    {
        var pos = Index;

        while (char.IsAsciiLetter(Char) || Char == '.')
        {
            NextChar();
        }

        return Input[pos..Index];
    }

    public ReadOnlySpan<char> ReadNumber()
    {
        var pos = Index;

        while (char.IsAsciiDigit(Char) || Char == '.')
        {
            NextChar();
        }

        return Input[pos..Index];
    }

    /// <summary>
    /// Get the next token and move to the next index
    /// </summary>
    public Token NextToken()
    {
        SkipSpace();

        if (char.IsAsciiDigit(Char))
        {
            var number = ReadNumber();
            var additive = PeekChar();

            // Handle scientific notation
            if (Char is 'E' or 'e')
            {
                if (additive is '+' or '-')
                {
                    NextChar();
                    NextChar();
                    return new(TokenKind.Number, $"{number}e{additive}{ReadNumber()}");
                }
                else
                {
                    throw new InvalidExpressionException(
                        "Scientific notation cannot have space",
                        Input.ToString(),
                        NextIndex,
                        1
                    );
                }
            }

            return new(TokenKind.Number, number.ToString());
        }

        if (char.IsAsciiLetter(Char))
        {
            var symbol = ReadSymbol().ToString();

            return Char switch
            {
                '(' => new(TokenKind.Function, symbol),
                _ => new(TokenKind.Variable, symbol),
            };
        }

        var ch = Char.ToString();
        var doubleChar = ch + PeekChar();

        if (Tokens.TryGetValue(doubleChar, out var tokenKind))
        {
            NextChar();
            NextChar();
            return new(tokenKind, doubleChar);
        }
        else
        {
            if (Tokens.TryGetValue(ch, out tokenKind))
            {
                NextChar();
                return new(tokenKind, ch);
            }
            else
            {
                NextChar();
                return new(TokenKind.Illegal, ch);
            }
        }
    }
}

internal struct Parser
{
    private static string RemovePrefix(string symbol)
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
            if (token.Kind < TokenKind.Number)
            {
                var op = Operators[token.Kind];

                while (
                    operators.Count > 0
                    && (
                        // additive
                        operators.Peek() is Delegate
                        || (
                            operators.Peek() is Operator next
                            && op.Precedence <= next.Precedence
                        )
                    )
                )
                {
                    output.Add(operators.Pop());
                }

                operators.Push(op);

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
            else if (token.Kind == TokenKind.Number)
            {
                if (
                    double.TryParse(
                        token.Literal,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var number
                    )
                )
                {
                    output.Add(number);
                    token = lexer.NextToken();
                }
            }
            else if (token.Kind == TokenKind.Variable)
            {
                if (Variables.TryGetValue(RemovePrefix(token.Literal), out var variable))
                {
                    output.Add(variable);
                    token = lexer.NextToken();
                }
                else
                {
                    throw new InvalidExpressionException(
                        $"Invalid variable: '{token}'",
                        lexer.Input.ToString(),
                        lexer.Index - token.Literal.Length,
                        token.Literal.Length
                    );
                }
            }
            else if (token.Kind == TokenKind.Function)
            {
                if (Functions.TryGetValue(RemovePrefix(token.Literal), out var funcall))
                {
                    args.Push(1);
                    funcall.Offset = lexer.Index - token.Literal.Length;
                    operators.Push(funcall);
                    token = lexer.NextToken();
                }
                else
                {
                    throw new InvalidExpressionException(
                        $"Invalid function: '{token}'",
                        lexer.Input.ToString(),
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
                        lexer.Input.ToString(),
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
                        lexer.Input.ToString(),
                        lexer.Index - 1,
                        1
                    );
                }

                if (operators.TryPeek(out var op) && op is Function function)
                {
                    _ = operators.Pop();
                    var received = args.Pop();

                    if (function.Args == 0)
                    {
                        function.Args = received;
                    }
                    else if (function.Args != received)
                    {
                        throw new ArgumentAmountException(
                            lexer.Input.ToString(),
                            function.Name,
                            function.Args,
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
                    lexer.Input.ToString(),
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
                    lexer.Input.ToString(),
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
                case Operator op:
                    args = PopArgs(operands, 2);
                    operands.Push(op.Operation(args[1], args[0]));
                    break;
                case Function function:
                    switch (function.Funcall)
                    {
                        case Func<double, double> f1:
                            args = PopArgs(operands, 1);
                            operands.Push(f1(args[0]));
                            break;
                        case Func<double, double, double> f2:
                            args = PopArgs(operands, 2);
                            operands.Push(f2(args[1], args[0]));
                            break;
                        case Func<double, double, double, double> f3:
                            args = PopArgs(operands, 3);
                            operands.Push(f3(args[2], args[1], args[0]));
                            break;
                        case Func<double[], double> vf:
                            args = PopArgs(operands, function.Args);
                            Array.Reverse(args);
                            operands.Push(vf(args));
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
            : throw new UnexpectedEvaluationException($"Evaluation ended with no results");
    }

    public static double Evaluate(string expr)
    {
        var lexer = new Lexer(expr);
        return Evaluate(Parser.Parse(ref lexer));
    }
}
