using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace fr.ipmfrance.webcam.com
{
    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("670d1d20-a068-11d0-b3f0-00aa003761c5"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAMCopyCaptureFileProgress
    {
        [PreserveSig]
        int Progress(int iProgress);
    }
}
