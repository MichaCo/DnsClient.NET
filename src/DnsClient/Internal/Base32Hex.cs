using System;
using System.Linq;

namespace DnsClient.Internal
{
    /// <summary>
    /// Base32 encoder with the extended hey alphabet
    /// </summary>
    /// <remarks>
    /// See https://datatracker.ietf.org/doc/html/rfc4648#section-7
    /// <![CDATA[
    ///              Table 4: The "Extended Hex" Base 32 Alphabet
    ///
    ///     Value Encoding  Value Encoding  Value Encoding  Value Encoding
    ///         0 0             9 9            18 I            27 R
    ///         1 1            10 A            19 J            28 S
    ///         2 2            11 B            20 K            29 T
    ///         3 3            12 C            21 L            30 U
    ///         4 4            13 D            22 M            31 V
    ///         5 5            14 E            23 N
    ///         6 6            15 F            24 O         (pad) =
    ///         7 7            16 G            25 P
    ///         8 8            17 H            26 Q
    ///
    /// ]]>
    /// </remarks>
    /// <seealso href="https://datatracker.ietf.org/doc/html/rfc4648#section-7">RFC4648</seealso>
    public static class Base32Hex
    {
        /// <summary>
        /// Converts the specified string, which encodes binary data as base-32 digits
        /// using the extended hex alphabet, to an equivalent 8-bit unsigned integer array.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <returns>An array of 8-bit unsigned integers that is equivalent to <paramref name="input"/>.</returns>
        public static byte[] FromBase32HexString(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (input.Length == 0)
            {
                return new byte[0];
            }

            input = input.TrimEnd('=');
            var byteCount = input.Length * 5 / 8;
            var result = new byte[byteCount];
            byte currentByte = 0, bitsRemaining = 8;
            var arrayIndex = 0;
            foreach (var value in input.Select(CharToValue))
            {
                int mask;
                if (bitsRemaining > 5)
                {
                    mask = value << (bitsRemaining - 5);
                    currentByte = (byte)(currentByte | mask);
                    bitsRemaining -= 5;
                }
                else
                {
                    mask = value >> (5 - bitsRemaining);
                    currentByte = (byte)(currentByte | mask);
                    result[arrayIndex++] = currentByte;
                    unchecked
                    {
                        currentByte = (byte)(value << (3 + bitsRemaining));
                    }
                    bitsRemaining += 3;
                }
            }
            if (arrayIndex != byteCount)
            {
                result[arrayIndex] = currentByte;
            }

            return result;
        }

        /// <summary>
        /// Converts an array of 8-bit unsigned integers to its equivalent string
        /// representation that is encoded with base-32 digits using the extended hex alphabet.
        /// </summary>
        /// <param name="input">An array of 8-bit unsigned integers.</param>
        /// <returns>The string representation in base 32 hex of <paramref name="input"/>.</returns>
        public static string ToBase32HexString(byte[] input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (input.Length == 0)
            {
                return string.Empty;
            }

            var charCount = (int)Math.Ceiling(input.Length / 5d) * 8;
            var result = new char[charCount];
            byte nextChar = 0, bitsRemaining = 5;
            var arrayIndex = 0;
            foreach (var b in input)
            {
                nextChar = (byte)(nextChar | (b >> (8 - bitsRemaining)));
                result[arrayIndex++] = ValueToChar(nextChar);
                if (bitsRemaining < 4)
                {
                    nextChar = (byte)((b >> (3 - bitsRemaining)) & 31);
                    result[arrayIndex++] = ValueToChar(nextChar);
                    bitsRemaining += 5;
                }
                bitsRemaining -= 3;
                nextChar = (byte)((b << bitsRemaining) & 31);
            }
            if (arrayIndex == charCount)
            {
                return new string(result);
            }

            result[arrayIndex++] = ValueToChar(nextChar);
            while (arrayIndex != charCount)
            {
                result[arrayIndex++] = '=';
            }

            return new string(result);
        }

        private static int CharToValue(char c)
        {
            var value = c;
            if (value <= 58 && value >= 48)
            {
                return value - 48;
            }
            if (value <= 86 && value >= 65)
            {
                return value - 55;
            }

            throw new ArgumentException("Character is not a Base32 character.", nameof(c));
        }

        private static char ValueToChar(byte b)
        {
            if (b < 10)
            {
                return (char)(b + 48);
            }
            if (b <= 32)
            {
                return (char)(b + 55);
            }

            throw new ArgumentException("Byte is not a value Base32 value.", nameof(b));
        }
    }
}
