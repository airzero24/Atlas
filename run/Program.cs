using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace run
{
    class Program
    {
        public static string ExecuteShellCommand(int PPID, bool BlockDLLs, string Command)
        {
            var saHandles = new SECURITY_ATTRIBUTES();
            saHandles.nLength = Marshal.SizeOf(saHandles);
            saHandles.bInheritHandle = true;
            saHandles.lpSecurityDescriptor = IntPtr.Zero;

            IntPtr hStdOutRead;
            IntPtr hStdOutWrite;
            IntPtr hDupStdOutWrite = IntPtr.Zero;

            CreatePipe(
                out hStdOutRead,
                out hStdOutWrite,
                ref saHandles,
                0);

            SetHandleInformation(
                hStdOutRead,
                HANDLE_FLAGS.INHERIT,
                0);

            var pInfo = new PROCESS_INFORMATION();
            var siEx = new STARTUPINFOEX();

            siEx.StartupInfo.cb = Marshal.SizeOf(siEx);
            siEx.StartupInfo.hStdErr = hStdOutWrite;
            siEx.StartupInfo.hStdOutput = hStdOutWrite;

            string result = string.Empty;

            try
            {
                var lpSize = IntPtr.Zero;
                if (BlockDLLs)
                {
                    InitializeProcThreadAttributeList(
                        IntPtr.Zero,
                        2,
                        0,
                        ref lpSize);

                    siEx.lpAttributeList = Marshal.AllocHGlobal(lpSize);

                    InitializeProcThreadAttributeList(
                        siEx.lpAttributeList,
                        2,
                        0,
                        ref lpSize);

                    var lpMitigationPolicy = Marshal.AllocHGlobal(IntPtr.Size);
                    Marshal.WriteInt64(lpMitigationPolicy, (long)BINARY_SIGNATURE_POLICY.PROCESS_CREATION_MITIGATION_POLICY_BLOCK_NON_MICROSOFT_BINARIES_ALWAYS_ON);

                    UpdateProcThreadAttribute(
                        siEx.lpAttributeList,
                        0,
                        0x20007,
                        lpMitigationPolicy,
                        (IntPtr)IntPtr.Size,
                        IntPtr.Zero,
                        IntPtr.Zero);
                }
                else
                {
                    InitializeProcThreadAttributeList(
                        IntPtr.Zero,
                        1,
                        0,
                        ref lpSize);

                    siEx.lpAttributeList = Marshal.AllocHGlobal(lpSize);

                    InitializeProcThreadAttributeList(
                        siEx.lpAttributeList,
                        1,
                        0,
                        ref lpSize);
                }

                var parentHandle = OpenProcess(
                    0x0080 | 0x0040,
                    false,
                    PPID);

                var lpParentProcess = Marshal.AllocHGlobal(IntPtr.Size);
                Marshal.WriteIntPtr(lpParentProcess, parentHandle);

                UpdateProcThreadAttribute(
                    siEx.lpAttributeList,
                    0,
                    0x00020000,
                    lpParentProcess,
                    (IntPtr)IntPtr.Size,
                    IntPtr.Zero,
                    IntPtr.Zero);

                var hCurrent = Process.GetCurrentProcess().Handle;

                DuplicateHandle(
                    hCurrent,
                    hStdOutWrite,
                    parentHandle,
                    ref hDupStdOutWrite,
                    0,
                    true,
                    0x00000001 | 0x00000002);

                siEx.StartupInfo.hStdErr = hDupStdOutWrite;
                siEx.StartupInfo.hStdOutput = hDupStdOutWrite;

                siEx.StartupInfo.dwFlags = 0x00000001 | 0x00000100;
                siEx.StartupInfo.wShowWindow = 0;

                var ps = new SECURITY_ATTRIBUTES();
                var ts = new SECURITY_ATTRIBUTES();
                ps.nLength = Marshal.SizeOf(ps);
                ts.nLength = Marshal.SizeOf(ts);

                CreateProcess(
                    null,
                    Command,
                    ref ps,
                    ref ts,
                    true,
                    CREATION_FLAGS.CREATE_NO_WINDOW | CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT,
                    IntPtr.Zero,
                    null,
                    ref siEx,
                    out pInfo);

                var safeHandle = new SafeFileHandle(hStdOutRead, false);
                var encoding = Encoding.GetEncoding(GetConsoleOutputCP());
                var reader = new StreamReader(new FileStream(safeHandle, FileAccess.Read, 4096, false), encoding, true);

                var exit = false;

                try
                {
                    do
                    {
                        if (WaitForSingleObject(pInfo.hProcess, 100) == 0)
                            exit = true;

                        char[] buf = null;
                        int bytesRead;
                        uint bytesToRead = 0;

                        var peekRet = PeekNamedPipe(
                            hStdOutRead,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            ref bytesToRead,
                            IntPtr.Zero);

                        if (peekRet == true && bytesToRead == 0)
                            if (exit == true)
                                break;
                            else
                                continue;

                        if (bytesToRead > 4096)
                            bytesToRead = 4096;

                        buf = new char[bytesToRead];
                        bytesRead = reader.Read(buf, 0, buf.Length);
                        if (bytesRead > 0)
                            result += new string(buf);

                    } while (true);
                    reader.Close();
                }
                catch { }
                finally
                {
                    safeHandle.Close();
                }

                CloseHandle(hStdOutRead);
            }
            catch { }
            finally
            {
                DeleteProcThreadAttributeList(siEx.lpAttributeList);
                Marshal.FreeHGlobal(siEx.lpAttributeList);
                CloseHandle(pInfo.hProcess);
                CloseHandle(pInfo.hThread);
            }

            return result;
        }

        [DllImport("kernel32.dll")]
        public static extern bool CreatePipe(out IntPtr hReadPipe, out IntPtr hWritePipe, ref SECURITY_ATTRIBUTES lpPipeAttributes, uint nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, Int32 Attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DeleteProcThreadAttributeList(IntPtr lpAttributeList);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern Int32 WaitForSingleObject(IntPtr Handle, uint Wait);

        [DllImport("kernel32.dll")]
        public static extern bool PeekNamedPipe(IntPtr hNamedPipe, IntPtr lpBuffer, IntPtr nBufferSize, IntPtr lpBytesRead, ref uint lpTotalBytesAvail, IntPtr lpBytesLeftThisMessage);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetHandleInformation(IntPtr hObject, HANDLE_FLAGS dwMask, HANDLE_FLAGS dwFlags);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(Int32 processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, ref IntPtr lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll")]
        public static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, CREATION_FLAGS dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int GetConsoleOutputCP();

        [Flags]
        public enum BINARY_SIGNATURE_POLICY : ulong
        {
            PROCESS_CREATION_MITIGATION_POLICY_BLOCK_NON_MICROSOFT_BINARIES_ALWAYS_ON = 0x100000000000
        }

        [Flags]
        public enum HANDLE_FLAGS : uint
        {
            None = 0,
            INHERIT = 1,
            PROTECT_FROM_CLOSE = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttributes;
            public uint dwFlags;
            public ushort wShowWindow;
            public ushort cbReserved;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdErr;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [Flags]
        public enum CREATION_FLAGS
        {
            CREATE_NO_WINDOW = 0x08000000,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000
        }

        public static void Main(string[] args)
        {
            try
            {
                string result = ExecuteShellCommand(Int32.Parse(args[0]), true, string.Join(" ", args.Skip(1).ToArray()));
                Console.WriteLine(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
