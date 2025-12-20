//-----------------------------------------------------------------------
// <copyright file="PrgFile.cs" company="Casasoft">
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

namespace Casasoft.Commodore
{
    /// <summary>
    /// Represents a Commodore PRG file: a two-byte load address followed by raw data bytes.
    /// Provides helpers to read from and write to disk and to produce BASIC DATA lines from the contained data.
    /// </summary>
    public class PrgFile
    {
        /// <summary>
        /// Gets the 16-bit load address stored at the beginning of the PRG file.
        /// This is the address where the data is intended to be loaded in the Commodore memory space.
        /// </summary>
        public ushort LoadAddress { get; private set; }

        /// <summary>
        /// Gets the raw data bytes contained in the PRG file, excluding the two-byte load address.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrgFile"/> class with the specified load address and data bytes.
        /// </summary>
        /// <param name="loadAddress">The 16-bit load address that precedes the data in the PRG format.</param>
        /// <param name="data">The data bytes that follow the load address.</param>
        public PrgFile(ushort loadAddress, byte[] data)
        {
            LoadAddress = loadAddress;
            Data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrgFile"/> class by loading a PRG file from disk.
        /// The file is expected to contain at least two bytes: the low and high parts of the load address,
        /// followed by zero or more data bytes.
        /// </summary>
        /// <param name="filePath">The path to the PRG file to read.</param>
        /// <exception cref="ArgumentException">Thrown when the specified file is smaller than two bytes and therefore not a valid PRG file.</exception>
        /// <exception cref="System.IO.IOException">Propagates any I/O errors that occur while reading the file.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Propagates when access to the file is denied.</exception>
        public PrgFile(string filePath)
        {
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            if (fileBytes.Length < 2)
            {
                throw new ArgumentException("Invalid PRG file: too short.");
            }
            LoadAddress = (ushort)(fileBytes[0] | (fileBytes[1] << 8));
            Data = new byte[fileBytes.Length - 2];
            Array.Copy(fileBytes, 2, Data, 0, Data.Length);
        }

        /// <summary>
        /// Saves the PRG file to disk, writing the two-byte little-endian load address followed by the data bytes.
        /// </summary>
        /// <param name="filePath">The destination file path where the PRG will be written.</param>
        /// <exception cref="System.IO.IOException">Propagates any I/O errors that occur while writing the file.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Propagates when access to the destination is denied.</exception>
        public void Save(string filePath)
        {
            byte[] fileBytes = new byte[Data.Length + 2];
            fileBytes[0] = (byte)(LoadAddress & 0xFF);
            fileBytes[1] = (byte)((LoadAddress >> 8) & 0xFF);
            Array.Copy(Data, 0, fileBytes, 2, Data.Length);
            File.WriteAllBytes(filePath, fileBytes);
        }

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
        /// <returns>
        /// A single string containing zero or more lines (terminated with the current environment line terminator)
        /// where each line encodes up to <paramref name="bytesPerLine"/> bytes from <see cref="Data"/> in decimal format.
        /// </returns>
        /// <remarks>
        /// The load address used at the start of each line is computed as <see cref="LoadAddress"/> + offset of the first byte on that line.
        /// </remarks>
        public string CreateDataLines(int bytesPerLine, string targetFilename)
        {
            if (bytesPerLine < 1 ) bytesPerLine = 8;
            if (bytesPerLine > 12) bytesPerLine = 12;

            StringBuilder sb = new StringBuilder();
            int totalBytes = Data.Length;
            sb.AppendLine(string.Format(DataReader, LoadAddress, LoadAddress + (ushort)(totalBytes - 1), targetFilename));

            for (int i = 0; i < totalBytes; i += bytesPerLine)
            {
                int lineLength = Math.Min(bytesPerLine, totalBytes - i);
                sb.AppendFormat("{0} DATA ", LoadAddress + (ushort)i);
                for (int j = 0; j < lineLength; j++)
                {
                    sb.AppendFormat("{0:D3}", Data[i + j]);
                    if(j < lineLength - 1)
                    {
                        sb.Append(", ");
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
