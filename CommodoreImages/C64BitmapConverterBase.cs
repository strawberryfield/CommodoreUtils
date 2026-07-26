//-----------------------------------------------------------------------
// <copyright file="C64BitmapConverterBase.cs" company="Casasoft">
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
/// Provides the image-to-C64-palette conversion pipeline shared by <see cref="MulticolorConverter"/>
/// and <see cref="HiresConverter"/>: downscaling to the working resolution, Floyd-Steinberg dithering
/// (or plain nearest-color quantization) with an optional brightness bias, and the low-level
/// brightness-weighted color-matching helpers used when selecting per-cell colors.
/// </summary>
/// <remarks>
/// Concrete converters only need to supply their working resolution (<see cref="ScreenWidth"/>/
/// <see cref="ScreenHeight"/>) and character-cell size (<see cref="CellPixelWidth"/>/
/// <see cref="CellPixelHeight"/>); everything else needed to get from a source <see cref="MagickImage"/>
/// to a quantized, palette-mapped working-resolution bitmap lives here. Packing the quantized pixels
/// into the final C64-specific bitmap/screen-RAM/color-RAM layout remains the responsibility of each
/// subclass, since multicolor and hires modes differ in how many colors a cell can hold and how they
/// are stored.
/// </remarks>
public class C64BitmapConverterBase
{
    /// <summary>Number of character columns on screen (40).</summary>
    protected const int CHAR_COLUMNS = 40;

    /// <summary>Number of character rows on screen (25).</summary>
    protected const int CHAR_ROWS = 25;

    /// <summary>Gets the width, in pixels, of the working resolution used by this converter.</summary>
    protected int ScreenWidth { get; }

    /// <summary>Gets the height, in pixels, of the working resolution used by this converter.</summary>
    protected int ScreenHeight { get; }

    /// <summary>Gets the width, in pixels, of a character cell for this converter's bitmap mode.</summary>
    protected int CellPixelWidth { get; }

    /// <summary>Gets the height, in pixels, of a character cell for this converter's bitmap mode.</summary>
    protected int CellPixelHeight { get; }

    /// <summary>
    /// Perceptual luminance (0..255) of each C64 palette color, used to bias color selection
    /// towards brighter colors.
    /// </summary>
    protected static readonly double[] PaletteLuminance = C64Palette.ColorsRgb
        .Select(c => 0.299 * c.r + 0.587 * c.g + 0.114 * c.b)
        .ToArray();

    /// <summary>
    /// Represents accumulated color error values for Floyd-Steinberg dithering.
    /// Stores separate error values for red, green, and blue channels.
    /// </summary>
    protected struct Error
    {
        /// <summary>Red channel error accumulation.</summary>
        public double R;

        /// <summary>Green channel error accumulation.</summary>
        public double G;

        /// <summary>Blue channel error accumulation.</summary>
        public double B;
    }

    /// <summary>
    /// Downscales the input image to this converter's working resolution
    /// (<see cref="ScreenWidth"/> x <see cref="ScreenHeight"/>).
    /// </summary>
    /// <param name="input">The source image to downscale.</param>
    /// <returns>A new SKBitmap resized to the working resolution.</returns>
    protected MagickImage DownscaleImage(MagickImage input)
    {
        var output = (MagickImage)input.Clone();
        output.HasAlpha = false;

        // Linear filter with mipmap-like smoothing, equivalent to a high-quality resize.
        output.FilterType = FilterType.Triangle;

        var geometry = new MagickGeometry((uint)ScreenWidth, (uint)ScreenHeight)
        {
            IgnoreAspectRatio = true
        };
        output.Resize(geometry);

        return output;
    }
    /// <summary>
    /// Applies Floyd–Steinberg dithering to the supplied <see cref="MagickImage"/> and quantizes colors to the Commodore 64 palette.
    /// Produces and returns a new <see cref="MagickImage"/> of size <c>ScreenWidth</c> x <c>ScreenHeight</c> with alpha disabled.
    /// </summary>
    /// <param name="input">Source image. Expected to contain at least <c>ScreenWidth</c> by <c>ScreenHeight</c> pixels; otherwise pixel access may fail.</param>
    /// <param name="brightnessBias">
    /// Optional bias applied when selecting the nearest C64 palette color. A value of <c>0.0</c> applies no bias;
    /// positive values favor brighter palette entries when resolving nearest-color ties or close matches.
    /// </param>
    /// <returns>
    /// A dithered <see cref="MagickImage"/> where every pixel color is taken from <c>C64Palette.ColorsRgb</c>.
    /// The returned image has <c>HasAlpha</c> set to <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Implementation details:
    /// - Uses an error accumulation buffer sized (<c>ScreenWidth + 2</c>, <c>ScreenHeight + 2</c>) to store fractional per-channel quantization errors.
    /// - For each pixel:
    ///   1. Read input pixel and add accumulated error from the buffer.
    ///   2. Clamp RGB to [0,255].
    ///   3. Choose nearest C64 palette color via <see cref="FindClosestColorBiased(byte, byte, byte, double)"/>.
    ///   4. Write the quantized palette color to the output.
    ///   5. Compute quantization error and diffuse it using Floyd–Steinberg weights:
    ///      - right: 7/16
    ///      - bottom-left: 3/16
    ///      - bottom: 5/16
    ///      - bottom-right: 1/16
    /// - Boundary pixels are handled with conditional updates so no out-of-range buffer accesses occur.
    /// - The method uses <c>GetPixels()</c> / <c>SetPixel()</c> for direct pixel manipulation and returns a newly allocated <see cref="MagickImage"/>; callers should dispose it when appropriate.
    /// </remarks>
    protected MagickImage ApplyDithering(MagickImage input, double brightnessBias = 0.0)
    {
        var output = new MagickImage(MagickColors.Black, (uint)ScreenWidth, (uint)ScreenHeight);
        output.HasAlpha = false;
        var errorBuffer = new Error[ScreenWidth + 2, ScreenHeight + 2];

        using var inputPixels = input.GetPixels();
        using var outputPixels = output.GetPixels();

        // Process each pixel
        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                var pixel = inputPixels.GetPixel(x, y).ToColor()!;

                // Add error diffusion
                int r = pixel.R + (int)Math.Round(errorBuffer[x, y].R);
                int g = pixel.G + (int)Math.Round(errorBuffer[x, y].G);
                int b = pixel.B + (int)Math.Round(errorBuffer[x, y].B);

                // Clamp values
                r = Math.Max(0, Math.Min(255, r));
                g = Math.Max(0, Math.Min(255, g));
                b = Math.Max(0, Math.Min(255, b));

                // Find closest C64 color, optionally biased towards brighter palette colors
                int closestColor = FindClosestColorBiased((byte)r, (byte)g, (byte)b, brightnessBias);
                var c64Color = C64Palette.ColorsRgb[closestColor];

                outputPixels.SetPixel(x, y, new byte[] { c64Color.r, c64Color.g, c64Color.b });

                // Calculate error
                int errR = r - c64Color.r;
                int errG = g - c64Color.g;
                int errB = b - c64Color.b;

                // Floyd-Steinberg error diffusion
                if (x < ScreenWidth - 1)
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
                if (y < ScreenHeight - 1)
                {
                    errorBuffer[x, y + 1].R += errR * 5.0 / 16.0;
                    errorBuffer[x, y + 1].G += errG * 5.0 / 16.0;
                    errorBuffer[x, y + 1].B += errB * 5.0 / 16.0;
                }
                if (x < ScreenWidth - 1 && y < ScreenHeight - 1)
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
    /// <param name="input">The working-resolution image to quantize.</param>
    /// <param name="brightnessBias">
    /// Brightness bias applied while picking the closest C64 palette color for each pixel (see
    /// <see cref="FindClosestColorBiased"/>). <c>0.0</c> reproduces the original unbiased nearest-color
    /// behavior.
    /// </param>
    /// <returns>A quantized SKBitmap with colors mapped to the C64 palette, without dithering.</returns>
    protected MagickImage ApplyQuantization(MagickImage input, double brightnessBias = 0.0)
    {
        var output = new MagickImage(MagickColors.Black, (uint)ScreenWidth, (uint)ScreenHeight);
        output.HasAlpha = false;

        using var inputPixels = input.GetPixels();
        using var outputPixels = output.GetPixels();

        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                var pixel = inputPixels.GetPixel(x, y).ToColor()!;

                // Find closest C64 color, optionally biased towards brighter palette colors; no error diffusion applied
                int closestColor = FindClosestColorBiased(pixel.R, pixel.G, pixel.B, brightnessBias);
                var c64Color = C64Palette.ColorsRgb[closestColor];

                outputPixels.SetPixel(x, y, new byte[] { c64Color.r, c64Color.g, c64Color.b });
            }
        }

        return output;
    }

    /// <summary>
    /// Computes a multiplicative weight for palette color <paramref name="colorIndex"/> based on its
    /// luminance: colors brighter than mid-gray get a weight above 1.0 (favored), darker colors get a
    /// weight below 1.0 (penalized). Used to nudge color-frequency-based selection towards brighter
    /// colors without ignoring frequency entirely.
    /// </summary>
    /// <param name="colorIndex">Index (0-15) into the C64 palette.</param>
    /// <param name="brightnessBias">Bias strength (0.0 = no effect).</param>
    protected static double BrightnessWeight(int colorIndex, double brightnessBias) =>
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
    /// ties or near-ties against darker colors; strongly dominant colors (much closer in RGB space than any
    /// alternative) are still selected regardless of bias, since the bias only reshuffles close calls.
    /// </param>
    /// <returns>The index (0-15) of the selected C64 palette color.</returns>
    /// <remarks>
    /// This operates at the individual-pixel level, so it also affects undithered (flat, plain
    /// nearest-color) quantization output, where large uniform regions would otherwise be completely
    /// unaffected by a purely count-based bias applied only at selection time.
    /// </remarks>
    protected static int FindClosestColorBiased(byte r, byte g, byte b, double brightnessBias)
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

            // Dividing by the brightness weight makes brighter colors "appear" closer than they
            // strictly are, biasing near-ties (and only near-ties) towards brighter palette entries.
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
    /// Extracts the color indices of a <see cref="CellPixelWidth"/> x <see cref="CellPixelHeight"/> block
    /// starting at (startX, startY).
    /// </summary>
    protected int[] ExtractBlockColors(int[,] colorIndices, int startX, int startY)
    {
        var colors = new int[CellPixelWidth * CellPixelHeight];
        int index = 0;
        for (int y = 0; y < CellPixelHeight; y++)
        {
            for (int x = 0; x < CellPixelWidth; x++)
            {
                colors[index++] = colorIndices[startX + x, startY + y];
            }
        }
        return colors;
    }
}