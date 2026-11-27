using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ExamHelper.Utilities
{
    /// <summary>
    /// SplitContainer bọc an toàn ngoại lệ GDI+ từ RepaintSplitterRect() trên môi trường thiếu interactive DC.
    /// </summary>
    internal sealed class SafeSplitContainer : SplitContainer
    {
        protected override void OnLayout(LayoutEventArgs e)
        {
            try
            {
                base.OnLayout(e);
            }
            catch (ExternalException)
            {
                // Kích thước Panel1/Panel2 bên trong base.OnLayout đã được tính trước bước RepaintSplitterRect().
            }
        }
    }
}
