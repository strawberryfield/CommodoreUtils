//-----------------------------------------------------------------------
// <copyright file="Prg2Data.cs" company="Casasoft">
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

using System.Text;

namespace Casasoft.Commodore;

/// <summary>
/// Represents a PRG file whose contents are emitted as BASIC DATA lines.
/// </summary>
/// <remarks>
/// <see cref="Prg2Data"/> extends <see cref="PrgFile"/> and provides helpers
/// to convert the binary PRG payload into textual BASIC DATA statements
/// suitable for embedding in a BASIC program loader.
/// </remarks>
public class Prg2Data : PrgFile
{
    #region constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Prg2Data"/> class by loading
    /// PRG data from the specified file path.
    /// </summary>
    /// <param name="filePath">
    /// The path to the PRG file to load. The constructor delegates loading and
    /// validation responsibilities to the base <see cref="PrgFile"/> class.
    /// </param>
    public Prg2Data(string filePath) : base(filePath)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Prg2Data"/> class with an explicit
    /// load address and an in-memory data buffer.
    /// </summary>
    /// <param name="loadAddress">The 16-bit load address where the first data byte belongs.</param>
    /// <param name="data">A byte array containing the PRG payload (not including the two-byte load address prefix).</param>
    public Prg2Data(ushort loadAddress, byte[] data) : base(loadAddress, data)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Prg2Data"/> class.
    /// </summary>
    public Prg2Data() : base()
    {
    }
    #endregion

    private readonly string DataReader = @"100 S={0}
110 E={1}
120 FOR X=S TO E :READ A :POKE X,A :NEXT
130 SYS 57812 (""{2}""),8,1
140 POKE 194,INT(S/256): POKE 193,S-PEEK(194)*256
150 E=E+1
160 POKE 175,INT(E/256): POKE 174,E-PEEK(175)*256
170 SYS 62957
180 REM";

    /// <summary>
    /// Creates a sequence of BASIC DATA lines representing the PRG file data.
    /// Each line begins with the load address for the first byte on the line followed by the ASCII " DATA "
    /// token and decimal three-digit values separated by commas (for example: "2049 DATA 032, 255").
    /// </summary>
    /// <param name="bytesPerLine">The maximum number of data bytes to include on each generated DATA line. Should be greater than zero.</param>
    /// <param name="targetFilename">The target filename to be referenced in the generated BASIC loader code.</param>
    /// <returns>
    /// A single string containing zero or more lines (terminated with the current environment line terminator)
    /// where each line encodes up to <paramref name="bytesPerLine"/> bytes from <see cref="PrgFile.Data"/> in decimal format.
    /// </returns>
    /// <remarks>
    /// The load address used at the start of each line is computed as <see cref="PrgFile.LoadAddress"/> + offset of the first byte on that line.
    /// </remarks>
    public string CreateDataLines(int bytesPerLine, string targetFilename)
    {
        if (bytesPerLine < 1) bytesPerLine = 8;
        if (bytesPerLine > 12) bytesPerLine = 12;

        StringBuilder sb = new();
        int totalBytes = Data.Length;
        sb.AppendLine(string.Format(DataReader, LoadAddress, LoadAddress + (ushort)(totalBytes - 1), targetFilename));

        for (int i = 0; i < totalBytes; i += bytesPerLine)
        {
            int lineLength = Math.Min(bytesPerLine, totalBytes - i);
            sb.AppendFormat("{0} DATA ", LoadAddress + (ushort)i);
            for (int j = 0; j < lineLength; j++)
            {
                sb.AppendFormat("{0:D3}", Data[i + j]);
                if (j < lineLength - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

}
