//-----------------------------------------------------------------------
// <copyright file="BrightnessBiasMode.cs" company="Casasoft">
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

namespace Casasoft.Commodore.Images;

/// <summary>
/// Controls at which stage(s) of the conversion pipeline the brightness bias
/// (see <see cref="MulticolorConverter.ConvertImage"/> and <see cref="HiresConverter.ConvertImage"/>)
/// is applied.
/// </summary>
/// <remarks>
/// The brightness bias can influence two distinct decisions:
/// <list type="bullet">
/// <item><description><b>Quantization</b>: which C64 palette color each source pixel is mapped to
/// (during dithering or plain nearest-color quantization). Biasing this stage nudges individual
/// pixels towards brighter palette colors whenever two candidate colors are near-equidistant in
/// RGB space, regardless of how "noisy"/dithered the image is.</description></item>
/// <item><description><b>Selection</b>: which colors are chosen as the per-cell (and, for multicolor,
/// screen-wide background) colors, based on already-quantized pixel color frequency counts.
/// Biasing this stage only changes the outcome when candidate colors have comparable counts;
/// it has no effect when one color is overwhelmingly dominant (e.g. large flat/undithered areas).</description></item>
/// </list>
/// </remarks>
public enum BrightnessBiasMode
{
    /// <summary>No brightness bias is applied anywhere; behaves as if brightnessBias were 0.</summary>
    None,

    /// <summary>Brightness bias is applied only when quantizing pixels to the C64 palette.</summary>
    Quantization,

    /// <summary>Brightness bias is applied only when selecting background/foreground colors from frequency counts.</summary>
    Selection,

    /// <summary>Brightness bias is applied both at quantization time and at background/foreground selection time (default).</summary>
    Both
}