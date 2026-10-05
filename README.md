# ⚡ Pexoris Portable System Utilities

> **Precision Windows Tools. Built to solve real problems.**  
> Inspired by the portable ethos of Sordum and Sysinternals, re-engineered for Windows 10 & 11 with a unified cyber-dark aesthetic, modern APIs, and zero bloatware.

Official Website: [pexoris.com](https://pexoris.com)  
License: [MIT](LICENSE)

---

## 🌟 Core Philosophy

1. **100% Standalone & Portable**: Single executable binaries. Never requires an installer. Just run, fix the issue, and you're done.
2. **Zero Dependency Overhead**: Runs out-of-the-box on clean Windows 7, 8, 10, and 11 without needing Python, Node, Java, or large .NET runtimes.
3. **Zero Clutter**: Leaves no persistent registry garbage or background services behind. Configuration is stored locally in `config.ini` alongside the executable.
4. **Unified Brand Aesthetics**: Every Pexoris tool shares the same cohesive design system, high-DPI icons, and high-contrast dark theme.
5. **Transparency & Trust**: Open-source source code, reproducible builds, and verified SHA256 hashes for every release.

---

## 🎨 Pexoris Light Mode Design System

All Pexoris utilities are built using a strict, unified visual design token system in **Light Mode**:

| Token | Hex Code | Visual Preview | Purpose |
|---|---|---|---|
| **Pexoris Cloud Canvas** | `#F4F6FA` | ⬜ Soft Slate Base | Main application background |
| **Pure White Surface** | `#FFFFFF` | ⬜ Pure White Card | Cards, drop zones, list views |
| **Vivid Ocean Blue** | `#0284C7` | 🔷 Primary Accent | Brand identity, primary buttons, borders, highlights |
| **Action Crimson** | `#EF4444` | 🟥 Danger Red | Force kill, terminate locks, destructive actions |
| **Emerald Safe** | `#10B981` | 🟩 Success Green | Clean state, verified files, unlocked status |
| **Slate Gray** | `#0F172A` / `#475569` | 🔘 Crisp Dark Slate | Crystal-clear typography on light backgrounds |

---

## 📂 Featured Tool: Pexoris FileUnlocker v1.0

![Pexoris Icon](src/PexorisFileUnlocker/Assets/app.ico)

Have you ever encountered this frustrating Windows error?
> *"The action can't be completed because the file is open in another program."*

**Pexoris FileUnlocker** is an ultra-lightweight (68 KB) standalone Windows utility that inspects locked files and folders, identifies the holding processes, and safely terminates or unlocks them.

### Key Capabilities
- **Native Restart Manager Engine (`rstrtmgr.dll`)**: Uses the official Windows Restart Manager subsystem to query active locks without crashing system services.
- **Drag & Drop Workflow**: Simply drop any locked `.dll`, `.exe`, `.tmp`, or folder into the window.
- **⚡ Kill Process & Free File**: Instantly terminates the locking PID with a single click.
- **🗑️ Force Unlock & Delete**: Eliminates locking processes and deletes the locked resource immediately.
- **⏳ Schedule Delete on Reboot (`MoveFileEx`)**: Writes to Windows `PendingFileRenameOperations` to erase stubborn driver or kernel-locked files on the next system startup.
- **✏️ Unlock & Rename**: Rename or relocate stuck files in place without rebooting.
- **🖱️ Windows Explorer Context Menu**: Seamless 1-click toggle to add `Unlock with Pexoris` to the Windows Explorer right-click menu.

### Binary Verification & Download
- **Release Package**: `build/PexorisFileUnlocker-v1.0-Portable.zip` (78 KB)
- **Direct Binary**: `build/PexorisFileUnlocker.exe` (70 KB)
- **SHA-256 Checksum**:
  ```text
  72c27f3378ca486f7eb2d9cf850f649954000eb12ff131b670e46514021d39d6
  ```

---

## 🛠️ How to Build from Source

Pexoris utilities are engineered to compile cleanly using the Windows built-in C# compiler (`csc.exe`) without needing Visual Studio or external SDK installations.

1. Clone or download this repository.
2. Run the automated build script:
   ```cmd
   build.bat
   ```
3. The standalone portable binary will be generated inside `build\PexorisFileUnlocker.exe`.

---

## 🚀 Live Released Tools (6/100 Completed)

| # | Utility | Problem Solved | Binary Size | ZIP Size | SHA-256 Hash |
|---|---|---|---|---|---|
| **01** | **[Pexoris FileUnlocker](website/tools/file-unlocker/)** | Force unlock and delete files locked by Windows processes | 70 KB | 48 KB | `82df6a096cce9a71be84e0302b1f8cbb2c7bba4511516dd5ea5aa8612760f38b` |
| **02** | **[Pexoris PortKiller](website/tools/port-killer/)** | 1-Click kills processes hogging ports `80`, `3000`, `8080` | 78 KB | 58 KB | `e03503f56e9c9f7a7bb629910d5ae684532b4f6e6378e946a48f76fa9cce6d25` |
| **03** | **[Pexoris DoH Switcher](website/tools/doh-switcher/)** | 1-Click encrypted DNS-over-HTTPS (Cloudflare, AdGuard, Google) | 38 KB | 17 KB | `9ad64768039f10ad8b9408cd5c82dbffee426cf4df308a7731d0fed85d5ef395` |
| **04** | **[Pexoris PrintFixer](website/tools/print-fixer/)** | Purge stuck print queue & restart spooler in seconds | 73 KB | 50 KB | `59b56e001c1459c2d203f47af41bda738bdf5a39758d14bf63c11b87f23642d1` |
| **05** | **[Pexoris AIShield](website/tools/ai-shield/)** | Block Windows Recall screenshots, Copilot & DiagTrack telemetry | 53 KB | 30 KB | `9998d40b2975e0c52933a26c4b9c6c6e783af9b7598b43854630ba0fe9e585bb` |
| **06** | **[Pexoris USBShield](website/tools/usb-shield/)** | Lock USB ports to Read-Only & block AutoPlay malware worms | 59 KB | 34 KB | `b4710e51ad864a876f3a040d5a4f6c8a8434a9c99325419e35199bdc88f0c592` |

---

## 📄 License
This project is open-source under the **MIT License**. Feel free to use, modify, and distribute it freely.
