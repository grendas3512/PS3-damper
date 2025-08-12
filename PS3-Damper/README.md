# PS3-Damper

A small Windows console tool to mount an ISO, copy all its contents to a folder next to the ISO (named after the ISO), and then unmount the ISO.

## What it does

-  Prompts for (or accepts as an argument) the full path to an .iso file
-  Elevates to mount the ISO via PowerShell `Mount-DiskImage`
-  Detects the assigned drive letter and copies all files while preserving folders
-  Unmounts the ISO via PowerShell `Dismount-DiskImage`

## Requirements

-  Windows 10/11 with built-in ISO mounting (PowerShell `Mount-DiskImage`)
-  .NET 9.0 SDK (or adapt the target framework if needed)
-  Ability to elevate (UAC prompt appears when mounting/unmounting)

## Build

From PowerShell (pwsh):

```powershell
# Build the solution from the repository root
dotnet build .\PS3-Damper.sln -c Debug
```

Build output will be in `PS3-Damper\bin\Debug\net9.0`.

## Run

You can run with an argument, or run and follow the prompt. Examples below use PowerShell (pwsh).

-  Run with ISO path as argument:

```powershell
dotnet run --project ".\PS3-Damper\PS3-Damper.csproj" -- "C:\Path\To\Your.iso"
```

-  Or run and enter the ISO path interactively when prompted:

```powershell
dotnet run --project ".\PS3-Damper\PS3-Damper.csproj"
```

-  Run the built executable directly:

```powershell
& ".\PS3-Damper\bin\Debug\net9.0\PS3-Damper.exe"
```

## Output

-  If your ISO is at `C:\ISOs\Game.iso`, the tool will mount it, then copy all contents to `C:\ISOs\Game\` (folder named after the ISO), preserving the internal directory structure.

## Notes and Troubleshooting

-  Admin prompt (UAC): Mount/Unmount operations require elevation. Approve the prompt when asked.
-  Paths with spaces: Always quote paths as shown in the examples.
-  Drive detection delay: The tool retries briefly while waiting for Windows to assign the drive letter.
-  Copy errors like "target is a directory": Fixed by preserving folder structure using relative paths.
-  If `Mount-DiskImage` is restricted by policy, ensure PowerShell is available and not blocked by your environment.

## Safety

-  The copy operation overwrites existing files in the destination folder.
-  The tool attempts to unmount even if copying fails.

## How it works (brief internals)

-  `IsoMounter.Mount` runs `Mount-DiskImage` (elevated), polls for the mounted drive letter with `Get-DiskImage | Get-Volume`, and returns the root like `E:\`.
-  `DirectoryCopier.CopyTree` iterates files, computes relative paths, creates folders as needed, and copies with overwrite.
-  `IsoMounter.Dismount` runs `Dismount-DiskImage` (elevated) to clean up.
