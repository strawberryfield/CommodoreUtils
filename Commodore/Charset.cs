//-----------------------------------------------------------------------
// <copyright file="Charset.cs" company="Casasoft">
//     Author: Roberto Ceccarelli (http://strawberryfield.altervista.org)
//     Copyright (c) 2025,2026 All rights reserved.
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

using System.Text;

namespace Casasoft.Commodore;

/// <summary>
/// Provides utilities for converting ASCII characters to PETSCII representation.
/// </summary>
/// <remarks>
/// PETSCII is the character encoding used by Commodore 8-bit computers (for example, the Commodore 64).
/// This static helper exposes conversion helpers in both directions (ASCII &lt;-&gt; PETSCII),
/// plus a few predicates used by the conversion routines.
/// </remarks>
public static class Charset
{
    #region filters
    /// <summary>
    /// Determines whether the specified character is a symbol or number in the PETSCII range.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>
    /// True if the character is a printable symbol or numeric-like character according to
    /// the PETSCII mapping range (between space and '@'), or if it is a common control
    /// character used in text streams (LF, CR, FF); otherwise false.
    /// </returns>
    /// <remarks>
    /// This predicate is used by <see cref="ASCII(char, bool)"/> to decide whether a PETSCII
    /// code should be forwarded as-is to the ASCII result.
    /// </remarks>
    public static bool IsSymbolOrNumber(char c) => (c <= '@' && c >= ' ') || c == 10 || c == 13 || c == 12;

    /// <summary>
    /// Determines whether the specified character is an extended symbol/number in PETSCII ranges.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character belongs to the extended symbol/number PETSCII ranges.</returns>
    /// <remarks>
    /// The check includes the basic PETSCII range and an extended range (0x80..0x9F) which
    /// can be used by some PETSCII variants for graphics and extended symbols.
    /// </remarks>
    public static bool IsExtendedSymbolOrNumber(char c) => (c <= '@') || (c <= 0x9f && c >= 0x80);

    /// <summary>
    /// Determines whether the specified character is an uppercase ASCII letter (A..Z).
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if <paramref name="c"/> is between 'A' and 'Z'.</returns>
    public static bool IsUpperCaseLetter(char c) => (c >= 'A' && c <= 'Z');

    /// <summary>
    /// Determines whether the specified character is an uppercase PETSCII letter in an alternate range.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if <paramref name="c"/> is in the PETSCII alternate uppercase range (0xC1..0xDA).</returns>
    /// <remarks>
    /// Some PETSCII modes store uppercase characters in a high-ASCII range; this predicate
    /// detects those alternate encodings so conversion routines can treat them correctly.
    /// </remarks>
    public static bool IsAlternateUpperCaseLetter(char c) => (c >= 0xC1 && c <= 0xDA);

    /// <summary>
    /// Determines whether the specified character is a lowercase ASCII letter (a..z).
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if <paramref name="c"/> is between 'a' and 'z'.</returns>
    public static bool IsLowerCaseLetter(char c) => (c >= 'a' && c <= 'z');
    #endregion

    #region ASCII to PETSCII conversion
    /// <summary>
    /// Represents a mapping between an ASCII character and its PETSCII equivalent.
    /// </summary>
    /// <param name="ASCII">The ASCII character.</param>
    /// <param name="PETSCII">The PETSCII string representation.</param>
    /// <remarks>
    /// The PETSCII representation is a string because some ASCII characters map to
    /// multi-character PETSCII sequences in this helper (for example accented letters).
    /// </remarks>
    public record class PETSCIIChar(char ASCII, string PETSCII);

    /// <summary>
    /// Table of special ASCII to PETSCII character mappings.
    /// </summary>
    /// <remarks>
    /// Use this table to handle characters that do not follow simple case conversions.
    /// Entries here are consulted when the character is not covered by the basic predicates.
    /// </remarks>
    public static readonly PETSCIIChar[] PETCharsTable =
    {
        new('[', "["),
        new(']', "]"),
        new('£', ((char) 0x5c).ToString()), // map pound sign to PETSCII 0x5C
        new('à', "A'"),
        new('è', "E'"),
        new('é', "E'"),
        new('ì', "I'"),
        new('ò', "O'"),
        new('ù', "U'"),
    };
    
    /// <summary>
    /// Converts an ASCII character to its PETSCII representation.
    /// </summary>
    /// <param name="c">The ASCII character to convert.</param>
    /// <param name="LowerCase">
    /// If true, instructs the converter to map uppercase letters into their lowercase PETSCII
    /// equivalents where applicable. When false, the converter preserves the PETSCII-native
    /// case mapping (PETSCII historically inverts ASCII case in some modes).
    /// </param>
    /// <returns>
    /// A string containing the PETSCII representation. The result may be:
    /// - a single character string when there is a one-to-one mapping, or
    /// - a multi-character string when the ASCII character is represented by a sequence
    ///   (for example, some accented letters).
    /// </returns>
    /// <remarks>
    /// The method follows these rules (high level):
    /// - If <paramref name="c"/> is in an extended symbol/number PETSCII range, it is returned as-is.
    /// - ASCII uppercase letters are either converted to lowercase (if <paramref name="LowerCase"/>)
    ///   or preserved.
    /// - ASCII lowercase letters are converted to uppercase PETSCII (since PETSCII historically
    ///   uses a different ordering).
    /// - If no direct rule applies, the <see cref="PETCharsTable"/> is consulted for special mappings.
    /// </remarks>
    public static string PETSCII(char c, bool LowerCase = false)
    {
        string ret = " ";
        if (IsExtendedSymbolOrNumber(c))
        {
            ret = c.ToString();
        }
        else if (IsUpperCaseLetter(c))
        {
            if (LowerCase)
                ret = ((char)(c + 32)).ToString();
            else
                ret = c.ToString();
        }
        else if (IsLowerCaseLetter(c))
        {
            ret = ((char)(c - 32)).ToString();
        }
        else
        {
            foreach (var item in PETCharsTable)
            {
                if (item.ASCII == c)
                {
                    ret = item.PETSCII;
                    break;
                }
            }
        }

        return ret;
    }

    /// <summary>
    /// Converts an ASCII string to its PETSCII representation.
    /// </summary>
    /// <param name="s">The ASCII string to convert. If null, an <see cref="ArgumentNullException"/> will be thrown by the caller.</param>
    /// <param name="LowerCase">
    /// If true, converts uppercase letters to lowercase PETSCII. When false,
    /// preserves the case mapping according to PETSCII rules.
    /// </param>
    /// <returns>The PETSCII string representation of the input string.</returns>
    /// <remarks>
    /// This method processes the input sequentially and concatenates each character's
    /// PETSCII representation (which may be multiple characters long).
    /// </remarks>
    public static string PETSCII(string s, bool LowerCase = false)
    {
        StringBuilder sb = new();
        foreach (char c in s)
        {
            sb.Append(PETSCII(c, LowerCase));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Converts an ASCII string into a PETSCII byte array.
    /// </summary>
    /// <param name="s">The ASCII string to convert.</param>
    /// <param name="LowerCase">If true, uppercase letters will be mapped to lowercase PETSCII equivalents.</param>
    /// <returns>
    /// Byte array where each element is the numeric PETSCII code of the corresponding output character.
    /// For multi-character PETSCII mappings the sequence of bytes for each mapped character is appended in order.
    /// </returns>
    public static byte[] PETSCIIBytes(string s, bool LowerCase = false)
    {
        List<byte> bytes = new();
        foreach (char c in s)
        {
            string petsciiStr = PETSCII(c, LowerCase);
            foreach (char pc in petsciiStr)
            {
                bytes.Add((byte)pc);
            }
        }
        return bytes.ToArray();
    }
    #endregion

    #region PETSCII to ASCII conversion
    /// <summary>
    /// Represents a mapping between an PETSCII character and its ASCII equivalent.
    /// </summary>
    /// <param name="PETSCII">The PETSCII character.</param>
    /// <param name="ASCII">The ASCII character.</param>
    public record class ASCIIChar(char PETSCII, char ASCII);

    /// <summary>
    /// Table of special PETSCII to ASCII character mappings.
    /// </summary>
    /// <remarks>
    /// Used to handle PETSCII codes that do not follow simple case arithmetic and need an explicit mapping.
    /// </remarks>
    public static readonly ASCIIChar[] ASCIICharsTable =
    {
        new('[', '['),
        new(']', ']'),
        new((char) 0x5c, '£'),
    };

    /// <summary>
    /// Converts a PETSCII character to its ASCII representation.
    /// </summary>
    /// <param name="c">The PETSCII character to convert.</param>
    /// <param name="LowerCase">
    /// If true, converts uppercase letters to lowercase ASCII. When false,
    /// preserves the original case mapping according to PETSCII rules.
    /// </param>
    /// <returns>The ASCII character representation of the PETSCII character.</returns>
    /// <remarks>
    /// Conversion rules at a glance:
    /// - Symbol/number PETSCII codes are forwarded as-is.
    /// - Uppercase PETSCII letters map to ASCII uppercase (or to ASCII lowercase when <paramref name="LowerCase"/> is true).
    /// - Alternate uppercase PETSCII range (0xC1..0xDA) is handled and can be mapped down to ASCII when requested.
    /// - Lowercase PETSCII letters are mapped to ASCII lowercase when <paramref name="LowerCase"/> is true.
    /// - If no rule applies, <see cref="ASCIICharsTable"/> is consulted for special mappings.
    /// </remarks>
    public static char ASCII(char c, bool LowerCase = false)
    {
        char ret = ' ';
        if (IsSymbolOrNumber(c))
        {
            ret = c;
        }
        else if (IsUpperCaseLetter(c))
        {
            if (LowerCase)
                ret = (char)(c + 32);
            else
                ret = c;
        }
        else if (IsAlternateUpperCaseLetter(c))
        {
            if (LowerCase)
                ret = (char)(c - 128);
            else
                ret = c;
        }
        else if (IsLowerCaseLetter(c))
        {
            if (LowerCase)
                ret = (char)(c - 32);
        }
        else
        {
            foreach (var item in ASCIICharsTable)
            {
                if (item.PETSCII == c)
                {
                    ret = item.ASCII;
                    break;
                }
            }
        }
        return ret;
    }

    /// <summary>
    /// Converts a PETSCII byte array to its ASCII string representation.
    /// </summary>
    /// <param name="s">The PETSCII byte array to convert. Each element is interpreted as a PETSCII code.</param>
    /// <param name="LowerCase">
    /// If true, converts uppercase letters to lowercase ASCII. When false,
    /// preserves the original case mapping according to PETSCII rules.
    /// </param>
    /// <returns>The ASCII string representation of the PETSCII byte array.</returns>
    /// <remarks>
    /// The input bytes are cast to <see cref="char"/> values and processed with <see cref="ASCII(char,bool)"/>.
    /// </remarks>
    public static string ASCII(byte[] s, bool LowerCase = false)
    {
        StringBuilder sb = new();
        foreach (char c in s.Select(v => (char)v))
        {
            sb.Append(ASCII(c, LowerCase));
        }
        return sb.ToString();
    }
    #endregion
}
