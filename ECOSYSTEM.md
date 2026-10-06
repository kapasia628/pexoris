# Pexoris Ecosystem Master Registry & Roadmap
> **Domain:** [pexoris.com](https://pexoris.com)  
> **Inspiration:** Sordum.org & Sysinternals (modernized with Apple Light Aesthetics, C# .NET 4.0 Win32, 0 installer, under 100 KB)  
> **Status:** 10 of 100 Tools Completed & Live in Ecosystem

---

## 1. Live Tools Registry

| # | Tool Name | Slug | Binary Size | ZIP Size | SHA-256 Hash | Google Drive Download Link | Local Web Page |
|---|---|---|---|---|---|---|---|
| **01** | **Pexoris FileUnlocker** | `file-unlocker` | 70 KB | 48 KB | `82df6a096cce9a71be84e0302b1f8cbb2c7bba4511516dd5ea5aa8612760f38b` | [Download](https://drive.google.com/uc?export=download&id=1NHqChEV65pzdy_ZCo60NaK7uxU_wmkF-) | `http://localhost/tools/website/tools/file-unlocker/` |
| **02** | **Pexoris PortKiller** | `port-killer` | 78 KB | 58 KB | `e03503f56e9c9f7a7bb629910d5ae684532b4f6e6378e946a48f76fa9cce6d25` | [Download](https://drive.google.com/uc?export=download&id=1vjBtdv2flluVJW8YMBVtNFTYFLpPKpIB) | `http://localhost/tools/website/tools/port-killer/` |
| **03** | **Pexoris DoH Switcher** | `doh-switcher` | 38 KB | 17 KB | `9ad64768039f10ad8b9408cd5c82dbffee426cf4df308a7731d0fed85d5ef395` | [Download](https://drive.google.com/uc?export=download&id=1SmJHBFupPHx4hLOnu9ISylt_nbrJtx9l) | `http://localhost/tools/website/tools/doh-switcher/` |
| **04** | **Pexoris PrintFixer** | `print-fixer` | 73 KB | 50 KB | `59b56e001c1459c2d203f47af41bda738bdf5a39758d14bf63c11b87f23642d1` | [Download](https://drive.google.com/uc?export=download&id=1us1wofg2_yPOx7FIofKxuX7jM3lsE-6W) | `http://localhost/tools/website/tools/print-fixer/` |
| **05** | **Pexoris AIShield** | `ai-shield` | 53 KB | 30 KB | `9998d40b2975e0c52933a26c4b9c6c6e783af9b7598b43854630ba0fe9e585bb` | [Download](https://drive.google.com/uc?export=download&id=1Lk_i7XyLGwSYCktaXudg__C_nQF4KYv2) | `http://localhost/tools/website/tools/ai-shield/` |
| **06** | **Pexoris USBShield** | `usb-shield` | 59 KB | 34 KB | `b4710e51ad864a876f3a040d5a4f6c8a8434a9c99325419e35199bdc88f0c592` | [Download](https://drive.google.com/uc?export=download&id=1jKcv_tS1te0zzu1ExlYeUfT8D97Vm2Xc) | `http://localhost/tools/website/tools/usb-shield/` |
| **07** | **Pexoris ContextMenuEditor** | `context-menu-editor` | 49 KB | 30 KB | `36cc8cc8034958952fb1d9cb49d5a61dceb56613bfb527cbe2f682abcb48d1e8` | [Download](https://drive.google.com/uc?export=download&id=1EFx4VOzqdNSafDdubZPcyoT8Bn9ZNfff) | `http://localhost/tools/website/tools/context-menu-editor/` |
| **08** | **Pexoris ServiceOptimizer** | `service-optimizer` | 69 KB | 44 KB | `827f5675cf999177fe82ee5a6f93281d8baea6222a0b017366eea7485f5cb8ca` | [Download](https://drive.google.com/uc?export=download&id=1ZEuYBVBT2AKpZ7XpzbcGkahuSya5UZBP) | `http://localhost/tools/website/tools/service-optimizer/` |
| **09** | **Pexoris HostsManager** | `hosts-manager` | 57 KB | 36 KB | `cb5b1d28889247c6ff18d852fdf2597f967a27f4834d3821ce9b04cec64eb556` | [Download](https://drive.google.com/uc?export=download&id=1cbsrisYM2H2wRRcPdcwXAwC_aMJFqmHu) | `http://localhost/tools/website/tools/hosts-manager/` |
| **10** | **Pexoris TempCleaner** | `temp-cleaner` | 56 KB | 37 KB | `86d70f11a3188d10b0a66956529ea5e97f4f7735a1c9dbeddc34a684dd54bf83` | [Download](https://drive.google.com/uc?export=download&id=14GAkL1D6kB9Pjyn_qXWTo9uc4KEUoOr6) | `http://localhost/tools/website/tools/temp-cleaner/` |

---

## 2. Pexoris Ecosystem Standard Summary

### A. Desktop Application Standards (C# / WinForms / .NET 4.0+)
1. **Window**: Fixed `860 x 640` borderless window with smooth `CS_DROPSHADOW` (`0x00020000`).
2. **Mandatory App & Taskbar Icon**: Must call `Icon.ExtractAssociatedIcon(Application.ExecutablePath)` and send `WM_SETICON` with `ICON_SMALL` & `ICON_BIG` in `OnHandleCreated` so Windows Taskbar, Alt+Tab, and Task Manager show the custom high-resolution branded icon instead of the generic .NET 4-square grid.
3. **Unique Full-Color Icons**: Both Title Bar (22x22) and Top Card (44x44) MUST render `this.Icon.ToBitmap()` via `PictureBox`. NEVER use generic text emojis (like `🛡️` or `📁`) which render as identical black-and-white symbols across multiple tools.
4. **Mandatory PexorisButton Engine**: All buttons must inherit from `PexorisButton` (owner-drawn, anti-aliased with `GetRoundedRectangleF`, rounded corners, zero black border artifacts). Never use raw WinForms `new Button()`.
5. **Title Bar** (44px): Brand logo + `Pexoris [ToolName] • Portable Utility` + Windows 11 `—` and `✕` controls (never Mac traffic lights).
6. **Top Control Card** (Y=58, H=120): Tool icon PictureBox, headline, subtitle, capsule status pill badge, input row + action presets.
7. **Explorer Table** (Y=214, H=280): Details ListView, `FullRowSelect`, Explorer visual style, soft red highlights for locked/occupied items.
8. **Action Bar** (Y=508, H=50): Slot 1 has red `⚡ Kill Process & Free Port` / primary action, slots 2-3 blue actions, slot 4 outline, slot 5 scan/refresh.
9. **Footer** (38px): Checkbox, status text, and `pexoris.com` link.
10. **Portability**: Single standalone `.exe` under 100 KB, 0 installer, 0 telemetry.

### B. Landing Page Architecture Standards (SEO • AEO • GEO • AIO • SXO)
1. **Directory**: `website/tools/[slug]/index.html`.
2. **Header**: Solid `#FFFFFF` background (`box-shadow: 0 2px 10px rgba(0,0,0,0.04)`), never semi-transparent.
3. **Nav Links**: Only section anchors (`← All Tools`, `Request a Tool`, `Features`, `Comparison`, `How to Use`, `FAQ`, Download CTA). No cross-tool links in the header.
4. **Structured Data**: 4 JSON-LD schemas (`SoftwareApplication`, `FAQPage`, `HowTo`, `BreadcrumbList`).
5. **Hero**: Problem error quote, Apple CTA buttons, trust badges, SHA-256 copy box.
6. **Showcase**: Pixel-perfect interactive CSS mockup of the desktop application.
7. **6-Feature Grid**: 3-column Apple cards with icon box and technical depth.
8. **Comparison Matrix**: Table comparing against native Windows tools and legacy freeware.
9. **4-Step How-To Guide**: Step-by-step tutorial cards.
11. **Footer**: Official `brand-logo.png` with fallback: `onerror="this.onerror=null; this.src='../../brand-logo.png';"`, sitemap, and legal links.

### C. Problem-Solving Blog & Organic Traffic Guides
1. **Directory**: `website/blog/[article-slug]/index.html`.
2. **Strategy**: Target exact high-volume Windows error keywords (e.g. *"How to Delete Stuck Print Job..."*, *"Fix Port 80 in use by PID 4..."*).
3. **3-Tier Anatomy**:
   - Quick Answer Box (35-40 words for Google AI Overviews and ChatGPT citation).
   - Technical Cause (Why Windows locked the resource).
   - Native Windows Way (Manual CMD / PowerShell / Registry).
   - 1-Click Pexoris Solution (Sticky CTA box with download link and SHA-256).
4. **Structured Data**: `BlogPosting`, `HowTo`, `FAQPage`.
5. **Cross-Linking**: Bi-directional links connecting Blog post to the Tool Landing page.

---

## 3. 1-Click Master Build System

Running `build.bat` in the project root:
1. Compiles all C# applications with `C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe`.
2. Embeds application icons and manifests (UAC administrator + DPI awareness V2).
3. Generates ZIP archives for distribution.
4. Computes SHA-256 hashes automatically.
5. Copies all final executables and ZIP packages to `final softwere for g drive uplod/`.

---

## 4. Upcoming Tools Pipeline (Roadmap to 100 Tools)

- [x] **#01 Pexoris FileUnlocker** (Unlock & force delete locked files)
- [x] **#02 Pexoris PortKiller** (Free stuck ports 80, 3000, 8080)
- [x] **#03 Pexoris DoH Switcher** (Windows 11 encrypted DNS & AdGuard)
- [x] **#04 Pexoris PrintFixer** (Clear stuck print queue & restart spooler)
- [x] **#05 Pexoris AIShield** (Disable Windows 11 Recall screenshots, telemetry & Copilot)
- [x] **#06 Pexoris USBShield** (Lock USB storage to Read-Only & block AutoPlay malware)
- [x] **#07 Pexoris Context Menu Editor** (Clean bloated right-click menus & restore classic Windows 11 menu)
- [x] **#08 Pexoris Service Optimizer** (Safely disable unnecessary Windows services)
- [x] **#09 Pexoris Hosts Manager** (Fast graphical hosts file editor with adblock presets)
- [x] **#10 Pexoris Temp Cleaner** (Deep Windows update cache & junk cleaner)
- [x] **#11 Pexoris Startup Inspector** (Inspect and disable stubborn autorun entries)
- [x] **#12 Pexoris Defender Toggle** (Temporarily pause Windows Defender during dev compilation)
- [ ] **#13 Pexoris DNSFlusher** (Flush DNS cache, reset Winsock catalog & purge ARP)
- [ ] **#14 Pexoris RamTrimmer** (Flush standby memory cache & empty working sets)
