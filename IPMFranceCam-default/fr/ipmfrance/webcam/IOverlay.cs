using System.Drawing;

namespace fr.ipmfrance.webcam
{
    /// <summary>
    /// Interface pour tracer des overlay
    /// </summary>
    public interface IOverlay
    {
        /// <summary>
        /// Dessin sur le bitmap
        /// </summary>
        /// <param name="frame"></param>
        void Draw(Graphics frame);
    }
}
