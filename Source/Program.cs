// PacePilot - A Windows framerate control front-end powered by installed RTSS.
// MIT-licensed original application source. Does not bundle RTSS or bypass anti-cheat.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Principal;
using System.Windows.Forms;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Reflection;

[assembly: AssemblyTitle("PacePilot")]
[assembly: AssemblyDescription("Windows Frame Pacing frontend for RTSS")]
[assembly: AssemblyCompany("Mr.HamidRouter")]
[assembly: AssemblyProduct("PacePilot")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Mr.HamidRouter")]
[assembly: AssemblyVersion("2.1.0.0")]
[assembly: AssemblyFileVersion("2.1.0.0")]

namespace PacePilot
{
    [Serializable]
    public class GameProfile
    {
        public string Exe { get; set; }
        public string FullPath { get; set; }
        public string DisplayName { get; set; }
        public string Platform { get; set; }
        public int Limit { get; set; }
        public GameProfile() { Exe = ""; FullPath = ""; DisplayName = ""; Platform = ""; Limit = 60; }
    }

    [Serializable]
    public class AppSettings
    {
        public string RtssFolder { get; set; }
        public string LanguageCode { get; set; }
        public string ThemeName { get; set; }
        public int GlobalLimit { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool StartWithWindows { get; set; }
        public bool AlwaysOnTop { get; set; }
        public bool AutoSelectKnownGames { get; set; }
        public string LastTarget { get; set; }
        public DateTime LastUpdateCheckUtc { get; set; }
        public List<GameProfile> Games { get; set; }
        public AppSettings()
        {
            RtssFolder = ""; ThemeName = "teal"; LanguageCode = "en"; GlobalLimit = 60; MinimizeToTray = true;
            LastTarget = "Global"; AutoSelectKnownGames = true; Games = new List<GameProfile>();
        }
    }

    public static class Store
    {
        public static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PacePilot");
        public static readonly string FileName = Path.Combine(Folder, "settings.xml");
        public static AppSettings Load()
        {
            try
            {
                using (FileStream f = File.OpenRead(FileName))
                    return (AppSettings)(new XmlSerializer(typeof(AppSettings))).Deserialize(f);
            }
            catch { return new AppSettings(); }
        }
        public static void Save(AppSettings s)
        {
            Directory.CreateDirectory(Folder);
            string temp = FileName + ".tmp";
            using (FileStream f = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                (new XmlSerializer(typeof(AppSettings))).Serialize(f, s);
            if (File.Exists(FileName)) File.Replace(temp, FileName, null);
            else File.Move(temp, FileName);
        }
    }

    public static class WinApi
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string file, IntPtr reserved, uint flags);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterHotKey(IntPtr window, int id, uint mod, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr window, int id);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern IntPtr GetProcAddress(IntPtr module, string name);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr module);
    }

    // RTSSHooks64 is provided by a separate RTSS installation. It is NEVER bundled.
    public static class RtssNative
    {
        [DllImport("RTSSHooks64.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetProfileProperty(string name, IntPtr value, uint size);
        [DllImport("RTSSHooks64.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetProfileProperty(string name, IntPtr value, uint size);
        [DllImport("RTSSHooks64.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void LoadProfile(string name);
        [DllImport("RTSSHooks64.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void SaveProfile(string name);
        [DllImport("RTSSHooks64.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void UpdateProfiles();
    }


    // RTSS stores the global profile as Profiles\Global (no extension),
    // and executable-specific profiles as Profiles\game.exe.cfg.
    // This fallback changes only the two framerate keys and preserves every
    // other byte of the file (including comments and non-ASCII encodings).
    public static class RtssProfileFile
    {
        private static readonly Encoding BytePreserving = Encoding.GetEncoding(28591);

        public static string SetFrameLimit(string content, int fps)
        {
            if (fps < 0 || fps > 1000) throw new ArgumentOutOfRangeException("fps");
            string newline = content.Contains("\r\n") ? "\r\n" : "\n";
            Match start = Regex.Match(content, @"(?im)^[ \t]*\[Framerate\][ \t]*\r?$");
            if (!start.Success)
                return content + (content.Length > 0 && !content.EndsWith("\n") ? newline : "")
                    + "[Framerate]" + newline + "Limit=" + fps + newline + "LimitDenominator=1" + newline;
            int sectionStart = start.Index + start.Length;
            Match next = Regex.Match(content.Substring(sectionStart), @"(?m)^[ \t]*\[[^\]\r\n]+\]");
            int sectionEnd = next.Success ? sectionStart + next.Index : content.Length;
            string block = content.Substring(sectionStart, sectionEnd - sectionStart);
            block = UpdateKey(block, "Limit", fps, newline);
            block = UpdateKey(block, "LimitDenominator", 1, newline);
            return content.Substring(0, sectionStart) + block + content.Substring(sectionEnd);
        }
        private static string UpdateKey(string block, string key, int value, string nl)
        {
            string pattern = @"(?im)^[ \t]*" + Regex.Escape(key) + @"[ \t]*=[^\r\n]*";
            if (Regex.IsMatch(block, pattern))
                return Regex.Replace(block, pattern, key + "=" + value, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (!block.EndsWith("\n") && block.Length > 0) block += nl;
            return block + key + "=" + value + nl;
        }
        public static int? ReadLimit(string path) { return ReadNumber(path, "Limit"); }
        public static int? ReadDenominator(string path) { return ReadNumber(path, "LimitDenominator"); }
        private static int? ReadNumber(string path, string key)
        {
            if (!File.Exists(path)) return null;
            string text = BytePreserving.GetString(File.ReadAllBytes(path));
            Match start = Regex.Match(text, @"(?im)^[ \t]*\[Framerate\][ \t]*\r?$");
            if (!start.Success) return null;
            int sectionStart = start.Index + start.Length;
            Match next = Regex.Match(text.Substring(sectionStart), @"(?m)^[ \t]*\[[^\]\r\n]+\]");
            string block = text.Substring(sectionStart, next.Success ? next.Index : text.Length - sectionStart);
            Match value = Regex.Match(block, @"(?im)^[ \t]*" + Regex.Escape(key) + @"[ \t]*=[ \t]*([0-9]+)");
            int limit;
            return value.Success && Int32.TryParse(value.Groups[1].Value, out limit) ? (int?)limit : null;
        }
        public static string WriteWithBackup(string file, int fps)
        {
            string root = Path.GetDirectoryName(file);
            if (!Directory.Exists(root)) throw new DirectoryNotFoundException("RTSS profile directory missing: " + root);
            if (!File.Exists(file))
            {
                // No existing settings to overwrite; create just the requested section.
                // Atomic move so a failed write does not leave a half-written profile.
                string fresh = Path.Combine(root, ".PacePilot_" + Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    File.WriteAllBytes(fresh, BytePreserving.GetBytes(SetFrameLimit("", fps)));
                    File.Move(fresh, file);
                }
                finally { try { if (File.Exists(fresh)) File.Delete(fresh); } catch { } }
                return "New profile created (no previous profile existed)";
            }
            string backups = Path.Combine(Store.Folder, "RTSS-Profile-Backups");
            Directory.CreateDirectory(backups);
            string tag = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0,8);
            string backup = Path.Combine(backups, Path.GetFileName(file) + "_" + tag + ".bak");
            File.Copy(file, backup, false);
            string tmp = Path.Combine(root, ".PacePilot_" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                string content = BytePreserving.GetString(File.ReadAllBytes(file));
                File.WriteAllBytes(tmp, BytePreserving.GetBytes(SetFrameLimit(content, fps)));
                File.Replace(tmp, file, null); // Atomic same-volume replacement.
            }
            finally { try { if (File.Exists(tmp)) File.Delete(tmp); } catch { } }
            return backup;
        }
    }

    public sealed class RtssElevationRequiredException : Exception
    {
        public readonly string Exe;
        public readonly int Fps;
        public RtssElevationRequiredException(string exe,int fps,Exception inner)
            : base("RTSS profile folder requires administrator permission.",inner)
        { Exe=exe; Fps=fps; }
    }

    internal static class ElevatedRtssProfileWriter
    {
        // Limited helper: user approves Windows UAC; no GUI mutex and no RTSS hook injection.
        public static int Execute(string[] args)
        {
            try
            {
                if(args.Length!=3)return 20;
                int fps;
                if(!Int32.TryParse(args[1],out fps)||fps<0||fps>1000)return 21;
                string exe=Encoding.UTF8.GetString(Convert.FromBase64String(args[2]));
                if(exe!="Global" &&
                    (String.IsNullOrWhiteSpace(exe)||Path.GetFileName(exe)!=exe||
                     !exe.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)))return 22;
                RtssControl rtss=new RtssControl();
                AppSettings settings=Store.Load();
                if(!rtss.Initialize(RtssControl.FindFolder(settings.RtssFolder)))return 23;
                string path=rtss.ProfileFile(exe);
                RtssProfileFile.WriteWithBackup(path,fps);
                return RtssProfileFile.ReadLimit(path)==fps &&
                       RtssProfileFile.ReadDenominator(path)==1 ? 0 : 24;
            }
            catch(Exception ex)
            {
                try
                {
                    string folder=Path.Combine(Store.Folder,"Logs");
                    Directory.CreateDirectory(folder);
                    File.AppendAllText(Path.Combine(folder,"RTSS-Elevated-Repair.log"),
                        DateTime.UtcNow.ToString("u")+" "+ex+Environment.NewLine);
                } catch { }
                return 25;
            }
        }
    }

    public sealed class RtssControl
    {
        private IntPtr loaded;
        public string LastTrace { get; private set; }
        public string Folder { get; private set; }
        public bool IsReady { get { return loaded != IntPtr.Zero; } }
        public static string FindFolder(string configured)
        {
            var candidates = new List<string>();
            if (!String.IsNullOrEmpty(configured)) candidates.Add(configured);
            foreach (string registryPath in new string[] { @"SOFTWARE\WOW6432Node\Unwinder\RTSS", @"SOFTWARE\Unwinder\RTSS" })
            {
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
                    {
                        if (key != null)
                        {
                            string registered = key.GetValue("InstallDir") as string;
                            if (!String.IsNullOrWhiteSpace(registered)) candidates.Add(registered);
                        }
                    }
                }
                catch (System.Security.SecurityException) { }
                catch (UnauthorizedAccessException) { }
            }
            foreach (var root in new string[] { Environment.GetEnvironmentVariable("ProgramFiles(x86)"), Environment.GetEnvironmentVariable("ProgramFiles") })
                if (!String.IsNullOrEmpty(root)) candidates.Add(Path.Combine(root, "RivaTuner Statistics Server"));
            foreach (string c in candidates)
                if (File.Exists(Path.Combine(c, "RTSSHooks64.dll")) && File.Exists(Path.Combine(c, "RTSS.exe"))) return c;
            return "";
        }
        public bool Initialize(string path)
        {
            if (IsReady) return true;
            if (String.IsNullOrEmpty(path)) return false;
            string dll = Path.Combine(path, "RTSSHooks64.dll");
            if (!File.Exists(dll)) return false;
            loaded = WinApi.LoadLibraryEx(dll, IntPtr.Zero, 0x00000008); // LOAD_WITH_ALTERED_SEARCH_PATH
            if (loaded != IntPtr.Zero)
            {
                foreach (string name in new string[] { "LoadProfile", "SaveProfile", "GetProfileProperty", "SetProfileProperty", "UpdateProfiles" })
                {
                    if (WinApi.GetProcAddress(loaded, name) == IntPtr.Zero)
                    {
                        WinApi.FreeLibrary(loaded);
                        loaded = IntPtr.Zero;
                        return false;
                    }
                }
                Folder = path;
            }
            return IsReady;
        }
        public bool IsRunning
        {
            get
            {
                try { return Process.GetProcessesByName("RTSS").Length != 0; }
                catch { return false; }
            }
        }
        public bool Start()
        {
            if (!IsReady) return false;
            if (IsRunning) return true;
            Process.Start(new ProcessStartInfo(Path.Combine(Folder, "RTSS.exe")) { WorkingDirectory = Folder, UseShellExecute = true });
            for (int i = 0; i < 12 && !IsRunning; ++i) System.Threading.Thread.Sleep(250);
            return IsRunning;
        }
        public string ProfileFile(string exe)
        {
            if (!IsReady) return "";
            if (exe == "Global") return Path.Combine(Folder, "Profiles", "Global");
            if (String.IsNullOrEmpty(exe) || Path.GetFileName(exe) != exe ||
                !exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid executable profile name.", "exe");
            return Path.Combine(Folder, "Profiles", exe + ".cfg");
        }
        public string Diagnostics(string exe)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("PacePilot 1.4 RTSS diagnostics  " + DateTime.Now.ToString("u"));
            result.AppendLine("Windows: " + Environment.OSVersion.VersionString);
            try { result.AppendLine("Administrator: " + new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)); }
            catch (Exception e) { result.AppendLine("Elevation check error: " + e.Message); }
            result.AppendLine("RTSS path: " + (Folder ?? "not detected"));
            result.AppendLine("DLL available: " + IsReady + " / RTSS running: " + IsRunning);
            if (IsReady)
            {
                try
                {
                    result.AppendLine("RTSS version: " + FileVersionInfo.GetVersionInfo(Path.Combine(Folder, "RTSS.exe")).FileVersion);
                    string file = ProfileFile(exe);
                    result.AppendLine("Selected target: " + exe);
                    result.AppendLine("Profile path: " + file);
                    result.AppendLine("File present: " + File.Exists(file));
                    int? diskFps = RtssProfileFile.ReadLimit(file);
                    int? diskDenom = RtssProfileFile.ReadDenominator(file);
                    result.AppendLine("Disk FPS: " + FormatLimit(diskFps));
                    result.AppendLine("Disk denominator: " + FormatLimit(diskDenom));
                    if (File.Exists(file))
                    {
                        try { using (FileStream f = File.Open(file, FileMode.Open, FileAccess.Write, FileShare.ReadWrite)) { }
                            result.AppendLine("Profile write access: OK"); }
                        catch (Exception e) { result.AppendLine("Profile write access: " + e.GetType().Name + ": " + e.Message); }
                    }
                    if (IsRunning) result.AppendLine("Native API FPS: " + GetLimit(exe));
                }
                catch (Exception e) { result.AppendLine("RTSS diagnostic error: " + e); }
            }
            if (!String.IsNullOrEmpty(LastTrace)) result.AppendLine("Latest apply trace:\r\n" + LastTrace);
            else
            {
                string history = Path.Combine(Store.Folder, "Logs", "RTSS-Actions.log");
                try
                {
                    if (File.Exists(history))
                    {
                        string saved = File.ReadAllText(history, Encoding.UTF8);
                        result.AppendLine("Previous action trace (last 6000 chars):\r\n" +
                            saved.Substring(Math.Max(0, saved.Length - 6000)));
                    }
                }
                catch (Exception e) { result.AppendLine("Could not read previous action trace: " + e.Message); }
            }
            return result.ToString();
        }
        public int GetLimit(string exe)
        {
            if (!IsReady || !IsRunning) throw new InvalidOperationException("Start RTSS first.");
            IntPtr ptr = Marshal.AllocHGlobal(4);
            try
            {
                RtssNative.LoadProfile(exe == "Global" ? "" : exe);
                if (!RtssNative.GetProfileProperty("FramerateLimit", ptr, 4))
                    throw new InvalidOperationException("Could not read RTSS profile property.");
                return Marshal.ReadInt32(ptr);
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }
        public void ApplyLimit(string exe, int fps)
        {
            if (!IsReady) throw new InvalidOperationException("RTSS is not installed or its hooks DLL is unavailable.");
            if (!IsRunning) throw new InvalidOperationException("RTSS is not running.");
            if (fps < 0 || fps > 1000) throw new ArgumentOutOfRangeException("fps");
            StringBuilder trace = new StringBuilder();
            string path = ProfileFile(exe);
            string name = exe == "Global" ? "" : exe;
            trace.AppendLine("Target=" + exe + " / requested FPS=" + fps);
            trace.AppendLine("Profile=" + path);
            IntPtr ptr = Marshal.AllocHGlobal(4);
            try
            {
                trace.AppendLine("Initial disk limit=" + FormatLimit(RtssProfileFile.ReadLimit(path))
                    + ", denominator=" + FormatLimit(RtssProfileFile.ReadDenominator(path)));
                RtssNative.LoadProfile(name);
                if (!RtssNative.GetProfileProperty("FramerateLimit", ptr, 4))
                    throw new InvalidOperationException("RTSS cannot read the existing FramerateLimit property.");
                trace.AppendLine("Initial native limit=" + Marshal.ReadInt32(ptr));
                // Integer FPS must use denominator 1; old profiles may have fractional caps.
                Marshal.WriteInt32(ptr, 1);
                bool denomSet = RtssNative.SetProfileProperty("FramerateLimitDenominator", ptr, 4);
                trace.AppendLine("Set denominator=1: " + denomSet);
                Marshal.WriteInt32(ptr, fps);
                bool accepted = RtssNative.SetProfileProperty("FramerateLimit", ptr, 4);
                trace.AppendLine("Set FPS accepted: " + accepted);
                if (!accepted) throw new InvalidOperationException("RTSS rejected the FPS value.");
                int memory = RtssNative.GetProfileProperty("FramerateLimit", ptr, 4) ? Marshal.ReadInt32(ptr) : -1;
                trace.AppendLine("Immediately after SetProfileProperty: " + memory);
                if (memory != fps)
                    throw new InvalidOperationException("RTSS could not retain the new setting in memory (" + memory + ").");
                RtssNative.SaveProfile(name);
                RtssNative.UpdateProfiles();
                int confirmed = GetLimit(exe);
                int? onDisk = RtssProfileFile.ReadLimit(path);
                int? denomDisk = RtssProfileFile.ReadDenominator(path);
                trace.AppendLine("After Save/Update: native=" + confirmed + ", disk=" + FormatLimit(onDisk)
                    + ", denominator=" + FormatLimit(denomDisk));
                if (confirmed == fps && onDisk == fps && denomDisk == 1)
                {
                    trace.AppendLine("Success: native + on-disk FPS and denominator verified.");
                    return;
                }
                if (onDisk != fps || denomDisk != 1)
                {
                    trace.AppendLine("Native SaveProfile did not persist the value. Trying backup + atomic disk repair.");
                    string backup;
                    try { backup = RtssProfileFile.WriteWithBackup(path, fps); }
                    catch(UnauthorizedAccessException denied)
                    {
                        trace.AppendLine("Protected RTSS profile folder: UAC approval required.");
                        throw new RtssElevationRequiredException(exe,fps,denied);
                    }
                    trace.AppendLine("Backup at " + backup);
                    trace.AppendLine("After repair, disk=" + FormatLimit(RtssProfileFile.ReadLimit(path))
                        + ", denominator=" + FormatLimit(RtssProfileFile.ReadDenominator(path)));
                    RtssNative.UpdateProfiles();
                    confirmed = GetLimit(exe);
                    trace.AppendLine("After repair + UpdateProfiles: native=" + confirmed);
                }
                if (confirmed != fps || RtssProfileFile.ReadLimit(path) != fps || RtssProfileFile.ReadDenominator(path) != 1)
                    throw new InvalidOperationException("RTSS FPS could not be verified: requested=" + fps
                        + ", native=" + confirmed + ", disk=" + FormatLimit(RtssProfileFile.ReadLimit(path))
                        + ", denominator=" + FormatLimit(RtssProfileFile.ReadDenominator(path))
                        + ". Check the RTSS profile or restart RTSS after closing games. See diagnostics.");
                trace.AppendLine("Success: runtime read-back verified after profile repair.");
            }
            catch (RtssElevationRequiredException ex)
            {
                trace.AppendLine("UAC REQUIRED: " + ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                trace.AppendLine("FAIL " + ex.GetType().Name + ": " + ex.Message);
                throw new InvalidOperationException("Unable to apply " + fps + " FPS. " + ex.Message
                    + " Open 'Copy RTSS diagnostics' for details.", ex);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
                LastTrace = trace.ToString();
                try
                {
                    string folder = Path.Combine(Store.Folder, "Logs");
                    Directory.CreateDirectory(folder);
                    File.AppendAllText(Path.Combine(folder, "RTSS-Actions.log"),
                        DateTime.Now.ToString("u") + "\r\n" + LastTrace + "\r\n", Encoding.UTF8);
                }
                catch { /* Logging failure must not mask the real RTSS result. */ }
            }
        }
        public void VerifyElevatedProfile(string exe,int fps)
        {
            string file=ProfileFile(exe);
            if(RtssProfileFile.ReadLimit(file)!=fps || RtssProfileFile.ReadDenominator(file)!=1)
                throw new InvalidOperationException("Administrator repair did not persist FPS.");
            RtssNative.UpdateProfiles();
            int runtime=GetLimit(exe);
            if(runtime!=fps)
                throw new InvalidOperationException("Disk profile saved but RTSS did not reload ("+runtime+"). Restart RTSS and retry.");
        }
        private static string FormatLimit(int? n) { return n.HasValue ? n.Value.ToString() : "unavailable"; }

    }

    public struct FrameSample
    {
        public float Fps;
        public float FrameMs;
        public bool Valid;
    }

    // Read-only RTSSSharedMemoryV2; FPS is measured from the actual rendering process.
    public static class LiveFrames
    {
        public static FrameSample Read(string exe)
        {
            var none = new FrameSample();
            try
            {
                using (var mmf = MemoryMappedFile.OpenExisting("RTSSSharedMemoryV2", MemoryMappedFileRights.Read))
                using (var view = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
                {
                    if (view.Capacity < 40 || view.ReadUInt32(0) != 0x52545353 || view.ReadUInt32(4) < 0x00020000) return none;
                    long entrySize = view.ReadUInt32(8);
                    long entriesOffset = view.ReadUInt32(12);
                    long count = Math.Min((long)view.ReadUInt32(16), 256L);
                    if (entrySize < 284 || entriesOffset < 36 || count < 1 || entriesOffset + entrySize * count > view.Capacity) return none;
                    uint activePid = 0;
                    WinApi.GetWindowThreadProcessId(WinApi.GetForegroundWindow(), out activePid);
                    FrameSample fallback = none;
                    for (long i = 0; i < count; ++i)
                    {
                        long pos = entriesOffset + entrySize * i;
                        uint pid = view.ReadUInt32(pos);
                        if (pid == 0) continue;
                        bool match;
                        if (exe == "Global") match = true;
                        else
                        {
                            byte[] nameBytes = new byte[260];
                            view.ReadArray<byte>(pos + 4, nameBytes, 0, nameBytes.Length);
                            string fullName = System.Text.Encoding.Default.GetString(nameBytes).TrimEnd('\0');
                            match = String.Equals(Path.GetFileName(fullName), exe, StringComparison.OrdinalIgnoreCase);
                        }
                        if (!match) continue;
                        uint frameUs = view.ReadUInt32(pos + 280);
                        uint t0 = view.ReadUInt32(pos + 268), t1 = view.ReadUInt32(pos + 272), frames = view.ReadUInt32(pos + 276);
                        float fps = 0;
                        float ms = 0;
                        if (frameUs >= 500 && frameUs < 1000000)
                        {
                            ms = frameUs / 1000f; fps = 1000f / ms;
                        }
                        else if (t1 > t0 && frames > 0)
                        {
                            fps = frames * 1000f / (t1 - t0); ms = 1000f / fps;
                        }
                        if (fps > 0 && fps < 2000 && ms > 0)
                        {
                            FrameSample entry = new FrameSample { Valid = true, Fps = fps, FrameMs = ms };
                            if (exe != "Global" || pid == activePid) return entry;
                            // When this window is focused, show a real monitored 3D app rather than "--".
                            // Only live processes are eligible for global-mode fallback telemetry.
                            try
                            {
                                using (Process active = Process.GetProcessById((int)pid))
                                {
                                    if (!String.Equals(active.ProcessName, "PacePilot", StringComparison.OrdinalIgnoreCase)
                                        && (!fallback.Valid || entry.Fps > fallback.Fps)) fallback = entry;
                                }
                            }
                            catch (ArgumentException) { }
                        }
                    }
                    return fallback;
                }
            }
            catch (FileNotFoundException) { return none; }
            catch (UnauthorizedAccessException) { return none; }
            catch (IOException) { return none; }
            catch (ArgumentException) { return none; }
            catch { return none; }
        }
    }

    public static class Guard
    {
        private static readonly string[] ProtectedGames = {
            "valorant", "fortniteclient", "fortnite", "r5apex", "rainbowsix", "cod", "modernwarfare", "league of legends", "pubg", "tcon", "destiny2"
        };
        private static readonly string[] ActiveServices = { "EasyAntiCheat", "EasyAntiCheat_EOS", "BEService", "vgc", "vgk", "EAAntiCheat.GameService" };
        public static string Check(string exe)
        {
            string stem = (exe ?? "").ToLowerInvariant();
            if (ProtectedGames.Any(x => stem.Contains(x))) return "This game is on the protected anti-cheat blocklist. No RTSS changes will be made.";
            foreach (string service in ActiveServices)
            {
                try { if (Process.GetProcessesByName(service).Length > 0) return "Anti-cheat process detected (" + service + "). Close the protected game before applying."; }
                catch { }
            }
            if (exe == "Global") return "Global mode can affect protected games. Never use an external limiter in online anti-cheat titles.";
            return "";
        }
    }

    public sealed class TrendPanel : Control
    {
        readonly List<float> values = new List<float>();
        public int Limit { get; set; }
        public bool Persian { get; set; }
        public TrendPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(23, 29, 41);
            Limit = 60;
        }
        public void Reset() { values.Clear(); Invalidate(); }
        public void Add(float frameMs)
        {
            values.Add(frameMs);
            if (values.Count > 100) values.RemoveAt(0);
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF plot = new RectangleF(14, 15, Width - 28, Height - 30);
            if (plot.Width < 20 || plot.Height < 20) return;
            float baseMs = 1000f / Math.Max(1, Limit);
            float maxMs = Math.Max(24, baseMs * 2.4f);
            foreach (float v in values) if (!float.IsNaN(v) && v > maxMs) maxMs = Math.Min(250, v * 1.1f);
            using (Pen grid = new Pen(Color.FromArgb(44, 55, 73), 1))
            {
                for (int j = 0; j <= 4; j++)
                {
                    float y = plot.Top + j * plot.Height / 4;
                    g.DrawLine(grid, plot.Left, y, plot.Right, y);
                }
            }
            float reference = plot.Bottom - baseMs / maxMs * plot.Height;
            using (Pen referencePen = new Pen(Color.FromArgb(64, 125, 240, 175), 1))
            { referencePen.DashStyle = DashStyle.Dash; g.DrawLine(referencePen, plot.Left, reference, plot.Right, reference); }
            using (Font font = new Font("Segoe UI", 8f))
            using (Brush brush = new SolidBrush(Color.FromArgb(139, 154, 176)))
            {
                g.DrawString(maxMs.ToString("0") + " ms", font, brush, plot.Left + 4, plot.Top + 3);
                g.DrawString((Persian ? "هدف " : "target ") + baseMs.ToString("0.0") + " ms", font, brush, plot.Left + 4, Math.Max(plot.Top + 18, reference - 17));
            }
            if (values.Count < 2) return;
            List<PointF> pts = new List<PointF>();
            int start = Math.Max(0, values.Count - 100);
            for (int i = start; i < values.Count; i++)
            {
                float v = values[i];
                if (float.IsNaN(v) || v <= 0)
                {
                    if (pts.Count > 1) DrawSegment(g, pts);
                    pts.Clear(); continue;
                }
                float x = plot.Left + (i - start) * plot.Width / 99f;
                float y = plot.Bottom - Math.Min(v, maxMs) * plot.Height / maxMs;
                pts.Add(new PointF(x, y));
            }
            if (pts.Count > 1) DrawSegment(g, pts);
        }
        private void DrawSegment(Graphics g, List<PointF> pts)
        {
            using (Pen glow = new Pen(Color.FromArgb(40, 98, 235, 197), 6)) g.DrawLines(glow, pts.ToArray());
            using (Pen line = new Pen(Color.FromArgb(84, 243, 194), 2)) g.DrawLines(line, pts.ToArray());
        }
    }

    public sealed class RoundPanel : Panel
    {
        public int Radius { get; set; }
        public RoundPanel() { DoubleBuffered = true; Radius = 18; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var path = new GraphicsPath())
            {
                int r = Math.Max(1, Radius);
                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                path.AddArc(rect.Left, rect.Top, r, r, 180, 90);
                path.AddArc(rect.Right - r, rect.Top, r, r, 270, 90);
                path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
                path.AddArc(rect.Left, rect.Bottom - r, r, r, 90, 90);
                path.CloseFigure();
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(49, 59, 74))) e.Graphics.DrawPath(pen, path);
            }
        }
    }

    public class MainForm : Form
    {
        readonly AppSettings settings;
        readonly RtssControl rtss = new RtssControl();
        readonly Color background = Color.FromArgb(13, 18, 29);
        readonly Color card = Color.FromArgb(23, 29, 41);
        readonly Color muted = Color.FromArgb(151, 163, 184);
        readonly Color accent = Color.FromArgb(82, 230, 178);
        ComboBox targets;
        NumericUpDown fps;
        Label liveFps, liveFrame, status, backend, subtitle, monitorHz;
        TrendPanel trend;
        Button applyButton, toggleButton;
        readonly Dictionary<int, Button> presetButtons = new Dictionary<int, Button>();
        Panel layoutBody, surface;
        RoundPanel limiterCard, metricsCard, graphCard;
        Label titleLabel, historyLabel;
        int lastAppliedFps = 60;
        bool globalConsent;
        CheckBox trayCheck, startupCheck, topCheck, autoCheck;
        NotifyIcon notify;
        System.Windows.Forms.Timer ticker;
        bool actuallyClosing;
        readonly bool testMode;
        bool installingRtss;
        bool promptedRtss;
        ComboBox languagePicker;
        Button rtssButton;
        private bool Farsi { get { return String.Equals(settings.LanguageCode, "fa", StringComparison.OrdinalIgnoreCase); } }
        private string T(string english, string persian) { return Farsi ? persian : english; }
        int restoredFps = 60;
        string selectedTarget = "Global";
        bool changingTarget;
        public MainForm(bool startInTray) : this(startInTray, false) { }
        public MainForm(bool startInTray, bool runTest)
        {
            testMode = runTest;
            settings = Store.Load();
            if (settings.LanguageCode != "fa") settings.LanguageCode = "en";
            if (settings.Games == null) settings.Games = new List<GameProfile>();
            Text = T("PacePilot 1.4 - Windows Frame Pacing", "PacePilot 1.4 - کنترل فریم ویندوز");
            BackColor = background; ForeColor = Color.White;
            ClientSize = new Size(1060, 730); MinimumSize = new Size(1000, 690);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f); AutoScaleMode = AutoScaleMode.Dpi;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { Icon = SystemIcons.Application; }
            BuildUI();
            if (!testMode) { InitTray(); InitBackend(); }
            ReloadTargets();
            targetSelect(settings.LastTarget);
            if (!testMode)
            {
                SetStartup(settings.StartWithWindows, false);
                ticker = new System.Windows.Forms.Timer(); ticker.Interval = 350;
                ticker.Tick += delegate { Tick(); };
                ticker.Start();
            }
            Shown += delegate { LayoutContent(); if (!testMode) BeginInvoke(new Action(CheckRtssFirstRun)); };
            TopMost = settings.AlwaysOnTop;
            if (startInTray) { WindowState = FormWindowState.Minimized; ShowInTaskbar = false; }
        }
        Label MakeLabel(Control parent, string text, int x, int y, int w, int h, int size, bool bold, Color color)
        {
            Label l = new Label(); l.Text = UI(text); l.RightToLeft = Farsi ? RightToLeft.Yes : RightToLeft.No; l.TextAlign = Farsi ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft; l.Location = new Point(x, y); l.Size = new Size(w, h);
            l.ForeColor = color; l.BackColor = Color.Transparent;
            l.Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
            l.AutoEllipsis = true;
            parent.Controls.Add(l); return l;
        }
        Button MakeButton(Control parent, string text, int x, int y, int w, int h, Color bg, Color fg)
        {
            Button b = new Button(); b.Text = UI(text); b.RightToLeft = Farsi ? RightToLeft.Yes : RightToLeft.No; b.Location = new Point(x,y); b.Size = new Size(w,h);
            b.BackColor = bg; b.ForeColor = fg; b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0; b.Cursor = Cursors.Hand;
            b.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            parent.Controls.Add(b); return b;
        }
        RoundPanel MakeCard(Control parent, int x, int y, int w, int h)
        {
            RoundPanel p = new RoundPanel(); p.Location = new Point(x,y); p.Size = new Size(w,h); p.BackColor = card;
            parent.Controls.Add(p); return p;
        }

        private string UI(string english)
        {
            if (!Farsi) return english;
            switch (english)
            {
                case "FRAME CONTROL": return "کنترل فریم";
                case "TARGET": return "پروفایل هدف";
                case "+ Select game EXE": return "+ انتخاب فایل بازی";
                case "Pick running game": return "انتخاب بازی در حال اجرا";
                case "Remove profile": return "حذف پروفایل";
                case "SHORTCUTS": return "کلیدهای میانبر";
                case "Alt + Shift + L     On / Off": return "Alt + Shift + L    روشن / خاموش";
                case "Alt + Shift + ↑     +1 FPS": return "Alt + Shift + ↑    +۱ فریم";
                case "Alt + Shift + ↓     -1 FPS": return "Alt + Shift + ↓    -۱ فریم";
                case "Copy RTSS diagnostics": return "کپی گزارش RTSS";
                case "RTSS setup / open": return "نصب / باز کردن RTSS";
                case "Developer: Mr.HamidRouter": return "توسعه‌دهنده: Mr.HamidRouter";
                case "A smoother frame. A steadier game.": return "فریم روان‌تر، بازی پایدارتر";
                case "Apply precision pacing with a real RTSS limiter": return "کنترل دقیق فریم با استفاده از RTSS";
                case "FRAMERATE LIMIT": return "محدودیت نرخ فریم";
                case "0 = unlimited": return "۰ = بدون محدودیت";
                case "APPLY LIMIT": return "اعمال محدودیت";
                case "Disable": return "غیرفعال‌کردن";
                case "LIVE FPS": return "فریم زنده";
                case "FRAME TIME": return "زمان فریم";
                case "Refresh display": return "بروزرسانی نمایشگر";
                case "FRAME-TIME HISTORY  ·  measured, never simulated": return "نمودار زمان فریم  ·  اندازه‌گیری واقعی";
                case "Ready": return "آماده";
                case "Use selected game": return "انتخاب بازی";
                default: return english;
            }
        }
        private void ChangeLanguage(string code)
        {
            if (code != "fa" && code != "en") return;
            if (settings.LanguageCode == code) return;
            string target = selectedTarget;
            settings.LanguageCode = code;
            Text = T("PacePilot 1.4 - Windows Frame Pacing", "PacePilot 1.4 - کنترل فریم ویندوز");
            Save();
            // Recreate localized controls without touching running RTSS or profiles.
            if (Controls.Count > 0)
            {
                Control existing = Controls[0];
                Controls.Remove(existing);
                existing.Dispose();
            }
            presetButtons.Clear();
            BuildUI();
            ReloadTargets();
            targetSelect(target);
            LayoutContent();
            if (!testMode) Tick();
            if (notify != null) SetupTrayMenu();
        }

        void BuildUI()
        {
            // Dock fill and left sibling panels can overlap depending on z-order.
            // A true two-column layout ensures the sidebar never hides the content.
            TableLayoutPanel shell = new TableLayoutPanel();
            shell.Dock = DockStyle.Fill; shell.Margin = Padding.Empty; shell.Padding = Padding.Empty;
            shell.ColumnCount = 2; shell.RowCount = 1;
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(shell);
            Panel sidebar = new Panel(); sidebar.Dock = DockStyle.Fill; sidebar.Margin = Padding.Empty;
            sidebar.BackColor = Color.FromArgb(17, 23, 35); sidebar.AutoScroll = true;
            shell.Controls.Add(sidebar, 0, 0);
            MakeLabel(sidebar, "◈", 20, 28, 40, 40, 25, true, accent);
            MakeLabel(sidebar, "PacePilot", 58, 29, 151, 42, 20, true, Color.White);
            MakeLabel(sidebar, "Language / زبان", 26, 79, 115, 22, 9, true, muted);
            languagePicker = new ComboBox(); languagePicker.Location = new Point(132, 78);
            languagePicker.Size = new Size(68, 26); languagePicker.DropDownStyle = ComboBoxStyle.DropDownList;
            languagePicker.FlatStyle = FlatStyle.Flat; languagePicker.BackColor = Color.FromArgb(36, 45, 59);
            languagePicker.ForeColor = Color.White; languagePicker.Items.Add("EN"); languagePicker.Items.Add("فا");
            sidebar.Controls.Add(languagePicker);
            languagePicker.SelectedIndex = Farsi ? 1 : 0;
            languagePicker.SelectedIndexChanged += delegate { ChangeLanguage(languagePicker.SelectedIndex == 1 ? "fa" : "en"); };
            MakeLabel(sidebar, "TARGET", 25, 135, 174, 21, 9, true, muted);
            targets = new ComboBox(); targets.Location = new Point(23, 162); targets.Width = 172;
            targets.DropDownStyle = ComboBoxStyle.DropDownList; targets.FlatStyle = FlatStyle.Flat;
            targets.BackColor = Color.FromArgb(36, 45, 59); targets.ForeColor = Color.White;
            sidebar.Controls.Add(targets);
            targets.SelectedIndexChanged += delegate { TargetChanged(); };
            Button addGame = MakeButton(sidebar, "+ Select game EXE", 22, 206, 174, 38, Color.FromArgb(41, 56, 68), Color.White);
            addGame.Click += delegate { AddGameFromDialog(); };
            Button running = MakeButton(sidebar, "Pick running game", 22, 254, 174, 38, Color.FromArgb(41, 56, 68), Color.White);
            running.Click += delegate { PickRunning(); };
            Button remove = MakeButton(sidebar, "Remove profile", 22, 302, 174, 33, Color.FromArgb(40, 43, 56), muted);
            remove.Click += delegate { RemoveProfile(); };
            autoCheck = new CheckBox(); autoCheck.Text = T("Auto-select saved games", "انتخاب خودکار بازی‌های ذخیره‌شده");
            autoCheck.Checked = settings.AutoSelectKnownGames;
            autoCheck.ForeColor = muted; autoCheck.Location = new Point(24, 347); autoCheck.Size = new Size(185, 26);
            sidebar.Controls.Add(autoCheck); autoCheck.RightToLeft = Farsi ? RightToLeft.Yes : RightToLeft.No;
            autoCheck.CheckedChanged += delegate { settings.AutoSelectKnownGames = autoCheck.Checked; Save(); };
            MakeLabel(sidebar, "SHORTCUTS", 25, 388, 170, 20, 9, true, muted);
            MakeLabel(sidebar, "Alt + Shift + L     On / Off", 25, 413, 180, 22, 9, false, Color.White);
            MakeLabel(sidebar, "Alt + Shift + ↑     +1 FPS", 25, 442, 180, 22, 9, false, Color.White);
            MakeLabel(sidebar, "Alt + Shift + ↓     -1 FPS", 25, 471, 180, 22, 9, false, Color.White);
            Button diagnostic = MakeButton(sidebar, "Copy RTSS diagnostics", 22, 515, 174, 33, Color.FromArgb(40, 43, 56), Color.White);
            diagnostic.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            diagnostic.Click += delegate {
                try { Clipboard.SetText(rtss.Diagnostics(selectedTarget)); status.ForeColor = accent; status.Text = T("RTSS diagnostic copied. You can paste it into chat.", "گزارش RTSS کپی شد؛ آن را برای پشتیبانی ارسال کنید."); }
                catch (Exception ex) { MessageBox.Show(T("Could not copy diagnostics: ", "خطا در کپی گزارش: ") + ex.Message); }
            };
            backend = MakeLabel(sidebar, "RTSS: checking...", 25, 565, 185, 26, 9, true, muted);
            backend.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            rtssButton = MakeButton(sidebar, "RTSS setup / open", 22, 607, 174, 37, Color.FromArgb(34, 47, 62), accent);
            rtssButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            rtssButton.Click += delegate { BackendMenu(); };

            Label developer = MakeLabel(sidebar, "Developer: Mr.HamidRouter", 22, 654, 190, 25, 9, false, muted);

            layoutBody = new Panel(); layoutBody.Dock = DockStyle.Fill; layoutBody.Margin = Padding.Empty;
            layoutBody.AutoScroll = true; layoutBody.BackColor = background;
            shell.Controls.Add(layoutBody, 1, 0);
            surface = new Panel(); surface.Location = Point.Empty; surface.Size = new Size(800, 690);
            layoutBody.Controls.Add(surface);
            titleLabel = MakeLabel(surface, "A smoother frame. A steadier game.", 28, 22, 720, 40, 18, true, Color.White);
            subtitle = MakeLabel(surface, "Apply precision pacing with a real RTSS limiter", 30, 64, 720, 30, 10, false, muted);
            limiterCard = MakeCard(surface, 28, 112, 744, 183);
            MakeLabel(limiterCard, "FRAMERATE LIMIT", 24, 21, 330, 23, 10, true, muted);
            fps = new NumericUpDown(); fps.Minimum = 0; fps.Maximum = 1000; fps.Value = 60;
            fps.Font = new Font("Segoe UI", 28, FontStyle.Bold); fps.BackColor = card; fps.ForeColor = Color.White;
            fps.BorderStyle = BorderStyle.None; fps.Location = new Point(25, 54); fps.Size = new Size(158, 54);
            fps.TextAlign = HorizontalAlignment.Center; limiterCard.Controls.Add(fps);
            MakeLabel(limiterCard, "FPS", 180, 67, 70, 45, 16, true, accent);
            MakeLabel(limiterCard, "0 = unlimited", 30, 121, 157, 24, 9, false, muted);
            foreach (int cap in new int[] { 30, 40, 60, 120, 144 })
            {
                int v = cap;
                Button preset = MakeButton(limiterCard, v.ToString(), 265, 72, 56, 36, Color.FromArgb(45, 55, 70), Color.White);
                preset.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                preset.FlatAppearance.BorderSize = 1;
                presetButtons.Add(v, preset);
                preset.Click += delegate { fps.Value = v; UpdateSelectionUI(); };
            }
            applyButton = MakeButton(limiterCard, "APPLY LIMIT", 475, 121, 215, 45, accent, Color.FromArgb(10, 24, 27));
            applyButton.Click += delegate { Apply(false); };
            toggleButton = MakeButton(limiterCard, "Disable", 295, 121, 170, 45, Color.FromArgb(43, 52, 67), Color.White);
            toggleButton.Click += delegate { Toggle(); };
            fps.ValueChanged += delegate { UpdateSelectionUI(); };

            metricsCard = MakeCard(surface, 28, 313, 744, 104);
            MakeLabel(metricsCard, "LIVE FPS", 20, 13, 130, 22, 9, true, muted);
            liveFps = MakeLabel(metricsCard, "--", 20, 36, 180, 53, 24, true, accent);
            MakeLabel(metricsCard, "FRAME TIME", 268, 13, 160, 22, 9, true, muted);
            liveFrame = MakeLabel(metricsCard, "-- ms", 268, 36, 200, 53, 24, true, Color.White);
            monitorHz = MakeLabel(metricsCard, "DISPLAY  -- Hz", 550, 20, 179, 30, 9, true, muted);
            Button refresh = MakeButton(metricsCard, "Refresh display", 550, 54, 155, 30, Color.FromArgb(39, 48, 61), Color.White);
            refresh.Click += delegate { ShowHz(); };
            refresh.Tag = "refresh";
            historyLabel = MakeLabel(surface, "FRAME-TIME HISTORY  ·  measured, never simulated", 29, 431, 745, 27, 9, true, muted);
            graphCard = MakeCard(surface, 28, 466, 744, 124);
            trend = new TrendPanel(); trend.Persian = Farsi; trend.Dock = DockStyle.Fill; trend.Padding = new Padding(6); graphCard.Controls.Add(trend);
            status = MakeLabel(surface, "Ready", 30, 598, 740, 28, 10, false, accent);
            trayCheck = new CheckBox(); trayCheck.Text = T("Close to tray", "بستن در کنار ساعت"); trayCheck.Checked = settings.MinimizeToTray;
            trayCheck.ForeColor = muted; trayCheck.Location = new Point(30, 644); trayCheck.Width = 135;
            surface.Controls.Add(trayCheck); trayCheck.RightToLeft = Farsi ? RightToLeft.Yes : RightToLeft.No;
            trayCheck.CheckedChanged += delegate { settings.MinimizeToTray = trayCheck.Checked; Save(); };
            startupCheck = new CheckBox(); startupCheck.Text = T("Start with Windows", "اجرا همراه ویندوز"); startupCheck.Checked = settings.StartWithWindows;
            startupCheck.ForeColor = muted; startupCheck.Location = new Point(184, 644); startupCheck.Width = 171;
            surface.Controls.Add(startupCheck); startupCheck.RightToLeft = Farsi ? RightToLeft.Yes : RightToLeft.No;
            startupCheck.CheckedChanged += delegate { SetStartup(startupCheck.Checked, true); };
            topCheck = new CheckBox(); topCheck.Text = T("Always on top", "همیشه روی پنجره‌ها"); topCheck.Checked = settings.AlwaysOnTop;
            topCheck.ForeColor = muted; topCheck.Location = new Point(377, 644); topCheck.Width = 139;
            surface.Controls.Add(topCheck); topCheck.RightToLeft = Farsi ? RightToLeft.Yes : RightToLeft.No;
            topCheck.CheckedChanged += delegate { TopMost = settings.AlwaysOnTop = topCheck.Checked; Save(); };
            layoutBody.Resize += delegate { LayoutContent(); };
            LayoutContent();
            UpdateSelectionUI();
            ShowHz();
        }
        void LayoutContent()
        {
            if (layoutBody == null || surface == null || graphCard == null) return;
            int width = Math.Min(1060, Math.Max(0, layoutBody.ClientSize.Width));
            int height = Math.Max(690, layoutBody.ClientSize.Height);
            surface.Bounds = new Rectangle(Math.Max(0, (layoutBody.ClientSize.Width - width) / 2), 0, width, height);
            int cardWidth = Math.Max(0, width - 56);
            limiterCard.Width = cardWidth;
            metricsCard.Width = cardWidth;
            graphCard.Width = cardWidth;
            graphCard.Height = Math.Max(105, height - 575);
            status.Location = new Point(30, height - 91);
            status.Width = Math.Max(0, width - 60);
            trayCheck.Top = startupCheck.Top = topCheck.Top = height - 45;
            titleLabel.Width = Math.Max(0, width - 56);
            subtitle.Width = Math.Max(0, width - 60);
            historyLabel.Width = Math.Max(0, width - 58);
            int px = Math.Max(260, cardWidth - 413);
            foreach (int cap in new int[] { 30, 40, 60, 120, 144 })
            {
                presetButtons[cap].Left = px;
                px += 65;
            }
            applyButton.Left = cardWidth - applyButton.Width - 24;
            toggleButton.Left = applyButton.Left - toggleButton.Width - 12;
            foreach (Control c in metricsCard.Controls)
            {
                if (c == monitorHz || (c is Button && Object.Equals(c.Tag, "refresh")))
                    c.Left = Math.Max(495, cardWidth - 192);
            }
        }
        void UpdateSelectionUI()
        {
            if (fps == null || applyButton == null) return;
            int limit = (int)fps.Value;
            foreach (KeyValuePair<int, Button> entry in presetButtons)
            {
                bool selected = entry.Key == limit;
                entry.Value.BackColor = selected ? accent : Color.FromArgb(45, 55, 70);
                entry.Value.ForeColor = selected ? background : Color.White;
                entry.Value.FlatAppearance.BorderColor = selected ? Color.White : Color.FromArgb(60, 76, 93);
            }
            toggleButton.Text = limit == 0 ? T("Enable", "فعال‌کردن") : T("Disable", "غیرفعال‌کردن");
            bool pending = limit != lastAppliedFps;
            applyButton.Text = pending ? T("APPLY LIMIT *", "اعمال محدودیت *") : T("APPLY LIMIT", "اعمال محدودیت");
            if (status != null)
            {
                status.ForeColor = muted;
                status.Text = pending
                    ? T("Selected ", "انتخاب‌شده: ") + (limit == 0 ? T("unlimited", "نامحدود") : limit + " FPS") + T(" — click APPLY LIMIT to activate in RTSS.", " — برای فعال‌سازی روی اعمال محدودیت بزنید.")
                    : T("Selected ", "انتخاب‌شده: ") + (limit == 0 ? T("unlimited", "نامحدود") : limit + " FPS") + T(" (saved value) — click APPLY LIMIT to verify RTSS.", " (مقدار ذخیره‌شده) — برای تأیید روی اعمال بزنید.");
            }
        }
        private void ShowHz()
        {
            try { monitorHz.Text = T("DISPLAY  ", "نمایشگر  ") + Math.Round(Screen.PrimaryScreen.Bounds.Width == 0 ? 0 : GetDisplayRefreshRate()).ToString() + " Hz"; }
            catch { monitorHz.Text = T("DISPLAY  unknown", "نرخ نمایشگر نامشخص"); }
        }
        private double GetDisplayRefreshRate()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            { return GetRefreshFromDeviceContext(g); }
        }
        private double GetRefreshFromDeviceContext(Graphics g)
        {
            IntPtr dc = g.GetHdc();
            try { return NativeDisplay.GetDeviceCaps(dc, 116); }
            finally { g.ReleaseHdc(dc); }
        }
        static class NativeDisplay { [DllImport("gdi32.dll")] public static extern int GetDeviceCaps(IntPtr hdc, int idx); }
        void InitTray()
        {
            notify = new NotifyIcon(); notify.Icon = Icon ?? SystemIcons.Application; notify.Text = "PacePilot 1.4";
            SetupTrayMenu();
            notify.DoubleClick += delegate { RestoreWindow(); }; notify.Visible = true;
        }
        void SetupTrayMenu()
        {
            ContextMenuStrip oldMenu = notify.ContextMenuStrip;
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.RightToLeft = Farsi ? RightToLeft.Yes : RightToLeft.No;
            menu.Items.Add(T("Show PacePilot", "نمایش PacePilot"), null, delegate { RestoreWindow(); });
            menu.Items.Add(T("Enable / disable limit", "فعال / غیرفعال‌کردن محدودیت"), null, delegate { Toggle(); });
            menu.Items.Add(T("Exit", "خروج"), null, delegate { actuallyClosing = true; Close(); });
            notify.ContextMenuStrip = menu;
            if (oldMenu != null) oldMenu.Dispose();
        }
        void RestoreWindow()
        {
            ShowInTaskbar = true; Show(); WindowState = FormWindowState.Normal; Activate();
        }
        void InitBackend()
        {
            string path = RtssControl.FindFolder(settings.RtssFolder);
            if (rtss.Initialize(path)) { settings.RtssFolder = path; backend.Text = T("● RTSS found", "● RTSS شناسایی شد"); }
            else backend.Text = T("○ RTSS not found", "○ RTSS نصب نیست");
            Save();
        }
        // Winget validates installer SHA256 against the curated package manifest.
        // We never download an executable from an arbitrary redirect or disable hash checks.
        private const string OfficialRtss = "https://www.guru3d.com/download/rtss-rivatuner-statistics-server-download/";
        private void CheckRtssFirstRun()
        {
            if (testMode || promptedRtss || rtss.IsReady || installingRtss) return;
            promptedRtss = true;
            if (MessageBox.Show(this,
                T("RivaTuner Statistics Server (RTSS) is not installed.\n\nDownload and install the official Guru3D.RTSS package using Windows Package Manager? Windows may ask for administrator approval.\n\nNo installation will start without your consent.",
                  "RivaTuner Statistics Server (RTSS) نصب نشده است.\n\nآیا بسته رسمی Guru3D.RTSS از طریق مدیریت بسته‌های ویندوز دانلود و نصب شود؟ ممکن است تأیید مدیر سیستم لازم باشد.\n\nبدون موافقت شما نصب آغاز نمی‌شود."),
                T("Install RTSS?", "نصب RTSS"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                InstallRtss();
        }
        private void InstallRtss()
        {
            if (installingRtss) return;
            if (rtss.IsReady) { BackendMenu(); return; }
            installingRtss = true;
            rtssButton.Enabled = false;
            status.ForeColor = muted;
            status.Text = T("Installing RTSS through winget; please wait and approve Windows prompts...", "نصب RTSS از طریق winget در حال انجام است؛ لطفاً منتظر بمانید و پیام‌های ویندوز را تأیید کنید...");
            var worker = new BackgroundWorker();
            worker.DoWork += delegate(object sender, DoWorkEventArgs e)
            {
                try
                {
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.FileName = "winget.exe";
                    info.Arguments = "install --id Guru3D.RTSS --exact --source winget --silent --accept-package-agreements --accept-source-agreements --disable-interactivity";
                    info.UseShellExecute = true;
                    info.Verb = "runas";  // Windows UAC prompt only after explicit user consent.
                    info.WindowStyle = ProcessWindowStyle.Normal;
                    using (Process process = Process.Start(info))
                    {
                        if (process == null) throw new InvalidOperationException("winget did not start");
                        if (!process.WaitForExit(300000))
                        {
                            try { process.Kill(); } catch { }
                            throw new TimeoutException("RTSS install took longer than 5 minutes");
                        }
                        e.Result = process.ExitCode;
                    }
                }
                catch (Exception ex) { e.Result = ex; }
            };
            worker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
            {
                installingRtss = false;
                rtssButton.Enabled = true;
                object outcome = e.Result;
                string path = RtssControl.FindFolder(settings.RtssFolder);
                if (rtss.Initialize(path))
                {
                    settings.RtssFolder = path;
                    Save();
                    try { rtss.Start(); } catch (Exception ex) { status.Text = T("RTSS installed; start failed: ", "RTSS نصب شد؛ اجرا ناموفق بود: ") + ex.Message; }
                    status.ForeColor = accent;
                    status.Text = T("RTSS installed and detected successfully.", "RTSS با موفقیت نصب و شناسایی شد.");
                    backend.Text = T("● RTSS found", "● RTSS شناسایی شد");
                    Tick();
                    return;
                }
                status.ForeColor = Color.FromArgb(255, 133, 128);
                status.Text = T("RTSS installation was not completed. Use official Guru3D download.", "نصب RTSS کامل نشد. از دانلود رسمی Guru3D استفاده کنید.");
                string error = outcome is Exception ? ((Exception)outcome).Message : "winget exit code: " + Convert.ToString(outcome);
                DialogResult answer = MessageBox.Show(this,
                    T("Automatic installation failed or was cancelled.\n", "نصب خودکار انجام نشد یا لغو شد.\n") + error +
                    T("\n\nOpen the official Guru3D RTSS download page? You can also select an existing RTSS folder afterward.", "\n\nصفحه دانلود رسمی RTSS باز شود؟ بعداً می‌توانید پوشه نصب را نیز انتخاب کنید."),
                    T("RTSS setup", "راه‌اندازی RTSS"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer == DialogResult.Yes) OpenRtssDownload();
            };
            worker.RunWorkerAsync();
        }
        private void OpenRtssDownload()
        {
            try { Process.Start(new ProcessStartInfo(OfficialRtss) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, T("Browser error", "خطای مرورگر")); }
        }
        void BackendMenu()
        {
            if (rtss.IsReady)
            {
                try
                {
                    if (!rtss.IsRunning) rtss.Start();
                    else Process.Start(new ProcessStartInfo(Path.Combine(rtss.Folder, "RTSS.exe")) { UseShellExecute = true });
                    status.Text = T("RTSS started or opened.", "RTSS اجرا یا باز شد.");
                    return;
                }
                catch (Exception ex) { status.Text = T("RTSS start error: ", "خطا در اجرای RTSS: ") + ex.Message; }
            }
            DialogResult option = MessageBox.Show(this,
                T("RTSS is missing.\n\nYES: Download and install with winget.\nNO: Select existing RTSS folder.\nCANCEL: Open official download page.",
                  "RTSS پیدا نشد.\n\nبله: دانلود و نصب خودکار با winget\nخیر: انتخاب پوشه نصب فعلی\nانصراف: صفحه دانلود رسمی"),
                T("RTSS setup", "نصب RTSS"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (option == DialogResult.Yes) { InstallRtss(); return; }
            if (option == DialogResult.Cancel) { OpenRtssDownload(); return; }
            using (FolderBrowserDialog f = new FolderBrowserDialog())
            {
                f.Description = T("Select the folder containing RTSS.exe and RTSSHooks64.dll", "پوشه شامل RTSS.exe و RTSSHooks64.dll را انتخاب کنید");
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    if (rtss.Initialize(f.SelectedPath))
                    { settings.RtssFolder = f.SelectedPath; Save(); backend.Text = T("● RTSS found", "● RTSS شناسایی شد"); status.Text = T("RTSS installation linked.", "پوشه نصب RTSS متصل شد."); }
                    else MessageBox.Show(T("RTSS.exe or RTSSHooks64.dll was not found in this folder.\nGet RTSS from Guru3D.", "فایل‌های RTSS.exe یا RTSSHooks64.dll در این پوشه پیدا نشدند.\nRTSS را از Guru3D دریافت کنید."), T("Incorrect RTSS folder", "پوشه RTSS نامعتبر"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        void ReloadTargets()
        {
            changingTarget = true;
            targets.Items.Clear(); targets.Items.Add(T("Global (all games)", "عمومی (همه بازی‌ها)"));
            foreach (GameProfile item in settings.Games.OrderBy(x => x.Exe, StringComparer.OrdinalIgnoreCase)) targets.Items.Add(item.Exe);
            changingTarget = false;
        }
        void targetSelect(string target)
        {
            changingTarget = true;
            int idx = target == "Global" ? 0 : targets.Items.IndexOf(target);
            targets.SelectedIndex = idx < 0 ? 0 : idx;
            changingTarget = false;
            TargetChanged();
        }
        void TargetChanged()
        {
            if (changingTarget) return;
            selectedTarget = targets.SelectedIndex <= 0 ? "Global" : targets.SelectedItem.ToString();
            settings.LastTarget = selectedTarget;
            GameProfile p = settings.Games.FirstOrDefault(x => String.Equals(x.Exe, selectedTarget, StringComparison.OrdinalIgnoreCase));
            int cap = selectedTarget == "Global" ? settings.GlobalLimit : p == null ? 60 : p.Limit;
            bool readFromRtss = false;
            if (rtss.IsReady && rtss.IsRunning)
            {
                try
                {
                    int actual = rtss.GetLimit(selectedTarget);
                    if (actual >= 0 && actual <= 1000) { cap = actual; readFromRtss = true; }
                }
                catch (Exception) { /* use last stored value if backend read fails */ }
            }
            lastAppliedFps = cap;
            fps.Value = Math.Max(0, Math.Min(1000, cap));
            restoredFps = cap > 0 ? cap : 60;
            subtitle.Text = selectedTarget == "Global" ? T("Global RTSS profile • affects all detected games", "پروفایل عمومی RTSS • برای تمام بازی‌ها") : T("Selected game: ", "بازی انتخاب‌شده: ") + selectedTarget;
            trend.Limit = cap > 0 ? cap : 60; trend.Reset();
            UpdateSelectionUI();
            status.ForeColor = readFromRtss ? accent : muted;
            status.Text = readFromRtss ? T("Current RTSS limit: ", "محدودیت فعلی RTSS: ") + (cap == 0 ? T("OFF", "خاموش") : cap + " FPS") + "  ·  " + selectedTarget
                : T("Target ready — last saved ", "پروفایل آماده است — آخرین مقدار ذخیره‌شده: ") + cap + T(" FPS. RTSS read-back unavailable.", " FPS. امکان بازخوانی RTSS نیست.");
            Save();
        }
        void AddGameFromDialog()
        {
            using (OpenFileDialog f = new OpenFileDialog())
            {
                f.Filter = T("Games (*.exe)|*.exe", "بازی‌ها (*.exe)|*.exe"); f.Title = T("Select a game executable", "فایل اجرایی بازی را انتخاب کنید");
                if (f.ShowDialog(this) == DialogResult.OK) AddGame(f.FileName);
            }
        }
        void AddGame(string path)
        {
            string exe = Path.GetFileName(path);
            if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return;
            if (String.Equals(exe, "PacePilot.exe", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(exe, "RTSS.exe", StringComparison.OrdinalIgnoreCase)) return;
            GameProfile p = settings.Games.FirstOrDefault(x => String.Equals(x.Exe, exe, StringComparison.OrdinalIgnoreCase));
            if (p == null) settings.Games.Add(new GameProfile { Exe = exe, FullPath = path, Limit = 60 });
            else p.FullPath = path;
            ReloadTargets(); targetSelect(exe); Save();
        }
        void PickRunning()
        {
            using (Form chooser = new Form())
            {
                chooser.Text = T("Choose a running game", "انتخاب بازی در حال اجرا"); chooser.BackColor = background; chooser.ForeColor = Color.White;
                chooser.ClientSize = new Size(570, 430); chooser.StartPosition = FormStartPosition.CenterParent;
                ListBox list = new ListBox(); list.Dock = DockStyle.Fill; list.BackColor = card; list.ForeColor = Color.White;
                list.Font = new Font("Segoe UI", 11); chooser.Controls.Add(list);
                var names = new Dictionary<string, string>();
                foreach (Process p in Process.GetProcesses().OrderBy(x => x.ProcessName))
                {
                    try
                    {
                        if (p.MainWindowHandle == IntPtr.Zero || String.IsNullOrWhiteSpace(p.MainWindowTitle)) continue;
                        if (p.Id == Process.GetCurrentProcess().Id) continue;
                        string exe = p.ProcessName + ".exe";
                        string display = exe + "    •    " + p.MainWindowTitle;
                        if (names.ContainsKey(display)) continue;
                        string path = exe;
                        try { path = p.MainModule.FileName; } catch { }
                        names.Add(display, path); list.Items.Add(display);
                    }
                    catch { }
                    finally { p.Dispose(); }
                }
                Panel footer = new Panel(); footer.Dock = DockStyle.Bottom; footer.Height = 63; footer.BackColor = background;
                chooser.Controls.Add(footer);
                Button confirm = MakeButton(footer, "Use selected game", 360, 9, 190, 45, accent, background);
                confirm.Click += delegate { if (list.SelectedItem != null) chooser.DialogResult = DialogResult.OK; };
                list.DoubleClick += delegate { if (list.SelectedItem != null) chooser.DialogResult = DialogResult.OK; };
                if (chooser.ShowDialog(this) == DialogResult.OK && list.SelectedItem != null)
                    AddGame(names[list.SelectedItem.ToString()]);
            }
        }
        void RemoveProfile()
        {
            if (selectedTarget == "Global") return;
            string removed = selectedTarget;
            // Only remove PacePilot's saved entry, never remove an RTSS profile made by another application.
            settings.Games.RemoveAll(x => String.Equals(x.Exe, removed, StringComparison.OrdinalIgnoreCase));
            ReloadTargets(); targetSelect("Global"); status.Text = T("Removed from PacePilot list. Existing RTSS settings were not modified.", "پروفایل فقط از PacePilot حذف شد؛ تنظیمات RTSS تغییر نکردند.");
        }
        private bool EnsureReady()
        {
            if (!rtss.IsReady) { BackendMenu(); return false; }
            if (!rtss.IsRunning)
            {
                try { if (!rtss.Start()) throw new Exception("RTSS process did not start."); }
                catch (Exception ex) { MessageBox.Show(T("RTSS failed to launch:\n", "اجرای RTSS ناموفق بود:\n") + ex.Message, T("RTSS offline", "RTSS غیرفعال")); return false; }
            }
            return true;
        }
        private string LocalizeWarning(string msg)
        {
            if (!Farsi) return msg;
            if (msg.StartsWith("This game is on")) return "این بازی در فهرست ضدتقلب قرار دارد. تنظیمات RTSS تغییر نمی‌کند.";
            if (msg.StartsWith("Anti-cheat process detected")) return "فرایند ضدتقلب فعال است: " + msg + " — ابتدا بازی محافظت‌شده را ببندید.";
            return "حالت عمومی ممکن است بر بازی‌های محافظت‌شده اثر بگذارد. از محدودکننده خارجی در این بازی‌ها استفاده نکنید.";
        }
        bool Apply(bool hotkey)
        {
            int limit = (int)fps.Value;
            string warning = Guard.Check(selectedTarget);
            if (warning.Length != 0 && (selectedTarget != "Global" || warning.StartsWith("Anti-cheat process", StringComparison.Ordinal)))
            {
                status.ForeColor = Color.FromArgb(255, 133, 128);
                status.Text = LocalizeWarning(warning);
                if (!hotkey) MessageBox.Show(status.Text, T("Anti-cheat safety", "ایمنی ضدتقلب"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (selectedTarget == "Global" && !globalConsent)
            {
                if (hotkey)
                {
                    status.Text = T("Open PacePilot and confirm global RTSS control before using global hotkeys.", "برای میانبرهای عمومی ابتدا کنترل RTSS را در PacePilot تأیید کنید.");
                    return false;
                }
                if (MessageBox.Show(T("Global limits also affect protected online games. Avoid using RTSS with anti-cheat-enabled titles.\n\nApply global limit?", "محدودیت عمومی روی بازی‌های آنلاین محافظت‌شده هم اثر می‌گذارد. با بازی‌های دارای ضدتقلب از RTSS استفاده نکنید.\n\nمحدودیت عمومی اعمال شود؟"), T("Global profile warning", "هشدار پروفایل عمومی"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return false;
                globalConsent = true;
            }
            if (!EnsureReady()) return false;
            try
            {
                rtss.ApplyLimit(selectedTarget, limit);
                if (selectedTarget == "Global") settings.GlobalLimit = limit;
                else
                {
                    GameProfile p = settings.Games.FirstOrDefault(x => String.Equals(x.Exe, selectedTarget, StringComparison.OrdinalIgnoreCase));
                    if (p != null) p.Limit = limit;
                }
                lastAppliedFps = limit;
                if (limit > 0) restoredFps = limit;
                trend.Limit = limit > 0 ? limit : 60;
                UpdateSelectionUI();
                Save();
                status.ForeColor = accent;
                status.Text = limit == 0 ? T("RTSS limit disabled and verified  ·  ", "غیرفعال‌شدن محدودیت RTSS تأیید شد  ·  ") + selectedTarget
                    : T("RTSS confirmed: ", "محدودیت RTSS تأیید شد: ") + selectedTarget + "  ·  " + limit + " FPS";
                return true;
            }
            catch (Exception ex)
            {
                status.ForeColor = Color.FromArgb(255, 133, 128);
                status.Text = T("Apply failed: ", "خطا در اعمال محدودیت: ") + ex.Message;
                if (!hotkey) MessageBox.Show(status.Text, T("RTSS error", "خطای RTSS"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        void Toggle()
        {
            int oldValue = (int)fps.Value;
            int nextValue = oldValue == 0 ? Math.Max(1, Math.Min(1000, restoredFps)) : 0;
            if (oldValue > 0) restoredFps = oldValue;
            fps.Value = nextValue;
            if (!Apply(true))
            {
                string failure = status.Text;
                fps.Value = oldValue;
                status.ForeColor = Color.FromArgb(255, 133, 128);
                status.Text = failure;
            }
        }
        void Adjust(int delta)
        {
            int oldValue = (int)fps.Value;
            fps.Value = Math.Max(1, Math.Min(1000, (oldValue == 0 ? restoredFps : oldValue) + delta));
            if (!Apply(true))
            {
                string failure = status.Text;
                fps.Value = oldValue;
                status.ForeColor = Color.FromArgb(255, 133, 128);
                status.Text = failure;
            }
        }
        void Tick()
        {
            if (settings.AutoSelectKnownGames)
            {
                try
                {
                    uint frontPid;
                    WinApi.GetWindowThreadProcessId(WinApi.GetForegroundWindow(), out frontPid);
                    if (frontPid != 0 && frontPid != Process.GetCurrentProcess().Id)
                    {
                        using (Process front = Process.GetProcessById((int)frontPid))
                        {
                            string frontExe = front.ProcessName + ".exe";
                            if (!String.Equals(frontExe, selectedTarget, StringComparison.OrdinalIgnoreCase)
                                && settings.Games.Any(x => String.Equals(x.Exe, frontExe, StringComparison.OrdinalIgnoreCase)))
                                targetSelect(frontExe);
                        }
                    }
                }
                catch { /* foreground window may disappear */ }
            }
            backend.Text = rtss.IsReady ? rtss.IsRunning ? T("● RTSS running", "● RTSS فعال است") : T("◌ RTSS is stopped", "◌ RTSS متوقف است") : T("○ RTSS not found", "○ RTSS نصب نیست");
            backend.ForeColor = rtss.IsRunning ? accent : muted;
            FrameSample sample = rtss.IsRunning ? LiveFrames.Read(selectedTarget) : new FrameSample();
            if (sample.Valid)
            {
                liveFps.Text = sample.Fps.ToString("0.0");
                liveFrame.Text = sample.FrameMs.ToString("0.00") + " ms";
                trend.Add(sample.FrameMs);
            }
            else { liveFps.Text = "--"; liveFrame.Text = "-- ms"; trend.Add(float.NaN); }
        }
        void Save()
        {
            if (testMode) return;
            try { Store.Save(settings); }
            catch (Exception ex) { if (status != null) status.Text = T("Settings not saved: ", "تنظیمات ذخیره نشد: ") + ex.Message; }
        }
        void SetStartup(bool enabled, bool persist)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null) throw new Exception("Cannot open user startup registry key.");
                    if (enabled) key.SetValue("PacePilot", "\"" + Application.ExecutablePath + "\" --tray");
                    else key.DeleteValue("PacePilot", false);
                }
                if (persist) { settings.StartWithWindows = enabled; Save(); }
            }
            catch (Exception ex)
            {
                if (persist) { settings.StartWithWindows = false; status.Text = T("Startup setup failed: ", "خطا در اجرای خودکار: ") + ex.Message; }
            }
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (testMode) return;
            WinApi.RegisterHotKey(Handle, 0xC100, 0x0001 | 0x0004, (uint)Keys.L);
            WinApi.RegisterHotKey(Handle, 0xC101, 0x0001 | 0x0004, (uint)Keys.Up);
            WinApi.RegisterHotKey(Handle, 0xC102, 0x0001 | 0x0004, (uint)Keys.Down);
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            WinApi.UnregisterHotKey(Handle, 0xC100); WinApi.UnregisterHotKey(Handle, 0xC101); WinApi.UnregisterHotKey(Handle, 0xC102);
            base.OnHandleDestroyed(e);
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                switch (m.WParam.ToInt32())
                {
                    case 0xC100: Toggle(); break;
                    case 0xC101: Adjust(1); break;
                    case 0xC102: Adjust(-1); break;
                }
                return;
            }
            base.WndProc(ref m);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized && settings != null && settings.MinimizeToTray)
            { Hide(); ShowInTaskbar = false; }
        }
        // UI regression checks run on Windows without RTSS or network access.
        // The test mode never changes saved settings, the registry or the RTSS profiles.
        public void RunUiSelfCheck()
        {
            foreach (Size size in new Size[] { new Size(1000, 690), new Size(1280, 800), new Size(1920, 1080) })
            {
                Size = size;
                PerformLayout();
                Application.DoEvents();
                LayoutContent();
                Control side = ((TableLayoutPanel)Controls[0]).GetControlFromPosition(0, 0);
                if (side.Right > layoutBody.Left)
                    throw new InvalidOperationException("Sidebar overlaps main content at " + size);
                if (limiterCard.Right > surface.Width || metricsCard.Right > surface.Width || graphCard.Right > surface.Width)
                    throw new InvalidOperationException("A main card extends outside the content area at " + size);
                if (graphCard.Bottom > status.Top || status.Bottom > trayCheck.Top)
                    throw new InvalidOperationException("Graph or status/footer overlaps at " + size);
                if (fps.Right > limiterCard.Width || applyButton.Right > limiterCard.Width || toggleButton.Left < 0)
                    throw new InvalidOperationException("FPS controls extend outside limiter card at " + size);
                foreach (Button b in presetButtons.Values)
                {
                    if (b.Left < 0 || b.Right > limiterCard.Width)
                        throw new InvalidOperationException("FPS preset clipped at " + size);
                }
            }
            presetButtons[144].PerformClick();
            if (fps.Value != 144 || presetButtons[144].BackColor != accent || !status.Text.Contains("144 FPS"))
                throw new InvalidOperationException("FPS preset 144 selection/highlight failed.");
            presetButtons[30].PerformClick();
            if (fps.Value != 30 || presetButtons[30].BackColor != accent || presetButtons[144].BackColor == accent)
                throw new InvalidOperationException("FPS preset 30 selection/highlight failed.");
            fps.Value = 75;
            if (presetButtons.Values.Any(b => b.BackColor == accent) || !status.Text.Contains("75 FPS"))
                throw new InvalidOperationException("Custom FPS selection failed.");
            // Language regression: rebuild controls, confirm translations and selected FPS.
            ChangeLanguage("fa");
            if (languagePicker.SelectedIndex != 1 || !titleLabel.Text.Contains("فریم") ||
                targets.Items[0].ToString().IndexOf("عمومی", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Persian UI / RTL translation failed.");
            presetButtons[60].PerformClick();
            if (fps.Value != 60 || presetButtons[60].BackColor != accent)
                throw new InvalidOperationException("FPS preset failed in Persian UI.");
            ChangeLanguage("en");
            if (languagePicker.SelectedIndex != 0 || !titleLabel.Text.Contains("smoother"))
                throw new InvalidOperationException("English UI switch failed.");
            // Restores only in-memory UI state; Apply was never invoked.
            fps.Value = lastAppliedFps;
            string sample = "[General]\r\nName=keep\r\n[Framerate]\r\nLimit=0\r\nLimitDenominator=100\r\n[OSD]\r\nEnable=1\r\n";
            string rewritten = RtssProfileFile.SetFrameLimit(sample, 30);
            if (!rewritten.Contains("Limit=30") || !rewritten.Contains("LimitDenominator=1") ||
                !rewritten.Contains("Name=keep") || !rewritten.Contains("Enable=1") ||
                RtssProfileFile.SetFrameLimit(rewritten, 30) != rewritten)
                throw new InvalidOperationException("RTSS profile file patch regression failed.");
            string created = RtssProfileFile.SetFrameLimit("[General]\nName=keep\n", 40);
            if (!created.Contains("[Framerate]\nLimit=40\nLimitDenominator=1"))
                throw new InvalidOperationException("RTSS missing-framerate-section regression failed.");
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!testMode && !actuallyClosing && settings.MinimizeToTray && e.CloseReason == CloseReason.UserClosing)
            { e.Cancel = true; Hide(); ShowInTaskbar = false; return; }
            if (ticker != null) { ticker.Stop(); ticker.Dispose(); }
            if (notify != null) { notify.Visible = false; notify.Dispose(); }
            base.OnFormClosing(e);
        }
    }

    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Limited profile repair runs elevated without acquiring the GUI singleton mutex.
            if(args.Length>0 && String.Equals(args[0],"--rtss-repair",StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode=ElevatedRtssProfileWriter.Execute(args);
                return;
            }
            // Read-only diagnostics are allowed even if an older GUI has the mutex.
            if (args.Any(x => String.Equals(x, "--diagnose-rtss", StringComparison.OrdinalIgnoreCase)))
            {
                string log = Path.Combine(Path.GetTempPath(), "PacePilot-rtss-diagnostic.txt");
                try
                {
                    AppSettings s = Store.Load();
                    RtssControl r = new RtssControl();
                    r.Initialize(RtssControl.FindFolder(s.RtssFolder));
                    File.WriteAllText(log, r.Diagnostics("Global"), Encoding.UTF8);
                }
                catch (Exception ex) { File.WriteAllText(log, "DIAGNOSTIC ERROR: " + ex, Encoding.UTF8); Environment.ExitCode = 1; }
                return;
            }
            bool isNew;
            using (Mutex mutex = new Mutex(true, "Local\\PacePilot_Windows_GUI", out isNew))
            {
                if (!isNew) { MessageBox.Show("Another PacePilot version is already running. Right-click its tray icon and choose Exit, then launch v1.3 again.\nBuilding the new EXE is safe even when the old version is open.", "PacePilot already open"); return; }
                try { WinApi.SetProcessDPIAware(); } catch { }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                bool tray = args.Any(x => String.Equals(x, "--tray", StringComparison.OrdinalIgnoreCase));
                if (args.Any(x => String.Equals(x, "--selftest", StringComparison.OrdinalIgnoreCase)))
                {
                    string log = Path.Combine(Path.GetTempPath(), "PacePilot-selftest.log");
                    try
                    {
                        using (StudioForm test = new StudioForm(false, true))
                        {
                            test.Opacity = 0;
                            test.Show();
                            Application.DoEvents();
                            test.SelfTest();
                            test.Close();
                        }
                        File.WriteAllText(log, "PASS: Six themes, English/Persian, resizing, FPS selection, RTSS profile patch regression.\r\nBackend RTSS/winget installation requires an interactive Windows test.\r\n");
                        Environment.ExitCode = 0;
                    }
                    catch (Exception ex)
                    {
                        File.WriteAllText(log, "FAIL: " + ex.ToString());
                        Environment.ExitCode = 1;
                    }
                }
                else Application.Run(new StudioForm(tray, false));
                GC.KeepAlive(mutex);
            }
        }
    }
}
