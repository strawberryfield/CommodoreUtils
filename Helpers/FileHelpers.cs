//-----------------------------------------------------------------------
// <copyright file="FileHelpers.cs" company="Casasoft">
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

namespace Casasoft.Helpers;

/// <summary>
/// Provides helper methods for file operations, such as expanding wildcard patterns in file paths.
/// </summary>
public static class FileHelpers
{
    /// <summary>
    /// Expands wildcard patterns in file paths to match actual files in the directory.
    /// This is necessary because the Windows shell does not automatically expand wildcards.
    /// For each file path in the input list, if it contains wildcard characters ('*' or '?'),
    /// it retrieves all matching files from the specified directory. Otherwise, it adds the
    /// file path directly to the result list.
    /// </summary>
    /// <param name="FilesList">A list of file paths, which may include wildcard patterns.</param>
    /// <returns>A list of file paths with wildcards expanded to match actual files.</returns>
    public static List<string> ExpandWildcards(List<string> FilesList)
    {
        List<string> files = new();
        foreach (string filename in FilesList)
        {
            if (filename.Contains('*') || filename.Contains('?'))
            {
                string? path = Path.GetDirectoryName(filename);
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = ".";
                }
                files.AddRange(Directory.GetFiles(path, Path.GetFileName(filename)).ToList());
            }
            else
            {
                files.Add(filename);
            }
        }
        return files;
    }
}
