# PacePilot Studio 2.0 | نرم‌افزار کنترل فریم ویندوز

**Developer / توسعه‌دهنده: Mr.HamidRouter** · **Windows 10/11 x64** · **English / فارسی** · **MIT License**

PacePilot is an independent Windows FPS-limiter front end powered by **RivaTuner Statistics Server (RTSS)**. It offers **six fully functional dashboard designs**, per-game/global FPS caps, RTSS live telemetry, a measured frametime graph, persistent game profiles, global hotkeys, tray controls, and assisted RTSS installation.

PacePilot یک نرم‌افزار مستقل ویندوز برای کنترل FPS با استفاده از RTSS است. **شش طرح گرافیکی واقعی و قابل‌تغییر**، تنظیم محدودیت فریم برای هر بازی، نمودار زنده مبتنی بر اندازه‌گیری RTSS، پروفایل‌های ذخیره‌شده، میانبرهای سراسری، اجرای کنار ساعت و نصب هدایت‌شده RTSS دارد.

## Download / دریافت نسخه اجرایی

Go to [GitHub Actions](https://github.com/MrHamidRouter/PacePilot/actions/workflows/windows-build.yml), open a **successful** Windows workflow run, download the `PacePilot-Windows-x64-v2.0` artifact, extract it and run the single `PacePilot.exe` file. Build artifacts may be unavailable until Windows CI succeeds. If none are available, clone the source and run `Start-PacePilot.cmd` on Windows.

برای دانلود فایل EXE، وارد [Actions](https://github.com/MrHamidRouter/PacePilot/actions/workflows/windows-build.yml) شوید، آخرین اجرای **موفق** را باز کنید و فایل `PacePilot-Windows-x64-v2.0` را از قسمت Artifacts دریافت کنید. پس از استخراج، فقط `PacePilot.exe` را اجرا کنید. اگر خروجی آماده موجود نیست، سورس را دانلود و فایل `Start-PacePilot.cmd` را روی ویندوز اجرا کنید.

The EXE is a .NET Framework 4.x desktop app, not a self-extracting installer. RTSS must be installed separately (PacePilot offers installation on first run).

## Six fully functional themes / شش طرح قابل‌استفاده

| Theme / طرح | Interface layout / چیدمان |
| --- | --- |
| Neon Teal / فیروزه‌ای | Wide game selector, main limit & statistics, full-width graph |
| Windows Blue / سفید آبی | Spacious stacked Windows 11-inspired dashboard |
| Cyber Purple / بنفش | Dedicated game column, FPS area and telemetry cards |
| Racing Red / قرمز مسابقه‌ای | Hero banner, performance grid and chart |
| Esports Lime / سبز گیمینگ | Competitive layout with side-by-side configuration |
| Graphite Gold / مشکی طلایی | Two-column step-by-step controls and telemetry |

Choose a theme and switch between **English** and **فارسی** from the left sidebar. Both selections are saved in `%APPDATA%\PacePilot\settings.xml` and restored on the next launch. All six skins use the **same real FPS/RTSS backend** and the same stored profiles. / زبان و طرح برنامه از پنل کناری قابل تغییر و ذخیره هستند.

Reference images are in [`Docs/Previews`](Docs/Previews); they are concept illustrations and may have minor differences from actual native Windows controls.

## Main features / امکانات

- **FPS caps / محدودکننده فریم:** 30, 40, 60, 90, 120, 144, 165, 240 and custom 0–1000 FPS, 0 = unlimited.
- **Global/per-game profiles / پروفایل اختصاصی هر بازی:** select a game executable or browse visible running processes; remember game-specific FPS values.
- **Measured monitoring / پایش واقعی:** RTSS shared-memory FPS/frametime samples. Missing telemetry shows `--`, not invented readings.
- **Runtime changes / تغییر حین بازی:** apply a new FPS setting via RTSS without reopening PacePilot. A particular game may still restrict external hooks.
- **Hotkeys / میانبرها:** `Alt + Shift + L` toggle; `Alt + Shift + Up/Down` adjust FPS by 1.
- **Tray & startup / کنار ساعت و استارت‌آپ:** minimize to tray, launch on Windows startup, always on top.
- **Anti-cheat precautions / هشدار ضدتقلب:** blocks known protected processes and warns before global changes. Detection cannot guarantee compatibility.
- **RTSS management / مدیریت RTSS:** search for an existing installation, offer consent-based `winget` installation of `Guru3D.RTSS`, manual folder selection, official download fallback, diagnostics.
- **Tests / تست:** self-test for theme layouts, responsive layout, presets, language switching and RTSS configuration parser. CI on Windows compiles the project and runs the test. Real-world gameplay and RTSS interaction require a Windows PC.

## RTSS setup / نصب RTSS

If RTSS is missing, PacePilot asks before doing anything. If the user approves, Windows Package Manager runs `winget install --id Guru3D.RTSS --exact --source winget --silent --accept-package-agreements --accept-source-agreements --disable-interactivity` with a Windows UAC prompt when necessary. `winget` verifies its installer package according to its manifest. If installation fails or is refused, [Guru3D's RTSS download page](https://www.guru3d.com/download/rtss-rivatuner-statistics-server-download/) is offered. RTSS binaries are **not** redistributed with PacePilot.

در نبود RTSS، برنامه ابتدا اجازه می‌گیرد؛ پس از تأیید کاربر با `winget` تلاش به نصب می‌کند. در صورت شکست، مسیر دانلود رسمی معرفی می‌شود. هیچ فایل اجرایی RTSS در بسته بازنشر نمی‌شود.

## Build locally / ساخت از سورس

Double-click `Start-PacePilot.cmd` on Windows 10/11 x64. The script finds Microsoft's .NET Framework C# compiler, builds a uniquely named EXE to avoid file-locking, executes `--selftest`, and launches the verified build. Files in `Docs/` are optional helpers; users need only the single launcher. The workflow produces `Bin/PacePilot.exe` for the Actions artifact.

روی ویندوز ۱۰/۱۱ نسخه ۶۴ بیتی، فایل `Start-PacePilot.cmd` را اجرا کنید. این فایل ساخت برنامه و تست را یکجا انجام می‌دهد. نیازی به اجرای چند فایل CMD نیست.

## Scope and limitations / محدودیت‌ها

PacePilot is **not a reimplementation of the proprietary FramePacer engine** and is not affiliated with FramePacer, pacerio, RTSS or Guru3D. The app relies on separately installed RTSS for actual FPS limiting and monitoring. **NVIDIA Reflex, DLSS Frame Generation / Dynamic MFG, ReShade injection, controller-driven navigation, proprietary multi-API injection and Microsoft Store protected-game interoperability are not implemented.** Do not interpret the UI mockups as evidence of these features. Smoothness depends on the game's engine, RTSS compatibility, and your hardware. An FPS cap cannot eliminate all stuttering.

PacePilot موتور اختصاصی FramePacer را کپی نمی‌کند. قابلیت‌های Reflex و DLSS Frame Generation و موارد وابسته به موتور اختصاصی **پیاده‌سازی نشده‌اند**. ادعای حذف کامل لگ یا سازگاری با همه بازی‌های آنلاین وجود ندارد. در بازی‌های دارای ضدتقلب از محدودکننده خارجی بدون بررسی قوانین بازی استفاده نکنید.

## License

PacePilot source: MIT. Third-party RTSS: separately licensed by its publisher. Theme concept mockups are visual previews, not executable interfaces.
