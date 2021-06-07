using System;

namespace fr.ipmfrance.win32
{
    public static class ComFactory
    {
        public static object Create(Guid guid)
        {
            Type type = Type.GetTypeFromCLSID(guid);
            if (type == null)
            {
                throw new ApplicationException("Echec à la création de " + guid.ToString());
            }
            return Activator.CreateInstance(type);
        }

    }
}
