//-----------------------------------------------------------------------
// <copyright file="A2Petscii/Program.cs" company="Casasoft">
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

#region main
bool ShouldShowHelp = false;
bool ShouldSuppressBanner = false;
bool ShouldUseUppercaseOnly = false;
string WrapSizeString = "0";
int WrapSize = Convert.ToInt16(WrapSizeString);
string OutputFile = string.Empty;

OptionSet p = new()
{
    { "q|quiet", "Suppress banner print", v => ShouldSuppressBanner = v != null },
    { "h|?|help", "Show this help", v => ShouldShowHelp = v != null },
    { "u|uppercase", "Use uppercase only charset", v => ShouldUseUppercaseOnly = v != null },
    { "w|wrap=", "Word-wrap at colum (default=0, set to 0 for no wrap)", o => WrapSizeString = o },
    { "o|out=", "Output file name (default same as input with .seq extension)", o => OutputFile = o },
};

List<string> FilesList = FileHelpers.ExpandWildcards(p.Parse(args));

if (!ShouldSuppressBanner)
    ShowBanner();

if (ShouldShowHelp || FilesList.Count == 0)
{
    ShowHelp();
    return;
}

if (!string.IsNullOrWhiteSpace(WrapSizeString))
{
    WrapSize = CommandLineHelpers.GetIntParameter(WrapSizeString, 0, "Invalid wrap size '{0}' using default 0 (no wrap)");
    if (WrapSize < 0)
    {
        WrapSize = 0;
    }
}

foreach (string file in FilesList)
{
    string filename = OutputFile;
    if (string.IsNullOrWhiteSpace(OutputFile))
    {
        filename = Path.ChangeExtension(file, ".seq");
    }
    string input = TextHelpers.NormalizeEol(File.ReadAllText(file), TextHelpers.EolType.CR);
    input = Charset.PETSCII(input, !ShouldUseUppercaseOnly);
    if (WrapSize > 0)
    {
        input = TextHelpers.WordWrap(input, WrapSize);
    }

    File.AppendAllText(filename, input);
}
#endregion

#region Procedures
void ShowHelp()
{
    Console.WriteLine("Usage: A2Petscii [OPTIONS] FILES");
    Console.WriteLine("Converts an ASCII text file to a .SEQ PETSCII file");
    Console.WriteLine();
    Console.WriteLine("Options:");
    p.WriteOptionDescriptions(Console.Out);
}

void ShowBanner() => Console.WriteLine("Casasoft A2Petscii v1.0\ncopyright (c) 2025 Roberto Ceccarelli - Casasoft\n");
#endregion