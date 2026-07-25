//-----------------------------------------------------------------------
// <copyright file="Strings2Prg.cs" company="Casasoft">
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
    /// Constructs a PRG payload containing a pointer table followed by null-terminated ASCII-encoded strings.
    /// The produced in-memory layout is:
    /// <list type="bullet">
    /// <item><description>[0..1] : number of strings (ushort, little-endian)</description></item>
    /// <item><description>[2..]  : sequence of 16-bit little-endian offsets (one per string) — absolute offsets relative to <paramref name="loadAddress"/></description></item>
    /// <item><description>[... ] : concatenated ASCII-encoded strings, each terminated with <c>0x00</c></description></item>
    /// </list>
    /// </summary>
    /// <param name="loadAddress">
    /// The 16-bit memory load address at which the resulting PRG data is intended to be loaded.
    /// Pointers written into the pointer table are absolute addresses computed as (<paramref name="loadAddress"/> + index).
    /// </param>
    /// <param name="strings">
    /// The list of .NET strings to encode. Each element is first converted to PETSCII using <see cref="Charset.PETSCII(string, bool)"/>,
    /// then ASCII-encoded and terminated with a single null byte (<c>0x00</c>).
    /// </param>
    /// <param name="LowerCase">
    /// If <see langword="true"/>, the PETSCII conversion will prefer lower-case characters; otherwise upper-case PETSCII is used.
    /// Defaults to <see langword="false"/>.
    /// </param>
    /// <param name="AddIndex">
    /// If <see langword="true"/>, the pointer table is copied into the beginning of the instance <see cref="PrgFile.Data"/> buffer
    /// and the string bytes follow immediately after it. If <see langword="false"/>, only the concatenated string bytes are copied
    /// into <see cref="PrgFile.Data"/> starting at index <c>0</c>; the pointer-table region in the allocated buffer will remain zeroed.
    /// Note: the constructor always allocates the Data buffer with space for the pointer table (pointer table length + total string bytes)
    /// regardless of the value of <paramref name="AddIndex"/>.
    /// </param>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown if <paramref name="strings"/> is <see langword="null"/>. The implementation does not perform an explicit guard here,
    /// so calling with a null reference will result in an exception from the runtime.
    /// </exception>
    /// <exception cref="System.OverflowException">
    /// May be thrown if computed absolute offsets (pointer values) exceed <see cref="ushort.MaxValue"/> when combined with
    /// <paramref name="loadAddress"/> (i.e., resulting address &gt; <see cref="ushort.MaxValue"/>).
    /// </exception>
    /// <remarks>
    /// - The pointer values stored in the pointer table are 16-bit little-endian addresses that point to the start of each string
    ///   when the PRG is loaded at <paramref name="loadAddress"/>.
    /// - No further validation is performed on string contents; callers should ensure strings are suitable for ASCII encoding
    ///   after PETSCII conversion.
    /// - The constructor sets the instance <see cref="PrgFile.LoadAddress"/> and <see cref="PrgFile.Data"/> properties.
    /// </remarks>
    public Strings2Prg(ushort loadAddress, List<string> strings, bool LowerCase = false, bool AddIndex = true)
    {
        (byte[] pointers, byte[] data) = CreateArrays(strings, loadAddress, LowerCase);
 
        this.LoadAddress = loadAddress;
        this.Data = new byte[(AddIndex ? pointers.Length : 0) + data.Length];
        if (AddIndex)
        {
            pointers.CopyTo(this.Data, 0);
            data.CopyTo(this.Data, pointers.Length);
        }
        else
        {
            data.CopyTo(this.Data, 0);
        }
    }
    #endregion

    /// <summary>
    /// Creates the raw pointer table bytes and the concatenated, null-terminated string bytes for a PRG.
    /// </summary>
    /// <param name="strings">A list of managed strings to be converted to PETSCII, ASCII-encoded and terminated with <c>0x00</c>.</param>
    /// <param name="loadAddress">
    /// The 16-bit load address at which the resulting PRG data is intended to be loaded.
    /// Pointer entries are written as absolute 16-bit addresses computed relative to this value.
    /// </param>
    /// <param name="LowerCase">If <see langword="true"/>, PETSCII conversion prefers lower-case; otherwise upper-case is used.</param>
    /// <returns>
    /// A tuple where:
    /// <list type="bullet">
    /// <item><description><c>Pointers</c> — a byte array containing a 16-bit count followed by 16-bit little-endian absolute addresses (one per string).</description></item>
    /// <item><description><c>StringsList</c> — a byte array containing the concatenated ASCII-encoded strings, each terminated with <c>0x00</c>, and an extra final null terminator.</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="System.ArgumentNullException">If <paramref name="strings"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.OverflowException">
    /// If computed absolute pointer values exceed <see cref="ushort.MaxValue"/> when combined with <paramref name="loadAddress"/>.
    /// </exception>
    /// <remarks>
    /// - The returned <c>Pointers</c> buffer layout:
    ///   [0..1] = number of strings (ushort, little-endian), followed by N 16-bit little-endian absolute addresses.
    /// - The returned <c>StringsList</c> buffer contains the ASCII bytes of each PETSCII-converted string followed by <c>0x00</c>.
    /// - The implementation appends an extra null byte after the last string; callers should account for it when computing sizes.
    /// </remarks>
    public static (byte[] Pointers, byte[] StringsList) CreateArrays(List<string> strings, ushort loadAddress, bool LowerCase = false)
    {
        byte[] pointers = new byte[strings.Count * 2 + 2];
        Conversions.InsertUShort(pointers, 0, (ushort)strings.Count);

        ushort currentOffset = (ushort)(pointers.Length + loadAddress);
        List<byte> data = new List<byte>();
        for (ushort j = 0; j < strings.Count; j++)
        {
            Conversions.InsertUShort(pointers, j * 2 + 2, currentOffset);
            byte[] strBytes = Charset.PETSCIIBytes(strings[j], LowerCase);
            data.AddRange(strBytes);
            data.Add(0); // null terminator

            currentOffset += (ushort)(strBytes.Length + 1);
        }
        data.Add(0); // final null terminator

        return (pointers, data.ToArray());
    }

    /// <summary>
    /// Adjusts the pointer table of an in-memory PRG payload so pointer entries reference a different load address.
    /// </summary>
    /// <param name="prgData">
    /// The PRG payload buffer that contains the pointer table. Expected layout:
    /// [0..1] = number of strings (ushort, little-endian),
    /// [2..]  = sequence of 16-bit little-endian absolute addresses (one per string).
    /// The buffer is modified in-place; pointers are rewritten as little-endian 16-bit values.
    /// </param>
    /// <param name="newLoadAddress">Target 16-bit load address to which pointer values will be adjusted.</param>
    /// <param name="oldLoadAddress">Original 16-bit load address that current pointer values reference. Defaults to 0.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="prgData"/> is too short to contain a valid pointer table or does not contain the expected number of pointers.
    /// </exception>
    /// <remarks>
    /// The method reads the string count from the first two bytes then iterates each pointer entry
    /// at offsets 2 + (index * 2). Each pointer value is transformed using the formula:
    /// adjusted = oldPointer - oldLoadAddress + newLoadAddress
    /// and the adjusted 16-bit little-endian value is written back to the same location.
    ///
    /// The function does not validate that adjusted pointers point within the provided buffer and performs
    /// no bounds checks on pointer targets. It operates only on the pointer table itself and leaves string
    /// contents untouched.
    /// </remarks>
    public static void RelocatePointers(byte[] prgData, ushort newLoadAddress, ushort oldLoadAddress = 0)
    {
        if (prgData.Length < 4)
        {
            throw new ArgumentException("PRG data is too short to contain a valid pointer table.");
        }
        ushort stringCount = Conversions.ReadUShort(prgData, 0);
        int expectedLength = 2 + stringCount * 2;
        if (prgData.Length < expectedLength)
        {
            throw new ArgumentException("PRG data is too short to contain the expected number of pointers.");
        }
        for (int i = 0; i < stringCount; i++)
        {
            int pointerOffset = 2 + i * 2;
            ushort oldPointer = Conversions.ReadUShort(prgData, pointerOffset);
            ushort adjustedPointer = (ushort)(oldPointer - oldLoadAddress + newLoadAddress);
            Conversions.InsertUShort(prgData, pointerOffset, adjustedPointer);
        }
    }
}
