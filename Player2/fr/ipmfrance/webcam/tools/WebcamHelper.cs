using fr.ipmfrance.webcam.com;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;


namespace fr.ipmfrance.webcam.tools
{
    public class WebcamHelper
    {

        private static List<FilterInfo> devices;
        private static FilterInfo theDevice;

        public static FilterInfo FindDevice()
        {
            devices = CollectFilters(FilterCategory.VideoInputDevice);

            devices.ForEach(delegate (FilterInfo filter)
            {
                if (filter.MonikerString.Contains("pnp"))
                {
                    theDevice = filter;

                }
            });
            return theDevice;
        }


        private static List<webcam.FilterInfo> CollectFilters(Guid category)
        {
            object comObj = null;
            ICreateDevEnum enumDev = null;
            IEnumMoniker enumMon = null;
            IMoniker[] devMon = new IMoniker[1];
            int hr;
            List<webcam.FilterInfo> result = new List<webcam.FilterInfo>();

            try
            {
                Type srvType = Type.GetTypeFromCLSID(Clsid.SystemDeviceEnum);
                if (srvType == null)
                {
                    Debug.WriteLine("Failed creating device enumerator");
                    throw new ApplicationException("Failed creating device enumerator");
                }

                comObj = Activator.CreateInstance(srvType);
                enumDev = (ICreateDevEnum)comObj;

                hr = enumDev.CreateClassEnumerator(ref category, out enumMon, 0);
                if (hr != 0)
                {

                    Debug.WriteLine("No devices of the category");
                    throw new ApplicationException("No devices of the category");
                }

                IntPtr n = IntPtr.Zero;
                while (true)
                {
                    hr = enumMon.Next(1, devMon, n);
                    if ((hr != 0) || (devMon[0] == null))
                        break;

                    webcam.FilterInfo filter = new webcam.FilterInfo(devMon[0]);
                    result.Add(filter);

                    Marshal.ReleaseComObject(devMon[0]);
                    devMon[0] = null;
                }

                result.Sort();
            }
            catch
            {
            }
            finally
            {
                enumDev = null;
                if (comObj != null)
                {
                    Marshal.ReleaseComObject(comObj);
                    comObj = null;
                }
                if (enumMon != null)
                {
                    Marshal.ReleaseComObject(enumMon);
                    enumMon = null;
                }
                if (devMon[0] != null)
                {
                    Marshal.ReleaseComObject(devMon[0]);
                    devMon[0] = null;
                }
            }
            return result;
        }

    }
}
