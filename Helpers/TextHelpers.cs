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
        StringBuilder wrappedText = new();

        string[] paragraphs = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        foreach (var paragraph in paragraphs)
        {

            string[] words = paragraph.Split(' ');
            int currentLineLength = 0;
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
            wrappedText.AppendLine();
        }
        return wrappedText.ToString();
    }   

    /// <summary>
    /// Specifies the supported end-of-line (EOL) sequences that can be used when normalizing text.
    /// </summary>
    public enum EolType
    {
        /// <summary>
        /// Carriage return + line feed sequence ("\r\n").
        /// Common on Windows platforms.
        /// </summary>
        CRLF,

        /// <summary>
        /// Line feed sequence ("\n").
        /// Common on Unix-like platforms (Linux, macOS).
        /// </summary>
        LF,

        /// <summary>
        /// Carriage return sequence ("\r").
        /// Historically used on older Mac systems or Commodore 8bit computers.
        /// </summary>
        CR
    }

    /// <summary>
    /// Normalizes all line endings in <paramref name="text"/> to the specified <paramref name="eolType"/>.
    /// </summary>
    /// <param name="text">The input text that may contain mixed line endings. If <c>null</c> or empty, the original value is returned.</param>
    /// <param name="eolType">The target end-of-line sequence to apply.</param>
    /// <returns>
    /// A new string where all line endings are converted to the requested EOL sequence.
    /// If <paramref name="text"/> is <c>null</c> or empty, the same value is returned.
    /// </returns>
    /// <remarks>
    /// The method first normalizes all known line ending combinations (CRLF and CR) to LF,
    /// then replaces LF with the requested EOL sequence. This two-step approach avoids
    /// accidental doubling or partial replacements when converting between sequences.
    /// </remarks>
    public static string NormalizeEol(string text, EolType eolType)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        string eol;
        switch (eolType)
        {
            case EolType.CRLF:
                eol = "\r\n";
                break;
            case EolType.LF:
                eol = "\n";
                break;
            case EolType.CR:
                eol = "\r";
                break;
            default:
                eol = Environment.NewLine;
                break;
        }
        // Normalize all line endings to LF first
        string normalizedText = text.Replace("\r\n", "\n").Replace("\r", "\n");
        // Then replace LF with the desired EOL
        normalizedText = normalizedText.Replace("\n", eol);
        return normalizedText;
    }
}
