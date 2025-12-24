//-----------------------------------------------------------------------
// <copyright file="IPrgFile.cs" company="Casasoft">
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
/// Represents a PRG (program) file that can be persisted to storage.
/// </summary>
/// <remarks>
/// Implementations encapsulate the data and format for a Commodore PRG file
/// and provide a mechanism to write that data to a file system path.
/// Calling code is responsible for ensuring the target path is valid and accessible.
/// </remarks>
public interface IPrgFile
{
    /// <summary>
    /// Saves the PRG file to the specified file system path.
    /// </summary>
    /// <param name="filePath">
    /// The path where the PRG file will be written. Can be an absolute or relative path.
    /// </param>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="filePath"/> is <c>null</c> or an empty string.
    /// </exception>
    /// <exception cref="System.IO.IOException">
    /// Thrown when an I/O error occurs while writing the file (for example, insufficient permissions,
    /// disk full, or other file system errors).
    /// </exception>
    /// <remarks>
    /// Implementations should overwrite an existing file at <paramref name="filePath"/> if present,
    /// unless otherwise documented by the concrete type. Callers should handle exceptions related to I/O
    /// and ensure appropriate permissions are available.
    /// </remarks>
    void Save(string filePath);
}
