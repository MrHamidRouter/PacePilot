// Optional HWiNFO64 shared-memory sensor reader.
// No API hooking, no drivers, and no invented readings.
// Enable "Shared Memory Support" in HWiNFO64's Sensors settings.
// Based on published HWiNFO_SENS_SM2 revision 1 memory layout.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

namespace PacePilot
{
    public static class HwInfoBridge
    {
        const uint HwInfoSignature=0x53695748;  // 'HWiS'
        public static string Status { get; private set; }
        static HwInfoBridge(){Status="Not connected";}
        public static void Enrich(ref SystemSample system)
        {
            Status="HWiNFO sensor shared memory not found";
            try
            {
                using(MemoryMappedFile map=MemoryMappedFile.OpenExisting(@"Global\HWiNFO_SENS_SM2",MemoryMappedFileRights.Read))
                using(MemoryMappedViewAccessor view=map.CreateViewAccessor(0,0,MemoryMappedFileAccess.Read))
                {
                    if(view.Capacity<44 || view.ReadUInt32(0)!=HwInfoSignature)
                    {Status="Unsupported HWiNFO sensor signature";return;}
                    uint version=view.ReadUInt32(4);
                    long sensorOffset=view.ReadUInt32(20),sensorSize=view.ReadUInt32(24),sensorCount=view.ReadUInt32(28);
                    long readingOffset=view.ReadUInt32(32),readingSize=view.ReadUInt32(36),readingCount=view.ReadUInt32(40);
                    if(version!=1||sensorOffset<44||sensorSize<264||sensorSize>65536||
                        sensorCount>2048||readingOffset<44||readingSize<316||readingSize>65536||
                        readingCount>16384||sensorOffset>view.Capacity||readingOffset>view.Capacity||
                        sensorCount>(view.Capacity-sensorOffset)/sensorSize||
                        readingCount>(view.Capacity-readingOffset)/readingSize)
                    {Status="Unsupported HWiNFO sensor memory layout";return;}
                    var devices=new List<string>();
                    for(long i=0;i<sensorCount;i++)
                    {
                        long p=sensorOffset+i*sensorSize;
                        string name=ReadAnsi(view,p+136,128);
                        if(String.IsNullOrWhiteSpace(name))name=ReadAnsi(view,p+8,128);
                        devices.Add(name);
                    }
                    for(long j=0;j<readingCount;j++)
                    {
                        long p=readingOffset+j*readingSize;
                        uint kind=view.ReadUInt32(p);
                        long index=view.ReadUInt32(p+4);
                        if(index<0||index>=devices.Count)continue;
                        string group=devices[(int)index];
                        string name=ReadAnsi(view,p+140,128);
                        if(String.IsNullOrWhiteSpace(name))name=ReadAnsi(view,p+12,128);
                        string unit=ReadAnsi(view,p+268,16);
                        double value=view.ReadDouble(p+284);
                        if(Double.IsNaN(value)||Double.IsInfinity(value))continue;
                        bool gpu=Contains(group,"gpu","radeon","geforce","nvidia","intel graphics");
                        bool cpu=Contains(group,"cpu","ryzen","core i7","core i5","core i9","xeon");
                        bool temp=kind==1;
                        bool watts=kind==5;
                        bool clock=kind==6;
                        bool usage=kind==7;
                        if(gpu)
                        {
                            if(temp && Contains(name,"GPU Temperature","GPU Core","GPU Hot Spot") && InRange(value,0,140))
                                system.GpuTempC=Pick(system.GpuTempC,value);
                            if(usage && Contains(name,"GPU Core Load","GPU D3D Usage","GPU Usage","GPU Utilization") && InRange(value,0,100))
                                system.GpuLoad=Pick(system.GpuLoad,value);
                            if(clock && Contains(name,"GPU Clock","GPU Core Clock","GPU Core") && InRange(value,10,5000))
                                system.GpuClockMhz=Pick(system.GpuClockMhz,value);
                            if(watts && Contains(name,"GPU Power","GPU ASIC Power","GPU Board Power","GPU Chip Power","Total Graphics Power") && InRange(value,0,1000))
                                system.GpuPowerW=Pick(system.GpuPowerW,value);
                            if(Contains(name,"GPU Memory Allocated","Dedicated GPU Memory Used","GPU Memory Used","Video Memory Used","GPU D3D Memory Dedicated"))
                            {
                                double? mb=ToMegabytes(value,unit);
                                if(mb.HasValue && InRange(mb.Value,0,262144))
                                    system.VramUsedGb=Pick(system.VramUsedGb,mb.Value/1024.0);
                            }
                            if(Contains(name,"GPU Memory Total","GPU Memory Available"))
                            {
                                double? mb=ToMegabytes(value,unit);
                                if(mb.HasValue && InRange(mb.Value,0,262144))
                                    system.VramTotalGb=Pick(system.VramTotalGb,mb.Value/1024.0);
                            }
                        }
                        if(cpu)
                        {
                            if(temp && Contains(name,"CPU Package","CPU (Tctl/Tdie)","CPU CCD1","CPU Core Max") && InRange(value,0,130))
                                system.CpuTempC=Pick(system.CpuTempC,value);
                            if(clock && Contains(name,"Core Effective Clock","Core Clock") && InRange(value,100,9000))
                                system.CpuClockGhz=Pick(system.CpuClockGhz,value/1000.0);
                        }
                        if(temp && Contains(name,"VRM MOS","VR MOS","VRM Temperature","VRM Temp") && InRange(value,0,150))
                            system.VrmTempC=Pick(system.VrmTempC,value);
                    }
                    Status="HWiNFO sensors connected";
                }
            }
            catch(FileNotFoundException) { }
            catch(Exception e) {Status="Sensor feed unavailable: "+e.GetType().Name;}
        }
        static bool Contains(string value,params string[] words)
        {
            return words.Any(w=>value.IndexOf(w,StringComparison.OrdinalIgnoreCase)>=0);
        }
        static double? Pick(double? old,double incoming) {return old.HasValue?old:incoming;}
        static bool InRange(double value,double low,double high){return value>=low&&value<=high;}
        static double? ToMegabytes(double value,string unit)
        {
            string u=(unit??"").Trim();
            if(u.Equals("GB",StringComparison.OrdinalIgnoreCase)||
               u.Equals("GiB",StringComparison.OrdinalIgnoreCase)||
               u.Equals("GBytes",StringComparison.OrdinalIgnoreCase)) return value*1024.0;
            if(u.Equals("MB",StringComparison.OrdinalIgnoreCase)||
               u.Equals("MiB",StringComparison.OrdinalIgnoreCase)||
               u.Equals("MBytes",StringComparison.OrdinalIgnoreCase)) return value;
            return null;
        }
        static string ReadAnsi(MemoryMappedViewAccessor view,long pos,int max)
        {
            if(pos<0||pos+max>view.Capacity)return "";
            byte[] raw=new byte[max];view.ReadArray(pos,raw,0,max);
            int stop=Array.IndexOf(raw,(byte)0);if(stop<0)stop=raw.Length;
            return Encoding.Default.GetString(raw,0,stop).Trim();
        }
        public static bool SelfTest()
        {
            return ToMegabytes(4096,"MB")==4096 &&
                   ToMegabytes(8,"GB")==8192 &&
                   !ToMegabytes(600,"percent").HasValue;
        }
    }
}
