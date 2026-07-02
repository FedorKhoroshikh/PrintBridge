# UI/UX specification — Canon Print Bridge window redesign

This document captures the **final concrete UI details** — filled in after the design work.
The non-prescriptive inputs are in `design-brief.md`; the choice of layout patterns is handed
to Claude Design. Sections §2–§8 below are the **owner's original draft mockup** (a candidate);
they will be rewritten to match the decisions chosen from the references. The hard
requirements remain in force: §4 (indicators), §7 (print completion), §8 (preview through a ready-made viewer),
§9 (look&feel). The implementation follows `implementation-plan.md`.

## 1. Purpose and context

A single-window WPF application (Win11). There is a single user (Fedor), a domestic scenario:
occasionally print a PDF to the old Canon LBP-1120 through the XP VM. So: minimal steps,
large targets, honest status indicators, no modal thickets.

Target look&feel: clean, light, "Fluent-ish" for Win11 — airy, calm
typography (Segoe UI), a single accent color for the main action, statuses as
colored dots. No visual noise or extra frames.

## 2. Layout

The window is a **split container** (two columns, the divider is a `GridSplitter`):

```
┌───────────────────────────┬──────────────────────────┐
│ ЛЕВАЯ КОЛОНКА (управление) │ ПРАВАЯ КОЛОНКА (Preview)  │
│                     [⧉] ◄──┼── кнопка show/hide preview│
│  ┌─ зона выбора файла ─┐   │                          │
│  │ Выберите файл или   │   │                          │
│  │ перетащите (drop)   │   │      PDF preview         │
│  └─────────────────────┘   │   (встроенный вьюер)     │
│                            │                          │
│  ● Виртуальная машина      │                          │
│  ● Образ ОС (Windows XP)   │                          │
│  ● Принтер <name>          │                          │
│                            │                          │
│ [Запустить принтер] [Печать]                          │
│  ┌─ Параметры ─────────┐   │                          │
│  │ Формат / Копии / …  │   │                          │
│  └─────────────────────┘   │                          │
│  ▓▓▓░░░░░  Готово за 0:01   │                          │
│  ┌─ Журнал ────────────┐   │                          │
│  └─────────────────────┘   │                          │
└───────────────────────────┴──────────────────────────┘
```

- By default the right panel is **hidden**, the window is narrow (as it is now, ~470px).
- The icon button in the top-right corner of the left column expands/collapses the Preview;
  when expanding, the window widens, and the `GridSplitter` lets you change the proportion.
- The left column from top to bottom: file zone → status indicators → action buttons →
  Parameters → progress row → Log (as in the current window, plus the indicators and the preview button).

## 3. File selection zone

- A large dashed drop zone with the text «Выберите файл или перетащите» (Select a file or drag it here).
- Clicking the zone = open the selection dialog (equivalent to the current «Обзор…» (Browse…)).
- Drag&drop of a PDF — as it is now. After selection: show the file name (and, if the preview is open,
  render the document on the right).
- Validation as it is now: only an existing `*.pdf`.

## 4. Status indicators (three dots)

Three independent signals, each — a colored dot + label. Polling is on a timer
(polling, ~2–3 s). Colors: **green** = ok, **yellow** = transitional/loading,
**grey** = unknown/off, **red** = was ok and dropped out / error.

| Indicator | Green (ok) | Yellow | Grey / Red |
|---|---|---|---|
| **Virtual Machine** | `VBoxManage list runningvms` contains `Microelectronics` | — | grey: not running; red: was running and disappeared |
| **OS image (Windows XP)** | the watcher heartbeat is fresh (XP booted, logged in, watcher alive) | VM running, but the heartbeat has not appeared yet (booting) | grey: VM not running; red: heartbeat went stale (XP/watcher crashed) |
| **Printer `<name>`** | the watcher sees the printer online (WMI) | — | grey: unknown; red: printer dropped out (USB lost / offline) |

Key requirements (from the statement of work):
- **(item 3)** "OS image" is green only when XP has **actually** booted and the watcher is alive —
  not while the VM is booting. The source is the heartbeat, not the fact that "VM running".
- **(item 7)** If the VM/XP were killed by hand — the indicators go red, "Print" is blocked.
- **(item 2)** "Print" is **disabled** while the printer is not active (all three print conditions not green).

The "Print" availability condition = VM running **and** heartbeat fresh **and** printer online
**and** a PDF selected. Otherwise — the button is disabled with a hint tooltip for what is missing.

Technically the heartbeat is a file that the watcher rewrites every cycle; freshness is measured
by **mtime on the host** (the XP clock is off). The heartbeat contract is in `implementation-plan.md`
(the section about `bridge.health.json`) and, after implementation, moves to `job-contract.md`.

## 5. Action buttons

- **«Запустить принтер (VM)» (Start printer (VM))** — brings the VM up **in the background (headless)**, no windows appear.
  During boot the indicators reflect progress (VM yellow → green, OS grey → yellow → green).
- **«Печать» (Print)** — available only when ready (see §4). Shows the progress and timer.
- **«Завершить работу» (Shut down)** — properly powers off the background VM/XP (ACPI shutdown) and resets the UI
  to its initial state: preview closed, indicators grey, the operation reset.

## 6. Print parameters

The current set: Paper size, Copies, Scale, Pages, Manual duplex.

- **(item 5)** Anything non-working/unverified — **disable** (do not remove), with an explanatory tooltip:
  - **Paper size** — lock to `A4` (in XP there is only the `Canon LBP-1120 A4` queue).
    Unlock when the A5/B5 queues have been created. Tooltip: «A5/B5 — после создания очередей в XP» (A5/B5 — after creating the queues in XP).
  - Copies, Scale, Pages — **work**, keep active.
  - **Manual duplex — removed from the UI** (owner's decision); we keep the duplex logic in the watcher
    for the future, we just don't show it.
- **Pages** we keep; placement (inline vs "Advanced") — per the design outcome.

## 7. Progress row and timer

As in the current version: an indeterminate `ProgressBar` + text on the right
(`Печать… 0:03` during, `Готово за 0:03` on completion).

**(item 4)** "Done" should appear when the printer has **actually** finished printing, not when
SumatraPDF handed the job to the spooler. The source of truth is the watcher: it waits for the
XP spooler queue to drain and only then writes `done`. The app shows "Done" upon `done`.

## 8. Preview (right panel)

- **(item 1, item 6)** Do not build a custom PDF rendering — reuse a **ready-made viewer**.
  The chosen approach: **WebView2** (Edge/Chromium's built-in PDF viewer) pointed
  at a local file. Gives scrolling/zoom/pages for free.
- The panel is the right part of the split container, expanded by an icon button (§2).
- When the selected file changes, the preview is re-rendered. The empty state — a "Preview" placeholder.
- Fallback: if there is no WebView2 runtime — a "Open in external viewer" button (shell-open).

## 9. Look&feel (inputs for design)

- **Font:** Segoe UI (UI), Consolas (log, timer, technical captions).
- **Accent:** a single color for "Print" (for example, the Win11 blue `#0067C0`), the other buttons neutral.
- **Status dots:** green `#2FA84F`, yellow `#E5B72B`, grey `#B8B8B8`, red `#D64545`.
- **Spacing:** generous (16px outer, 8–10px between blocks), a large drop zone.
- **Borders:** minimal; GroupBox can be replaced with light divider headings.
- **States:** every interactive element has an honest disabled look + a reason tooltip.
- **App icon:** `printer-xp-icon.ico` (embedded), in the window title and on the taskbar.

## 10. On design development with the "Claude Design" tools

Claude's generative design tools are web-oriented (HTML/CSS/React) and do not
map onto WPF/XAML — their output would have to be rewritten by hand. Hence the recommendation:
**a separate design tool is not needed here**; this specification (§2–§9) is itself the "inputs
file". We implement directly in XAML with a tidy `ResourceDictionary` (styles, colors, brushes).
If quick visual variants are desired — you can sketch static mockups, but the "design → code"
pipeline gives no benefit for desktop.
