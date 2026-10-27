/* ==============================================================================================
 * EXAMHELPER PRO - BỘ HÀM MẪU CHUẨN ĐI THI (COPY-PASTE VÀ GỌI NGAY)
 * 
 * HƯỚNG DẪN DÙNG TRONG PHÒNG THI:
 * 1. Mở file này, copy các hàm tương ứng vào Form1.cs hoặc Form2.cs của bạn.
 * 2. Thay đổi tên bảng (TABLE_NAME), tên cột khóa (KEY_COLUMN) và tên Database.
 * 3. Đổi tên các control theo đề thi (txtMa, txtTen, cbo, dtp...).
 * ============================================================================================== */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ExamHelper.Binding;
using ExamHelper.Database;
using ExamHelper.Utilities;
using ExamHelper.Validation;

namespace ExamHelper.Templates
{
    // ==========================================================================================
    // PHẦN 1: TOÀN BỘ CÁC HÀM CHO FORM 1 - QUẢN LÝ DỮ LIỆU CHÍNH (CRUD)
    // ==========================================================================================
    public class Form1_MauChuan : Form
    {
        // --------------------------------------------------------------------------------------
        // KHỐI 1: CẤU HÌNH HẰNG SỐ (Sửa 3 dòng này theo đề bài)
        // --------------------------------------------------------------------------------------
        private const string DB_NAME = "QLNhanSu";         // Tên Database của đề
        private const string TABLE_NAME = "NhanVien";      // Tên bảng chính
        private const string KEY_COLUMN = "MaNV";          // Tên cột khóa chính

        // Giả lập các control (Khi bạn kéo thả trong Designer thì các biến này đã có sẵn)
        private TextBox txtMaNV, txtHoTen, txtHeSoLuong;
        private ComboBox cboPhongBan;
        private DateTimePicker dtpNgaySinh, dtpNgayVaoLam;
        private RadioButton rdoNam, rdoNu;
        private DataGridView dgvDanhSach;

        // --------------------------------------------------------------------------------------
        // KHỐI 2: FORM LOAD & HÀM NẠP BẢNG (LOAD DATA)
        // --------------------------------------------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {
            // Thiết lập kết nối SQL Server (Tự động nhận diện localhost)
            DbHelper.SetDefaultConnectionString($"Server=.;Database={DB_NAME};Trusted_Connection=True;");

            // Nạp ComboBox danh mục khóa ngoại (Ví dụ: Phòng ban, Lớp học, Thể loại...)
            // Cú pháp: Bind(cbo, DataTable, "Cột_Giá_Trị_Value", "Cột_Hiển_Thị_Display")
            DataTable dtDanhMuc = DbHelper.GetDataTable("SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB");
            ComboBoxHelper.Bind(cboPhongBan, dtDanhMuc, "MaPB", "TenPB");

            LoadData();
        }

        private void LoadData()
        {
            // Viết câu lệnh SELECT hiển thị lên Grid (Có JOIN để lấy tên thay vì mã nếu đề yêu cầu)
            string sql = @"SELECT nv.MaNV, nv.HoTen, 
                                  CASE nv.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END AS GioiTinhHienThi,
                                  nv.NgaySinh, pb.TenPB, nv.HeSoLuong, nv.NgayVaoLam,
                                  CAST(ROUND(nv.HeSoLuong * 2340000, 0) AS DECIMAL(18,0)) AS Luong,
                                  nv.GioiTinh, nv.MaPB
                           FROM NhanVien nv 
                           JOIN PhongBan pb ON nv.MaPB = pb.MaPB 
                           ORDER BY nv.MaNV";

            DataTable dt = DbHelper.GetDataTable(sql);

            // Nạp dữ liệu lên DataGridView kèm định dạng cột
            GridHelper.Bind(dgvDanhSach, dt,
                new GridHelper.GridColumn("MaNV", "Mã NV", 90),
                new GridHelper.GridColumn("HoTen", "Họ tên", 180),
                new GridHelper.GridColumn("GioiTinhHienThi", "Giới tính", 80),
                new GridHelper.GridColumn("NgaySinh", "Ngày sinh", 100, "dd/MM/yyyy"),
                new GridHelper.GridColumn("TenPB", "Phòng ban", 140),
                new GridHelper.GridColumn("HeSoLuong", "Hệ số lương", 90, "0.##"),
                new GridHelper.GridColumn("NgayVaoLam", "Ngày vào làm", 100, "dd/MM/yyyy"),
                new GridHelper.GridColumn("Luong", "Lương", 120, "N0") // N0 = Phân cách hàng nghìn (1.500.000)
            );

            // Bật chế độ chỉ đọc, chọn nguyên dòng
            GridHelper.SetReadOnly(dgvDanhSach);
        }

        // --------------------------------------------------------------------------------------
        // KHỐI 3: CLICK CHỌN DÒNG TRÊN BẢNG -> NẠP LÊN CÁC Ô NHẬP LIỆU & KHÓA MÃ
        // --------------------------------------------------------------------------------------
        private void dgvDanhSach_SelectionChanged(object sender, EventArgs e)
        {
            DataRowView row = GridHelper.GetSelectedRow(dgvDanhSach);
            if (row == null) return;

            // Ánh xạ an toàn từ DataRow lên controls (Không bao giờ lỗi DBNull)
            FormMapper.LoadRow(row,
                FormMapper.Map(txtMaNV, "MaNV"),
                FormMapper.Map(txtHoTen, "HoTen"),
                FormMapper.Map(dtpNgaySinh, "NgaySinh"),
                FormMapper.Map(cboPhongBan, "MaPB"),
                FormMapper.Map(txtHeSoLuong, "HeSoLuong"),
                FormMapper.Map(dtpNgayVaoLam, "NgayVaoLam")
            );

            // Xử lý RadioButton Giới tính nếu có
            if (row["GioiTinh"] != DBNull.Value)
            {
                rdoNam.Checked = Convert.ToBoolean(row["GioiTinh"]);
                rdoNu.Checked = !rdoNam.Checked;
            }

            // Khóa ô mã chính sang chỉ đọc khi chọn dòng sửa/xem
            txtMaNV.ReadOnly = true;
        }

        // --------------------------------------------------------------------------------------
        // KHỐI 4: NÚT THÊM (VALIDATE -> CHECK TRÙNG MÃ -> INSERT -> NẠP LẠI BẢNG)
        // --------------------------------------------------------------------------------------
        private void btnThem_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra tính hợp lệ dữ liệu
            var val = ValidateForm();
            if (!val.IsValid)
            {
                MessageHelper.Warning(val.ErrorMessage);
                FormHelper.FocusControl(val.ErrorControl);
                return;
            }

            // 2. Kiểm tra trùng khóa chính
            string ma = txtMaNV.Text.Trim();
            if (CrudHelper.Exists(TABLE_NAME, KEY_COLUMN, ma))
            {
                MessageHelper.DuplicateKey("Mã", ma);
                FormHelper.FocusControl(txtMaNV);
                return;
            }

            // 3. Đọc dữ liệu từ form và thêm vào CSDL bằng SqlParameter
            var values = GetFormValues();
            if (CrudHelper.Insert(TABLE_NAME, values) > 0)
            {
                MessageHelper.Success("thêm mới");
                LoadData();
                btnLamMoi_Click(null, null); // Xóa trắng form sau khi thêm
            }
        }

        // --------------------------------------------------------------------------------------
        // KHỐI 5: NÚT SỬA (CHECK CHỌN DÒNG -> VALIDATE -> UPDATE -> NẠP LẠI BẢNG)
        // --------------------------------------------------------------------------------------
        private void btnSua_Click(object sender, EventArgs e)
        {
            // 1. Yêu cầu phải chọn dòng trên DataGridView
            if (!GridHelper.HasSelection(dgvDanhSach))
            {
                MessageHelper.NoSelection("dòng cần sửa");
                return;
            }

            // 2. Kiểm tra tính hợp lệ dữ liệu
            var val = ValidateForm();
            if (!val.IsValid)
            {
                MessageHelper.Warning(val.ErrorMessage);
                FormHelper.FocusControl(val.ErrorControl);
                return;
            }

            // 3. Cập nhật dữ liệu vào CSDL (Khóa chính tự động được giữ nguyên trong WHERE)
            var values = GetFormValues();
            string ma = txtMaNV.Text.Trim();
            if (CrudHelper.Update(TABLE_NAME, values, KEY_COLUMN, ma) > 0)
            {
                MessageHelper.Success("cập nhật");
                LoadData();
            }
        }

        // --------------------------------------------------------------------------------------
        // KHỐI 6: NÚT XÓA (CHECK CHỌN DÒNG -> XÁC NHẬN YES/NO -> DELETE)
        // --------------------------------------------------------------------------------------
        private void btnXoa_Click(object sender, EventArgs e)
        {
            if (!GridHelper.HasSelection(dgvDanhSach))
            {
                MessageHelper.NoSelection("dòng cần xóa");
                return;
            }

            string ma = txtMaNV.Text.Trim();
            // Hộp thoại hỏi đúng mẫu chuẩn: "Bạn có chắc chắn muốn xóa ... không?"
            if (MessageHelper.ConfirmDelete($"bản ghi có mã {ma}") && CrudHelper.Delete(TABLE_NAME, KEY_COLUMN, ma) > 0)
            {
                MessageHelper.Success("xóa");
                LoadData();
                btnLamMoi_Click(null, null);
            }
        }

        // --------------------------------------------------------------------------------------
        // KHỐI 7: NÚT LÀM MỚI, THỐNG KÊ, THOÁT
        // --------------------------------------------------------------------------------------
        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            // Xóa trắng các TextBox
            FormMapper.ClearControls(txtMaNV, txtHoTen, txtHeSoLuong);
            
            // Đặt lại các giá trị mặc định
            rdoNam.Checked = true;
            dtpNgaySinh.Value = DateTime.Today;
            dtpNgayVaoLam.Value = DateTime.Today;
            if (cboPhongBan.Items.Count > 0) cboPhongBan.SelectedIndex = 0;

            // Mở khóa ô mã để nhập mới và focus con trỏ
            txtMaNV.ReadOnly = false;
            txtMaNV.Focus();
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {
            if (MessageHelper.Confirm("Bạn có muốn thoát không?", "Xác nhận thoát"))
            {
                this.Close(); // Hoặc Application.Exit();
            }
        }

        // --------------------------------------------------------------------------------------
        // KHỐI 8: 2 HÀM TRỢ GIÚP (VALIDATE FORM & ĐỌC DỮ LIỆU TỪ CONTROL)
        // --------------------------------------------------------------------------------------
        private ValidationResult ValidateForm()
        {
            return Validator.ValidateAll(
                dtpNgayVaoLam.Value, // Ngày tham chiếu nếu cần tính tuổi tính đến ngày vào làm
                Validator.Required(txtMaNV, "Mã nhân viên"),
                Validator.Pattern(txtMaNV, "Mã nhân viên", @"^NV\d{3}$", "Mã phải có định dạng 'NV' + 3 chữ số (ví dụ: NV008)!"),
                Validator.Required(txtHoTen, "Họ tên"),
                Validator.DecimalRange(txtHeSoLuong, "Hệ số lương", 2.34m, 8.0m, "Hệ số lương phải từ 2,34 đến 8,0!"),
                Validator.AgeRangeAtDate(dtpNgaySinh, "Tuổi nhân viên", 18, 100, "Nhân viên phải đủ 18 tuổi tính đến ngày vào làm!")
            );
        }

        private Dictionary<string, object> GetFormValues()
        {
            var dict = FormMapper.GetValues(
                FormMapper.Map(txtMaNV, "MaNV"),
                FormMapper.Map(txtHoTen, "HoTen"),
                FormMapper.Map(cboPhongBan, "MaPB"),
                FormMapper.Map(dtpNgaySinh, "NgaySinh"),
                FormMapper.Map(txtHeSoLuong, "HeSoLuong"),
                FormMapper.Map(dtpNgayVaoLam, "NgayVaoLam")
            );

            // Gán giá trị RadioButton nếu có
            dict["GioiTinh"] = rdoNam.Checked ? 1 : 0;
            return dict;
        }
    }


    // ==========================================================================================
    // PHẦN 2: TOÀN BỘ CÁC HÀM CHO FORM 2 - TÌM KIẾM ĐA ĐIỀU KIỆN & THỐNG KÊ
    // ==========================================================================================
    public class Form2_MauChuan : Form
    {
        private ComboBox cboPhongBan, cboGioiTinh;
        private TextBox txtTuKhoa;
        private DataGridView dgvKetQua;
        private Label lblSoLuong, lblTongTien, lblGiaTriCaoNhat;
        private DataTable _dtKetQua;

        // --------------------------------------------------------------------------------------
        // KHỐI 1: FORM LOAD -> NẠP COMBOBOX CÓ DÒNG "TẤT CẢ"
        // --------------------------------------------------------------------------------------
        private void Form2_Load(object sender, EventArgs e)
        {
            // ComboBox có thêm dòng "Tất cả" ở đầu danh sách
            DataTable dtPB = DbHelper.GetDataTable("SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB");
            ComboBoxHelper.BindWithAll(cboPhongBan, dtPB, "MaPB", "TenPB", "-- Tất cả --");

            // ComboBox Giới tính: chọn mục đầu tiên (Tất cả)
            if (cboGioiTinh.Items.Count > 0) cboGioiTinh.SelectedIndex = 0;

            // Nạp dữ liệu ban đầu hiển thị tất cả
            TimKiem(isInitial: true);
        }

        // --------------------------------------------------------------------------------------
        // KHỐI 2: HÀM TÌM KIẾM KẾT HỢP ĐA ĐIỀU KIỆN & THỐNG KÊ (COUNT, SUM, MAX)
        // --------------------------------------------------------------------------------------
        private void btnTimKiem_Click(object sender, EventArgs e) => TimKiem(isInitial: false);

        private void TimKiem(bool isInitial)
        {
            // Tạo bộ lọc: tự động bỏ qua nếu chọn "Tất cả" hoặc để trống
            var filter = FilterBuilder.New()
                .SkipIfAllSelected(cboPhongBan, "pb.MaPB")
                .Contains("nv.HoTen", txtTuKhoa.Text); // Tìm kiếm từ khóa theo tên (nếu có ô nhập)

            // Điều kiện giới tính: Index 0 = Tất cả, Index 1 = Nam (1), Index 2 = Nữ (0)
            if (cboGioiTinh.SelectedIndex == 1) filter.Equal("nv.GioiTinh", 1);
            else if (cboGioiTinh.SelectedIndex == 2) filter.Equal("nv.GioiTinh", 0);

            var (where, parms) = filter.Build();
            string sql = @"SELECT nv.MaNV, nv.HoTen, 
                                  CASE nv.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END AS GioiTinhHienThi,
                                  nv.NgaySinh, pb.TenPB, nv.HeSoLuong, nv.NgayVaoLam,
                                  CAST(ROUND(nv.HeSoLuong * 2340000, 0) AS DECIMAL(18,0)) AS Luong
                           FROM NhanVien nv 
                           JOIN PhongBan pb ON nv.MaPB = pb.MaPB" 
                           + where + " ORDER BY pb.TenPB, nv.MaNV";

            _dtKetQua = DbHelper.GetDataTable(sql, parms);

            // Nạp kết quả lên bảng
            GridHelper.Bind(dgvKetQua, _dtKetQua,
                new GridHelper.GridColumn("MaNV", "Mã NV", 90),
                new GridHelper.GridColumn("HoTen", "Họ tên", 180),
                new GridHelper.GridColumn("GioiTinhHienThi", "Giới tính", 80),
                new GridHelper.GridColumn("NgaySinh", "Ngày sinh", 100, "dd/MM/yyyy"),
                new GridHelper.GridColumn("TenPB", "Phòng ban", 140),
                new GridHelper.GridColumn("HeSoLuong", "Hệ số lương", 90, "0.##"),
                new GridHelper.GridColumn("NgayVaoLam", "Ngày vào làm", 100, "dd/MM/yyyy"),
                new GridHelper.GridColumn("Luong", "Lương", 120, "N0")
            );
            GridHelper.SetReadOnly(dgvKetQua);

            // Cập nhật các chỉ số thống kê trên dữ liệu sau lọc
            int count = DataHelper.Count(_dtKetQua);
            lblSoLuong.Text = $"Số bản ghi: {count}";
            lblTongTien.Text = $"Tổng tiền: {DataHelper.Sum(_dtKetQua, "Luong"):N0} đ";
            lblGiaTriCaoNhat.Text = $"Hệ số lương cao nhất: {Convert.ToDouble(DataHelper.Max<object>(_dtKetQua, "HeSoLuong") ?? 0)}";

            // Thông báo nếu không tìm thấy dữ liệu khi bấm nút tìm
            if (!isInitial && count == 0)
            {
                MessageHelper.Info("Không tìm thấy dữ liệu phù hợp", "Kết quả tìm kiếm");
            }
        }

        // --------------------------------------------------------------------------------------
        // KHỐI 3: NÚT XUẤT BÁO CÁO & ĐÓNG FORM
        // --------------------------------------------------------------------------------------
        private void btnXuatBaoCao_Click(object sender, EventArgs e)
        {
            if (_dtKetQua == null || _dtKetQua.Rows.Count == 0)
            {
                MessageHelper.Warning("Không có dữ liệu để xuất báo cáo");
                return;
            }

            string dieuKien = $"Phòng ban: {cboPhongBan.Text}; Giới tính: {cboGioiTinh.Text}";

            // CÁCH 1 (KHUYÊN DÙNG - 1 DÒNG DÙNG FORM BÁO CÁO DÙNG CHUNG CÓ SẴN):
            ReportHelper.ShowReport(
                dataTable: _dtKetQua,
                reportTitle: "BẢNG LƯƠNG NHÂN VIÊN",
                filterCondition: dieuKien,
                groupByColumn: "TenPB", // Gom nhóm theo Phòng ban
                columns: new[] {
                    new ReportColumn("MaNV", "Mã NV", HorizontalAlignment.Center),
                    new ReportColumn("HoTen", "Họ tên"),
                    new ReportColumn("NgaySinh", "Ngày sinh", "dd/MM/yyyy", HorizontalAlignment.Center),
                    new ReportColumn("HeSoLuong", "Hệ số lương", "0.##", HorizontalAlignment.Center),
                    new ReportColumn("Luong", "Lương", "N0", HorizontalAlignment.Right, isSum: true) // isSum = tính tổng
                }
            );

            // HOẶC CÁCH 1B (SIÊU TỐC - TỰ ĐỘNG TẤT CẢ):
            // ReportHelper.ShowReport(_dtKetQua, "BẢNG LƯƠNG NHÂN VIÊN", "TenPB", "Luong", dieuKien);

            // CÁCH 2: Dùng Form tự tạo bên dưới nếu giám khảo yêu cầu phải add thêm Form riêng
            // new FormBaoCao_MauChuan(_dtKetQua, dieuKien).ShowDialog();
        }

        private void btnDong_Click(object sender, EventArgs e) => this.Close();
    }



    // ==========================================================================================
    // PHẦN 3: FORM BÁO CÁO CHUẨN (GOM NHÓM THEO DANH MỤC, XUẤT EXCEL & IN ẤN)
    // ==========================================================================================
    public class FormBaoCao_MauChuan : Form
    {
        private readonly DataTable _dataTable;
        private readonly string _dieuKienLoc;
        private WebBrowser webViewer;

        public FormBaoCao_MauChuan(DataTable dataTable, string dieuKienLoc)
        {
            _dataTable = dataTable;
            _dieuKienLoc = dieuKienLoc;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            webViewer = new WebBrowser { Dock = DockStyle.Fill };
            this.Controls.Add(webViewer);
            this.Size = new System.Drawing.Size(900, 700);
            this.Text = "Báo cáo thống kê";
            this.Load += (s, e) => webViewer.DocumentText = TaoHtmlBaoCao();
        }

        // Nút In ấn: gọi webViewer.ShowPrintDialog();
        public void InBaoCao() => webViewer.ShowPrintDialog();

        // Nút Xuất Excel: Lưu nội dung HTML thành file .xls
        public void XuatExcel()
        {
            using (var sfd = new SaveFileDialog { Filter = "Excel Workbook (*.xls)|*.xls", FileName = "BaoCao.xls" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, TaoHtmlBaoCao(), Encoding.UTF8);
                    MessageHelper.Success("xuất file Excel");
                }
            }
        }

        // Tạo mã HTML bảng báo cáo chuẩn yêu cầu của đề thi
        private string TaoHtmlBaoCao()
        {
            var sb = new StringBuilder();
            sb.Append(@"<html><head><meta charset='utf-8'><style>
                body { font-family: 'Times New Roman', serif; margin: 30px; font-size: 14px; }
                table { width: 100%; border-collapse: collapse; margin-top: 15px; }
                th, td { border: 1px solid #444; padding: 6px 8px; }
                th { background-color: #e2eaf2; text-align: center; }
                .nhom { font-weight: bold; background-color: #f7f9fa; }
                .tong { font-weight: bold; background-color: #fcefe6; }
                .c { text-align: center; } .r { text-align: right; }
            </style></head><body>");

            // Tiêu đề đầu trang
            sb.Append("<table><tr><td style='border:none;font-weight:bold;'>CÔNG TY TNHH AN PHÁT</td><td style='border:none;text-align:right;font-style:italic;'>Ngày lập: " + DateTime.Now.ToString("dd/MM/yyyy") + "</td></tr></table>");
            sb.Append("<h2 style='text-align:center;margin:15px 0 5px;'>BẢNG LƯƠNG NHÂN VIÊN</h2>");
            sb.Append($"<p style='text-align:center;font-style:italic;'>Điều kiện lọc: {_dieuKienLoc}</p>");

            // Tiêu đề bảng
            sb.Append("<table><tr><th>STT</th><th>Mã NV</th><th>Họ tên</th><th>Ngày sinh</th><th>Hệ số lương</th><th>Lương</th></tr>");

            // Gom nhóm theo cột "TenPB"
            DataTable distinctNhom = _dataTable.DefaultView.ToTable(true, "TenPB");
            int stt = 1;
            decimal tongTien = 0;

            foreach (DataRow n in distinctNhom.Rows)
            {
                string tenNhom = n["TenPB"].ToString();
                DataRow[] rows = _dataTable.Select($"TenPB = '{tenNhom.Replace("'", "''")}'");
                sb.Append($"<tr><td colspan='6' class='nhom'>Phòng ban: {tenNhom} ({rows.Length} bản ghi)</td></tr>");

                foreach (DataRow r in rows)
                {
                    decimal luong = Convert.ToDecimal(r["Luong"]);
                    tongTien += luong;
                    sb.Append($"<tr><td class='c'>{stt++}</td><td class='c'>{r["MaNV"]}</td><td>{r["HoTen"]}</td><td class='c'>{Convert.ToDateTime(r["NgaySinh"]):dd/MM/yyyy}</td><td class='c'>{r["HeSoLuong"]}</td><td class='r'>{luong:N0} đ</td></tr>");
                }
            }

            // Dòng Tổng cộng
            sb.Append($"<tr class='tong'><td colspan='2' class='c'>TỔNG CỘNG</td><td colspan='3'></td><td class='r'>{tongTien:N0} đ</td></tr>");
            sb.Append("</table>");

            // Chữ ký Người lập biểu
            sb.Append("<table style='margin-top:35px;'><tr><td style='border:none;'></td><td style='border:none;text-align:right;padding-right:40px;'><b>Người lập biểu</b><br><i>(Ký, ghi rõ họ tên)</i></td></tr></table>");
            sb.Append("<div style='text-align:center;font-style:italic;margin-top:30px;'>Trang 1/1</div></body></html>");

            return sb.ToString();
        }
    }
}
