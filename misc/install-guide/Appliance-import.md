# Appliance import (Windows XP print guest)

Part 2 of the manual VM track (after `VirtualBox-setup.md`). Imports the pre-configured Windows XP VM (`Microelectronics.ova`) — watcher, SumatraPDF, print queue, autologin and the USB filter all travel inside the image.

> The OVA is **not** in the public repository (Windows XP licensing + size). It is distributed privately.

---

## 0. Where the OVA comes from

**Producing it (author, once):** power the VM off, then export it — `VBoxManage export Microelectronics -o Microelectronics.ova --ovf20` (or GUI: **File → Export Appliance → OVF 2.0**). One `Microelectronics.ova` file.

**Getting it (deployer):** download `Microelectronics.ova` from the shared Google Drive folder — <https://drive.google.com/drive/folders/1jXaIFZQi8pWcPw98zz9iFhFckAuUXkUS?usp=sharing> — then copy it to the target machine. (Access is limited to this folder; keep the link private — it carries a Windows XP image.)

## 1. Import the appliance

VirtualBox → **File → Import Appliance…** → select `Microelectronics.ova` → **Next**.

Review the summary. Notes:

- Keep the name **`Microelectronics`** (the app looks for this VM name by default; if you change it, update `VmName` in the app's `appsettings.json` / Settings).
- **MAC address policy:** "Generate new MAC addresses for all adapters" is fine.
- If you moved the default machine folder (Part 1 §4), the disk lands there.

Click **Import** and accept the licence prompt.

## 2. Shared folder (host ↔ guest)

The image expects a shared folder mapped to the host print-queue path. Default:

- Host: `C:\Virtualization\Shared\Queue`  ↔  Guest: `\\vboxsvr\Shared\Queue`

**Create the host folder** if it doesn't exist (the app installer also creates its `QueueRoot`; keep the two the same). Then check the VM's shared-folder mapping: **Machine → Settings → Shared Folders** — the entry should point at your host queue folder. If the path differs on this machine, edit it to match, or repoint the app's `QueueRoot` to wherever the mapping points.

## 3. USB printer passthrough

The OVA carries a USB filter **"Canon CAPT"** (VendorId `04a9`, ProductId `262b`). With the Extension Pack installed (Part 1 §3), the printer is captured automatically when the VM starts and you power on the printer. Verify under **Machine → Settings → USB**: USB controller enabled + the Canon filter present.

## 4. First boot

Start the VM (from the app's launcher, or **Start** in VirtualBox Manager). Expect:

- XP boots (~30–60 s) and **auto-logs in** (user `User`).
- The watcher (`C:\CanonBridge\watcher.vbs`) **auto-starts** from the Startup folder and begins writing `status\bridge.health.json` into the shared queue every ~2 s.
- With the printer powered on, it attaches via the USB filter.

## 5. Verify end-to-end

- On the **host**, `C:\Virtualization\Shared\Queue\status\bridge.health.json` appears and its modified-time refreshes every couple of seconds.
- In the **app**, the readiness row turns green: **Virtual machine → OS image → Printer**. Only then is **Print** enabled.
- Send a test PDF; it should print and the app should report **Done** after the page actually comes out.

## Caveats

- The **guest clock is unreliable** — judge freshness by the host file's mtime, not by timestamps written inside XP.
- **B&W only**, manual duplex only, A4 queue (`Canon LBP-1120 A4`).
- If readiness stays on "Guest booting…": the watcher isn't running or the shared folder path doesn't match — re-check §2 and that `C:\CanonBridge\watcher.vbs` is in the Startup folder.
