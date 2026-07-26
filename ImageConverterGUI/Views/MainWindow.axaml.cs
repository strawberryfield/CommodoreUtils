//-----------------------------------------------------------------------
// <copyright file="MainWindow.axaml.cs" company="Casasoft">
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

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Casasoft.Commodore;
using Casasoft.Commodore.Images;
using Casasoft.Helpers;
using ImageMagick;

namespace Casasoft.ImageConverterGUI.Views;

/// <summary>
/// Main (and only) window of the GUI front-end for the Commodore 64 image converter.
/// Lets the user pick any ImageMagick-readable image, choose hires/multicolor mode and
/// dithering/brightness options, preview the resulting C64 image, and save both the
/// preview picture and the actual .PRG files (bitmap, screen RAM, color RAM, background),
/// mirroring what the <c>ImageConverter</c> console tool produces.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// The most recently produced conversion result (bitmap/screen RAM, plus color RAM and
    /// background color when in multicolor mode). Populated by <see cref="btnConvert_Click"/>
    /// and consumed by <see cref="btnSaveC64_Click"/>.
    /// </summary>
    private IC64BitmapData? _convertedData;

    /// <summary>
    /// The rendered preview (320x200) of <see cref="_convertedData"/>, as it would look on
    /// a real C64. Owned by this window; disposed and replaced on every new conversion.
    /// </summary>
    private MagickImage? _previewImage;

    /// <summary>
    /// Initializes the window, wires up mode-dependent UI state (the background-color field
    /// only makes sense in multicolor mode) and loads the XAML-defined controls.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        rbMulticolor.IsCheckedChanged += (_, _) => UpdateBackgroundFieldState();
        rbHires.IsCheckedChanged += (_, _) => UpdateBackgroundFieldState();
        UpdateBackgroundFieldState();
    }

    /// <summary>
    /// Enables the background-address field only for multicolor mode: hires mode has no
    /// shared/global background register (see <see cref="C64HiresData"/> remarks).
    /// </summary>
    private void UpdateBackgroundFieldState() =>
        txtBackgroundAddress.IsEnabled = rbMulticolor.IsChecked == true;

    /// <summary>Keeps the numeric readout next to the brightness slider in sync.</summary>
    private void sldBrightness_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e) =>
        txtBrightnessValue.Text = e.NewValue.ToString("0.00");

    /// <summary>
    /// Loads the selected source image (any format ImageMagick can read, any size - it is
    /// resized internally by the converter, no need to pre-scale to 320x200), converts it to
    /// the selected C64 bitmap format, and displays the resulting preview.
    /// </summary>
    private async void btnConvert_Click(object? sender, RoutedEventArgs e)
    {
        txtStatus.Text = string.Empty;

        string inputPath = ftbInput.Value;
        if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
        {
            txtStatus.Text = "Seleziona un file immagine valido.";
            return;
        }

        bool useHires = rbHires.IsChecked == true;
        bool useDithering = chkDither.IsChecked == true;
        double brightnessBias = sldBrightness.Value;
        BrightnessBiasMode brightnessMode = cmbBrightnessMode.SelectedIndex switch
        {
            0 => BrightnessBiasMode.None,
            1 => BrightnessBiasMode.Quantization,
            2 => BrightnessBiasMode.Selection,
            _ => BrightnessBiasMode.Both,
        };

        try
        {
            using MagickImage source = new(inputPath);

            // Polymorphic converter selection, same pattern as ImageConverter/Program.cs.
            // Both HiresConverter and MulticolorConverter internally downscale the source
            // image to their working resolution (DownscaleImage), so any input size is fine.
            IC64BitmapConverter<IC64BitmapData> converter = useHires
                ? new HiresConverter()
                : new MulticolorConverter();

            _convertedData = converter.ConvertImage(source, useDithering, brightnessBias, brightnessMode);

            _previewImage?.Dispose();
            _previewImage = _convertedData.ToMagickImage();

            imgPreview.Source = ToAvaloniaBitmap(_previewImage);

            btnSavePreview.IsEnabled = true;
            btnSaveC64.IsEnabled = true;

            txtStatus.Text = _convertedData is C64MulticolorData mc
                ? $"Conversione completata. Colore di sfondo: {mc.BackgroundColor} (POKE 53281,{mc.BackgroundColor})"
                : "Conversione completata.";
        }
        catch (MagickException ex)
        {
            txtStatus.Text = $"Errore nel caricamento dell'immagine: {ex.Message}";
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"Errore durante la conversione: {ex.Message}";
        }
    }

    /// <summary>
    /// Converts a <see cref="MagickImage"/> into an Avalonia <see cref="Bitmap"/> via an
    /// in-memory PNG round-trip.
    /// </summary>
    /// <remarks>
    /// This intentionally does <b>not</b> call <c>ImageViewer.SetImage(MagickImage)</c> from
    /// Casasoft.Avalonia.Controls: that control is compiled against Magick.NET-Q16-AnyCPU,
    /// while this project (matching CommodoreImages.csproj) uses Magick.NET-Q8-AnyCPU. The two
    /// packages produce distinct, incompatible <c>MagickImage</c> types, so passing one project's
    /// instance into the other's API would not compile. Going through a plain Avalonia
    /// <see cref="Bitmap"/> (set via <c>ImageViewer.Source</c>) sidesteps the mismatch entirely.
    /// </remarks>
    private static Bitmap ToAvaloniaBitmap(MagickImage image)
    {
        using MemoryStream ms = new();
        image.Write(ms, MagickFormat.Png);
        ms.Position = 0;
        return new Bitmap(ms);
    }

    /// <summary>
    /// Saves the current preview image (as shown in the viewer) to a user-chosen file, in
    /// PNG, JPEG or BMP format depending on the chosen extension.
    /// </summary>
    /// <summary>
    /// Saves the current preview image (as shown in the viewer) to a user-chosen file, in
    /// PNG, JPEG or BMP format depending on the chosen file type / extension.
    /// </summary>
    private async void btnSavePreview_Click(object? sender, RoutedEventArgs e)
    {
        if (_previewImage is null) return;

        IStorageProvider? storageProvider = StorageProvider;
        if (storageProvider is null) return;

        string suggestedBaseName = Path.GetFileNameWithoutExtension(ftbInput.Value) is { Length: > 0 } n
            ? n + "_preview"
            : "preview";

        var pngType = new FilePickerFileType("PNG") { Patterns = new[] { "*.png" } };
        var jpgType = new FilePickerFileType("JPEG") { Patterns = new[] { "*.jpg", "*.jpeg" } };
        var bmpType = new FilePickerFileType("BMP") { Patterns = new[] { "*.bmp" } };

        IStorageFile? file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salva anteprima",
            SuggestedFileName = suggestedBaseName + ".png",
            DefaultExtension = "png",
            FileTypeChoices = new[] { pngType, jpgType, bmpType }
        });

        if (file is null) return;

        string? localPath = file.TryGetLocalPath();
        if (localPath is null)
        {
            txtStatus.Text = "Impossibile determinare il percorso locale del file.";
            return;
        }

        // Determina il formato dall'estensione effettiva del percorso scelto dall'utente,
        // aggiungendola se mancante (alcuni storage provider non la impostano automaticamente).
        string ext = Path.GetExtension(localPath).ToLowerInvariant();
        MagickFormat format;
        switch (ext)
        {
            case ".jpg":
            case ".jpeg":
                format = MagickFormat.Jpg;
                break;
            case ".bmp":
                format = MagickFormat.Bmp;
                break;
            case ".png":
                format = MagickFormat.Png;
                break;
            default:
                // Nessuna estensione (o non riconosciuta): forza .png e aggiungila al path.
                format = MagickFormat.Png;
                localPath += ".png";
                break;
        }

        _previewImage.Write(localPath, format);
        txtStatus.Text = $"Anteprima salvata in {localPath}";
    }

    /// <summary>
    /// Saves the converted Commodore 64 data as .PRG files, using the same naming
    /// convention as the <c>ImageConverter</c> console tool: <c>{base}.bm.prg</c> (bitmap),
    /// <c>{base}.sc.prg</c> (screen RAM) always, plus <c>{base}.co.prg</c> (color RAM) and
    /// <c>{base}.bg.prg</c> (background color) when converting in multicolor mode.
    /// </summary>
    private async void btnSaveC64_Click(object? sender, RoutedEventArgs e)
    {
        if (_convertedData is null) return;

        IStorageProvider? storageProvider = StorageProvider;
        if (storageProvider is null) return;

        string suggestedName = Path.GetFileNameWithoutExtension(ftbInput.Value) is { Length: > 0 } n
            ? n
            : "image";

        IStorageFile? file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salva file Commodore 64 (nome base, senza estensione)",
            SuggestedFileName = suggestedName + ".prg",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("File PRG base") { Patterns = new[] { "*.prg" } },
            }
        });

        if (file is null) return;

        string? localPath = file.TryGetLocalPath();
        if (localPath is null)
        {
            txtStatus.Text = "Impossibile determinare il percorso locale del file.";
            return;
        }

        string baseName = Path.Combine(
            Path.GetDirectoryName(localPath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(localPath));

        ushort pixelAddress = (ushort)CommandLineHelpers.GetIntParameter(
            txtPixelAddress.Text ?? "$E000", 0xE000,
            "Indirizzo bitmap non valido '{0}', uso il default $E000");
        ushort colorAddress = (ushort)CommandLineHelpers.GetIntParameter(
            txtColorAddress.Text ?? "$C000", 0xC000,
            "Indirizzo schermo non valido '{0}', uso il default $C000");
        ushort backgroundAddress = (ushort)CommandLineHelpers.GetIntParameter(
            txtBackgroundAddress.Text ?? "$D021", 0xD021,
            "Indirizzo sfondo non valido '{0}', uso il default $D021");

        IPrgFile prg = new PrgFile(pixelAddress, _convertedData.BitmapData);
        prg.Save(baseName + ".bm.prg");

        prg = new PrgFile(colorAddress, _convertedData.ScreenRam);
        prg.Save(baseName + ".sc.prg");

        if (_convertedData is C64MulticolorData multicolor)
        {
            prg = new PrgFile(0xD800, multicolor.ColorRam);
            prg.Save(baseName + ".co.prg");

            prg = new PrgFile(backgroundAddress, new byte[] { multicolor.BackgroundColor });
            prg.Save(baseName + ".bg.prg");

            txtStatus.Text = $"File salvati: {Path.GetFileName(baseName)}.bm.prg / .sc.prg / .co.prg / .bg.prg";
        }
        else
        {
            txtStatus.Text = $"File salvati: {Path.GetFileName(baseName)}.bm.prg / .sc.prg";
        }
    }
}
