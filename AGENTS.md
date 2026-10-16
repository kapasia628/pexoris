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

## Official Live Tools Registry (33/100 Tools Completed)
| # | Tool Name | Slug | Binary Size | ZIP Size | SHA-256 Hash | Google Drive Download Link |
|---|---|---|---|---|---|---|
| **01** | **Pexoris FileUnlocker** | `file-unlocker` | 70 KB | 48 KB | `82df6a096cce9a71be84e0302b1f8cbb2c7bba4511516dd5ea5aa8612760f38b` | `https://drive.google.com/uc?export=download&id=1NHqChEV65pzdy_ZCo60NaK7uxU_wmkF-` |
| **02** | **Pexoris PortKiller** | `port-killer` | 78 KB | 58 KB | `e03503f56e9c9f7a7bb629910d5ae684532b4f6e6378e946a48f76fa9cce6d25` | `https://drive.google.com/uc?export=download&id=1vjBtdv2flluVJW8YMBVtNFTYFLpPKpIB` |
| **03** | **Pexoris DoH Switcher** | `doh-switcher` | 38 KB | 17 KB | `9ad64768039f10ad8b9408cd5c82dbffee426cf4df308a7731d0fed85d5ef395` | `https://drive.google.com/uc?export=download&id=1SmJHBFupPHx4hLOnu9ISylt_nbrJtx9l` |
| **04** | **Pexoris PrintFixer** | `print-fixer` | 73 KB | 50 KB | `59b56e001c1459c2d203f47af41bda738bdf5a39758d14bf63c11b87f23642d1` | `https://drive.google.com/uc?export=download&id=1us1wofg2_yPOx7FIofKxuX7jM3lsE-6W` |
| **05** | **Pexoris AIShield** | `ai-shield` | 53 KB | 30 KB | `9998d40b2975e0c52933a26c4b9c6c6e783af9b7598b43854630ba0fe9e585bb` | `https://drive.google.com/uc?export=download&id=1Lk_i7XyLGwSYCktaXudg__C_nQF4KYv2` |
| **06** | **Pexoris USBShield** | `usb-shield` | 59 KB | 34 KB | `b4710e51ad864a876f3a040d5a4f6c8a8434a9c99325419e35199bdc88f0c592` | `https://drive.google.com/uc?export=download&id=1jKcv_tS1te0zzu1ExlYeUfT8D97Vm2Xc` |
| **07** | **Pexoris ContextMenuEditor** | `context-menu-editor` | 49 KB | 30 KB | `36cc8cc8034958952fb1d9cb49d5a61dceb56613bfb527cbe2f682abcb48d1e8` | `https://drive.google.com/uc?export=download&id=1EFx4VOzqdNSafDdubZPcyoT8Bn9ZNfff` |
| **08** | **Pexoris ServiceOptimizer** | `service-optimizer` | 69 KB | 44 KB | `827f5675cf999177fe82ee5a6f93281d8baea6222a0b017366eea7485f5cb8ca` | `https://drive.google.com/uc?export=download&id=1ZEuYBVBT2AKpZ7XpzbcGkahuSya5UZBP` |
| **09** | **Pexoris HostsManager** | `hosts-manager` | 57 KB | 36 KB | `cb5b1d28889247c6ff18d852fdf2597f967a27f4834d3821ce9b04cec64eb556` | `https://drive.google.com/uc?export=download&id=1cbsrisYM2H2wRRcPdcwXAwC_aMJFqmHu` |
| **10** | **Pexoris TempCleaner** | `temp-cleaner` | 56 KB | 37 KB | `86d70f11a3188d10b0a66956529ea5e97f4f7735a1c9dbeddc34a684dd54bf83` | `https://drive.google.com/uc?export=download&id=14GAkL1D6kB9Pjyn_qXWTo9uc4KEUoOr6` |
| **11** | **Pexoris StartupInspector** | `startup-inspector` | 57 KB | 33 KB | `22fd202a60bbe8a2523f4a8b5719c76160852933ea031ca2fd34c4406b720a0d` | `https://drive.google.com/uc?export=download&id=1ZjrUfr54jGmpPd9oJio15Jsrk7EFnU36` |
| **12** | **Pexoris DefenderToggle** | `defender-toggle` | 55 KB | 34 KB | `0c0f65b7e9007a37d00b9c017cc98eb41ec1cf2eb94dd30a6e8083184a4eb445` | `https://drive.google.com/uc?export=download&id=1hfwkzQhgZMpFy7-48ppOlF0MUXIBDWSo` |
| **13** | **Pexoris DNSFlusher** | `dns-flusher` | 62 KB | 40 KB | `7487381d2b792f4b5f3d7ccf0a7e844019f028f414769ec7514281b51018e581` | `https://drive.google.com/uc?export=download&id=1SQCWEifK1tDpYkEgoJIHeLRS9KrAoTRH` |
| **14** | **Pexoris RamTrimmer** | `ram-trimmer` | 58 KB | 36 KB | `bebaa40676eb39a6b3765d5dae52d228e7c1d779511edbb3b631c37e4ba1f2be` | `https://drive.google.com/uc?export=download&id=1_pd3rtHSuh5oDF1sW0t2n5Mjx7XDHWsJ` |
| **15** | **Pexoris ExifStripper** | `exif-stripper` | 53 KB | 34 KB | `dcab2739eb121b91b13ca163dfe13fafa0841efe9b7a40aa8ec6aea68ad4077d` | `https://drive.google.com/uc?export=download&id=1um9SbbFSx5IJ2SuIzqXzyqPlxuK2SwDS` |
| **16** | **Pexoris IconCacheRebuilder** | `icon-cache-rebuilder` | 44 KB | 28 KB | `dc2ac33a0ae80271c1824851480fce0435b732a934cbbce79dc15e69204ff3f4` | `https://drive.google.com/uc?export=download&id=1BeSTVCd8Y-2HEB0yXx3c0vWsqr9Ej5Kq` |
| **17** | **Pexoris WinUpdateReset** | `winupdate-reset` | 51 KB | 31 KB | `ec99ef3fb73c7ad79756adcc8d3b3d19d589ccc70874217fb1d74196cef14289` | `https://drive.google.com/uc?export=download&id=1_Nm5yAN3dTY2hBmDdOi1SQZWYV2_1UnR` |
| **18** | **Pexoris WifiKeyRevealer** | `wifi-key-revealer` | 48 KB | 30 KB | `98da0ce232a31f2122ac83310407dfec6e5483fdaf2d109ea52799f8fcf42751` | `https://drive.google.com/uc?export=download&id=1ItRU8XZ6CAR9NyY-DJtXQVngajRt8R1E` |
| **19** | **Pexoris DiskCleaner** | `disk-cleaner` | 50 KB | 33 KB | `3e8ae3ba56087c6fa4b84f760d1bec13cad5501375158b4eca4318ccb9c13c10` | `https://drive.google.com/uc?export=download&id=1gqndT3bEwkcN9KghbetyfUj0oig77wjk` |
| **20** | **Pexoris AppUninstaller** | `app-uninstaller` | 51 KB | 32 KB | `d53e0a731cf94a5ebec2481b8d340fe8c3d95db41a9799090505b76466cf5b3f` | `https://drive.google.com/uc?export=download&id=1lXUG0xfZfZS4CaLdmlDLNCwfwQO7KY_v` |
| **21** | **Pexoris DuplicateFinder** | `duplicate-finder` | 56 KB | 34 KB | `c4bcb5798bdc53008ddaf9624b82099064f8c16cb4a72bae93bf209f9159fe15` | `https://drive.google.com/uc?export=download&id=1oY4iSq1oYICy5EtriWtmpt5xmZ2pJFZF` |
| **22** | **Pexoris PrivacyShield** | `privacy-shield` | 65 KB | 37 KB | `9630b2589125cc47453292e69284867a43f07dcd467209fe2a7258c58c6a84ab` | `https://drive.google.com/uc?export=download&id=1oAT8UmMNAyrobJmUBzoL5gxTbesN8gZ8` |
| **23** | **Pexoris ProcessTamer** | `process-tamer` | 50 KB | 34 KB | `719a5fe88cd55a08c5002cb935950cd1b850e44995b67728d3bf123075479d6d` | `https://drive.google.com/uc?export=download&id=12Z3ONrnpWoyBeQHmCkfZAUKPWXanAZo0` |
| **24** | **Pexoris FontCacheRebuilder** | `font-cache-rebuilder` | 52 KB | 36 KB | `afe722889f5bd05c055bc1ad5f3d94bf40cd6cada2e7b677a861bab7e9e9f991` | `https://drive.google.com/uc?export=download&id=1PQp5eXXgiouH-fHGkAIoFnU7MaY9852K` |
| **25** | **Pexoris BatteryOptimizer** | `battery-optimizer` | 52 KB | 34 KB | `1e4e10f04707cc705d60ec6704488216856d43582c30c30820bcfd4a8efa7db3` | `https://drive.google.com/uc?export=download&id=1HvVJzxtS3afaVRqJeUxJTkmOmBX5BiR_` |
| **26** | **Pexoris TelemetryBlocker** | `telemetry-blocker` | 56 KB | 37 KB | `426cec2ae87b2d3fcbcf34371a31539bf434cdf730a3cad360cdccc02bf14714` | `https://drive.google.com/uc?export=download&id=1dVCRO6HkDjZps5_oUXeoLufjAO8sbYPh` |
| **27** | **Pexoris NetworkReset** | `network-reset` | 57 KB | 37 KB | `53e07848628778ef83078e0c9761385f885dd3ff78d90756c15b90637a1883b8` | `https://drive.google.com/uc?export=download&id=1C9u1j6moBnOe3nvzH92yRh7N4q2B3Fez` |
| **28** | **Pexoris BluetoothFixer** | `bluetooth-fixer` | 60 KB | 39 KB | `c1e352dff704d4e3ee34b67d34699266d65412e45384bcb6f72f19028bd33cb3` | `https://drive.google.com/uc?export=download&id=1swgiAfhupOc09OLQK3VU9JtvLmXJL7h5` |
| **29** | **Pexoris AudioFixer** | `audio-fixer` | 59 KB | 39 KB | `4bd062a69de0a86298f81d194128311059535362a0e542e3034edc6c7f35d9a2` | `https://drive.google.com/uc?export=download&id=1SzmgNT9AsTG-sFO2Pe0d_TRZXqlYmpUt` |
| **30** | **Pexoris SafeModeLauncher** | `safemode-launcher` | 66 KB | 47 KB | `f98a88848d1edbdb1d3ac85098bab1dd07ad5b9e19a9f28f4aac788db0dc9146` | `https://drive.google.com/uc?export=download&id=1WyIkZkgbuZ5X9vPpL9Qr8uFIQohxdZaG` |
| **31** | **Pexoris FirewallBlocker** | `firewall-blocker` | 82 KB | 52 KB | `da7b6ee255e51ee89c3cb1181268d26f4e786c112fa686fd39f5c6e24b6b7b89` | `https://drive.google.com/uc?export=download&id=1pGdZlMqTtUpAlKIurkNgUJyefd_RoQ-N` |
| **32** | **Pexoris ShortcutDoctor** | `shortcut-doctor` | 63 KB | 40 KB | `e2c1eae1c19a02f0e36813d505544cde2c13b4476544dfdc95efc4662b7540e3` | `https://drive.google.com/uc?export=download&id=1ZTtEVqRYkLSuaQPTkHsvl4Q6xDGu42Wz` |
| **33** | **Pexoris StartupTimer** | `startup-timer` | 63 KB | 41 KB | `1c98d99b642eba1521c951824c1730574d58ff5f37797dae48071eee14bf6a5b` | *Pending Upload* |

## Strict Header & Footer Rules
1. **Header**: Must always use solid background (`background: #FFFFFF; box-shadow: 0 2px 10px rgba(0,0,0,0.04);`), NEVER semi-transparent `rgba()`, so scrolled content never bleeds through.
2. **Nav Links**: Top nav menu only contains section anchors (`← All Tools`, `Request a Tool`, `Features`, `Comparison`, `How to Use`, `FAQ`, Download CTA). Never put individual tool links in the top header.
3. **Footer Logo**: Always use `brand-logo.png` with fallback: `onerror="this.onerror=null; this.src='../../brand-logo.png';"`.
