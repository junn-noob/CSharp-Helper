using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using ExamHelper.Binding;
using ExamHelper.Database;
using ExamHelper.Utilities;
using ExamHelper.Validation;

namespace De01_QLNhanSu
{
    public partial class frmQuanLyNhanVien : Form
    {
        public frmQuanLyNhanVien() => InitializeComponent();

        private void frmQuanLyNhanVien_Load(object sender, EventArgs e)
        {
            ComboBoxHelper.Bind(cboPhongBan, "SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB");
            LoadData();
        }

        private void LoadData()
        {
            string sql = @"SELECT nv.MaNV, nv.HoTen, CASE nv.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END AS GioiTinhHT,
                                  nv.NgaySinh, pb.TenPB, nv.HeSoLuong, nv.NgayVaoLam,
                                  CAST(ROUND(nv.HeSoLuong * 2340000, 0) AS DECIMAL(18,0)) AS Luong, nv.GioiTinh, nv.MaPB
                           FROM NhanVien nv JOIN PhongBan pb ON nv.MaPB = pb.MaPB ORDER BY nv.MaNV";
            GridHelper.Bind(dgvDanhSach, sql,
                new GridHelper.GridColumn("MaNV", "Mã NV", 90),
                new GridHelper.GridColumn("HoTen", "Họ tên", 175),
                new GridHelper.GridColumn("GioiTinhHT", "Giới tính", 80),
                new GridHelper.GridColumn("NgaySinh", "Ngày sinh", 100, "dd/MM/yyyy"),
                new GridHelper.GridColumn("TenPB", "Phòng ban", 130),
                new GridHelper.GridColumn("HeSoLuong", "Hệ số lương", 95, "0.##", DataGridViewContentAlignment.MiddleRight),
                new GridHelper.GridColumn("NgayVaoLam", "Ngày vào làm", 105, "dd/MM/yyyy"),
                new GridHelper.GridColumn("Luong", "Lương", -1, "N0", DataGridViewContentAlignment.MiddleRight));
        }

        private void dgvDanhSach_SelectionChanged(object sender, EventArgs e)
        {
            DataRowView row = GridHelper.GetSelectedRow(dgvDanhSach);
            if (row == null) return;
            FormMapper.AutoLoad(this, row);
            txtMaNV.ReadOnly = true;
        }

        private bool IsInputValid()
        {
            var val = Validator.ValidateAll(
                Validator.Required(txtMaNV, "Mã NV"),
                Validator.Pattern(txtMaNV, "Mã NV", @"^NV\d{3}$", "Mã NV phải có dạng NV + 3 chữ số!"),
                Validator.Required(txtHoTen, "Họ tên"),
                Validator.Required(txtHeSoLuong, "Hệ số lương"),
                Validator.DecimalRange(txtHeSoLuong, "Hệ số lương", 2.34m, 8.0m, "Hệ số lương phải từ 2,34 – 8,0!"),
                Validator.AgeRangeAtDate(dtpNgaySinh, dtpNgayVaoLam, "Tuổi", 18, 120, "Nhân viên phải đủ 18 tuổi tính đến ngày vào làm!"));
            if (!val.IsValid) { MessageHelper.Warning(val.ErrorMessage); FormHelper.FocusControl(val.ErrorControl); }
            return val.IsValid;
        }

        private Dictionary<string, object> GetValues() => FormMapper.AutoGetValues(this, dgvDanhSach.DataSource as DataTable);

        private void btnThem_Click(object sender, EventArgs e)
        {
            if (!IsInputValid()) return;
            string ma = txtMaNV.Text.Trim();
            if (CrudHelper.Exists("NhanVien", "MaNV", ma)) { MessageHelper.DuplicateKey("Mã NV", ma); FormHelper.FocusControl(txtMaNV); return; }
            if (CrudHelper.Insert("NhanVien", GetValues()) > 0) { MessageHelper.Success("thêm mới"); LoadData(); }
        }

        private void btnSua_Click(object sender, EventArgs e)
        {
            if (!GridHelper.HasSelection(dgvDanhSach)) { MessageHelper.NoSelection("nhân viên"); return; }
            if (!IsInputValid()) return;
            if (CrudHelper.Update("NhanVien", GetValues(), "MaNV", txtMaNV.Text.Trim()) > 0) { MessageHelper.Success("cập nhật"); LoadData(); }
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            if (!GridHelper.HasSelection(dgvDanhSach)) { MessageHelper.NoSelection("nhân viên"); return; }
            string ma = txtMaNV.Text.Trim();
            if (MessageHelper.ConfirmDelete($"nhân viên {ma}") && CrudHelper.Delete("NhanVien", "MaNV", ma) > 0) { MessageHelper.Success("xóa"); LoadData(); }
        }

        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            FormMapper.AutoClear(this);
            rdoNam.Checked = true;
            GridHelper.ClearSelection(dgvDanhSach);
            txtMaNV.ReadOnly = false;
            txtMaNV.Focus();
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {
            if (MessageHelper.Confirm("Bạn có muốn thoát không?")) Application.Exit();
        }
    }
}
