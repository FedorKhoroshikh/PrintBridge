# XP-side setup (one time)

The `watcher.vbs` watcher lives inside the guest XP and actually prints the jobs.
Below is the one-time setup.

## 1. SumatraPDF (XP-compatible version)

The LBP-1120 is printed to from the command line via SumatraPDF.

- You need **version 3.1.2** — the last one that works on Windows XP.
- Direct link (official archive):
  `https://www.sumatrapdfreader.org/dl/rel/3.1.2/SumatraPDF-3.1.2-install.exe`
- Install to the default path: `C:\Program Files\SumatraPDF\SumatraPDF.exe`
  (if otherwise — adjust `Const SUMATRA` at the top of `watcher.vbs`).

Check from the XP command line:
```
"C:\Program Files\SumatraPDF\SumatraPDF.exe" -print-to "Canon LBP-1120 A4" -silent "\\vboxsvr\Shared\Queue\printed\любой.pdf"
```

## 2. Print queues per paper size

The paper size is set via **a separate queue for each paper size** (more reliable than flags).
The CAPT driver is already installed; we add it once more under different names.

For each paper size (**A4**, **A5**, **B5**):

1. Start → Printers and Faxes → **Add Printer**.
2. Local printer, **uncheck** "Automatically detect". Port — the same **USB001** (the one the Canon already works on).
3. Driver — **Canon LASER SHOT LBP-1120** (from the list / "Have Disk" on the same inf).
4. When asked about the existing driver — **"Keep existing driver"**.
5. Printer name — exactly:
   - `Canon LBP-1120 A4`
   - `Canon LBP-1120 A5`
   - `Canon LBP-1120 B5`
6. Sharing is not needed. Test print — optional.
7. After creation: right-click the queue → **Printing Preferences** → set its **paper size**
   (A4 / A5 / B5 respectively) → OK. This becomes the queue's default.

> At minimum — create only `Canon LBP-1120 A4` if the other paper sizes are not needed.
> The names must match `PRINTER_BASE & " " & paper` from `watcher.vbs`.

## 3. Watcher in startup

1. Copy `watcher.vbs` inside XP, e.g. to `C:\CanonBridge\watcher.vbs`.
2. Check the constants at the top of the file (`QUEUE_DIR`, `SUMATRA`, `PRINTER_BASE`).
3. Create a shortcut that launches it **without a window** via `wscript`:
   - Shortcut target: `wscript.exe "C:\CanonBridge\watcher.vbs"`
4. Put the shortcut into startup:
   `C:\Documents and Settings\<user>\Главное меню\Программы\Автозагрузка`
   (or Start → All Programs → right-click "Startup" → Open).

## 4. XP autologin (so startup runs without a password)

Startup only runs after logon. To make XP log in by itself:

- Start → Run → `control userpasswords2` → uncheck
  "Users must enter a user name and password" → OK → specify the user/password.

## 5. Check

1. Start the VM (via the «Запустить принтер (VM)» button in the app or the launcher).
2. Inside XP, make sure the `wscript.exe` process is running (Task Manager).
3. On Win11, in the app pick a PDF, paper size A4 → **Печать**.
4. In the app log the statuses should go through `printing → done`, and a sheet comes out of the printer.

## Notes

- **Duplex — manual only**: it prints the odd pages, waits for the stack to be flipped,
  prints the even pages. The LBP-1120 has no duplex module.
- The watcher does not write separate logs; the diagnostic signal is `status\<id>.status.json`
  (which the app reads and displays).
- If a job is "stuck" in `awaiting-flip` — the app will show a dialog; click OK
  after flipping. The wait timeout is 5 minutes (`FLIP_TIMEOUT_S`).
