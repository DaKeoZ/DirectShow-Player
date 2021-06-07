using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace fr.ipmfrance.webcam.com
{
    [ComVisible(false)]
    [StructLayout(LayoutKind.Sequential)]
    public class DsLong
    {
        private long Value;
        public DsLong(long Value)
        {
            this.Value = Value;
        }

        public override string ToString()
        {
            return this.Value.ToString();
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public static implicit operator long(DsLong l)
        {
            return l.Value;
        }

        public static implicit operator DsLong(long l)
        {
            return new DsLong(l);
        }

        public long ToInt64()
        {
            return this.Value;
        }

        public static DsLong FromInt64(long l)
        {
            return new DsLong(l);
        }
    }
}
