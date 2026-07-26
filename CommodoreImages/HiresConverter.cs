//-----------------------------------------------------------------------
// <copyright file="HiresConverter.cs" company="Casasoft">
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

using SkiaSharp;

namespace Casasoft.Commodore.Images;

/// <summary>
/// Converts RGB images to Commodore 64 standard (hires, 2-color) bitmap format using SkiaSharp.
///
/// This class implements the same three-stage conversion process as <see cref="MulticolorConverter"/>,
/// adapted to the full 320x200 hires resolution and 2-colors-per-cell constraint:
/// <list type="number">
/// <item><description>Downscales the input image to 320x200 pixels using resize.</description></item>
/// <item><description>Applies Floyd-Steinberg dithering to quantize colors to the C64 palette while minimizing color loss.</description></item>
/// <item><description>Generates hires bitmap data and screen RAM compatible with C64 hardware.</description></item>
/// </list>
///
/// The C64 standard bitmap (hires) mode uses 8x8 pixel character cells at full 320x200 resolution
/// (1 bit per pixel, no double-wide pixels as in multicolor mode). Each cell can use up to 2 colors,
/// both stored in Screen RAM (there is no shared/global background register involved, unlike multicolor mode):
/// <list type="bullet">
/// <item><description>bit value 0 -&gt; Screen RAM low nibble (per-cell background color)</description></item>
/// <item><description>bit value 1 -&gt; Screen RAM high nibble (per-cell foreground color)</description></item>
/// </list>
/// </summary>
public class HiresConverter
{
    /// <summary>The width of the C64 screen in hires pixels (320).</summary>
    private const int SCREEN_WIDTH = 320;

    /// <summary>The height of the C64 screen (200 pixels).</summary>
    private const int SCREEN_HEIGHT = 200;

    /// <summary>The width, in pixels, of a character cell (8).</summary>
    private const int CELL_PIXEL_WIDTH = 8;

    /// <summary>The height, in pixels, of a character cell (8).</summary>
    private const int CELL_PIXEL_HEIGHT = 8;

    /// <summary>Number of character columns on screen (40).</summary>
    private const int CHAR_COLUMNS = 40;

    /// <summary>Number of character rows on screen (25).</summary>
    private const int CHAR_ROWS = 25;

    /// <summary>
    /// Perceptual luminance (0..255) of each C64 palette color, used to bias color selection
    /// towards brighter colors when picking the per-cell background/foreground colors.
    /// </summary>
    private static readonly double[] PaletteLuminance = C64Palette.ColorsRgb
        .Select(c => 0.299 * c.r + 0.587 * c.g + 0.114 * c.b)
        .ToArray();

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
    /// Converts an RGB image to C64 hires (2-color) format.
    /// </summary>
    /// <param name="input">The input image as an SKBitmap to be converted.</param>
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
    /// background/foreground colors from frequency counts.
    /// </param>
    /// <returns>A <see cref="C64HiresData"/> object containing the bitmap data and screen RAM.</returns>
    public C64HiresData ConvertImage(SKBitmap input, bool useDithering = true, double brightnessBias = 0.35,
        BrightnessBiasMode brightnessMode = BrightnessBiasMode.Both)
    {
        var result = new C64HiresData();

        // Step 1: Downscale from original size to 320x200
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

        // Step 3: Generate hires bitmap data and screen RAM
        GenerateHiresData(quantized, result, selectionBias);

        return result;
    }

    /// <summary>
    /// Downscales the input image to C64 hires screen dimensions (320x200).
    /// </summary>
    /// <param name="input">The source image to downscale.</param>
    /// <returns>A new SKBitmap resized to 320x200 pixels.</returns>
    private SKBitmap DownscaleImage(SKBitmap input)
    {
        var info = new SKImageInfo(SCREEN_WIDTH, SCREEN_HEIGHT, input.ColorType, input.AlphaType);
        var output = new SKBitmap(info);

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
    /// <param name="input">The 320x200 image to dither.</param>
    /// <param name="brightnessBias">
    /// Brightness bias applied while picking the closest C64 palette color for each pixel (see
    /// <see cref="FindClosestColorBiased"/>). <c>0.0</c> reproduces the original unbiased nearest-color
    /// behavior.
    /// </param>
    /// <returns>A dithered SKBitmap with colors quantized to the C64 palette.</returns>
    private SKBitmap ApplyDithering(SKBitmap input, double brightnessBias = 0.0)
    {
        var info = new SKImageInfo(SCREEN_WIDTH, SCREEN_HEIGHT, SKColorType.Rgba8888, SKAlphaType.Opaque);
        var output = new SKBitmap(info);
        var errorBuffer = new Error[SCREEN_WIDTH + 2, SCREEN_HEIGHT + 2];

        for (int y = 0; y < SCREEN_HEIGHT; y++)
        {
            for (int x = 0; x < SCREEN_WIDTH; x++)
            {
                var pixel = input.GetPixel(x, y);

                int r = pixel.Red + (int)Math.Round(errorBuffer[x, y].R);
                int g = pixel.Green + (int)Math.Round(errorBuffer[x, y].G);
                int b = pixel.Blue + (int)Math.Round(errorBuffer[x, y].B);

                r = Math.Max(0, Math.Min(255, r));
                g = Math.Max(0, Math.Min(255, g));
                b = Math.Max(0, Math.Min(255, b));

                int closestColor = FindClosestColorBiased((byte)r, (byte)g, (byte)b, brightnessBias);
                var c64Color = C64Palette.ColorsRgb[closestColor];

                output.SetPixel(x, y, new SKColor((byte)c64Color.r, (byte)c64Color.g, (byte)c64Color.b));

                int errR = r - c64Color.r;
                int errG = g - c64Color.g;
                int errB = b - c64Color.b;

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
    /// Converts the image to the C64 color palette by mapping each pixel to its closest palette color,
    /// with no error diffusion (i.e. no dithering).
    /// </summary>
    /// <param name="input">The 320x200 image to quantize.</param>
    /// <param name="brightnessBias">
    /// Brightness bias applied while picking the closest C64 palette color for each pixel (see
    /// <see cref="FindClosestColorBiased"/>). <c>0.0</c> reproduces the original unbiased nearest-color
    /// behavior.
    /// </param>
    /// <returns>A quantized SKBitmap with colors mapped to the C64 palette, without dithering.</returns>
    private SKBitmap ApplyQuantization(SKBitmap input, double brightnessBias = 0.0)
    {
        var info = new SKImageInfo(SCREEN_WIDTH, SCREEN_HEIGHT, SKColorType.Rgba8888, SKAlphaType.Opaque);
        var output = new SKBitmap(info);

        for (int y = 0; y < SCREEN_HEIGHT; y++)
        {
            for (int x = 0; x < SCREEN_WIDTH; x++)
            {
                var pixel = input.GetPixel(x, y);

                int closestColor = FindClosestColorBiased(pixel.Red, pixel.Green, pixel.Blue, brightnessBias);
                var c64Color = C64Palette.ColorsRgb[closestColor];

                output.SetPixel(x, y, new SKColor((byte)c64Color.r, (byte)c64Color.g, (byte)c64Color.b));
            }
        }

        return output;
    }

    /// <summary>
    /// Generates the hires bitmap data and screen RAM for the C64.
    /// </summary>
    /// <param name="input">The dithered 320x200 image with C64 palette colors.</param>
    /// <param name="output">The output structure to populate with bitmap and screen RAM data.</param>
    /// <param name="brightnessBias">See <see cref="ConvertImage"/> for details.</param>
    private void GenerateHiresData(SKBitmap input, C64HiresData output, double brightnessBias)
    {
        // Get color indices for all pixels (320x200 working resolution, 1:1 with hires pixels)
        int[,] colorIndices = new int[SCREEN_WIDTH, SCREEN_HEIGHT];
        for (int y = 0; y < SCREEN_HEIGHT; y++)
        {
            for (int x = 0; x < SCREEN_WIDTH; x++)
            {
                var pixel = input.GetPixel(x, y);
                colorIndices[x, y] = C64Palette.FindClosestColor(pixel.Red, pixel.Green, pixel.Blue);
            }
        }

        // Process each 8x8 character cell (40 columns x 25 rows). Unlike multicolor mode there is no
        // shared/global background register: both colors available to a cell are entirely local to it.
        for (int cy = 0; cy < CHAR_ROWS; cy++)
        {
            for (int cx = 0; cx < CHAR_COLUMNS; cx++)
            {
                int screenOffset = cy * CHAR_COLUMNS + cx;
                int bitmapOffset = cy * CHAR_COLUMNS * CELL_PIXEL_HEIGHT + cx * CELL_PIXEL_HEIGHT;

                // Extract the 8x8 block and find the best 2 colors for this cell
                var blockColors = ExtractBlockColors(colorIndices, cx * CELL_PIXEL_WIDTH, cy * CELL_PIXEL_HEIGHT);
                (int bgColor, int fgColor) = SelectCellColors(blockColors, brightnessBias);

                // Generate bitmap data for this character cell
                for (int y = 0; y < CELL_PIXEL_HEIGHT; y++)
                {
                    byte bitmapByte = 0;
                    for (int x = 0; x < CELL_PIXEL_WIDTH; x++)
                    {
                        int pixelX = cx * CELL_PIXEL_WIDTH + x;
                        int pixelY = cy * CELL_PIXEL_HEIGHT + y;
                        int colorIndex = colorIndices[pixelX, pixelY];

                        int bit = GetHiresBit(colorIndex, bgColor, fgColor);

                        int shift = CELL_PIXEL_WIDTH - 1 - x;
                        bitmapByte |= (byte)((bit & 0x01) << shift);
                    }
                    output.BitmapData[bitmapOffset + y] = bitmapByte;
                }

                // Screen RAM: high nibble = foreground color, low nibble = background color
                output.ScreenRam[screenOffset] = (byte)(((fgColor & 0x0F) << 4) | (bgColor & 0x0F));
            }
        }
    }

    /// <summary>
    /// Computes a multiplicative weight for palette color <paramref name="colorIndex"/> based on its
    /// luminance: colors brighter than mid-gray get a weight above 1.0 (favored), darker colors get a
    /// weight below 1.0 (penalized). Used to nudge color-frequency-based selection towards brighter
    /// colors without ignoring frequency entirely.
    /// </summary>
    /// <param name="colorIndex">Index (0-15) into the C64 palette.</param>
    /// <param name="brightnessBias">Bias strength (0.0 = no effect); see <see cref="ConvertImage"/>.</param>
    private static double BrightnessWeight(int colorIndex, double brightnessBias) =>
        1.0 + brightnessBias * (PaletteLuminance[colorIndex] / 255.0 - 0.5);

    /// <summary>
    /// Finds the index of the closest matching C64 palette color for a given RGB value, optionally biasing
    /// the choice towards brighter palette colors.
    /// </summary>
    /// <param name="r">The red component (0-255).</param>
    /// <param name="g">The green component (0-255).</param>
    /// <param name="b">The blue component (0-255).</param>
    /// <param name="brightnessBias">
    /// Bias strength; <c>0.0</c> makes this behave exactly like <see cref="C64Palette.FindClosestColor"/>
    /// (plain nearest-color by Euclidean RGB distance). Positive values divide each candidate color's squared
    /// distance by its <see cref="BrightnessWeight"/>, so brighter colors effectively appear "closer" and win
    /// ties or near-ties against darker colors.
    /// </param>
    /// <returns>The index (0-15) of the selected C64 palette color.</returns>
    private static int FindClosestColorBiased(byte r, byte g, byte b, double brightnessBias)
    {
        if (brightnessBias == 0.0)
        {
            return C64Palette.FindClosestColor(r, g, b);
        }

        int closestIndex = 0;
        double minScore = double.MaxValue;

        for (int i = 0; i < C64Palette.ColorsRgb.Length; i++)
        {
            var c = C64Palette.ColorsRgb[i];
            int dr = r - c.r;
            int dg = g - c.g;
            int db = b - c.b;
            double distance = dr * dr + dg * dg + db * db;

            double score = distance / BrightnessWeight(i, brightnessBias);

            if (score < minScore)
            {
                minScore = score;
                closestIndex = i;
            }
        }

        return closestIndex;
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
    /// Selects the best 2 colors (background and foreground) for a cell out of the colors found in its
    /// 8x8 block, favoring the most frequent colors weighted by brightness so that among colors with
    /// similar frequency, brighter ones are preferred over dark ones. The single most frequent (weighted)
    /// color becomes the cell's background; the second most frequent becomes its foreground. If the block
    /// contains only 1 distinct color, both background and foreground are set to it (harmless, since
    /// <see cref="GetHiresBit"/> will map every pixel in the cell to bit 0 in that case).
    /// </summary>
    /// <param name="blockColors">Palette indices of every pixel in the 8x8 cell block.</param>
    /// <param name="brightnessBias">Bias strength (0.0 = no effect); see <see cref="ConvertImage"/>.</param>
    /// <returns>A tuple of (background color index, foreground color index).</returns>
    private (int Background, int Foreground) SelectCellColors(int[] blockColors, double brightnessBias)
    {
        int[] colorCounts = new int[16];
        foreach (int c in blockColors)
        {
            if (c >= 0 && c < 16)
                colorCounts[c]++;
        }

        var topColors = Enumerable.Range(0, 16)
            .Where(i => colorCounts[i] > 0)
            .OrderByDescending(i => colorCounts[i] * BrightnessWeight(i, brightnessBias))
            .Take(2)
            .ToList();

        while (topColors.Count < 2)
            topColors.Add(topColors.Count > 0 ? topColors[0] : 0);

        return (topColors[0], topColors[1]);
    }

    /// <summary>
    /// Maps a pixel's palette color index to one of the 2 colors available for its cell
    /// (background or foreground), choosing the closest match if there is no exact hit.
    /// </summary>
    /// <returns>0 if the pixel maps to the background color, 1 if it maps to the foreground color.</returns>
    private int GetHiresBit(int pixelColor, int bgColor, int fgColor)
    {
        if (pixelColor == bgColor) return 0;
        if (pixelColor == fgColor) return 1;

        var bg = C64Palette.ColorsRgb[bgColor];
        var fg = C64Palette.ColorsRgb[fgColor];
        var px = C64Palette.ColorsRgb[pixelColor];

        int distBg = (bg.r - px.r) * (bg.r - px.r) +
                     (bg.g - px.g) * (bg.g - px.g) +
                     (bg.b - px.b) * (bg.b - px.b);

        int distFg = (fg.r - px.r) * (fg.r - px.r) +
                     (fg.g - px.g) * (fg.g - px.g) +
                     (fg.b - px.b) * (fg.b - px.b);

        return distFg < distBg ? 1 : 0;
    }
}