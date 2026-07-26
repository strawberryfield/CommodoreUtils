//-----------------------------------------------------------------------
// <copyright file="C64BitmapRenderer.cs" company="Casasoft">
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

namespace Casasoft.Commodore.Images;

/// <summary>
/// Provides static methods for rendering Commodore 64 bitmap graphics data into MagickImage format.
/// Supports both Hires (high-resolution) and Multicolor bitmap modes with proper color palette mapping.
/// </summary>
public static class C64BitmapRenderer
{
    /// <summary>
    /// Converts C64 Hires bitmap data to a MagickImage representation.
    /// 
    /// Hires mode renders at 320x200 resolution with 2 colors per 8x8 cell (foreground and background).
    /// The color information is stored in ScreenRam where the high nibble represents the foreground color
    /// and the low nibble represents the background color.
    /// </summary>
    /// <param name="hiresData">The C64 Hires bitmap data containing ScreenRam and BitmapData.</param>
    /// <returns>A MagickImage object with dimensions 320x200 representing the rendered Hires bitmap.</returns>
    public static MagickImage ToMagickImage(C64HiresData hiresData)
    {
        var image = new MagickImage(MagickColors.Black, 320, 200);

        using (var pixels = image.GetPixels())
        {
            for (int cellY = 0; cellY < 25; cellY++)
            {
                for (int cellX = 0; cellX < 40; cellX++)
                {
                    int cellIndex = cellY * 40 + cellX;
                    byte colorByte = hiresData.ScreenRam[cellIndex];

                    // Nibble alto = Foreground (Bit 1), Nibble basso = Background (Bit 0)
                    MagickColor fgColor = C64Palette.GetMagickColor(colorByte >> 4);
                    MagickColor bgColor = C64Palette.GetMagickColor(colorByte);

                    int tileOffset = cellIndex * 8;

                    for (int row = 0; row < 8; row++)
                    {
                        byte rowByte = hiresData.BitmapData[tileOffset + row];
                        int pixelY = cellY * 8 + row;

                        for (int col = 0; col < 8; col++)
                        {
                            int pixelX = cellX * 8 + col;
                            bool isBitSet = (rowByte & (0x80 >> col)) != 0;

                            var writeColor = isBitSet ? fgColor : bgColor;
                            pixels.SetPixel(pixelX, pixelY, ToQuantumArray(writeColor));
                        }
                    }
                }
            }
        }

        return image;
    }

    /// <summary>
    /// Converts C64 Multicolor bitmap data to a MagickImage representation.
    /// 
    /// Multicolor mode renders at 320x200 resolution (maintaining correct aspect ratio for C64's 160x200 logical resolution)
    /// with 4 colors per 8x8 cell. Colors are determined by bit pair patterns:
    /// - 00: Global background color from $D021
    /// - 01: Color from ScreenRam high nibble
    /// - 10: Color from ScreenRam low nibble
    /// - 11: Color from ColorRam
    /// Each logical color pixel spans 2 physical pixels horizontally to maintain proportions.
    /// </summary>
    /// <param name="mcData">The C64 Multicolor bitmap data containing ScreenRam, ColorRam, BitmapData, and background color.</param>
    /// <returns>A MagickImage object with dimensions 320x200 representing the rendered Multicolor bitmap.</returns>
    public static MagickImage ToMagickImage(C64MulticolorData mcData)
    {
        var image = new MagickImage(MagickColors.Black, 320, 200);
        MagickColor color00 = C64Palette.GetMagickColor(mcData.BackgroundColor);

        using (var pixels = image.GetPixels())
        {
            for (int cellY = 0; cellY < 25; cellY++)
            {
                for (int cellX = 0; cellX < 40; cellX++)
                {
                    int cellIndex = cellY * 40 + cellX;

                    // Mappatura pattern di bit -> colori:
                    // 00: Background globale ($D021)
                    // 01: ScreenRam Nibble Alto
                    // 10: ScreenRam Nibble Basso
                    // 11: ColorRam Nibble Basso
                    byte screenByte = mcData.ScreenRam[cellIndex];
                    MagickColor color01 = C64Palette.GetMagickColor(screenByte >> 4);
                    MagickColor color10 = C64Palette.GetMagickColor(screenByte);
                    MagickColor color11 = C64Palette.GetMagickColor(mcData.ColorRam[cellIndex]);

                    MagickColor[] cellPalette = { color00, color01, color10, color11 };
                    int tileOffset = cellIndex * 8;

                    for (int row = 0; row < 8; row++)
                    {
                        byte rowByte = mcData.BitmapData[tileOffset + row];
                        int pixelY = cellY * 8 + row;

                        // 4 pixel multicolor per riga (ciascuno largo 2 pixel bitmap)
                        for (int col = 0; col < 4; col++)
                        {
                            int bitPair = (rowByte >> (6 - (col * 2))) & 0x03;
                            MagickColor pixelColor = cellPalette[bitPair];

                            int pixelX = (cellX * 8) + (col * 2);

                            // Disegna 2 pixel adiacenti per mantenere le proporzioni corrette (320x200)
                            var q = ToQuantumArray(pixelColor);
                            pixels.SetPixel(pixelX, pixelY, q);
                            pixels.SetPixel(pixelX + 1, pixelY, q);
                        }
                    }
                }
            }
        }

        return image;
    }

    private static ushort[] ToQuantumArray(MagickColor c)
    {
        // Converte ogni canale da 0..255 al range Quantum dell'assemblato corrente (0..65535 in Q16)
        ushort r = (ushort)Math.Round(c.R * (double)Quantum.Max / 255.0);
        ushort g = (ushort)Math.Round(c.G * (double)Quantum.Max / 255.0);
        ushort b = (ushort)Math.Round(c.B * (double)Quantum.Max / 255.0);

        return new ushort[] { r, g, b };
    }
}