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

namespace Casasoft.Commodore
{
    /// <summary>
    /// Represents a Commodore PRG file: a two-byte load address followed by raw data bytes.
    /// Provides helpers to read from and write to disk and to produce BASIC DATA lines from the contained data.
    /// </summary>
    public class PrgFile
    {
        #region Properties
        /// <summary>
        /// Gets the 16-bit load address stored at the beginning of the PRG file.
        /// This is the address where the data is intended to be loaded in the Commodore memory space.
        /// </summary>
        public ushort LoadAddress { get; protected set; }

        /// <summary>
        /// Gets the raw data bytes contained in the PRG file, excluding the two-byte load address.
        /// </summary>
        public byte[] Data { get; protected set; }
        #endregion

        #region Constructors
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
            fileBytes[0] = (byte)(LoadAddress & 0xFF);
            fileBytes[1] = (byte)((LoadAddress >> 8) & 0xFF);
            Array.Copy(Data, 0, fileBytes, 2, Data.Length);
            File.WriteAllBytes(filePath, fileBytes);
        }

    }
}
