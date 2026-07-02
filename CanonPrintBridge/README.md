# Canon Print Bridge

A bridge for printing to the **Canon LASER SHOT LBP-1120** from Windows 11 x64.

The LBP-1120 is a host-based CAPT printer with no 64-bit driver. Printing
works only inside a guest Windows XP (VirtualBox, USB passthrough). This project
makes printing "almost direct": on Win11 you choose a PDF and parameters, and the actual
printing is performed in XP automatically.

```
[Win11]  WPF application
   • picked a PDF + parameters (paper size, copies, scale, pages, manual duplex)
   • Печать ──► writes <id>.pdf + <id>.job.json to the shared Queue folder
   • shows status ◄── reads status\<id>.status.json
                               │
[XP VM]  watcher.vbs (in startup)
   • sees job.json ──► selects the print queue by paper size
   • SumatraPDF prints to the Canon (copies / scale / odd-even)
   • on manual duplex: odd → "flip the stack" → even
   • writes status.json
```

## Structure

```
CanonPrintBridge.sln            ← open in Rider / VS
src/CanonPrintBridge/           ← WPF application (.NET 8, net8.0-windows)
  appsettings.json              ← paths: QueueRoot, LauncherPath
xp-watcher/
  watcher.vbs                   ← watcher for XP
  README-XP-setup.md            ← one-time setup of the XP side
docs/
  job-contract.md               ← job.json / status.json contract (the "API")
  ui-spec.md                    ← UI/UX specification of the window redesign
  implementation-plan.md        ← implementation plan for the redesign
```

Current status and the actual environment (host + XP) are in `STATUS.md`.

## Build

```powershell
dotnet build CanonPrintBridge.sln -c Release
# ready-made exe:
# src\CanonPrintBridge\bin\Release\net8.0-windows\CanonPrintBridge.exe
```

Or just open `CanonPrintBridge.sln` in Rider and run it.

## Setup

1. **Win11**: if needed, adjust `src/CanonPrintBridge/appsettings.json`
   (`QueueRoot` — the host path of the shared folder; `LauncherPath` — path to `Print-Canon.ps1`).
2. **XP**: perform the one-time setup per `xp-watcher/README-XP-setup.md`
   (SumatraPDF 3.1.2, print queues per paper size, watcher in startup, autologin).

## Usage

1. The **«Запустить принтер (VM)»** button — brings up the VM and passes through USB
   (calls the existing `Print-Canon.ps1`).
2. Drag a PDF into the window (or **«Обзор…»**).
3. Set the parameters → **Печать**.
4. Follow the status in the log; on manual duplex a prompt to flip the stack will appear.

## Limitations (due to the LBP-1120 hardware)

- **B/W only** — the printer is monochrome.
- **Duplex — manual only** — there is no duplex module.
- **Paper size — via XP queues** (`Canon LBP-1120 A4/A5/B5`), not via driver flags.
- The XP VM must be **running and logged in** (can be headless + autologin).
