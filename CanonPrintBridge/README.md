# Canon Print Bridge — app notes

> Project overview, features and install instructions are in the [root README](../README.md). This file holds developer notes for the WPF app.

A bridge for printing to the **Canon LASER SHOT LBP-1120** from Windows 11 x64.

The LBP-1120 is a host-based CAPT printer with no 64-bit driver, so it prints only inside a guest Windows XP (VirtualBox, USB passthrough). This project makes printing "almost direct": on Windows 11 you choose a file and parameters, and the actual printing is performed in XP automatically.

```
[Win11]  WPF application
   • picked a file + parameters (paper size, copies, scale, page range)
   • Print ──► writes <id>.pdf + <id>.job.json to the shared Queue folder
   • shows status ◄── reads status\<id>.status.json
                               │
[XP VM]  watcher.vbs (in startup)
   • sees job.json ──► selects the print queue by paper size
   • SumatraPDF prints to the Canon (copies / scale / page range)
   • writes status.json
```

## Structure

```
CanonPrintBridge.sln            ← open in Rider / VS
src/CanonPrintBridge/           ← WPF application (.NET 8, net8.0-windows)
  appsettings.json              ← paths + language (QueueRoot, LauncherPath, …)
  Services/                     ← queue, health, converters, localization
  Resources/                    ← Strings.ru.json / Strings.en.json
xp-watcher/
  watcher.vbs                   ← watcher for XP
  README-XP-setup.md            ← one-time setup of the XP side
```

## Build

```powershell
dotnet build CanonPrintBridge.sln -c Release
# ready-made exe:
# src\CanonPrintBridge\bin\Release\net8.0-windows\CanonPrintBridge.exe
```

Or just open `CanonPrintBridge.sln` in Rider and run it.

## Setup

1. **Win11**: if needed, adjust `src/CanonPrintBridge/appsettings.json` — `QueueRoot` (the host path of the shared folder), `LauncherPath`, `VBoxManagePath`, `OfficeToPdfPath`, `Language`.
2. **XP**: perform the one-time setup per `xp-watcher/README-XP-setup.md` (SumatraPDF 3.1.2, print queues per paper size, watcher in startup, autologin).

## Usage

1. **Start printer (VM)** — brings up the VM and passes through USB (calls the existing `Print-Canon.ps1`).
2. Drag a file into the window (or **Open…**). Non-PDF (Word, image, text) is converted to PDF automatically.
3. Set the parameters → **Print**.
4. Follow the status in the log; **Done** appears after the page actually prints.

## Limitations (due to the LBP-1120 hardware)

- **B/W only** — the printer is monochrome.
- **Duplex — manual only** — there is no duplex module.
- **Paper size — via XP queues** (`Canon LBP-1120 A4`), not via driver flags.
- The XP VM must be **running and logged in** (can be headless + autologin).
