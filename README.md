# PacePilot v1.4 — Windows FPS limiter / محدودکننده فریم ویندوز

**Developer / توسعه‌دهنده: Mr.HamidRouter**  
**Languages / زبان‌ها: فارسی 🇮🇷 · English 🇬🇧**

PacePilot is a free, independent **Windows desktop GUI for RTSS (RivaTuner Statistics Server)**. Its real FPS cap is provided by RTSS, not simulated by the UI. PacePilot is not affiliated with Guru3D, RTSS or FramePacer.

PacePilot یک برنامه مستقل ویندوزی برای کنترل محدودیت FPS از طریق RTSS است. محدودیت واقعی توسط RTSS اعمال می‌شود، نه اینکه فقط در رابط برنامه نمایش داده شود.

## ⬇️ Download EXE / دانلود فایل اجرایی

1. Go to the [GitHub Actions page](https://github.com/MrHamidRouter/PacePilot/actions/workflows/windows-build.yml).
2. Open the latest successful **Windows EXE build and self-test** run.
3. Download the artifact **PacePilot-Windows-x64-v1.4** and extract it.
4. Run **PacePilot.exe**. **No Visual Studio, build script or source files needed for the end user.**

برای نسخه آماده، از بخش Actions آخرین اجرای موفق را باز کنید و فایل خروجی **PacePilot-Windows-x64-v1.4** را دریافت کنید؛ سپس `PacePilot.exe` را مستقیم اجرا کنید. نیازی به اجرای CMD یا نصب ابزار برنامه‌نویسی نیست.

**Source ZIP / سورس:** To build locally, extract the repository and run `Start-PacePilot.cmd` once on Windows 10/11 x64. This produces `Bin\PacePilot.exe` and runs the GUI self-test. / برای ساخت از سورس روی ویندوز، یک بار `Start-PacePilot.cmd` را اجرا کنید.

## 🛠️ Automatic RTSS setup / نصب خودکار RTSS

If RTSS is absent when PacePilot opens, **PacePilot asks your permission** to download and install **`Guru3D.RTSS`** through Microsoft's Windows Package Manager (`winget`). Winget verifies the download against its package manifest. Approve the Windows UAC prompt if shown; PacePilot then rechecks the install and starts RTSS. Already-installed RTSS is reused without any reinstall.

در اولین اجرا، اگر RTSS نصب نباشد، PacePilot اجازه دانلود و نصب آن را می‌پرسد. با تأیید شما، بسته `Guru3D.RTSS` از طریق ابزار **winget** دانلود و نصب می‌شود. در صورت نمایش تأیید دسترسی Administrator، آن را بررسی و تأیید کنید. پس از نصب، برنامه RTSS را دوباره شناسایی و اجرا می‌کند. اگر RTSS از قبل موجود باشد، **هیچ نصب مجددی انجام نمی‌شود**.

If winget is unavailable, the user cancels, installation fails or Windows is offline, PacePilot offers the [official Guru3D download page](https://www.guru3d.com/download/rtss-rivatuner-statistics-server-download/) and lets users select an existing installation folder. **We never bundle or redistribute RTSS binaries, fetch them from unknown sites, or bypass installer hash verification.** Installation requires an internet connection, `winget` support and possibly Windows administrator approval.

اگر winget موجود نباشد، اینترنت قطع باشد یا نصب شکست بخورد، امکان رفتن به صفحه دانلود رسمی Guru3D و انتخاب دستی پوشه RTSS وجود دارد. فایل‌های RTSS در این مخزن بازنشر نمی‌شوند و بررسی امنیتی دانلود دور زده نمی‌شود.

## 🌐 Language / زبان

Choose **EN** or **فا** at the top of PacePilot's sidebar. The UI switches immediately and saves your preference under `%APPDATA%\PacePilot\settings.xml`. UI labels, buttons, profile messages, warnings and the tray menu support both languages. RTSS diagnostic logs remain in technical English for troubleshooting.

با انتخاب **فا** یا **EN** در ستون سمت چپ، زبان برنامه بلافاصله تغییر می‌کند و برای دفعات بعد ذخیره خواهد شد. دکمه‌ها، وضعیت‌ها، هشدارها و منوی کنار ساعت ترجمه شده‌اند. متن‌های فنی گزارش عیب‌یابی برای خوانایی پشتیبانی انگلیسی باقی می‌مانند.

## 🎮 Capabilities / امکانات

- Adjustable FPS cap (0 = unlimited), preset buttons, live RTSS FPS and frametime chart. / تنظیم FPS، دکمه‌های آماده، نمایش زنده FPS و نمودار زمان فریم.
- Game-specific profiles and global profile; automatic saved-game selection. / پروفایل مجزا برای هر بازی و پروفایل عمومی.
- Global hotkeys, tray icon, Windows startup and topmost window. / میانبرهای سراسری، آیکن کنار ساعت و اجرای خودکار.
- Detailed RTSS diagnostics and on-disk profile backup before fallback modification. / گزارش عیب‌یابی و نسخه پشتیبان از پروفایل RTSS در صورت نیاز.
- Protective warnings for supported anti-cheat cases; these are **not** comprehensive anti-cheat compatibility guarantees. / هشدارهای محافظتی برای برخی بازی‌های دارای ضدتقلب؛ سازگاری همه بازی‌ها تضمین نمی‌شود.

## ✅ Testing / تست

The Windows CI workflow compiles the C# WinForms app and runs a **read-only UI self-test** covering the English/Persian switch, layout, FPS preset selection and RTSS profile-file parser. It does **not** test actual RTSS installation or live gameplay; those require a real Windows machine with RTSS and a game. / تست خودکار شامل کامپایل، دوزبانه بودن، چیدمان و دکمه‌های FPS است؛ تست نصب واقعی RTSS و عملکرد درون بازی هنوز باید روی سیستم ویندوزی انجام شود.

## ⚠️ Notes / توجه

- Windows 10/11 x64 and .NET Framework 4.x required. / ویندوز ۱۰ یا ۱۱ نسخه ۶۴ بیتی و .NET Framework 4.x لازم است.
- Online games with anti-cheat may block or prohibit overlays/external limiters. / در بازی‌های آنلاین دارای ضدتقلب ممکن است محدودکننده خارجی مجاز نباشد.
- The software never installs RTSS without explicit user consent. / برنامه بدون اجازه کاربر RTSS را نصب نمی‌کند.

## 📄 License / مجوز

PacePilot source is MIT licensed; RTSS remains third-party freeware subject to its own terms. / کد PacePilot تحت MIT است و مجوز RTSS مستقل است.
