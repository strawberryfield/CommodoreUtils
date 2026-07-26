//-----------------------------------------------------------------------
// <copyright file="C64Palette.cs" company="Casasoft">
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

using ImageMagick;
using System.Drawing;

namespace Casasoft.Commodore.Images;

/// <summary>
/// Provides palette definitions and color utilities for the Commodore 64.
/// </summary>
/// <remarks>
/// The Commodore 64 uses a 16-color palette. This class provides both System.Drawing.Color
/// representations and RGB tuple representations of each color, along with utilities for
/// finding the closest matching palette color to a given RGB value.
/// </remarks>
public static class C64Palette
{
    /// <summary>
    /// Array of 16 colors representing the Commodore 64 color palette using System.Drawing.Color.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Index</term><description>Color</description></listheader>
    /// <item><term>0</term><description>Black</description></item>
    /// <item><term>1</term><description>White</description></item>
    /// <item><term>2</term><description>Red</description></item>
    /// <item><term>3</term><description>Cyan</description></item>
    /// <item><term>4</term><description>Purple</description></item>
    /// <item><term>5</term><description>Green</description></item>
    /// <item><term>6</term><description>Blue</description></item>
    /// <item><term>7</term><description>Yellow</description></item>
    /// <item><term>8</term><description>Orange</description></item>
    /// <item><term>9</term><description>Brown</description></item>
    /// <item><term>10</term><description>Light Red</description></item>
    /// <item><term>11</term><description>Dark Gray</description></item>
    /// <item><term>12</term><description>Gray</description></item>
    /// <item><term>13</term><description>Light Green</description></item>
    /// <item><term>14</term><description>Light Blue</description></item>
    /// <item><term>15</term><description>Light Gray</description></item>
    /// </list>
    /// </remarks>
    public static readonly Color[] Colors = {
        Color.Black,           // 0
        Color.White,           // 1
        Color.Red,             // 2
        Color.Cyan,            // 3
        Color.Purple,          // 4
        Color.Green,           // 5
        Color.Blue,            // 6
        Color.Yellow,          // 7
        Color.Orange,          // 8
        Color.Brown,           // 9
        Color.FromArgb(255, 119, 119), // 10 - Light Red (no Color.LightRed in System.Drawing)
        Color.DarkGray,        // 11
        Color.Gray,            // 12
        Color.LightGreen,      // 13
        Color.LightBlue,       // 14
        Color.LightGray        // 15
    };

    /// <summary>
    /// Array of 16 RGB tuples representing the Commodore 64 color palette with accurate hardware values.
    /// </summary>
    /// <remarks>
    /// Each tuple contains (Red, Green, Blue) byte values (0-255) for the corresponding palette color.
    /// These values represent the authentic Commodore 64 hardware colors and should be preferred
    /// over the System.Drawing.Color equivalents for accurate color representation.
    /// </remarks>
    public static readonly (byte r, byte g, byte b)[] ColorsRgb = {
        (0, 0, 0),           // 0 - Black
        (255, 255, 255),   // 1 - White
        (136, 0, 0),       // 2 - Red
        (170, 255, 238),   // 3 - Cyan
        (153, 68, 187),    // 4 - Purple
        (0, 204, 85),      // 5 - Green
        (0, 0, 170),       // 6 - Blue
        (238, 238, 119),   // 7 - Yellow
        (221, 136, 85),    // 8 - Orange
        (102, 68, 0),      // 9 - Brown
        (255, 119, 119),   // 10 - Light Red
        (68, 68, 68),      // 11 - Dark Gray
        (170, 170, 170),   // 12 - Gray
        (85, 255, 170),    // 13 - Light Green
        (85, 95, 255),     // 14 - Light Blue
        (254, 254, 254)     // 15 - Light Gray (adjusted for C64)
    };

    /// <summary>
    /// Finds the index of the closest matching color in the Commodore 64 palette for a given RGB value.
    /// </summary>
    /// <param name="r">The red component (0-255).</param>
    /// <param name="g">The green component (0-255).</param>
    /// <param name="b">The blue component (0-255).</param>
    /// <returns>The index (0-15) of the closest matching palette color.</returns>
    /// <remarks>
    /// Uses Euclidean distance in 3D RGB color space to determine the closest match.
    /// This is useful for color quantization when converting arbitrary RGB images to Commodore 64 palette.
    /// </remarks>
    public static int FindClosestColor(byte r, byte g, byte b)
    {
        int closestIndex = 0;
        int minDistance = int.MaxValue;

        for (int i = 0; i < ColorsRgb.Length; i++)
        {
            var c = ColorsRgb[i];
            int dr = r - c.r;
            int dg = g - c.g;
            int db = b - c.b;
            int distance = dr * dr + dg * dg + db * db;

            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    /// <summary>
    /// Restituisce il valore MagickColor corrispondente all'indice di colore C64 basato su ColorsRgb.
    /// </summary>
    public static MagickColor GetMagickColor(int colorIndex)
    {
        var (r, g, b) = C64Palette.ColorsRgb[colorIndex & 0x0F];
        return new MagickColor(r, g, b);
    }
}