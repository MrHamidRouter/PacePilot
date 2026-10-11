// Official PacePilot release checks; no downloads or installs are performed automatically.
using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
namespace PacePilot
{
    [DataContract]
    public sealed class GitHubRelease
    {
        [DataMember(Name="tag_name")] public string Tag;
        [DataMember(Name="html_url")] public string Url;
        [DataMember(Name="prerelease")] public bool Prerelease;
        [DataMember(Name="draft")] public bool Draft;
    }
    public static class GitHubUpdates
    {
        public const string Api="https://api.github.com/repos/MrHamidRouter/PacePilot/releases/latest";
        public static readonly Version Current=new Version(2,4,1);
        public static GitHubRelease Check()
        {
            using(WebClient client=new WebClient())
            {
                client.Headers.Add(HttpRequestHeader.UserAgent,"PacePilot/2.4.1");
                client.Headers.Add(HttpRequestHeader.Accept,"application/vnd.github+json");
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                string json=client.DownloadString(Api);
                using(MemoryStream input=new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var decoder=new DataContractJsonSerializer(typeof(GitHubRelease));
                    return (GitHubRelease)decoder.ReadObject(input);
                }
            }
        }
        public static bool IsNewer(GitHubRelease release)
        {
            if(release==null||release.Draft||release.Prerelease||String.IsNullOrWhiteSpace(release.Tag))return false;
            Version version;
            if(!Version.TryParse(release.Tag.Trim().TrimStart('v','V'),out version))return false;
            return version.CompareTo(Current)>0&&
                !String.IsNullOrWhiteSpace(release.Url)&&
                release.Url.StartsWith("https://github.com/MrHamidRouter/PacePilot/releases/",StringComparison.OrdinalIgnoreCase);
        }
        public static bool SelfTest()
        {
            return IsNewer(new GitHubRelease{Tag="v2.4.2",Url="https://github.com/MrHamidRouter/PacePilot/releases/tag/v2.4.2"}) &&
              !IsNewer(new GitHubRelease{Tag="v2.0.2",Url="https://github.com/MrHamidRouter/PacePilot/releases/tag/v2.0.2"}) &&
              !IsNewer(new GitHubRelease{Tag="v2.4.0",Url="https://untrusted.example/releases/tag/v2.4.0"});
        }
    }
}