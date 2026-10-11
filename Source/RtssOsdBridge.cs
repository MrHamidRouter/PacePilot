// PacePilot RTSS On-Screen Display bridge (RTSSSharedMemoryV2).
// Uses RTSS's supported client OSD slots; never injects into a game process.
// Developer: Mr.HamidRouter
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace PacePilot
{
    public sealed class RtssOsdBridge
    {
        const string Mapping = "RTSSSharedMemoryV2";
        const string Owner = "PacePilot";
        const uint Signature = 0x52545353;  // Official RTSS shared-memory signature.
        public string LastError { get; private set; }

        public static string FrameBar(float ms)
        {
            if (float.IsNaN(ms) || float.IsInfinity(ms) || ms <= 0) return "[--------------------]";
            int active = Math.Max(0, Math.Min(20, (int)Math.Round(ms / 50.0f * 20)));
            return "[" + new string('#', active) + new string('-', 20-active) + "]";
        }

        public static string Format(FrameSample sample, SystemSample sys, string exe, bool cpu, bool ram)
        {
            StringBuilder text=new StringBuilder();
            text.Append("PacePilot | ").Append(exe=="Global"?"All compatible games":exe).Append('\n');
            text.Append("FPS ").Append(sample.Valid?sample.Fps.ToString("0.0"):"--");
            text.Append("   FRAME ").Append(sample.Valid?sample.FrameMs.ToString("0.00")+"ms":"--").Append('\n');
            text.Append("FRAME TIME  ").Append(FrameBar(sample.Valid?sample.FrameMs:float.NaN));
            if (cpu && sys.CpuPercent.HasValue) text.Append("\nCPU ").Append(sys.CpuPercent.Value.ToString("0")).Append('%');
            if (ram && sys.RamTotalGb>0) text.Append("   RAM ").Append(sys.RamUsedGb.ToString("0.0")).Append('/').Append(sys.RamTotalGb.ToString("0.0")).Append("GB");
            if(sys.CpuTempC.HasValue) text.Append("   CPU ").Append(sys.CpuTempC.Value.ToString("0")).Append("C");
            if(sys.GpuLoad.HasValue || sys.GpuTempC.HasValue || sys.GpuPowerW.HasValue || sys.GpuClockMhz.HasValue)
            {
                text.Append("\nGPU");
                if(sys.GpuLoad.HasValue)text.Append(' ').Append(sys.GpuLoad.Value.ToString("0")).Append('%');
                if(sys.GpuTempC.HasValue)text.Append(' ').Append(sys.GpuTempC.Value.ToString("0")).Append('C');
                if(sys.GpuClockMhz.HasValue)text.Append(' ').Append(sys.GpuClockMhz.Value.ToString("0")).Append("MHz");
                if(sys.GpuPowerW.HasValue)text.Append(' ').Append(sys.GpuPowerW.Value.ToString("0")).Append('W');
            }
            if(sys.VramUsedGb.HasValue || sys.VrmTempC.HasValue)
            {
                text.Append("\n");
                if(sys.VramUsedGb.HasValue)text.Append("VRAM ").Append(sys.VramUsedGb.Value.ToString("0.0")).Append("GB");
                if(sys.VrmTempC.HasValue)text.Append("   VRM ").Append(sys.VrmTempC.Value.ToString("0")).Append('C');
            }
            return text.ToString();
        }

        // Match SDK layout: header's OSD entry size/offset/count at 20/24/28; frame counter at 32.
        // Owner at slot+256, extended text at slot+512. Slot #0 is reserved for Afterburner.
        public bool Update(string text)
        {
            return Write(text,false);
        }
        public void Clear()
        {
            Write("",true);
        }
        bool Write(string content,bool clear)
        {
            LastError="";
            try
            {
                using(MemoryMappedFile mmf=MemoryMappedFile.OpenExisting(Mapping,MemoryMappedFileRights.ReadWrite))
                using(MemoryMappedViewAccessor view=mmf.CreateViewAccessor(0,0,MemoryMappedFileAccess.ReadWrite))
                {
                    if(view.Capacity<40 || view.ReadUInt32(0)!=Signature || view.ReadUInt32(4)<0x00020007)
                    { LastError="RTSS shared memory is unavailable or too old.";return false; }
                    uint version=view.ReadUInt32(4);
                    long stride=view.ReadUInt32(20), offset=view.ReadUInt32(24);
                    long count=view.ReadUInt32(28);
                    if(stride<4608 || stride>1048576 || offset<40 || count<2 || count>256 ||
                        offset>view.Capacity || count>(view.Capacity-offset)/stride)
                    {LastError="Invalid RTSS OSD slot layout.";return false;}
                    const int ownerOffset=256, extendedOffset=512;
                    // Get an exclusive RTSS renderer lock where supported (2.14+).
                    // Unsafe pointer is scoped to the acquired view and never leaves this method.
                    using(new RtssViewWriteLock(view,version>=0x0002000e))
                    {
                        long slot=-1,free=-1;
                        for(long i=1;i<count;i++)
                        {
                            long pos=offset+i*stride;
                            byte[] buf=new byte[256];view.ReadArray(pos+ownerOffset,buf,0,buf.Length);
                            int nul=Array.IndexOf(buf,(byte)0);if(nul<0)nul=buf.Length;
                            string id=Encoding.ASCII.GetString(buf,0,nul);
                            if(id==Owner) {slot=pos;break;}
                            if(id.Length==0 && free<0)free=pos;
                        }
                        if(slot<0)slot=clear?-1:free;
                        if(slot<0) {if(clear)return true;LastError="No free RTSS OSD slot.";return false;}
                        if(clear)
                        {
                            view.WriteArray(slot,new byte[256],0,256);
                            view.WriteArray(slot+extendedOffset,new byte[4096],0,4096);
                            view.WriteArray(slot+ownerOffset,new byte[256],0,256);
                        }
                        else
                        {
                            // Use ASCII; untrusted text never writes outside the owned slot.
                            byte[] owner=new byte[256];Encoding.ASCII.GetBytes(Owner,0,Owner.Length,owner,0);
                            byte[] payload=new byte[4096];
                            string safe=content??"";
                            byte[] encoded=Encoding.ASCII.GetBytes(safe);
                            Buffer.BlockCopy(encoded,0,payload,0,Math.Min(encoded.Length,4095));
                            view.WriteArray(slot+ownerOffset,owner,0,owner.Length);
                            view.WriteArray(slot+extendedOffset,payload,0,payload.Length);
                            view.WriteArray(slot,new byte[256],0,256);
                        }
                        view.Write(32,unchecked(view.ReadUInt32(32)+1));
                        return true;
                    }
                }
            }
            catch(Exception e)
            {
                LastError=e.GetType().Name+": "+e.Message;
                return false;
            }
        }

        // Wrap the RTSS busy flag in an atomic compare-exchange and ALWAYS release.
        private sealed unsafe class RtssViewWriteLock : IDisposable
        {
            private MemoryMappedViewAccessor view;
            private byte* pointer;
            private bool acquired,locked;
            public RtssViewWriteLock(MemoryMappedViewAccessor v,bool supported)
            {
                view=v;
                if(!supported)return;
                v.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
                acquired=true;
                int* busy=(int*)(pointer+36);
                // RTSS renderer sets bit 0. Do not disturb any other flags.
                for(int attempt=0;attempt<5;attempt++)
                {
                    if(Interlocked.CompareExchange(ref *busy,1,0)==0){locked=true;return;}
                    Thread.Sleep(1);
                }
                v.SafeMemoryMappedViewHandle.ReleasePointer();
                acquired=false;
                throw new IOException("RTSS OSD is busy; try again.");
            }
            public void Dispose()
            {
                if(acquired)
                {
                    if(locked)Interlocked.Exchange(ref *((int*)(pointer+36)),0);
                    view.SafeMemoryMappedViewHandle.ReleasePointer();
                    acquired=false;
                }
            }
        }
    }

    public struct SystemSample
    {
        public double? CpuPercent;
        public double RamUsedGb, RamTotalGb;
        public double? CpuTempC, CpuClockGhz, GpuLoad, GpuTempC, GpuClockMhz, GpuPowerW, VramUsedGb, VramTotalGb, VrmTempC;
    }
    // Native Windows counters are collected without third-party services or fake GPU temperatures.
    public sealed class SystemSampler
    {
        ulong previousIdle,previousKernel,previousUser;
        [DllImport("kernel32.dll",SetLastError=true)]
        static extern bool GetSystemTimes(out FileTime idle,out FileTime kernel,out FileTime user);
        [DllImport("kernel32.dll",CharSet=CharSet.Auto,SetLastError=true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
        [StructLayout(LayoutKind.Sequential)] struct FileTime {public uint low,high; public ulong Value {get{return ((ulong)high<<32)|low;}}}
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Auto)]
        struct MemoryStatus
        {
            public uint length,load;
            public ulong totalPhysical,availablePhysical,totalPage,availablePage,totalVirtual,availableVirtual,availableExtended;
        }
        public SystemSample Read()
        {
            SystemSample s=new SystemSample();
            try
            {
                FileTime idle,kernel,user;
                if(GetSystemTimes(out idle,out kernel,out user))
                {
                    ulong i=idle.Value,k=kernel.Value,u=user.Value;
                    ulong elapsed=(k-previousKernel)+(u-previousUser);
                    if(previousKernel!=0&&k>=previousKernel&&u>=previousUser&&i>=previousIdle&&elapsed>0)
                        s.CpuPercent=Math.Max(0,Math.Min(100,100.0*(elapsed-(i-previousIdle))/(double)elapsed));
                    previousIdle=i;previousKernel=k;previousUser=u;
                }
            }catch{}
            try
            {
                MemoryStatus m=new MemoryStatus();m.length=(uint)Marshal.SizeOf(typeof(MemoryStatus));
                if(GlobalMemoryStatusEx(ref m)&&m.totalPhysical>0)
                {
                    s.RamUsedGb=(m.totalPhysical-m.availablePhysical)/1073741824.0;
                    s.RamTotalGb=m.totalPhysical/1073741824.0;
                }
            }catch{}
            return s;
        }
        public static bool SelfTest()
        {
            return RtssOsdBridge.FrameBar(0).Length==22 &&
                   RtssOsdBridge.FrameBar(50).Contains("####################") &&
                   RtssOsdBridge.Format(new FrameSample(),new SystemSample(),"Global",true,true).Contains("FPS --");
        }
    }
}