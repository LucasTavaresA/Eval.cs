﻿// Licensed under the LGPL3 license.
// See the LICENSE file in the project root for more information.

using System;
using Eval;
using static Eval.Globals;

namespace ExprGen;

public struct Program
{
    private const int Generations = 100;

    public static void Main()
    {
        for (var i = 0; i < Generations; i++)
        {
            Console.WriteLine(Generators.Expression());
        }
    }
}

public struct Generators
{
    private record NOutOf(int N, int OutOf);

    private static readonly NOutOf NegativeChance = new(3, 10);
    private static readonly Random Random = new();
    private static readonly (int Min, int Max) ExprLength = (2, 4);
    private static readonly (int Min, int Max) SpaceLength = (0, 2);
    private static readonly (int Integer, int Decimal) NumberLength = (1, 1);

    private static bool Chance(NOutOf chance)
    {
        return Random.Next(chance.OutOf + 1) < chance.N;
    }

    private static string SpaceOut(string expr)
    {
        Lexer lexer = new(expr);
        var tokens = Space() + lexer.NextToken();
        var next = lexer.NextToken();

        while (next.Kind != TokenKind.End)
        {
            tokens += Space() + next;
            next = lexer.NextToken();
        }

        return tokens;
    }

    private static string Space()
    {
        return new string(' ', Random.Next(SpaceLength.Min, SpaceLength.Max + 1));
    }

    private static double Number()
    {
        return Math.Round(
            Random.NextDouble() * Math.Pow(10, NumberLength.Integer),
            NumberLength.Decimal
        );
    }

    private static string Negative()
    {
        return Chance(NegativeChance) ? "-" : "";
    }

    private static string Operator()
    {
        return BinaryOperatorsKeys[Random.Next(BinaryOperatorsKeys.Length)];
    }

    internal static string Expression()
    {
        var expression = Negative() + Number();

        for (var i = ExprLength.Min; i < Random.Next(ExprLength.Min, ExprLength.Max + 1); i++)
        {
            expression += Operator() + Number();
        }

        return SpaceOut(expression);
    }
}
