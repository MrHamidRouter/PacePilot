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
    // Native owner-drawn controls: themed gradients and antialiased corners in every skin.
    public sealed class StudioButton : Button
    {
        bool hovering,pressed;
        public StudioButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,true);
            DoubleBuffered=true;
        }
        protected override void OnMouseEnter(EventArgs e){hovering=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hovering=false;pressed=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}
        protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            if(Width<3||Height<3)return;
            Rectangle rect=new Rectangle(0,0,Width-1,Height-1);
            Color back=BackColor;
            if(!Enabled)back=ControlPaint.Dark(back,.22f);
            else if(pressed)back=ControlPaint.Dark(back,.14f);
            else if(hovering)back=ControlPaint.Light(back,.12f);
            using(GraphicsPath shape=StudioCard.RoundRect(rect,9))
            using(LinearGradientBrush gradient=new LinearGradientBrush(rect,
                ControlPaint.Light(back,.13f),back,LinearGradientMode.Vertical))
            {
                g.FillPath(gradient,shape);
                using(Pen edge=new Pen(FlatAppearance.BorderColor.IsEmpty?ControlPaint.Light(back,.18f):FlatAppearance.BorderColor))
                    g.DrawPath(edge,shape);
            }
            TextFormatFlags flags=TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|
                TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine;
            if(TextAlign==ContentAlignment.MiddleLeft)flags|=TextFormatFlags.Left;
            else if(TextAlign==ContentAlignment.MiddleRight)flags|=TextFormatFlags.Right;
            else flags|=TextFormatFlags.HorizontalCenter;
            if(RightToLeft==RightToLeft.Yes)flags|=TextFormatFlags.RightToLeft;
            Rectangle textRect=new Rectangle(Padding.Left+5,0,Math.Max(1,Width-Padding.Left-Padding.Right-10),Height);
            TextRenderer.DrawText(g,Text,Font,textRect,Enabled?ForeColor:SystemColors.GrayText,flags);
        }
    }

    public class StudioCard : Panel
    {
        public StudioTheme Theme;
        public StudioCard(StudioTheme t) { Theme=t; DoubleBuffered=true; BackColor=t.Panel; Margin=Padding.Empty; }
        public static GraphicsPath RoundRect(Rectangle rect,int radius)
        {
            GraphicsPath path=new GraphicsPath();
            int r=Math.Min(radius*2,Math.Min(rect.Width,rect.Height));
            if(r<=0){path.AddRectangle(rect);return path;}
            path.AddArc(rect.X,rect.Y,r,r,180,90);
            path.AddArc(rect.Right-r,rect.Y,r,r,270,90);
            path.AddArc(rect.Right-r,rect.Bottom-r,r,r,0,90);
            path.AddArc(rect.X,rect.Bottom-r,r,r,90,90);
            path.CloseFigure();
            return path;
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if(Width<8||Height<8)return;
            using(GraphicsPath path=RoundRect(new Rectangle(0,0,Width,Height),12))
            {
                Region old=Region;Region=new Region(path);if(old!=null)old.Dispose();
            }
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            if(Width<3||Height<3){base.OnPaintBackground(e);return;}
            using(GraphicsPath shape=RoundRect(new Rectangle(0,0,Width,Height),12))
            using(LinearGradientBrush gradient=new LinearGradientBrush(
                new Rectangle(0,0,Width,Height),
                Theme.Light?ControlPaint.Light(Theme.Panel,.03f):ControlPaint.Light(Theme.Panel,.11f),
                Theme.Panel,LinearGradientMode.ForwardDiagonal))
            {
                e.Graphics.FillPath(gradient,shape);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            if(Width<3||Height<3)return;
            using(GraphicsPath path=RoundRect(new Rectangle(0,0,Width-1,Height-1),12))
            using(Pen border=new Pen(Theme.Border,1.15f))
                e.Graphics.DrawPath(border,path);
            using(Pen shine=new Pen(Color.FromArgb(Theme.Light?80:115,Theme.Accent),1.4f))
                e.Graphics.DrawLine(shine,16,1,Math.Max(17,Math.Min(Width-17,150)),1);
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
            int validSamples=Values.Count(x=>!float.IsNaN(x)&&x>0);
            if(validSamples<2)
            {
                string msg="WAITING FOR VERIFIED RTSS FRAME DATA";
                using(Font font=new Font("Segoe UI Semibold",10,FontStyle.Bold))
                using(Brush brush=new SolidBrush(Theme.Secondary))
                {
                    SizeF label=g.MeasureString(msg,font);
                    g.DrawString(msg,font,brush,
                        r.Left+Math.Max(0,(r.Width-label.Width)/2),
                        r.Top+Math.Max(0,(r.Height-label.Height)/2));
                }
                return;
            }
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
            PointF[] points=seq.ToArray();
            if(points.Length>=3)
            {
                float bottom=Height-22;
                using(GraphicsPath area=new GraphicsPath())
                {
                    area.AddLines(points);
                    area.AddLine(points[points.Length-1],new PointF(points[points.Length-1].X,bottom));
                    area.AddLine(new PointF(points[0].X,bottom),points[0]);
                    area.CloseFigure();
                    using(LinearGradientBrush shade=new LinearGradientBrush(
                        new Rectangle(0,0,Math.Max(1,Width),Math.Max(1,Height)),
                        Color.FromArgb(49,Theme.Graph),Color.FromArgb(0,Theme.Graph),
                        LinearGradientMode.Vertical))
                        g.FillPath(shade,area);
                }
            }
            using(Pen glow=new Pen(Color.FromArgb(38,Theme.Graph),7))g.DrawLines(glow,points);
            using(Pen pen=new Pen(Theme.Graph,2.1f))g.DrawLines(pen,points);
            using(Brush dot=new SolidBrush(Theme.Graph))
                g.FillEllipse(dot,points[points.Length-1].X-3,points[points.Length-1].Y-3,6,6);
        }
    }

    public sealed class StudioForm : Form
    {
        readonly bool testing;
        readonly AppSettings settings;
        readonly RtssControl rtss=new RtssControl();
        readonly RtssOsdBridge osd=new RtssOsdBridge();
        readonly SystemSampler sampler=new SystemSampler();
        SystemSample lastSystem;
        FrameSample lastFrame;
        Label cpuMonitorValue,ramMonitorValue,osdMonitorValue;
        Label cpuTempMonitor,gpuLoadMonitor,gpuTempMonitor,gpuClockMonitor,gpuPowerMonitor,vramMonitor,gpuVrmMonitor,vrmMonitor,sensorStatusMonitor;
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
        FrameActivityBar activity;
        Label frameFeedStatus,ramReadyValue;
        readonly Dictionary<string,Label> dashboardHardware=new Dictionary<string,Label>();
        readonly Dictionary<string,Label> hardwareCaptions=new Dictionary<string,Label>();
        readonly ToolTip hardwareTooltips=new ToolTip();
        HardwareInventory hardwareInventory=new HardwareInventory();
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
            Text="PacePilot Studio v"+GitHubUpdates.CurrentLabel+"  |  Mr.HamidRouter";
            try { Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { Icon=SystemIcons.Application; }
            currentTarget=String.IsNullOrEmpty(settings.LastTarget)?"Global":settings.LastTarget;
            Build();
            if(!testing)
            {
                InitializeRtss(); InitTray();
                timer=new System.Windows.Forms.Timer(); timer.Interval=500; timer.Tick+=(s,e)=>UpdateLive(); timer.Start();
                SetStartup(false);
                // Inventory is queried only once on a background worker, never on the UI timer.
                Load+=(s,e)=>BeginLoadHardwareInventory();
                Shown+=(s,e)=>BeginInvoke(new System.Action(()=>{AskInstall();ScanInstalledGames(false);CheckUpdates(false);}));
            }
            TopMost=settings.AlwaysOnTop;
            if(startInTray && !testing) { WindowState=FormWindowState.Minimized; ShowInTaskbar=false; }
        }
        void BeginLoadHardwareInventory()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(delegate(object unused)
            {
                HardwareInventory loaded;
                try { loaded=HardwareInventoryService.Read(); }
                catch { loaded=new HardwareInventory(); }
                try
                {
                    if(!IsDisposed && IsHandleCreated)
                        BeginInvoke(new System.Action(delegate
                        {
                            if(IsDisposed)return;
                            hardwareInventory=loaded;
                            UpdateHardwareCaptions();
                        }));
                }
                catch(InvalidOperationException){}
            });
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
            Button b=new StudioButton(); b.Text=L(en,fa); b.SetBounds(x,y,w,h);
            b.ForeColor=prominent?theme.AccentInk:theme.Ink;
            b.BackColor=prominent?theme.Accent:theme.Sidebar;
            b.FlatStyle=FlatStyle.Flat; b.FlatAppearance.BorderColor=prominent?theme.Accent:theme.Border;
            b.FlatAppearance.BorderSize=1; b.Cursor=Cursors.Hand;
            b.Font=new Font("Segoe UI Semibold",9.5f,FontStyle.Bold);
            b.FlatAppearance.MouseOverBackColor=prominent?ControlPaint.Light(theme.Accent,.14f):ControlPaint.Light(theme.Sidebar,.16f);
            b.FlatAppearance.MouseDownBackColor=prominent?ControlPaint.Dark(theme.Accent,.16f):theme.Panel;
            b.Resize+=(sender,e)=>
            {
                if(b.Width<8||b.Height<8)return;
                using(GraphicsPath rounded=StudioCard.RoundRect(new Rectangle(0,0,b.Width,b.Height),9))
                {
                    Region oldRegion=b.Region;b.Region=new Region(rounded);if(oldRegion!=null)oldRegion.Dispose();
                }
            };
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
            side=new PremiumSidebar(theme); side.Dock=DockStyle.Fill; side.BackColor=theme.Sidebar;side.AutoScroll=true;
            side.Margin=Padding.Empty; shell.Controls.Add(side,0,0);
            TextAt(side,"◈ PacePilot","◈ PacePilot",20,20,198,40,19,true,theme.Accent);
            TextAt(side,"PRECISION GAMING","ابزار حرفه‌ای گیمینگ",21,59,198,23,8,true,theme.Secondary);
            TextAt(side,"LANGUAGE / زبان","زبان / LANGUAGE",20,99,183,18,8,false,theme.Secondary);
            langBox=SelectBox(side,21,121,193,30,new string[]{"English","فارسی"},Fa?1:0);
            langBox.SelectedIndexChanged+=(s,e)=>{if(rebuilding)return; settings.LanguageCode=langBox.SelectedIndex==1?"fa":"en";Persist();Build();};
            TextAt(side,"UI THEME","پوسته برنامه",20,164,188,20,9,true,theme.Secondary);
            themeBox=SelectBox(side,21,187,193,32,StudioTheme.All.Select(x=>x.Display).ToArray(),Array.FindIndex(StudioTheme.All,x=>x.Id==theme.Id));
            themeBox.SelectedIndexChanged+=(s,e)=>{if(rebuilding||themeBox.SelectedIndex<0)return;settings.ThemeName=StudioTheme.All[themeBox.SelectedIndex].Id; theme=StudioTheme.Find(settings.ThemeName);Persist();Build();};
            string[] nav={"dashboard","profiles","monitor","settings","rtss","logs","about"};
            string[] en={"Dashboard","Game Library","Performance HUD","Appearance & Settings","RTSS & Tools","Diagnostics","About"};
            string[] fa={"داشبورد","کتابخانه بازی‌ها","پایش و نمایش بازی","ظاهر و تنظیمات","RTSS و ابزارها","گزارش و خطایابی","درباره برنامه"};
            string[] symbols={"⌂","▣","▤","⚙","◉","≡","ⓘ"};
            for(int i=0;i<nav.Length;i++)
            {
                string page=nav[i]; bool chosen=currentPage==page;
                Button button=Action(side,symbols[i]+"   "+en[i],symbols[i]+"   "+fa[i],14,231+i*46,205,42,chosen,(s,e)=>{currentPage=page;Build();});
                button.TextAlign=ContentAlignment.MiddleLeft; button.Padding=new Padding(10,0,0,0);
            }
            TextAt(side,"Developer:","توسعه‌دهنده:",22,566,194,23,9,false,theme.Secondary);
            TextAt(side,"Mr.HamidRouter","Mr.HamidRouter",22,589,194,28,11,true,theme.Accent);
            TextAt(side,"Gaming Performance Utility","ابزار تخصصی عملکرد بازی",22,627,190,25,8,false,theme.Secondary);
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
            dashboardHardware.Clear();hardwareCaptions.Clear();activity=null;frameFeedStatus=null;ramReadyValue=null;
            canvas.SuspendLayout();
            while(canvas.Controls.Count>0){Control removed=canvas.Controls[0];canvas.Controls.Remove(removed);removed.Dispose();}
            presets.Clear();
            int w=canvas.Width-38;
            if(currentPage=="dashboard") DrawDashboard(19,w);
            else if(currentPage=="profiles") DrawProfiles(19,w);
            else if(currentPage=="monitor") DrawMonitor(19,w);
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
        // Unified structure across ALL six visual themes; only artwork, palette and highlights change.
        void DrawDashboard(int x,int w)
        {
            Header(x,w,"PACEPILOT  /  PERFORMANCE STUDIO","PacePilot | استودیوی عملکرد بازی",
                "Gaming performance • Precision FPS limiter • Live verified monitoring",
                "کنترل دقیق نرخ فریم • پایش زنده • مدیریت اختصاصی بازی‌ها");
            int gap=12;
            int top=91;
            MakeHero(x,top,w,111);
            top+=123;
            bool compact=w<840;
            int left=compact?w:(int)(w*.57f);
            int right=compact?w:w-left-gap;
            int rowHeight=compact?376:170;
            MakeTarget(x,top,left,156);
            if(compact)
            {
                MakeStats(x,top+168,w,195,false);
                top+=rowHeight;
            }
            else
            {
                MakeStats(x+left+gap,top,right,156,false);
                top+=168;
            }
            MakeLimiter(x,top,left,201);
            if(compact)
            {
                MakeMemoryReady(x,top+213,w,171);
                top+=396;
            }
            else
            {
                MakeMemoryReady(x+left+gap,top,right,201);
                top+=213;
            }
            MakeGraph(x,top,w,209);
            top+=221;
            MakeFrameActivity(x,top,w,112);
            top+=124;
            int sensorColumns=w<620?3:(w<1140?5:9);
            int sensorRows=(9+sensorColumns-1)/sensorColumns;
            int sensorHeight=54+sensorRows*99;
            MakeHardwareStrip(x,top,w,sensorHeight);
            top+=sensorHeight+12;
            MakeOptions(x,top,w,160);
            top+=174;
            statusLabel=TextAt(canvas,"Ready. Choose a supported game. FPS/frametime require an active RTSS render feed.",
                "آماده؛ بازی سازگار را انتخاب کنید. نمودار نیازمند دریافت داده واقعی از RTSS است.",
                x,top,w,44,10,false,theme.Accent);
            canvas.Height=Math.Max(viewport.ClientSize.Height,top+70);
            RefreshSelection();RefreshRtssStatus();
        }
        void MakeFrameActivity(int x,int y,int w,int h)
        {
            StudioCard card=Card("FRAME ACTIVITY  •  VERIFIED DATA ONLY","فعالیت فریم • صرفاً داده واقعی",x,y,w,h);
            activity=new FrameActivityBar(theme);
            activity.SetBounds(15,41,w-30,h-54);
            activity.TargetFps=fpsBox==null?60:Math.Max(1,(int)fpsBox.Value);
            card.Controls.Add(activity);
            frameFeedStatus=TextAt(card,"WAITING FOR RENDER HOOK","در انتظار دریافت اطلاعات بازی",w/2,11,w/2-24,26,9,false,theme.Secondary);
            frameFeedStatus.TextAlign=ContentAlignment.MiddleRight;
        }
        void MakeHardwareStrip(int x,int y,int w,int h)
        {
            StudioCard card=Card("HARDWARE TELEMETRY  •  SYSTEM SENSORS","سنسورهای سخت‌افزاری • پایش زنده",x,y,w,h);
            string[] keys={"CPU","CTEMP","GPU","VRAM","RAM","PWR","TEMP","GVRM","VRM"};
            string[] en={"CPU Usage","CPU Temp","GPU Load","VRAM","RAM","GPU Power","GPU Temp","GPU VRM Temp","Board VRM Temp"};
            string[] fa={"درگیری CPU","دمای CPU","گرافیک","حافظه گرافیک","رم","توان GPU","دمای GPU","دمای VRM گرافیک","دمای VRM مادربرد"};
            int padding=11,gap=5,columns=w<620?3:(w<1140?5:9);
            int tileW=Math.Max(62,(w-2*padding-gap*(columns-1))/columns);
            for(int i=0;i<keys.Length;i++)
            {
                int x0=padding+(i%columns)*(tileW+gap);
                int y0=44+(i/columns)*99;
                Panel panel=new PremiumMetricPanel(theme);panel.SetBounds(x0,y0,tileW,94);card.Controls.Add(panel);
                Label name=TextAt(panel,en[i],fa[i],7,5,tileW-12,23,8,false,theme.Secondary);
                Label value=TextAt(panel,"—","—",7,28,tileW-12,34,tileW<98?10:13,true,theme.Accent);
                Label caption=TextAt(panel,"Identifying hardware…","در حال شناسایی قطعه…",7,63,tileW-12,23,7,false,theme.Secondary);
                hardwareCaptions[keys[i]]=caption;
                hardwareTooltips.SetToolTip(panel,caption.Text);
                if(keys[i]!="CPU"&&keys[i]!="RAM")
                {
                    panel.Cursor=Cursors.Help;
                    value.Cursor=Cursors.Help;
                    name.Cursor=Cursors.Help;
                    EventHandler sensorHelp=(sender,e)=>ShowSensorHelp();
                    panel.Click+=sensorHelp;value.Click+=sensorHelp;name.Click+=sensorHelp;
                }
                dashboardHardware[keys[i]]=value;
            }
            UpdateHardwareStrip();
            UpdateHardwareCaptions();
        }
        void UpdateHardwareCaptions()
        {
            string cpu=hardwareInventory.CpuName;
            string gpu=hardwareInventory.GpuName;
            string board=hardwareInventory.MotherboardName;
            string ram=hardwareInventory.RamSummary;
            foreach(KeyValuePair<string,Label> entry in hardwareCaptions)
            {
                Label caption=entry.Value;
                if(caption==null||caption.IsDisposed)continue;
                string identity=(entry.Key=="CPU"||entry.Key=="CTEMP")?cpu:
                    (entry.Key=="RAM")?ram:(entry.Key=="VRM")?board:gpu;
                caption.Text=identity;
                hardwareTooltips.SetToolTip(caption,entry.Key=="RAM" && 
                    !String.IsNullOrWhiteSpace(hardwareInventory.RamModules)?
                    hardwareInventory.RamModules:identity);
                if(caption.Parent!=null)
                    hardwareTooltips.SetToolTip(caption.Parent,identity);
            }
        }
        void OpenSensorMapping()
        {
            using(SensorMappingForm window=new SensorMappingForm(settings,Fa))
            {
                if(window.ShowDialog(this)!=DialogResult.OK)return;
                settings.GpuVrmSensorId=window.SelectedGpuId;
                settings.BoardVrmSensorId=window.SelectedBoardId;
                Persist();
                UpdateLive();
                DrawPage();
            }
        }
        void ShowSensorHelp()
        {
            string info=L("Built-in sensor engine: ","موتور داخلی حسگرها: ")+BuiltInSensors.Status+"\n"+
                L("Detected sensor entries: ","تعداد حسگرهای پیدا‌شده: ")+BuiltInSensors.ReadingCount.ToString()+"\n"+
                L("Windows GPU counters: ","شمارنده‌های گرافیک ویندوز: ")+HardwareFallback.Status+"\n"+
                L("Actual temperature sensors detected:","حسگرهای دمای واقعی شناسایی‌شده:")+"\n"+
                BuiltInSensors.TemperatureSensorReport+"\n\n"+
                L("PacePilot collects sensors by itself. No HWiNFO, MSI Afterburner or LibreHardwareMonitor installation or running process is required. If a sensor is blank, the device may not expose it, or Windows may block the sensor driver. Restart PacePilot as Administrator if needed; never disable Windows security features to force sensor access. VRM temperature is only available on compatible boards.",
                  "PacePilot خودش اطلاعات حسگرها را می‌خواند و نصب یا اجرای HWiNFO، MSI Afterburner یا LibreHardwareMonitor لازم نیست. اگر سنسوری خالی باشد، ممکن است سخت‌افزار آن را ارائه نکند یا ویندوز دسترسی به درایور حسگر را محدود کرده باشد. در صورت نیاز برنامه را با Administrator اجرا کنید؛ برای دسترسی سنسورها امنیت ویندوز را خاموش نکنید. دمای VRM تنها روی مادربردهای سازگار در دسترس است.");
            DialogResult choice=MessageBox.Show(this,info+"\n\n"+
                L("Open manual VRM Sensor Mapping now?","تنظیمات انتخاب دستی حسگر VRM باز شود؟"),
                L("Hardware sensor diagnostics","عیب‌یابی سنسورهای سخت‌افزار"),
                MessageBoxButtons.YesNo,MessageBoxIcon.Information);
            if(choice==DialogResult.Yes)OpenSensorMapping();
        }
        void UpdateHardwareStrip()
        {
            string ram=lastSystem.RamTotalGb>0?lastSystem.RamUsedGb.ToString("0.0")+"/"+lastSystem.RamTotalGb.ToString("0.0")+" GB":"—";
            string vram=lastSystem.VramUsedGb.HasValue?
                lastSystem.VramUsedGb.Value.ToString("0.0")+"/"+(lastSystem.VramTotalGb.HasValue?lastSystem.VramTotalGb.Value.ToString("0.0"):"?")+" GB":"—";
            string[] keys={"CPU","CTEMP","GPU","VRAM","RAM","PWR","TEMP","GVRM","VRM"};
            string[] values={
                lastSystem.CpuPercent.HasValue?lastSystem.CpuPercent.Value.ToString("0")+"%":"—",
                SensorText(lastSystem.CpuTempC,"°C"),
                SensorText(lastSystem.GpuLoad,"%"),vram,ram,SensorText(lastSystem.GpuPowerW,"W"),
                SensorText(lastSystem.GpuTempC,"°C"),SensorText(lastSystem.GpuVrmTempC,"°C"),SensorText(lastSystem.VrmTempC,"°C")
            };
            for(int i=0;i<keys.Length;i++)
            {
                Label l;
                if(dashboardHardware.TryGetValue(keys[i],out l)&&l!=null&&!l.IsDisposed)l.Text=values[i];
            }
        }
        void MakeMemoryReady(int x,int y,int w,int h)
        {
            StudioCard card=Card("PRE-GAME RAM READINESS","آماده‌سازی رم قبل از بازی",x,y,w,h);
            ramReadyValue=TextAt(card,"Checking available memory…","در حال بررسی حافظه آزاد…",17,45,w-34,34,12,true,theme.Accent);
            int btnY=h-49;
            Action(card,"Prepare / Inspect","بررسی و آماده‌سازی",17,btnY,Math.Max(126,(w-45)/2),36,true,
                (sender,e)=>PrepareRam());
            Action(card,"Heavy apps","برنامه‌های پرمصرف",Math.Max(158,(w-45)/2)+24,btnY,
                Math.Max(95,w-Math.Max(158,(w-45)/2)-42),36,false,(sender,e)=>ReviewHeavyApps());
            RamReadinessSnapshot ram=RamReadiness.Inspect();
            UpdateRamReadyValue(ram);
        }
        void UpdateRamReadyValue(RamReadinessSnapshot ram)
        {
            if(ramReadyValue==null||ramReadyValue.IsDisposed)return;
            ramReadyValue.Text=ram.TotalGb>0?
                L("Available ","حافظه آزاد ")+ram.AvailableGb.ToString("0.0")+" / "+ram.TotalGb.ToString("0.0")+" GB  ("+ram.AvailablePercent+"%)"
                :L("Memory status unavailable","وضعیت حافظه در دسترس نیست");
        }
        void PrepareRam()
        {
            RamReadinessSnapshot before=RamReadiness.Inspect();
            bool trimmed=RamReadiness.TrimOwnMemory();
            RamReadinessSnapshot after=RamReadiness.Inspect();
            UpdateRamReadyValue(after);
            string result=L("PacePilot RAM preparation completed.","آماده‌سازی رم PacePilot انجام شد.")+"\n"+
                L("Available before: ","حافظه آزاد قبل: ")+before.AvailableGb.ToString("0.00")+" GB\n"+
                L("Available now: ","حافظه آزاد اکنون: ")+after.AvailableGb.ToString("0.00")+" GB\n"+
                L("Trim own working set: ","آزادسازی حافظه اختصاصی برنامه: ")+(trimmed?"OK":"unavailable")+"\n\n"+
                L("No system processes or standby lists were modified. Memory trimming does not guarantee more FPS.",
                  "هیچ برنامه سیستمی بسته نشده و حافظه کش ویندوز دستکاری نشده است. افزایش FPS تضمینی نیست.");
            MessageBox.Show(this,result,L("RAM readiness","آماده‌سازی حافظه"),MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        void ReviewHeavyApps()
        {
            RamReadinessSnapshot ram=RamReadiness.Inspect();
            string apps=ram.HeavyApps.Count==0?
                L("No heavy desktop applications detected.","برنامه سنگین قابل مشاهده‌ای پیدا نشد.") :
                String.Join("\n",ram.HeavyApps.ToArray());
            MessageBox.Show(this,apps+"\n\n"+L("Close unneeded apps manually after saving your work; PacePilot does not kill processes.",
                "پس از ذخیره کارها، برنامه‌های غیرضروری را خودتان ببندید؛ PacePilot هیچ پردازشی را اجباری متوقف نمی‌کند."),
                L("Background memory usage","برنامه‌های پرمصرف"),MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        void MakeHero(int x,int y,int w,int h)
        {
            PremiumHero c=new PremiumHero(theme);
            c.SetBounds(x,y,w,h);canvas.Controls.Add(c);
            TextAt(c,"PACEPILOT   /   FRAME CONTROL","PacePilot  |  کنترل حرفه‌ای فریم",22,17,Math.Max(250,w*2/3),37,18,true,theme.Ink);
            TextAt(c,"PRECISION LIMITER     ·     VERIFIED LIVE TELEMETRY",
                "محدودسازی دقیق فریم   •   اطلاعات واقعی و زنده",
                23,55,Math.Max(250,w*2/3),28,10,true,theme.Secondary);
            TextAt(c,"●  RTSS-POWERED","●  مبتنی بر RTSS",23,88,190,20,8,true,theme.Accent);
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
                Panel sub=new PremiumMetricPanel(theme);sub.SetBounds(cx,cy,itemW,itemH);c.Controls.Add(sub);
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
            if(activity!=null&&!activity.IsDisposed)activity.TargetFps=Math.Max(1,val);
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
                bool blocked=Guard.ShouldBlock(currentTarget,warning);
                if(blocked){SetStatus(L("Protected game: no limit applied.","بازی محافظت‌شده: محدودیت اعمال نشد."),false);if(!quiet)MessageBox.Show(this,warning,"PacePilot safety",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
                if(quiet){SetStatus(L("Confirm Global mode in the dashboard before using a hotkey.","برای اعمال حالت عمومی باید ابتدا در داشبورد تأیید کنید."),false);return;}
                if(MessageBox.Show(this,L("Global limit applies to every compatible game. Proceed?","محدودیت عمومی روی بازی‌های سازگار اعمال می‌شود. ادامه می‌دهید؟"),"PacePilot",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
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
            lastFrame=sample;
            lastSystem=sampler.Read();
            // Self-contained GPU/CPU/VRM engine. No separate monitoring applications.
            BuiltInSensors.Enrich(ref lastSystem);
            HardwareFallback.Enrich(ref lastSystem);
            HardwareSensorService.ApplyMappings(ref lastSystem,settings);
            if(cpuTempMonitor!=null&&!cpuTempMonitor.IsDisposed)cpuTempMonitor.Text=SensorText(lastSystem.CpuTempC,"°C");
            if(gpuLoadMonitor!=null&&!gpuLoadMonitor.IsDisposed)gpuLoadMonitor.Text=SensorText(lastSystem.GpuLoad,"%");
            if(gpuTempMonitor!=null&&!gpuTempMonitor.IsDisposed)gpuTempMonitor.Text=SensorText(lastSystem.GpuTempC,"°C");
            if(gpuClockMonitor!=null&&!gpuClockMonitor.IsDisposed)gpuClockMonitor.Text=SensorText(lastSystem.GpuClockMhz,"MHz");
            if(gpuPowerMonitor!=null&&!gpuPowerMonitor.IsDisposed)gpuPowerMonitor.Text=SensorText(lastSystem.GpuPowerW,"W");
            if(vramMonitor!=null&&!vramMonitor.IsDisposed)vramMonitor.Text=lastSystem.VramUsedGb.HasValue?
                lastSystem.VramUsedGb.Value.ToString("0.0")+" / "+(lastSystem.VramTotalGb.HasValue?lastSystem.VramTotalGb.Value.ToString("0.0"):"?")+" GB":"—";
            if(gpuVrmMonitor!=null&&!gpuVrmMonitor.IsDisposed)gpuVrmMonitor.Text=SensorText(lastSystem.GpuVrmTempC,"°C");
            if(vrmMonitor!=null&&!vrmMonitor.IsDisposed)vrmMonitor.Text=SensorText(lastSystem.VrmTempC,"°C");
            if(sensorStatusMonitor!=null&&!sensorStatusMonitor.IsDisposed)
                sensorStatusMonitor.Text=lastSystem.GpuLoad.HasValue||lastSystem.GpuTempC.HasValue?
                    L("Built-in GPU sensors active","سنسورهای داخلی گرافیک فعال هستند"):
                    L("Internal sensors unavailable · click blank card for diagnostics","سنسور داخلی در دسترس نیست؛ روی کارت خالی کلیک کنید");
            if(cpuMonitorValue!=null&&!cpuMonitorValue.IsDisposed)cpuMonitorValue.Text=lastSystem.CpuPercent.HasValue?lastSystem.CpuPercent.Value.ToString("0")+"%":"—";
            if(ramMonitorValue!=null&&!ramMonitorValue.IsDisposed)ramMonitorValue.Text=lastSystem.RamTotalGb>0?lastSystem.RamUsedGb.ToString("0.0")+" / "+lastSystem.RamTotalGb.ToString("0.0")+" GB":"—";
            if(osdMonitorValue!=null&&!osdMonitorValue.IsDisposed)osdMonitorValue.Text=settings.OsdEnabled?L("ON  •  Alt+Shift+O","فعال  •  Alt+Shift+O"):L("OFF  •  Alt+Shift+O","خاموش  •  Alt+Shift+O");
            if(settings.OsdEnabled && rtss.IsRunning && !Guard.IsProtected(currentTarget) && !Guard.IsLauncherHelper(currentTarget))
                osd.Update(RtssOsdBridge.Format(sample,lastSystem,currentTarget,settings.OsdShowCpu,settings.OsdShowRam,settings.OsdShowGpu,settings.OsdShowVrm));
            if(liveValue!=null&&!liveValue.IsDisposed)liveValue.Text=sample.Valid?sample.Fps.ToString("0.0"):"--";
            if(frameValue!=null&&!frameValue.IsDisposed)frameValue.Text=sample.Valid?sample.FrameMs.ToString("0.00")+" ms":"--";
            if(graph!=null&&!graph.IsDisposed)graph.Push(sample.Valid?sample.FrameMs:float.NaN);
            if(activity!=null&&!activity.IsDisposed)activity.Push(sample.Valid?sample.FrameMs:float.NaN);
            if(frameFeedStatus!=null&&!frameFeedStatus.IsDisposed)
            {
                frameFeedStatus.Text=sample.Valid?
                    L("LIVE  •  "+sample.Fps.ToString("0")+" FPS","زنده • "+sample.Fps.ToString("0")+" فریم") :
                    (Guard.IsLauncherHelper(currentTarget)?
                        L("SELECT THE REAL GAME EXE","فایل اصلی بازی را انتخاب کنید"):
                        L("AWAITING RTSS GAME HOOK","در انتظار اتصال RTSS به بازی"));
                frameFeedStatus.ForeColor=sample.Valid?theme.Accent:Color.Orange;
            }
            UpdateHardwareStrip();
        }
        static string SensorText(double? value,string unit){return value.HasValue?value.Value.ToString("0.0")+" "+unit:"—";}
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

        void ScanInstalledGames() { ScanInstalledGames(true); }
        void ScanInstalledGames(bool announce)
        {
            if(testing||gameScanPending)return;
            gameScanPending=true;
            BackgroundWorker scan=new BackgroundWorker();
            scan.DoWork+=(sender,e)=>e.Result=GameDiscovery.Scan();
            scan.RunWorkerCompleted+=(sender,e)=>
            {
                gameScanPending=false;
                if(IsDisposed)return;
                if(e.Error!=null)
                {
                    if(announce)MessageBox.Show(this,e.Error.Message,L("Game scan","اسکن بازی‌ها"));
                    return;
                }
                List<DetectedGame> games=e.Result as List<DetectedGame>;
                if(games==null)return;
                int added=0;
                foreach(DetectedGame game in games)
                {
                    string exe=Path.GetFileName(game.Exe);
                    GameProfile item=settings.Games.FirstOrDefault(p=>String.Equals(p.Exe,exe,StringComparison.OrdinalIgnoreCase));
                    if(item==null)
                    {
                        settings.Games.Add(new GameProfile{
                            Exe=exe,FullPath=game.Exe,DisplayName=game.Title,Platform=game.Platform,Limit=60});
                        added++;
                    }
                    else if(String.Equals(item.FullPath,game.Exe,StringComparison.OrdinalIgnoreCase))
                    { item.DisplayName=game.Title;item.Platform=game.Platform; }
                }
                if(added>0)Persist();
                if(added>0 && (currentPage=="profiles"||currentPage=="dashboard"))DrawPage();
                if(announce)MessageBox.Show(this,
                    L("Games detected: ","بازی‌های شناسایی‌شده: ")+games.Count+"\n"+
                    L("New profiles: ","پروفایل‌های جدید: ")+added+"\n"+
                    L("Steam, Epic Games and GOG were checked. Some games need manual selection.",
                      "کتابخانه‌های Steam، Epic و GOG بررسی شدند؛ برخی بازی‌ها به انتخاب دستی نیاز دارند."),
                    L("Game library","کتابخانه بازی‌ها"),MessageBoxButtons.OK,MessageBoxIcon.Information);
            };
            scan.RunWorkerAsync();
        }
        void CheckUpdates(bool manual)
        {
            if(testing||updateCheckPending)return;
            if(!manual&&settings.LastUpdateCheckUtc>DateTime.UtcNow.AddHours(-24))return;
            updateCheckPending=true;
            BackgroundWorker worker=new BackgroundWorker();
            worker.DoWork+=(sender,e)=>e.Result=GitHubUpdates.Check();
            worker.RunWorkerCompleted+=(sender,e)=>
            {
                updateCheckPending=false;
                if(IsDisposed)return;
                if(e.Error!=null)
                {
                    if(manual)MessageBox.Show(this,
                        L("Cannot contact GitHub: ","اتصال به گیت‌هاب برقرار نشد: ")+e.Error.Message,
                        L("Update check","بررسی بروزرسانی"),MessageBoxButtons.OK,MessageBoxIcon.Warning);
                    return;
                }
                settings.LastUpdateCheckUtc=DateTime.UtcNow;Persist();
                GitHubRelease release=e.Result as GitHubRelease;
                if(GitHubUpdates.IsNewer(release))
                {
                    if(MessageBox.Show(this,
                        L("A new PacePilot version is available: ","نسخه جدید PacePilot منتشر شده است: ")+release.Tag+"\n"+
                        L("Open the official GitHub release?","صفحه رسمی دریافت در گیت‌هاب باز شود؟"),
                        L("PacePilot update","بروزرسانی PacePilot"),
                        MessageBoxButtons.YesNo,MessageBoxIcon.Information)==DialogResult.Yes)
                        OpenUrl(release.Url);
                }
                else if(manual)
                {
                    string installed=GitHubUpdates.CurrentLabel;
                    string latest=GitHubUpdates.ReleaseLabel(release);
                    string description=L("Installed version: ","نسخه نصب‌شده: ")+installed+"\n"+
                        L("Latest official GitHub release: ","آخرین نسخه منتشرشده در گیت‌هاب: ")+latest+"\n\n"+
                        L("No newer compatible release was found.","نسخه جدیدتری برای دریافت پیدا نشد.");
                    MessageBox.Show(this,description,L("Update check","بررسی بروزرسانی"),MessageBoxButtons.OK,MessageBoxIcon.Information);
                }
            };
            worker.RunWorkerAsync();
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
            Header(x,w,"Your Game Library","کتابخانه بازی‌های شما","Discover installed games and customize their FPS profiles.","بازی‌های نصب‌شده را پیدا کرده و پروفایل اختصاصی تنظیم کنید.");
            StudioCard c=Card("SAVED GAMES","بازی‌های ذخیره‌شده",x,101,w,Math.Max(230,110+settings.Games.Count*58));
            Action(c,"+ Add a game","+ افزودن بازی",17,51,170,37,true,(s,e)=>AddGame());
            Action(c,"Scan installed games","شناسایی بازی‌ها",201,51,Math.Min(240,w-220),37,false,(s,e)=>ScanInstalledGames());
            int i=0;
            foreach(GameProfile game in settings.Games.ToList())
            {
                GameProfile item=game;
                int rowY=100+i*58;
                TextAt(c,(String.IsNullOrWhiteSpace(game.DisplayName)?game.Exe:game.DisplayName)+"  •  "+game.Limit+" FPS",(String.IsNullOrWhiteSpace(game.DisplayName)?game.Exe:game.DisplayName)+"  •  "+game.Limit+" فریم",19,rowY,Math.Max(200,w-182),44,10,true);
                Action(c,"Edit","ویرایش",Math.Max(215,w-125),rowY+3,92,34,false,(s,e)=>{currentTarget=item.Exe;currentPage="dashboard";Build();});
                i++;
            }
            canvas.Height=Math.Max(viewport.Height,c.Bottom+90);
        }
        void DrawSettings(int x,int w)
        {
            Header(x,w,"Personalize PacePilot","شخصی‌سازی PacePilot",
                "Six native visual systems. All cards, graphs and controls remain interactive.",
                "شش پوسته گرافیکی واقعی؛ همه کارت‌ها، نمودارها و دکمه‌ها تعاملی هستند.");
            StudioCard c=Card("THEME GALLERY","گالری پوسته‌ها",x,101,w,433);
            int itemW=(w-63)/3;
            for(int i=0;i<6;i++)
            {
                StudioTheme th=StudioTheme.All[i];int px=16+(i%3)*(itemW+9),py=44+(i/3)*186;
                Panel tile=new Panel();tile.SetBounds(px,py,itemW,174);
                tile.BackColor=theme.Id==th.Id?theme.Accent:theme.Border;
                tile.Cursor=Cursors.Hand;c.Controls.Add(tile);
                PremiumThemePreview picture=new PremiumThemePreview(th);picture.SetBounds(3,3,itemW-6,135);
                picture.BackColor=th.Bg;picture.Cursor=Cursors.Hand;tile.Controls.Add(picture);
                Label name=new Label();name.Text=th.Display;name.SetBounds(3,141,itemW-6,25);
                name.ForeColor=theme.Id==th.Id?theme.AccentInk:theme.Ink;
                name.Font=new Font("Segoe UI",9f,FontStyle.Bold);
                name.TextAlign=ContentAlignment.MiddleCenter;name.Cursor=Cursors.Hand;tile.Controls.Add(name);
                EventHandler applyTheme=(sender,e)=>{settings.ThemeName=th.Id;theme=th;Persist();Build();};
                tile.Click+=applyTheme;picture.Click+=applyTheme;name.Click+=applyTheme;
            }
            StudioCard opt=Card("GENERAL OPTIONS","تنظیمات عمومی",x,550,w,200);
            Check(opt,"Start with Windows","اجرا همراه ویندوز",17,46,w/2-25,()=>settings.StartWithWindows,v=>{settings.StartWithWindows=v;SetStartup(true);});
            Check(opt,"Minimize to system tray","انتقال به کنار ساعت",w/2,46,w/2-20,()=>settings.MinimizeToTray,v=>settings.MinimizeToTray=v);
            Check(opt,"Always on top","همیشه روی سایر پنجره‌ها",17,91,w/2-25,()=>settings.AlwaysOnTop,v=>{settings.AlwaysOnTop=v;TopMost=v;});
            Check(opt,"Auto-select known games","انتخاب خودکار بازی‌های ذخیره‌شده",w/2,91,w/2-20,()=>settings.AutoSelectKnownGames,v=>settings.AutoSelectKnownGames=v);
            Action(opt,"Check for updates","بررسی بروزرسانی",17,138,210,41,true,(sender,e)=>CheckUpdates(true));
            StudioCard map=Card("SENSOR MAPPING  /  VRM","انتخاب و تطبیق سنسور VRM",x,765,w,151);
            TextAt(map,"Choose and save exact GPU VRM and motherboard VRM identifiers.",
                "شناسه واقعی سنسور VRM گرافیک و مادربرد را انتخاب و ذخیره کنید.",
                17,44,w-34,30,10,false,theme.Secondary);
            Action(map,"Map VRM Sensors…","انتخاب دستی سنسور VRM",17,92,260,40,true,(sender,e)=>OpenSensorMapping());
            canvas.Height=Math.Max(viewport.Height,963);
        }
        void ToggleOsd()
        {
            if(testing)return;
            if(settings.OsdEnabled)
            {
                settings.OsdEnabled=false;
                osd.Clear();
                Persist();UpdateLive();
                SetStatus(L("In-game overlay disabled.","نمایش اطلاعات داخل بازی خاموش شد."),true);
                return;
            }
            if(Guard.IsProtected(currentTarget)||Guard.IsLauncherHelper(currentTarget))
            {
                MessageBox.Show(this,L(
                    "This is a protected game or launcher process. PacePilot does not override its anti-cheat restrictions. Choose a compatible game to use the RTSS overlay.",
                    "این بازی یا پردازش لانچر محافظت‌شده است. PacePilot محدودیت‌های ضدتقلب را دور نمی‌زند. برای OSD یک بازی سازگار انتخاب کنید."),
                    "PacePilot OSD",MessageBoxButtons.OK,MessageBoxIcon.Information);
                return;
            }
            if(!rtss.IsReady)
            {
                MessageBox.Show(this,L("RTSS is not installed. Install RTSS first.","RTSS نصب نیست. ابتدا RTSS را نصب کنید."),"PacePilot OSD");
                return;
            }
            try
            {
                if(!rtss.IsRunning&&!rtss.Start())throw new InvalidOperationException("RTSS is not running.");
                if(!osd.Update(RtssOsdBridge.Format(lastFrame,lastSystem,currentTarget,settings.OsdShowCpu,settings.OsdShowRam,settings.OsdShowGpu,settings.OsdShowVrm)))
                    throw new IOException(osd.LastError);
                settings.OsdEnabled=true;Persist();UpdateLive();
                SetStatus(L("RTSS OSD enabled. Alt+Shift+O toggles the overlay.","نمایش OSD فعال شد؛ Alt+Shift+O برای روشن/خاموش."),true);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this,ex.Message,L("RTSS overlay unavailable","نمایش اطلاعات RTSS در دسترس نیست"),MessageBoxButtons.OK,MessageBoxIcon.Warning);
            }
        }
        void DrawMonitor(int x,int w)
        {
            Header(x,w,"Gaming Performance HUD","نمایش عملکرد هنگام بازی",
                "Live measurements, RTSS overlay and a configurable hotkey. No synthetic data.",
                "اندازه‌گیری زنده، نمایش اطلاعات روی بازی و میانبر قابل‌استفاده.");
            int gap=12;int half=(w-gap)/2;
            StudioCard cpu=Card("CPU LOAD","پردازنده | CPU",x,101,half,161);
            cpuMonitorValue=TextAt(cpu,"—","—",19,54,half-38,69,27,true,theme.Accent);
            TextAt(cpu,"Measured CPU load (%)","میزان استفاده واقعی از پردازنده",19,122,half-38,25,9,false,theme.Secondary);
            StudioCard ram=Card("SYSTEM RAM","حافظه RAM",x+half+gap,101,half,161);
            ramMonitorValue=TextAt(ram,"—","—",19,54,half-38,69,23,true,theme.Accent);
            TextAt(ram,"Used / installed physical memory","میزان مصرف و ظرفیت حافظه فیزیکی",19,122,half-38,25,9,false,theme.Secondary);
            StudioCard hud=Card("RTSS IN-GAME OVERLAY","نمایش اطلاعات روی بازی (OSD)",x,274,w,224);
            osdMonitorValue=TextAt(hud,settings.OsdEnabled?"ON  •  Alt+Shift+O":"OFF  •  Alt+Shift+O",
                settings.OsdEnabled?"فعال  •  Alt+Shift+O":"خاموش  •  Alt+Shift+O",
                20,50,w-40,38,15,true,theme.Accent);
            Action(hud,settings.OsdEnabled?"Hide OSD":"Show OSD",settings.OsdEnabled?"مخفی‌کردن OSD":"نمایش OSD",
                18,104,210,43,true,(sender,e)=>{ToggleOsd();DrawPage();});
            Check(hud,"Include CPU usage","نمایش استفاده CPU",249,109,Math.Max(140,(w-278)/2),
                ()=>settings.OsdShowCpu,v=>settings.OsdShowCpu=v);
            Check(hud,"Include RAM usage","نمایش مصرف RAM",249,147,Math.Max(140,(w-278)/2),
                ()=>settings.OsdShowRam,v=>settings.OsdShowRam=v);
            Check(hud,"Include GPU data","نمایش GPU",Math.Min(500,w-150),109,Math.Max(120,w-520),
                ()=>settings.OsdShowGpu,v=>settings.OsdShowGpu=v);
            Check(hud,"Include VRM temp","نمایش دمای VRM",Math.Min(500,w-150),147,Math.Max(120,w-520),
                ()=>settings.OsdShowVrm,v=>settings.OsdShowVrm=v);
            TextAt(hud,"Keyboard shortcut: Alt+Shift+O   |   Requires RTSS injection support for the selected game.",
                "میانبر: Alt+Shift+O  |  نمایش روی بازی نیازمند پشتیبانی آن بازی از RTSS است.",
                20,187,w-40,27,9,false,theme.Secondary);
            StudioCard preview=Card("LIVE FRAME-TIME BAR","نوار زنده زمان فریم",x,510,w,132);
            string frameText=RtssOsdBridge.FrameBar(lastFrame.Valid?lastFrame.FrameMs:float.NaN);
            Label bar=TextAt(preview,frameText,frameText,18,49,w-36,48,16,true,theme.Accent);
            bar.Font=new Font("Consolas",16,FontStyle.Bold);
            StudioCard hardware=Card("HARDWARE SENSORS • REAL VALUES","سنسورهای سخت‌افزار • داده واقعی",x,654,w,90);
            sensorStatusMonitor=TextAt(hardware,"Connecting...","در حال اتصال...",18,46,w-36,29,11,false,theme.Secondary);
            int third=(w-24)/3,space=12;
            StudioCard gpuUsage=Card("GPU USAGE","درگیری گرافیک",x,756,third,129);
            gpuLoadMonitor=TextAt(gpuUsage,"—","—",16,48,third-32,60,23,true,theme.Accent);
            StudioCard gpuTemp=Card("GPU TEMPERATURE","دمای گرافیک",x+third+space,756,third,129);
            gpuTempMonitor=TextAt(gpuTemp,"—","—",16,48,third-32,60,22,true,theme.Accent);
            StudioCard gpuClock=Card("GPU CORE CLOCK","فرکانس گرافیک",x+2*(third+space),756,third,129);
            gpuClockMonitor=TextAt(gpuClock,"—","—",16,48,third-32,60,22,true,theme.Accent);
            StudioCard gpuPower=Card("GPU POWER","توان مصرفی گرافیک",x,898,third,129);
            gpuPowerMonitor=TextAt(gpuPower,"—","—",16,48,third-32,60,22,true,theme.Accent);
            StudioCard vram=Card("VIDEO MEMORY","حافظه گرافیکی VRAM",x+third+space,898,third,129);
            vramMonitor=TextAt(vram,"—","—",14,48,third-28,60,17,true,theme.Accent);
            StudioCard gpuVrm=Card("GPU VRM TEMPERATURE","دمای VRM گرافیک",x+2*(third+space),898,third,129);
            gpuVrmMonitor=TextAt(gpuVrm,"—","—",16,48,third-32,60,21,true,theme.Accent);
            int bottomHalf=(w-12)/2;
            StudioCard cpuTemp=Card("CPU PACKAGE TEMPERATURE","دمای پردازنده CPU",x,1039,bottomHalf,102);
            cpuTempMonitor=TextAt(cpuTemp,"—","—",18,47,bottomHalf-36,42,22,true,theme.Accent);
            StudioCard boardVrm=Card("MOTHERBOARD VRM TEMPERATURE","دمای VRM مادربرد",x+bottomHalf+12,1039,bottomHalf,102);
            vrmMonitor=TextAt(boardVrm,"—","—",18,47,bottomHalf-36,42,22,true,theme.Accent);
            TextAt(canvas,"GPU/VRM readings require HWiNFO64 Sensors: Shared Memory Support.",
                "حسگرهای GPU و VRM در خود PacePilot خوانده می‌شوند؛ حسگرهای پشتیبانی‌نشده با — نمایش داده می‌شوند.",
                x,1156,w,49,10,false,theme.Secondary);
            canvas.Height=Math.Max(viewport.ClientSize.Height,1230);
            if(!testing)UpdateLive();
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
            TextAt(report,"Protected games are blocked individually; background anti-cheat services alone no longer block unrelated games. Use Alt+Shift+O to toggle the RTSS overlay.","بازی‌های محافظت‌شده به‌صورت مستقل محدود می‌شوند؛ سرویس ضدتقلب پس‌زمینه مانع سایر بازی‌ها نمی‌شود. کلید Alt+Shift+O برای OSD است.",17,104,w-37,100,10,false,theme.Secondary);
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
            Header(x,w,"About PacePilot","درباره PacePilot","Precision FPS management for Windows gaming.","مدیریت دقیق نرخ فریم برای بازی‌های ویندوز.");
            StudioCard c=Card("PACEPILOT STUDIO "+GitHubUpdates.CurrentLabel,"PacePilot Studio نسخه "+GitHubUpdates.CurrentLabel,x,101,w,367);
            TextAt(c,"Developer: Mr.HamidRouter","توسعه‌دهنده: Mr.HamidRouter",22,62,w-44,35,15,true,theme.Accent);
            TextAt(c,"Bilingual interface  |  Custom themes  |  RTSS integration","رابط دو‌زبانه | طرح‌های اختصاصی | یکپارچگی با RTSS",22,110,w-44,40,11,false);
            TextAt(c,"This is an independent, MIT-licensed RTSS frontend. It is not affiliated with FramePacer, RTSS or Guru3D. NVIDIA Reflex, DLSS frame generation, and a proprietary frame-injection engine are not implemented. RTSS installation requires consent and may need administrator approval.",
                "این برنامه رابط مستقلی برای RTSS با مجوز MIT است. به FramePacer، RTSS یا Guru3D وابسته نیست. قابلیت‌های NVIDIA Reflex، تولید فریم DLSS و موتور اختصاصی تزریق فریم پیاده‌سازی نشده‌اند. نصب RTSS به اجازه کاربر نیاز دارد.",22,166,w-44,125,10,false,theme.Secondary);
            Action(c,"Project on GitHub","پروژه در گیت‌هاب",22,304,215,40,true,(s,e)=>OpenUrl("https://github.com/MrHamidRouter/PacePilot"));
            Action(c,"Check for updates","بررسی بروزرسانی",250,304,210,40,false,(s,e)=>CheckUpdates(true));
            StudioCard trust=Card("TRANSPARENT RELEASE  /  VERIFY YOUR DOWNLOAD",
                "انتشار شفاف  |  امکان بررسی اصالت فایل",x,485,w,252);
            TextAt(trust,"Portable C# / WinForms x64. Source, build logs and SHA-256 checksums are public.",
                "نسخه قابل‌حمل C# / WinForms؛ سورس، گزارش Build و کد هش SHA-256 عمومی هستند.",
                20,45,w-40,46,11,true,theme.Ink);
            TextAt(trust,"An unsigned EXE may show Unknown Publisher / SmartScreen. A checksum verifies the",
                "فایل بدون امضای معتبر ممکن است در SmartScreen ناشر ناشناس نمایش دهد.",
                20,100,w-40,26,10,false,theme.Secondary);
            TextAt(trust,"download against the published release, but it cannot guarantee malware safety.",
                "تطبیق SHA-256 با نسخه رسمی، تغییرنکردن فایل را نشان می‌دهد؛ بی‌خطر بودن را تضمین نمی‌کند.",
                20,126,w-40,43,10,false,theme.Secondary);
            int trustButton=(w-64)/3;
            Action(trust,"Copy EXE SHA-256","کپی هش SHA-256",20,185,trustButton,42,true,(s,e)=>CopyExecutableHash());
            Action(trust,"Official downloads","دانلود رسمی",32+trustButton,185,trustButton,42,false,
                (s,e)=>OpenUrl("https://github.com/MrHamidRouter/PacePilot/releases/latest"));
            Action(trust,"Build & security","ساخت و امنیت",44+trustButton*2,185,trustButton,42,false,
                (s,e)=>OpenUrl("https://github.com/MrHamidRouter/PacePilot/blob/main/Docs/SECURITY_AND_RELEASE.md"));
            canvas.Height=Math.Max(viewport.Height,790);
        }
        void CopyExecutableHash()
        {
            try
            {
                string path=Application.ExecutablePath;
                string sha=ReleaseTrust.Sha256File(path);
                Clipboard.SetText(sha);
                MessageBox.Show(this,L(
                    "Local EXE SHA-256 (copied): ",
                    "کد SHA-256 فایل فعلی (کپی شد): ")+sha+"\n\n"+
                    ReleaseTrust.EmbeddedSignatureInfo(path)+"\n"+
                    L("Compare with INTEGRITY_MANIFEST.json in the official GitHub release ZIP. This check alone does not establish malware safety.",
                      "مقدار را با INTEGRITY_MANIFEST.json در ZIP رسمی گیت‌هاب مقایسه کنید. بررسی هش به‌تنهایی بی‌خطر بودن برنامه را ثابت نمی‌کند."),
                    L("PacePilot download integrity","بررسی اصالت فایل PacePilot"),
                    MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
            catch(Exception e){MessageBox.Show(this,e.Message,L("Checksum error","خطای بررسی هش"),MessageBoxButtons.OK,MessageBoxIcon.Warning);}
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
            if(persist && settings.StartWithWindows)
                MessageBox.Show(this,
                    L("This version requests Administrator permission on every launch. Windows cannot guarantee a silent elevated startup. A UAC prompt may appear at sign-in. For quiet background startup use a non-elevated monitoring process.",
                      "این نسخه هنگام اجرا درخواست Administrator می‌دهد. اجرای خودکار با دسترسی مدیر بدون پنجره UAC در ویندوز تضمین نمی‌شود و ممکن است هنگام ورود به ویندوز درخواست تأیید ظاهر شود."),
                    L("Startup and Administrator access","اجرای خودکار و دسترسی مدیر"),MessageBoxButtons.OK,MessageBoxIcon.Information);
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
                int hotkey=m.WParam.ToInt32();
                if(hotkey==0xD103){ToggleOsd();return;}
                if(currentPage!="dashboard"){currentPage="dashboard";Build();}
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
                WinApi.RegisterHotKey(Handle,0xD103,0x1|0x4,(uint)Keys.O);
            }
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            if(!testing)
            {
                WinApi.UnregisterHotKey(Handle,0xD100);WinApi.UnregisterHotKey(Handle,0xD101);WinApi.UnregisterHotKey(Handle,0xD102);WinApi.UnregisterHotKey(Handle,0xD103);
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
            if(timer!=null)timer.Dispose();osd.Clear();hardwareTooltips.Dispose();
            if(tray!=null){tray.Visible=false;tray.Dispose();}
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
                    if(activity==null||dashboardHardware.Count!=9||hardwareCaptions.Count!=9||
                       ramReadyValue==null||frameFeedStatus==null)
                        throw new Exception("Missing RAM readiness, hardware, or RTSS activity widgets: "+t.Id);
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
            lastSystem.CpuTempC=64;
            UpdateHardwareStrip();
            if(!dashboardHardware.ContainsKey("CTEMP") ||
               dashboardHardware["CTEMP"].Text!=SensorText(64,"°C"))
                throw new Exception("CPU temperature tile is missing or not connected to live sensor data");
            lastSystem.CpuTempC=null;
            if(!side.Controls.OfType<Button>().Any(b=>b.Text.Contains("داشبورد")))throw new Exception("Persian navigation");
            settings.LanguageCode="en";Build();
            if(!side.Controls.OfType<Button>().Any(b=>b.Text.Contains("Dashboard")))throw new Exception("English navigation");
            if(!GameDiscovery.SelfTest())throw new Exception("Game discovery parser");
            if(!GitHubUpdates.SelfTest())throw new Exception("GitHub update comparator");
            if(!ReleaseTrust.SelfTest())throw new Exception("SHA-256 release-integrity implementation");
            if(!SystemSampler.SelfTest())throw new Exception("RTSS OSD formatter");
            if(!HwInfoBridge.SelfTest())throw new Exception("HWiNFO sensor parser");
            if(!HardwareFallback.SelfTest())throw new Exception("Windows GPU performance counter fallback");
            if(!BuiltInSensors.SelfTest())throw new Exception("Built-in sensor engine / missing bundled DLL");
            if(!HardwareInventoryService.SelfTest()||!HardwareSensorService.SelfTest())
                throw new Exception("Hardware inventory or mapping model");
            using(SensorMappingForm mappingForm=new SensorMappingForm(new AppSettings(),true))
            {
                if(mappingForm.Controls.OfType<ComboBox>().Count()!=2 ||
                   mappingForm.SelectedGpuId!="" || mappingForm.SelectedBoardId!="")
                    throw new Exception("Sensor mapping form controls or defaults");
            }
            // Verify a saved sensor identifier survives XML serialization.
            AppSettings mappingCheck=new AppSettings();
            mappingCheck.GpuVrmSensorId="/gpu-amd/0/temperature/1";
            mappingCheck.BoardVrmSensorId="/lpc/nuvoton/temperature/3";
            System.Xml.Serialization.XmlSerializer serializer=
                new System.Xml.Serialization.XmlSerializer(typeof(AppSettings));
            using(System.IO.MemoryStream stream=new System.IO.MemoryStream())
            {
                serializer.Serialize(stream,mappingCheck);
                stream.Position=0;
                AppSettings restored=(AppSettings)serializer.Deserialize(stream);
                if(restored.GpuVrmSensorId!=mappingCheck.GpuVrmSensorId ||
                   restored.BoardVrmSensorId!=mappingCheck.BoardVrmSensorId)
                    throw new Exception("VRM mapping XML settings roundtrip");
            }
            if(!AfterburnerBridge.SelfTest())throw new Exception("Afterburner shared memory parser");
            if(!RamReadiness.SelfTest()||!FrameActivityBar.SelfTest())throw new Exception("RAM readiness or real frametime visual");
            // Native vector graphics should always render at scale without needing JPG artwork.
            using(PremiumHero hero=new PremiumHero(StudioTheme.All[0]))
            using(PremiumThemePreview preview=new PremiumThemePreview(StudioTheme.All[0]))
            {
                hero.Size=new Size(550,110);preview.Size=new Size(230,130);
                using(Bitmap frame=new Bitmap(550,110))hero.DrawToBitmap(frame,new Rectangle(0,0,550,110));
                using(Bitmap tile=new Bitmap(230,130))preview.DrawToBitmap(tile,new Rectangle(0,0,230,130));
            }
            if(!Guard.IsProtected("TslGame.exe") || Guard.IsProtected("Cyberpunk2077.exe") ||
               !Guard.IsLauncherHelper("UnrealCEFSubProcess_3071.exe") ||
               Guard.ShouldBlock("Cyberpunk2077.exe",Guard.Check("Cyberpunk2077.exe")))
                throw new Exception("Protected title / unrelated game compatibility guard");
            currentPage="monitor";Build();if(!canvas.Controls.OfType<StudioCard>().Any())throw new Exception("Performance monitor page");
            string content="[General]\r\nName=keep\r\n[Framerate]\r\nLimit=0\r\nLimitDenominator=99\r\n";
            string patched=RtssProfileFile.SetFrameLimit(content,60);
            if(!patched.Contains("Limit=60")||!patched.Contains("LimitDenominator=1")||!patched.Contains("Name=keep"))throw new Exception("RTSS profile regression");
        }
    }
}
