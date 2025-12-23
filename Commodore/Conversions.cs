//-----------------------------------------------------------------------
// <copyright file="Conversions.cs" company="Casasoft">
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

namespace Casasoft.Commodore;


/// <summary>
/// Provides conversion helpers between 16-bit unsigned integers and their byte components.
/// </summary>
public static class Conversions
{
    /// <summary>
    /// Provides conversion helpers between 16-bit unsigned integers and their byte components.
    /// </summary>
    /// <remarks>
    /// All conversions in this class use little-endian ordering:
    /// the <c>low</c> byte represents the least-significant 8 bits and
    /// the <c>hi</c> byte represents the most-significant 8 bits.
    /// Methods are pure and have no side effects.
    /// </remarks>
    public static ushort ToUShort(byte low, byte hi) => (ushort)(low | hi << 8);

    /// <summary>
    /// Splits a 16-bit unsigned integer into its low and high bytes (little-endian).
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer to split.</param>
    /// <returns>
    /// A tuple where <c>low</c> is the least-significant byte (bits 0-7)
    /// and <c>hi</c> is the most-significant byte (bits 8-15).
    /// </returns>
    /// <example>
    /// Example:
    /// <code>
    /// var (low, hi) = Conversions.ToBytes(0x1234); // low == 0x34, hi == 0x12
    /// </code>
    /// </example>
    public static (byte low, byte hi) ToBytes(ushort value) => ((byte)(value & 0xFF), (byte)((value >> 8) & 0xFF));

    /// <summary>
    /// Inserts a 16-bit unsigned integer into a byte array at the specified index in little-endian order.
    /// </summary>
    /// <param name="array">The byte array to insert the value into.</param>
    /// <param name="index">The starting index in the array where the value will be inserted.</param>
    /// <param name="value">The 16-bit unsigned integer value to insert.</param>
    /// <remarks>
    /// The <paramref name="value"/> is split into low and high bytes and stored at <paramref name="array"/>[<paramref name="index"/>]
    /// and <paramref name="array"/>[<paramref name="index"/> + 1], respectively.
    /// </remarks>
    public static void InsertUShort(byte[] array, int index, ushort value) => (array[index], array[index + 1]) = ToBytes(value);
}
