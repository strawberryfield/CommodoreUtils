//-----------------------------------------------------------------------
// <copyright file="CommandLineHelpers.cs" company="Casasoft">
//     Author: Roberto Ceccarelli (http://strawberryfield.altervista.org)
//     Copyright (c) 2025-2026 All rights reserved.
// </copyright>
//
// This file is part of Casasoft Commodore Utils
// https://github.com/strawberryfield/CommodoreUtils
//
// Casasoft Commodore Utils is free software:
// you can redistribute it and/or modify it
// under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Casasoft Commodore Utils is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY
//-----------------------------------------------------------------------

namespace Casasoft.Helpers;

/// <summary>
/// Provides helper methods for command line parameter parsing.
/// </summary>
public static class CommandLineHelpers
{
    /// <summary>
    /// Parse an integer value from a string accepting decimal or hexadecimal notation.
    /// </summary>
    /// <param name="val">
    /// The input string to parse. Supported formats:
    /// - Decimal (for example: "123")
    /// - Hexadecimal prefixed with "0x" (for example: "0xC000")
    /// - Hexadecimal prefixed with '$' (for example: "$C000")
    /// Hex digits are case-insensitive.
    /// </param>
    /// <param name="fallback">
    /// The value to return when parsing fails. When an error occurs the provided
    /// <paramref name="message"/> will be written to <see cref="System.Console.Error"/>
    /// and this <paramref name="fallback"/> value will be returned.
    /// </param>
    /// <param name="message">
    /// A composite format string used to report parsing errors. The original input
    /// <paramref name="val"/> is supplied as the first format argument (i.e. {0}).
    /// Example: "Invalid value: {0}".
    /// </param>
    /// <returns>
    /// The parsed integer value if parsing succeeds; otherwise the specified
    /// <paramref name="fallback"/> value.
    /// </returns>
    /// <remarks>
    /// Hexadecimal parsing uses <see cref="System.Globalization.NumberStyles.HexNumber"/>
    /// and the TryParse overload that accepts a <see cref="System.ReadOnlySpan{Char}"/>.
    /// The method writes a formatted error message to standard error on failure and does not throw
    /// for parse errors. This method will throw a <see cref="System.NullReferenceException"/>
    /// if <paramref name="val"/> is null because it calls <see cref="string.StartsWith(string, StringComparison)"/>
    /// </remarks>
    public static int GetIntParameter(string val, int fallback, string message)
    {
        int ret;
        void WriteError()
        {
            Console.Error.WriteLine(string.Format(message, val));
            ret = fallback;
        }

        if (val.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(val.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out ret))
            {
                WriteError();
            }
        }
        else if (val.StartsWith("$"))
        {
            if (!int.TryParse(val.AsSpan(1), System.Globalization.NumberStyles.HexNumber, null, out ret))
            {
                WriteError();
            }
        }
        else if (!int.TryParse(val, out ret))
        {
            WriteError();
        }
        return ret;
    }

    /// <summary>
    /// Parse a floating point (decimal/double) value from a string, accepting indifferently
    /// the comma (',') or the dot ('.') as decimal separator.
    /// </summary>
    /// <param name="val">
    /// The input string to parse. Both "0,35" and "0.35" style notations are accepted, regardless
    /// of the current culture, since the value is normalized before parsing.
    /// </param>
    /// <param name="fallback">
    /// The value to return when parsing fails. When an error occurs the provided
    /// <paramref name="message"/> will be written to <see cref="System.Console.Error"/>
    /// and this <paramref name="fallback"/> value will be returned.
    /// </param>
    /// <param name="message">
    /// A composite format string used to report parsing errors. The original input
    /// <paramref name="val"/> is supplied as the first format argument (i.e. {0}).
    /// Example: "Invalid value: {0}".
    /// </param>
    /// <returns>
    /// The parsed <see cref="double"/> value if parsing succeeds; otherwise the specified
    /// <paramref name="fallback"/> value.
    /// </returns>
    /// <remarks>
    /// The comma character is always replaced with a dot before parsing, and parsing is performed
    /// using <see cref="System.Globalization.CultureInfo.InvariantCulture"/>, so the method behaves
    /// consistently regardless of the host system's regional settings and accepts either decimal
    /// separator. This method writes a formatted error message to standard error on failure and
    /// does not throw for parse errors. It will throw a <see cref="System.NullReferenceException"/>
    /// if <paramref name="val"/> is null.
    /// </remarks>
    public static double GetDoubleParameter(string val, double fallback, string message)
    {
        string normalized = val.Replace(',', '.');
        if (!double.TryParse(normalized, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double ret))
        {
            Console.Error.WriteLine(string.Format(message, val));
            ret = fallback;
        }
        return ret;
    }

    /// <summary>
    /// Human-readable note explaining accepted integer parameter formats.
    /// </summary>
    public static string HexParameterNote { get; } = @"Integer parameters can be in decimal or hex form.
An hex value must be prefixed by '0x' or '$' (ie: 0xc000 or $C000)
Letters are case-insensitive.";
}