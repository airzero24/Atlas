#define DEFAULT_EKE

using System.Collections.Generic;

namespace Atlas
{
    public class Config
    {
        public static string[] Servers = { "http://127.0.0.1", "http://172.16.176.129" };
        public static string Domain = "";
        public static string PayloadUUID = "e2561744-cc94-4d33-b793-e42d1494c6e0";
        public static string UUID = "";
        public static string UserAgent = "Mozilla 5.0";
        public static string HostHeader = null;
        public static int Sleep = 5;
        public static int Jitter = 20;
        public static string KillDate = "2099-12-25";
#if (DEFAULT || DEFULAT_PSK || DEFAULT_EKE)
        public static string Url = "/api/v1.4/agent_message";
        public static string Param = "id";
#endif
#if (Default_PSK || DEFAULT_EKE)
        public static string Psk = "jPL6Hu5UZQpqLPkdPLCXOsOMbOHAjZudiCzjrGfS3PU=";
#endif
#if DEFAULT_EKE
        public static string SessionId = "";
        public static string tempUUID = "";
        public static System.Security.Cryptography.RSACryptoServiceProvider Rsa;
#endif
        public static Dictionary<string, string> Modules = new Dictionary<string, string>();
    }
}
