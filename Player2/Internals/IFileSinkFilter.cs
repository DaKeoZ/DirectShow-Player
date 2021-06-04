using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace fr.ipmfrance.webcam.com
{
    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("a2104830-7c70-11cf-8bce-00aa00a3f1a6"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFileSinkFilter
    {
        [PreserveSig]
        int SetFileName(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
            [In, MarshalAs(UnmanagedType.LPStruct)] AMMediaType pmt
            );

        [PreserveSig]
        int GetCurFile(
            [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszFileName,
            [Out, MarshalAs(UnmanagedType.LPStruct)] AMMediaType pmt
            );
    }
}
