//-----------------------------------------------------------------------
// <copyright file="Charset.cs" company="Casasoft">
//     Author: Roberto Ceccarelli (http://strawberryfield.altervista.org)
//     Copyright (c) 2025 All rights reserved.
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

namespace Casasoft.Commodore;

/// <summary>
/// Provides utilities for converting ASCII characters to PETSCII representation.
/// </summary>
public static class Charset
{
    #region filters
    /// <summary>
    /// Determines whether the specified character is a symbol or number in the PETSCII range.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is a symbol or number; otherwise, false.</returns>
    public static bool IsSymbolOrNumber(char c) => (c >= ' ' && c <= '@') || c == 10 || c == 13;

    /// <summary>
    /// Determines whether the specified character is an uppercase ASCII letter.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is an uppercase letter; otherwise, false.</returns>
    public static bool IsUpperCaseLetter(char c) => (c >= 'A' && c <= 'Z');

    /// <summary>
    /// Determines whether the specified character is a lowercase ASCII letter.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is a lowercase letter; otherwise, false.</returns>
    public static bool IsLowerCaseLetter(char c) => (c >= 'a' && c <= 'z');
    #endregion

    #region ASCII to PETSCII conversion
    /// <summary>
    /// Represents a mapping between an ASCII character and its PETSCII equivalent.
    /// </summary>
    /// <param name="ASCII">The ASCII character.</param>
    /// <param name="PETSCII">The PETSCII string representation.</param>
    public record class PETSCIIChar(char ASCII, string PETSCII);

    /// <summary>
    /// Table of special ASCII to PETSCII character mappings.
    /// </summary>
    public static readonly PETSCIIChar[] PETCharsTable =
    {
        new('[', "["),
        new(']', "]"),
        new('£', ((char) 0x5c).ToString()),
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
    /// <param name="LowerCase">If true, converts uppercase letters to lowercase PETSCII.</param>
    /// <returns>The PETSCII string representation of the character.</returns>
    public static string PETSCII(char c, bool LowerCase = false)
    {
        string ret = " ";
        if (IsSymbolOrNumber(c))
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
    public static readonly ASCIIChar[] ASCIICharsTable =
    {
        new('[', '['),
        new(']', ']'),
        new((char) 0x5c, '£'),
    };

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
    #endregion
}
