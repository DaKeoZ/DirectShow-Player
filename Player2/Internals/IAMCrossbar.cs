// AForge Direct Show Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2009-2012
// contacts@aforgenet.com
//

using System;
using System.Runtime.InteropServices;

namespace fr.ipmfrance.webcam.com
{
    /// <summary>
    /// The IAMCrossbar interface routes signals from an analog or digital source to a video capture filter.
    /// </summary>
    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid( "C6E13380-30AC-11D0-A18C-00A0C9118956" ),
    InterfaceType( ComInterfaceType.InterfaceIsIUnknown )]
    internal interface IAMCrossbar
    {
        [PreserveSig]
        int get_PinCounts( [Out] out int outputPinCount, [Out] out int inputPinCount );
   
        [PreserveSig]
        int CanRoute( [In] int outputPinIndex, [In] int inputPinIndex );
        
        [PreserveSig]
        int Route( [In] int outputPinIndex, [In] int inputPinIndex );
        
        [PreserveSig]
        int get_IsRoutedTo( [In] int outputPinIndex, [Out] out int inputPinIndex );
        
        [PreserveSig]
        int get_CrossbarPinInfo(
            [In, MarshalAs( UnmanagedType.Bool )] bool isInputPin,
            [In] int pinIndex,
            [Out] out int pinIndexRelated,
            [Out] out PhysicalConnectorType physicalType );
    }
}
