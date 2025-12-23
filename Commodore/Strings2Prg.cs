//-----------------------------------------------------------------------
// <copyright file="Strings2Prg.cs" company="Casasoft">
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
/// Represents a Commodore PRG file that stores a collection of null-terminated strings
/// with a preceding pointer table. The on-disk data layout produced by this class is:
/// [0..1] : number of strings (ushort, little-endian)
/// [2..]  : sequence of 16-bit little-endian offsets (one per string)
/// [..]   : concatenated ASCII-encoded strings, each terminated with 0x00
/// </summary>
/// <remarks>
/// Instances of this class may be created from an existing PRG on disk or constructed
/// from a list of strings which will be converted to PETSCII and encoded for Commodore usage.
/// The resulting <see cref="PrgFile.LoadAddress"/> and <see cref="PrgFile.Data"/> properties
/// reflect the constructed PRG data.
/// </remarks>
public class Strings2Prg : PrgFile
{
    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="Strings2Prg"/> class with default property values.
    /// </summary>
    /// <remarks>
    /// This parameterless constructor does not load or construct any PRG data. Use one of the other
    /// constructors to initialize the instance from a file, raw data, or a collection of strings.
    /// </remarks>
    public Strings2Prg()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Strings2Prg"/> class by loading data from the specified file.
    /// </summary>
    /// <param name="filePath">Path to the PRG file to load. The base class handles file I/O and parsing.</param>
    /// <exception cref="System.ArgumentException">May be thrown by the base constructor for invalid file paths.</exception>
    /// <exception cref="System.IO.IOException">May be thrown by the base constructor for I/O errors.</exception>
    public Strings2Prg(string filePath) : base(filePath)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Strings2Prg"/> class from raw PRG data and a load address.
    /// </summary>
    /// <param name="loadAddress">The 16-bit load address at which the <paramref name="data"/> is intended to be loaded.</param>
    /// <param name="data">Raw PRG payload (excluding the two-byte load address prefix used on disk by some formats).</param>
    public Strings2Prg(ushort loadAddress, byte[] data) : base(loadAddress, data)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Strings2Prg"/> class from a collection of strings.
    /// The constructor builds a pointer table followed by the encoded string data suitable for a Commodore PRG:
    /// the first two bytes contain the number of strings (little-endian ushort), followed by one 16-bit little-endian
    /// offset per string pointing to each string's data relative to <paramref name="loadAddress"/>. Each string is
    /// encoded as ASCII bytes and terminated with a single null byte (0x00).
    /// </summary>
    /// <param name="loadAddress">
    /// The 16-bit load address at which the PRG data will be placed in memory. Pointers written into the resulting
    /// data buffer are relative to this address.
    /// </param>
    /// <param name="strings">
    /// The list of strings to encode into the PRG data section. Each entry is converted to PETSCII via
    /// <see cref="Charset.PETSCII(string, bool)"/> before ASCII encoding and null-termination.
    /// </param>
    /// <param name="LowerCase">
    /// If <see langword="true"/>, the <see cref="Charset.PETSCII(string, bool)"/> conversion will prefer lower-case PETSCII.
    /// Defaults to <see langword="false"/>.
    /// </param>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown if <paramref name="strings"/> is <see langword="null"/>. Note: the constructor does not perform explicit
    /// null checks; attempting to call it with a null reference will result in an exception.
    /// </exception>
    /// <exception cref="System.OverflowException">
    /// May be thrown if the computed offsets exceed the range of a 16-bit unsigned integer when combined with
    /// <paramref name="loadAddress"/> (i.e., resulting address &gt; <see cref="ushort.MaxValue"/>).
    /// </exception>
    /// <remarks>
    /// - The pointer table layout:
    ///   [0..1]   : number of strings (ushort, little-endian)
    ///   [2..]    : sequence of ushort offsets (little-endian), one per string
    ///   [pointers.Length..] : concatenated ASCII-encoded strings each terminated by 0x00
    /// - The constructor sets the instance <see cref="PrgFile.LoadAddress"/> and <see cref="PrgFile.Data"/> properties.
    /// - No further validation is performed on string contents; callers should ensure strings are suitable for ASCII encoding.
    /// </remarks>
    public Strings2Prg(ushort loadAddress, List<string> strings, bool LowerCase = false)
    {
        byte[] pointers = new byte[strings.Count * 2 + 2];
        Conversions.InsertUShort(pointers, 0, (ushort)strings.Count);

        ushort currentOffset = (ushort)(pointers.Length + loadAddress);
        List<byte> data = new List<byte>();
        for (ushort j = 0; j < strings.Count; j++)
        {
            string str = Charset.PETSCII(strings[j], LowerCase);
            Conversions.InsertUShort(pointers, j * 2 + 2, currentOffset);
            byte[] strBytes = Encoding.ASCII.GetBytes(str);
            data.AddRange(strBytes);
            data.Add(0); // null terminator
            currentOffset += (ushort)(strBytes.Length + 1);
        }
        this.LoadAddress = loadAddress;
        this.Data = new byte[pointers.Length + data.Count];
        pointers.CopyTo(this.Data, 0);
        data.ToArray().CopyTo(this.Data, pointers.Length);
    }
    #endregion
}
