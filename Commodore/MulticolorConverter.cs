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
/// The C64 multicolor mode uses 8x8 pixel character cells, allowing 4 colors per cell:
/// one background color (shared across the entire screen) and 3 foreground colors (per cell).
/// Each pixel is represented by 2 bits in the bitmap data.
/// </summary>
public class MulticolorConverter
{
    /// <summary>The width of the C64 screen in multicolor pixels (160).</summary>
    private const int SCREEN_WIDTH = 160;

    /// <summary>The height of the C64 screen (200 pixels).</summary>
    private const int SCREEN_HEIGHT = 200;

    /// <summary>The width of a multicolor character cell block in pixels (8).</summary>
    private const int MULTICOLOR_BLOCK_WIDTH = 8;

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
    /// <returns>A <see cref="C64MulticolorData"/> object containing the bitmap data, color RAM, and screen RAM.</returns>
    public C64MulticolorData ConvertImage(SKBitmap input)
    {
        var result = new C64MulticolorData();

        // Step 1: Downscale from original size to 160x200
        using var downscaled = DownscaleImage(input);

        // Step 2: Apply Floyd-Steinberg dithering to C64 palette
        using var dithered = ApplyDithering(downscaled);

        // Step 3: Generate multicolor bitmap data, color RAM, and screen RAM
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
    /// Generates the multicolor bitmap data, color RAM, and screen RAM for the C64.
    /// </summary>
    /// <param name="input">The dithered 160x200 image with C64 palette colors.</param>
    /// <param name="output">The output structure to populate with bitmap and RAM data.</param>
    private void GenerateMulticolorData(SKBitmap input, C64MulticolorData output)
    {
        // Get color indices for all pixels
        int[,] colorIndices = new int[SCREEN_WIDTH, SCREEN_HEIGHT];
        for (int y = 0; y < SCREEN_HEIGHT; y++)
        {
            for (int x = 0; x < SCREEN_WIDTH; x++)
            {
                var pixel = input.GetPixel(x, y);
                colorIndices[x, y] = C64Palette.FindClosestColor(pixel.Red, pixel.Green, pixel.Blue);
            }
        }

        // Process each 8x8 character cell (40 columns x 25 rows)
        for (int cy = 0; cy < 25; cy++)
        {
            for (int cx = 0; cx < 40; cx++)
            {
                int screenOffset = cy * 40 + cx;
                int bitmapOffset = cy * 40 * 8 + cx * 8;

                // Extract the 8x8 block and find best 4 colors
                // Extract the 8x8 block (4 multicolor-pixel wide x 8 rows) and find best 4 colors
                var blockColors = ExtractBlockColors(colorIndices, cx * 4, cy * 8);

                // Select background (most frequent color) and 3 foreground colors
                var (bgColor, fgColors) = SelectBestColors(blockColors);

                // Generate bitmap data for this character
                for (int y = 0; y < 8; y++)
                {
                    byte bitmapByte = 0;
                    for (int x = 0; x < 4; x++)
                    {
                        int pixelX = cx * 4 + x;
                        int pixelY = cy * 8 + y;
                        int colorIndex = colorIndices[pixelX, pixelY];

                        // Find which of the 4 colors this is
                        int multicolorIndex = GetMulticolorIndex(colorIndex, bgColor, fgColors);

                        // Pack 2 bits per pixel, leftmost pixel in the high bits
                        bitmapByte |= (byte)((multicolorIndex & 0x03) << ((3 - x) * 2));
                    }
                    output.BitmapData[bitmapOffset + y] = bitmapByte;
                }

                // Set Color RAM (foreground color - we use the first foreground color as main)
                output.ColorRam[screenOffset] = (byte)fgColors[0];

                // Set Screen RAM (background color in lower nibble)
                output.ScreenRam[screenOffset] = (byte)(bgColor & 0x0F);
            }
        }
    }

    private int[] ExtractBlockColors(int[,] colorIndices, int startX, int startY)
    {
        var colors = new int[32]; // 4 (larghezza in pixel-multicolor) x 8 (altezza)
        int index = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                colors[index++] = colorIndices[startX + x, startY + y];
            }
        }
        return colors;
    }

    private (int bgColor, int[] fgColors) SelectBestColors(int[] blockColors)
    {
        int[] colorCounts = new int[16];
        foreach (int c in blockColors)
        {
            if (c >= 0 && c < 16)
                colorCounts[c]++;
        }

        int bgColor = 0;
        int maxCount = 0;
        for (int i = 0; i < 16; i++)
        {
            if (colorCounts[i] > maxCount)
            {
                maxCount = colorCounts[i];
                bgColor = i;
            }
        }

        int[] fgColors = new int[3];
        int[] fgCounts = new int[3];
        for (int i = 0; i < 16; i++)
        {
            if (i == bgColor) continue;

            for (int j = 0; j < 3; j++)
            {
                if (fgCounts[j] < colorCounts[i] || fgCounts[j] == 0)
                {
                    for (int k = 2; k > j; k--)
                    {
                        fgCounts[k] = fgCounts[k - 1];
                        fgColors[k] = fgColors[k - 1];
                    }
                    fgColors[j] = i;
                    fgCounts[j] = colorCounts[i];
                    break;
                }
            }
        }

        for (int i = 0; i < 3; i++)
        {
            if (fgColors[i] == 0 && fgCounts[i] == 0)
                fgColors[i] = bgColor;
        }

        return (bgColor, fgColors);
    }

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