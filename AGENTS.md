# Pexoris Development & Ecosystem Guidelines

This repository hosts the **Pexoris** portable Windows utilities ecosystem (`pexoris.com`), inspired by Sordum.org.

All desktop tools and landing pages must adhere to the **Pexoris Ecosystem Standard**:
1. **Desktop Applications**:
   - 860x640 borderless window, smooth CS_DROPSHADOW, 44px title bar with brand logo + `—` and `✕` controls (never Mac traffic light circles).
   - **Taskbar Icon Extraction**: Must call `Icon.ExtractAssociatedIcon(Application.ExecutablePath)` and send `WM_SETICON` with `ICON_SMALL` & `ICON_BIG` so the Windows taskbar, Alt+Tab, and Task Manager never display the generic .NET 4-square icon.
   - **Unique Full-Color Icons**: Top Control Card and Title Bar must render the tool's actual 44x44 / 22x22 colored icon bitmap via `PictureBox`. Never use plain text emojis (like `🛡️`) which render identically across tools.
   - **PexorisButton Engine**: All buttons must inherit from `PexorisButton` with anti-aliased rounded corners (`CornerRadius = 7f`), smooth hover/pressed transitions, no black border artifacts, and curated styles (`DestructiveRed`, `PrimaryBlue`, `SuccessGreen`, `SecondaryOutline`). Never use raw `new Button()`.
   - Top Control Card (120px) with icon PictureBox, headline, subtitle, capsule status pill, input row + action presets.
   - Sockets/Handles Explorer ListView with Details view, FullRowSelect, and 1px border.
   - Action Buttons Bar (Y=508, H=50) with `⚡ Kill Process & Free Port` (Red) in slot 1, quick blue buttons, outline buttons, and scan.
   - Footer (38px) with checkbox, status text, and `pexoris.com` link.
   - Single portable `.exe`, C# .NET 4.0+, 0 installer, under 100 KB.
2. **Landing Pages**:
   - Dedicated clean slug folder: `website/tools/[tool-slug]/index.html`.
   - Comprehensive SEO, AEO, GEO, AIO, SXO architecture.
   - JSON-LD schemas: `SoftwareApplication`, `FAQPage`, `HowTo`, `BreadcrumbList`.
   - Hero with pain point error quote, trust badges, SHA-256 copy box (NO download button in hero).
   - Interactive CSS mockup matching desktop app pixel-by-pixel.
   - 6-Feature grid (3-column Apple cards).
   - Competitive comparison matrix table.
   - 4-step How-To guide.
   - Interactive FAQ accordion.
   - **Single Download Location (Strict)**: The download button (`Download Free (Portable ZIP)`) must ONLY be located at the very bottom of the page in a dedicated `<section id="download">` card right before the footer. NEVER place download buttons in the header, hero, or as Direct .EXE.
   - Clean footer with legal, sitemap, and category links.
3. **Problem-Solving Blog & Organic Search Guides**:
   - For every tool created, always generate a dedicated troubleshooting guide in `website/blog/[article-slug]/index.html` targeting exact Windows error queries.
   - Structure: Technical Root Cause + Manual Native Windows Solution (CMD/Registry) + 1-Click Pexoris Instant Fix with sticky download card.
   - Schema: `BlogPosting`, `HowTo`, `FAQPage` with AEO quick-answer snippet.
   - Full bi-directional cross-linking between Tool Landing Page and Blog Guides.
   - **Scoped Site Header**: Always scope top nav CSS with `header.site-header`. Never use generic `header` type selector. Article titles must use `<div class="post-header">` with `position: static` and no background.

Full specification is maintained in [`.agents/rules/pexoris-standard.md`](file:///c:/xampp/htdocs/tools/.agents/rules/pexoris-standard.md).

## Official Live Tools Registry (8/100 Tools Completed)
| # | Tool Name | Slug | Binary Size | ZIP Size | SHA-256 Hash | Google Drive Download Link |
|---|---|---|---|---|---|---|
| **01** | **Pexoris FileUnlocker** | `file-unlocker` | 70 KB | 48 KB | `82df6a096cce9a71be84e0302b1f8cbb2c7bba4511516dd5ea5aa8612760f38b` | `https://drive.google.com/uc?export=download&id=1NHqChEV65pzdy_ZCo60NaK7uxU_wmkF-` |
| **02** | **Pexoris PortKiller** | `port-killer` | 78 KB | 58 KB | `e03503f56e9c9f7a7bb629910d5ae684532b4f6e6378e946a48f76fa9cce6d25` | `https://drive.google.com/uc?export=download&id=1vjBtdv2flluVJW8YMBVtNFTYFLpPKpIB` |
| **03** | **Pexoris DoH Switcher** | `doh-switcher` | 38 KB | 17 KB | `9ad64768039f10ad8b9408cd5c82dbffee426cf4df308a7731d0fed85d5ef395` | `https://drive.google.com/uc?export=download&id=1SmJHBFupPHx4hLOnu9ISylt_nbrJtx9l` |
| **04** | **Pexoris PrintFixer** | `print-fixer` | 73 KB | 50 KB | `59b56e001c1459c2d203f47af41bda738bdf5a39758d14bf63c11b87f23642d1` | `https://drive.google.com/uc?export=download&id=1us1wofg2_yPOx7FIofKxuX7jM3lsE-6W` |
| **05** | **Pexoris AIShield** | `ai-shield` | 53 KB | 30 KB | `9998d40b2975e0c52933a26c4b9c6c6e783af9b7598b43854630ba0fe9e585bb` | `https://drive.google.com/uc?export=download&id=1Lk_i7XyLGwSYCktaXudg__C_nQF4KYv2` |
| **06** | **Pexoris USBShield** | `usb-shield` | 59 KB | 34 KB | `b4710e51ad864a876f3a040d5a4f6c8a8434a9c99325419e35199bdc88f0c592` | `https://drive.google.com/uc?export=download&id=1jKcv_tS1te0zzu1ExlYeUfT8D97Vm2Xc` |
| **07** | **Pexoris ContextMenuEditor** | `context-menu-editor` | 49 KB | 30 KB | `36cc8cc8034958952fb1d9cb49d5a61dceb56613bfb527cbe2f682abcb48d1e8` | `https://drive.google.com/uc?export=download&id=1EFx4VOzqdNSafDdubZPcyoT8Bn9ZNfff` |
| **08** | **Pexoris ServiceOptimizer** | `service-optimizer` | 69 KB | 44 KB | `827f5675cf999177fe82ee5a6f93281d8baea6222a0b017366eea7485f5cb8ca` | `https://drive.google.com/uc?export=download&id=PENDING_GD_UPLOAD` |

## Strict Header & Footer Rules
1. **Header**: Must always use solid background (`background: #FFFFFF; box-shadow: 0 2px 10px rgba(0,0,0,0.04);`), NEVER semi-transparent `rgba()`, so scrolled content never bleeds through.
2. **Nav Links**: Top nav menu only contains section anchors (`← All Tools`, `Request a Tool`, `Features`, `Comparison`, `How to Use`, `FAQ`, Download CTA). Never put individual tool links in the top header.
3. **Footer Logo**: Always use `brand-logo.png` with fallback: `onerror="this.onerror=null; this.src='../../brand-logo.png';"`.
