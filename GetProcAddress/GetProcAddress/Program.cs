﻿using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace GetProcAddress
{
    internal class Program
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        static extern bool NtReadVirtualMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead
        );
        
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)] static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);
        // [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
        // [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)] static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        static IntPtr CustomGetProcAddress(IntPtr pDosHdr, String func_name)
        {
            // One offset changes between 32 and 64-bit processes
            int exportrva_offset = 136;
            if (IntPtr.Size == 4)
            {
                exportrva_offset = 120;
            }

            // Current process handle
            IntPtr hProcess = Process.GetCurrentProcess().Handle;

            // DOS header(IMAGE_DOS_HEADER)->e_lfanew
            IntPtr e_lfanew_addr = pDosHdr + (int)0x3C;
            byte[] e_lfanew_bytearr = new byte[4];
            NtReadVirtualMemory(hProcess, e_lfanew_addr, e_lfanew_bytearr, e_lfanew_bytearr.Length, out _);
            ulong e_lfanew_value = BitConverter.ToUInt32(e_lfanew_bytearr, 0);
            Console.WriteLine("[*] e_lfanew: \t\t\t\t\t0x" + (e_lfanew_value).ToString("X"));

            // NT Header (IMAGE_NT_HEADERS)->FileHeader(IMAGE_FILE_HEADER)->SizeOfOptionalHeader
            IntPtr sizeopthdr_addr = pDosHdr + (int)e_lfanew_value + 20;
            byte[] sizeopthdr_bytearr = new byte[2];
            NtReadVirtualMemory(hProcess, sizeopthdr_addr, sizeopthdr_bytearr, sizeopthdr_bytearr.Length, out _);
            ulong sizeopthdr_value = BitConverter.ToUInt16(sizeopthdr_bytearr, 0);
            Console.WriteLine("[*] SizeOfOptionalHeader: \t\t\t0x" + (sizeopthdr_value).ToString("X"));
            int numberDataDirectory = ((int)sizeopthdr_value / 16) - 1;

            // exportTableRVA: Optional Header(IMAGE_OPTIONAL_HEADER64)->DataDirectory(IMAGE_DATA_DIRECTORY)[0]->VirtualAddress
            IntPtr exportTableRVA_addr = pDosHdr + (int)e_lfanew_value + exportrva_offset;
            byte[] exportTableRVA_bytearr = new byte[4];
            NtReadVirtualMemory(hProcess, exportTableRVA_addr, exportTableRVA_bytearr, exportTableRVA_bytearr.Length, out _);
            ulong exportTableRVA_value = BitConverter.ToUInt32(exportTableRVA_bytearr, 0);
            Console.WriteLine("[*] exportTableRVA address: \t\t\t0x" + (exportTableRVA_addr).ToString("X"));
            
            if (exportTableRVA_value != 0)
            {
                // NumberOfNames: ExportTable(IMAGE_EXPORT_DIRECTORY)->NumberOfNames
                IntPtr numberOfNames_addr = pDosHdr + (int)exportTableRVA_value + 0x18;
                byte[] numberOfNames_bytearr = new byte[4];
                NtReadVirtualMemory(hProcess, numberOfNames_addr, numberOfNames_bytearr, numberOfNames_bytearr.Length, out _);
                int numberOfNames_value = (int)BitConverter.ToUInt32(numberOfNames_bytearr, 0);
                Console.WriteLine("[*] numberOfNames: \t\t\t\t0x" + (numberOfNames_value).ToString("X"));

                // AddressOfFunctions: ExportTable(IMAGE_EXPORT_DIRECTORY)->AddressOfFunctions
                IntPtr addressOfFunctionsVRA_addr = pDosHdr + (int)exportTableRVA_value + 0x1C;
                byte[] addressOfFunctionsVRA_bytearr = new byte[4];
                NtReadVirtualMemory(hProcess, addressOfFunctionsVRA_addr, addressOfFunctionsVRA_bytearr, addressOfFunctionsVRA_bytearr.Length, out _);
                ulong addressOfFunctionsVRA_value = BitConverter.ToUInt32(addressOfFunctionsVRA_bytearr, 0);
                Console.WriteLine("[*] addressOfFunctionsVRA: \t\t\t0x" + (addressOfFunctionsVRA_value).ToString("X"));

                // AddressOfNames: ExportTable(IMAGE_EXPORT_DIRECTORY)->AddressOfNames
                IntPtr addressOfNamesVRA_addr = pDosHdr + (int)exportTableRVA_value + 0x20;
                byte[] addressOfNamesVRA_bytearr = new byte[4];
                NtReadVirtualMemory(hProcess, addressOfNamesVRA_addr, addressOfNamesVRA_bytearr, addressOfNamesVRA_bytearr.Length, out _);
                ulong addressOfNamesVRA_value = BitConverter.ToUInt32(addressOfNamesVRA_bytearr, 0);
                Console.WriteLine("[*] addressOfNamesVRA: \t\t\t\t0x" + (addressOfNamesVRA_value).ToString("X"));

                // AddressOfNameOrdinals: ExportTable(IMAGE_EXPORT_DIRECTORY)->AddressOfNameOrdinals
                IntPtr addressOfNameOrdinalsVRA_addr = pDosHdr + (int)exportTableRVA_value + 0x24;
                byte[] addressOfNameOrdinalsVRA_bytearr = new byte[4];
                NtReadVirtualMemory(hProcess, addressOfNameOrdinalsVRA_addr, addressOfNameOrdinalsVRA_bytearr, addressOfNameOrdinalsVRA_bytearr.Length, out _);
                ulong addressOfNameOrdinalsVRA_value = BitConverter.ToUInt32(addressOfNameOrdinalsVRA_bytearr, 0);
                Console.WriteLine("[*] addressOfNameOrdinalsVRA: \t\t\t0x" + (addressOfNameOrdinalsVRA_value).ToString("X"));

                IntPtr addressOfFunctionsRA = IntPtr.Add(pDosHdr, (int)addressOfFunctionsVRA_value);
                IntPtr addressOfNamesRA = IntPtr.Add(pDosHdr, (int)addressOfNamesVRA_value);
                IntPtr addressOfNameOrdinalsRA = IntPtr.Add(pDosHdr, (int)addressOfNameOrdinalsVRA_value);

                IntPtr auxaddressOfNamesRA = addressOfNamesRA;
                IntPtr auxaddressOfNameOrdinalsRA = addressOfNameOrdinalsRA;
                IntPtr auxaddressOfFunctionsRA = addressOfFunctionsRA;

                for (int i = 0; i < numberOfNames_value; i++)
                {
                    byte[] data5 = new byte[Marshal.SizeOf(typeof(UInt32))];
                    NtReadVirtualMemory(hProcess, auxaddressOfNamesRA, data5, data5.Length, out _);
                    UInt32 functionAddressVRA = (UInt32) BitConverter.ToUInt32(data5, 0);
                    IntPtr functionAddressRA = IntPtr.Add(pDosHdr, (int)functionAddressVRA);
                    byte[] data6 = new byte[func_name.Length];
                    NtReadVirtualMemory(hProcess, functionAddressRA, data6, data6.Length, out _);
                    String functionName = Encoding.ASCII.GetString(data6);
                    if (functionName == func_name)
                    {
                        // AdddressofNames --> AddressOfNamesOrdinals
                        byte[] data7 = new byte[Marshal.SizeOf(typeof(UInt16))];
                        NtReadVirtualMemory(hProcess, auxaddressOfNameOrdinalsRA, data7, data7.Length, out _);
                        UInt16 ordinal = (UInt16)BitConverter.ToUInt16(data7, 0);
                        // AddressOfNamesOrdinals --> AddressOfFunctions
                        auxaddressOfFunctionsRA += 4 * ordinal;
                        byte[] data8 = new byte[Marshal.SizeOf(typeof(UInt32))];
                        NtReadVirtualMemory(hProcess, auxaddressOfFunctionsRA, data8, data8.Length, out _);
                        UInt32 auxaddressOfFunctionsRAVal = (UInt32)BitConverter.ToUInt32(data8, 0);
                        IntPtr functionAddress = IntPtr.Add(pDosHdr, (int)auxaddressOfFunctionsRAVal);
                        return functionAddress;
                    }
                    auxaddressOfNamesRA += 4;
                    auxaddressOfNameOrdinalsRA += 2;
                }
            }
            return IntPtr.Zero;
        }


        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("[-] Usage: GetProcAddress.exe DLL_NAME FUNCTION_NAME");
                System.Environment.Exit(0);
            }
            string dll_name = args[0];
            string func_name = args[1];

            IntPtr dll_handle = LoadLibrary(dll_name); // Alternative: IntPtr dll_handle = GetModuleHandle(dll_name);
            IntPtr func_address = CustomGetProcAddress(dll_handle, func_name);

            if (func_address == IntPtr.Zero)
            {
                Console.WriteLine("[-] Function name or DLL not found");
            }
            else
            {
                Console.WriteLine("[+] RESULT: \t\t\t\t\t0x" + func_address.ToString("X"));
                // Console.WriteLine("[+] Address of {0} ({1}): 0x{2} [GetProcAddress]", func_name, dll_name, GetProcAddress(LoadLibrary(dll_name), func_name).ToString("X"));
            }
        }
    }
}