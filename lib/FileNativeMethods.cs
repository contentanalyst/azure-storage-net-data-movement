﻿//------------------------------------------------------------------------------
// <copyright file="FileNativeMethods.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.DataMovement.Interop
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Win32.SafeHandles;

    internal static partial class NativeMethods
    {
        public const int ERROR_SUCCESS = 0;
        public const int ERROR_FILE_NOT_FOUND = 2;
        public const int ERROR_DIRECTORY_NOT_FOUND = 3;
        public const int ERROR_NO_MORE_FILES = 18;
        public const int ERROR_ALREADY_EXISTS = 183;
        public const int ERROR_HANDLE_EOF = 38;

        [System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
        public struct OFSTRUCT
        {
            public byte cBytes;
            public byte fFixedDisc;
            public UInt16 nErrCode;
            public UInt16 Reserved1;
            public UInt16 Reserved2;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPathName;
        }

        // Open or create file
        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeFileHandle CreateFileW(
             [MarshalAs(UnmanagedType.LPWStr)] string filename,
             [MarshalAs(UnmanagedType.U4)] FileAccess access,
             [MarshalAs(UnmanagedType.U4)] FileShare share,
             IntPtr securityAttributes,
             [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
             [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
             IntPtr templateFile);

        // Create directory
        [DllImport("kernel32.dll", EntryPoint = "CreateDirectoryW", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateDirectoryW([MarshalAs(UnmanagedType.LPWStr)] string lpPathName, IntPtr lpSecurityAttributes);

#if !DOTNET5_4
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadFile(SafeFileHandle hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, ref NativeOverlapped template);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteFile(SafeFileHandle hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, ref NativeOverlapped template);
#endif

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetEndOfFile(SafeFileHandle hFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetFileSizeEx(SafeFileHandle hFile, out long lpFileSize);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int SetFilePointer(SafeFileHandle handle, int lDistanceToMove, out int lpDistanceToMoveHigh, uint dwMoveMethod);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern uint GetFullPathNameW(
            string lpFileName,
            uint nBufferLength,
            [Out] StringBuilder lpBuffer,
            [Out] StringBuilder lpFilePart);

        [DllImport("kernel32.dll", EntryPoint = "FindFirstFileW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeFindHandle FindFirstFileW(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", EntryPoint = "FindNextFileW", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FindNextFileW(SafeFindHandle hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("shlwapi.dll", EntryPoint = "PathFileExistsW", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PathFileExistsW([MarshalAs(UnmanagedType.LPWStr)]string pszPath);

        [DllImport("kernel32.dll", EntryPoint = "GetFileAttributesW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint GetFileAttributesW(string lpFileName);

        public static long Seek(SafeFileHandle handle, long offset, SeekOrigin origin)
        {
            uint moveMethod = 0;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    moveMethod = 0;
                    break;

                case SeekOrigin.Current:
                    moveMethod = 1;
                    break;

                case SeekOrigin.End:
                    moveMethod = 2;
                    break;
            }

            int lo = (int)(offset & 0xffffffff);
            int hi = (int)(offset >> 32);

            lo = SetFilePointer(handle, lo, out hi, moveMethod);

            if (lo == -1)
            {
                Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            return (((long)hi << 32) | (uint)lo);
        }

#if !DOTNET5_4

        /// <summary>
        /// Throw exception if last Win32 error is not zero.
        /// </summary>
        public static void ThrowExceptionForLastWin32ErrorIfExists()
        {
            ThrowExceptionForLastWin32ErrorIfExists(new int[] {
                ERROR_SUCCESS
            });
        }

        /// <summary>
        /// Throw exception if last Win32 error is not expected.
        /// </summary>
        /// <param name="expectErrorCodes">Error codes that are expected.</param>
        public static void ThrowExceptionForLastWin32ErrorIfExists(int[] expectErrorCodes)
        {
            int errorCode = Marshal.GetLastWin32Error();

            if (expectErrorCodes != null
                && expectErrorCodes.Contains(errorCode))
            {
                return;
            }
            throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        /// <summary>
        /// Throw exception if the Win32 error given is not expected.
        /// </summary>
        /// <param name="errorCode">Win32 error code want to check.</param>
        /// <param name="expectErrorCodes">Error codes that are expected.</param>
        public static void ThrowExceptionForLastWin32ErrorIfExists(int errorCode, int[] expectErrorCodes)
        {
            if (expectErrorCodes != null
                && expectErrorCodes.Contains(errorCode))
            {
                return;
            }
            throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
#endif
    }
}
