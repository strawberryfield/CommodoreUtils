//-----------------------------------------------------------------------
// <copyright file="TextHelpers.cs" company="Casasoft">
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

using System.Text;

namespace Casasoft.Helpers;

/// <summary>
/// Provides helper methods for text manipulation.
/// </summary>
public static class TextHelpers
{
    /// <summary>
    /// Wraps the input text so that each line does not exceed the specified maximum line length.
    /// Words are kept intact; lines are broken at spaces.
    /// </summary>
    /// <param name="text">The input text to wrap.</param>
    /// <param name="maxLineLength">The maximum length of each line.</param>
    /// <returns>The word-wrapped text.</returns>
    public static string WordWrap(string text, int maxLineLength)
    {
        if (string.IsNullOrWhiteSpace(text) || maxLineLength <= 0)
            return text;
        var words = text.Split(' ');
        var wrappedText = new StringBuilder();
        var currentLineLength = 0;
        foreach (var word in words)
        {
            if (currentLineLength + word.Length + 1 > maxLineLength)
            {
                if (wrappedText.Length > 0)
                    wrappedText.AppendLine();
                wrappedText.Append(word);
                currentLineLength = word.Length;
            }
            else
            {
                if (currentLineLength > 0)
                {
                    wrappedText.Append(' ');
                    currentLineLength++;
                }
                wrappedText.Append(word);
                currentLineLength += word.Length;
            }
        }
        return wrappedText.ToString();
    }   
}
