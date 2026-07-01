# CLAUDE.md — Canon Print Bridge

Single-project repository: **Canon Print Bridge** — printing on the Canon LASER SHOT
LBP-1120 (host-based CAPT, no x64 driver) from Windows 11 via a Windows XP guest
in VirtualBox. The app on Win11 selects a PDF + parameters; the actual printing happens
in XP automatically through a file-based queue in a shared folder.

The repository root is `T:\Dev\`. The main code is in `CanonPrintBridge/`.

## Layout

| Path | What |
|---|---|
| `CanonPrintBridge/` | .NET 8 solution (`net8.0-windows`, WPF) |
| `CanonPrintBridge/src/CanonPrintBridge/` | WPF application (Win11 side) |
| `CanonPrintBridge/xp-watcher/` | watcher `watcher.vbs` + one-time XP setup |
| `CanonPrintBridge/docs/` | contracts and specs (see below) |
| `CanonPrintBridge/README.md` | overview and build |
| `CanonPrintBridge/STATUS.md` | current status and actual environment (host+XP) |
| `printer-xp-icon.ico` | application icon (embedded resource) |

Documents in `docs/`:
- `job-contract.md` — file contract `job.json` / `status.json` (this is the "API").
- `ui-spec.md` — UI/UX spec and look&feel brief (the redesign task).
- `implementation-plan.md` — redesign implementation plan.

## Build / publish

```powershell
dotnet build CanonPrintBridge/CanonPrintBridge.sln -c Release
# single-file (see implementation-plan.md, "single exe" phase):
dotnet publish CanonPrintBridge/src/CanonPrintBridge -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Resulting exe after build: `CanonPrintBridge/src/CanonPrintBridge/bin/Release/net8.0-windows/CanonPrintBridge.exe`.

## Actual environment (host + XP), outside the code

Full details in `CanonPrintBridge/STATUS.md`. In brief:
- VirtualBox VM: **`Microelectronics`** (XP). `VBoxManage`: `C:\Program Files\Oracle\VirtualBox\VBoxManage.exe`.
- USB filter "Canon CAPT" (VendorId `04a9`, ProductId `262b`) → the printer is captured automatically when the VM starts.
- XP print queue: **`Canon LBP-1120 A4`** (A5/B5 not created yet).
- Watcher in XP: `C:\CanonBridge\watcher.vbs`, auto-started from the Startup folder of the `User` profile, autologin.
- Bridge folder: host `C:\Virtualization\Shared\Queue\` ↔ guest `\\vboxsvr\Shared\Queue`.
- VM launcher: `Printer_Canon_lbp_1120\Print-Canon.ps1` (in the repo root; the build copies it next to the exe, `LauncherPath` is relative — portability).

## LBP-1120 hardware limitations

Black & white only; duplex — manual only; paper size — via separate XP queues, not driver flags.

## Gotchas (do not repeat)

- **PowerShell 5.1**: `.ps1` is read as ANSI/1251 — Cyrillic without a BOM breaks the parser →
  keep the launcher **ASCII-only**. `$ErrorActionPreference='Stop'` turns stderr from
  VBoxManage into a terminating error → in the launcher use `'Continue'` + `2>$null`.
- **`watcher.vbs`**: comments — English only (cscript/XP handle non-ASCII in the source poorly).
- The guest XP clock is off → the `updatedAt` timestamps in statuses may lie; judge freshness by the
  mtime of files on the **host**, not by the time inside XP.
