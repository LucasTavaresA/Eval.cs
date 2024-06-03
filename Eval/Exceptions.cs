// Licensed under the LGPL3 license.
// See the LICENSE file in the project root for more information.

using System;

namespace Eval
{
    public class UnexpectedEvaluationException : Exception
    {
        public UnexpectedEvaluationException(string message)
            : base(message)
        {
        }
    }

    public class InvalidExpressionException : InvalidOperationException
    {
        public string Src { get; }
        public int Offset { get; }
        public int Length { get; }

        public InvalidExpressionException(string message, string src, int offset, int length)
            : base(message)
        {
            Src = src;
            Offset = offset;
            Length = length;
        }
    }

    public class ArgumentAmountException : ArgumentException
    {
        public override string Message
            => $"{Function}() expects {Expected} arguments but received {Received}";
        public string Src { get; }
        public string Function { get; }
        public int Expected { get; }
        public int Received { get; }
        public int Offset { get; }
        public int Length { get; }

        public ArgumentAmountException(int received)
        {
            Received = received;
        }

        public ArgumentAmountException(
            string src,
            string function,
            int expected,
            int received,
            int offset,
            int length
        )
        {
            Src = src;
            Function = function;
            Expected = expected;
            Received = received;
            Offset = offset;
            Length = length;
        }
    }
}
