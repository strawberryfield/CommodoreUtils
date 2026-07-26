//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Casasoft">
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

using Avalonia;

namespace Casasoft.ImageConverterGUI;

/// <summary>
/// Application entry point. Bootstraps the Avalonia framework and starts the
/// classic desktop lifetime (Windows/Linux/macOS window shell).
/// </summary>
internal static class Program
{
    /// <summary>
    /// Managed entry point. Kept minimal and free of Avalonia types so that the
    /// AOT/trimming-friendly <see cref="BuildAvaloniaApp"/> factory can also be
    /// reused by design tools / previewers.
    /// </summary>
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Configures the Avalonia application: platform auto-detection (Win32, X11,
    /// Avalonia native for macOS), default font, and trace logging.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
