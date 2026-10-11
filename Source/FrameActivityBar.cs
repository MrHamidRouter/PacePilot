// PacePilot 2.3 | Real frametime activity and safe pre-game RAM readiness.
// No synthetic samples, process killing, standby-list flushing or undocumented drivers.
// Developer: Mr.HamidRouter
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PacePilot
{
    public sealed class FrameActivityBar : Control
    {
        readonly List<float> samples=new List<float>();
        public StudioTheme Theme;
        public int TargetFps=60;
        public FrameActivityBar(StudioTheme t)
        {
            Theme=t;DoubleBuffered=true;BackColor=t.Panel;
            MinimumSize=new Size(130,42);
        }
        public void Push(float ms)
        {
            samples.Add(ms>0 && ms<1000 && !float.IsNaN(ms) && !float.IsInfinity(ms)?ms:float.NaN);
            if(samples.Count>150)samples.RemoveAt(0);
            Invalidate();
        }
        public int ValidCount {get{return samples.Count(x=>!float.IsNaN(x));}}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            int left=12,right=Width-10,baseline=Height-13;
            if(right<=left||baseline<=10)return;
            if(ValidCount==0)
            {
                using(Font font=new Font("Segoe UI",9))
                using(Brush br=new SolidBrush(Theme.Secondary))
                {
                    string label="WAITING FOR REAL RTSS FRAME SAMPLES  ·  LAUNCH A SUPPORTED GAME";
                    SizeF size=g.MeasureString(label,font);
                    g.DrawString(label,font,br,Math.Max(4,(Width-size.Width)/2),Math.Max(6,(Height-size.Height)/2));
                }
                return;
            }
            int maxBars=Math.Max(12,(right-left)/5),start=Math.Max(0,samples.Count-maxBars);
            float reference=1000f/Math.Max(1,TargetFps);
            float topMs=Math.Max(40,reference*3f);
            float bw=(right-left)/(float)Math.Max(1,samples.Count-start);
            for(int i=start;i<samples.Count;i++)
            {
                float ms=samples[i];
                if(float.IsNaN(ms))continue;
                float ratio=Math.Min(1f,ms/topMs);
                int h=Math.Max(3,(int)(ratio*(baseline-4)));
                RectangleF bar=new RectangleF(left+(i-start)*bw,baseline-h,Math.Max(1,bw-1.25f),h);
                Color color=ms>reference*1.65f?Color.OrangeRed:Theme.Accent;
                using(LinearGradientBrush brush=new LinearGradientBrush(bar,Color.FromArgb(240,color),Color.FromArgb(70,color),LinearGradientMode.Vertical))
                    g.FillRectangle(brush,bar);
            }
            using(Font f=new Font("Segoe UI",8))
            using(Brush b=new SolidBrush(Theme.Secondary))
                g.DrawString("REAL RTSS SAMPLES   "+ValidCount.ToString()+" / "+samples.Count.ToString(),f,b,12,Height-12);
        }
        public static bool SelfTest()
        {
            using(FrameActivityBar b=new FrameActivityBar(StudioTheme.All[0]))
            {
                b.Push(float.NaN);b.Push(-1);b.Push(16.7f);
                return b.ValidCount==1;
            }
        }
    }

    public sealed class RamReadinessSnapshot
    {
        public double TotalGb,UsedGb,AvailableGb,AppWorkingSetMb;
        public readonly List<string> HeavyApps=new List<string>();
        public int AvailablePercent {get{return TotalGb>0?(int)Math.Max(0,Math.Min(100,AvailableGb*100/TotalGb)):0;}}
    }
    public static class RamReadiness
    {
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Auto)]
        struct MemoryStatus
        {
            public uint length,load;
            public ulong totalPhysical,availablePhysical,totalPage,availablePage,totalVirtual,availableVirtual,availableExtended;
        }
        [DllImport("kernel32.dll",CharSet=CharSet.Auto,SetLastError=true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
        [DllImport("psapi.dll",SetLastError=true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        static extern bool EmptyWorkingSet(IntPtr handle);
        public static RamReadinessSnapshot Inspect()
        {
            RamReadinessSnapshot s=new RamReadinessSnapshot();
            MemoryStatus m=new MemoryStatus();m.length=(uint)Marshal.SizeOf(typeof(MemoryStatus));
            if(GlobalMemoryStatusEx(ref m) && m.totalPhysical>0)
            {
                s.TotalGb=m.totalPhysical/1073741824.0;
                s.AvailableGb=m.availablePhysical/1073741824.0;
                s.UsedGb=s.TotalGb-s.AvailableGb;
            }
            try {using(Process own=Process.GetCurrentProcess())s.AppWorkingSetMb=own.WorkingSet64/1048576.0;}catch{}
            try
            {
                List<KeyValuePair<string,long>> top=new List<KeyValuePair<string,long>>();
                int self=Process.GetCurrentProcess().Id;
                foreach(Process p in Process.GetProcesses())
                {
                    using(p)
                    {
                        try
                        {
                            if(p.Id==self||p.Id==0||p.SessionId!=Process.GetCurrentProcess().SessionId)continue;
                            if(p.MainWindowHandle==IntPtr.Zero)continue; // user-facing apps only
                            if(p.WorkingSet64>350L*1048576L)
                                top.Add(new KeyValuePair<string,long>(p.ProcessName,p.WorkingSet64));
                        }catch{}
                    }
                }
                foreach(var app in top.OrderByDescending(x=>x.Value).Take(4))
                    s.HeavyApps.Add(app.Key+" — "+(app.Value/1048576L)+" MB");
            }catch{}
            return s;
        }
        // Explicit user action. Only trims this application's pages, never another process.
        public static bool TrimOwnMemory()
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            try
            {
                using(Process current=Process.GetCurrentProcess())
                    return EmptyWorkingSet(current.Handle);
            }
            catch {return false;}
        }
        public static bool SelfTest()
        {
            RamReadinessSnapshot s=new RamReadinessSnapshot();
            s.TotalGb=16;s.AvailableGb=4;
            return s.AvailablePercent==25;
        }
    }
}
