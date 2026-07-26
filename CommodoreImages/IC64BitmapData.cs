//-----------------------------------------------------------------------
// <copyright file="IC64BitmapData.cs" company="Casasoft">
//     Author: Roberto Ceccarelli (http://strawberryfield.altervista.org)
//     Copyright (c) 2026 All rights reserved.
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

namespace Casasoft.Commodore.Images;

/// <summary>
/// Defines the common structure and operations for Commodore 64 bitmap graphics data.
/// </summary>
public interface IC64BitmapData
{
    /// <summary>
    /// Gets or sets the bitmap data containing character patterns (8000 bytes).
    /// </summary>
    byte[] BitmapData { get; set; }

    /// <summary>
    /// Gets or sets the screen RAM containing per-cell color codes (1000 bytes).
    /// </summary>
    byte[] ScreenRam { get; set; }

    /// <summary>
    /// Saves the bitmap data and associated RAM buffers to binary files starting with the given base path.
    /// </summary>
    /// <param name="basePath">The base file path (without extension) for saving binary files.</param>
    void SaveToFile(string basePath);
}