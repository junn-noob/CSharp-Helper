using System.Drawing;
using System.Windows.Forms;

namespace De01_QLNhanSu
{
    public partial class frmThongKe
    {
        private ComboBox cboPhongBan;
        private ComboBox cboGioiTinh;
        private Button btnTimKiem;
        private Button btnXuatBaoCao;
        private Button btnDong;
        private DataGridView dgvKetQua;
        private Label lblSoNV;
        private Label lblTongLuong;
        private Label lblHeSoMax;

        private void InitializeComponent()
        {
            Text = "Thống kê nhân viên - frmThongKe";
            Size = new Size(940, 590);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9.5f);

            var grpFilter = new GroupBox { Text = "Điều kiện tìm kiếm", Location = new Point(12, 10), Size = new Size(900, 105), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var lblPB = new Label { Text = "Phòng ban", Location = new Point(20, 32), AutoSize = true };
            cboPhongBan = new ComboBox { Name = "cboPhongBan", Location = new Point(100, 29), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            var lblGT = new Label { Text = "Giới tính", Location = new Point(305, 32), AutoSize = true };
            cboGioiTinh = new ComboBox { Name = "cboGioiTinh", Location = new Point(375, 29), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };

            btnTimKiem = new Button { Name = "btnTimKiem", Text = "&Tìm kiếm", Location = new Point(450, 62), Size = new Size(130, 32) };
            btnXuatBaoCao = new Button { Name = "btnXuatBaoCao", Text = "&Xuất báo cáo", Location = new Point(595, 62), Size = new Size(145, 32) };
            btnDong = new Button { Name = "btnDong", Text = "Đó&ng", Location = new Point(755, 62), Size = new Size(125, 32) };

            grpFilter.Controls.AddRange(new Control[] { lblPB, cboPhongBan, lblGT, cboGioiTinh, btnTimKiem, btnXuatBaoCao, btnDong });

            var grpResult = new GroupBox { Text = "Kết quả", Location = new Point(12, 125), Size = new Size(900, 315), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            dgvKetQua = new DataGridView { Name = "dgvKetQua", Dock = DockStyle.Fill };
            grpResult.Controls.Add(dgvKetQua);

            var grpStats = new GroupBox { Text = "Thống kê", Location = new Point(12, 448), Size = new Size(900, 92), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            var statFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            var statColor = Color.FromArgb(0, 70, 173);
            lblSoNV = new Label { Name = "lblSoNV", Text = "Số nhân viên: 0", Location = new Point(25, 28), AutoSize = true, Font = statFont, ForeColor = statColor };
            lblTongLuong = new Label { Name = "lblTongLuong", Text = "Tổng lương: 0", Location = new Point(460, 28), AutoSize = true, Font = statFont, ForeColor = statColor };
            lblHeSoMax = new Label { Name = "lblHeSoMax", Text = "Hệ số lương cao nhất: 0", Location = new Point(25, 58), AutoSize = true, Font = statFont, ForeColor = statColor };
            grpStats.Controls.AddRange(new Control[] { lblSoNV, lblTongLuong, lblHeSoMax });

            Controls.AddRange(new Control[] { grpFilter, grpResult, grpStats });

            Load += frmThongKe_Load;
            btnTimKiem.Click += (s, e) => TimKiem(false);
            btnXuatBaoCao.Click += btnXuatBaoCao_Click;
            btnDong.Click += (s, e) => Close();
        }
    }
}
