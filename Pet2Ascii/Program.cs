//-----------------------------------------------------------------------
// <copyright file="Pet2Ascii/Program.cs" company="Casasoft">
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
string OutputFile = string.Empty;

OptionSet p = new()
{
    { "q|quiet", "Suppress banner print", v => ShouldSuppressBanner = v != null },
    { "h|?|help", "Show this help", v => ShouldShowHelp = v != null },
    { "u|uppercase", "Use uppercase only charset", v => ShouldUseUppercaseOnly = v != null },
    { "o|out=", "Output file name (default same as input with .txt extension)", o => OutputFile = o },
};

List<string> FilesList = FileHelpers.ExpandWildcards(p.Parse(args));

if (!ShouldSuppressBanner)
    ShowBanner();

if (ShouldShowHelp || FilesList.Count == 0)
{
    ShowHelp();
    return;
}

foreach (string file in FilesList)
{
    string filename = OutputFile;
    if (string.IsNullOrWhiteSpace(OutputFile))
    {
        filename = Path.ChangeExtension(file, ".txt");
    }
    string input = Charset.ASCII(File.ReadAllBytes(file), !ShouldUseUppercaseOnly);

    File.AppendAllText(filename, input);
}
#endregion

#region Procedures
void ShowHelp()
{
    Console.WriteLine("Usage: Pet2Ascii [OPTIONS] FILES");
    Console.WriteLine("Converts a PETSCII .SEQ file to an ASCII text file");
    Console.WriteLine();
    Console.WriteLine("Options:");
    p.WriteOptionDescriptions(Console.Out);
}

void ShowBanner() => Console.WriteLine("Casasoft Pet2Ascii v1.0\ncopyright (c) 2025 Roberto Ceccarelli - Casasoft\n");
#endregion