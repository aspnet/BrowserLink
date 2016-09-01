// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    internal static class NativeMethods
    {

        [DllImport("urlmon.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false)]
        internal static extern int FindMimeFromData(
            IntPtr pBC,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzUrl,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1, SizeParamIndex = 3)]
            byte[] pBuffer,
            int cbSize,
            [MarshalAs(UnmanagedType.LPWStr)]  string pwzMimeProposed,
            int dwMimeFlags,
            out IntPtr ppwzMimeOut,
            int dwReserved);


        [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern IntPtr OpenFileMapping(
            uint dwDesiredAccess,
            bool bInheritHandle,
            string lpName);

        [DllImport("kernel32.dll", SetLastError = false)]
        internal static extern bool CloseHandle(
            IntPtr hHandle);

    }
}