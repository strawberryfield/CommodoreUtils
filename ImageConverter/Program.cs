//-----------------------------------------------------------------------
// <copyright file="ImageConverter/Program.cs" company="Casasoft">
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

using Casasoft.Commodore;
using Casasoft.Helpers;
using Mono.Options;
using SkiaSharp;
using System.Text;

#region main
bool ShouldShowHelp = false;
bool ShouldSuppressBanner = false;
string PixelAddressString = "$E000";
string ColorAddressString = "$C000";
ushort PixelAddress = Convert.ToUInt16(PixelAddressString.Substring(1), 16);
ushort ColorAddress = Convert.ToUInt16(ColorAddressString.Substring(1), 16);
string OutputFile = string.Empty;

OptionSet p = new()
{
    { "q|quiet", "Suppress banner print", v => ShouldSuppressBanner = v != null },
    { "h|?|help", "Show this help", v => ShouldShowHelp = v != null },
    { "p|pixeladdress=", "Pixel address (default 0xE000)", p => PixelAddressString = p },
    { "c|coloraddress=", "Color address (default 0xC000)", c => ColorAddressString = c },
    { "o|out=", "Output file name (default same as input with .PRG extension)", o => OutputFile = o },
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

IPrgFile prg;
foreach (string file in FilesList)
{
    string filename = OutputFile;
    if (string.IsNullOrWhiteSpace(OutputFile))
    {
        filename = file;
    }
    filename = Path.GetFileNameWithoutExtension(filename);

    // Load image using SkiaSharp
    using var fs = File.OpenRead(file);
    using var skBitmap = SkiaSharp.SKBitmap.Decode(fs);
    if (skBitmap == null)
    {
        Console.WriteLine($"Error: Failed to load image {file}");
        return;
    }

    if (skBitmap.Width != 320 || skBitmap.Height != 200)
    {
        Console.WriteLine($"Error: Image must be 320x200 pixels, got {skBitmap.Width}x{skBitmap.Height}");
        return;
    }

    MulticolorConverter converter = new MulticolorConverter();
    C64MulticolorData result = converter.ConvertImage(skBitmap);

    prg = new PrgFile(PixelAddress,result.BitmapData);
    prg.Save(filename+".bm.prg");
    prg = new PrgFile(ColorAddress, result.ScreenRam);
    prg.Save(filename+".sc.prg");
    prg = new PrgFile(0xD800, result.ColorRam);
    prg.Save(filename + ".co.prg");
}
#endregion

#region Procedures
void ShowHelp()
{
    Console.WriteLine("Usage: Text2Prg [OPTIONS] FILES");
    Console.WriteLine("Converts a text or a list of strings into a .PRG file\n");
    Console.WriteLine("Options:");
    p.WriteOptionDescriptions(Console.Out);
    Console.WriteLine($"\n{CommandLineHelpers.HexParameterNote}");
}

void ShowBanner() => Console.WriteLine("Casasoft Text2Prg v1.0\ncopyright (c) 2025 Roberto Ceccarelli - Casasoft\n");
#endregion