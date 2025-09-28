//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Casasoft">
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
using System.Text;

#region main
bool ShouldShowHelp = false;
bool ShouldSuppressBanner = false;
string OutputFile = string.Empty;

OptionSet p = new OptionSet()
{
    { "q|quiet", "Suppress banner print", v => ShouldSuppressBanner = v != null },
    { "h|?|help", "Show this help", v => ShouldShowHelp = v != null },
    { "o|out=", "Output file name", o => OutputFile = o },
};

List<string> FilesList = FileHelpers.ExpandWildcards(p.Parse(args));

if (!ShouldSuppressBanner)
    ShowBanner();

if (ShouldShowHelp || FilesList.Count == 0)
{
    ShowHelp();
    return;
}

foreach(string file in FilesList)
{
    string filename = OutputFile;
    if(string.IsNullOrWhiteSpace(OutputFile))
    {
        filename = Path.ChangeExtension(file, ".seq");
    }
    string input = File.ReadAllText(file);
    StringBuilder sb = new();
    foreach (char c in input)
    {
        sb.Append(Charset.PETSCII(c, true));
    }
    File.AppendAllText(filename, sb.ToString());
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

void ShowBanner() => Console.WriteLine("Casasoft A2Petscii v1.0\n(c) 2025 Roberto Ceccarelli - Casasoft\n");
#endregion