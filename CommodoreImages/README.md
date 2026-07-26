# Casasoft Commodore Images

Copyright (c) 2025-2026 Roberto Ceccarelli  
https://strawberryfield.altervista.org  
Released under GNU AGPL 3.0

A library for converting images to Commodore 64 bitmap formats. Supports both multicolor (160x200, 4 colors per cell) and hires (320x200, 2 colors per cell) bitmap modes with Floyd-Steinberg dithering and brightness bias controls.

## Features

- **MulticolorConverter** - Converts images to C64 multicolor bitmap format (160x200 pixels, 4 colors per 8x8 character cell)
- **HiresConverter** - Converts images to C64 standard (hires) bitmap format (320x200 pixels, 2 colors per 8x8 character cell)
- **C64MulticolorData** - Container for multicolor bitmap data (8KB bitmap, 1KB color RAM, 1KB screen RAM)
- **C64HiresData** - Container for hires bitmap data (8KB bitmap, 1KB screen RAM)
- **C64Palette** - C64 16-color palette with accurate RGB values and closest-color matching

## Dependencies

- **SkiaSharp** - Image processing

## Usage

```csharp
using Casasoft.Commodore;

// Load an image and convert to multicolor format
using var bitmap = SKBitmap.Decode("input.png");
var converter = new MulticolorConverter();
var result = converter.ConvertImage(bitmap, useDithering: true, brightnessBias: 0.35);

// Access the resulting data
byte[] bitmapData = result.BitmapData;  // 8KB
byte[] colorRam = result.ColorRam;      // 1KB
byte[] screenRam = result.ScreenRam;    // 1KB
byte bgColor = result.BackgroundColor;  // Background color (VIC-II $D021)
```

## License

This project is licensed under the GNU Affero General Public License v3.0 (AGPL-3.0). See the [LICENSE](../LICENSE) file for details.