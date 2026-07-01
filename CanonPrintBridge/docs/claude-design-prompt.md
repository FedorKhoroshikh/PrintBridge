# Prompt for claude.ai/design

Paste into a new Claude Design project **together with two attachments**:

1. **`design-brief.md`** — the full brief (requirements, constraints, open decisions).
2. **A screenshot of the current draft prototype** (`new-ui.png`) —
   **only as a reference of the current state**; the layout can and should be improved/reworked.

> Why this way rather than a pixel-perfect mockup: the more detailed and prescriptive the sketch, the more it
> "anchors" the tool and it simply repeats our amateur vision. We give **content and
> constraints**, and leave the layout to good design patterns.

---

## Prompt text (copy below)

Design the interface of a desktop application for **Windows 11**. It's a utility for printing to an old
printer through a background virtual machine. All requirements, content inventory, states and
constraints are in the attached **`design-brief.md`**; read it in full.

**Output format (important):**
- This is **not a web landing page** but **a single resizable desktop window** for Windows 11 in the **Fluent** style.
  The density and patterns should be those of a native desktop utility, not a website.
- The result will be ported into **WPF/XAML**, so propose layouts expressible with standard
  desktop controls: `Grid`/`DockPanel`, `GridSplitter`, `Expander`, `TabControl`/pages,
  `ComboBox`, buttons, `ProgressBar`, circle status indicators, popup panels/separate windows.
- The UI language is **Russian**. The primary theme is light Fluent (dark — optional).

**Approach:**
- **Do not copy** the attached draft screenshot — it's only the current rough vision. Decide **yourself, by
  good design principles**, what goes where, which controls to use, what to move
  into additional windows/pages, what to make collapsible.
- Work **from the content and states** (brief §2–§4): file selection; three status indicators
  (VM / OS image / Printer) with states ok/loading/off/lost; actions (start the
  printer, print, shut down); parameters (paper size — locked to A4, copies, scale,
  pages); progress + timer; log; PDF preview; settings screen.
- **Open decisions (brief §6) — up to you:** one form or navigation; where and how to show
  the preview; where to hide the "advanced" stuff and the log; how to lay out the settings; modals vs
  inline banners; where the indicators live.
- Where appropriate — use **icons** (Segoe Fluent Icons) in actions (print / shutdown /
  open / settings / preview) and in statuses. The app already has a window icon.

**Constraints (from the brief, to be observed):** black & white only; paper size only A4 (for now); manual duplex
printing is not in the UI; the VM runs in the background (we don't manage the VM window — only indicators); «Готово» — only
upon the fact of print completion; preview — through a ready built-in viewer (not our own rendering).

**What I want as output:**
1. Main window — 1–2 layout variants (states "preview hidden" and "preview open").
2. Settings screen/dialog.
3. State storyboard: indicators (ok / loading / off / lost); «Печать» disabled
   with a reason tooltip; printing in progress (progress + timer); done; error; empty state.
4. A short **rationale** for the key decisions (why this structure, where the preview, where the log).
