# Casasoft Commodore Package

A small utility library for reading, writing and generating Commodore PRG files in .NET.

This package provides in-memory representations and helpers for PRG files (two-byte little-endian load address followed by data), helpers to produce BASIC DATA lines, and utilities to build PRG payloads that contain pointer tables and null-terminated strings.

## Features

- Read and write PRG files with explicit load address handling.
- Create PRG payloads from collections of strings (PETSCII conversion + pointer table).
- Produce Commodore BASIC `DATA` lines for embedding PRG data in source listings.
- Utilities for relocating pointer tables when changing load addresses.

Quick Start
-----------

Install (from NuGet)
- If the package is published as `Casasoft.Commodore.Utils`:
  - `dotnet add package Casasoft.Commodore.Utils`

Build from source
- Restore and build:
  - `dotnet restore`
  - `dotnet build -c Release`
- Pack:
  - `dotnet pack -c Release -o ./nupkg`

## Usage examples

Create a `PrgFile` from a file:

```
// C# example var prg = new PrgFile("myprogram.prg"); 
Console.WriteLine($"Load address: {prg.LoadAddress:X4}");
```

Save a `PrgFile`:

```
prg.Save("out.prg");
```

Create a PRG containing pointer-indexed strings:

````
var strings = new List<string> { "HELLO", "WORLD" }; 
var s2p = new Strings2Prg(0x0801, strings, LowerCase: false, AddIndex: true); 
s2p.Save("strings.prg");
````

Relocate pointers in an existing PRG payload in memory:

```
// Adjust pointer table values so strings point to a new load address 
Strings2Prg.RelocatePointers(s2p.Data, newLoadAddress: 0x1000, oldLoadAddress: 0x0801);
```

Generate BASIC DATA lines for embedding:

```
string basic = prg.CreateDataLines(bytesPerLine: 8, targetFilename: "OUT.PRG"); 
Console.WriteLine(basic);
```


## API

Main classes:

- `PrgFile` — represents a PRG payload with `LoadAddress` and `Data`, load/save helpers and `CreateDataLines`.
- `Strings2Prg` — helper to construct PRG payloads with a pointer table and null-terminated strings. Includes `CreateArrays` and `RelocatePointers` helpers.

See XML doc comments in the source for detailed API behavior and edge-case notes.

## Contributing

- Fork the repo, create a feature branch, and submit a pull request.
- Keep changes targeted and include unit tests where appropriate.
- Follow the existing code style and include XML comments for public APIs.

## License

Casasoft Commodore Utils is licensed under the GNU Affero General Public License v3 (AGPL-3.0). See the repository license file for details.

Authors and Maintainers
-----------------------
- Roberto Ceccarelli (http://strawberryfield.altervista.org)
- Company: Casasoft

