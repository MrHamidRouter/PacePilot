// Local, read-only Steam/Epic/GOG game discovery for PacePilot.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Win32;
namespace PacePilot
{
    public sealed class DetectedGame
    {
        public string Title, Exe, Platform;
        public DetectedGame(string title,string exe,string platform) { Title=title;Exe=exe;Platform=platform; }
        public override string ToString() { return Title+"  ["+Platform+"]"; }
    }
    public static class GameDiscovery
    {
        static string Key(string text,string name)
        {
            Match m=Regex.Match(text,"\""+Regex.Escape(name)+"\"\\s*:?\\s*\"([^\"]+)\"",RegexOptions.IgnoreCase);
            return m.Success?m.Groups[1].Value.Replace("\\\\","\\").Replace("\\/","/"):"";
        }
        static void Add(Dictionary<string,DetectedGame> output,string title,string exe,string platform)
        {
            try
            {
                if(String.IsNullOrWhiteSpace(exe)||!exe.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)||!File.Exists(exe))return;
                string path=Path.GetFullPath(exe);
                if(!output.ContainsKey(path))output[path]=new DetectedGame(
                   String.IsNullOrWhiteSpace(title)?Path.GetFileNameWithoutExtension(path):title,path,platform);
            }catch{}
        }
        static string LocateExe(string root,string hint)
        {
            if(!Directory.Exists(root))return "";
            Queue<Tuple<string,int>> q=new Queue<Tuple<string,int>>();q.Enqueue(Tuple.Create(root,0));
            string best="";int scoreBest=int.MinValue,visited=0;
            string normalized=Regex.Replace(hint??"","[^A-Za-z0-9]","").ToLowerInvariant();
            while(q.Count>0&&visited++<120)
            {
                Tuple<string,int> entry=q.Dequeue();string[] files;
                try{files=Directory.GetFiles(entry.Item1,"*.exe",SearchOption.TopDirectoryOnly);}catch{continue;}
                foreach(string file in files.Take(100))
                {
                    string name=Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                    if(Regex.IsMatch(name,"unins|uninstall|setup|crash|redist|easyanticheat|battleye|dxsetup|updater|reporter|helper"))continue;
                    string simple=Regex.Replace(name,"[^a-z0-9]","");
                    int score=100-entry.Item2*12;
                    if(normalized.Length>0&&simple==normalized)score+=110;
                    else if(normalized.Length>4&&(simple.Contains(normalized)||normalized.Contains(simple)))score+=55;
                    if(name.EndsWith("shipping"))score+=30;
                    if(name.Contains("launcher"))score-=30;
                    if(score>scoreBest){scoreBest=score;best=file;}
                }
                if(entry.Item2>=3)continue;
                string[] folders;try{folders=Directory.GetDirectories(entry.Item1);}catch{continue;}
                foreach(string sub in folders.Take(60))
                {
                    string n=Path.GetFileName(sub).ToLowerInvariant();
                    if(n.Contains("redistribut")||n.Contains("commonredist")||n=="directx"||n=="installer")continue;
                    q.Enqueue(Tuple.Create(sub,entry.Item2+1));
                }
            }
            return best;
        }
        static IEnumerable<string> SteamRoots()
        {
            var roots=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(RegistryHive hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine})
            foreach(RegistryView view in new[]{RegistryView.Registry32,RegistryView.Registry64})
            try
            {
                using(RegistryKey k=RegistryKey.OpenBaseKey(hive,view).OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    if(k==null)continue;
                    foreach(string name in new[]{"SteamPath","InstallPath"})
                    {
                        string path=Convert.ToString(k.GetValue(name));
                        if(Directory.Exists(path))roots.Add(path);
                    }
                }
            }catch{}
            foreach(string parent in new[]{Environment.GetEnvironmentVariable("ProgramFiles(x86)"),Environment.GetEnvironmentVariable("ProgramFiles")})
                if(!String.IsNullOrWhiteSpace(parent)&&Directory.Exists(Path.Combine(parent,"Steam")))roots.Add(Path.Combine(parent,"Steam"));
            return roots;
        }
        static void Steam(Dictionary<string,DetectedGame> output)
        {
            foreach(string steam in SteamRoots())
            {
                var libs=new HashSet<string>(StringComparer.OrdinalIgnoreCase){steam};
                string vdf=Path.Combine(steam,"steamapps","libraryfolders.vdf");
                if(File.Exists(vdf))try
                {
                    foreach(Match m in Regex.Matches(File.ReadAllText(vdf),"\"path\"\\s+\"([^\"]+)\"",RegexOptions.IgnoreCase))
                    {
                        string lib=m.Groups[1].Value.Replace("\\\\","\\");
                        if(Directory.Exists(lib))libs.Add(lib);
                    }
                }catch{}
                foreach(string lib in libs)
                {
                    string apps=Path.Combine(lib,"steamapps");if(!Directory.Exists(apps))continue;
                    string[] manifests;try{manifests=Directory.GetFiles(apps,"appmanifest_*.acf");}catch{continue;}
                    foreach(string manifest in manifests.Take(1200))try
                    {
                        string data=File.ReadAllText(manifest);
                        string name=Key(data,"name"),folder=Key(data,"installdir");
                        if(!String.IsNullOrEmpty(folder))Add(output,name,LocateExe(Path.Combine(apps,"common",folder),folder),"Steam");
                    }catch{}
                }
            }
        }
        static void Epic(Dictionary<string,DetectedGame> output)
        {
            string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"Epic","EpicGamesLauncher","Data","Manifests");
            if(!Directory.Exists(dir))return;
            foreach(string manifest in Directory.GetFiles(dir,"*.item").Take(1200))try
            {
                string data=File.ReadAllText(manifest),title=Key(data,"DisplayName"),root=Key(data,"InstallLocation"),launch=Key(data,"LaunchExecutable");
                if(!Directory.Exists(root))continue;
                string exe=String.IsNullOrWhiteSpace(launch)?"":(Path.IsPathRooted(launch)?launch:Path.Combine(root,launch.Replace('/',Path.DirectorySeparatorChar)));
                if(!File.Exists(exe))exe=LocateExe(root,title);
                Add(output,title,exe,"Epic Games");
            }catch{}
        }
        static void Gog(Dictionary<string,DetectedGame> output)
        {
            foreach(RegistryHive hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine})
            foreach(RegistryView view in new[]{RegistryView.Registry32,RegistryView.Registry64})
            try
            {
                using(RegistryKey root=RegistryKey.OpenBaseKey(hive,view))
                using(RegistryKey games=root.OpenSubKey(@"SOFTWARE\GOG.com\Games"))
                {
                    if(games==null)continue;
                    foreach(string id in games.GetSubKeyNames().Take(1000))try
                    {
                        using(RegistryKey g=games.OpenSubKey(id))
                        {
                            if(g==null)continue;
                            string path=Convert.ToString(g.GetValue("path"));
                            Add(output,Convert.ToString(g.GetValue("gameName")),LocateExe(path,Convert.ToString(g.GetValue("gameName"))),"GOG");
                        }
                    }catch{}
                }
            }catch{}
        }
        public static List<DetectedGame> Scan()
        {
            var result=new Dictionary<string,DetectedGame>(StringComparer.OrdinalIgnoreCase);
            try{Steam(result);}catch{}
            try{Epic(result);}catch{}
            try{Gog(result);}catch{}
            return result.Values.OrderBy(x=>x.Title,StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        public static bool SelfTest()
        {
            return Key("\"name\" \"Example\"","name")=="Example"&&
                   Key("{\"DisplayName\":\"Example\"}","DisplayName")=="Example";
        }
    }
}