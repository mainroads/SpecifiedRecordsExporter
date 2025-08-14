# Specified Records Exporter (PowerShell)

This folder contains a PowerShell implementation of the Specified Records Exporter.

## Usage

```powershell
pwsh ./SpecifiedRecordsExporter.ps1
```

The script is interactive and will prompt for:

1. The root directory containing the project files.
2. Optional free text to prepend to renamed files.

It removes junk files, extracts non-CAD zip archives, recompresses folders that contain
CAD files, renames all files based on their folder path and places them in the root
folder, and finally deletes empty directories. The script mirrors the business logic
from the application's `Worker.cs` including retry loops for file operations, path
length checks when extracting archives and the same CAD/non-CAD handling rules.