// Licensed under the LGPL3 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Eval;
using static Eval.Globals;

// generate csharp tests using exprgen
// $ ExprGen | EvalTest gen >> file.cs
if (args.Length > 0 && args[0] == "gen")
{
    string input;

    while ((input = Console.ReadLine()) != null)
    {
        Console.WriteLine($"""Test({input}, "{input}");""");
    }

    Environment.Exit(0);
}

double[] xs;
double[] xss;
double result;

static int Factorial(int n)
{
    return n == 0 ? 1 : Enumerable.Range(1, n).Aggregate(1, (acc, x) => acc * x);
}

double Percentage(double x, double y)
{
    return x * (y / 100);
}

void Separator(string header = "")
{
    string separator = new('-', (80 - header.Length) / 2);
    Console.WriteLine(separator + header + separator);
}

/// <summary>
/// Joins the tokens separated by the lexer
/// </summary>
string GetTokens(string expr)
{
    Lexer lexer = new(expr);
    string tokens = "" + lexer.NextToken();
    Token next = lexer.NextToken();

    while (next.Kind != TokenKind.End)
    {
        tokens += " " + next;
        next = lexer.NextToken();
    }

    return tokens;
}

static string Error(string message, string src, int offset, int length)
{
    if (length < 1)
    {
        return $"{message}";
    }

    string marker = new(' ', offset);

    if (length == 1)
    {
        marker += "^";
    }
    else if (length == 2)
    {
        marker += "^^";
    }
    else if (length > 2)
    {
        marker += $"^{new('~', length - 2)}^";
    }

    return $"{message}\n{src}\n{marker}";
}

void TestExceptions(string expectedException, string expression)
{
    Console.WriteLine($"ExpectedException: {expectedException}");
    Console.WriteLine($"Expression: {expression}");
    try
    {
        Console.WriteLine($"Lexicons: {GetTokens(expression)}");
        result = Evaluator.Evaluate(expression);
        Console.WriteLine("❌ TestExceptions() Should not be evaluated Correctly!");
        Environment.Exit(1);
    }
    catch (Exception e)
    {
        Console.WriteLine(
            e switch
            {
                InvalidExpressionException ie => Error(ie.Message, ie.Src, ie.Offset, ie.Length),
                ArgumentAmountException ae => Error(ae.Message, ae.Src, ae.Offset, ae.Length),
                _ => e.Message
            }
        );

        Separator();
    }
}

void Test(double expectedResult, string expression, string reason = "")
{
    Console.WriteLine($"Expression: {expression}");
    Console.WriteLine($"Lexicons: {GetTokens(expression)}");
    result = Evaluator.Evaluate(expression);
    Console.WriteLine($"Result: {expectedResult} = {result}");
    Console.WriteLine("Passed: " + (expectedResult == result ? "✅" : $"{reason}❌"));
    Separator();
}

// disable parens warnings
#pragma warning disable IDE0048
// disable formatting warnings
#pragma warning disable IDE0055

Separator();

Test(+42, "+42");
xs = new double[] { 2, 3, 5 };
Test(Math.Pow(-xs.Average(), -5), "Math.Pow(-average(2, 3, 5), -5)");
xs = new double[] { 2, 3, 5 };
Test(Math.Pow(-xs.Average(), 4), "Math.Pow(-average(2, 3, 5), 4)");
Test(42, "42");
Test(-42, "-42");
Test(-Math.PI, "-pi");
Test(1 + 1 - Math.PI, "1 + 1 - Math.PI");
Test(-Math.E + 1 - Math.PI, "-e + 1 - Math.PI");
Test(-1 + 1 - Math.PI, "-1 + 1 - Math.PI");
Test(-2 + Math.PI - Math.Ceiling(3.2), "-2 + Math.PI - Math.Ceiling(3.2)");
xs = new double[] { 2, 3, 5 };
Test(xs.Average(), "IEnumerable.Average(2, 3, 5)");
xs = new double[] { 2, 35, 5 };
Test(xs.Max(), "Max(2, 35, 5)");
Test(1 - 2 - 3, "1 - 2 - 3");
Test(1 - 2 * 3, "1 - 2 * 3");
Test((1 - 2) * 3, "(1 - 2) * 3");
Test(1 - Math.Log(10) * 3, "1 - Math.Log(10) * 3");
Test((1 - Math.Pow(2, (1 + 2) * 3)) * 3, "(1 - Math.Pow(2, (1 + 2) * 3)) * 3");
xs = new double[] { 2, 9, 5 };
Test((1 - xs.Average()) * 3, "(1 - IEnumerable.Average(2, (1 + 2) * 3, 5)) * 3");
Test(Math.BitIncrement(2.5) + 7.1 * Math.Floor(-7.4), "bitincrement(2.5) + 7.1 * floor(-7.4)");
Test(20e+3, "20e+3");
Test(23 + 2e-13 * 2.3, "23 + 2e-13 * 2.3");
Test(23 - 10e-3, "23 - 10e-3");
Test(19e-11 / -12, "19e-11 /- 12");
xss = new double[] { 4, 5, 1 };
xs = new double[] { 2, (1 + 2) * 3, 23, xss.Last(), 3 };
Test((1 - xs.Average()) * 3, "(1 - average(2, (1 + 2) * 3, 23, last(4, 5, 1), 3)) * 3");
xss = new double[] { 4, 5, 1 };
xs = new double[] { 2, 9, 23, xss.Last(), 3 };
Test((1 - xs.Average()) * 3, "(1 - average(2, 9, 23, last(4, 5, 1), 3)) * 3");
xs = new double[] { 1, 2 };
xss = new double[] { 4, xs.Last(), 5 };
Test(xss.Last(), "last(4, last(1, 2), 5)");
Test(Percentage(921.315 * -20.93, 34.567), "921.315 * -20.93 % 34.567");
Test(921.315 * (-20.93 % 34.567), "921.315 * mod(-20.93, 34.567)");
Test(Percentage(25, 200), "25%200");
Test(Percentage(200, 25), "200%25");
Test(1.6 * -6.7 / -9.6, "1.6*-6.7/- 9.6");
Test(9 >> (int)(3 / +1.2), " 9>>3  /+ 1.2");
Test(4 >> 8 >> 1, "  4 >>  8 >>  1");
Test(+(int)(5.7 * 4) << 6, "  +5.7  *4<<6");
Test((int)(6.7 / +3) >> 1, "6.7 /+ 3>>  1");
Test(-5 << 3, "-  5 << 3");
Test(4/.2, "4/.2");
Test((int)-.5 << 3, "-.5 << 3");
xs = new double[] { 1, 2 };
xss = new double[] { .4, xs.Last(), 5 };
Test(xss.Last(), "last(.4, last(1, 2), 5)");
xs = new double[] { 1, 2 };
xss = new double[] { 4, xs.Last(), .5 };
Test(xss.Last(), "last(4, last(1, 2), .5)");
Test(xss.Last(), "last(4., last(1, 2), .5)");
Test(xss.Last(), "last(4, last(1, 2.), .5)");
Test(7.9 / -0, "7.9/-0", "Lambda function makes this negative ");
Test(Math.Log(-42), "Math.Log(-42)", "Nan is always false ");
Test(Math.Pow(4, 7), "4 ^ 7");
Test((1 - Math.Pow(2, (1 + 2) * 3)) * 3, "(1 - (2 ^ ((1 + 2) * 3))) * 3");
Test(Math.Pow(-new double[] { 2, 3, 5 }.Average(), -5), "(-average(2, 3, 5)^ -5)");
Test(Math.Pow(-new double[] { 2, 3, 5 }.Average(), 4), "(-average(2, 3, 5)^ 4)");
Test(Factorial(5), "5!");
Test(Factorial(0), "0!");
Test(Factorial(1), "1!");
Test(-Factorial(0), "-0!");
Test(-Factorial(5), "-5!");
Test(Factorial(15), "15!");
Test(1 + 1 - Factorial((int)Math.PI), "1 + 1 - Math.PI!");
Test(-2 + Math.PI - Factorial((int)Math.Ceiling(3.2)), "-2 + Math.PI - Math.Ceiling(3.2)!");
Test(23 + Factorial((int)2e-13) * 2.3, "23 + 2e-13! * 2.3");

// TODO(LucasTA): try fuzzing to catch more edge cases

Separator("[Should error properly]");
Separator();

TestExceptions("Lack of operands!", "-");
TestExceptions("Lack of operands!", "+");
TestExceptions("Lack of operands!", "(9)+");
TestExceptions("Invalid function!", "avg(2, 3, 5)");
TestExceptions("Invalid variable!", "average(2, pie, 5)");
TestExceptions("Closing unexisting paren!", "6 +3) /5-+8%6 / 8 ^5 ^4 * 2*+1");
TestExceptions("Opened paren is not closed!", "(1 - Math.Pow(2, (1 + 2) * 3) * 3");
TestExceptions("Not closing early opened function!", "1 - Math.Pow(2, (1 + 2) * 3 * 3");
TestExceptions("scientific notation cannot have space", "2e +10");
TestExceptions("'$' is not a valid character!", "(1 - Math.Pow($, (1 + 2)))");
TestExceptions("More arguments than supported!", "Math.Pow(8, 4, -2, 5, 4)");
TestExceptions("Empty parens!", "last()");
TestExceptions("Empty parens!", "4 /+ 2 * last() + 3");
TestExceptions("Less arguments than supported!", "Math.Pow(8)");
TestExceptions("Evaluating empty string ", "");
TestExceptions("Evaluating null string ", null);
TestExceptions("Invalid number!", "4.2.0");
TestExceptions("Invalid number!", "4..");
TestExceptions("Lack of operator after function!", "Math.Pow(8, 7)6");

Separator("[GENERATED]");
Separator();
