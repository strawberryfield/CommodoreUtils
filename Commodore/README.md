# Commodore Package

A small utility library for reading, writing and generating Commodore PRG files in .NET.

This package provides in-memory representations and helpers for PRG files (two-byte little-endian load address followed by data), helpers to produce BASIC DATA lines, and utilities to build PRG payloads that contain pointer tables and null-terminated strings.

## Features

- Read and write PRG files with explicit load address handling.
- Create PRG payloads from collections of strings (PETSCII conversion + pointer table).
- Produce Commodore BASIC `DATA` lines for embedding PRG data in source listings.
- Utilities for relocating pointer tables when changing load addresses.

## Getting started

Prerequisites:

- .NET 10 SDK
- Visual Studio or any IDE that supports .NET 10

Build and test from the command line:

__dotnet build__

__dotnet test__

In Visual Studio open the solution and use the __Build__ command and run unit tests from the __Test Explorer__ window.

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

See `CONTRIBUTING.md` for repository-level contribution guidelines. Follow the project's coding standards (see `.editorconfig`) and run the test suite before submitting pull requests.

## License

Casasoft Commodore Utils is licensed under the GNU Affero General Public License v3 (AGPL-3.0). See the repository license file for details.

## Contact

Author: Roberto Ceccarelli (http://strawberryfield.altervista.org)

