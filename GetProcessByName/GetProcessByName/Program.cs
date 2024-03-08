using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace GetProcessByName
{
    internal class Program
    {
        [DllImport("ntdll.dll")] static extern bool NtGetNextProcess(IntPtr handle, int MAX_ALLOWED, int param3, int param4, out IntPtr outHandle);
        [DllImport("psapi.dll")] static extern uint GetProcessImageFileName(IntPtr hProcess, [Out] StringBuilder lpImageFileName, [In][MarshalAs(UnmanagedType.U4)] int nSize);
        [DllImport("kernel32.dll")] static extern int GetProcessId(IntPtr handle);

        public static List<IntPtr> GetProcessByName(string proc_name)
        {
            IntPtr aux_handle = IntPtr.Zero;
            int MAXIMUM_ALLOWED = 0x02000000;
            List<IntPtr> handles_list = new List<IntPtr>();

            while (!NtGetNextProcess(aux_handle, MAXIMUM_ALLOWED, 0, 0, out aux_handle))
            {
                StringBuilder fileName = new StringBuilder(100);
                GetProcessImageFileName(aux_handle, fileName, 100);
                char[] stringArray = fileName.ToString().ToCharArray();
                Array.Reverse(stringArray);
                string reversedStr = new string(stringArray);
                int index = reversedStr.IndexOf("\\");
                if (index != -1)
                {
                    string res = reversedStr.Substring(0, index);
                    stringArray = res.ToString().ToCharArray();
                    Array.Reverse(stringArray);
                    res = new string(stringArray);
                    if (res == proc_name)
                    {
                        handles_list.Add(aux_handle);
                    }
                }
            }
            return handles_list;
        }

        static void Main(string[] args)
        {
            if (args.Length == 0) {
                Console.WriteLine("[+] Usage: GetProcessByName.exe <process>. Example: GetProcessByName.exe notepad.exe");
                System.Environment.Exit(0);
            }
            string proc_name = args[0];
            List<IntPtr> handles_list = GetProcessByName(proc_name);
            foreach (var proc_handle in handles_list)
            {
                int pid = GetProcessId(proc_handle);
                Console.WriteLine("[+] Handle: " + proc_handle + "  \tPID: {0}", pid);
            }
        }
    }
}