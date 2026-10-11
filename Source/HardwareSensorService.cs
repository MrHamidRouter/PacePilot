// PacePilot 2.6: hardware sensor catalog and persisted, explicit VRM mapping.
// Uses the ONE existing LibreHardwareMonitor background poller (BuiltInSensors).
// No second sensor engine, no game injection, no fabricated temperatures.
using System;
using System.Collections.Generic;
using System.Linq;

namespace PacePilot
{
    public sealed class HardwareSensorRecord
    {
        public string Id {get;set;}
        public string Name {get;set;}
        public string HardwarePath {get;set;}
        public string RootType {get;set;}
        public double? CurrentC {get;set;}
        public HardwareSensorRecord()
        {Id="";Name="";HardwarePath="";RootType="";}

        public override string ToString()
        {
            return Name+"  ["+(CurrentC.HasValue?CurrentC.Value.ToString("0.0")+" °C":"N/A")+"]  "+
                HardwarePath+"  "+Id;
        }
    }

    public static class HardwareSensorService
    {
        // The snapshot was already updated asynchronously by BuiltInSensors.Enrich.
        public static List<HardwareSensorRecord> ReadLatest()
        {return BuiltInSensors.GetTemperatureSensors();}

        public static bool IsGpu(HardwareSensorRecord sensor)
        {return sensor!=null&&(sensor.RootType=="GpuAmd"||sensor.RootType=="GpuNvidia"||sensor.RootType=="GpuIntel");}
        public static bool IsMotherboard(HardwareSensorRecord sensor)
        {return sensor!=null&&sensor.RootType=="Motherboard";}

        public static double? GetMappedTemperature(string sensorId,bool gpu)
        {
            if(String.IsNullOrWhiteSpace(sensorId))return null;
            HardwareSensorRecord matched=ReadLatest().FirstOrDefault(sensor=>
                String.Equals(sensor.Id,sensorId,StringComparison.OrdinalIgnoreCase) &&
                (gpu?IsGpu(sensor):IsMotherboard(sensor)));
            if(matched==null||!matched.CurrentC.HasValue)return null;
            double t=matched.CurrentC.Value;
            if(Double.IsNaN(t)||Double.IsInfinity(t)||t<0||t>150)return null;
            return t;
        }
        public static void ApplyMappings(ref SystemSample value,AppSettings settings)
        {
            if(settings==null)return;
            // An explicit manual mapping overrides ALL automatic sources,
            // including null: a disconnected sensor must never show a stale reading.
            if(!String.IsNullOrWhiteSpace(settings.GpuVrmSensorId))
                value.GpuVrmTempC=GetMappedTemperature(settings.GpuVrmSensorId,true);
            if(!String.IsNullOrWhiteSpace(settings.BoardVrmSensorId))
                value.VrmTempC=GetMappedTemperature(settings.BoardVrmSensorId,false);
        }

        public static bool SelfTest()
        {
            HardwareSensorRecord a=new HardwareSensorRecord{
                Id="/gpu-amd/0/temperature/0",Name="GPU VRM",RootType="GpuAmd",CurrentC=77};
            HardwareSensorRecord b=new HardwareSensorRecord{
                Id="/lpc/nct/temperature/3",Name="Temperature #3",RootType="Motherboard",CurrentC=null};
            return IsGpu(a)&&!IsMotherboard(a)&&IsMotherboard(b)&&!IsGpu(b);
        }
    }
}
