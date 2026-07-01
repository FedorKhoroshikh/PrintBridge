# Status and plans — Canon Print Bridge

Last updated: **2026-07-01**.

## Current status: WORKING ✅

The end-to-end autonomous pipeline is confirmed on real paper, including the "cold" cycle
(full XP shutdown → start → autologin → watcher auto-start → USB auto-capture → print).

What is confirmed:
- **Win11 → XP → CAPT** printing runs fully automatically: pick a PDF + parameters → «Печать» → sheet.
- The VirtualBox **USB filter** captures the printer itself when the VM starts (no manual selection in the USB menu needed).
- The **watcher** (`watcher.vbs`) auto-starts in XP, **autologin** under `User` works.
- The **launcher** «Запустить принтер (VM)» ("Start printer (VM)") brings up the VM (fixed, see below).
- The app has a **progress bar + timer** («Печать… 0:03» ("Printing… 0:03") → «Готово за 0:03» ("Done in 0:03")).

## Actual environment (host + XP), outside the code

| What | Value |
|---|---|
| Code / solution | `T:\Dev\CanonPrintBridge\` (`.sln`, .NET 8 `net8.0-windows`) |
| Resulting exe | `src\CanonPrintBridge\bin\Release\net8.0-windows\CanonPrintBridge.exe` |
| Queue (host) | `C:\Virtualization\Shared\Queue\` |
| Queue (guest) | `\\vboxsvr\Shared\Queue` |
| Setup artifacts | `C:\Virtualization\Shared\_setup\` (Sumatra 3.1.2, watcher.vbs, shortcut, test.pdf) |
| VM | `Microelectronics` (Windows XP) |
| VBoxManage | `C:\Program Files\Oracle\VirtualBox\VBoxManage.exe` |
| USB filter | "Canon CAPT", VendorId `04a9`, ProductId `262b` |
| XP print queue | `Canon LBP-1120 A4` (A4 by default; A5/B5 not created yet) |
| Watcher in XP | `C:\CanonBridge\watcher.vbs` + shortcut in the Startup folder of the `User` profile |
| Launcher | `Printer_Canon_lbp_1120\Print-Canon.ps1` (in the repo root; copied to output next to the exe, `LauncherPath` is relative) |

## Gotchas, already fixed (do not repeat)

- **Launcher path**: the config had a typo `Printer_Canon_ltp_1120` → correct is `lbp`. Fixed in `appsettings.json` and `AppConfig.cs`.
- **Launcher failed silently** (PowerShell 5.1):
  - Cyrillic in `.ps1` without a BOM breaks the parser (read as ANSI/1251) → the launcher was made **ASCII-only**;
  - `$ErrorActionPreference='Stop'` turned stderr from VBoxManage into a terminating error before `startvm` → now `'Continue'` + `2>$null`.

---

## Plans (later)

### 1. Self-contained / portable "out of the box" solution
Goal: move the **application + VM `Microelectronics`** to another machine and run the same
printer, plugged into the new machine, **without extra setup**.

What already travels "inside" the VM (in the image): SumatraPDF, the `Canon LBP-1120 A4` queue,
the watcher in startup, autologin, the USB filter (it lives in the VM settings). The new machine
does NOT need the CAPT driver on the host — it lives in the guest; a printer of the same model
gets captured by the filter via 04a9:262b.

What is machine-dependent and needs solving:
- **VM image**: export to `.ova` (`VBoxManage export Microelectronics -o ...`) for transfer.
- **Host path of the shared folder** (`C:\Virtualization\Shared`) is set in the VM settings and in
  `appsettings.json` → on the new machine either recreate the same path or parameterize it.
  The guest side (`\\vboxsvr\Shared\Queue`) is stable — it depends only on the `Shared` share name.
- **Config paths** (`QueueRoot`, `LauncherPath`) — make relative / auto-detected
  or defer to first run (a setup wizard).
- **Bootstrap**: a small installer/script that on the new machine installs VirtualBox (or
  checks it), imports the `.ova`, creates the shared folder, sets the paths. Then it's "out of the box".

### 2. Single exe (single-file)
- Build `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`
  (+ `-p:IncludeNativeLibrariesForSelfExtract=true`). Also offer a framework-dependent option (smaller size).
- Account for `appsettings.json`: either embed defaults (already in `AppConfig`) or place it alongside.

### 3. Nicer UI
- Ideas: drag-drop with a large zone, filename preview, a "VM ready" indicator (via a heartbeat
  from the watcher — the watcher writes `Queue\status\watcher.alive`, the app reads it and toggles the
  print button's readiness precisely, rather than by timer).
- Possibly: job history, repeat last, selecting multiple PDFs, remembering parameters.
- Possibly: app icon, minimize to tray, auto-start the VM when the app launches.

### Other (minor)
- The guest XP clock is off (`updatedAt` in statuses shows past dates) — sync the
  time in XP if you want honest timestamps. Does not affect operation.
- The `Canon LBP-1120 A5` / `B5` queues — create them in XP if needed (see `xp-watcher/README-XP-setup.md`).
