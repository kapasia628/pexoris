---
description: "Strict Architectural and UI Standard for all 100+ Pexoris Windows Portable Utilities and Landing Pages"
globs: ["**/*"]
alwaysApply: true
---

# Pexoris Ecosystem Standard (100+ Windows Tools & Landing Pages)

This document defines the strict, unchangeable standard for building Windows desktop utilities and their landing pages across the entire **Pexoris** ecosystem (`pexoris.com`). Every single tool must feel like it was created by the exact same developer, maintaining identical UX hierarchy, aesthetic refinement, and search optimization.

---

## 1. Desktop Application UI Standard (C# / WinForms)

All desktop utilities are standalone, single-file executables compiled with `csc.exe` (.NET Framework 4.0+), weighing under 100 KB with 0 installer and 0 telemetry.

### A. Window Frame & Chrome (Strict Standard)
- **Window Size**: Fixed `860 x 640` borderless window (`FormBorderStyle = FormBorderStyle.None`).
- **Shadow**: Native `CS_DROPSHADOW` (`0x00020000`) for Apple-grade smooth window elevation.
- **Mandatory Taskbar Icon Extraction**:
  - Because standalone executables run without external `Assets\app.ico` files, the Form MUST extract its compiled icon directly from itself:
    ```csharp
    this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
    ```
  - In `OnHandleCreated(EventArgs e)`: Send `WM_SETICON` with `ICON_SMALL` (0) and `ICON_BIG` (1) to guarantee Windows Taskbar, Alt+Tab, and Task Manager show the custom icon instead of the generic .NET grid icon.
- **Title Bar** (Height: 44px, Background: `#FFFFFF`, 1px bottom border `#E5E7EB`):
  - Left: 22x22 `PictureBox` rendering `this.Icon.ToBitmap()` + Bold Title `Pexoris [ToolName] • Portable Utility` (`FontHeader 10pt`).
  - Right: Clean Windows 11 controls: `—` (minimize) and `✕` (close, with `#EF4444` red hover).
  - Dragging: Handled via Win32 `ReleaseCapture()` and `WM_NCLBUTTONDOWN`.
  - **NEVER use macOS colored traffic light circles.** Always use clean `—` and `✕`.

### B. Top Control Card (Y: 58, Height: 120, Width: 824)
- **Background**: `#FFFFFF` with 8-10px rounded corners and 1px `#E2E8F0` border.
- **Top Row**:
  - Left: 44x44 `PictureBox` displaying the tool's unique custom colored icon (`this.Icon.ToBitmap()`). **NEVER use generic text emojis (like `🛡️` or `📁`) which render as identical black-and-white symbols across multiple tools.**
  - Heading (`FontBold 10.5pt`): Primary value proposition headline.
  - Subtitle (`FontBody 8.5pt`, `#6B7280`): Technical engine details.
  - Right: Capsule Status Pill Badge (`READY` in grey, or `OCCUPIED`/`LOCKED` in red).
- **Bottom Row**:
  - Input field (Search / Path) or dropdown with height 32-34px.
  - 1-Click Action Buttons / Quick Filters (Width: 100-140px, Height: 34px) using `PexorisButton`.

### C. Section Header Label (Y: 190, Height: 20)
- Bold label above table: `Processes Holding ... :` (`FontBold 9.5pt`).

### D. Explorer ListView Container (Y: 214, Height: 280, Width: 824)
- Container with 1px `#E2E8F0` border.
- `ListView` with `DockStyle.Fill`, `View.Details`, `FullRowSelect = true`, `BorderStyle.None`.
- Explorer theme enabled via `SetWindowTheme(hwnd, "Explorer", null)`.
- Sockets / Handles highlighted in soft red (`#FEF2F2`) when active/occupied.
- Context Menu on Right-Click with primary action (e.g. `⚡ Kill Process & Free Port`).

### E. Action Buttons Bar & Button Engine (Y: 508, Height: 50, Width: 824)
- **MANDATORY `PexorisButton` Usage**: Never use plain WinForms `new Button()`. Always inherit from `PexorisButton` with anti-aliased rounded corners (`CornerRadius = 7f`), smooth hover/pressed transitions, no black border artifacts:
  - `StyleType = ButtonStyleType.DestructiveRed` (Slot 1 Primary Red `#EF4444`)
  - `StyleType = ButtonStyleType.PrimaryBlue` (Slot 2 Blue `#007AFF`)
  - `StyleType = ButtonStyleType.SecondaryOutline` (Slot 3 & 4 Outline `#FFFFFF` with `#D1D5DB` border)
  - `StyleType = ButtonStyleType.SuccessGreen` (Teal/Emerald `#10B981`)
- **Button 1 (Primary Destructive Red)**: Width ~190-240px, Apple Red.
- **Button 2 (Primary Blue)**: Width 140-180px.
- **Button 3 & 4 (Outline / Secondary)**: Width 130-170px.
- **Button 5 (Scan / Refresh)**: Width 70-120px.

### F. Footer Status Bar (Height: 38px, `Dock = DockStyle.Bottom`)
- 1px top border `#E5E7EB`.
- Left: Checkbox (`☑ Auto-refresh...` or `☑ Add to Windows Context Menu`).
- Middle: Status message (`Ready. 100% Standalone & Portable.`).
- Right: Clickable `pexoris.com` LinkLabel (`#007AFF`).

---

## 2. Landing Page Architecture Standard (SEO • AEO • GEO • AIO • SXO)

Every tool has its dedicated folder: `website/tools/[tool-slug]/index.html` (Accessible via clean URL: `https://pexoris.com/tools/[tool-slug]/`).

### A. Search & AI Optimization Layer (SEO, AEO, GEO, AIO, SXO)
1. **Title Tag**: High-CTR, keyword-rich formula: `Pexoris [ToolName] — [Primary Benefit / Target Keyword] in Windows 11 (Portable & Free)`.
2. **Meta Description**: Problem-solution-action formula stating exact file size, 100% portable, and 0 bloatware.
3. **Long-Tail Keywords**: Specific Windows error messages, dev frameworks (Node, Apache, XAMPP, Docker), and sysadmin keywords.
4. **Structured Data (JSON-LD)**:
   - `SoftwareApplication`: Operating system, category, rating 4.9, size in KB, feature list, download URL.
   - `FAQPage`: AEO-optimized answers answering the top 5-6 questions in direct 40-word summaries for Google Featured Snippets, ChatGPT Search, Perplexity AI, Claude, and Gemini.
   - `HowTo`: 4 sequential steps for Google Rich Results.
   - `BreadcrumbList`: Pexoris -> Tools -> [Tool].
5. **OpenGraph & Twitter Cards**: High-res logo, summary card, social share metadata.

### B. Visual Sections (1:1 Layout Blueprint)
1. **Sticky Header**: Brand logo, Pexoris title, Tool version badge, navigation links (`Home`, `All Tools`, `Features`, `Comparison`, `How to Use`, `FAQ`, `Blog`, `Request a Tool`). **NEVER put a download button in the header**.
2. **Hero Section**:
   - `pill-guarantee`: `100% Portable • Zero Installer • Zero Telemetry • Clean VirusTotal`.
   - `hero-h1`: Punchy headline with gradient highlight.
   - `pain-point-quote`: Red error message pill matching the real Windows error.
   - `hero-p`: Concise description emphasizing exact KB size and 0 reboot.
   - `cta-container`: Trust badges row + SHA-256 copy snippet. **NEVER put a download button in the hero** — users must scroll through features and guides first.
   - **NO Direct .EXE**: Never provide a direct .EXE button anywhere on the landing page or site.
3. **Interactive Window Showcase (Mockup)**:
   - Pixel-accurate CSS reproduction of the real Windows desktop app: title bar (`—`, `✕`), top control card, table with highlighted rows, 5 action buttons, and footer.
4. **Features Grid (6 Cards)**:
   - 3-column Apple cards with icon box, bold title, and technical explanation.
5. **Competitive Comparison Matrix**:
   - Table comparing Pexoris against Windows built-in tools (CMD netstat, taskkill) and legacy freeware.
6. **Step-by-Step How-To Guide (4 Cards)**:
   - Numbered steps (1 to 4) explaining how to solve the problem in seconds.
7. **FAQ Accordion**:
   - Collapsible, smooth-animating Q&A addressing technical nuances, security, and portability.
8. **Final Bottom Download Card (High-Conversion Last Step)**:
   - `<section id="download">` placed immediately above the footer.
   - This is the **ONLY** place on the landing page where the download button appears: `Download Free (Portable ZIP — [Size])` linking to the Google Drive download URL.
   - Clean VirusTotal 0/72 guarantee badge and SHA-256 hash.
9. **Footer**:
   - Clean links, legal, privacy, terms, tool catalog, and copyright.
9. **Interactive JavaScript**:
   - 1-Click Copy SHA-256 hash with visual feedback.
   - Smooth accordion click handlers.
   - Configurable `EXTERNAL_DOWNLOAD_URL` constant for Google Drive / CDN mirrors.
10. **Strict Header & Footer Rules**:
   - **Header**: Must always use solid background (`background: #FFFFFF; box-shadow: 0 2px 10px rgba(0,0,0,0.04)`), NEVER semi-transparent `rgba()`, so scrolled content never bleeds through.
   - **Nav Links**: Top nav menu only contains section anchors (`← All Tools`, `Request a Tool`, `Features`, `Comparison`, `How to Use`, `FAQ`, Download CTA). Never put individual tool cross-links in the top header.
   - **Footer Logo**: Always use `brand-logo.png` with fallback: `onerror="this.onerror=null; this.src='../../brand-logo.png';"`.
11. **Mandatory Problem-Solving Blog & Organic Search Guides**:
   - For every new tool created, always author its dedicated troubleshooting article in `website/blog/[article-slug]/index.html` targeting high-search Windows error keywords.
   - 3-Tier Structure: Technical Cause + Native Windows Manual Fix (CMD/Registry) + 1-Click Pexoris Instant Fix with sticky download card.
   - JSON-LD Schemas: `BlogPosting`, `HowTo`, `FAQPage` with 40-word AEO Quick-Answer summary box.
   - Bi-directional cross-linking between Tool Landing Page and Blog Guides.
   - **Scoped Site Header**: Top navigation MUST use `header.site-header`. Never use generic `header` type selector in blog CSS (which causes the article title to stick and display unwanted background boxes). The article title must use `<div class="post-header">` with `position: static` and no background.

---

## 3. Official Live Tools Registry (4/100 Tools Completed)

| # | Tool Name | Slug | Binary Size | ZIP Size | SHA-256 Hash | Google Drive Download Link |
|---|---|---|---|---|---|---|
| **01** | **Pexoris FileUnlocker** | `file-unlocker` | 70 KB | 48 KB | `82df6a096cce9a71be84e0302b1f8cbb2c7bba4511516dd5ea5aa8612760f38b` | `https://drive.google.com/uc?export=download&id=1NHqChEV65pzdy_ZCo60NaK7uxU_wmkF-` |
| **02** | **Pexoris PortKiller** | `port-killer` | 78 KB | 58 KB | `e03503f56e9c9f7a7bb629910d5ae684532b4f6e6378e946a48f76fa9cce6d25` | `https://drive.google.com/uc?export=download&id=1vjBtdv2flluVJW8YMBVtNFTYFLpPKpIB` |
| **03** | **Pexoris DoH Switcher** | `doh-switcher` | 38 KB | 17 KB | `9ad64768039f10ad8b9408cd5c82dbffee426cf4df308a7731d0fed85d5ef395` | `https://drive.google.com/uc?export=download&id=1SmJHBFupPHx4hLOnu9ISylt_nbrJtx9l` |
| **04** | **Pexoris PrintFixer** | `print-fixer` | 73 KB | 50 KB | `59b56e001c1459c2d203f47af41bda738bdf5a39758d14bf63c11b87f23642d1` | `https://drive.google.com/uc?export=download&id=1us1wofg2_yPOx7FIofKxuX7jM3lsE-6W` |

