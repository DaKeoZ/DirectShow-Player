

namespace fr.ipmfrance.webcam
{
    public static class OverlayFactory
    {
        public static IOverlay Create()
        {
            return new HelloOverlay();
        }
    }
}
