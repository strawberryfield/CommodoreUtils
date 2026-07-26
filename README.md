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

# Pack NuGet packages
dotnet pack -c Release -o ./nupkg
```

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
│   │   └── Strings2Prg.cs # String collection PRG with pointer table
│   └── Conversions.cs    # Little-endian ushort↔bytes helpers
├── CommodoreImages/       # Image conversion library (net10.0, separate NuGet package)
│   ├── C64Palette.cs     # C64 16-color palette definitions
│   ├── C64MulticolorData.cs # Multicolor bitmap container (8KB bitmap, 1KB color RAM, 1KB screen RAM)
│   ├── C64HiresData.cs      # Hires bitmap container (8KB bitmap, 1KB screen RAM)
│   ├── MulticolorConverter.cs # Image to C64 multicolor conversion (Magick.NET)
│   └── HiresConverter.cs     # Image to C64 hires conversion (Magick.NET)
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

## Tools

### Pet2Ascii - PETSCII to ASCII Converter

Converts PETSCII `.SEQ` files to ASCII text files.

```bash
# Convert PETSCII .SEQ to ASCII text
dotnet run --project Pet2Ascii/Pet2Ascii.csproj input.seq

# Uppercase-only conversion
dotnet run --project Pet2Ascii/Pet2Ascii.csproj -u input.seq

# Specify output file
dotnet run --project Pet2Ascii/Pet2Ascii.csproj -o output.txt input.seq
```

**Options:**
| Option | Description |
|--------|-------------|
| `-q, --quiet` | Suppress banner output |
| `-u, --uppercase` | Use uppercase-only charset |
| `-o, --out=` | Output file name (default: same as input with .txt extension) |

### A2Petscii - ASCII to PETSCII Converter

Converts ASCII text files to PETSCII `.SEQ` files.

```bash
# Convert ASCII text to PETSCII .SEQ file
dotnet run --project A2Petscii/A2Petscii.csproj input.txt

# Uppercase-only conversion
dotnet run --project A2Petscii/A2Petscii.csproj -u input.txt

# Word wrap at column 40
dotnet run --project A2Petscii/A2Petscii.csproj -w 40 input.txt

# Specify output file
dotnet run --project A2Petscii/A2Petscii.csproj -o output.seq input.txt
```

**Options:**
| Option | Description |
|--------|-------------|
| `-q, --quiet` | Suppress banner output |
| `-u, --uppercase` | Use uppercase-only charset |
| `-w, --wrap=` | Word-wrap at column (default: 0, no wrap) |
| `-o, --out=` | Output file name (default: same as input with .seq extension) |

### Text2Prg - Text to PRG Converter

Converts text files or lists of strings into Commodore 64 PRG files.

```bash
# Convert text to PRG (default address $C000)
dotnet run --project Text2Prg/Text2Prg.csproj input.txt

# Specify start address
dotnet run --project Text2Prg/Text2Prg.csproj -a $1000 input.txt

# Convert as list of strings with pointer table
dotnet run --project Text2Prg/Text2Prg.csproj -l input.txt

# Uppercase-only charset
dotnet run --project Text2Prg/Text2Prg.csproj -u input.txt
```

**Options:**
| Option | Description |
|--------|-------------|
| `-q, --quiet` | Suppress banner output |
| `-u, --uppercase` | Use uppercase-only charset |
| `-l, --listmode` | Convert to a list of strings |
| `-a, --address=` | Start address (default: $C000) |
| `-s, --stringsonly` | Do not include pointer index (listmode only) |
| `-o, --out=` | Output file name (default: same as input with .prg extension) |

### Prg2Data - PRG to DATA Lines Converter

Converts `.PRG` files to ASCII text with BASIC DATA lines, useful for embedding PRG data in C64 BASIC loaders.

```bash
# Convert PRG to DATA lines
dotnet run --project Prg2Data/Prg2Data.csproj input.prg
```

**Options:**
| Option | Description |
|--------|-------------|
| `-q, --quiet` | Suppress banner output |
| `-o, --out=` | Output file name (default: same as input with .txt extension) |

### ImageConverter - Image to C64 Bitmap Converter

Converts 320x200 PNG/BMP images to C64 bitmap format, producing PRG files for direct loading on a Commodore 64.

```bash
# Convert to multicolor format (default)
dotnet run --project ImageConverter/ImageConverter.csproj -p $E000 -c $D800 input.png

# Convert to hires (2-color) format
dotnet run --project ImageConverter/ImageConverter.csproj -2 -p $E000 -c $D800 input.png

# Disable dithering
dotnet run --project ImageConverter/ImageConverter.csproj -d input.png

# Adjust brightness bias (reduces dark colors)
dotnet run --project ImageConverter/ImageConverter.csproj --brightness=0.5 input.png
```

**Output Files:**
- **Multicolor mode**: `.bm.prg` (bitmap), `.sc.prg` (screen RAM), `.co.prg` (color RAM), `.bg.prg` (background color)
- **Hires mode**: `.bm.prg` (bitmap), `.sc.prg` (screen RAM)

**Options:**
| Option | Description |
|--------|-------------|
| `-q, --quiet` | Suppress banner output |
| `-h, -?` | Show help |
| `-2, --hires` | Convert to 2-color hires format instead of multicolor |
| `-p, --pixeladdress=` | Bitmap address (default: $E000) |
| `-c, --coloraddress=` | Color RAM address (default: $D800) |
| `-b, --backgroundaddress=` | Background color address (default: $D021, VIC-II register) |
| `-o, --out=` | Output file name (default: same as input with .prg extension) |
| `-d, --no-dither` | Disable Floyd-Steinberg dithering (plain quantization) |
| `--brightness=` | Brightness bias 0.0-1.0 favoring brighter colors (default: 0.35) |
| `--brightness-mode=` | Where to apply bias: 'quantization', 'selection', or 'both' (default) |

### Img2Prg - BYTE List to PRG Converter

Converts text files containing BYTE data statements into PRG files.

```bash
# Convert BYTE list to PRG
dotnet run --project Img2Prg/Img2Prg.csproj input.txt

# Specify start address
dotnet run --project Img2Prg/Img2Prg.csproj -a $E000 input.txt
```

**Options:**
| Option | Description |
|--------|-------------|
| `-q, --quiet` | Suppress banner output |
| `-a, --address=` | Start address (default: $C000) |
| `-o, --out=` | Output file name (default: same as input with .prg extension) |

## Image Conversion Examples

The `c64/samples/` folder contains sample images demonstrating the ImageConverter output:

### Multicolor Bitmap Conversion

The C64 multicolor mode uses 160x200 resolution with double-wide pixels (4 colors per 8x8 cell):

| Image | Description |
|-------|-------------|
| ![Multicolor with Floyd-Steinberg dithering](ImageConverter/c64/samples/multicolor-dither.jpg) | Multicolor bitmap with dithering - smooth color transitions and reduced banding |
| ![Hires with Floyd-Steinberg dithering](ImageConverter/c64/samples/hires-dither.jpg) | Hires (2-color) bitmap with dithering - standard resolution with error diffusion |

### Hires Bitmap Conversion

The C64 hires mode uses full 320x200 resolution with 2 colors per 8x8 cell:

| Image | Description |
|-------|-------------|
| ![Hires quantized](ImageConverter/c64/samples/hires-quantized.jpg) | Hires bitmap without dithering - sharp, clean output for simple images |

### Running on C64

The `c64/` folder includes BASIC viewer programs and a disk image:

- **mcviewer.bas** - Multicolor bitmap viewer - loads `.BM.PRG`, `.SC.PRG`, `.CO.PRG`, and `.BG.PRG` files
- **hrviewer.bas** - Hires bitmap viewer - loads `.BM.PRG` and `.SC.PRG` files
- **imgviewer.d64** - Disk image containing both viewer programs
- **convert.cmd** - Example batch file showing conversion commands

## C64 Viewer Programs

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
