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

## 🗺️ Pexoris Suite Roadmap

| Utility | Problem Solved | Target Platform | Status |
|---|---|---|---|
| **Pexoris FileUnlocker** | Unlocks locked files, handles, and stubborn folders | Windows 7 - 11 | **v1.0 Released** |
| **Pexoris PortKiller** | 1-Click kills processes hogging ports `80`, `3000`, `8080` | Windows 10 & 11 | **In Development** |
| **Pexoris DoH Switcher** | 1-Click encrypted DNS-over-HTTPS (Cloudflare, AdGuard, NextDNS) | Windows 11 | Planned |
| **Pexoris AI Shield** | Disables Windows 11 Recall snapshots and Copilot telemetry | Windows 11 | Planned |
| **Pexoris ExifStripper** | Strips GPS location and camera metadata from photos/documents | Windows 10 & 11 | Planned |
| **Pexoris Context+** | Restores instant Windows 10 classic context menu on Windows 11 | Windows 11 | Planned |

---

## 📄 License
This project is open-source under the **MIT License**. Feel free to use, modify, and distribute it freely.
