using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace fr.ipmfrance.webcam.com
{
    [ComVisible(false)]
    [StructLayout(LayoutKind.Explicit)]
    public class DsGuid
    {
        public static readonly DsGuid Empty = Guid.Empty;
        [FieldOffset(0)]
        private Guid guid;

        public DsGuid()
        {
            this.guid = Guid.Empty;
        }

        public DsGuid(Guid g)
        {
            this.guid = g;
        }

        public DsGuid(string g)
        {
            this.guid = new Guid(g);
        }

        public static DsGuid FromGuid(Guid g)
        {
            return new DsGuid(g);
        }

        public override int GetHashCode()
        {
            return this.guid.GetHashCode();
        }

        public static implicit operator Guid(DsGuid g)
        {
            return g.guid;
        }

        public static implicit operator DsGuid(Guid g)
        {
            return new DsGuid(g);
        }

        public Guid ToGuid()
        {
            return this.guid;
        }

        public override string ToString()
        {
            return this.guid.ToString();
        }

        public string ToString(string format)
        {
            return this.guid.ToString(format);
        }
    }
}
