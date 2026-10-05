# Pexoris Ecosystem Comprehensive Audit Report

**Date:** October 5, 2026  
**Project:** Pexoris Portable Windows Utilities (`pexoris.com`)  
**Scope:** Desktop Software (C# .NET), Website UI, SEO, Assets & Schemas

---

## 📌 Executive Summary

An exhaustive diagnostic audit was conducted across all 6 desktop applications, 18 HTML landing pages & troubleshooting guides, stylesheets, assets, and metadata.

Overall, the core architecture is solid:
- All 6 C# desktop binaries compile without errors via `build.bat`.
- Core Win32 APIs (Restart Manager, WinSpool, TCP table inspection) are implemented cleanly without reliance on slow external command-line scripts.
- Structured Data (JSON-LD `SoftwareApplication`, `FAQPage`, `HowTo`, `BreadcrumbList`) conforms to Schema.org standards across all landing pages.

However, **9 distinct issues** spanning **Software Compliance**, **SEO/Asset Integrity**, and **UI/CSS Standards** were identified and cataloged below.

---

## 1. 💻 Software / Desktop Applications (C# .NET)

### Bug 1.1: `PexorisPrintFixer` Missing Ecosystem Standards
* **Impact:** Visual inconsistency, generic Windows icon on user desktop/taskbar.
* **File:** [`src/PexorisPrintFixer/MainForm.cs`](file:///c:/xampp/htdocs/tools/src/PexorisPrintFixer/MainForm.cs) and [`Theme.cs`](file:///c:/xampp/htdocs/tools/src/PexorisPrintFixer/Theme.cs)
* **Details:**
  1. **Taskbar / Alt+Tab Icon Missing in Standalone Mode:**
     `MainForm.cs` (lines 88–96) attempts to load `this.Icon` from a relative path `Assets\app.ico` on disk. When end users download and run `PexorisPrintFixer.exe` as a standalone portable file without an `Assets/` directory, `Assets\app.ico` does not exist. It fails to call:
     ```csharp
     this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
     SendMessage(this.Handle, WM_SETICON, (IntPtr)ICON_BIG, this.Icon.Handle);
     SendMessage(this.Handle, WM_SETICON, (IntPtr)ICON_SMALL, this.Icon.Handle);
     ```
     Consequently, Windows displays the default generic 4-square .NET WinForms icon in the taskbar and Alt+Tab switcher.
  2. **Raw `new Button()` instead of `PexorisButton`:**
     Buttons (`btnPurgeAll`, `btnPrintTest`, `btnRestartSpooler`, `btnCancelJob`, `btnRefresh`, `btnFilterAll`, `btnFilterJobs`, `btnFilterOffline`) use WinForms `Button` with manual background styling rather than inheriting from the anti-aliased `PexorisButton` engine with 7px rounded corners.
  3. **Text Emoji instead of Full-Color PictureBox:**
     The top control card uses `lblCardIcon = new Label { Text = "🖨️" }` with Segoe UI Emoji instead of a crisp 44x44 / 22x22 `PictureBox` rendering the actual brand icon bitmap.

---

## 2. 🔍 SEO, Assets & Metadata Integrity

### Bug 2.1: 404 Favicon Links in 5 Blog Posts
* **Impact:** 404 HTTP errors in browser network inspector, missing tab favicon in Google search results & user browsers.
* **Affected Files:**
  - [`website/blog/enable-dns-over-https-windows-11/index.html`](file:///c:/xampp/htdocs/tools/website/blog/enable-dns-over-https-windows-11/index.html#L16)
  - [`website/blog/fix-port-80-3000-already-in-use-windows/index.html`](file:///c:/xampp/htdocs/tools/website/blog/fix-port-80-3000-already-in-use-windows/index.html#L16)
  - [`website/blog/force-delete-locked-file-windows-11/index.html`](file:///c:/xampp/htdocs/tools/website/blog/force-delete-locked-file-windows-11/index.html#L16)
  - [`website/blog/disable-windows-recall-copilot-windows-11/index.html`](file:///c:/xampp/htdocs/tools/website/blog/disable-windows-recall-copilot-windows-11/index.html#L16)
  - [`website/blog/disable-usb-autoplay-write-protect-windows-11/index.html`](file:///c:/xampp/htdocs/tools/website/blog/disable-usb-autoplay-write-protect-windows-11/index.html#L15)
* **Details:**
  These guides include `<link rel="icon" type="image/x-icon" href="favicon.ico">`, but no `favicon.ico` exists inside those subfolders.
* **Fix:** Change `href="favicon.ico"` to `href="../../favicon.ico"` (which points to root `website/favicon.ico`).

---

### Bug 2.2: SHA-256 Checksum Discrepancies
* **Impact:** User confusion and failed hash verification when downloading binaries.
* **Details:**
  1. **Pexoris PortKiller (`website/tools/port-killer/index.html`):**
     - Hero copy box (line 1177): `fc19439683c05b1512111b39e614f32a90a4d6b506d0ff0dd6bfc2f6c372f367`
     - Bottom section & `AGENTS.md` (line 1541): `e03503f56e9c9f7a7bb629910d5ae684532b4f6e6378e946a48f76fa9cce6d25`
     - *Issue:* Conflicting hashes on the same page.
  2. **Pexoris FileUnlocker (`website/tools/file-unlocker/index.html`):**
     - Page lists: `72c27f3378ca486f7eb2d9cf850f649954000eb12ff131b670e46514021d39d6`
     - `AGENTS.md` lists: `82df6a096cce9a71be84e0302b1f8cbb2c7bba4511516dd5ea5aa8612760f38b`
     - *Fix:* Synchronize with the official Google Drive package hash.

---

### Bug 2.3: Mojibake Character Encoding in AI Shield Blog
* **Impact:** Visual rendering glitch in top navigation.
* **File:** [`website/blog/disable-windows-recall-copilot-windows-11/index.html`](file:///c:/xampp/htdocs/tools/website/blog/disable-windows-recall-copilot-windows-11/index.html#L494)
* **Details:**
  Line 494 renders `Explore Tools â†’` due to corrupted UTF-8 byte sequence (`0xE2 0x86 0x92`).
* **Fix:** Replace with `Explore Tools &rarr;` or `Explore Tools →`.

---

### Bug 2.4: Broken Logo Fallback Paths
* **Impact:** Broken image icon if primary logo fails to render.
* **Affected Files:**
  - [`website/privacy/index.html`](file:///c:/xampp/htdocs/tools/website/privacy/index.html#L457): `onerror="this.src='brand-logo.png';"` (fails because `brand-logo.png` is in parent directory `../`).
  - [`website/terms/index.html`](file:///c:/xampp/htdocs/tools/website/terms/index.html#L457): `onerror="this.src='brand-logo.png';"` (fails for the same reason).
  - [`website/tools/index.html`](file:///c:/xampp/htdocs/tools/website/tools/index.html#L527): `onerror="this.src='../../brand-logo.png';"` (points outside website directory to `tools/brand-logo.png`).
  - [`website/tools/*/index.html`]: `onerror="this.src='../brand-logo.png';"` points to non-existent `website/tools/brand-logo.png`.
* **Fix:** Normalize fallbacks to point to valid logo copies (`brand-logo.png` or `../../brand-logo.png`).

---

## 3. 🎨 UI & Design System Violations

### Bug 3.1: Semi-Transparent Header in Request Page
* **Impact:** Background text and images bleed through the header navigation upon scrolling.
* **File:** [`website/request/index.html`](file:///c:/xampp/htdocs/tools/website/request/index.html)
* **Violation of Rule:**
  > *"Header: Must always use solid background (`background: #FFFFFF; box-shadow: 0 2px 10px rgba(0,0,0,0.04);`), NEVER semi-transparent `rgba()`, so scrolled content never bleeds through."*
* **Current Code:** `header { background: rgba(255, 255, 255, 0.92); }`
* **Fix:** Change to `header.site-header { background: #FFFFFF; box-shadow: 0 2px 10px rgba(0,0,0,0.04); }`.

---

### Bug 3.2: Unscoped Generic `header` CSS Selector
* **Impact:** Global CSS pollution affecting potential future `<header>` sub-elements.
* **Affected Files:**
  - `website/request/index.html`
  - `website/tools/doh-switcher/index.html`
  - `website/tools/file-unlocker/index.html`
  - `website/tools/port-killer/index.html`
  - `website/tools/print-fixer/index.html`
* **Violation of Rule:**
  > *"Scoped Site Header: Always scope top nav CSS with `header.site-header`. Never use generic `header` type selector."*
* **Fix:** Replace `header {` with `header.site-header {`.

---

## 📋 Actionable Resolution Roadmap

| Priority | Area | Task | Effort |
|:---:|:---:|:---|:---:|
| **P0** | Software | Upgrade `PexorisPrintFixer` to implement `Icon.ExtractAssociatedIcon`, `WM_SETICON`, `PexorisButton`, and `PictureBox` logo | ~15 mins |
| **P0** | SEO | Fix Favicon 404 links across all 5 blog guides (`../../favicon.ico`) | ~5 mins |
| **P1** | SEO | Harmonize SHA-256 hashes on `port-killer` and `file-unlocker` landing pages | ~5 mins |
| **P1** | UI | Fix semi-transparent header in `request/index.html` to solid `#FFFFFF` | ~3 mins |
| **P2** | UI | Scope generic `header` selectors to `header.site-header` across 5 pages | ~5 mins |
| **P2** | Content | Fix Mojibake `Explore Tools â†’` in AI Shield blog | ~2 mins |
| **P2** | Assets | Fix logo `onerror` fallback paths across legal and tool pages | ~5 mins |
