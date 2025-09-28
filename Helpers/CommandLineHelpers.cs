//-----------------------------------------------------------------------
// <copyright file="CommandLineHelpers.cs" company="Casasoft">
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

namespace Casasoft.Helpers;

public static class CommandLineHelpers
{
    /// <summary>
    /// Get integer value from string
    /// </summary>
    /// <param name="val">input string</param>
    /// <param name="fallback">default value in case of parsing error</param>
    /// <param name="message">error message</param>
    /// <returns></returns>
    public static int GetIntParameter(string val, int fallback, string message)
    {
        int ret;
        if (!int.TryParse(val, out ret))
        {
            Console.Error.WriteLine(string.Format(message, val));
            ret = fallback;
        }
        return ret;
    }
}
