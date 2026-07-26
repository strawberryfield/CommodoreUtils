//-----------------------------------------------------------------------
// <copyright file="ImageConverter/Program.cs" company="Casasoft">
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

using Casasoft.Commodore;
using Casasoft.Commodore.Images;
using Casasoft.Helpers;
using ImageMagick;
using Mono.Options;

#region main
bool ShouldShowHelp = false;
bool ShouldSuppressBanner = false;
string PixelAddressString = "$E000";
string ColorAddressString = "$C000";
string BackgroundAddressString = "$D021";
ushort PixelAddress = Convert.ToUInt16(PixelAddressString.Substring(1), 16);
ushort ColorAddress = Convert.ToUInt16(ColorAddressString.Substring(1), 16);
ushort BackgroundAddress = Convert.ToUInt16(BackgroundAddressString.Substring(1), 16);
string OutputFile = string.Empty;
bool UseDithering = true;
double BrightnessBias = 0.35;
string BrightnessBiasString = string.Empty;
string BrightnessModeString = "both";
BrightnessBiasMode BrightnessMode = BrightnessBiasMode.Both;
bool UseHires = false;

OptionSet p = new()
{
    { "q|quiet", "Suppress banner print", v => ShouldSuppressBanner = v != null },
    { "h|?|help", "Show this help", v => ShouldShowHelp = v != null },
    { "2|hires", "Convert to 2-color (hires, standard bitmap) format instead of multicolor (default: multicolor)", h => UseHires = h != null },
    { "p|pixeladdress=", "Pixel address (default 0xE000)", p => PixelAddressString = p },
    { "c|coloraddress=", "Color address (default 0xC000)", c => ColorAddressString = c },
    { "b|backgroundaddress=", "Background color address (default 0xD021, the VIC-II background register)", b => BackgroundAddressString = b },
    { "o|out=", "Output file name (default same as input with .PRG extension)", o => OutputFile = o },
    { "d|no-dither", "Disable Floyd-Steinberg dithering (plain nearest-color quantization)", d => UseDithering = d == null },
    { "brightness=", "Brightness bias 0.0-1.0 favoring brighter colors over dark ones when choosing background/foreground colors (default 0.35, 0 = original behavior). Accepts '.' or ',' as decimal separator", v => BrightnessBiasString = v },
    { "brightness-mode=", "Where to apply the brightness bias: 'quantization' (per-pixel color choice, visible even without dithering), 'selection' (background/foreground color choice from frequency counts, original behavior), or 'both' (default)", m => BrightnessModeString = m },
};

List<string> FilesList = FileHelpers.ExpandWildcards(p.Parse(args));

if (!ShouldSuppressBanner)
    ShowBanner();

if (ShouldShowHelp || FilesList.Count == 0)
{
    ShowHelp();
    return;
}

if (!string.IsNullOrWhiteSpace(PixelAddressString))
{
    PixelAddress = (ushort)CommandLineHelpers.GetIntParameter(PixelAddressString, PixelAddress,
        "Invalid pixel address '{0}' using default $E000");
}

if (!string.IsNullOrWhiteSpace(ColorAddressString))
{
    ColorAddress = (ushort)CommandLineHelpers.GetIntParameter(ColorAddressString, ColorAddress,
        "Invalid color address '{0}' using default $C000");
}

if (!string.IsNullOrWhiteSpace(BackgroundAddressString))
{
    BackgroundAddress = (ushort)CommandLineHelpers.GetIntParameter(BackgroundAddressString, BackgroundAddress,
        "Invalid background address '{0}' using default $D021");
}

if (!string.IsNullOrWhiteSpace(BrightnessBiasString))
{
    BrightnessBias = CommandLineHelpers.GetDoubleParameter(BrightnessBiasString, BrightnessBias,
        "Invalid brightness value '{0}' using default 0.35");
}

if (!string.IsNullOrWhiteSpace(BrightnessModeString))
{
    if (!Enum.TryParse(BrightnessModeString, true, out BrightnessMode))
    {
        Console.WriteLine($"Invalid brightness mode '{BrightnessModeString}', using default 'both'");
        BrightnessMode = BrightnessBiasMode.Both;
    }
}

IPrgFile prg;
foreach (string file in FilesList)
{
    string filename = OutputFile;
    if (string.IsNullOrWhiteSpace(OutputFile))
    {
        filename = file;
    }
    filename = Path.Combine(Path.GetDirectoryName(filename) ?? string.Empty, Path.GetFileNameWithoutExtension(filename));

    // Load image using Magick.NET (ImageMagick)
    MagickImage magickImage;
    try
    {
        magickImage = new MagickImage(file);
    }
    catch (MagickException ex)
    {
        Console.WriteLine($"Error: Failed to load image {file} ({ex.Message})");
        return;
    }
    using (magickImage)
    {
        if (magickImage.Width != 320 || magickImage.Height != 200)
        {
            Console.WriteLine($"Error: Image must be 320x200 pixels, got {magickImage.Width}x{magickImage.Height}");
            return;
        }

        if (UseHires)
        {
            HiresConverter hiresConverter = new HiresConverter();
            C64HiresData hiresResult = hiresConverter.ConvertImage(magickImage, UseDithering, BrightnessBias, BrightnessMode);

            prg = new PrgFile(PixelAddress, hiresResult.BitmapData);
            prg.Save(filename + ".bm.prg");
            prg = new PrgFile(ColorAddress, hiresResult.ScreenRam);
            prg.Save(filename + ".sc.prg");

            // Standard hires bitmap mode has no Color RAM and does not use the VIC-II background
            // register ($D021): both colors of every cell live entirely in Screen RAM, so no
            // .co.prg or .bg.prg files are produced here (unlike the multicolor converter below).
        }
        else
        {
            MulticolorConverter converter = new MulticolorConverter();
            C64MulticolorData result = converter.ConvertImage(magickImage, UseDithering, BrightnessBias, BrightnessMode);

            prg = new PrgFile(PixelAddress, result.BitmapData);
            prg.Save(filename + ".bm.prg");
            prg = new PrgFile(ColorAddress, result.ScreenRam);
            prg.Save(filename + ".sc.prg");
            prg = new PrgFile(0xD800, result.ColorRam);
            prg.Save(filename + ".co.prg");

            // Background color (VIC-II "00" bit-pattern) is a single, screen-wide register value.
            // By default it targets $D021 directly: loading this 1-byte PRG with
            // LOAD"filename.bg.prg",8,1 writes the byte straight into the background
            // color register, since $D021 sits in the CPU-visible I/O area.
            prg = new PrgFile(BackgroundAddress, new byte[] { result.BackgroundColor });
            prg.Save(filename + ".bg.prg");
        }
    }
}
#endregion

#region Procedures
void ShowHelp()
{
    Console.WriteLine("Usage: ImageConverter [OPTIONS] FILES");
    Console.WriteLine("Converts an image into .PRG files\n");
    Console.WriteLine("Options:");
    p.WriteOptionDescriptions(Console.Out);
    Console.WriteLine($"\n{CommandLineHelpers.HexParameterNote}");
}

void ShowBanner() => Console.WriteLine("Casasoft ImageConverter v1.0\ncopyright (c) 2026 Roberto Ceccarelli - Casasoft\n");
#endregion
