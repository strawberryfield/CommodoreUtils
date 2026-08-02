Casasoft Commodore Utils - Tools Reference
===========================================

This document provides detailed information about all the command-line tools in the Casasoft Commodore Utils solution, excluding ImageConverter and ImageConverterGUI which are covered in `README-ImageConverter.md`.

## Overview

This solution provides utilities for Commodore 64 development, enabling conversion between modern formats and C64 PRG/SEQ files. Each tool is designed to work with the core `Commodore` and `Helpers` libraries.

## Tools

### Pet2Ascii - PETSCII to ASCII Converter

**Purpose:** Converts PETSCII `.SEQ` files to ASCII text files.

**Usage Examples:**

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

**Technical Details:**
- Reads binary input files as PETSCII bytes
- Converts to ASCII using the `Charset.ASCII()` method
- Supports case conversion via `-u` flag
- Handles standard .NET text encoding

### A2Petscii - ASCII to PETSCII Converter

**Purpose:** Converts ASCII text files to PETSCII `.SEQ` files.

**Usage Examples:**

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

**Technical Details:**
- Normalizes EOL characters to CR (carriage return)
- Applies PETSCII encoding using `Charset.PETSCII()`
- Supports word wrapping via `TextHelpers.WordWrap()`
- Handles wildcard file expansion for batch processing

### Text2Prg - Text to PRG Converter

**Purpose:** Converts text files or lists of strings into Commodore 64 PRG files.

**Usage Examples:**

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

**Technical Details:**
- **List Mode (`-l`)**: Creates PRG files with pointer tables for string arrays using `Strings2Prg`
- **Text Mode**: Converts plain text to PRG with null-terminated strings
- Supports both hex and decimal address formats
- Uses `CommandLineHelpers.GetIntParameter()` for address validation
- Implements wildcard file expansion for batch processing

### Prg2Data - PRG to DATA Lines Converter

**Purpose:** Converts `.PRG` files to ASCII text with BASIC DATA lines, useful for embedding PRG data in C64 BASIC loaders.

**Usage Examples:**

```bash
# Convert PRG to DATA lines
dotnet run --project Prg2Data/Prg2Data.csproj input.prg
```

**Options:**
| Option | Description |
|--------|-------------|
| `-q, --quiet` | Suppress banner output |
| `-o, --out=` | Output file name (default: same as input with .txt extension) |

**Technical Details:**
- Uses `PrgFile.CreateDataLines()` to generate BASIC DATA statements
- Generates formatted output with 8 bytes per line by default
- Preserves filename for BASIC loader reference
- Simple single-file processing per command

### Img2Prg - BYTE List to PRG Converter

**Purpose:** Converts text files containing BYTE data statements into PRG files.

**Usage Examples:**

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

**Technical Details:**
- Parses text files with `BYTE xxx,yyy,zzz...` format
- Supports up to 1024 bytes of data
- Uses fixed 1024-byte array with offset at 1024 bytes
- Simple line-by-line parsing with byte conversion

### Commodore Package - Core Library

**Purpose:** A small utility library for reading, writing and generating Commodore PRG files in .NET.

**Features:**

- Read and write PRG files with explicit load address handling
- Create PRG payloads from collections of strings (PETSCII conversion + pointer table)
- Produce Commodore BASIC `DATA` lines for embedding PRG data in source listings
- Utilities for relocating pointer tables when changing load addresses

**Usage Examples:**

```csharp
// Create a PrgFile from a file:
var prg = new PrgFile("myprogram.prg");
Console.WriteLine($"Load address: {prg.LoadAddress:X4}");

// Save a PrgFile:
prg.Save("out.prg");

// Create a PRG containing pointer-indexed strings:
var strings = new List<string> { "HELLO", "WORLD" }; 
var s2p = new Strings2Prg(0x0801, strings, LowerCase: false, AddIndex: true); 
s2p.Save("strings.prg");

// Generate BASIC DATA lines for embedding:
string basic = prg.CreateDataLines(bytesPerLine: 8, targetFilename: "OUT.PRG"); 
Console.WriteLine(basic);
```

### Helpers Package - Shared Utilities

**Purpose:** Lightweight helper utilities targeted at .NET 10.0, providing common helper functions and extension methods for reuse across Casasoft projects.

**Status:**
- Target framework: `net10.0`
- XML documentation generation enabled (`GenerateDocumentationFile` = `True`)
- License: AGPL-3.0-or-later

**API Documentation:**
- XML documentation is produced during build. The `.xml` file is created adjacent to the assembly when `dotnet build` or `dotnet pack` runs.

**Available Helper Classes:**
- `CommandLineHelpers` - Hex/decimal integer parsing and format help
- `FileHelpers` - Wildcard expansion for file paths
- `TextHelpers` - EOL normalization and word wrapping utilities

## Build and Package Commands

### Solution Build Commands

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
```

### NuGet Package Commands

```bash
# Pack all NuGet packages
dotnet pack -c Release -o ./nupkg

# Individual package builds
# Commodore (Casasoft.Commodore.Utils)
dotnet pack -c Release Commodore/Commodore.csproj -o ./nupkg

# CommodoreImages (Casasoft.Commodore.Images)
dotnet pack -c Release CommodoreImages/CommodoreImages.csproj -o ./nupkg

# Helpers (Casasoft.Helpers)
dotnet pack -c Release Helpers/Helpers.csproj -o ./nupkg
```

### Visual Studio / Rider Integration

- **Dependencies:** Requires .NET 10.0 SDK
- **Build output:** Binaries are placed in `bin/Release/net10.0/` subdirectories
- **Package output:** NuGet packages are created in `./nupkg/` directory

## Running Applications

### Command Line Tools

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

### Image Conversion Samples

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

## Dependencies

### Required Packages

- **Target framework:** .NET 10.0
- **Mono.Options:** Command-line parsing (all executable projects)
- **SkiaSharp:** Image processing for `CommodoreImages` and `ImageConverter` projects only
- **Avalonia:** Cross-platform UI framework for `ImageConverterGUI` project (configured for win-x64, linux-x64, and osx-arm64 runtimes only)
- **Avalonia.Desktop:** Desktop integration for `ImageConverterGUI` project (configured for win-x64, linux-x64, and osx-arm64 runtimes only)
- **Avalonia.Themes.Fluent:** Fluent design theme for `ImageConverterGUI` project (configured for win-x64, linux-x64, and osx-arm64 runtimes only)
- **Avalonia.Fonts.Inter:** Inter font package for `ImageConverterGUI` project (configured for win-x64, linux-x64, and osx-arm64 runtimes only)
- **Casasoft.Avalonia.Controls:** Custom controls (FileTextBox, NumericUpDown, etc.) for `ImageConverterGUI` project (configured for win-x64, linux-x64, and osx-arm64 runtimes only)
- **Magick.NET-Q16-AnyCPU:** Image processing via ImageMagick for `ImageConverterGUI` project only (matched quantum depth with CommodoreImages for compatibility)

### NuGet Package Structure

- **Casasoft.Commodore.Utils** - Core Commodore utilities (PRG file handling, charset conversion, string arrays)
- **Casasoft.Commodore.Images** - Image conversion library (multicolor and hires formats)
- **Casasoft.Helpers** - Shared utility library (command-line parsing, file utilities, text utilities)

## Coding Conventions

- **File header template:** All `.cs` files must include the standard copyright header (defined in `.editorconfig`)
- **Indentation:** 4 spaces
- **Line endings:** CRLF
- **Expression-bodied members:** Allowed for accessors and properties; disabled for constructors/methods
- **Naming:** PascalCase for types and non-field members; interfaces prefixed with `I`
- **Using directives:** Place outside namespace declarations

## License

This project is licensed under the GNU Affero General Public License v3.0 (AGPL-3.0). See the [LICENSE](LICENSE) file for details.

## Authors

- Roberto Ceccarelli (https://strawberryfield.altervista.org)
- Company: Casasoft

## Contributing

Contributions are welcome! Please fork the repository, create a feature branch, and submit a pull request. Keep changes targeted and include unit tests where appropriate. Follow the existing code style and include XML comments for public APIs.

## Project Structure

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
│   │   └── Strings2Prg.cs # String list to PRG conversion with pointer tables
│   └── Conversions.cs    # Little-endian ushort↔bytes helpers
├── CommodoreImages/       # Image conversion library (net10.0, separate NuGet package)
│   ├── C64Palette.cs         # C64 16-color palette definitions
│   ├── C64MulticolorData.cs  # Multicolor bitmap data container
│   ├── C64HiresData.cs       # Hires bitmap data container
│   ├── MulticolorConverter.cs  # Image to C64 multicolor conversion
│   └── HiresConverter.cs     # Image to C64 hires conversion
├── Pet2Ascii/            # PETSCII .SEQ to ASCII converter
├── A2Petscii/           # ASCII to PETSCII .SEQ converter
├── Text2Prg/            # Text/string to .PRG converter
├── Prg2Data/            # .PRG to BASIC DATA lines converter
├── ImageConverter/       # PNG/BMP to C64 multicolor .PRG converter
└── Img2Prg/             # BYTE list text file to .PRG converter
```