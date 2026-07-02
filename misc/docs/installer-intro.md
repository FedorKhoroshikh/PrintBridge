# Windows installer — intro / overview

A short overview for independent study, in the same spirit as `claude-design-intro.md`.
Marks what specifically matters for our case (Canon Print Bridge).

---

## 1. What an installer is

A program that **puts an app onto a target machine in a repeatable, reversible way**:
copies files to a chosen folder, creates Start Menu / Desktop shortcuts, writes
configuration and (optionally) registry entries, registers an **uninstaller**, and
can run **custom steps** (create folders, write config, check prerequisites).

It replaces a manual "copy these files, edit this path, create that folder" guide with
**one `setup.exe`** the user double-clicks. And an **uninstall** entry in
"Apps & features" that cleanly removes everything.

## 2. What it is made of

- **Engine / runtime** — the built-in logic that drives the wizard, copies files,
  rolls back on failure, and writes the uninstaller. You don't write this; the tool
  provides it.
- **Script** — *you* write this. Declares metadata (name, version, publisher, icon),
  the file payload, shortcuts, wizard pages, and custom code. In Inno Setup it's a
  `.iss` file with sections: `[Setup] [Files] [Icons] [Run] [Code]`.
- **Payload** — the actual bytes to install: our published app bundle
  (`CanonPrintBridge.exe`, `OfficeToPDF.exe`, `Print-Canon.ps1`, `Resources/`,
  `appsettings.json`). The installer **embeds** them (compressed) into `setup.exe`.
- **Wizard pages (UI)** — the sequence of screens: Welcome, License, install folder,
  options/components, Ready, progress, Finish. Standard and mostly templated.
- **Custom code** — hooks that run at install/uninstall time (e.g. write chosen
  language + paths into `appsettings.json`, create the queue folder, detect VirtualBox).
- **Uninstaller** — auto-generated; the engine records what it installed and can undo it.

## 3. How it works (two phases)

**Build time (developer machine, or CI):**
`your script + payload  →  compiler  →  setup.exe`
For Inno Setup the compiler is `ISCC.exe`. Output is one self-contained `setup.exe`.

**Install time (user machine):**
`setup.exe` → wizard collects choices (folder, language, paths) → extracts payload →
writes shortcuts + config + uninstaller → done. Everything is transacted: on error it
rolls back. Uninstall reverses it.

## 4. Tooling options

- **Inno Setup** — free, script-based (`.iss`), single `setup.exe`, mature, lightweight,
  easy custom pages via Pascal `[Code]`. **← recommended for us.**
- **WiX Toolset** — produces **MSI** (enterprise-grade, Group Policy deploy, very
  powerful) but XML-heavy and a steep learning curve. Overkill for a household tool.
- **MSIX** — modern Store-style packaging with sandboxing; the sandbox **fights** our
  needs (shelling out to `OfficeToPDF.exe`, driving `VBoxManage`, arbitrary shared
  folders). Poor fit.
- **Custom bootstrapper** (e.g. a WPF installer) — full visual control, but you rebuild
  the whole install/rollback/uninstall machinery by hand. Not worth it.

## 5. What OUR installer would do

1. **Welcome + License** (Unlicense) pages.
2. **Install folder** — default `C:\Program Files\Canon Print Bridge`, user can change.
3. **Language** page — RU / EN → written as `Language` in `appsettings.json`.
4. **Paths** (auto with sane defaults, editable):
   - `QueueRoot` (default `C:\Virtualization\Shared\Queue`; the folder is **created**),
   - `VBoxManagePath` (**auto-detected**; prompt only if missing),
   - `OfficeToPdfPath` — fixed relative to the install folder (bundled).
5. **Prerequisite checks** (soft warnings, non-blocking): VirtualBox present?
   Extension Pack? Office (for `.docx`)? Without them, PDF/images/text still work.
6. **Shortcuts** — Start Menu + optional Desktop, using our multi-size icon.
7. **Uninstall** — removes files/shortcuts; asks whether to also delete the queue
   folder and `appsettings.json`.
8. *(Optional)* **"Import VM"** step — point at a `Microelectronics.ova` and run
   `VBoxManage import`. The OVA itself ships out-of-band (Windows XP licensing + size).

## 6. UI & branding — where design fits

The wizard is a **standard, expected flow**; users trust the convention. A full custom
UI (Claude Design) is **overhead** and has no clean handoff (Claude Design emits web
code, not Inno wizard pages). The high-ROI brand touch is small and cheap:

- `WizardStyle=modern`, our app **icon** on `setup.exe` and shortcuts;
- a custom **left banner bitmap** (164×314) and **small header** (55×58) — *this* is the
  only piece worth generating in Claude Design if a brand accent is wanted;
- correct product name/version and clean RU/EN wording.

## 7. Build & CI

- Local: install Inno Setup, run `ISCC.exe CanonPrintBridge.iss` → `setup.exe`.
- CI (optional): a Windows job `choco install innosetup` → publish the app → compile the
  `.iss` → attach `setup.exe` as a release asset (alongside the raw zip). Needs internet
  on the runner for choco.

## 8. What the installer does NOT do

- It **installs the app, not the VM.** The Windows XP VM (`Microelectronics.ova`) is a
  separate, out-of-band artifact (licensing + ~GBs). The installer can *optionally* help
  import it, but doesn't carry it.
- It doesn't install VirtualBox / the Extension Pack / Office — only checks and warns.

## 9. Caveats

- Inno Setup is Windows-only (fine — the app is Windows-only).
- Writing to `C:\Program Files` needs elevation (`PrivilegesRequired=admin`); the app
  then writes `appsettings.json` next to itself — so either store config in the install
  dir with admin, or in `%ProgramData%` / `%LocalAppData%` to avoid re-elevation.
  → decision to make when we build it.
- Code-signing: unsigned `setup.exe` triggers SmartScreen "unknown publisher". For a
  household tool that's acceptable; a cert removes it later.

---

## Sources

- [Inno Setup — official site & docs](https://jrsoftware.org/isinfo.php)
- [Inno Setup Help — Script sections](https://jrsoftware.org/ishelp/)
- [WiX Toolset](https://wixtoolset.org/)
- [MSIX overview — Microsoft Learn](https://learn.microsoft.com/windows/msix/overview)
