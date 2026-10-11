// PacePilot - read-only MSI Afterburner hardware monitor fallback.
// MAHMSharedMemory is optional. The shared memory header determines all entry sizes.
// Never writes into MSI Afterburner, RTSS, hardware or any protected game.
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace PacePilot
{
    public static class AfterburnerBridge
    {
        const uint Signature=0x4D41484D; // 'MAHM'
        public static string Status {get;private set;}
        public static int SensorCount {get;private set;}
        static AfterburnerBridge(){Status="MSI Afterburner sensor sharing is not active";}
        static string ReadAnsi(MemoryMappedViewAccessor view,long offset,int bytes)
        {
            byte[] buf=new byte[bytes];
            view.ReadArray(offset,buf,0,buf.Length);
            int end=Array.IndexOf(buf,(byte)0);if(end<0)end=buf.Length;
            return Encoding.ASCII.GetString(buf,0,end).Trim();
        }
        static bool Contains(string source,params string[] aliases)
        {
            foreach(string alias in aliases)
                if((source??"").IndexOf(alias,StringComparison.OrdinalIgnoreCase)>=0)return true;
            return false;
        }
        static double? Put(double? existing,double incoming,double min,double max)
        {
            if(existing.HasValue||double.IsNaN(incoming)||double.IsInfinity(incoming)||incoming<min||incoming>max)return existing;
            return incoming;
        }
        static double? ToGb(double value,string unit)
        {
            if(Contains(unit,"MB","MBytes","MiB"))return value/1024.0;
            if(Contains(unit,"GB","GiB","GBytes"))return value;
            return null;
        }
        public static void Enrich(ref SystemSample system)
        {
            Status="MSI Afterburner sensor sharing is not active";
            SensorCount=0;
            try
            {
                using(MemoryMappedFile map=MemoryMappedFile.OpenExisting("MAHMSharedMemory",MemoryMappedFileRights.Read))
                using(MemoryMappedViewAccessor view=map.CreateViewAccessor(0,0,MemoryMappedFileAccess.Read))
                {
                    if(view.Capacity<32||view.ReadUInt32(0)!=Signature)
                    {Status="MSI Afterburner shared memory signature is unsupported";return;}
                    long header=view.ReadUInt32(8),count=view.ReadUInt32(12),stride=view.ReadUInt32(16);
                    long gpuCount=view.ReadUInt32(24),gpuStride=view.ReadUInt32(28);
                    if(header<32||header>65536||count<0||count>4096||stride<1324||stride>65536||
                       gpuCount<0||gpuCount>64||gpuStride<0||gpuStride>65536||
                       header>view.Capacity||count>(view.Capacity-header)/stride||
                       gpuCount>0 && (gpuStride<1304 || gpuCount>(view.Capacity-header-count*stride)/gpuStride))
                    {Status="Unsupported MSI Afterburner sensor memory layout";return;}
                    SensorCount=(int)count;
                    for(long gpu=0;gpu<gpuCount;gpu++)
                    {
                        long pos=header+count*stride+gpu*gpuStride;
                        double? total=ToGb(view.ReadUInt32(pos+1300),"MB");
                        if(total.HasValue)system.VramTotalGb=Put(system.VramTotalGb,total.Value,.1,256);
                    }
                    for(long i=0;i<count;i++)
                    {
                        long pos=header+i*stride;
                        string name=ReadAnsi(view,pos,260);
                        string unit=ReadAnsi(view,pos+260,260);
                        double val=view.ReadSingle(pos+1300);
                        if(double.IsNaN(val)||double.IsInfinity(val))continue;
                        uint gpuId=view.ReadUInt32(pos+1316);
                        // 0x80 and higher indicate non-GPU source groups, per MAHM source convention.
                        bool gpu=gpuCount>0 && gpuId<gpuCount;
                        if(gpu)
                        {
                            if(Contains(name,"GPU temperature","GPU core temperature","GPU hotspot"))
                                system.GpuTempC=Put(system.GpuTempC,val,0,150);
                            else if(Contains(name,"GPU usage","GPU load"))
                                system.GpuLoad=Put(system.GpuLoad,val,0,100);
                            else if(Contains(name,"Core clock","GPU core clock"))
                                system.GpuClockMhz=Put(system.GpuClockMhz,val,1,10000);
                            else if(Contains(name,"Memory usage","Dedicated memory","VRAM used","GPU memory"))
                            {
                                double? gb=ToGb(val,unit);
                                if(gb.HasValue)system.VramUsedGb=Put(system.VramUsedGb,gb.Value,0,256);
                            }
                            else if(Contains(name,"GPU power","Board power","GPU total power") && Contains(unit,"W"))
                                system.GpuPowerW=Put(system.GpuPowerW,val,0,1000);
                        }
                        if(Contains(name,"CPU temperature","CPU package"))
                            system.CpuTempC=Put(system.CpuTempC,val,0,150);
                        if(Contains(name,"VRM temperature","VR MOS","VRM MOS") && Contains(unit,"C"))
                            system.VrmTempC=Put(system.VrmTempC,val,0,150);
                    }
                    Status="MSI Afterburner shared sensor feed active ("+SensorCount+" entries)";
                }
            }
            catch(FileNotFoundException){Status="MSI Afterburner is not publishing shared sensors";}
            catch(Exception e){Status="MSI Afterburner sensor bridge: "+e.GetType().Name;}
        }
        public static bool SelfTest()
        {
            return ToGb(4096,"MB")==4 && ToGb(8,"GB")==8 &&
                   !ToGb(90,"%").HasValue && Put(null,72,0,100)==72 &&
                   !Put(null,900,0,100).HasValue;
        }
    }
}
