using System.Drawing;
using System.Windows.Forms;

namespace De01_QLNhanSu
{
    public partial class frmQuanLyNhanVien
    {
        private TextBox txtMaNV;
        private TextBox txtHoTen;
        private RadioButton rdoNam;
        private RadioButton rdoNu;
        private DateTimePicker dtpNgaySinh;
        private ComboBox cboPhongBan;
        private TextBox txtHeSoLuong;
        private DateTimePicker dtpNgayVaoLam;
        private DataGridView dgvDanhSach;
        private Button btnThem;
        private Button btnSua;
        private Button btnXoa;
        private Button btnLamMoi;
        private Button btnThongKe;
        private Button btnThoat;

        private void InitializeComponent()
        {
            Text = "Quản lý nhân viên - frmQuanLyNhanVien";
            Size = new Size(960, 620);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);

            var grpInfo = new GroupBox { Text = "Thông tin nhân viên", Location = new Point(12, 10), Size = new Size(920, 235), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var lblMa = new Label { Text = "Mã NV", Location = new Point(20, 32), AutoSize = true };
            txtMaNV = new TextBox { Name = "txtMaNV", Location = new Point(135, 29), Width = 170 };
            var lblHoTen = new Label { Text = "Họ tên", Location = new Point(20, 68), AutoSize = true };
            txtHoTen = new TextBox { Name = "txtHoTen", Location = new Point(135, 65), Width = 240 };
            var lblGioiTinh = new Label { Text = "Giới tính", Location = new Point(20, 104), AutoSize = true };
            var pnlGender = new Panel { Location = new Point(135, 99), Size = new Size(180, 28) };
            rdoNam = new RadioButton { Name = "rdoNam", Text = "Nam", Tag = "GioiTinh:1", Location = new Point(0, 3), AutoSize = true, Checked = true };
            rdoNu = new RadioButton { Name = "rdoNu", Text = "Nữ", Tag = "GioiTinh:0", Location = new Point(80, 3), AutoSize = true };
            pnlGender.Controls.AddRange(new Control[] { rdoNam, rdoNu });
            var lblNgaySinh = new Label { Text = "Ngày sinh", Location = new Point(20, 140), AutoSize = true };
            dtpNgaySinh = new DateTimePicker { Name = "dtpNgaySinh", Location = new Point(135, 137), Width = 160, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
            var lblPhongBan = new Label { Text = "Phòng ban", Location = new Point(470, 32), AutoSize = true };
            cboPhongBan = new ComboBox { Name = "cboPhongBan", Location = new Point(585, 29), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            var lblHeSo = new Label { Text = "Hệ số lương", Location = new Point(470, 68), AutoSize = true };
            txtHeSoLuong = new TextBox { Name = "txtHeSoLuong", Location = new Point(585, 65), Width = 140 };
            var lblNgayVao = new Label { Text = "Ngày vào làm", Location = new Point(470, 104), AutoSize = true };
            dtpNgayVaoLam = new DateTimePicker { Name = "dtpNgayVaoLam", Location = new Point(585, 101), Width = 160, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };

            btnThem = new Button { Name = "btnThem", Text = "&Thêm", Location = new Point(55, 185), Size = new Size(125, 32) };
            btnSua = new Button { Name = "btnSua", Text = "&Sửa", Location = new Point(195, 185), Size = new Size(125, 32) };
            btnXoa = new Button { Name = "btnXoa", Text = "&Xóa", Location = new Point(335, 185), Size = new Size(125, 32) };
            btnLamMoi = new Button { Name = "btnLamMoi", Text = "&Làm mới", Location = new Point(475, 185), Size = new Size(125, 32) };
            btnThongKe = new Button { Name = "btnThongKe", Text = "Thống &kê", Location = new Point(615, 185), Size = new Size(125, 32) };
            btnThoat = new Button { Name = "btnThoat", Text = "T&hoát", Location = new Point(755, 185), Size = new Size(125, 32) };

            grpInfo.Controls.AddRange(new Control[]
            {
                lblMa, txtMaNV, lblHoTen, txtHoTen, lblGioiTinh, pnlGender, lblNgaySinh, dtpNgaySinh,
                lblPhongBan, cboPhongBan, lblHeSo, txtHeSoLuong, lblNgayVao, dtpNgayVaoLam,
                btnThem, btnSua, btnXoa, btnLamMoi, btnThongKe, btnThoat
            });

            var grpList = new GroupBox { Text = "Danh sách nhân viên", Location = new Point(12, 255), Size = new Size(920, 315), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            dgvDanhSach = new DataGridView { Name = "dgvDanhSach", Dock = DockStyle.Fill };
            grpList.Controls.Add(dgvDanhSach);

            Controls.AddRange(new Control[] { grpInfo, grpList });

            Load += frmQuanLyNhanVien_Load;
            btnThongKe.Click += (s, e) => new frmThongKe().ShowDialog(this);
        }
    }
}
