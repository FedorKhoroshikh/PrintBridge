# VirtualBox setup (host side)

Part 1 of the manual VM track. Runs **in parallel** with the app installer
(`Setup PrintBridge <version>.exe`): the installer sets up the Win11 app, these
steps prepare VirtualBox so the Windows XP print guest can run. Do this once per
machine. Part 2 is `Appliance-import.md`.

Based on the internal guide "Как получить образ виртуальной машины".

---

## 1. Download VirtualBox + the Extension Pack

Go to <https://www.virtualbox.org/> → **Downloads**, and get **both**:

- **VirtualBox platform package** for **Windows hosts** (the installer `.exe`).
- **VirtualBox Extension Pack** (`Oracle_VM_VirtualBox_Extension_Pack-*.vbox-extpack`).
  Download the version that **matches** the platform package.

> The Extension Pack is **required** — it provides USB 2.0/3.0 passthrough, which is
> how the Canon LBP-1120 (USB, host-based CAPT) reaches the XP guest. Without it the
> printer will not attach.

## 2. Install VirtualBox

Run the platform installer and accept the defaults (Next → Next → Install). Allow the
network-interface warning; finish and let it launch.

## 3. Install the Extension Pack

VirtualBox → **File → Preferences → Extensions** (or **Tools → Extension Pack Manager**)
→ **Add** → select the downloaded `.vbox-extpack` → accept the licence → install.
Verify it appears in the list.

## 4. (Optional) Move the default machine folder

If drive **C:** is low on space, change where VMs are stored so the imported disk
doesn't fill the system drive:

**File → Preferences → General → Default Machine Folder** → pick a folder on a larger
drive (e.g. `D:\VirtualBox VMs` or `C:\Virtualization\VMs`).

Do this **before** importing the appliance (Part 2), so the image lands there.

## 5. Enable hardware virtualization (VT-x / AMD-V) in BIOS/UEFI

The VM will not run correctly without it. Reboot into BIOS/UEFI (usually `Del`/`F2`
at power-on) and enable:

- Intel: **Intel Virtualization Technology (VT-x)** (and **VT-d** if present);
- AMD: **SVM Mode** (AMD-V).

Save and reboot. If Windows shows VirtualBox VMs failing with a VT-x error, this is
the cause. (On some laptops, also disable **Hyper-V** / "Virtual Machine Platform" in
Windows Features, which can lock VT-x away from VirtualBox.)

## 6. Verify

- VirtualBox Manager opens without errors.
- **File → Preferences → Extensions** lists the Extension Pack.
- No VT-x warning when starting a VM.

Next: **`Appliance-import.md`** — import `Microelectronics.ova` and first boot.
