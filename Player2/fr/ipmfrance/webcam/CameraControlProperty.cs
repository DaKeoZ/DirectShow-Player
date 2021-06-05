using System;

namespace fr.ipmfrance.webcam
{
    /// <summary>
    /// The enumeration specifies a setting on a camera.
    /// </summary>
    public enum CameraControlProperty
    {
        /// <summary>
        /// Pan control.
        /// </summary>
        Pan = 0,
        /// <summary>
        /// Tilt control.
        /// </summary>
        Tilt,
        /// <summary>
        /// Roll control.
        /// </summary>
        Roll,
        /// <summary>
        /// Zoom control.
        /// </summary>
        Zoom,
        /// <summary>
        /// Exposure control.
        /// </summary>
        Exposure,
        /// <summary>
        /// Iris control.
        /// </summary>
        Iris,
        /// <summary>
        /// Focus control.
        /// </summary>
        Focus
    }
}
