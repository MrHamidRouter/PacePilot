// PacePilot v2.4 Premium Visual System
// All graphics are native vector / GDI+ primitives: no screenshot wallpaper.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PacePilot
{
    internal static class PremiumPaint
    {
        public static Color Alpha(Color c,int alpha)
        { return Color.FromArgb(Math.Max(0,Math.Min(255,alpha)),c); }
        public static Color Mix(Color a,Color b,float amount)
        {
            amount=Math.Max(0,Math.Min(1,amount));
            return Color.FromArgb((int)(a.R*(1-amount)+b.R*amount),
                                  (int)(a.G*(1-amount)+b.G*amount),
                                  (int)(a.B*(1-amount)+b.B*amount));
        }
        public static void Glow(Graphics g,Rectangle rect,Color accent)
        {
            if(rect.Width<2||rect.Height<2)return;
            using(GraphicsPath ellipse=new GraphicsPath())
            {
                ellipse.AddEllipse(rect);
                using(PathGradientBrush shade=new PathGradientBrush(ellipse))
                {
                    shade.CenterColor=Alpha(accent,64);
                    shade.SurroundColors=new Color[]{Alpha(accent,0)};
                    g.FillPath(shade,ellipse);
                }
            }
        }
        public static void Trace(Graphics g,Color c,float thickness,params PointF[] points)
        {
            if(points.Length<2)return;
            using(Pen glow=new Pen(Alpha(c,29),thickness+7))
            using(Pen line=new Pen(Alpha(c,180),thickness))
            {
                g.DrawLines(glow,points);
                g.DrawLines(line,points);
            }
        }
        public static void Ornament(Graphics g,Rectangle r,StudioTheme t)
        {
            if(r.Width<30||r.Height<20)return;
            g.SmoothingMode=SmoothingMode.AntiAlias;
            Glow(g,new Rectangle(r.Right-r.Width/3,r.Y-r.Height/2,r.Width/2,r.Height*2),t.Accent);
            Random rng=new Random(24);
            using(Pen grid=new Pen(Alpha(t.Accent,t.Light?19:22),1))
            {
                for(int i=0;i<13;i++)
                {
                    int xx=r.Left+(i*r.Width/13);
                    float skew=r.Height*.4f;
                    g.DrawLine(grid,xx,r.Bottom,xx+skew,r.Top);
                }
                for(int i=0;i<5;i++)
                    g.DrawLine(grid,r.Left,r.Top+(r.Height*i/5),r.Right,r.Top+(r.Height*i/5));
            }
            float cy=r.Top+r.Height*.56f;
            Trace(g,t.Accent,1.7f,new PointF(r.Left+r.Width*.20f,cy+13),
                  new PointF(r.Left+r.Width*.40f,cy-9),
                  new PointF(r.Left+r.Width*.75f,cy-9),
                  new PointF(r.Left+r.Width*.93f,cy-23));
            using(Pen secondary=new Pen(Alpha(t.Ink,48),1.1f))
            {
                g.DrawArc(secondary,new Rectangle(r.Right-(int)(r.Width*.37f),r.Top-(int)(r.Height*.52f),
                              (int)(r.Width*.49f),(int)(r.Height*1.1f)),35,210);
                g.DrawArc(secondary,new Rectangle(r.Right-(int)(r.Width*.31f),r.Top-(int)(r.Height*.43f),
                              (int)(r.Width*.36f),(int)(r.Height*.82f)),35,230);
            }
            using(SolidBrush b=new SolidBrush(Alpha(t.Accent,200)))
            {
                for(int i=0;i<3;i++)
                    g.FillRectangle(b,r.Right-92+i*20,r.Top+19,13,3);
            }
        }
    }

    public sealed class PremiumMetricPanel : Panel
    {
        public StudioTheme Theme;
        public PremiumMetricPanel(StudioTheme theme)
        {
            Theme=theme;BackColor=theme.Sidebar;DoubleBuffered=true;
            SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|
                     ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if(Width<6||Height<6)return;
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(GraphicsPath r=StudioCard.RoundRect(new Rectangle(0,0,Width-1,Height-1),8))
            using(LinearGradientBrush fill=new LinearGradientBrush(
                new Rectangle(0,0,Width,Height),
                PremiumPaint.Mix(Theme.Sidebar,Theme.Panel,.18f),
                PremiumPaint.Mix(Theme.Sidebar,Theme.Panel,.56f),
                LinearGradientMode.ForwardDiagonal))
            using(Pen outline=new Pen(Theme.Border,1))
            {
                e.Graphics.FillPath(fill,r);
                e.Graphics.DrawPath(outline,r);
            }
            using(SolidBrush p=new SolidBrush(PremiumPaint.Alpha(Theme.Accent,190)))
                e.Graphics.FillRectangle(p,9,9,3,3);
        }
    }

    // Fills only the actual sidebar surface; menu controls stay independently clickable.
    public sealed class PremiumSidebar : Panel
    {
        public StudioTheme Theme;
        public PremiumSidebar(StudioTheme t)
        {
            Theme=t;DoubleBuffered=true;BackColor=t.Sidebar;
            SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|
                     ControlStyles.OptimizedDoubleBuffer,true);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Rectangle bounds=ClientRectangle;
            if(bounds.Width<2||bounds.Height<2)return;
            using(LinearGradientBrush bg=new LinearGradientBrush(bounds,
                     PremiumPaint.Mix(Theme.Sidebar,Theme.Panel,.26f),Theme.Sidebar,
                     LinearGradientMode.Vertical))
                e.Graphics.FillRectangle(bg,bounds);
            using(Pen border=new Pen(Theme.Border))
                e.Graphics.DrawLine(border,bounds.Width-1,0,bounds.Width-1,bounds.Height);
            PremiumPaint.Glow(e.Graphics,new Rectangle(-170,-90,340,270),Theme.Accent);
            using(Pen track=new Pen(PremiumPaint.Alpha(Theme.Accent,18),1))
                for(int n=0;n<5;n++)
                    e.Graphics.DrawLine(track,0,bounds.Height-90-n*17,bounds.Width,bounds.Height-150-n*17);
        }
    }

    // Semantic hero, NOT a crop of a screenshot; maintains visual quality at any DPI.
    public sealed class PremiumHero : StudioCard
    {
        public PremiumHero(StudioTheme theme) : base(theme) {}
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Rectangle r=ClientRectangle;
            if(r.Width<2||r.Height<2)return;
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(GraphicsPath p=RoundRect(new Rectangle(0,0,r.Width,r.Height),16))
            using(LinearGradientBrush fill=new LinearGradientBrush(r,
                       PremiumPaint.Mix(Theme.Panel,Theme.Sidebar,.24f),
                       PremiumPaint.Mix(Theme.Sidebar,Theme.Accent,Theme.Light?.11f:.15f),
                       LinearGradientMode.Horizontal))
                e.Graphics.FillPath(fill,p);
            GraphicsState state=e.Graphics.Save();
            e.Graphics.SetClip(new Rectangle(r.Width/3,1,r.Width-r.Width/3-1,r.Height-2));
            PremiumPaint.Ornament(e.Graphics,new Rectangle(0,0,r.Width,r.Height),Theme);
            e.Graphics.Restore(state);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using(Pen p=new Pen(PremiumPaint.Alpha(Theme.Accent,180),2))
                e.Graphics.DrawLine(p,18,Height-3,Math.Min(Width-20,158),Height-3);
        }
    }

    // Six genuine renderable UI thumbnails, made of the same card/graph/button primitives.
    public sealed class PremiumThemePreview : Control
    {
        public StudioTheme PreviewTheme;
        public PremiumThemePreview(StudioTheme theme)
        {
            PreviewTheme=theme;DoubleBuffered=true;Cursor=Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|
                     ControlStyles.OptimizedDoubleBuffer,true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            StudioTheme t=PreviewTheme;
            Rectangle r=new Rectangle(0,0,Math.Max(1,Width-1),Math.Max(1,Height-1));
            using(SolidBrush b=new SolidBrush(t.Bg)) g.FillRectangle(b,r);
            int side=Math.Max(24,(int)(Width*.23f));
            using(SolidBrush b=new SolidBrush(t.Sidebar))g.FillRectangle(b,0,0,side,Height);
            using(SolidBrush b=new SolidBrush(t.Accent))
            {
                g.FillRectangle(b,6,8,Math.Max(9,side-12),3);
                g.FillRectangle(b,6,30,3,10);
            }
            using(Pen subtle=new Pen(t.Border,2))
            {
                for(int i=0;i<5;i++)g.DrawLine(subtle,7,51+i*16,side-7,51+i*16);
            }
            int main=side+8,avail=Math.Max(10,Width-main-8);
            using(SolidBrush b=new SolidBrush(t.Panel))
            {
                g.FillRectangle(b,main,7,avail,22);
                g.FillRectangle(b,main,35,(int)(avail*.58f),40);
                g.FillRectangle(b,main+(int)(avail*.58f)+4,35,avail-(int)(avail*.58f)-4,40);
                g.FillRectangle(b,main,81,avail,Height-88);
            }
            using(Pen b=new Pen(t.Border,1))
            {
                g.DrawRectangle(b,main,7,avail,22);
                g.DrawRectangle(b,main,81,avail,Math.Max(2,Height-88));
            }
            using(SolidBrush a=new SolidBrush(t.Accent))
            {
                g.FillRectangle(a,main+6,14,Math.Min(40,avail/3),3);
                g.FillRectangle(a,main+6,63,Math.Max(11,(int)(avail*.38f)),6);
                g.FillRectangle(a,main+6,87,Math.Max(11,(int)(avail*.38f)),4);
            }
            using(Pen p=new Pen(t.Accent,1.8f))
            {
                float w=avail-16;float h=Math.Max(10,Height-111);
                PointF[] points=new PointF[14];
                for(int i=0;i<14;i++)
                {
                    float x=main+8+i*w/13;
                    float y=Height-15-h*(.46f+(float)Math.Sin(i*.9f)*.12f);
                    points[i]=new PointF(x,y);
                }
                g.DrawLines(p,points);
            }
            if(Width>5&&Height>5)
                using(Pen stroke=new Pen(t.Border))g.DrawRectangle(stroke,r);
        }
    }
}
