// Licensed under the LGPL3 license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Eval
{
    internal readonly struct Ascii
    {
        /// <summary>
        /// Checks if the character is a letter
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsAsciiLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        /// <summary>
        /// Checks if the character is a digit
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsAsciiDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// Checks if the character is a whitespace
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c >= '\t' && c <= '\r';
        }

        /// <summary>
        /// Checks if the character is a letter or digit
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsAlphaNumeric(char c)
        {
            return IsAsciiLetter(c) || IsAsciiDigit(c);
        }
    }
}
