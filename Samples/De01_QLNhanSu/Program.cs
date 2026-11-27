using System;
using System.Windows.Forms;

namespace De01_QLNhanSu
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmQuanLyNhanVien());
        }
    }
}
