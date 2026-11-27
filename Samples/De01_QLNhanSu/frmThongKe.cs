using System;
using System.Data;
using System.Windows.Forms;
using ExamHelper.Binding;
using ExamHelper.Database;
using ExamHelper.Utilities;

namespace De01_QLNhanSu
{
    public partial class frmThongKe : Form
    {
        private DataTable _dtKetQua;
        public frmThongKe() => InitializeComponent();

        private void frmThongKe_Load(object sender, EventArgs e)
        {
            ComboBoxHelper.BindWithAll(cboPhongBan, "SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB", "Tất cả");
            ComboBoxHelper.BindItems(cboGioiTinh, "Tất cả", "Nam", "Nữ");
            TimKiem(true);
        }

        private void TimKiem(bool isInitial)
        {
            var filter = FilterBuilder.New().SkipIfAllSelected(cboPhongBan, "nv.MaPB");
            if (cboGioiTinh.SelectedIndex == 1) filter.Equal("nv.GioiTinh", 1);
            else if (cboGioiTinh.SelectedIndex == 2) filter.Equal("nv.GioiTinh", 0);

            string sql = @"SELECT nv.MaNV, nv.HoTen, CASE nv.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END AS GioiTinhHT,
                                  nv.NgaySinh, pb.TenPB, nv.HeSoLuong, nv.NgayVaoLam,
                                  CAST(ROUND(nv.HeSoLuong * 2340000, 0) AS DECIMAL(18,0)) AS Luong
                           FROM NhanVien nv JOIN PhongBan pb ON nv.MaPB = pb.MaPB";
            _dtKetQua = DbHelper.GetDataTable(sql, filter, "pb.TenPB, nv.MaNV");

            GridHelper.Bind(dgvKetQua, _dtKetQua,
                new GridHelper.GridColumn("MaNV", "Mã NV", 90),
                new GridHelper.GridColumn("HoTen", "Họ tên", 170),
                new GridHelper.GridColumn("GioiTinhHT", "Giới tính", 80),
                new GridHelper.GridColumn("NgaySinh", "Ngày sinh", 100, "dd/MM/yyyy"),
                new GridHelper.GridColumn("TenPB", "Phòng ban", 125),
                new GridHelper.GridColumn("HeSoLuong", "Hệ số lương", 95, "0.##", DataGridViewContentAlignment.MiddleRight),
                new GridHelper.GridColumn("NgayVaoLam", "Ngày vào làm", 105, "dd/MM/yyyy"),
                new GridHelper.GridColumn("Luong", "Lương", -1, "N0", DataGridViewContentAlignment.MiddleRight));
            GridHelper.SetReadOnly(dgvKetQua);

            int count = DataHelper.Count(_dtKetQua);
            lblSoNV.Text = $"Số nhân viên: {count}";
            lblTongLuong.Text = $"Tổng lương: {DataHelper.Sum(_dtKetQua, "Luong"):N0}";
            lblHeSoMax.Text = $"Hệ số lương cao nhất: {DataHelper.Max<double>(_dtKetQua, "HeSoLuong"):0.##}";
            if (!isInitial && count == 0) MessageHelper.Info("Không tìm thấy dữ liệu phù hợp");
        }

        private void btnXuatBaoCao_Click(object sender, EventArgs e)
        {
            if (_dtKetQua == null || _dtKetQua.Rows.Count == 0) { MessageHelper.Warning("Không có dữ liệu để xuất báo cáo"); return; }
            ReportHelper.ShowReport(_dtKetQua, "BẢNG LƯƠNG NHÂN VIÊN",
                $"Phòng ban: {cboPhongBan.Text}; Giới tính: {cboGioiTinh.Text}", "TenPB", "Phòng ban: ",
                new[] {
                    new ReportColumn("MaNV", "Mã NV", HorizontalAlignment.Center),
                    new ReportColumn("HoTen", "Họ tên"),
                    new ReportColumn("NgaySinh", "Ngày sinh", "dd/MM/yyyy", HorizontalAlignment.Center),
                    new ReportColumn("HeSoLuong", "Hệ số lương", "0.##", HorizontalAlignment.Right),
                    new ReportColumn("Luong", "Lương", "N0", HorizontalAlignment.Right, isSum: true)
                }, "CÔNG TY TNHH AN PHÁT", this);
        }
    }
}
