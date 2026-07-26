//-----------------------------------------------------------------------
// <copyright file="MulticolorConverter.cs" company="Casasoft">
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
/// Converts RGB images to Commodore 64 multicolor bitmap format using Magick.NET (ImageMagick).
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
public class MulticolorConverter : C64BitmapConverterBase, IC64BitmapConverter<C64MulticolorData>
{
    /// <summary>The width of the C64 screen in multicolor pixels (160).</summary>
    private const int SCREEN_WIDTH = 160;

    /// <summary>The height of the C64 screen (200 pixels).</summary>
    private const int SCREEN_HEIGHT = 200;

    /// <summary>The width, in logical multicolor pixels, of a character cell (4 double-wide pixels = 8 screen pixels).</summary>
    private const int CELL_PIXEL_WIDTH = 4;

    /// <summary>The height, in pixels, of a character cell (8).</summary>
    private const int CELL_PIXEL_HEIGHT = 8;

    /// <summary>
    /// Converts an RGB image to C64 multicolor format.
    /// </summary>
    /// <param name="input">The input image as a <see cref="MagickImage"/> to be converted.</param>
    /// <param name="useDithering">
    /// If <see langword="true"/> (default), Floyd-Steinberg error-diffusion dithering is applied while
    /// quantizing colors to the C64 palette. If <see langword="false"/>, each pixel is simply mapped to
    /// its closest C64 palette color with no error diffusion, which produces flatter, more "banded"
    /// results but avoids the dithering pattern/noise.
    /// </param>
    /// <param name="brightnessBias">
    /// Controls how strongly brighter palette colors are favored over darker ones. <c>0.0</c> disables the
    /// bias entirely (pure "closest color" / "most frequent color wins" behavior, the original default).
    /// Positive values (e.g. <c>0.35</c>, the default) penalize dark colors and reward bright ones so that
    /// black/dark tones are only chosen when they are strongly dominant, reducing large flat black areas in
    /// the output. Reasonable range is roughly 0.0 (no bias) to 1.0 (strong bias). Where in the pipeline this
    /// bias is applied is controlled by <paramref name="brightnessMode"/>.
    /// </param>
    /// <param name="brightnessMode">
    /// Selects which stage(s) of the conversion the <paramref name="brightnessBias"/> affects; see
    /// <see cref="BrightnessBiasMode"/>. Defaults to <see cref="BrightnessBiasMode.Both"/>, which applies the
    /// bias both when quantizing individual pixels to the C64 palette and when selecting the per-cell
    /// background/foreground colors from frequency counts. Using <see cref="BrightnessBiasMode.Selection"/>
    /// only (the original behavior) has little to no visible effect on flat, undithered images, because a
    /// dominant color's frequency count usually cannot be outweighed by the bias alone; enabling
    /// <see cref="BrightnessBiasMode.Quantization"/> makes the bias affect pixel-level color choice directly,
    /// which is visible even without dithering.
    /// </param>
    /// <returns>A <see cref="C64MulticolorData"/> object containing the bitmap data, color RAM, screen RAM and background color.</returns>
    public C64MulticolorData ConvertImage(MagickImage input, bool useDithering = true, double brightnessBias = 0.35,
        BrightnessBiasMode brightnessMode = BrightnessBiasMode.Both)
    {
        var result = new C64MulticolorData();

        // Step 1: Downscale from original size to 160x200
        using var downscaled = DownscaleImage(input);

        // Determine, for each stage, the effective bias to apply (0.0 = no effect, matches original behavior).
        bool applyAtQuantization = brightnessMode is BrightnessBiasMode.Quantization or BrightnessBiasMode.Both;
        bool applyAtSelection = brightnessMode is BrightnessBiasMode.Selection or BrightnessBiasMode.Both;
        double quantizationBias = applyAtQuantization ? brightnessBias : 0.0;
        double selectionBias = applyAtSelection ? brightnessBias : 0.0;

        // Step 2: Quantize colors to the C64 palette, with or without Floyd-Steinberg dithering
        using var quantized = useDithering
            ? ApplyDithering(downscaled, quantizationBias)
            : ApplyQuantization(downscaled, quantizationBias);

        // Step 3: Generate multicolor bitmap data, color RAM, screen RAM and background color
        GenerateMulticolorData(quantized, result, selectionBias);

        return result;
    }

    /// <summary>
    /// Generates the multicolor bitmap data, color RAM, screen RAM and background color for the C64.
    /// </summary>
    /// <param name="input">The dithered 160x200 image with C64 palette colors.</param>
    /// <param name="output">The output structure to populate with bitmap, RAM data and background color.</param>
    /// <param name="brightnessBias">See <see cref="ConvertImage"/> for details.</param>
    private void GenerateMulticolorData(MagickImage input, C64MulticolorData output, double brightnessBias)
    {
        // Get color indices for all pixels (160x200 working resolution, 1:1 with multicolor logical pixels)
        int[,] colorIndices = new int[SCREEN_WIDTH, SCREEN_HEIGHT];
        using (var inputPixels = input.GetPixels())
        {
            for (int y = 0; y < SCREEN_HEIGHT; y++)
            {
                for (int x = 0; x < SCREEN_WIDTH; x++)
                {
                    var pixel = inputPixels.GetPixel(x, y).ToColor()!;
                    colorIndices[x, y] = C64Palette.FindClosestColor(pixel.R, pixel.G, pixel.B);
                }
            }
        }

        // The background color (bit-pattern 00) is a single, screen-wide VIC-II register ($D021),
        // so it must be the same for the whole image: pick the most frequent color overall.
        int globalBackground = ComputeGlobalBackground(colorIndices, brightnessBias);
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
                int[] fgColors = SelectBestColors(blockColors, globalBackground, brightnessBias);

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
    /// Finds the color to be used as the screen-wide VIC-II background color, favoring the most
    /// frequent color across the whole (working-resolution) image but weighting the choice by
    /// brightness so that dark colors (e.g. black) are only picked when they are strongly dominant.
    /// </summary>
    /// <param name="colorIndices">Per-pixel palette indices for the whole working-resolution image.</param>
    /// <param name="brightnessBias">
    /// Bias strength; 0.0 reproduces the original "pure most frequent color" behavior.
    /// See <see cref="ConvertImage"/> for details.
    /// </param>
    private int ComputeGlobalBackground(int[,] colorIndices, double brightnessBias)
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
        double bestScore = -1;
        for (int i = 0; i < 16; i++)
        {
            double score = counts[i] * BrightnessWeight(i, brightnessBias);
            if (score > bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        return best;
    }

    /// <summary>
    /// Selects the best 3 foreground colors for a cell, given the fixed, screen-wide background color.
    /// The 3 colors in the block (excluding the background) with the highest brightness-weighted score
    /// are chosen, so that among colors with similar frequency, brighter ones are preferred over dark
    /// ones; if the block contains fewer than 3 distinct non-background colors, the remaining slots are
    /// filled with the background color itself (harmless, since GetMulticolorIndex will map matching
    /// pixels to index 0 anyway).
    /// </summary>
    /// <param name="blockColors">Palette indices of every pixel in the 4x8 cell block.</param>
    /// <param name="globalBackground">The fixed, screen-wide background color index for this image.</param>
    /// <param name="brightnessBias">Bias strength (0.0 = no effect); see <see cref="ConvertImage"/>.</param>
    private int[] SelectBestColors(int[] blockColors, int globalBackground, double brightnessBias)
    {
        int[] colorCounts = new int[16];
        foreach (int c in blockColors)
        {
            if (c >= 0 && c < 16)
                colorCounts[c]++;
        }

        var topColors = Enumerable.Range(0, 16)
            .Where(i => i != globalBackground && colorCounts[i] > 0)
            .OrderByDescending(i => colorCounts[i] * BrightnessWeight(i, brightnessBias))
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
