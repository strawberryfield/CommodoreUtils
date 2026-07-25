//-----------------------------------------------------------------------
// <copyright file="PrgFile.cs" company="Casasoft">
//     Author: Roberto Ceccarelli (http://strawberryfield.altervista.org)
//     Copyright (c) 2025,2026 All rights reserved.
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
/// Represents a Commodore PRG file: a two-byte little-endian load address followed by raw data bytes.
/// Provides helpers to read from and write to disk and to produce BASIC DATA lines from the contained data.
/// </summary>
/// <remarks>
/// A PRG file always begins with two bytes containing the load address (low byte first,
/// high byte second). The remaining bytes are the payload that should be loaded at the
/// specified address in the Commodore memory space.
///
/// This class is a simple in-memory representation and intentionally exposes the raw
/// byte array stored in <see cref="Data"/>. Callers that require immutability should
/// clone the array when constructing or after retrieving the property.
/// </remarks>
public class PrgFile : IPrgFile
{
    #region Properties
    /// <summary>
    /// Gets the 16-bit load address stored at the beginning of the PRG file.
    /// </summary>
    /// <value>
    /// The little-endian 16-bit address where the data contained in <see cref="Data"/>
    /// is intended to be loaded in the Commodore memory space.
    /// </value>
    public ushort LoadAddress { get; protected set; }

    /// <summary>
    /// Gets the raw data bytes contained in the PRG file, excluding the two-byte load address.
    /// </summary>
    /// <value>
    /// An array of bytes representing the payload of the PRG file. The array does not include
    /// the two-byte load address. The property may reference an empty array but is never null.
    /// </value>
    public byte[] Data { get; protected set; }
    #endregion

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="PrgFile"/> class with the specified load address and data bytes.
    /// </summary>
    /// <param name="loadAddress">The 16-bit load address that precedes the data in the PRG format.</param>
    /// <param name="data">The data bytes that follow the load address. The provided array is stored directly; it is not copied.</param>
    /// <remarks>
    /// Because the <paramref name="data"/> array is not cloned, callers must not modify the array
    /// if they expect the <see cref="PrgFile"/> instance to remain immutable.
    /// </remarks>
    public PrgFile(ushort loadAddress, byte[] data)
    {
        LoadAddress = loadAddress;
        Data = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrgFile"/> class by loading a PRG file from disk.
    /// </summary>
    /// <param name="filePath">The path to the PRG file to read.</param>
    /// <exception cref="ArgumentException">Thrown when the specified file is smaller than two bytes and therefore not a valid PRG file.</exception>
    /// <exception cref="System.IO.IOException">Propagates any I/O errors that occur while reading the file.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Propagates when access to the file is denied.</exception>
    /// <remarks>
    /// The file is expected to contain at least two bytes: the low and high parts of the load address,
    /// followed by zero or more data bytes. The constructor reads the entire file into memory.
    /// </remarks>
    public PrgFile(string filePath)
    {
        byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
        if (fileBytes.Length < 2)
        {
            throw new ArgumentException("Invalid PRG file: too short.");
        }
        LoadAddress = Conversions.ToUShort(fileBytes[0], fileBytes[1]);
        Data = new byte[fileBytes.Length - 2];
        Array.Copy(fileBytes, 2, Data, 0, Data.Length);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrgFile"/> class with default values.
    /// </summary>
    /// <remarks>
    /// The default instance has <see cref="LoadAddress"/> set to 0 and <see cref="Data"/>
    /// set to an empty array. Use this constructor when you intend to populate the instance
    /// programmatically.
    /// </remarks>
    public PrgFile()
    {
        LoadAddress = 0;
        Data = Array.Empty<byte>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrgFile"/> class with the specified load address and a list of byte array parts.
    /// </summary>
    /// <param name="loadAddress">The 16-bit load address that precedes the data in the PRG format.</param>
    /// <param name="parts">A list of byte arrays representing segments of the data to be concatenated. The arrays are copied into a single contiguous data array.</param>
    /// <remarks>
    /// This constructor combines all provided byte array segments into a single data array, which is stored directly.
    /// Callers must not modify the original arrays if they expect the <see cref="PrgFile"/> instance to remain immutable.
    /// </remarks>
    public PrgFile(ushort loadAddress, List<byte[]> parts)
    {
        LoadAddress = loadAddress;
        int offset = 0;
        int totalLength = parts.Aggregate(0, (accumulator, part) => accumulator += part.Length);
        Data = new byte[totalLength];
        foreach (var part in parts)
        {
            Array.Copy(part, 0, Data, offset, part.Length);
            offset += part.Length;
        }
    }
    #endregion

    /// <summary>
    /// Saves the PRG file to disk, writing the two-byte little-endian load address followed by the data bytes.
    /// </summary>
    /// <param name="filePath">The destination file path where the PRG will be written.</param>
    /// <exception cref="System.IO.IOException">Propagates any I/O errors that occur while writing the file.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Propagates when access to the destination is denied.</exception>
    public void Save(string filePath)
    {
        byte[] fileBytes = new byte[Data.Length + 2];
        (fileBytes[0], fileBytes[1]) = Conversions.ToBytes(LoadAddress);
        Array.Copy(Data, 0, fileBytes, 2, Data.Length);
        File.WriteAllBytes(filePath, fileBytes);
    }

    #region Prg2Data
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

    #endregion
}
