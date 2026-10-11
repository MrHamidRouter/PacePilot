// PacePilot 2.4.1 - Hardware sensors independent of RTSS.
// Optional HWiNFO bridge is primary. This fallback uses Open/LibreHardwareMonitor WMI,
// and Windows GPU performance counters when they exist. All unavailable values remain null.
// Polling runs on a worker thread so a missing WMI namespace never freezes the dashboard.
// Developer: Mr.HamidRouter
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Threading;

namespace PacePilot
{
    internal sealed class HardwareSensorSnapshot
    {
        public double? CpuTempC,CpuClockGhz,GpuLoad,GpuTempC,GpuClockMhz,GpuPowerW,VramUsedGb,VramTotalGb,GpuVrmTempC,VrmTempC;
        public string Source="No secondary sensor provider detected";
        public int ObservedSensors;
        public HardwareSensorSnapshot Copy()
        {
            return (HardwareSensorSnapshot)MemberwiseClone();
        }
    }
    public static class HardwareFallback
    {
        static readonly object Sync=new object();
        static HardwareSensorSnapshot snapshot=new HardwareSensorSnapshot();
        static bool updating;
        static DateTime nextUpdate=DateTime.MinValue;
        public static string Status {get{lock(Sync)return snapshot.Source;}}
        public static int ObservedSensors {get{lock(Sync)return snapshot.ObservedSensors;}}
        static bool HasValue(double? v,double min,double max)
        {return v.HasValue && v.Value>=min && v.Value<=max && !double.IsNaN(v.Value) && !double.IsInfinity(v.Value);}
        static bool Has(string a,params string[] terms)
        {return terms.Any(x=>(a??"").IndexOf(x,StringComparison.OrdinalIgnoreCase)>=0);}
        static double? Choose(double? current,double value,double min,double max)
        {return current.HasValue||!HasValue(value,min,max)?current:(double?)value;}
        static double? ChooseNullable(double? current,double? value,double min,double max)
        {return current.HasValue||!HasValue(value,min,max)?current:value;}

        public static void Enrich(ref SystemSample system)
        {
            HardwareSensorSnapshot cache;
            lock(Sync)
            {
                cache=snapshot.Copy();
                if(!updating && DateTime.UtcNow>=nextUpdate)
                {
                    updating=true;
                    nextUpdate=DateTime.UtcNow.AddSeconds(5);
                    ThreadPool.QueueUserWorkItem(delegate(object state){Refresh();});
                }
            }
            system.CpuTempC=ChooseNullable(system.CpuTempC,cache.CpuTempC,0,150);
            system.CpuClockGhz=ChooseNullable(system.CpuClockGhz,cache.CpuClockGhz,.1,9);
            system.GpuLoad=ChooseNullable(system.GpuLoad,cache.GpuLoad,0,100);
            system.GpuTempC=ChooseNullable(system.GpuTempC,cache.GpuTempC,0,150);
            system.GpuClockMhz=ChooseNullable(system.GpuClockMhz,cache.GpuClockMhz,1,10000);
            system.GpuPowerW=ChooseNullable(system.GpuPowerW,cache.GpuPowerW,0,1000);
            system.VramUsedGb=ChooseNullable(system.VramUsedGb,cache.VramUsedGb,0,256);
            system.VramTotalGb=ChooseNullable(system.VramTotalGb,cache.VramTotalGb,0,256);
            system.GpuVrmTempC=ChooseNullable(system.GpuVrmTempC,cache.GpuVrmTempC,0,150);
            system.VrmTempC=ChooseNullable(system.VrmTempC,cache.VrmTempC,0,150);
        }
        static void Refresh()
        {
            HardwareSensorSnapshot next=new HardwareSensorSnapshot();
            try
            {
                // Secondary Windows-native counters only. No WMI monitor namespaces.
                // The in-process sensor engine supplies all supported GPU/VRM temperatures.
                WindowsCounters(next);
                next.Source=next.GpuLoad.HasValue||next.VramUsedGb.HasValue?
                    "Built-in Windows GPU driver performance counters":
                    "Windows GPU counters are not exposed by this driver";
            }
            catch(Exception e)
            {
                next.Source="Hardware sensor discovery: "+e.GetType().Name;
            }
            lock(Sync){snapshot=next;updating=false;}
        }
        static int ReadWmi(string scope,HardwareSensorSnapshot next)
        {
            try
            {
                int total=0;
                using(ManagementObjectSearcher query=new ManagementObjectSearcher(
                    scope,"SELECT Name,SensorType,Value,Parent,Identifier FROM Sensor"))
                using(ManagementObjectCollection rows=query.Get())
                {
                    foreach(ManagementObject row in rows)
                    using(row)
                    {
                        string name=Convert.ToString(row["Name"])??"";
                        string kind=Convert.ToString(row["SensorType"])??"";
                        string parent=Convert.ToString(row["Parent"])??"";
                        string id=Convert.ToString(row["Identifier"])??"";
                        object raw=row["Value"];
                        if(raw==null)continue;
                        double value;
                        try{value=Convert.ToDouble(raw,CultureInfo.InvariantCulture);}catch{continue;}
                        if(double.IsNaN(value)||double.IsInfinity(value))continue;
                        bool gpu=Has(parent,"gpu-","gpu/","gpuintel","gpu-amd","gpu-nvidia","gpu-intel") ||
                            Has(id,"gpu-","gpu/","gpu-intel");
                        bool cpu=Has(parent,"cpu","intelcpu","amdcpu")||Has(id,"cpu");
                        bool temp=Has(kind,"Temperature");
                        bool load=Has(kind,"Load");
                        bool clock=Has(kind,"Clock");
                        bool power=Has(kind,"Power");
                        bool mb=Has(kind,"SmallData");
                        bool gb=Has(kind,"Data")&&!mb;
                        bool vrm=temp && Has(name,"VRM","VR MOS","MOSFET","VRM MOS","VR VDDC");
                        if(vrm && gpu)next.GpuVrmTempC=Choose(next.GpuVrmTempC,value,0,150);
                        else if(vrm && !gpu)next.VrmTempC=Choose(next.VrmTempC,value,0,150);
                        if(gpu)
                        {
                            if(temp && Has(name,"GPU Core","GPU Temperature","GPU Hot Spot","GPU Hotspot","GPU Edge"))
                                next.GpuTempC=Choose(next.GpuTempC,value,0,150);
                            if(load && Has(name,"GPU Core","GPU 3D","GPU Total","GPU Usage","GPU D3D"))
                                next.GpuLoad=Choose(next.GpuLoad,value,0,100);
                            if(clock && Has(name,"GPU Core","GPU Clock"))next.GpuClockMhz=Choose(next.GpuClockMhz,value,1,10000);
                            if(power && Has(name,"GPU Package","GPU Core","GPU Power","GPU Board","GPU ASIC","GPU Chip"))
                                next.GpuPowerW=Choose(next.GpuPowerW,value,0,1000);
                            if((mb||gb) && Has(name,"GPU Memory Used","GPU Dedicated Memory Used","GPU Memory Allocated","GPU VRAM Used"))
                                next.VramUsedGb=Choose(next.VramUsedGb,gb?value:value/1024.0,0,256);
                            if((mb||gb) && Has(name,"GPU Memory Total","GPU Dedicated Memory Total","GPU VRAM Total"))
                                next.VramTotalGb=Choose(next.VramTotalGb,gb?value:value/1024.0,0,256);
                        }
                        if(cpu)
                        {
                            if(temp && Has(name,"CPU Package","CPU Core","CPU Tctl","CPU Die"))
                                next.CpuTempC=Choose(next.CpuTempC,value,0,150);
                            if(clock && Has(name,"Core 0","CPU Core","CPU Clock"))
                                next.CpuClockGhz=Choose(next.CpuClockGhz,value/1000.0,.1,9);
                        }
                        total++;
                    }
                }
                if(total>0)next.Source=scope;
                return total;
            }
            catch(ManagementException){return 0;}
            catch(UnauthorizedAccessException){return 0;}
            catch(System.Runtime.InteropServices.COMException){return 0;}
            catch(InvalidOperationException){return 0;}
        }
        static void WindowsCounters(HardwareSensorSnapshot next)
        {
            // These OS counters do not provide hardware temperature or watts.
            // GPU Engine is vendor-agnostic, but some drivers do not expose it.
            if(!next.GpuLoad.HasValue)
            {
                try
                {
                    if(PerformanceCounterCategory.Exists("GPU Engine"))
                    {
                        PerformanceCounterCategory category=new PerformanceCounterCategory("GPU Engine");
                        string[] names=category.GetInstanceNames();
                        var indices=names.Where(n=>Has(n,"engtype_3D","engtype_Compute")).Take(12).ToArray();
                        if(indices.Length>0)
                        {
                            List<PerformanceCounter> counters=new List<PerformanceCounter>();
                            try
                            {
                                foreach(string item in indices)
                                {
                                    PerformanceCounter pc=new PerformanceCounter("GPU Engine","Utilization Percentage",item,true);
                                    pc.NextValue();counters.Add(pc);
                                }
                                Thread.Sleep(260);
                                double peak=0;
                                foreach(PerformanceCounter pc in counters)peak=Math.Max(peak,Math.Min(100,pc.NextValue()));
                                next.GpuLoad=Choose(next.GpuLoad,peak,0,100);
                            }
                            finally{foreach(PerformanceCounter pc in counters)pc.Dispose();}
                        }
                    }
                }catch{} // Unsupported GPU driver: keep the field null.
            }
            if(!next.VramUsedGb.HasValue || !next.VramTotalGb.HasValue)
            {
                try
                {
                    if(PerformanceCounterCategory.Exists("GPU Adapter Memory"))
                    {
                        PerformanceCounterCategory cat=new PerformanceCounterCategory("GPU Adapter Memory");
                        string[] names=cat.GetInstanceNames();
                        foreach(string instance in names.Take(4))
                        {
                            // Dedicated Usage and Dedicated Limit are Windows adapter counters.
                            // The latter is an OS budget; do not call it physical VRAM capacity.
                            if(!next.VramUsedGb.HasValue)
                            {
                                try
                                {
                                    using(PerformanceCounter used=new PerformanceCounter("GPU Adapter Memory","Dedicated Usage",instance,true))
                                        next.VramUsedGb=Choose(next.VramUsedGb,used.NextValue()/1073741824.0,0,256);
                                }
                                catch(InvalidOperationException){}
                            }
                            if(!next.VramTotalGb.HasValue)
                            {
                                try
                                {
                                    using(PerformanceCounter limit=new PerformanceCounter("GPU Adapter Memory","Dedicated Limit",instance,true))
                                        next.VramTotalGb=Choose(next.VramTotalGb,limit.NextValue()/1073741824.0,.1,256);
                                }
                                catch(InvalidOperationException){}
                            }
                        }
                    }
                }catch{}
            }
        }
        public static bool SelfTest()
        {
            HardwareSensorSnapshot s=new HardwareSensorSnapshot();
            s.GpuLoad=Choose(null,72,0,100);
            s.VrmTempC=Choose(null,65,0,150);
            return s.GpuLoad==72 && s.VrmTempC==65 &&
                !Choose(null,1000,0,100).HasValue &&
                Has("/gpu-amd/0","gpu-amd") && Has("VRM MOS","vrm");
        }
    }
}
