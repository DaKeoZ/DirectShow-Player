using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace fr.ipmfrance.webcam
{
    public class HelloOverlay : IOverlay
    {
        public void Draw(Graphics frame)
        {
            SolidBrush brush = new SolidBrush(Color.Blue);
            Font f = new Font(FontFamily.GenericSansSerif, 12);
            DateTime now = DateTime.Now;
            frame.DrawString(now.ToString(), f, brush, new PointF(605, 205));
            frame.DrawString("Hello World", f, brush, new PointF(605, 225));
            f.Dispose();
            brush.Dispose();
        }
    }
}
