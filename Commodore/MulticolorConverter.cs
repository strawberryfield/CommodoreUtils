//-----------------------------------------------------------------------
// <copyright file="MulticolorConverter.cs" company="Casasoft">
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

using SkiaSharp;

namespace Casasoft.Commodore;

/// <summary>
/// Converts RGB images to Commodore 64 multicolor bitmap format using SkiaSharp.
/// 
/// This class implements a three-stage conversion process:
/// <list type="number">
/// <item><description>Downscales the input image to 160x200 pixels using resize.</description></item>
/// <item><description>Applies Floyd-Steinberg dithering to quantize colors to the C64 palette while minimizing color loss.</description></item>
/// <item><description>Generates multicolor bitmap data, color RAM, and screen RAM compatible with C64 hardware.</description></item>
/// </list>
/// 
/// The C64 multicolor bitmap mode uses 8x8 pixel character cells. Each cell has 4 "double-wide"
/// logical pixels per row (2 bits per pixel, each pixel rendered 2 screen-pixels wide), so a cell
/// covers 4x8 logical pixels which map 1:1 onto the 160x200 working resolution used here.
/// Each cell can use up to 4 colors:
/// <list type="bullet">
/// <item><description>bit-pattern 00 -&gt; the single, screen-wide background color (VIC-II $D021)</description></item>
/// <item><description>bit-pattern 01 -&gt; Screen RAM high nibble (per cell)</description></item>
/// <item><description>bit-pattern 10 -&gt; Screen RAM low nibble (per cell)</description></item>
/// <item><description>bit-pattern 11 -&gt; Color RAM low nibble (per cell)</description></item>
/// </list>
/// </summary>
public class MulticolorConverter
{
    /// <summary>The width of the C64 screen in multicolor pixels (160).</summary>
    private const int SCREEN_WIDTH = 160;

    /// <summary>The height of the C64 screen (200 pixels).</summary>
    private const int SCREEN_HEIGHT = 200;

    /// <summary>The width, in logical multicolor pixels, of a character cell (4 double-wide pixels = 8 screen pixels).</summary>
    private const int CELL_PIXEL_WIDTH = 4;

    /// <summary>The height, in pixels, of a character cell (8).</summary>
    private const int CELL_PIXEL_HEIGHT = 8;

    /// <summary>Number of character columns on screen (40).</summary>
    private const int CHAR_COLUMNS = 40;

    /// <summary>Number of character rows on screen (25).</summary>
    private const int CHAR_ROWS = 25;

    /// <summary>
    /// Represents accumulated color error values for Floyd-Steinberg dithering.
    /// Stores separate error values for red, green, and blue channels.
    /// </summary>
    private struct Error
    {
        /// <summary>Red channel error accumulation.</summary>
        public double R;

        /// <summary>Green channel error accumulation.</summary>
        public double G;

        /// <summary>Blue channel error accumulation.</summary>
        public double B;
    }

    /// <summary>
    /// Converts an RGB image to C64 multicolor format.
    /// </summary>
    /// <param name="input">The input image as an SKBitmap to be converted.</param>
    /// <returns>A <see cref="C64MulticolorData"/> object containing the bitmap data, color RAM, screen RAM and background color.</returns>
    public C64MulticolorData ConvertImage(SKBitmap input)
    {
        var result = new C64MulticolorData();

        // Step 1: Downscale from original size to 160x200
        using var downscaled = DownscaleImage(input);

        // Step 2: Apply Floyd-Steinberg dithering to C64 palette
        using var dithered = ApplyDithering(downscaled);

        // Step 3: Generate multicolor bitmap data, color RAM, screen RAM and background color
        GenerateMulticolorData(dithered, result);

        return result;
    }

    /// <summary>
    /// Downscales the input image to C64 screen dimensions (160x200).
    /// </summary>
    /// <param name="input">The source image to downscale.</param>
    /// <returns>A new SKBitmap resized to 160x200 pixels.</returns>
    private SKBitmap DownscaleImage(SKBitmap input)
    {
        var info = new SKImageInfo(SCREEN_WIDTH, SCREEN_HEIGHT, input.ColorType, input.AlphaType);
        var output = new SKBitmap(info);

        // SKSamplingOptions con filtro lineare ed effetto mipmap lineare (equivalente a una qualità alta)
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

        if (!input.ScalePixels(output, sampling))
        {
            throw new InvalidOperationException("Failed to scale image.");
        }

        return output;
    }

    /// <summary>
    /// Applies Floyd-Steinberg dithering to convert the image to the C64 color palette.
    /// </summary>
    /// <param name="input">The 160x200 image to dither.</param>
    /// <returns>A dithered SKBitmap with colors quantized to the C64 palette.</returns>
    private SKBitmap ApplyDithering(SKBitmap input)
    {
        // Enforce a standard 32-bit RGBA or BGRA target format to easily manipulate pixels safely
        var info = new SKImageInfo(SCREEN_WIDTH, SCREEN_HEIGHT, SKColorType.Rgba8888, SKAlphaType.Opaque);
        var output = new SKBitmap(info);
        var errorBuffer = new Error[SCREEN_WIDTH + 2, SCREEN_HEIGHT + 2];

        // Process each pixel
        for (int y = 0; y < SCREEN_HEIGHT; y++)
        {
            for (int x = 0; x < SCREEN_WIDTH; x++)
            {
                var pixel = input.GetPixel(x, y);

                // Add error diffusion
                int r = pixel.Red + (int)Math.Round(errorBuffer[x, y].R);
                int g = pixel.Green + (int)Math.Round(errorBuffer[x, y].G);
                int b = pixel.Blue + (int)Math.Round(errorBuffer[x, y].B);

                // Clamp values
                r = Math.Max(0, Math.Min(255, r));
                g = Math.Max(0, Math.Min(255, g));
                b = Math.Max(0, Math.Min(255, b));

                // Find closest C64 color
                int closestColor = C64Palette.FindClosestColor((byte)r, (byte)g, (byte)b);
                var c64Color = C64Palette.ColorsRgb[closestColor];

                output.SetPixel(x, y, new SKColor((byte)c64Color.r, (byte)c64Color.g, (byte)c64Color.b));

                // Calculate error
                int errR = r - c64Color.r;
                int errG = g - c64Color.g;
                int errB = b - c64Color.b;

                // Floyd-Steinberg error diffusion
                if (x < SCREEN_WIDTH - 1)
                {
                    errorBuffer[x + 1, y].R += errR * 7.0 / 16.0;
                    errorBuffer[x + 1, y].G += errG * 7.0 / 16.0;
                    errorBuffer[x + 1, y].B += errB * 7.0 / 16.0;
                }
                if (x > 0)
                {
                    errorBuffer[x - 1, y + 1].R += errR * 3.0 / 16.0;
                    errorBuffer[x - 1, y + 1].G += errG * 3.0 / 16.0;
                    errorBuffer[x - 1, y + 1].B += errB * 3.0 / 16.0;
                }
                if (y < SCREEN_HEIGHT - 1)
                {
                    errorBuffer[x, y + 1].R += errR * 5.0 / 16.0;
                    errorBuffer[x, y + 1].G += errG * 5.0 / 16.0;
                    errorBuffer[x, y + 1].B += errB * 5.0 / 16.0;
                }
                if (x < SCREEN_WIDTH - 1 && y < SCREEN_HEIGHT - 1)
                {
                    errorBuffer[x + 1, y + 1].R += errR * 1.0 / 16.0;
                    errorBuffer[x + 1, y + 1].G += errG * 1.0 / 16.0;
                    errorBuffer[x + 1, y + 1].B += errB * 1.0 / 16.0;
                }
            }
        }

        return output;
    }

    /// <summary>
    /// Generates the multicolor bitmap data, color RAM, screen RAM and background color for the C64.
    /// </summary>
    /// <param name="input">The dithered 160x200 image with C64 palette colors.</param>
    /// <param name="output">The output structure to populate with bitmap, RAM data and background color.</param>
    private void GenerateMulticolorData(SKBitmap input, C64MulticolorData output)
    {
        // Get color indices for all pixels (160x200 working resolution, 1:1 with multicolor logical pixels)
        int[,] colorIndices = new int[SCREEN_WIDTH, SCREEN_HEIGHT];
        for (int y = 0; y < SCREEN_HEIGHT; y++)
        {
            for (int x = 0; x < SCREEN_WIDTH; x++)
            {
                var pixel = input.GetPixel(x, y);
                colorIndices[x, y] = C64Palette.FindClosestColor(pixel.Red, pixel.Green, pixel.Blue);
            }
        }

        // The background color (bit-pattern 00) is a single, screen-wide VIC-II register ($D021),
        // so it must be the same for the whole image: pick the most frequent color overall.
        int globalBackground = ComputeGlobalBackground(colorIndices);
        output.BackgroundColor = (byte)globalBackground;

        // Process each 8x8 character cell (40 columns x 25 rows).
        // In source-pixel terms each cell is CELL_PIXEL_WIDTH (4) x CELL_PIXEL_HEIGHT (8),
        // matching the 160x200 working resolution (40*4=160, 25*8=200).
        for (int cy = 0; cy < CHAR_ROWS; cy++)
        {
            for (int cx = 0; cx < CHAR_COLUMNS; cx++)
            {
                int screenOffset = cy * CHAR_COLUMNS + cx;
                int bitmapOffset = cy * CHAR_COLUMNS * CELL_PIXEL_HEIGHT + cx * CELL_PIXEL_HEIGHT;

                // Extract the 4x8 block and find the best 3 foreground colors for this cell
                var blockColors = ExtractBlockColors(colorIndices, cx * CELL_PIXEL_WIDTH, cy * CELL_PIXEL_HEIGHT);
                int[] fgColors = SelectBestColors(blockColors, globalBackground);

                // Generate bitmap data for this character cell
                for (int y = 0; y < CELL_PIXEL_HEIGHT; y++)
                {
                    byte bitmapByte = 0;
                    for (int x = 0; x < CELL_PIXEL_WIDTH; x++)
                    {
                        int pixelX = cx * CELL_PIXEL_WIDTH + x;
                        int pixelY = cy * CELL_PIXEL_HEIGHT + y;
                        int colorIndex = colorIndices[pixelX, pixelY];

                        // Find which of the 4 available colors (bg + 3 fg) this pixel is closest to
                        int multicolorIndex = GetMulticolorIndex(colorIndex, globalBackground, fgColors);

                        // Pack 2 bits per pixel, MSB first (leftmost logical pixel in bits 7-6)
                        int shift = (CELL_PIXEL_WIDTH - 1 - x) * 2;
                        bitmapByte |= (byte)((multicolorIndex & 0x03) << shift);
                    }
                    output.BitmapData[bitmapOffset + y] = bitmapByte;
                }

                // Screen RAM: high nibble = bit-pattern 01 color, low nibble = bit-pattern 10 color
                output.ScreenRam[screenOffset] = (byte)(((fgColors[0] & 0x0F) << 4) | (fgColors[1] & 0x0F));

                // Color RAM: bit-pattern 11 color
                output.ColorRam[screenOffset] = (byte)(fgColors[2] & 0x0F);
            }
        }
    }

    /// <summary>
    /// Finds the single color used most often across the whole (working-resolution) image,
    /// to be used as the screen-wide VIC-II background color.
    /// </summary>
    private int ComputeGlobalBackground(int[,] colorIndices)
    {
        int[] counts = new int[16];
        for (int y = 0; y < SCREEN_HEIGHT; y++)
        {
            for (int x = 0; x < SCREEN_WIDTH; x++)
            {
                counts[colorIndices[x, y]]++;
            }
        }

        int best = 0;
        int bestCount = -1;
        for (int i = 0; i < 16; i++)
        {
            if (counts[i] > bestCount)
            {
                bestCount = counts[i];
                best = i;
            }
        }
        return best;
    }

    /// <summary>
    /// Extracts the color indices of a CELL_PIXEL_WIDTH x CELL_PIXEL_HEIGHT block starting at (startX, startY).
    /// </summary>
    private int[] ExtractBlockColors(int[,] colorIndices, int startX, int startY)
    {
        var colors = new int[CELL_PIXEL_WIDTH * CELL_PIXEL_HEIGHT];
        int index = 0;
        for (int y = 0; y < CELL_PIXEL_HEIGHT; y++)
        {
            for (int x = 0; x < CELL_PIXEL_WIDTH; x++)
            {
                colors[index++] = colorIndices[startX + x, startY + y];
            }
        }
        return colors;
    }

    /// <summary>
    /// Selects the best 3 foreground colors for a cell, given the fixed, screen-wide background color.
    /// The 3 most frequent colors in the block (excluding the background) are chosen; if the block
    /// contains fewer than 3 distinct non-background colors, the remaining slots are filled with the
    /// background color itself (harmless, since GetMulticolorIndex will map matching pixels to index 0 anyway).
    /// </summary>
    private int[] SelectBestColors(int[] blockColors, int globalBackground)
    {
        int[] colorCounts = new int[16];
        foreach (int c in blockColors)
        {
            if (c >= 0 && c < 16)
                colorCounts[c]++;
        }

        var topColors = Enumerable.Range(0, 16)
            .Where(i => i != globalBackground && colorCounts[i] > 0)
            .OrderByDescending(i => colorCounts[i])
            .Take(3)
            .ToList();

        while (topColors.Count < 3)
            topColors.Add(globalBackground);

        return topColors.ToArray();
    }

    /// <summary>
    /// Maps a pixel's palette color index to one of the 4 colors available for its cell
    /// (background + 3 foreground colors), choosing the closest match if there is no exact hit.
    /// </summary>
    private int GetMulticolorIndex(int pixelColor, int bgColor, int[] fgColors)
    {
        if (pixelColor == bgColor) return 0;
        if (pixelColor == fgColors[0]) return 1;
        if (pixelColor == fgColors[1]) return 2;
        if (pixelColor == fgColors[2]) return 3;

        int bestMatch = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < 4; i++)
        {
            int compareColor = (i == 0) ? bgColor : fgColors[i - 1];
            var c1 = C64Palette.ColorsRgb[compareColor];
            var c2 = C64Palette.ColorsRgb[pixelColor];

            int dist = (c1.r - c2.r) * (c1.r - c2.r) +
                      (c1.g - c2.g) * (c1.g - c2.g) +
                      (c1.b - c2.b) * (c1.b - c2.b);

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestMatch = i;
            }
        }

        return bestMatch;
    }
}
