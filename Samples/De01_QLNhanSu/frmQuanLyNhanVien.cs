using System;
using System.Windows.Forms;
using ExamHelper.Binding;
using ExamHelper.Utilities;
using ExamHelper.Validation;

namespace De01_QLNhanSu
{
    public partial class frmQuanLyNhanVien : Form
    {
        private CrudBinder _crud;

        public frmQuanLyNhanVien() => InitializeComponent();

        private void frmQuanLyNhanVien_Load(object sender, EventArgs e)
        {
            string sql = @"SELECT nv.MaNV, nv.HoTen, CASE nv.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END AS GioiTinhHT,
                                  nv.NgaySinh, pb.TenPB, nv.HeSoLuong, nv.NgayVaoLam,
                                  CAST(ROUND(nv.HeSoLuong * 2340000, 0) AS DECIMAL(18,0)) AS Luong, nv.GioiTinh, nv.MaPB
                           FROM NhanVien nv JOIN PhongBan pb ON nv.MaPB = pb.MaPB ORDER BY nv.MaNV";

            _crud = CrudBinder.For(this, tableName: "NhanVien", keyColumn: "MaNV", keyControl: txtMaNV, grid: dgvDanhSach, entityLabel: "nhân viên")
                .WithCombo(cboPhongBan, "SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB")
                .WithQuery(sql,
                    new GridHelper.GridColumn("MaNV", "Mã NV", 90),
                    new GridHelper.GridColumn("HoTen", "Họ tên", 175),
                    new GridHelper.GridColumn("GioiTinhHT", "Giới tính", 80),
                    new GridHelper.GridColumn("NgaySinh", "Ngày sinh", 100, "dd/MM/yyyy"),
                    new GridHelper.GridColumn("TenPB", "Phòng ban", 130),
                    new GridHelper.GridColumn("HeSoLuong", "Hệ số lương", 95, "0.##", DataGridViewContentAlignment.MiddleRight),
                    new GridHelper.GridColumn("NgayVaoLam", "Ngày vào làm", 105, "dd/MM/yyyy"),
                    new GridHelper.GridColumn("Luong", "Lương", -1, "N0", DataGridViewContentAlignment.MiddleRight))
                .WithDefaultRadios(rdoNam)
                .WithExtraValidation(() => new[]
                {
                    Validator.Pattern(txtMaNV, "Mã NV", @"^NV\d{3}$", "Mã NV phải có dạng NV + 3 chữ số!"),
                    Validator.DecimalRange(txtHeSoLuong, "Hệ số lương", 2.34m, 8.0m, "Hệ số lương phải từ 2,34 – 8,0!"),
                    Validator.AgeRangeAtDate(dtpNgaySinh, dtpNgayVaoLam, "Tuổi", 18, 120, "Nhân viên phải đủ 18 tuổi tính đến ngày vào làm!")
                })
                .BindButtons(btnThem, btnSua, btnXoa, btnLamMoi, btnThoat)
                .Start();
        }
    }
}
