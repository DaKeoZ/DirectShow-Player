using System;

namespace fr.ipmfrance.webcam
{
    /// <summary>
    /// L'énumération définit si un paramètre de caméra est contrôlé manuellement ou automatiquement.
    /// </summary>
    [Flags]
    public enum CameraControlFlags
    {
        /// <summary>
        /// No control flag.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Auto control Flag.
        /// </summary>
        Auto = 0x0001,
        /// <summary>
        /// Manual control Flag.
        /// </summary>
        Manual = 0x0002
    }

}
