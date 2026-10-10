// PacePilot Studio: six functional layouts, original Windows Forms UI.
// Developer: Mr.HamidRouter. No kernel hooks or anti-cheat bypasses.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Text;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PacePilot
{
    public sealed class StudioTheme
    {
        public readonly string Id, Display;
        public readonly Color Bg, Panel, Sidebar, Border, Ink, Secondary, Accent, AccentInk, Graph;
        public readonly int Layout;
        public readonly bool Light;
        public StudioTheme(string id, string display, int layout, bool light, string bg, string panel, string sidebar, string border, string ink, string secondary, string accent, string accentInk, string graph)
        {
            Id=id; Display=display; Layout=layout; Light=light;
            Bg=ColorTranslator.FromHtml(bg); Panel=ColorTranslator.FromHtml(panel); Sidebar=ColorTranslator.FromHtml(sidebar);
            Border=ColorTranslator.FromHtml(border); Ink=ColorTranslator.FromHtml(ink); Secondary=ColorTranslator.FromHtml(secondary);
            Accent=ColorTranslator.FromHtml(accent); AccentInk=ColorTranslator.FromHtml(accentInk); Graph=ColorTranslator.FromHtml(graph);
        }
        public static readonly StudioTheme[] All = {
            new StudioTheme("teal","01  •  Neon Teal",0,false,"#08121B","#111F2A","#07141C","#244451","#EFFBFF","#9BAFBC","#41E4CF","#062727","#42DDCD"),
            new StudioTheme("light","02  •  Windows Blue",1,true,"#EFF5FF","#FFFFFF","#F6F9FF","#D6E0F0","#162C50","#6A7F9B","#116CFF","#FFFFFF","#278DFF"),
            new StudioTheme("purple","03  •  Cyber Purple",2,false,"#090E29","#151940","#101434","#343A75","#F4F1FF","#B3ABD8","#9858FF","#FFFFFF","#B95BFF"),
            new StudioTheme("racing","04  •  Racing Red",3,false,"#100E12","#1D181C","#160E13","#513031","#FFF2ED","#B7A6A5","#FF523A","#210A08","#FF704B"),
            new StudioTheme("lime","05  •  Esports Lime",4,false,"#0B0C0C","#191B18","#121410","#373F2A","#F6FDEA","#AEB6A1","#B6FF24","#172007","#BBFF3D"),
            new StudioTheme("gold","06  •  Graphite Gold",5,false,"#11100F","#211D19","#181512","#514431","#FFF6DE","#BBAF97","#F2C662","#281B0B","#EEC36B")
        };
        public static StudioTheme Find(string id) { return All.FirstOrDefault(x=>x.Id==id) ?? All[0]; }
    }

    // Artwork from the six approved mockups is embedded into the EXE.
    internal static class ThemeVisuals
    {
        static readonly Dictionary<string,Image> Cache=new Dictionary<string,Image>();
        public static Image Get(string id)
        {
            lock(Cache)
            {
                if(Cache.ContainsKey(id))return Cache[id];
                try
                {
                    using(Stream src=typeof(StudioForm).Assembly.GetManifestResourceStream("PacePilot.UI."+id))
                    {
                        if(src==null)return null;
                        using(Image image=Image.FromStream(src)) Cache[id]=new Bitmap(image);
                    }
                    return Cache[id];
                } catch {return null;}
            }
        }
        public static Rectangle Crop(string id,bool side,Image image)
        {
            if(image==null)return Rectangle.Empty;
            double x=.60,y=.05,w=.39,h=.20;
            if(id=="teal"){x=side?0:.04;y=side?.42:.42;w=side?.19:.22;h=side?.35:.25;}
            if(id=="light"){x=side?.80:.65;y=side?.50:.66;w=side?.19:.33;h=side?.43:.33;}
            if(id=="purple"){x=side?0:.67;y=side?.52:.10;w=side?.19:.32;h=side?.45:.20;}
            if(id=="racing"){x=side?0:.50;y=side?.46:.01;w=side?.18:.48;h=side?.43:.20;}
            if(id=="lime"){x=side?0:.01;y=side?.52:.51;w=side?.19:.19;h=side?.45:.36;}
            if(id=="gold"){x=side?0:.43;y=side?.58:0;w=side?.19:.55;h=side?.33:.12;}
            return new Rectangle((int)(x*image.Width),(int)(y*image.Height),
                Math.Max(1,(int)(w*image.Width)),Math.Max(1,(int)(h*image.Height)));
        }
        public static Bitmap Sidebar(StudioTheme theme)
        {
            Image image=Get(theme.Id);if(image==null)return null;
            Bitmap bitmap=new Bitmap(360,900);
            using(Graphics graphics=Graphics.FromImage(bitmap))
            {
                graphics.Clear(theme.Sidebar);
                graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image,new Rectangle(0,250,360,645),Crop(theme.Id,true,image),GraphicsUnit.Pixel);
                using(LinearGradientBrush overlay=new LinearGradientBrush(new Rectangle(0,0,360,900),
                    Color.FromArgb(theme.Light?245:244,theme.Sidebar),
                    Color.FromArgb(theme.Light?195:112,theme.Sidebar),90))
                    graphics.FillRectangle(overlay,0,0,360,900);
            }
            return bitmap;
        }
    }
    public sealed class StudioCard : Panel
    {
        public StudioTheme Theme;
        public StudioCard(StudioTheme t) { Theme=t; DoubleBuffered=true; BackColor=t.Panel; Margin=Padding.Empty; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(Pen p=new Pen(Theme.Border)) e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);
        }
    }
    public sealed class ThemeCard : StudioCard
    {
        public Image Artwork;
        public Rectangle CropRectangle;
        public ThemeCard(StudioTheme theme):base(theme){}
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if(Artwork==null)return;
            e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(Artwork,new Rectangle(0,0,Width,Height),CropRectangle,GraphicsUnit.Pixel);
            using(LinearGradientBrush shade=new LinearGradientBrush(new Rectangle(0,0,Math.Max(1,Width),Math.Max(1,Height)),
                Color.FromArgb(245,Theme.Panel),Color.FromArgb(70,Theme.Panel),0f))
                e.Graphics.FillRectangle(shade,0,0,Width,Height);
        }
    }
    public sealed class StudioGraph : Control
    {
        public StudioTheme Theme;
        public readonly List<float> Values = new List<float>();
        public int Target=60;
        public StudioGraph(StudioTheme t) { Theme=t; DoubleBuffered=true; BackColor=t.Panel; }
        public void Push(float v) { Values.Add(v); if(Values.Count>180) Values.RemoveAt(0); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g=e.Graphics; g.SmoothingMode=SmoothingMode.AntiAlias;
            RectangleF r=new RectangleF(42,13,Math.Max(1,Width-56),Math.Max(1,Height-35));
            float target=1000f/Math.Max(1,Target), maximum=Math.Max(35,target*2.6f);
            foreach(float v in Values) if(v>maximum && !float.IsNaN(v)) maximum=Math.Min(200,v*1.13f);
            using(Pen line=new Pen(Theme.Border))
            {
                for(int i=0;i<=4;i++)
                {
                    float y=r.Y+(r.Height*i/4f);
                    g.DrawLine(line,r.Left,y,r.Right,y);
                }
            }
            using(Font f=new Font("Segoe UI",8)) using(Brush br=new SolidBrush(Theme.Secondary))
            {
                g.DrawString(maximum.ToString("0")+" ms",f,br,1,9);
                g.DrawString("0 ms",f,br,5,Math.Max(0,Height-30));
                g.DrawString("60s",f,br,r.Left,Math.Max(0,Height-19));
                g.DrawString("now",f,br,Math.Max(0,Width-35),Math.Max(0,Height-19));
            }
            float ty=r.Bottom-target/maximum*r.Height;
            using(Pen refPen=new Pen(Theme.Secondary,1)) { refPen.DashStyle=DashStyle.Dash; g.DrawLine(refPen,r.Left,ty,r.Right,ty); }
            if(Values.Count<2) return;
            List<PointF> seq=new List<PointF>();
            float spacing=r.Width/Math.Max(179,Values.Count-1);
            for(int i=0;i<Values.Count;i++)
            {
                float value=Values[i];
                if(float.IsNaN(value)||value<=0) { PaintLine(g,seq); seq.Clear(); continue; }
                seq.Add(new PointF(r.Right-(Values.Count-i-1)*spacing,r.Bottom-Math.Min(value,maximum)*r.Height/maximum));
            }
            PaintLine(g,seq);
        }
        void PaintLine(Graphics g,List<PointF> seq)
        {
            if(seq.Count<2)return;
            using(Pen glow=new Pen(Color.FromArgb(37,Theme.Graph),6)) g.DrawLines(glow,seq.ToArray());
            using(Pen pen=new Pen(Theme.Graph,2)) g.DrawLines(pen,seq.ToArray());
        }
    }

    public sealed class StudioForm : Form
    {
        readonly bool testing;
        readonly AppSettings settings;
        readonly RtssControl rtss=new RtssControl();
        StudioTheme theme;
        string currentTarget="Global", currentPage="dashboard";
        bool rebuilding=false, finishing=false, prompted=false, installing=false;
        bool refreshPending=false;
        bool gameScanPending=false,updateCheckPending=false;
        int applied=0;
        int lastNonZero=60;
        TableLayoutPanel shell;
        Panel side, viewport, canvas;
        ComboBox themeBox, langBox, targetBox;
        NumericUpDown fpsBox;
        Button apply, disable;
        Label liveValue, frameValue, rtssValue, hzValue, statusLabel;
        StudioGraph graph;
        List<Button> presets = new List<Button>();
        readonly List<Control> pageControls=new List<Control>();
        NotifyIcon tray;
        System.Windows.Forms.Timer timer;
        const string RtssOfficial="https://www.guru3d.com/download/rtss-rivatuner-statistics-server-download/";
        bool Fa { get { return settings.LanguageCode=="fa"; } }
        string L(string en,string fa) { return Fa?fa:en; }
        string CurrentTheme { get { return String.IsNullOrWhiteSpace(settings.ThemeName)?"teal":settings.ThemeName; } }
        public StudioForm(bool startInTray,bool test)
        {
            testing=test; settings=Store.Load();
            if(settings.LanguageCode!="fa")settings.LanguageCode="en";
            if(settings.Games==null)settings.Games=new List<GameProfile>();
            theme=StudioTheme.Find(CurrentTheme);
            ClientSize=new Size(1340,865); MinimumSize=new Size(950,665);
            Font=new Font("Segoe UI",9f); StartPosition=FormStartPosition.CenterScreen;
            AutoScaleMode=AutoScaleMode.Dpi;
            Text="PacePilot Studio v2.1  |  Mr.HamidRouter";
            try { Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { Icon=SystemIcons.Application; }
            currentTarget=String.IsNullOrEmpty(settings.LastTarget)?"Global":settings.LastTarget;
            Build();
            if(!testing)
            {
                InitializeRtss(); InitTray();
                timer=new System.Windows.Forms.Timer(); timer.Interval=500; timer.Tick+=(s,e)=>UpdateLive(); timer.Start();
                SetStartup(false);
                Shown+=(s,e)=>BeginInvoke(new System.Action(()=>{AskInstall();ScanInstalledGames(false);CheckUpdates(false);}));
            }
            TopMost=settings.AlwaysOnTop;
            if(startInTray && !testing) { WindowState=FormWindowState.Minimized; ShowInTaskbar=false; }
        }
        void Persist()
        {
            if(testing)return;
            try { Store.Save(settings); }
            catch(Exception ex){ SetStatus(L("Cannot save settings: ","خطا در ذخیره تنظیمات: ")+ex.Message,false); }
        }
        Label TextAt(Control p,string en,string fa,int x,int y,int w,int h,int size,bool strong,Color? color=null)
        {
            Label t=new Label(); t.Text=L(en,fa); t.BackColor=Color.Transparent;
            t.ForeColor=color??theme.Ink; t.Font=new Font("Segoe UI",size,strong?FontStyle.Bold:FontStyle.Regular);
            t.TextAlign=Fa?ContentAlignment.MiddleRight:ContentAlignment.MiddleLeft; t.RightToLeft=Fa?RightToLeft.Yes:RightToLeft.No;
            t.SetBounds(x,y,w,h); t.AutoEllipsis=true; p.Controls.Add(t); return t;
        }
        Button Action(Control p,string en,string fa,int x,int y,int w,int h,bool prominent,EventHandler click)
        {
            Button b=new Button(); b.Text=L(en,fa); b.SetBounds(x,y,w,h);
            b.ForeColor=prominent?theme.AccentInk:theme.Ink;
            b.BackColor=prominent?theme.Accent:theme.Sidebar;
            b.FlatStyle=FlatStyle.Flat; b.FlatAppearance.BorderColor=prominent?theme.Accent:theme.Border;
            b.FlatAppearance.BorderSize=1; b.Cursor=Cursors.Hand;
            b.Font=new Font("Segoe UI Semibold",9.5f,FontStyle.Bold);
            b.RightToLeft=Fa?RightToLeft.Yes:RightToLeft.No;
            if(click!=null)b.Click+=click; p.Controls.Add(b);return b;
        }
        StudioCard Card(string en,string fa,int x,int y,int w,int h)
        {
            StudioCard c=new StudioCard(theme); c.SetBounds(x,y,w,h); canvas.Controls.Add(c);
            TextAt(c,en,fa,17,12,Math.Max(1,w-30),30,11,true,theme.Accent);
            return c;
        }
        void Build()
        {
            rebuilding=true;
            SuspendLayout();
            while(Controls.Count>0){Control c=Controls[0]; Controls.Remove(c);c.Dispose();}
            presets.Clear(); fpsBox=null; targetBox=null; statusLabel=null; graph=null;
            BackColor=theme.Bg; ForeColor=theme.Ink;
            int sideWidth=theme.Layout==1?220:235;
            shell=new TableLayoutPanel(); shell.Dock=DockStyle.Fill; shell.Padding=Padding.Empty; shell.Margin=Padding.Empty;
            shell.RowCount=1;shell.ColumnCount=2;
            shell.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,sideWidth));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); Controls.Add(shell);
            side=new Panel(); side.Dock=DockStyle.Fill; side.BackColor=theme.Sidebar;side.AutoScroll=true;
            side.BackgroundImage=ThemeVisuals.Sidebar(theme);side.BackgroundImageLayout=ImageLayout.Stretch;
            side.Margin=Padding.Empty; shell.Controls.Add(side,0,0);
            TextAt(side,"◈ PacePilot","◈ PacePilot",20,20,198,40,19,true,theme.Accent);
            TextAt(side,"PRECISION GAMING","ابزار حرفه‌ای گیمینگ",21,59,198,23,8,true,theme.Secondary);
            TextAt(side,"LANGUAGE / زبان","زبان / LANGUAGE",20,99,183,18,8,false,theme.Secondary);
            langBox=SelectBox(side,21,121,193,30,new string[]{"English","فارسی"},Fa?1:0);
            langBox.SelectedIndexChanged+=(s,e)=>{if(rebuilding)return; settings.LanguageCode=langBox.SelectedIndex==1?"fa":"en";Persist();Build();};
            TextAt(side,"UI THEME","پوسته برنامه",20,164,188,20,9,true,theme.Secondary);
            themeBox=SelectBox(side,21,187,193,32,StudioTheme.All.Select(x=>x.Display).ToArray(),Array.FindIndex(StudioTheme.All,x=>x.Id==theme.Id));
            themeBox.SelectedIndexChanged+=(s,e)=>{if(rebuilding||themeBox.SelectedIndex<0)return;settings.ThemeName=StudioTheme.All[themeBox.SelectedIndex].Id; theme=StudioTheme.Find(settings.ThemeName);Persist();Build();};
            string[] nav={"dashboard","profiles","settings","rtss","logs","about"};
            string[] en={"Dashboard","Game Library","Appearance & Settings","RTSS & Tools","Diagnostics","About"};
            string[] fa={"داشبورد","کتابخانه بازی‌ها","ظاهر و تنظیمات","RTSS و ابزارها","گزارش و خطایابی","درباره برنامه"};
            string[] symbols={"⌂","▣","⚙","◉","≡","ⓘ"};
            for(int i=0;i<nav.Length;i++)
            {
                string page=nav[i]; bool chosen=currentPage==page;
                Button button=Action(side,symbols[i]+"   "+en[i],symbols[i]+"   "+fa[i],14,242+i*52,205,45,chosen,(s,e)=>{currentPage=page;Build();});
                button.TextAlign=ContentAlignment.MiddleLeft; button.Padding=new Padding(10,0,0,0);
            }
            TextAt(side,"Developer:","توسعه‌دهنده:",22,573,194,23,9,false,theme.Secondary);
            TextAt(side,"Mr.HamidRouter","Mr.HamidRouter",22,596,194,28,11,true,theme.Accent);
            TextAt(side,"Gaming Performance Utility","ابزار تخصصی عملکرد بازی",22,633,190,25,8,false,theme.Secondary);
            viewport=new Panel(); viewport.Dock=DockStyle.Fill; viewport.AutoScroll=true;viewport.BackColor=theme.Bg;viewport.Margin=Padding.Empty;
            shell.Controls.Add(viewport,1,0);
            canvas=new Panel(); canvas.Location=Point.Empty;canvas.BackColor=theme.Bg; viewport.Controls.Add(canvas);
            viewport.Resize+=(s,e)=>FitCanvas();
            FitCanvas();
            rebuilding=false; ResumeLayout(true);
        }
        ComboBox SelectBox(Control parent,int x,int y,int w,int h,string[] items,int selected)
        {
            ComboBox c=new ComboBox(); c.SetBounds(x,y,w,h); c.FlatStyle=FlatStyle.Flat;
            c.DropDownStyle=ComboBoxStyle.DropDownList;c.BackColor=theme.Panel;c.ForeColor=theme.Ink;
            c.Font=new Font("Segoe UI",10);c.Items.AddRange(items);
            parent.Controls.Add(c); if(items.Length>0)c.SelectedIndex=Math.Max(0,Math.Min(items.Length-1,selected)); return c;
        }
        void FitCanvas()
        {
            if(canvas==null||viewport==null||viewport.IsDisposed)return;
            int width=Math.Max(675,viewport.ClientSize.Width-25);
            if(canvas.Width==width&&canvas.Controls.Count>0)return;
            canvas.Size=new Size(width,Math.Max(900,viewport.ClientSize.Height));
            DrawPage();
        }
        void DrawPage()
        {
            if(canvas==null)return;
            canvas.SuspendLayout();
            while(canvas.Controls.Count>0){Control removed=canvas.Controls[0];canvas.Controls.Remove(removed);removed.Dispose();}
            presets.Clear();
            int w=canvas.Width-38;
            if(currentPage=="dashboard") DrawDashboard(19,w);
            else if(currentPage=="profiles") DrawProfiles(19,w);
            else if(currentPage=="settings") DrawSettings(19,w);
            else if(currentPage=="rtss") DrawRtss(19,w);
            else if(currentPage=="logs") DrawLogs(19,w);
            else DrawAbout(19,w);
            canvas.ResumeLayout(true);
        }
        void Header(int x,int w,string en,string fa,string hintEn,string hintFa)
        {
            TextAt(canvas,en,fa,x,16,w,40,20,true);
            TextAt(canvas,hintEn,hintFa,x,52,w,26,9,false,theme.Secondary);
        }
        void DrawDashboard(int x,int w)
        {
            string heading=theme.Layout==3?"PACEPILOT / VELOCITY":theme.Layout==2?"PACEPILOT / NEBULA":theme.Layout==5?"PACEPILOT / EXECUTIVE":"PacePilot  •  Frame Control";
            Header(x,w,heading,"PacePilot | کنترل هوشمند فریم",
                "Precision FPS limiting • Per-game profiles • Real RTSS telemetry",
                "محدودسازی FPS • پروفایل بازی • نمایش اطلاعات واقعی RTSS");
            int gap=12, top=98, height=0;
            int wide=w, left=(int)(w*0.59), right=w-left-gap;
            if(theme.Layout!=3){MakeHero(x,top,wide,85);top+=97;}
            if(theme.Layout==1)
            {
                MakeTarget(x,top,wide,148); MakeLimiter(x,top+160,wide,176);
                MakeStats(x,top+348,wide,124,true); MakeGraph(x,top+484,wide,230);
                MakeOptions(x,top+726,wide,152);height=top+897;
            }
            else if(theme.Layout==2)
            {
                int gameWidth=(int)(w*.31), bodyWidth=w-gameWidth-gap;
                MakeTarget(x,top,gameWidth,308);
                MakeLimiter(x+gameWidth+gap,top,bodyWidth,181);
                MakeStats(x+gameWidth+gap,top+193,bodyWidth,114,true);
                MakeGraph(x,top+320,wide,262);
                MakeOptions(x,top+594,wide,168);height=top+781;
            }
            else if(theme.Layout==3)
            {
                MakeHero(x,top,wide,90);top+=102;
                MakeTarget(x,top,left,159); MakeStats(x+left+gap,top,right,159,false);
                MakeLimiter(x,top+171,left,193); MakeOptions(x+left+gap,top+171,right,193);
                MakeGraph(x,top+376,wide,253);height=top+648;
            }
            else if(theme.Layout==4)
            {
                MakeTarget(x,top,left,166);MakeOptions(x+left+gap,top,right,174);
                MakeLimiter(x,top+186,left,181);MakeStats(x+left+gap,top+186,right,181,false);
                MakeGraph(x,top+379,wide,295);height=top+693;
            }
            else if(theme.Layout==5)
            {
                left=(int)(w*.46);right=w-left-gap;
                MakeTarget(x,top,left,176);MakeLimiter(x,top+188,left,219);
                MakeStats(x+left+gap,top,right,146,true);MakeGraph(x+left+gap,top+158,right,249);
                MakeOptions(x,top+419,wide,178);height=top+618;
            }
            else
            {
                MakeTarget(x,top,wide,139);
                MakeLimiter(x,top+151,left,185);MakeStats(x+left+gap,top+151,right,185,false);
                MakeGraph(x,top+349,wide,259);
                MakeOptions(x,top+620,wide,148);height=top+786;
            }
            statusLabel=TextAt(canvas,"Ready. Select a game and apply a limit.","آماده؛ بازی را انتخاب و محدودیت را اعمال کنید.",x,height,wide,42,10,false,theme.Accent);
            canvas.Height=Math.Max(viewport.ClientSize.Height,height+84);
            RefreshSelection(); RefreshRtssStatus();
        }
        void MakeHero(int x,int y,int w,int h)
        {
            ThemeCard c=new ThemeCard(theme);
            c.SetBounds(x,y,w,h);c.Artwork=ThemeVisuals.Get(theme.Id);
            c.CropRectangle=ThemeVisuals.Crop(theme.Id,false,c.Artwork);
            canvas.Controls.Add(c);
            TextAt(c,"PACEPILOT   /   PRECISION GAMING","PacePilot  |  تجربه روان‌تر بازی",18,9,w-36,32,15,true,theme.Accent);
            TextAt(c,"PERFORMANCE  •  PRECISION  •  CONTROL","عملکرد بهینه • کنترل دقیق • تجربه حرفه‌ای",19,41,w-39,25,10,true,theme.Ink);
        }
        void MakeTarget(int x,int y,int w,int h)
        {
            StudioCard c=Card("GAME / PROFILE","بازی / پروفایل",x,y,w,h);
            TextAt(c,"Select a game to control","بازی موردنظر را انتخاب کنید",17,41,w-30,21,9,false,theme.Secondary);
            List<string> items=new List<string>(); items.Add("Global");
            items.AddRange(settings.Games.Select(z=>z.Exe).Distinct(StringComparer.OrdinalIgnoreCase));
            targetBox=SelectBox(c,17,65,w-34,32,items.ToArray(),Math.Max(0,items.FindIndex(s=>s.Equals(currentTarget,StringComparison.OrdinalIgnoreCase))));
            targetBox.SelectedIndexChanged+=(s,e)=> { if(rebuilding||targetBox==null)return;currentTarget=Convert.ToString(targetBox.SelectedItem);settings.LastTarget=currentTarget;Persist();ReadTargetLimit(); };
            int buttonY=108;
            if(h<140) buttonY=99;
            int buttonW=(w-45)/2;
            Action(c,"+ Game EXE","+ فایل بازی",17,buttonY,buttonW,32,false,(s,e)=>AddGame());
            Action(c,"Running game","بازی در حال اجرا",buttonW+28,buttonY,buttonW,32,false,(s,e)=>PickGame());
            if(h>=205)
            {
                TextAt(c,"Active profile: "+currentTarget,"پروفایل فعال: "+currentTarget,18,154,w-36,32,9,true,theme.Secondary);
                Action(c,"Remove profile","حذف پروفایل",17,201,w-34,38,false,(s,e)=>RemoveProfile());
            }
        }
        void MakeLimiter(int x,int y,int w,int h)
        {
            StudioCard c=Card("FPS LIMITER","محدودکننده نرخ فریم",x,y,w,h);
            int[] numbers={30,40,60,90,120,144,165,240};
            int cols=(w<355 && h>=205)?4:8;
            int bx=17, by=47, bw=(w-34-6*(cols-1))/cols;
            for(int i=0;i<numbers.Length;i++)
            {
                int v=numbers[i]; int col=i%cols,row=i/cols;
                Button b=Action(c,v.ToString(),v.ToString(),bx+col*(bw+6),by+row*35,bw,31,false,(s,e)=>{fpsBox.Value=v;RefreshSelection();});
                b.Tag=v;presets.Add(b);
            }
            int rowBottom=by+(numbers.Length+cols-1)/cols*35;
            int fpsY=rowBottom+4;
            TextAt(c,"Custom FPS","فریم دلخواه",17,fpsY,110,25,9,false,theme.Secondary);
            fpsBox=new NumericUpDown(); fpsBox.SetBounds(Math.Min(124,w/3),fpsY,Math.Max(85,w/5),31);
            fpsBox.Minimum=0;fpsBox.Maximum=1000;fpsBox.Value=settings.GlobalLimit>1000?60:Math.Max(0,settings.GlobalLimit);
            fpsBox.Font=new Font("Segoe UI",11,FontStyle.Bold);fpsBox.BackColor=theme.Sidebar;fpsBox.ForeColor=theme.Ink;
            fpsBox.TextAlign=HorizontalAlignment.Center;c.Controls.Add(fpsBox);
            fpsBox.ValueChanged+=(s,e)=>RefreshSelection();
            int actionY=h-47;
            int actionW=Math.Max(100,(w-44)*2/3);
            apply=Action(c,"▶  APPLY LIMIT","▶  اعمال محدودیت",17,actionY,actionW,36,true,(s,e)=>ApplyLimit(false));
            disable=Action(c,"Disable","غیرفعال",actionW+23,actionY,Math.Max(1,w-actionW-40),36,false,(s,e)=>{fpsBox.Value=0;ApplyLimit(false);});
            if(actionY<fpsY+32 && h<200)
            {
                // Narrow compact cards keep controls separated by using the upper/right area for custom FPS.
                fpsBox.SetBounds(Math.Max(w-105,120),fpsY,85,28);
            }
            if(!String.IsNullOrEmpty(currentTarget) && rtss.IsRunning)
                ReadTargetLimit();
        }
        void MakeStats(int x,int y,int w,int h,bool horizontal)
        {
            StudioCard c=Card("LIVE TELEMETRY","آمار زنده",x,y,w,h);
            int pad=13; int available=w-2*pad; int itemW=horizontal?(available-18)/4:(available-7)/2;
            int itemH=horizontal?h-54:(h-55)/2;
            int count=0;
            string[] ens={"FPS","Frame time","Refresh","RTSS"};string[] fas={"نرخ فریم","زمان فریم","نرخ نوسازی","وضعیت RTSS"};
            for(int i=0;i<4;i++)
            {
                int cx=pad+(horizontal?i*(itemW+6):(i%2)*(itemW+7));
                int cy=39+(horizontal?0:(i/2)*(itemH+4));
                Panel sub=new Panel();sub.SetBounds(cx,cy,itemW,itemH);sub.BackColor=theme.Sidebar;c.Controls.Add(sub);
                TextAt(sub,ens[i],fas[i],8,3,Math.Max(1,itemW-14),19,8,false,theme.Secondary);
                Label val=TextAt(sub,"--","--",8,21,Math.Max(1,itemW-14),Math.Max(19,itemH-25),12,true,i==3?theme.Accent:theme.Ink);
                if(i==0)liveValue=val; if(i==1)frameValue=val;if(i==2)hzValue=val;if(i==3)rtssValue=val;
            }
            if(hzValue!=null)hzValue.Text=ScreenRefresh().ToString("0")+" Hz";
        }
        void MakeGraph(int x,int y,int w,int h)
        {
            StudioCard c=Card("FRAME-TIME HISTORY   •   REAL SAMPLES ONLY","تاریخچه زمان فریم • فقط داده واقعی",x,y,w,h);
            graph=new StudioGraph(theme);graph.SetBounds(11,43,w-22,h-54);graph.Target=fpsBox==null?60:(int)fpsBox.Value;c.Controls.Add(graph);
        }
        void MakeOptions(int x,int y,int w,int h)
        {
            StudioCard c=Card("QUICK SETTINGS","تنظیمات سریع",x,y,w,h);
            int y0=44; int cols=w>=570?2:1;int colW=(w-34)/cols;
            Check(c,"Minimize to tray","انتقال به کنار ساعت",17,y0,colW-8,()=>settings.MinimizeToTray,v=>settings.MinimizeToTray=v);
            Check(c,"Always on top","همیشه روی پنجره‌ها",17+(cols==2?colW:0),y0,colW-8,()=>settings.AlwaysOnTop,v=>{settings.AlwaysOnTop=v;TopMost=v;});
            int secondY=y0+(cols==2?39:31);
            Check(c,"Auto-select game","انتخاب خودکار بازی",17,secondY,colW-8,()=>settings.AutoSelectKnownGames,v=>settings.AutoSelectKnownGames=v);
            Check(c,"Run at startup","اجرا با ویندوز",17+(cols==2?colW:0),secondY+(cols==2?0:31),colW-8,()=>settings.StartWithWindows,v=>{settings.StartWithWindows=v;SetStartup(true);});
            if(h>=176)TextAt(c,"Monitoring is live; no synthetic FPS data.","داده‌های فریم واقعی هستند، نه شبیه‌سازی‌شده.",18,h-35,w-36,24,8,false,theme.Secondary);
        }
        void Check(Control parent,string en,string fa,int x,int y,int w,Func<bool> value,Action<bool> changed)
        {
            CheckBox b=new CheckBox();b.Text=L(en,fa);b.ForeColor=theme.Ink;b.BackColor=Color.Transparent;
            b.RightToLeft=Fa?RightToLeft.Yes:RightToLeft.No;
            b.SetBounds(x,y,Math.Max(10,w),29);b.Checked=value();
            parent.Controls.Add(b);b.CheckedChanged+=(s,e)=>{changed(b.Checked);Persist();};
        }
        void RefreshSelection()
        {
            if(fpsBox==null||fpsBox.IsDisposed)return;
            int val=(int)fpsBox.Value;
            foreach(Button b in presets)
            {
                bool selected=(int)b.Tag==val;
                b.BackColor=selected?theme.Accent:theme.Sidebar;
                b.ForeColor=selected?theme.AccentInk:theme.Ink;
                b.FlatAppearance.BorderColor=selected?theme.Accent:theme.Border;
            }
            if(graph!=null)graph.Target=Math.Max(1,val);
            if(apply!=null)apply.Text=L("▶ APPLY "+val+" FPS","▶ اعمال "+val+" فریم");
        }
        void ReadTargetLimit()
        {
            if(fpsBox==null||fpsBox.IsDisposed)return;
            int limit=settings.GlobalLimit;
            GameProfile game=settings.Games.FirstOrDefault(g=>String.Equals(g.Exe,currentTarget,StringComparison.OrdinalIgnoreCase));
            if(game!=null) limit=game.Limit;
            if(rtss.IsRunning && rtss.IsReady)try {limit=rtss.GetLimit(currentTarget);}catch{}
            fpsBox.Value=Math.Max(0,Math.Min(1000,limit));
            RefreshSelection();
        }
        void SetStatus(string message,bool good)
        {
            if(statusLabel!=null&&!statusLabel.IsDisposed){statusLabel.Text=message;statusLabel.ForeColor=good?theme.Accent:Color.OrangeRed;}
        }
        void ApplyLimit(bool quiet)
        {
            if(fpsBox==null)return;
            string warning=Guard.Check(currentTarget);
            if(!String.IsNullOrEmpty(warning))
            {
                bool blocked=warning.Contains("blocklist")||warning.Contains("Anti-cheat process");
                if(blocked){SetStatus(L("Protected game: no limit applied.","بازی محافظت‌شده: محدودیت اعمال نشد."),false);if(!quiet)MessageBox.Show(this,warning,"PacePilot safety",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
                if(!quiet && MessageBox.Show(this,L("Global mode affects every game, including protected titles. Continue?","محدودیت عمومی می‌تواند روی بازی‌های محافظت‌شده هم اثر بگذارد. ادامه می‌دهید؟"),"PacePilot",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
            }
            try
            {
                if(!rtss.IsReady)throw new InvalidOperationException(L("RTSS is not detected. Open RTSS setup.","RTSS شناسایی نشد. بخش نصب RTSS را باز کنید."));
                if(!rtss.IsRunning && !rtss.Start())throw new InvalidOperationException(L("Cannot launch RTSS.","امکان اجرای RTSS نیست."));
                int n=(int)fpsBox.Value;
                rtss.ApplyLimit(currentTarget,n);
                applied=n;if(n>0)lastNonZero=n;
                if(currentTarget=="Global")settings.GlobalLimit=n;
                else
                {
                    GameProfile g=settings.Games.FirstOrDefault(p=>String.Equals(p.Exe,currentTarget,StringComparison.OrdinalIgnoreCase));
                    if(g!=null)g.Limit=n;
                }
                Persist();SetStatus(L("Verified: ","تأیید شد: ")+currentTarget+"  •  "+n+" FPS",true);
            }
            catch(RtssElevationRequiredException need)
            {
                if(MessageBox.Show(this,
                    L("Windows protects the RTSS profiles folder. Approve administrator access to save only this FPS profile?",
                      "پوشه پروفایل‌های RTSS محافظت‌شده است. برای ذخیره همین محدودیت فریم، مجوز Administrator را تأیید می‌کنید؟"),
                    L("RTSS profile permissions","مجوز ذخیره پروفایل RTSS"),
                    MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)
                {
                    SetStatus(L("Profile write was canceled.","ذخیره پروفایل لغو شد."),false);
                    return;
                }
                try
                {
                    ProcessStartInfo info=new ProcessStartInfo(Application.ExecutablePath);
                    info.Arguments="--rtss-repair "+need.Fps+" "+Convert.ToBase64String(Encoding.UTF8.GetBytes(need.Exe));
                    info.UseShellExecute=true;
                    info.Verb="runas";
                    info.WindowStyle=ProcessWindowStyle.Hidden;
                    using(Process helper=Process.Start(info))
                    {
                        if(helper==null)throw new InvalidOperationException("Elevated helper did not start.");
                        if(!helper.WaitForExit(120000))
                            throw new TimeoutException("Timed out waiting for RTSS profile repair.");
                        if(helper.ExitCode!=0)
                            throw new InvalidOperationException("RTSS elevated writer returned code "+helper.ExitCode+". See diagnostics.");
                    }
                    rtss.VerifyElevatedProfile(need.Exe,need.Fps);
                    int n=need.Fps;
                    applied=n;if(n>0)lastNonZero=n;
                    if(currentTarget=="Global")settings.GlobalLimit=n;
                    else
                    {
                        GameProfile g=settings.Games.FirstOrDefault(x=>String.Equals(x.Exe,currentTarget,StringComparison.OrdinalIgnoreCase));
                        if(g!=null)g.Limit=n;
                    }
                    Persist();SetStatus(L("Verified and saved: ","ذخیره و تأیید شد: ")+n+" FPS",true);
                }
                catch(Exception ex)
                {
                    SetStatus(L("Could not repair RTSS profile: ","ترمیم پروفایل RTSS ناموفق بود: ")+ex.Message,false);
                    if(!quiet)MessageBox.Show(this,ex.Message,L("RTSS profile error","خطای پروفایل RTSS"),MessageBoxButtons.OK,MessageBoxIcon.Error);
                }
            }
            catch(Exception ex)
            {
                SetStatus(L("Could not apply: ","اعمال ناموفق: ")+ex.Message,false);
                if(!quiet)MessageBox.Show(this,ex.Message,L("RTSS error","خطای RTSS"),MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
        void UpdateLive()
        {
            if(rebuilding||IsDisposed)return;
            RefreshRtssStatus();
            if(settings.AutoSelectKnownGames)
            {
                try
                {
                    uint pid;WinApi.GetWindowThreadProcessId(WinApi.GetForegroundWindow(),out pid);
                    if(pid>0 && pid!=Process.GetCurrentProcess().Id)
                    {
                        using(Process p=Process.GetProcessById((int)pid))
                        {
                            string exe=p.ProcessName+".exe";
                            if(settings.Games.Any(g=>String.Equals(g.Exe,exe,StringComparison.OrdinalIgnoreCase)) && !exe.Equals(currentTarget,StringComparison.OrdinalIgnoreCase))
                            {
                                currentTarget=exe;settings.LastTarget=exe;
                                if(targetBox!=null){int index=targetBox.Items.IndexOf(exe);if(index>=0)targetBox.SelectedIndex=index;}
                            }
                        }
                    }
                }
                catch{}
            }
            FrameSample sample=rtss.IsRunning?LiveFrames.Read(currentTarget):new FrameSample();
            if(liveValue!=null&&!liveValue.IsDisposed)liveValue.Text=sample.Valid?sample.Fps.ToString("0.0"):"--";
            if(frameValue!=null&&!frameValue.IsDisposed)frameValue.Text=sample.Valid?sample.FrameMs.ToString("0.00")+" ms":"--";
            if(graph!=null&&!graph.IsDisposed)graph.Push(sample.Valid?sample.FrameMs:float.NaN);
        }
        void RefreshRtssStatus()
        {
            if(rtssValue!=null&&!rtssValue.IsDisposed)rtssValue.Text=rtss.IsRunning?L("Running","فعال"):rtss.IsReady?L("Stopped","متوقف"):L("Missing","نصب نیست");
        }
        int ScreenRefresh()
        {
            try { using(Graphics g=Graphics.FromHwnd(IntPtr.Zero)) { IntPtr hdc=g.GetHdc();try{return GetDeviceCaps(hdc,116);}finally{g.ReleaseHdc(hdc);} } } catch{return 0;}
        }
        [System.Runtime.InteropServices.DllImport("gdi32.dll")] static extern int GetDeviceCaps(IntPtr hdc,int index);
        void AddGame()
        {
            using(OpenFileDialog f=new OpenFileDialog())
            {
                f.Filter="Game executables (*.exe)|*.exe"; f.Title=L("Select a game executable","فایل اجرایی بازی را انتخاب کنید");
                if(f.ShowDialog(this)!=DialogResult.OK)return;
                AddProfile(f.FileName);
            }
        }
        void AddProfile(string file)
        {
            if(!File.Exists(file)||!file.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))return;
            string exe=Path.GetFileName(file);
            if(settings.Games.All(g=>!g.Exe.Equals(exe,StringComparison.OrdinalIgnoreCase)))
                settings.Games.Add(new GameProfile{Exe=exe,FullPath=file,Limit=60});
            currentTarget=exe;settings.LastTarget=exe;Persist();DrawPage();
        }
        void PickGame()
        {
            using(Form dialog=new Form())
            {
                dialog.Text=L("Choose a running game","انتخاب بازی در حال اجرا");dialog.ClientSize=new Size(495,430);
                dialog.StartPosition=FormStartPosition.CenterParent;
                ListBox list=new ListBox();list.Dock=DockStyle.Fill;list.Font=new Font("Segoe UI",10);
                var running=new List<Tuple<string,string>>();
                foreach(Process p in Process.GetProcesses())try
                {
                    if(p.Id==Process.GetCurrentProcess().Id||p.MainWindowHandle==IntPtr.Zero)continue;
                    string path=p.MainModule.FileName;
                    if(!String.IsNullOrEmpty(path)&&path.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))
                        running.Add(Tuple.Create(p.ProcessName+"  —  "+p.MainWindowTitle,path));
                }catch{}finally{p.Dispose();}
                foreach(var p in running.OrderBy(v=>v.Item1))list.Items.Add(p);
                list.DisplayMember="Item1";
                Button ok=new Button{Text=L("Select game","انتخاب بازی"),Dock=DockStyle.Bottom,Height=43,DialogResult=DialogResult.OK};
                dialog.Controls.Add(list);dialog.Controls.Add(ok);dialog.AcceptButton=ok;
                if(dialog.ShowDialog(this)==DialogResult.OK && list.SelectedItem is Tuple<string,string>)AddProfile(((Tuple<string,string>)list.SelectedItem).Item2);
            }
        }
        void RemoveProfile()
        {
            if(currentTarget=="Global")return;
            if(MessageBox.Show(this,L("Remove this saved PacePilot profile? RTSS game settings are not deleted.","این پروفایل ذخیره‌شده در PacePilot حذف شود؟ تنظیمات RTSS بازی حذف نمی‌شود."),"PacePilot",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            settings.Games.RemoveAll(g=>String.Equals(g.Exe,currentTarget,StringComparison.OrdinalIgnoreCase));
            currentTarget="Global";settings.LastTarget=currentTarget;Persist();DrawPage();
        }
        void DrawProfiles(int x,int w)
        {
            Header(x,w,"Game Profiles","پروفایل بازی‌ها","Choose a profile, then edit its limit on Dashboard.","پروفایل را انتخاب کنید و محدودیت آن را در داشبورد تغییر دهید.");
            StudioCard c=Card("SAVED GAMES","بازی‌های ذخیره‌شده",x,101,w,Math.Max(230,110+settings.Games.Count*58));
            Action(c,"+ Add a game","+ افزودن بازی",17,51,180,37,true,(s,e)=>AddGame());
            int i=0;
            foreach(GameProfile game in settings.Games.ToList())
            {
                GameProfile item=game;
                int rowY=100+i*58;
                TextAt(c,game.Exe+"  •  "+game.Limit+" FPS",game.Exe+"  •  "+game.Limit+" فریم",19,rowY,Math.Max(200,w-182),44,10,true);
                Action(c,"Edit","ویرایش",Math.Max(215,w-125),rowY+3,92,34,false,(s,e)=>{currentTarget=item.Exe;currentPage="dashboard";Build();});
                i++;
            }
            canvas.Height=Math.Max(viewport.Height,c.Bottom+90);
        }
        void DrawSettings(int x,int w)
        {
            Header(x,w,"Preferences & appearance","تنظیمات و ظاهر برنامه","All six design systems are functional and can be switched without restarting.","هر شش طرح واقعی و قابل انتخاب هستند، بدون نیاز به راه‌اندازی مجدد.");
            StudioCard c=Card("CHOOSE A VISUAL THEME","انتخاب طرح گرافیکی",x,101,w,265);
            int itemW=(w-54)/3;
            for(int i=0;i<6;i++)
            {
                StudioTheme th=StudioTheme.All[i]; int px=16+(i%3)*(itemW+8),py=49+(i/3)*94;
                Button b=Action(c,th.Display,th.Display,px,py,itemW,76,theme.Id==th.Id,(s,e)=>{settings.ThemeName=th.Id;theme=th;Persist();Build();});
            }
            StudioCard opt=Card("GENERAL OPTIONS","تنظیمات عمومی",x,379,w,165);
            Check(opt,"Start with Windows","اجرا همراه ویندوز",17,46,w/2-25,()=>settings.StartWithWindows,v=>{settings.StartWithWindows=v;SetStartup(true);});
            Check(opt,"Minimize to system tray","انتقال به کنار ساعت",w/2,46,w/2-20,()=>settings.MinimizeToTray,v=>settings.MinimizeToTray=v);
            Check(opt,"Always on top","همیشه روی سایر پنجره‌ها",17,91,w/2-25,()=>settings.AlwaysOnTop,v=>{settings.AlwaysOnTop=v;TopMost=v;});
            Check(opt,"Auto-select known games","انتخاب خودکار بازی‌های ذخیره‌شده",w/2,91,w/2-20,()=>settings.AutoSelectKnownGames,v=>settings.AutoSelectKnownGames=v);
            canvas.Height=Math.Max(viewport.Height,650);
        }
        void DrawRtss(int x,int w)
        {
            Header(x,w,"RTSS integration","یکپارچه‌سازی RTSS","Actual framerate enforcement uses the separately installed RTSS engine.","اعمال واقعی محدودیت فریم توسط موتور جداگانه RTSS انجام می‌شود.");
            StudioCard c=Card("RTSS STATUS","وضعیت RTSS",x,101,w,266);
            TextAt(c,rtss.IsReady?"Detected: "+rtss.Folder:"Not detected","",17,51,w-34,35,10,true);
            TextAt(c,"Running: "+rtss.IsRunning,"در حال اجرا: "+(rtss.IsRunning?"بله":"خیر"),17,99,w-34,29,10,false,theme.Secondary);
            Action(c,"Install RTSS (winget)","نصب RTSS (winget)",17,161,Math.Max(130,(w-58)/3),43,true,(s,e)=>AskInstall(true));
            Action(c,"Open / Start","بازکردن / اجرا",30+Math.Max(130,(w-58)/3),161,Math.Max(128,(w-58)/3),43,false,(s,e)=>OpenRtss());
            Action(c,"Select folder","انتخاب پوشه نصب",43+2*Math.Max(130,(w-58)/3),161,Math.Max(120,(w-58)/3),43,false,(s,e)=>SelectRtssFolder());
            Action(c,"Official download website","سایت رسمی دانلود",17,212,260,34,false,(s,e)=>OpenUrl(RtssOfficial));
            StudioCard report=Card("DIAGNOSTICS","عیب‌یابی",x,383,w,240);
            Action(report,"Copy full diagnostics","کپی گزارش کامل",17,48,230,38,true,(s,e)=>{try{Clipboard.SetText(rtss.Diagnostics(currentTarget));MessageBox.Show(this,L("Diagnostics copied.","گزارش کپی شد."));}catch(Exception ex){MessageBox.Show(this,ex.Message);}});
            TextAt(report,"PacePilot does not modify protected anti-cheat games. Do not use a global cap in protected online titles.","PacePilot تنظیمات بازی‌های دارای ضدتقلب محافظت‌شده را تغییر نمی‌دهد. از محدودیت عمومی برای این بازی‌ها استفاده نکنید.",17,104,w-37,100,10,false,theme.Secondary);
            canvas.Height=Math.Max(viewport.Height,700);
        }
        void DrawLogs(int x,int w)
        {
            Header(x,w,"Diagnostics & logs","گزارش و عیب‌یابی","Information comes from RTSS and the actual configuration, never simulated.","گزارش‌ها از RTSS و تنظیمات واقعی گرفته می‌شوند.");
            StudioCard c=Card("SYSTEM REPORT","گزارش سیستم",x,101,w,490);
            TextBox report=new TextBox();report.Multiline=true;report.ReadOnly=true;report.ScrollBars=ScrollBars.Both;
            report.WordWrap=false;report.Font=new Font("Consolas",9);report.BackColor=theme.Sidebar;report.ForeColor=theme.Ink;
            report.SetBounds(17,53,w-34,345);c.Controls.Add(report);
            try{report.Text=rtss.Diagnostics(currentTarget);}catch(Exception ex){report.Text=ex.ToString();}
            Action(c,"Copy report","کپی گزارش",17,414,169,39,true,(s,e)=>Clipboard.SetText(report.Text));
            Action(c,"Refresh","بروزرسانی",199,414,140,39,false,(s,e)=>{report.Text=rtss.Diagnostics(currentTarget);});
            canvas.Height=Math.Max(viewport.Height,650);
        }
        void DrawAbout(int x,int w)
        {
            Header(x,w,"About PacePilot","درباره PacePilot","Windows frame pacing utility with six interface designs.","برنامه ویندوزی کنترل فریم با شش طرح متفاوت.");
            StudioCard c=Card("PACEPILOT STUDIO 2.0","PacePilot Studio نسخه ۲",x,101,w,367);
            TextAt(c,"Developer: Mr.HamidRouter","توسعه‌دهنده: Mr.HamidRouter",22,62,w-44,35,15,true,theme.Accent);
            TextAt(c,"English + فارسی  |  6 live themes  |  RTSS framerate engine","فارسی + انگلیسی | شش پوسته واقعی | موتور محدودسازی RTSS",22,110,w-44,40,11,false);
            TextAt(c,"This is an independent, MIT-licensed RTSS frontend. It is not affiliated with FramePacer, RTSS or Guru3D. NVIDIA Reflex, DLSS frame generation, and a proprietary frame-injection engine are not implemented. RTSS installation requires consent and may need administrator approval.",
                "این برنامه رابط مستقلی برای RTSS با مجوز MIT است. به FramePacer، RTSS یا Guru3D وابسته نیست. قابلیت‌های NVIDIA Reflex، تولید فریم DLSS و موتور اختصاصی تزریق فریم پیاده‌سازی نشده‌اند. نصب RTSS به اجازه کاربر نیاز دارد.",22,166,w-44,125,10,false,theme.Secondary);
            Action(c,"Project on GitHub","پروژه در گیت‌هاب",22,304,215,40,true,(s,e)=>OpenUrl("https://github.com/MrHamidRouter/PacePilot"));
            canvas.Height=Math.Max(viewport.Height,570);
        }
        void OpenUrl(string url){try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(this,ex.Message);}}
        void InitializeRtss()
        {
            string dir=RtssControl.FindFolder(settings.RtssFolder);
            // Detect RTSS without starting its Direct3D hooks during PacePilot startup.
            // RTSS is launched on first Apply or explicitly through the RTSS Tools page.
            if(rtss.Initialize(dir)) {settings.RtssFolder=dir;Persist();}
        }
        void AskInstall(){AskInstall(false);}
        void AskInstall(bool explicitClick)
        {
            if(testing||installing)return;
            if(rtss.IsReady){if(explicitClick)MessageBox.Show(this,L("RTSS is already installed.","RTSS قبلاً نصب شده است."));return;}
            if(prompted&&!explicitClick)return;
            prompted=true;
            if(MessageBox.Show(this,L("RTSS is not installed. Download and install it through Microsoft's winget package manager? Windows may request administrator approval. Nothing will be installed without your consent.",
                 "RTSS نصب نیست. آیا از طریق مدیریت بسته‌های رسمی ویندوز (winget) دانلود و نصب شود؟ ممکن است مجوز مدیر سیستم لازم باشد. بدون اجازه شما چیزی نصب نمی‌شود."),L("RTSS setup","نصب RTSS"),MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
            installing=true;
            var worker=new BackgroundWorker();
            worker.DoWork+=(s,e)=>
            {
                try
                {
                    ProcessStartInfo pi=new ProcessStartInfo("winget.exe","install --id Guru3D.RTSS --exact --source winget --silent --accept-package-agreements --accept-source-agreements --disable-interactivity");
                    pi.UseShellExecute=true;pi.Verb="runas";
                    using(Process p=Process.Start(pi))
                    {
                        if(p==null)throw new InvalidOperationException("winget could not start");
                        if(!p.WaitForExit(300000)){try{p.Kill();}catch{}throw new TimeoutException("RTSS installation timed out");}
                        e.Result=p.ExitCode;
                    }
                }
                catch(Exception ex){e.Result=ex;}
            };
            worker.RunWorkerCompleted+=(s,e)=>
            {
                installing=false;InitializeRtss();
                if(rtss.IsReady){MessageBox.Show(this,L("RTSS installed and detected.","RTSS نصب و شناسایی شد."));DrawPage();return;}
                string issue=e.Result is Exception?((Exception)e.Result).Message:"winget exit code: "+Convert.ToString(e.Result);
                if(MessageBox.Show(this,L("Automatic RTSS installation failed: ","نصب خودکار RTSS انجام نشد: ")+issue+"\n"+L("Open official download page?","صفحه دانلود رسمی باز شود؟"),"RTSS",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes)OpenUrl(RtssOfficial);
            };
            worker.RunWorkerAsync();
        }
        void SelectRtssFolder()
        {
            using(FolderBrowserDialog f=new FolderBrowserDialog())
            {
                f.Description=L("Select the RTSS installation folder","پوشه نصب RTSS را انتخاب کنید");
                if(f.ShowDialog(this)!=DialogResult.OK)return;
                if(!File.Exists(Path.Combine(f.SelectedPath,"RTSSHooks64.dll"))){MessageBox.Show(this,L("This is not a valid RTSS installation.","این پوشه حاوی نصب معتبر RTSS نیست."));return;}
                settings.RtssFolder=f.SelectedPath;Persist();InitializeRtss();DrawPage();
            }
        }
        void OpenRtss()
        {
            try
            {
                if(!rtss.IsReady)InitializeRtss();
                if(!rtss.IsReady)throw new Exception("RTSS not installed");
                if(!rtss.IsRunning)
                {
                    if(!rtss.Start())throw new Exception(L("RTSS could not be started.","RTSS اجرا نشد."));
                }
                else
                {
                    // When RTSS is running, a second invocation may bring up its UI.
                    Process.Start(new ProcessStartInfo(Path.Combine(rtss.Folder,"RTSS.exe")){UseShellExecute=true});
                }
                RefreshRtssStatus();
            }
            catch(Exception ex){MessageBox.Show(this,ex.Message);}
        }
        void SetStartup(bool persist)
        {
            if(testing)return;
            try
            {
                using(RegistryKey key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true))
                {
                    if(key==null)throw new Exception("Windows Startup registry key missing");
                    if(settings.StartWithWindows)key.SetValue("PacePilot","\""+Application.ExecutablePath+"\" --tray");
                    else key.DeleteValue("PacePilot",false);
                }
            }
            catch(Exception ex){if(persist)MessageBox.Show(this,L("Cannot set startup: ","خطا در اجرای خودکار: ")+ex.Message);}
        }
        void InitTray()
        {
            tray=new NotifyIcon();tray.Icon=Icon??SystemIcons.Application;tray.Text="PacePilot Studio";tray.Visible=true;
            ContextMenuStrip menu=new ContextMenuStrip();
            menu.Items.Add(L("Open PacePilot","نمایش PacePilot"),null,(s,e)=>{Show();WindowState=FormWindowState.Normal;ShowInTaskbar=true;Activate();});
            menu.Items.Add(L("Disable limit","غیرفعال کردن محدودیت"),null,(s,e)=>{currentPage="dashboard";Build();if(fpsBox!=null){fpsBox.Value=0;ApplyLimit(true);}});
            menu.Items.Add(L("Exit","خروج"),null,(s,e)=>{finishing=true;Close();});
            tray.ContextMenuStrip=menu;
            tray.DoubleClick+=(s,e)=>{Show();WindowState=FormWindowState.Normal;ShowInTaskbar=true;Activate();};
        }
        protected override void WndProc(ref Message m)
        {
            if(m.Msg==0x0312 && !testing)
            {
                if(currentPage!="dashboard"){currentPage="dashboard";Build();}
                int hotkey=m.WParam.ToInt32();
                if(fpsBox!=null)
                {
                    if(hotkey==0xD100){fpsBox.Value=fpsBox.Value==0?Math.Max(1,lastNonZero):0;ApplyLimit(true);}
                    else if(hotkey==0xD101){fpsBox.Value=Math.Min(1000,fpsBox.Value+1);ApplyLimit(true);}
                    else if(hotkey==0xD102){fpsBox.Value=Math.Max(1,fpsBox.Value-1);ApplyLimit(true);}
                }
                return;
            }
            base.WndProc(ref m);
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if(!testing)
            {
                WinApi.RegisterHotKey(Handle,0xD100,0x1|0x4,(uint)Keys.L);
                WinApi.RegisterHotKey(Handle,0xD101,0x1|0x4,(uint)Keys.Up);
                WinApi.RegisterHotKey(Handle,0xD102,0x1|0x4,(uint)Keys.Down);
            }
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            if(!testing)
            {
                WinApi.UnregisterHotKey(Handle,0xD100);WinApi.UnregisterHotKey(Handle,0xD101);WinApi.UnregisterHotKey(Handle,0xD102);
            }
            base.OnHandleDestroyed(e);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if(!testing && WindowState==FormWindowState.Minimized && settings!=null && settings.MinimizeToTray)
            {Hide();ShowInTaskbar=false;}
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if(!testing&&!finishing&&settings.MinimizeToTray&&e.CloseReason==CloseReason.UserClosing)
            {e.Cancel=true;Hide();ShowInTaskbar=false;return;}
            if(timer!=null)timer.Dispose();if(tray!=null){tray.Visible=false;tray.Dispose();}
            base.OnFormClosing(e);
        }
        public void SelfTest()
        {
            foreach(StudioTheme t in StudioTheme.All)
            {
                theme=t;settings.ThemeName=t.Id;currentPage="dashboard";Build();
                foreach(Size size in new[]{new Size(950,665),new Size(1320,850),new Size(1600,1000)})
                {
                    Size=size;PerformLayout();Application.DoEvents();FitCanvas();
                    if(shell.GetControlFromPosition(0,0).Right>viewport.Left)throw new Exception("Overlap: "+t.Id);
                    if(canvas.Controls.Count<4||fpsBox==null||targetBox==null||graph==null)throw new Exception("Missing core controls: "+t.Id);
                    if(apply.Right>apply.Parent.Width||disable.Right>disable.Parent.Width||fpsBox.Right>fpsBox.Parent.Width)throw new Exception("Clipped FPS controls: "+t.Id);
                }
                fpsBox.Value=144;
                if(!presets.Any(b=>Convert.ToInt32(b.Tag)==144&&b.BackColor==theme.Accent))throw new Exception("Preset select: "+t.Id);
                fpsBox.Value=73;
                if(presets.Any(b=>b.BackColor==theme.Accent))throw new Exception("Custom selection: "+t.Id);
                currentPage="profiles";Build();if(!canvas.Controls.OfType<StudioCard>().Any())throw new Exception("Profile page: "+t.Id);
                currentPage="settings";Build();if(!canvas.Controls.OfType<StudioCard>().Any())throw new Exception("Preferences page: "+t.Id);
                currentPage="dashboard";
            }
            settings.LanguageCode="fa";theme=StudioTheme.All[0];Build();
            if(!side.Controls.OfType<Button>().Any(b=>b.Text.Contains("داشبورد")))throw new Exception("Persian navigation");
            settings.LanguageCode="en";Build();
            if(!side.Controls.OfType<Button>().Any(b=>b.Text.Contains("Dashboard")))throw new Exception("English navigation");
            string content="[General]\r\nName=keep\r\n[Framerate]\r\nLimit=0\r\nLimitDenominator=99\r\n";
            string patched=RtssProfileFile.SetFrameLimit(content,60);
            if(!patched.Contains("Limit=60")||!patched.Contains("LimitDenominator=1")||!patched.Contains("Name=keep"))throw new Exception("RTSS profile regression");
        }
    }
}
