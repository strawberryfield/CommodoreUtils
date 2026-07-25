//-----------------------------------------------------------------------
// <copyright file="C64HiresData.cs" company="Casasoft">
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

namespace Casasoft.Commodore;

/// <summary>
/// Represents the hires (2-color, standard bitmap) graphics data for a Commodore 64 display.
/// This class manages the two memory areas required for C64 standard (high resolution) bitmap graphics:
/// bitmap data (character definitions, 1 bit per pixel) and screen RAM (per-cell foreground/background colors).
/// </summary>
/// <remarks>
/// Unlike multicolor bitmap mode (see <see cref="C64MulticolorData"/>), standard hires bitmap mode has
/// a full 320x200 pixel resolution (1 bit per pixel instead of 2) and does not use Color RAM or the
/// VIC-II background color register ($D021): both colors available in each 8x8 character cell are
/// entirely encoded in <see cref="ScreenRam"/>.
/// </remarks>
public class C64HiresData
{
    /// <summary>
    /// Gets or sets the bitmap data containing character patterns.
    /// Size: 8000 bytes (40 columns × 25 rows × 8 bytes per character cell).
    /// Each byte represents 8 hires pixels (1 bit per pixel) for one row of a character cell:
    /// a set bit selects the cell's foreground color, a clear bit selects the cell's background color.
    /// </summary>
    public byte[] BitmapData { get; set; } = new byte[8000];    // 40x25x8 = 8KB

    /// <summary>
    /// Gets or sets the screen RAM containing the two colors available to each screen cell.
    /// Size: 1000 bytes (40 columns × 25 rows).
    /// High nibble holds the foreground color (bit value 1), low nibble holds the background color (bit value 0).
    /// </summary>
    public byte[] ScreenRam { get; set; } = new byte[1000];       // 40x25 = 1KB

    /// <summary>
    /// Saves the bitmap data and screen RAM to separate binary files.
    /// </summary>
    /// <param name="basePath">The base file path (without extension) for saving the binary files.
    /// Two files will be created: {basePath}_bitmap.bin and {basePath}_screenram.bin</param>
    public void SaveToFile(string basePath)
    {
        File.WriteAllBytes($"{basePath}_bitmap.bin", this.BitmapData);
        File.WriteAllBytes($"{basePath}_screenram.bin", this.ScreenRam);
        Console.WriteLine($"Saved files with base path: {basePath}");
    }
}
