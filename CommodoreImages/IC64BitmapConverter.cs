//-----------------------------------------------------------------------
// <copyright file="IC64BitmapConverter.cs" company="Casasoft">
//     Author: Roberto Ceccarelli (http://strawberryfield.altervista.org)
//     Copyright (c) 2026 All rights reserved.
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

using ImageMagick;

namespace Casasoft.Commodore.Images;

/// <summary>
/// Provides a common interface for Commodore 64 bitmap converters.
/// </summary>
/// <typeparam name="TData">The type of C64 graphics data produced by the converter (must implement <see cref="IC64BitmapData"/>).</typeparam>
public interface IC64BitmapConverter<out TData> where TData : IC64BitmapData
{
    /// <summary>
    /// Converts an RGB image to Commodore 64 graphics format.
    /// </summary>
    /// <param name="input">The source image as a <see cref="MagickImage"/>.</param>
    /// <param name="useDithering">Whether to apply Floyd-Steinberg error-diffusion dithering.</param>
    /// <param name="brightnessBias">Bias applied when selecting colors to favor brighter palette entries.</param>
    /// <param name="brightnessMode">Determines which conversion stage(s) the brightness bias affects.</param>
    /// <returns>An instance of <typeparamref name="TData"/> containing the converted graphics data.</returns>
    TData ConvertImage(
        MagickImage input,
        bool useDithering = true,
        double brightnessBias = 0.35,
        BrightnessBiasMode brightnessMode = BrightnessBiasMode.Both);
}