//-----------------------------------------------------------------------
// <copyright file="C64MulticolorData.cs" company="Casasoft">
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
/// Represents the multicolor graphics data for a Commodore 64 display.
/// This class manages the three separate memory areas required for C64 multicolor bitmap graphics:
/// bitmap data (character definitions), color RAM (per-cell color information), and screen RAM (character codes),
/// plus the single, screen-wide background color register value.
/// </summary>
public class C64MulticolorData : IC64BitmapData
{
    /// <summary>
    /// Gets or sets the bitmap data containing character patterns.
    /// Size: 8000 bytes (40 columns × 25 rows × 8 bytes per character cell).
    /// Each byte represents 4 multicolor pixels (2 bits per pixel) for one row of a character cell.
    /// </summary>
    public byte[] BitmapData { get; set; } = new byte[8000];    // 40x25x8 = 8KB

    /// <summary>
    /// Gets or sets the color RAM containing color information for each screen cell.
    /// Size: 1000 bytes (40 columns × 25 rows).
    /// Each byte (lower nibble) specifies the "11" bit-pattern color for the corresponding character cell.
    /// </summary>
    public byte[] ColorRam { get; set; } = new byte[1000];       // 40x25 = 1KB

    /// <summary>
    /// Gets or sets the screen RAM containing color codes for each screen cell.
    /// Size: 1000 bytes (40 columns × 25 rows).
    /// High nibble holds the "01" bit-pattern color, low nibble holds the "10" bit-pattern color.
    /// </summary>
    public byte[] ScreenRam { get; set; } = new byte[1000];       // 40x25 = 1KB

    /// <summary>
    /// Gets or sets the single, screen-wide background color (VIC-II "00" bit-pattern),
    /// corresponding to the C64 background color register ($D021).
    /// This must be POKEd/written to $D021 by whatever loads/displays the image,
    /// since it is not part of BitmapData/ScreenRam/ColorRam.
    /// </summary>
    public byte BackgroundColor { get; set; }

    /// <summary>
    /// Saves the bitmap data, color RAM, and screen RAM to separate binary files.
    /// </summary>
    /// <param name="basePath">The base file path (without extension) for saving the binary files.
    /// Three files will be created: {basePath}_bitmap.bin, {basePath}_colorram.bin, and {basePath}_screenram.bin</param>
    public void SaveToFile(string basePath)
    {
        File.WriteAllBytes($"{basePath}_bitmap.bin", this.BitmapData);
        File.WriteAllBytes($"{basePath}_colorram.bin", this.ColorRam);
        File.WriteAllBytes($"{basePath}_screenram.bin", this.ScreenRam);
        Console.WriteLine($"Saved files with base path: {basePath}");
        Console.WriteLine($"Background color (POKE 53281,{BackgroundColor}): {BackgroundColor}");
    }
}