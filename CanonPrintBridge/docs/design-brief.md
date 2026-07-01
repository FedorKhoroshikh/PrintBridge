# Design brief — Canon Print Bridge (inputs for UI development)

This is the **input for generative UI development** (Claude Design). The tool's job is to
propose good layout and interaction patterns based on design principles, rather than
reproducing the owner's raw sketch. The prototypes are then downloaded and used as
**visual references**; the final implementation is in WPF/XAML.

> The final concrete details (after the references) are captured in `ui-spec.md`.
> The file contracts are in `job-contract.md`. The work plan is in `implementation-plan.md`.

---

## 0. How to use this brief (important for the designer)

- This is a **desktop application for Windows 11**, not web. Build the prototypes as
  **a single resizable window** of native density (compact utility spacing, not a
  web landing page). The mockups will be ported to **WPF/XAML**, so propose layouts
  expressible with standard desktop controls.
- Think in **WPF controls / Designer Toolbox**: `Window`, `Grid`/`DockPanel`/`StackPanel`,
  `GridSplitter`, `Expander`, `TabControl` or `Frame`+`Page` (if navigation is needed),
  `GroupBox`, `ComboBox`, `TextBox`, `CheckBox`, `Button`, `ProgressBar`, `TextBlock`,
  `Ellipse`/`Border` (indicators), `ScrollViewer`, `Popup`/a separate `Window` (extra windows),
  `ContextMenu`, `ToolTip`. **Icons — Segoe Fluent Icons**; appropriate in actions (print,
  shutdown, open, settings, show preview) and in statuses — use them where they
  improve clarity. The app already has a window icon (`printer-xp-icon.ico`).
- **Theme:** the reference point is **Fluent / Windows 11** (the **WPF-UI** library is planned).
  That means these are available: NavigationView, Card/InfoBar, modern buttons/fields, accordions,
  a Mica-style title bar. Use these patterns where they fit.
- **The interface language is Russian.** The light theme is primary; dark is optional.
- **1–2 variants** of the main window are welcome (for example, "preview collapsed" and "expanded"),
  plus a settings screen and a storyboard of states. Include a short rationale of the pattern for each decision.

---

## 1. Context and user

The utility prints PDFs to an old **Canon LASER SHOT LBP-1120** (host-based CAPT, no
x64 driver). The actual printing happens in a guest **Windows XP** (VirtualBox, USB passthrough);
the app on Win11 places a job into a file-based queue, and the watcher in XP prints and writes status.

There is a single user, and the scenario is domestic and **rare**: occasionally print one PDF.
Hence the UX goals: minimal steps to print, **honest status indicators**, low
cognitive load, no unnecessary modal thickets. The error "pressed Print, but the VM
isn't up" must be impossible by construction.

The VM starts **in the background (headless)** — the VirtualBox windows and the XP desktop are not
shown; the UI merely reflects state and controls starting/stopping the background VM
(so there is no need to manage the VM window, only the indicators).

---

## 2. Functional inventory (what must be available in the UI)

The designer decides **where and how** to place this; the list is what exists at all.

1. **File selection** — a drop zone + selection via a dialog. Only `*.pdf`.
2. **Three status indicators** (see §3): Virtual Machine / OS image (Windows XP) / Printer.
3. **Actions:** «Запустить принтер (VM)» (Start printer (VM)), «Печать» (Print), «Завершить работу» (Shut down) — the last
   one properly powers off the background VM/XP and **resets the UI to its initial state** (preview closed,
   indicators grey, the operation reset).
4. **Print parameters:**
   - **Paper size** — locked to `A4` (in XP there is only the `Canon LBP-1120 A4` queue;
     A5/B5 will appear later) → show as disabled/locked with an explanation.
   - **Copies** — an integer ≥ 1.
   - **Scale** — fit to page / as is / shrink if necessary.
   - **Pages** — "empty = all", or `1-4,7`.
   - *(There is NO manual duplex printing in the UI — deliberately excluded; the backend remains.)*
5. **Progress + timer** — a busy indicator and the operation time («Печать… 0:03» (Printing… 0:03) → «Готово за 0:03» (Done in 0:03)).
6. **Log** — a log of operations (technical captions in monospace).
7. **PDF preview** — view of the selected file in an **embedded ready-made viewer** (not a custom rendering).
8. **Settings** *(we design now, implement later)* — shared folder paths, the VBoxManage path,
   the VM name, the launcher path. Needed for future portability to another machine.

---

## 3. Status indicators (hard requirement)

Three independent signals. Each has 4 states; color + label + tooltip reason.

| Indicator | Ok (green) | Booting (yellow) | Off (grey) | Lost (red) |
|---|---|---|---|---|
| **Virtual Machine** | VM running | — | not running | was running and disappeared |
| **OS image (Windows XP)** | XP booted, logged in, watcher alive | VM is coming up, but the guest is not ready yet | VM not running | guest/watcher dropped out |
| **Printer `<name>`** | printer online | — | unknown | printer lost (USB dropped / offline) |

The owner's key requirements:
- "OS image" is green only when XP has **actually** come up (not while the VM is still booting).
- If the VM/XP were killed by hand — the indicators turn red, printing is blocked.
- **"Print" is available only when ready:** VM=Ok **and** OS=Ok **and** Printer=Ok **and**
  a PDF is selected. Otherwise the button is disabled with a tooltip for what is missing.

The designer decides **the presentation form** of the indicators (a header strip, a side rail, a status bar
at the bottom, WPF-UI cards, etc.) and how to show the "Print" disabled state.

---

## 4. Print flow and shutdown (hard requirement)

- "Print" → the job goes into the queue; statuses: queued → printing → done (or error).
- **Show "done" only when it has actually happened** — the end of physical printing (the watcher waits for
  the XP spooler queue to drain), not right after sending. The progress/timer live for the whole time.
- Print errors need to be surfaced to the user (the form — a modal / `InfoBar` / banner — is a design decision).

---

## 5. Preview (a hard requirement in essence; the form is open)

- View the PDF through a **ready-made embedded viewer** (WebView2 is planned — Edge's built-in
  PDF viewer). Do **not** build a custom page rendering.
- **The designer picks the presentation form:** a right split container (`GridSplitter`), a separate
  window, a sliding flyout, a tab. Keep in mind the scenario is "one file occasionally": a permanent
  heavy panel may be excessive. A button/toggle for showing it is needed.
- Provide for the empty state (no file selected) and the loading state.

---

## 6. Open decisions — handed to the designer (the essence of the task)

This is exactly where we expect development along "the right patterns", not the owner's sketch:

1. **Structure:** everything on one form, or navigation (`TabControl` / `Frame`+`Page` /
   WPF-UI NavigationView)? For example: "Print" / "Status" / "Settings".
2. **Preview:** split vs separate window vs flyout vs tab (§5).
3. **"Advanced" parameters** (Pages): inline, in an «Дополнительно» (Advanced) `Expander`, or in the print dialog?
4. **Indicators:** where and in what form (§3).
5. **Log:** always open, collapsed by default (`Expander`), or in a separate panel?
   For a rare scenario it may be noise.
6. **Settings:** a separate dialog window, a tab/page, or a flyout? (we implement later, but
   design now — §2.8).
7. **Surfacing errors and statuses:** modal windows vs inline `InfoBar`/banners.
8. **Onboarding:** a hint at first launch / when the VM is not up.
9. **"Shut down":** a separate prominent button, a menu item, or overflow?
   The action is destructive (powers off the VM) — how to design the confirmation/safety.

---

## 7. QoL candidates (optional — the designer may propose, the owner will select)

Not required; mark them as optional:
- **Cancel print** (a "Cancel" button during a job).
- **State memory:** the last folder/parameters, the window size and position.
- **Inline banners** instead of modals for hints/errors.
- **Lifecycle:** minimizing to the tray, auto-starting the VM when the app launches.
- (Job history, multi-jobs — out of the current scope, do not propose as a mainline feature.)

---

## 8. Hardware limits (context — why some things are absent)

- **Black & white only** (the printer is monochrome) → no color options.
- **Duplex is manual only**, and it is **excluded from the UI** by the owner's decision.
- **Paper size is via separate XP queues**, not driver flags → currently only `A4`.
- Printing is possible only when the XP VM is running and logged in (hence — the indicators and gating).

---

## 9. Expected output (design deliverables)

- Main window: layout variant(s), the "preview hidden" and "preview open" states.
- A **settings** screen/dialog.
- A storyboard of states: indicators (Ok/Booting/Off/Lost), "Print" disabled with a tooltip,
  printing in progress (progress/timer), done, error, empty state.
- Light theme (dark — optional).
- A short rationale for the key patterns (why this structure / where the preview / where the log).
