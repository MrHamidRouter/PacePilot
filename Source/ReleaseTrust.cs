// PacePilot release transparency tools. Hash is for integrity comparison, not malware detection.
// Real Windows publisher reputation requires a publicly trusted Authenticode certificate.
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace PacePilot
{
    public static class ReleaseTrust
    {
        public static string Sha256File(string file)
        {
            if(String.IsNullOrWhiteSpace(file)||!File.Exists(file))
                throw new FileNotFoundException("EXE not found",file);
            using(FileStream fs=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read))
            using(SHA256 algorithm=SHA256.Create())
            {
                byte[] digest=algorithm.ComputeHash(fs);
                StringBuilder hex=new StringBuilder(digest.Length*2);
                foreach(byte b in digest)hex.Append(b.ToString("x2"));
                return hex.ToString();
            }
        }
        public static string EmbeddedSignatureInfo(string file)
        {
            try
            {
                using(X509Certificate2 certificate=new X509Certificate2(
                    X509Certificate.CreateFromSignedFile(file)))
                {
                    // Reading a certificate DOES NOT prove a valid/trusted signature.
                    return "Embedded certificate: "+certificate.Subject+
                        " (signature trust not verified)";
                }
            }
            catch(CryptographicException){return "Unsigned (no Authenticode publisher certificate)";}
            catch(ArgumentException){return "Unsigned (no Authenticode publisher certificate)";}
            catch(Exception e){return "Signature status unavailable: "+e.GetType().Name;}
        }
        public static bool SelfTest()
        {
            string tmp=Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(tmp,new byte[]{0x61,0x62,0x63});
                return Sha256File(tmp)=="ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
            }
            finally{try{File.Delete(tmp);}catch{}}
        }
    }
}
