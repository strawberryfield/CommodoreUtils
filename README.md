# Casasoft Commodore Utils

Copyright (c) 2025-2026 Roberto Ceccarelli  
https://strawberryfield.altervista.org  
Released under GNU AGPL 3.0

A comprehensive toolkit for Commodore 64 development, enabling conversion between modern formats and C64 PRG/SEQ files. This solution provides utilities for text encoding, image conversion, and PRG file manipulation.

## Features

- **PETSCII Text Conversion** - Convert between ASCII and PETSCII text formats
- **Image Conversion** - Convert 320x200 images to C64 bitmap formats (multicolor and hires)
- **PRG File Tools** - Convert text/data to PRG files and extract DATA lines for BASIC loaders
- **Helper Libraries** - Reusable utilities for .NET development targeting C64 applications

## Requirements

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) or later

## Build Commands

```bash
# Build entire solution
dotnet build -c Release

# Build specific projects
dotnet build -c Release Commodore/Commodore.csproj
dotnet build -c Release CommodoreImages/CommodoreImages.csproj
dotnet build -c Release Pet2Ascii/Pet2Ascii.csproj
dotnet build -c Release A2Petscii/A2Petscii.csproj
dotnet build -c Release Text2Prg/Text2Prg.csproj
dotnet build -c Release Prg2Data/Prg2Data.csproj
dotnet build -c Release ImageConverter/ImageConverter.csproj
dotnet build -c Release Img2Prg/Img2Prg.csproj
dotnet build -c Release ImageConverterGUI/ImageConverterGUI.csproj

# Pack NuGet packages
dotnet pack -c Release -o ./nupkg
```

### Detailed Documentation

For comprehensive documentation of each project:
- [README-Tools.md](README-Tools.md) - Documentation for all command-line tools
- [README-ImageConverter.md](README-ImageConverter.md) - Documentation for ImageConverter and ImageConverterGUI projects

## Solution Architecture

```
CommodoreUtils.sln
├── Helpers/              # Shared utility library (net10.0)
│   ├── FileHelpers.cs    # Wildcard expansion for file paths
│   ├── TextHelpers.cs    # EOL normalization, word wrapping
│   └── CommandLineHelpers.cs # Hex parameter parsing
├── Commodore/            # Core Commodore library (net10.0)
│   ├── Charset.cs        # ASCII ↔ PETSCII conversion
│   ├── Prg/
│   │   ├── IPrgFile.cs    # PRG file interface
│   │   ├── PrgFile.cs     # PRG file implementation
│   │   └── Strings2Prg.cs # String list to PRG conversion with pointer table
│   └── Conversions.cs    # Little-endian ushort↔bytes helpers
├── CommodoreImages/       # Image conversion library (net10.0, separate NuGet package)
│   ├── C64Palette.cs         # C64 16-color palette definitions
│   ├── C64MulticolorData.cs  # Multicolor bitmap data container (8KB bitmap, 1KB color RAM, 1KB screen RAM)
│   ├── C64HiresData.cs       # Hires bitmap data container (8KB bitmap, 1KB screen RAM)
│   ├── MulticolorConverter.cs # Image to C64 multicolor conversion (SkiaSharp)
│   └── HiresConverter.cs       # Image to C64 hires conversion (SkiaSharp)
├── Pet2Ascii/            # PETSCII .SEQ to ASCII converter
├── A2Petscii/           # ASCII to PETSCII .SEQ converter
├── Text2Prg/            # Text/string to .PRG converter
├── Prg2Data/            # .PRG to BASIC DATA lines converter
├── ImageConverter/       # PNG/BMP to C64 multicolor .PRG converter
└── Img2Prg/             # BYTE list text file to .PRG converter
```

### Dependencies

- **Mono.Options** - Command-line parsing (all executable projects)
- **Magick.NET (ImageMagick)** - Image processing (for ImageConverter and CommodoreImages)

## Quick Start

### Running Tools

Each executable tool can be run directly via `dotnet run --project <project-path>`:

```bash
# Convert ASCII text to PETSCII .SEQ file
dotnet run --project A2Petscii/A2Petscii.csproj -u input.txt

# Convert PETSCII .SEQ to ASCII text
dotnet run --project Pet2Ascii/Pet2Ascii.csproj input.seq

# Convert text to .PRG (default address $C000)
dotnet run --project Text2Prg/Text2Prg.csproj -a $1000 input.txt

# Convert .PRG to DATA lines
dotnet run --project Prg2Data/Prg2Data.csproj input.prg

# Convert 320x200 image to C64 multicolor .PRG
dotnet run --project ImageConverter/ImageConverter.csproj -p $E000 -c $D800 input.png

# Run the Avalonia GUI for image conversion
dotnet run --project ImageConverterGUI/ImageConverterGUI.csproj

# Convert BYTE list text file to .PRG
dotnet run --project Img2Prg/Img2Prg.csproj input.txt
```

### Image Conversion Examples

The `c64/samples/` folder contains sample images demonstrating the ImageConverter output:

- **multicolor-dither.jpg** - Multicolor bitmap with Floyd-Steinberg dithering
- **hires-dither.jpg** - Hires (2-color) bitmap with dithering
- **hires-quantized.jpg** - Hires bitmap without dithering

### Running on C64

To display converted images on a real C64 or emulator:

1. Load the appropriate viewer: `LOAD"MCVIEWER",8,1` (multicolor) or `LOAD"HRVIEWER",8,1` (hires)
2. Run the program: `RUN`
3. Enter the image name (without extension) when prompted
4. The viewer loads all component files and displays the image

The viewers configure the VIC-II chip for bitmap mode and restore the original screen configuration when you press ENTER.

## License

This project is licensed under the GNU Affero General Public License v3.0 (AGPL-3.0). See the [LICENSE](LICENSE) file for details.

## Authors

- Roberto Ceccarelli (https://strawberryfield.altervista.org)
- Company: Casasoft

## Contributing

Contributions are welcome! Please fork the repository, create a feature branch, and submit a pull request. Keep changes targeted and include unit tests where appropriate. Follow the existing code style and include XML comments for public APIs.