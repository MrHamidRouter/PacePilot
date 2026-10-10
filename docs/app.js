(() => {
  'use strict';
  const themeNames = ['Neon Teal','Windows Blue','Cyber Purple','Racing Red','Esports Lime','Graphite Gold'];
  const themeDescriptions = [
    'Cool teal clarity with a gaming-first dashboard.',
    'An open, minimal layout inspired by Windows 11.',
    'Futuristic deep indigo with electric purple detail.',
    'Vivid racing-inspired red, orange and dark carbon.',
    'Bold esports contrast with bright lime-green accents.',
    'Refined graphite panels with elegant gold highlights.'
  ];
  const previewImages = [
    '01-Neon-Teal.jpg','02-Windows-Blue.jpg','03-Cyber-Purple.jpg',
    '04-Racing-Red.jpg','05-Esports-Lime.jpg','06-Graphite-Gold.jpg'
  ];
  const palettes = [
    ['#0d1821','#083c48','#20beaa','#8bfcdf'],
    ['#f5f8ff','#ccdbe9','#378af7','#1e355d'],
    ['#100e38','#5b21b6','#a855f7','#23bafd'],
    ['#161316','#672019','#fb5d2e','#f4bc59'],
    ['#080c09','#263c23','#a8f617','#d5fb91'],
    ['#141414','#342b23','#e0b96a','#ffe9aa']
  ];
  const imageBase = 'https://raw.githubusercontent.com/MrHamidRouter/PacePilot/main/Docs/Previews/';
  const fa = {
    navThemes:'پوسته‌ها',navFeatures:'امکانات',navSetup:'شروع سریع',navDownload:'دریافت',
    eyebrow:'رایگان · متن‌باز · ویندوز ۱۰ و ۱۱',hero1:'کنترل FPS در اختیار تو.',hero2:'تسلط روی هر فریم.',
    heroDesc:'محدودسازی فریم برای تمام برنامه‌ها یا بازی‌های مشخص، بررسی FPS و زمان فریم واقعی، و شش رابط گرافیکی متنوع. مبتنی بر RTSS و بدون آمار ساختگی.',
    downloadZip:'دریافت برای ویندوز · ZIP',exploreThemes:'مشاهده شش پوسته',
    downloadNote:'ZIP را استخراج کن. اگر RTSS نصب نیست، فایل Setup-PacePilot.bat را اجرا کن؛ در غیر این صورت PacePilot.exe را باز کن.',
    statThemes:'پوسته رابط',statLang:'زبان',statEngine:'موتور محدودکننده',preview:'پیش‌نمایش طرح',sixDesigns:'شش ظاهر؛ یک موتور.',
    customization:'شخصی‌سازی',themeTitle:'رابط کاربری مطابق سلیقه تو.',
    themeDesc:'یک طرح انتخاب کن. برنامه ویندوز شش پوسته قابل‌تغییر دارد؛ این تصاویر طرح‌های مفهومی هستند و لزوماً تصویر دقیق اجرای برنامه نیستند.',
    themeCyberDescription:'بنفش تیره آینده‌نگر با جزئیات نئونی.',
    themeTeal:'فیروزه‌ای',themeBlue:'سفید و آبی',themeCyber:'بنفش سایبری',themeRed:'قرمز مسابقه‌ای',themeLime:'سبز گیمینگ',themeGold:'مشکی و طلایی',
    builtForPlay:'برای بازی‌های کامپیوتری',featuresTitle:'کنترل دقیق، بدون شلوغی.',featuresDesc:'ابزارهای کاربردی کنترل فریم، بدون ادعای داشتن موتور اختصاصی FramePacer.',
    feature1Title:'محدودیت دقیق FPS',feature1Body:'حد فریم پیش‌فرض یا سفارشی با استفاده از RivaTuner Statistics Server.',
    feature2Title:'پروفایل اختصاصی بازی',feature2Body:'انتخاب فایل اجرایی یا بازی در حال اجرا و ذخیره سقف فریم جداگانه.',
    feature3Title:'آمار واقعی',feature3Body:'خواندن FPS و زمان فریم از RTSS در صورت پشتیبانی؛ اطلاعات ناموجود شبیه‌سازی نمی‌شود.',
    feature4Title:'کلیدهای میانبر',feature4Body:'فعال‌سازی و تغییر محدودیت FPS هنگام بازی با میانبرهای سراسری.',
    feature5Title:'فارسی و انگلیسی',feature5Body:'تغییر زبان و پوسته در برنامه همراه با ذخیره انتخاب‌ها.',
    feature6Title:'نصب آسان RTSS',feature6Body:'شناسایی RTSS و نصب خودکار با winget، فقط در صورت نیاز و با اجرای فایل راه‌انداز.',
    quickStart:'شروع سریع',setupTitle:'از دانلود ZIP تا اجرای بازی، فقط سه مرحله.',
    step1Title:'دریافت',step1Body:'ZIP آماده ویندوز را از قسمت Releases پروژه دریافت کن.',
    step2Title:'اجرا',step2Body:'فایل را استخراج کن و PacePilot.exe را اجرا کن؛ هیچ کامپایلی لازم نیست.',
    step3Title:'شروع بازی',step3Body:'اگر RTSS موجود نیست، Setup-PacePilot.bat را یک بار اجرا کن؛ سپس بازی و FPS را انتخاب کن.',
    readyLabel:'آماده‌ای؟',ctaTitle:'کنترل فریم از همین‌جا شروع می‌شود.',ctaDesc:'رایگان و متن‌باز؛ ساخته شده توسط Mr.HamidRouter.',
    buildFallback:'تاریخچه ساخت / دانلود جایگزین ↗',noteTitle:'توجه:',
    noteText:'PacePilot پروژه مستقلی مبتنی بر RTSS است و وابسته به FramePacer، NVIDIA یا Guru3D نیست. موتور اختصاصی FramePacer، فناوری Reflex و DLSS Frame Generation در آن پیاده‌سازی نشده‌اند. پشتیبانی به بازی و سیستم ضدتقلب بستگی دارد.',
    reportIssue:'ثبت مشکل',footerLicense:'مجوز MIT · نرم‌افزار دسکتاپ ویندوز'
  };
  const themeNameFA = ['فیروزه‌ای','سفید و آبی','بنفش سایبری','قرمز مسابقه‌ای','سبز گیمینگ','مشکی و طلایی'];
  const themeDescFA = ['فیروزه‌ای نئونی، ویژه کاربران حرفه‌ای.', 'روشن، تمیز و الهام‌گرفته از ویندوز ۱۱.', 'بنفش تیره آینده‌نگر با جزئیات نئونی.', 'قرمز و نارنجی پرانرژی با حس مسابقه.', 'کنتراست بالا با سبز لیمویی جذاب.', 'ترکیب مشکی گرافیتی و طلایی لوکس.'];
  let theme = 2;
  let language = 'en';
  try { const saved = Number(localStorage.getItem('pacepilot-site-theme')); if (Number.isInteger(saved) && saved >= 0 && saved < 6) theme = saved; const sl=localStorage.getItem('pacepilot-site-language'); if(sl==='fa'||sl==='en')language=sl; } catch(e){}
  const $ = id => document.getElementById(id);
  const themeButtons = [...document.querySelectorAll('[data-theme]')];
  function setTheme(i) {
    theme = (i + 6) % 6;
    const preview = imageBase + previewImages[theme];
    if ($('themeImage')) { $('themeImage').src=preview; $('themeImage').alt=themeNames[theme]+' UI design concept'; }
    if ($('heroImage')) { $('heroImage').src=preview; $('heroImage').alt=themeNames[theme]+' UI design concept'; }
    $('themeNumber').textContent = String(theme+1).padStart(2,'0')+' / 06';
    $('themeName').textContent = language==='fa'?themeNameFA[theme]:themeNames[theme];
    $('heroThemeLabel').textContent = language==='fa'?themeNameFA[theme]:themeNames[theme];
    $('themeDescription').textContent = language==='fa'?themeDescFA[theme]:themeDescriptions[theme];
    $('themePalette').replaceChildren(...palettes[theme].map(color => { const el=document.createElement('i'); el.style.background=color; return el; }));
    themeButtons.forEach(btn=>{const selected=Number(btn.dataset.theme)===theme;btn.classList.toggle('selected',selected);btn.setAttribute('aria-pressed',String(selected));});
    try{localStorage.setItem('pacepilot-site-theme',String(theme));}catch(e){}
  }
  function setLanguage(lang){
    language=lang;document.documentElement.lang=lang;document.documentElement.dir=lang==='fa'?'rtl':'ltr';
    document.querySelectorAll('[data-i18n]').forEach(el=>{const key=el.dataset.i18n;if(lang==='fa'&&fa[key])el.textContent=fa[key];else if(el.dataset.english)el.textContent=el.dataset.english;});
    $('languageToggle').textContent=lang==='fa'?'EN / FA':'FA / EN';
    $('languageToggle').setAttribute('aria-label',lang==='fa'?'Switch to English':'تغییر به فارسی');
    setTheme(theme);try{localStorage.setItem('pacepilot-site-language',lang)}catch(e){}
  }
  document.querySelectorAll('[data-i18n]').forEach(el=>el.dataset.english=el.textContent);
  themeButtons.forEach(btn=>btn.addEventListener('click',()=>setTheme(Number(btn.dataset.theme))));
  $('previousTheme').addEventListener('click',()=>setTheme(theme-1));
  $('nextTheme').addEventListener('click',()=>setTheme(theme+1));
  $('languageToggle').addEventListener('click',()=>setLanguage(language==='en'?'fa':'en'));
  setLanguage(language);
})();