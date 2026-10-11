// PacePilot 2.6 - Hardware inventory; built into the application.
// WMI is executed only once on a background thread, never on the UI timer.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Text;

namespace PacePilot
{
    public sealed class HardwareInventory
    {
        public string CpuName { get; set; }
        public string MotherboardName { get; set; }
        public string GpuName { get; set; }
        public string RamSummary { get; set; }
        public string RamModules { get; set; }
        public HardwareInventory()
        {
            CpuName="Unknown CPU"; MotherboardName="Unknown motherboard";
            GpuName="Unknown GPU"; RamSummary="Unknown RAM";RamModules="";
        }
    }

    public static class HardwareInventoryService
    {
        static string Clean(object raw)
        {
            string value=Convert.ToString(raw,CultureInfo.InvariantCulture);
            return String.IsNullOrWhiteSpace(value)?"Unknown":value.Trim();
        }
        static string Safe(ManagementBaseObject row,string key)
        {
            try{return Clean(row[key]);}catch{return "Unknown";}
        }
        static List<string> Search(string wql,Func<ManagementObject,string> item)
        {
            List<string> values=new List<string>();
            try
            {
                using(ManagementObjectSearcher finder=new ManagementObjectSearcher(wql))
                using(ManagementObjectCollection rows=finder.Get())
                {
                    foreach(ManagementObject row in rows)
                    using(row)
                    {
                        try
                        {
                            string value=item(row);
                            if(!String.IsNullOrWhiteSpace(value))values.Add(value.Trim());
                        }
                        catch{} // a driver may expose an incomplete WMI entry
                    }
                }
            }
            catch(ManagementException) {}
            catch(UnauthorizedAccessException) {}
            catch(System.Runtime.InteropServices.COMException) {}
            catch(InvalidOperationException) {}
            return values;
        }
        public static HardwareInventory Read()
        {
            HardwareInventory info=new HardwareInventory();
            info.CpuName=Search("SELECT Name FROM Win32_Processor",
                row=>Safe(row,"Name")).FirstOrDefault()??info.CpuName;
            info.MotherboardName=Search("SELECT Manufacturer,Product FROM Win32_BaseBoard",
                row=>Safe(row,"Manufacturer")+" "+Safe(row,"Product")).FirstOrDefault()??info.MotherboardName;
            string[] cards=Search("SELECT Name FROM Win32_VideoController",
                row=>Safe(row,"Name")).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if(cards.Length>0)
            {
                // Prefer a discrete AMD/NVIDIA adapter over integrated graphics.
                info.GpuName=cards.OrderByDescending(x=>
                    x.IndexOf("Radeon RX",StringComparison.OrdinalIgnoreCase)>=0||
                    x.IndexOf("NVIDIA",StringComparison.OrdinalIgnoreCase)>=0||
                    x.IndexOf("GeForce",StringComparison.OrdinalIgnoreCase)>=0?2:
                    x.IndexOf("Radeon",StringComparison.OrdinalIgnoreCase)>=0?1:0).First();
            }
            List<string> modules=Search(
                "SELECT Manufacturer,PartNumber,Capacity,ConfiguredClockSpeed,DeviceLocator FROM Win32_PhysicalMemory",
                row=>
                {
                    double gb=0;
                    try{gb=Convert.ToDouble(row["Capacity"],CultureInfo.InvariantCulture)/1073741824.0;}catch{}
                    return Safe(row,"Manufacturer")+" "+Safe(row,"PartNumber")+
                        " | "+(gb>0?gb.ToString("0.#",CultureInfo.InvariantCulture)+" GB":"unknown capacity")+
                        " | "+Safe(row,"ConfiguredClockSpeed")+" MHz"+
                        " | "+Safe(row,"DeviceLocator");
                });
            if(modules.Count>0)
            {
                info.RamModules=String.Join("; ",modules.ToArray());
                double total=0;
                foreach(string m in modules)
                {
                    System.Text.RegularExpressions.Match match=
                        System.Text.RegularExpressions.Regex.Match(m,@"\|\s*(\d+(?:\.\d+)?)\s*GB");
                    double n;
                    if(match.Success && double.TryParse(match.Groups[1].Value,NumberStyles.Float,
                        CultureInfo.InvariantCulture,out n)) total+=n;
                }
                info.RamSummary=modules.Count+" DIMM"+(modules.Count>1?"s":"")+
                    (total>0?" / "+total.ToString("0.#",CultureInfo.InvariantCulture)+" GB":"")+
                    " | "+modules[0].Split('|')[0].Trim();
            }
            return info;
        }
        public static bool SelfTest()
        {
            return Clean("  test  ")=="test"&&Clean(null)=="Unknown";
        }
    }
}
