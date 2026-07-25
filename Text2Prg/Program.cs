//-----------------------------------------------------------------------
// <copyright file="Text2Prg/Program.cs" company="Casasoft">
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
using Casasoft.Commodore.Prg;
using Casasoft.Helpers;
using Mono.Options;
using System.Text;

#region main
bool ShouldShowHelp = false;
bool ShouldSuppressBanner = false;
bool ShouldUseUppercaseOnly = false;
bool ListMode = false;
bool stringsOnly = false;
string StartAddressString = "$C000";
ushort StartAddress = Convert.ToUInt16(StartAddressString.Substring(1), 16);
string OutputFile = string.Empty;

OptionSet p = new()
{
    { "q|quiet", "Suppress banner print", v => ShouldSuppressBanner = v != null },
    { "h|?|help", "Show this help", v => ShouldShowHelp = v != null },
    { "u|uppercase", "Use uppercase only charset", v => ShouldUseUppercaseOnly = v != null },
    { "l|listmode", "Convert to a list of strings", v => ListMode = v != null },
    { "a|address=", "Start address (default 0xC000)", a => StartAddressString = a },
    { "s|stringsonly", "Do not include index", v => stringsOnly = v != null  },
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

    if (ListMode)
    {
        List<string> lines = File.ReadAllLines(file).ToList();
        prg = new Strings2Prg(StartAddress, lines, !ShouldUseUppercaseOnly, !stringsOnly);
    }
    else
    {
        string text = TextHelpers.NormalizeEol(File.ReadAllText(file), TextHelpers.EolType.CR);
        text = Charset.PETSCII(text, !ShouldUseUppercaseOnly) + (char)0;
        prg = new PrgFile(StartAddress, Encoding.ASCII.GetBytes(text));
    }
    prg.Save(filename);
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