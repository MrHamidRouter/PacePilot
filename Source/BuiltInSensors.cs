// PacePilot 2.5 - Built-in, self-contained hardware telemetry.
// Uses LibreHardwareMonitorLib packaged with PacePilot (no LibreHardwareMonitor app, HWiNFO, or Afterburner process).
// Dedicated background worker. Never modifies hardware, clock limits, voltages or fan controls.
// Third-party sensor support is inherently hardware/driver dependent.
// Developer: Mr.HamidRouter
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PacePilot
{
    internal sealed class NativeSensorValues
    {
        public double? CpuTemp,CpuClock,GpuLoad,GpuTemp,GpuClock,GpuPower,VramUsed,VramTotal,VrmTemp;
        public int Count;
        public string Status="Sensor engine not yet initialized";
        public NativeSensorValues Copy(){return (NativeSensorValues)MemberwiseClone();}
    }

    public static class BuiltInSensors
    {
        static readonly object Sync=new object();
        static NativeSensorValues cached=new NativeSensorValues();
        static bool pending,initializing;
        static DateTime nextPoll=DateTime.MinValue;
        static object monitor;
        static Assembly library;
        static string failure;
        static DateTime nextOpen=DateTime.MinValue;
        public static string Status{get{lock(Sync)return cached.Status;}}
        public static int ReadingCount{get{lock(Sync)return cached.Count;}}
        public static string LibraryPath{get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"LibreHardwareMonitorLib.dll");}}
        static object Property(object instance,string name)
        {
            return instance.GetType().GetProperty(name,BindingFlags.Public|BindingFlags.Instance).GetValue(instance,null);
        }
        static void Set(object instance,string name,bool value)
        {
            PropertyInfo p=instance.GetType().GetProperty(name,BindingFlags.Public|BindingFlags.Instance);
            if(p!=null&&p.CanWrite)p.SetValue(instance,value,null);
        }
        static void Invoke(object instance,string method)
        {
            MethodInfo m=instance.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.Instance, null,Type.EmptyTypes,null);
            if(m!=null)m.Invoke(instance,null);
        }
        static bool Has(string str,params string[] names)
        {
            if(String.IsNullOrEmpty(str))return false;
            foreach(string n in names)if(str.IndexOf(n,StringComparison.OrdinalIgnoreCase)>=0)return true;
            return false;
        }
        static double? Take(double? original,double? next,double min,double max)
        {
            if(original.HasValue||!next.HasValue||double.IsInfinity(next.Value)||double.IsNaN(next.Value)||
                next.Value<min||next.Value>max)return original;
            return next;
        }
        static double? Number(object raw)
        {
            if(raw==null)return null;
            try{return Convert.ToDouble(raw,CultureInfo.InvariantCulture);}catch{return null;}
        }
        static bool SensorEquals(string a,params string[] names)
        {
            foreach(string n in names)if(String.Equals(a,n,StringComparison.OrdinalIgnoreCase))return true;
            return false;
        }
        static double? ConvertVramGb(double? value,string type)
        {
            if(!value.HasValue)return null;
            // LHM SensorType.SmallData = MB; SensorType.Data = GB.
            if(type=="SmallData")return value.Value/1024.0;
            if(type=="Data")return value;
            return null;
        }
        static void Record(NativeSensorValues target,string device,string name,string type,double? val)
        {
            if(!val.HasValue||double.IsNaN(val.Value)||double.IsInfinity(val.Value))return;
            bool gpu=Has(device,"GpuAmd","GpuNvidia","GpuIntel");
            bool cpu=Has(device,"Cpu");
            bool motherboard=Has(device,"Motherboard","SuperIO","EmbeddedController","Lpc");
            double v=val.Value;
            if(gpu)
            {
                if(type=="Load" && SensorEquals(name,"GPU Core","GPU 3D","GPU Total","GPU Core Load"))
                    target.GpuLoad=Take(target.GpuLoad,v,0,100);
                if(type=="Temperature" && SensorEquals(name,"GPU Core","GPU Temperature","GPU Edge","GPU Hot Spot"))
                    target.GpuTemp=Take(target.GpuTemp,v,0,150);
                if(type=="Clock" && SensorEquals(name,"GPU Core","GPU Core Clock"))
                    target.GpuClock=Take(target.GpuClock,v,1,10000);
                if(type=="Power" && (Has(name,"GPU Power","GPU Package","GPU Board","GPU ASIC","GPU Core","GPU PPT")))
                    target.GpuPower=Take(target.GpuPower,v,0,1000);
                double? gb=ConvertVramGb(val,type);
                if(gb.HasValue)
                {
                    if(SensorEquals(name,"GPU Memory Used","GPU Dedicated Memory Used","GPU Memory Allocated"))
                        target.VramUsed=Take(target.VramUsed,gb,0,256);
                    else if(SensorEquals(name,"GPU Memory Total","GPU Dedicated Memory Total"))
                        target.VramTotal=Take(target.VramTotal,gb,.1,256);
                }
            }
            if(cpu)
            {
                if(type=="Temperature" && Has(name,"CPU Package","Core Average","CPU Core","CPU Tctl","Core Max"))
                    target.CpuTemp=Take(target.CpuTemp,v,0,150);
                if(type=="Clock" && Has(name,"CPU Core #1","Core #1","Core 0","Core (Effective)"))
                    target.CpuClock=Take(target.CpuClock,v/1000.0,.1,9);
            }
            if(type=="Temperature" && (motherboard||gpu) && Has(name,"VRM","VR MOS","MOSFET","Vcore MOS","VR VDDC"))
                target.VrmTemp=Take(target.VrmTemp,v,0,150);
            target.Count++;
        }
        static IEnumerable<object> Enumerate(object instance,string prop)
        {
            object result=Property(instance,prop);
            IEnumerable values=result as IEnumerable;
            if(values==null)yield break;
            foreach(object o in values)if(o!=null)yield return o;
        }
        static void Visit(object hardware,NativeSensorValues target,int depth)
        {
            if(hardware==null||depth>6)return;
            // Calling Update on each hardware updates all of its sensors.
            Invoke(hardware,"Update");
            string device=Convert.ToString(Property(hardware,"HardwareType"))??"";
            // Some motherboard temperature sensors live in SuperIO child hardware.
            foreach(object item in Enumerate(hardware,"Sensors"))
            {
                string type=Convert.ToString(Property(item,"SensorType"))??"";
                string name=Convert.ToString(Property(item,"Name"))??"";
                double? value=Number(Property(item,"Value"));
                Record(target,device,name,type,value);
            }
            foreach(object child in Enumerate(hardware,"SubHardware"))Visit(child,target,depth+1);
        }
        static void OpenIfNeeded()
        {
            if(monitor!=null)return;
            if(DateTime.UtcNow<nextOpen)throw new InvalidOperationException(failure??"Hardware sensor retry is pending.");
            if(!File.Exists(LibraryPath))throw new FileNotFoundException("Bundled hardware sensor module is missing",LibraryPath);
            library=Assembly.LoadFrom(LibraryPath);
            Type computer=library.GetType("LibreHardwareMonitor.Hardware.Computer",true,false);
            monitor=Activator.CreateInstance(computer);
            Set(monitor,"IsCpuEnabled",true);
            Set(monitor,"IsGpuEnabled",true);
            Set(monitor,"IsMotherboardEnabled",true);
            Set(monitor,"IsMemoryEnabled",true);
            // Only enable devices needed for the gaming dashboard: avoids probing unrelated storage controllers.
            try { Invoke(monitor,"Open"); }
            catch
            {
                try{Invoke(monitor,"Close");}catch{}
                monitor=null;
                throw;
            }
        }
        static void RunPoll()
        {
            NativeSensorValues fresh=new NativeSensorValues();
            try
            {
                OpenIfNeeded();
                List<object> devices=Enumerate(monitor,"Hardware").ToList();
                // Enumerate AMD and NVIDIA discrete GPUs before any integrated Intel graphics.
                foreach(object device in devices.OrderBy(d=>Priority(Convert.ToString(Property(d,"HardwareType")))))
                {
                    try{Visit(device,fresh,0);}
                    catch(Exception ex)
                    {
                        // One inaccessible device must not prevent reporting the others.
                        if(fresh.Count==0)failure=ex.GetType().Name;
                    }
                }
                fresh.Status=fresh.Count>0?
                    "Built-in LibreHardwareMonitor: "+fresh.Count+" sensors detected":
                    "Built-in sensor engine active; the driver/hardware exposed no readable sensors";
                failure=null;
            }
            catch(Exception ex)
            {
                Exception original=ex is TargetInvocationException&&ex.InnerException!=null?ex.InnerException:ex;
                failure=original.GetType().Name+": "+original.Message;
                fresh.Status="Internal sensor module unavailable: "+failure;
                nextOpen=DateTime.UtcNow.AddSeconds(25);
            }
            finally
            {
                lock(Sync)
                {
                    cached=fresh;
                    pending=false;
                    nextPoll=DateTime.UtcNow.AddSeconds(fresh.Count>0?3:8);
                }
            }
        }
        static int Priority(string kind)
        {
            if(Has(kind,"GpuAmd","GpuNvidia"))return 0;
            if(Has(kind,"GpuIntel"))return 1;
            return 2;
        }
        public static void Enrich(ref SystemSample sys)
        {
            NativeSensorValues sample;
            lock(Sync)
            {
                sample=cached.Copy();
                if(!pending && DateTime.UtcNow>=nextPoll)
                {
                    pending=true;
                    nextPoll=DateTime.UtcNow.AddSeconds(8);
                    ThreadPool.QueueUserWorkItem(delegate(object state){RunPoll();});
                }
            }
            sys.CpuTempC=Take(sys.CpuTempC,sample.CpuTemp,0,150);
            sys.CpuClockGhz=Take(sys.CpuClockGhz,sample.CpuClock,.1,9);
            sys.GpuLoad=Take(sys.GpuLoad,sample.GpuLoad,0,100);
            sys.GpuTempC=Take(sys.GpuTempC,sample.GpuTemp,0,150);
            sys.GpuClockMhz=Take(sys.GpuClockMhz,sample.GpuClock,1,10000);
            sys.GpuPowerW=Take(sys.GpuPowerW,sample.GpuPower,0,1000);
            sys.VramUsedGb=Take(sys.VramUsedGb,sample.VramUsed,0,256);
            sys.VramTotalGb=Take(sys.VramTotalGb,sample.VramTotal,.1,256);
            sys.VrmTempC=Take(sys.VrmTempC,sample.VrmTemp,0,150);
        }
        public static bool SelfTest()
        {
            NativeSensorValues s=new NativeSensorValues();
            Record(s,"GpuAmd","GPU Core","Temperature",72);
            Record(s,"GpuAmd","GPU Core","Load",91);
            Record(s,"GpuAmd","GPU Memory Used","SmallData",4096);
            Record(s,"GpuAmd","GPU Power","Power",115);
            Record(s,"SuperIO","VRM MOS","Temperature",69);
            Record(s,"Cpu","CPU Package","Temperature",64);
            return s.GpuLoad==91 && s.GpuTemp==72 && s.VramUsed==4 && s.GpuPower==115 &&
                s.VrmTemp==69 && s.CpuTemp==64 &&
                Priority("GpuAmd")<Priority("GpuIntel") &&
                File.Exists(LibraryPath);
        }
    }
}
