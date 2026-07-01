# Implementation plan — Canon Print Bridge redesign

The problem statement is in `ui-spec.md`. Here: contracts, per-file changes, phase order, risks.
Phases are ordered by dependencies and so that each can be verified separately.

## Phase overview

| # | Phase | Why (problem-statement items) | Affects |
|---|---|---|---|
| 0 | Infrastructure (git, docs) | preparation | done in this pass |
| 1 | Heartbeat + health contract | items 2, 3, 7 (status indicators) | `watcher.vbs`, `docs/job-contract.md` |
| 2 | Real print completion | item 4 («Готово» too early) | `watcher.vbs` |
| 3 | State polling in the app | items 2, 3, 7 | new service + `MainWindow` |
| 4 | Three status indicators + gating «Печать» | items 2, 3, 7 | `MainWindow.xaml(.cs)` |
| 5 | Preview (WebView2 + split) | items 1, 6 | `csproj`, `MainWindow.xaml(.cs)` |
| 6 | Parameter gating | item 5 | `MainWindow.xaml` |
| 7 | Visual redesign (styles) | look&feel | `MainWindow.xaml`, `App.xaml` |
| 8 | App icon | impl-2 | `csproj`, `MainWindow.xaml` |
| 9 | Single-file publish | impl-1 | `csproj`, publish script |
| 10 | Headless startup + «Завершить работу» | background, teardown | launcher/`AppConfig`, `MainWindow` |
| 11 | Settings screen (design now, implementation later) | portability | new `Window` + `AppConfig` |
| 12 | Input formats → PDF (docx/images) | broaden supported inputs | `MainWindow`, new converter service, `csproj` |

Phases 1–2 (XP side) are independent of 3–8 (WPF side) and can proceed in parallel.
Recommended build-verify order: 1 → 2 → 3 → 4 → 6 → 5 → 7 → 8 → 9.

---

## Phase 1 — Heartbeat + health contract

**Idea.** The watcher rewrites a health file every cycle. The app reads it and
measures freshness by **mtime on the host** (XP's clock is unreliable).

**New contract** — `Queue\status\bridge.health.json` (written by the watcher, read by WPF):

```json
{
  "watcher": true,
  "printerPresent": true,
  "printerName": "Canon LBP-1120 A4",
  "tick": "2026-07-01 11:14:03"
}
```

- The file is rewritten atomically (`*.tmp` + rename), like the statuses.
- `printerPresent` — the watcher queries WMI: `Win32_Printer WHERE Name LIKE 'Canon LBP-1120%'`,
  considers it online if a record exists and `WorkOffline = False`.
- `tick` — for diagnostics; does not affect decisions (freshness = mtime on the host).

**Changes in `watcher.vbs`:**
- Add `WriteHealth` to the main loop (after/before scanning jobs).
- `WriteHealth` — writes `bridge.health.json` with `watcher=true` and the result of `PrinterOnline()`.
- `PrinterOnline()` — a WMI query via `GetObject("winmgmts:")`, returns True/False + name.
- Keep the write frequency ≈ once per cycle (`POLL_MS = 2000`) — enough for the app.
- Comments — in English (cscript/XP limitation, see CLAUDE.md).

**Update `docs/job-contract.md`:** add a section about `bridge.health.json`.

**Verification:** start the VM; make sure the file appears and its mtime updates every ~2 s;
unplug the USB printer in the VirtualBox menu → `printerPresent` becomes `false`.

---

## Phase 2 — Real print completion (item 4)

**Cause of the bug.** `RunPrint` = `sh.Run(cmd, 0, True)` waits for SumatraPDF to **exit**, but it
finishes after handing the job to the Windows spooler — before physical printing. `done` is written early.

**Solution.** After SumatraPDF returns, wait for the printer's spooler queue to drain, then `done`.

- New `WaitForSpoolDrain(printerName, timeoutS)`:
  - poll `Win32_PrintJob` (filtered by printer name) once every ~1–2 s;
  - exit when there are 0 jobs and 0 has held for a short "settle" (~2 s);
  - timeout (e.g. 180 s) → don't block forever (then `done`, but with a note in `message`).
- Call it after a successful `RunPrint` (for duplex — after the second pass).
- ⚠️ Caveat: for CAPT the physical output may lag the spooler drain by a couple of seconds
  (the printer is host-based, with its own status monitor). Spooler drain is the best available signal;
  record this honestly in a comment and in `job-contract.md`.

**Verification:** print a multi-page PDF — `done` appears after the last page actually comes out
(within a couple of seconds), not immediately.

---

## Phase 3 — State polling in the app

**New service** `Services/HealthService.cs`:
- `bool IsVmRunning()` — run `VBoxManage list runningvms`, look for the VM name
  (VM name and VBoxManage path — move into `AppConfig`: `VmName`, `VBoxManagePath`).
- `GuestHealth ReadHealth()` — read `bridge.health.json`; return `printerPresent`,
  `printerName` and **age by mtime on the host** (`File.GetLastWriteTime`).
- Freshness: a heartbeat is considered alive if younger than a threshold (e.g. 8 s at POLL 2 s).

**State model** (an enum per indicator): `Ok / Booting / Off / Lost`.
- VM: running → present; a transition from "was running" to "gone" while the app watches → `Lost` (red).
- OS: VM running + fresh heartbeat → `Ok`; VM running + no/stale heartbeat → `Booting`/`Lost`.
- Printer: fresh heartbeat + `printerPresent` → `Ok`; fresh + not present → `Lost`; otherwise `Off`.

Extend `AppConfig` with fields `VmName` (`Microelectronics`), `VBoxManagePath`
(`C:\Program Files\Oracle\VirtualBox\VBoxManage.exe`) with the current values as defaults.

**Verification:** unit-test the transition logic; a manual run (start/stop VM, unplug USB).

---

## Phase 4 — Status indicators + gating «Печать» (items 2, 3, 7)

- A `DispatcherTimer` (~2–3 s) calls `HealthService`, updates the three circles (color + tooltip).
- The circles — three `Ellipse` + captions in the left column (see `ui-spec.md §4`).
- `PrintButton.IsEnabled` = VM `Ok` && OS `Ok` && Printer `Ok` && a PDF is selected.
  Otherwise disabled + a tooltip with the reason (what is missing).
- Do not conflict with the existing print timer (`_tick`): this is a separate status timer,
  running the whole time the window is open.

**Verification:** kill the VM by hand while idle → the circles turn red, «Печать» goes off (item 7).

---

## Phase 5 — Preview (items 1, 6)

- Package: `Microsoft.Web.WebView2` (NuGet). Requires the WebView2 Runtime (usually present on Win11).
- Layout: a root `Grid` with three columns — left (controls, fixed min width),
  `GridSplitter`, right (`WebView2`). The right one is collapsed by default (Width=0), the window is narrow.
- An icon button (top-right of the left column) — a toggle: expands the right column and
  widens the window; clicking again collapses it.
- On `SetPdf` — if the preview is open, `webView.Source = new Uri(pdfPath)` (Edge renders the PDF).
- Fallback: no WebView2 Runtime → hide the panel, show an «Открыть во внешнем вьюере» button
  (`Process.Start` with `UseShellExecute`).
- ⚠️ For single-file (phase 9) WebView2 has native libraries — you need
  `IncludeNativeLibrariesForSelfExtract=true`; verify that the extracted loader is found.

**Verification:** pick a PDF, open the preview — the document is visible, scroll/zoom work.

---

## Phase 6 — Parameter gating (item 5)

- **Paper size:** lock the choice to `A4` (`IsEnabled=false` or leave only A4),
  tooltip "A5/B5 — after the queues are created in XP". Unlock later.
- Copies/Scale/Pages — working, keep them.
- **Manual duplex — remove from the UI** (owner's decision); do NOT delete the duplex logic
  in the watcher (`awaiting-flip` / `.continue`) — keep it for the future.
- Placement of "Pages" (inline vs an «Дополнительно» `Expander`) — per the design outcome.

---

## Phase 7 — Visual redesign (look&feel)

- **Theme:** wire up the Fluent library **WPF-UI** (Win11 look out of the box); verify
  compatibility with single-file (phase 9) and WebView2. Fallback — stock WPF + our own ResourceDictionary.
- `App.xaml`: a `ResourceDictionary` with styles (status color brushes, accent for the «Печать» button,
  typography, spacing) — per `ui-spec.md §9`.
- Re-lay out `MainWindow.xaml` into the target layout (§2): drop zone, circles, buttons,
  parameters, progress, log; replace heavy GroupBoxes with lightweight separators.
- Preserve all existing handlers and logic; change only the presentation.

---

## Phase 8 — Icon (impl-2)

- Copy/move `printer-xp-icon.ico` into `src/CanonPrintBridge/Assets/`.
- `csproj`: `<ApplicationIcon>Assets\printer-xp-icon.ico</ApplicationIcon>` (exe icon) +
  add it as a `<Resource>`; in `MainWindow.xaml` — `Icon="Assets/printer-xp-icon.ico"`
  (window/taskbar icon).

---

## Phase 9 — Single-file publish (impl-1)

- `csproj`/publish profile:
  `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`
  `-p:IncludeNativeLibrariesForSelfExtract=true -o publish`.
- Also provide a framework-dependent variant (smaller size) as an option.
- `appsettings.json`: defaults are already baked into `AppConfig` — single-file will start even without an external
  file; if you want to override — put `appsettings.json`/`appsettings.local.json` next to it.
- Add the publish command and a note about the WebView2 Runtime to `README.md`.
- **Verification:** run the resulting single exe on a clean profile; make sure the preview
  (WebView2) and VM startup work.

---

## Phase 10 — Headless startup + «Завершить работу»

**Headless start.** Currently the launcher brings the VM up `--type gui` and is invoked through a visible
PowerShell window. The goal is for **nothing to flicker**:
- Start the VM `--type headless` (VirtualBox loads XP in the background; the USB filter and watcher work
  as usual — the autologin session is there, it just isn't shown to the host).
- **Preferably:** the app invokes `VBoxManage` **directly** (a hidden process,
  `CreateNoWindow`), without an intermediate PowerShell window. Then the launcher console disappears too.
  The `Print-Canon.ps1` launcher remains as a fallback/manual path (can be switched to headless).
- Keep GUI mode as an option for debugging (a flag/setting).
- The `Print-Canon.ps1` launcher has been moved into the repository (`Printer_Canon_lbp_1120\`) and is
  **copied to the build output** next to the exe; `LauncherPath` is now relative
  (portability). Make the headless edits in this script (or replace it with a direct call).

**«Завершить работу».** A new action: cleanly power down the VM and reset the UI.
- `VBoxManage controlvm <VM> acpipowerbutton` — a soft ACPI shutdown of XP (clean);
  fallback/timeout → `savestate` or `poweroff`.
- After the stop: indicators → gray (`Off`), close the preview, reset status/timer,
  «Печать» — disabled. UI in its initial state.
- The VM name and VBoxManage path are taken from `AppConfig` (see phase 3).

**Verification:** «Запустить» → `VBoxHeadless` is in Task Manager, no windows, printing works;
«Завершить» → XP shuts down cleanly, indicators gray, UI reset.

---

## Phase 11 — Settings screen (design now, implementation later)

- A separate window/dialog (or a page, if there is navigation) for editing `AppConfig`:
  `QueueRoot`, `VBoxManagePath`, `VmName`, `LauncherPath`.
- Saving to `appsettings.json` (or `appsettings.local.json`) next to the exe.
- Needed for future portability to another machine (see `STATUS.md`).
- **Design — in this iteration; code — as a separate phase later** (owner's decision).

---

## Phase 12 — Input formats → PDF (print files that convert to PDF)

Support not only PDF but files that convert to PDF: Word `.docx/.doc`, images `.png/.jpg/…`
(optionally `.xlsx/.pptx`).

**Architecture.** Keep the queue **PDF-only** (`job-contract.md` stays unchanged). Convert on the
**Win11 host** before `SubmitAsync`: detect by extension; if the input is not PDF, produce a temp
PDF and queue that. The preview shows the converted PDF.

**Converters (driven by what is installed on this host — Office present, no LibreOffice):**
- **Office** (`.docx/.doc/.xlsx/.pptx/.rtf`): Microsoft Office COM automation —
  `Word.Application` / `Excel.Application` / `PowerPoint.Application` `ExportAsFixedFormat` to PDF.
  Run invisibly; guarantee process cleanup (no orphan `WINWORD.EXE`).
- **Images** (`.png/.jpg/.jpeg/.bmp/.tif`): render into a PDF (a lightweight library —
  PdfSharp / QuestPDF — one image per page, fit to A4). Alternative: SumatraPDF on XP prints
  images directly, but host-side conversion keeps preview + queue uniform.

**UI:** widen the file-picker filter and the drop-zone accept list; add a "Converting…" status/log
line; on failure surface a clear error.

**Verification:** print a `.docx` and a `.png` — each converts and prints; preview shows the PDF.

---

## Risks and caveats

- **CAPT output delay** (phase 2): spooler drain ≠ the moment the last page comes out;
  the discrepancy is ~seconds. Acceptable; recorded in the contract.
- **WebView2 Runtime** (phase 5): if absent — degrade to an external viewer, not a crash.
- **WMI in XP** (phases 1–2): `Win32_PrintJob`/`Win32_Printer` must be available; if not —
  a fallback for print completion: a small pause after SumatraPDF (worse, but not blocking).
- **Paths in the config** are machine-specific (the portability topic is separate, in `STATUS.md`).
- **Office dependency** (phase 12): host-side `.docx`→PDF uses Office COM (installed here); absent on
  another machine → needs a fallback converter (LibreOffice headless or a bundled lib) — portability.
- **PowerShell 5.1 / non-ASCII** — follow the rules from `CLAUDE.md` on any script edit.
