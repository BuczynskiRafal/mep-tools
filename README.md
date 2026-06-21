# Fast Connect MEP

A Revit add-in that connects MEP elements. Pick two pipes or fittings and it lines them up and joins them.

![icon](assets/icon.png)

## Usage

Open the **MEP Tools** tab on the ribbon and click **Fast Connect**. Pick the element that should stay in place, then the one to move. The add-in finds the nearest matching free connectors, moves and rotates the second element so they meet, and connects them. It runs as a single transaction, so one Ctrl+Z undoes the whole thing.

## Build and install

Targets Revit 2026 on .NET 8. Build on Windows:

```powershell
dotnet build src\FastConnect.Revit -c Release
```

Copy the two output DLLs and the manifest into the Revit add-ins folder:

```powershell
$dest = "$env:APPDATA\Autodesk\Revit\Addins\2026"
New-Item -ItemType Directory -Force -Path $dest
Copy-Item "src\FastConnect.Revit\bin\Release\FastConnect.Revit.dll" $dest
Copy-Item "src\FastConnect.Revit\bin\Release\FastConnect.Core.dll"  $dest
Copy-Item "src\FastConnect.Revit\FastConnect.addin"                 $dest
```

Do not copy `RevitAPI*.dll`; Revit loads those itself.

## Tests

```bash
dotnet test
```

## Stack

C#, .NET 8, Revit API 2026, xUnit.
