# Canon Print Bridge

Print to the **Canon LASER SHOT LBP-1120** from **Windows 11 (x64)**.

The LBP-1120 is a host-based **CAPT** printer with no 64-bit driver, so it cannot print from modern Windows directly. This project makes printing "almost direct": a WPF app on Windows 11 lets you pick a file and parameters, and the actual printing happens automatically inside a **Windows XP** guest running in **VirtualBox** (with the USB printer passed through). The two sides talk through a simple file-based queue in a shared folder.

```
[Windows 11]  Canon Print Bridge (WPF, .NET 8)
   • pick a file (PDF / Word / image / text) + parameters
   • non-PDF is converted to PDF on the host, then queued
   • Print ──► writes <id>.pdf + <id>.job.json to the shared Queue folder
   • shows readiness + status ◄── reads status\<id>.status.json and a heartbeat
                                │  (shared folder)
[Windows XP VM]  watcher.vbs (auto-started)
   • sees job.json ──► selects the print queue by paper size
   • SumatraPDF prints to the Canon (copies / scale / page range)
   • writes status.json and a health heartbeat
```

## Features

- **Input formats** — PDF, Word (`.docx/.doc/.rtf`), images (`png/jpg/bmp/gif/tif`), plain text (`.txt/.log/.csv/.md`). Non-PDF is converted to PDF on the Windows 11 side before printing; the queue stays PDF-only.
- **Live preview** (WebView2) with **page selection** — preview only the pages you chose (Windows-style ranges like `1,3,5-8`).
- **Readiness indicators** — VM, XP guest, and printer status, updated from a heartbeat the guest writes; **Print** is enabled only when everything is ready.
- **VM control** — start the VM from the app; graceful shutdown (ACPI, then forced power-off) that also closes the VirtualBox window.
- **Bilingual UI** — Russian / English, switchable at runtime.
- Real print-completion reporting (waits for the spooler to drain, not just for the converter to exit).

## Requirements (target machine)

- **VirtualBox** + the matching **Extension Pack** (the Extension Pack provides the USB passthrough the printer needs).
- The pre-configured **Windows XP appliance** (`Microelectronics.ova`) — distributed separately (Windows XP licensing + size), not part of this repository.
- **WebView2 Runtime** — for the preview pane (already present on most Windows 11 installs).
- **Microsoft Office** — optional, only for converting Word documents (via the bundled `OfficeToPDF.exe`). PDF, images and text work without it.

The published app is **self-contained**, so the .NET runtime does not need to be installed.

## Install

1. **App** — download `Setup PrintBridge <version>.exe` from the [Releases](../../releases) page and run it. The wizard lets you choose the install folder, the print-queue path, the `VBoxManage.exe` path and the UI language, and writes them into `appsettings.json`.
2. **VirtualBox + VM** — install VirtualBox and the Extension Pack, enable VT-x/AMD-V, then import the `Microelectronics.ova` appliance. (Setup guide provided alongside the appliance.)

## Build from source

```powershell
# requires the .NET 8 SDK
dotnet build CanonPrintBridge/CanonPrintBridge.sln -c Release

# portable, self-contained single-file bundle:
dotnet publish CanonPrintBridge/src/CanonPrintBridge -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

CI builds the same bundle and the Inno Setup installer on every push; version tags (`v*`) attach both to a GitHub Release.

## Usage

1. **Start printer (VM)** — brings up the XP guest and passes through the USB printer.
2. Drag a file into the window (or **Open…**). Non-PDF is converted automatically.
3. Set parameters (copies, scale, page range) and press **Print**.
4. Watch the readiness row and the log; **Done** appears after the page actually prints.

## Layout

| Path | What |
|---|---|
| `CanonPrintBridge/src/CanonPrintBridge/` | WPF application (Windows 11 side) |
| `CanonPrintBridge/xp-watcher/` | `watcher.vbs` + one-time XP setup notes |
| `externals/OfficeToPDF/` | bundled Word→PDF CLI (Apache-2.0) |
| `installer/PrintBridge.iss` | Inno Setup installer script |
| `Printer_Canon_lbp_1120/` | VM launcher (`Print-Canon.ps1`) |

## Hardware limitations (LBP-1120)

- **Black & white only** — the printer is monochrome.
- **Manual duplex only** — no duplex unit.
- **Paper size via separate XP queues** (`Canon LBP-1120 A4`), not driver flags.

## License

[The Unlicense](LICENSE) (public domain). Bundled third-party components keep their own licenses (e.g. `externals/OfficeToPDF` is Apache-2.0).
