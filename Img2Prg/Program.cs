//-----------------------------------------------------------------------
// <copyright file="Img2Prg/Program.cs" company="Casasoft">
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
using Casasoft.Helpers;
using Mono.Options;

#region main
bool ShouldShowHelp = false;
bool ShouldSuppressBanner = false;
string StartAddressString = "$C000";
ushort StartAddress = Convert.ToUInt16(StartAddressString.Substring(1), 16);
string OutputFile = string.Empty;

OptionSet p = new()
{
    { "q|quiet", "Suppress banner print", v => ShouldSuppressBanner = v != null },
    { "h|?|help", "Show this help", v => ShouldShowHelp = v != null },
    { "a|address=", "Start address (default 0xC000)", a => StartAddressString = a },
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

if (!string.IsNullOrWhiteSpace(StartAddressString))
{
    StartAddress = (ushort)CommandLineHelpers.GetIntParameter(StartAddressString, StartAddress,
        "Invalid start address '{0}' using default $C000");
}

IPrgFile prg;
foreach (string file in FilesList)
{
    string filename = OutputFile;
    if (string.IsNullOrWhiteSpace(OutputFile))
    {
        filename = Path.ChangeExtension(file, ".prg");
    }

    string[] lines = File.ReadAllLines(file);
    byte[] data = new byte[2024];
    int addr = 0;
    foreach (string line in lines)
    {
        if (!line.StartsWith("BYTE")) continue;
        string values = line.Substring(4).Trim();
        string[] byteStrings = values.Split(',');
        foreach (string byteStr in byteStrings)
        {
            data[addr] = Convert.ToByte(byteStr.Trim());
            addr++;
        }
        if (addr == 1000) { addr = 1024; }
    }
    prg = new PrgFile(StartAddress, data);
    prg.Save(filename);
}

#endregion

#region Procedures
void ShowHelp()
{
    Console.WriteLine("Usage: Img2Prg [OPTIONS] FILES");
    Console.WriteLine("Converts a text list of bytes of an image into a .PRG file\n");
    Console.WriteLine("Options:");
    p.WriteOptionDescriptions(Console.Out);
    Console.WriteLine($"\n{CommandLineHelpers.HexParameterNote}");
}

void ShowBanner() => Console.WriteLine("Casasoft Img2Prg v1.0\ncopyright (c) 2026 Roberto Ceccarelli - Casasoft\n");
#endregion