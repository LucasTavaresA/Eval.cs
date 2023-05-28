using System;
using System.Linq;
using Eval;

double[] xs;
double[] xss;
double result;

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
    string tokens = "" + lexer.Pop();
    string next = lexer.Pop();

    while (next != "")
    {
        tokens += " " + next;
        next = lexer.Pop();
    }

    return tokens;
}

void TestExceptions(string expectedException, string expression)
{
    Console.WriteLine($"ExpectedException: {expectedException}");
    Console.WriteLine($"Expression: {expression}");
    try
    {
        Console.WriteLine($"Lexicons: {GetTokens(expression)}");
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        return;
    }
    finally
    {
        try
        {
            result = Evaluator.Evaluate(expression);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Separator();
        }
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
Test(23 + 2e-13 * 2.3D, "23 + 2e-13 * 2.3D");
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
Test(921.315D * -20.93D % 34.567D, "921.315D * -20.93D % 34.567D");
Test(1.6 * -6.7 / -9.6, "1.6*-6.7/- 9.6");
Test(9 >> (int)(3 / +1.2), " 9>>3  /+ 1.2");
Test(4 >> 8 >> 1, "  4 >>  8 >>  1");
Test(+(int)(5.7 * 4) << 6, "  +5.7  *4<<6");
Test((int)(6.7 / +3) >> 1, "6.7 /+ 3>>  1");
Test(-5 << 3, "-  5 << 3");
Test(7.9 / -0, "7.9/-0", "Lambda function makes this negative ");
Test(Math.Log(-42), "Math.Log(-42)", "Nan is always false ");

Separator("[Should error properly]");
Separator();

TestExceptions("Invalid function!", "avg(2, 3, 5)");
TestExceptions("Invalid variable!", "average(2, pie, 5)");
TestExceptions("Closing unexisting paren!", "6 +3) /5-+8%6 / 8 ^5 ^4 * 2*+1");
TestExceptions("Opened paren is not closed!", "(1 - Math.Pow(2, (1 + 2) * 3) * 3");
TestExceptions("Not closing early opened function!", "1 - Math.Pow(2, (1 + 2) * 3 * 3");
TestExceptions("scientific notation cannot have space", "2e +10");
TestExceptions("'$' is not a invalid character!", "(1 - Math.Pow($, (1 + 2)))");
TestExceptions("'#' is not a invalid character!", "(1 - Math#Pow(1, (1 + 2)))");
TestExceptions("More arguments than supported!", "Math.Pow(8, 4, -2, 5, 4)");
TestExceptions("Variadic received 0 arguments!", "last()");
TestExceptions("Less arguments than supported!", "Math.Pow(8)");
TestExceptions("Evaluating empty string ", "");

Separator("[GENERATED]");
Separator();
