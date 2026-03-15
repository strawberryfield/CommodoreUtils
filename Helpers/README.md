Casasoft.Helpers
================

Lightweight helper utilities targeted at .NET 10.0. Provides common helper functions and extension methods for reuse across Casasoft projects.

Status
------
- Target framework: `net10.0`
- XML documentation generation enabled (`GenerateDocumentationFile` = `True`)
- License: AGPL-3.0-or-later

Quick Start
-----------

Install (from NuGet)
- If the package is published as `Casasoft.Helpers`:
  - `dotnet add package Casasoft.Helpers`

Build from source
- Restore and build:
  - `dotnet restore`
  - `dotnet build -c Release`
- Pack:
  - `dotnet pack -c Release -o ./nupkg`

Usage
-----
- Add a reference to the library and use the `Casasoft.Helpers` namespace in your code:

```csharp
using Casasoft.Helpers;

// example usage (replace with actual API from the library)
var result = HelperClass.DoSomething();
```

API Documentation
-----------------
- XML documentation is produced during build. The `.xml` file is created adjacent to the assembly when `dotnet build` or `dotnet pack` runs.
- Generate more user-facing docs or publish API docs from the XML comments as needed.

Contributing
------------
- Fork the repo, create a feature branch, and submit a pull request.
- Keep changes targeted and include unit tests where appropriate.
- Follow the existing code style and include XML comments for public APIs.

Authors and Maintainers
-----------------------
- Roberto Ceccarelli (http://strawberryfield.altervista.org)
- Company: Casasoft

License
-------
This project is licensed under the AGPL-3.0-or-later. See the `LICENSE` file for details.