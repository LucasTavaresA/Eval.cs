// Licensed under the LGPL3 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using static Eval.Globals;

namespace Eval;

public struct Lexer
{
    public readonly string Src { get; }
    public int Index { get; private set; }
    private int NextIndex { get; set; }
    private char Ch { get; set; }

    public Lexer(string src)
    {
        Src = src == null
            ? throw new InvalidOperationException($"Expression cannot be null")
            : src == ""
                ? throw new InvalidOperationException($"Expression cannot be empty")
                : src;

        NextChar();
    }

    public void NextChar()
    {
        Ch = NextIndex >= Src.Length ? '\0' : Src[NextIndex];
        Index = NextIndex;
        NextIndex += 1;
    }

    public char PeekChar()
    {
        return NextIndex >= Src.Length ? '\0' : Src[NextIndex];
    }

    private void SkipSpace()
    {
        while (char.IsWhiteSpace(Ch))
        {
            NextChar();
        }
    }

    public ReadOnlySpan<char> ReadSymbol()
    {
        var pos = Index;

        while (char.IsAsciiLetter(Ch) || Ch == '.')
        {
            NextChar();
        }

        return Src.AsSpan()[pos..Index];
    }

    public ReadOnlySpan<char> ReadNumber()
    {
        var pos = Index;

        while (char.IsAsciiDigit(Ch) || Ch == '.')
        {
            NextChar();
        }

        return Src.AsSpan()[pos..Index];
    }

    /// <summary>
    /// Get the next token and move to the next index
    /// </summary>
    public Token NextToken()
    {
        SkipSpace();

        if (char.IsAsciiDigit(Ch))
        {
            var number = ReadNumber();
            var additive = PeekChar();

            // Handle scientific notation
            if (Ch is 'E' or 'e')
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
                        Src,
                        NextIndex,
                        1
                    );
                }
            }

            return new(TokenKind.Number, number.ToString());
        }

        if (char.IsAsciiLetter(Ch))
        {
            var symbol = ReadSymbol();

            return Ch switch
            {
                '(' => new(TokenKind.Function, symbol.ToString()),
                _ => new(TokenKind.Variable, symbol.ToString()),
            };
        }

        var nextChar = PeekChar();

        switch (Ch)
        {
            case '\0':
                NextChar();
                return new(TokenKind.End, "");
            case '(':
                NextChar();
                return new(TokenKind.OpenParen, "(");
            case ')':
                NextChar();
                return new(TokenKind.CloseParen, ")");
            case ',':
                NextChar();
                return new(TokenKind.Comma, ",");
            case '^':
                NextChar();
                return new(TokenKind.Exponent, "^");
            case '+':
                switch (nextChar)
                {
                    case '-':
                        NextChar();
                        NextChar();
                        return new(TokenKind.Minus, "+-");
                    case '+':
                        NextChar();
                        NextChar();
                        return new(TokenKind.Plus, "++");
                    default:
                        NextChar();
                        return new(TokenKind.Plus, "+");
                }
            case '-':
                switch (nextChar)
                {
                    case '-':
                        NextChar();
                        NextChar();
                        return new(TokenKind.Minus, "--");
                    case '+':
                        NextChar();
                        NextChar();
                        return new(TokenKind.Minus, "-+");
                    default:
                        NextChar();
                        return new(TokenKind.Minus, "-");
                }
            case '*':
                switch (nextChar)
                {
                    case '+':
                        NextChar();
                        NextChar();
                        return new(TokenKind.Multiply, "*+");
                    default:
                        NextChar();
                        return new(TokenKind.Multiply, "*");
                }
            case '/':
                switch (nextChar)
                {
                    case '+':
                        NextChar();
                        NextChar();
                        return new(TokenKind.Divide, "/+");
                    default:
                        NextChar();
                        return new(TokenKind.Divide, "/");
                }
            case '%':
                switch (nextChar)
                {
                    case '+':
                        NextChar();
                        NextChar();
                        return new(TokenKind.Modulo, "%+");
                    default:
                        NextChar();
                        return new(TokenKind.Modulo, "%");
                }
            case '<':
                switch (nextChar)
                {
                    case '<':
                        NextChar();
                        NextChar();
                        return new(TokenKind.ShiftLeft, "<<");
                    default:
                        throw new InvalidExpressionException(
                            $"Invalid operator '<{nextChar}'",
                            Src,
                            NextIndex,
                            2
                        );
                }
            case '>':
                switch (nextChar)
                {
                    case '>':
                        NextChar();
                        NextChar();
                        return new(TokenKind.ShiftRight, ">>");
                    default:
                        throw new InvalidExpressionException(
                            $"Invalid operator '>{nextChar}'",
                            Src,
                            NextIndex,
                            2
                        );
                }
            default:
                throw new InvalidExpressionException(
                    $"Invalid character '{Ch}'",
                    Src,
                    Index,
                    1
                );
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
                        lexer.Src,
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
                            lexer.Src,
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
            : throw new UnexpectedEvaluationException(
                $"Evaluation ended with no results"
            );
    }

    public static double Evaluate(string expr)
    {
        var lexer = new Lexer(expr);
        return Evaluate(Parser.Parse(ref lexer));
    }
}
