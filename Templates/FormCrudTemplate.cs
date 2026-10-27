/* ==============================================================================================
 * EXAMHELPER PRO - MẪU FORM CRUD CHUẨN ĐI THI (DÙNG CHO MỌI ĐỀ WINFORMS)
 * Hướng dẫn 5 bước làm bài thi thần tốc:
 * Bước 1: Thay tên bảng "TenBang", tên cột khóa "MaKhoa", chuỗi kết nối Database.
 * Bước 2: Khai báo các Control của đề trong InitializeComponent (hoặc kéo thả trong Designer).
 * Bước 3: Trong LoadData(), thay câu SQL SELECT và danh sách cột hiển thị trên DataGridView.
 * Bước 4: Trong FormMapper, liệt kê các cặp Control - Tên Cột.
 * Bước 5: Trong BtnLuu_Click, khai báo các quy tắc kiểm tra (Validator).
 * ============================================================================================== */

#pragma warning disable CS0649, CS0169

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using ExamHelper.Binding;
using ExamHelper.Database;
using ExamHelper.Utilities;
using ExamHelper.Validation;

namespace ExamHelper.Templates
{
    public class FormCrudTemplate : Form
    {
        #region 1. Khai báo Control (Thay đổi theo đề thi)

        // Các Control nhập liệu
        private TextBox txtMa;
        private TextBox txtTen;
        private ComboBox cboDanhMuc;
        private DateTimePicker dtpNgay;
        private TextBox txtSoTien;

        // Các Control tìm kiếm
        private ComboBox cboTimDanhMuc;
        private TextBox txtTimKiem;
        private Button btnTim;
        private Button btnTatCa;

        // Bảng & Nút chức năng
        private DataGridView dgvDanhSach;
        private Button btnThem, btnSua, btnXoa, btnLuu, btnHuy;

        // Thống kê
        private Label lblTongSo, lblTongTien;

        // Biến trạng thái
        private bool _isAdding = false;
        private DataTable _dtData;

        // Cấu hình bảng dữ liệu
        private const string TABLE_NAME = "SanPham";      // Tên bảng chính
        private const string KEY_COLUMN = "MaSP";         // Cột khóa chính

        #endregion

        public FormCrudTemplate()
        {
            InitializeComponent();
            FormHelper.SetFormDefaults(this, "Quản lý dữ liệu");
        }

        private void InitializeComponent()
        {
            // Nếu bạn dùng Visual Studio kéo thả giao diện thì bỏ qua phần này
            this.Size = new System.Drawing.Size(850, 600);
            this.Load += Form_Load;
        }

        #region 2. Nạp dữ liệu ban đầu

        private void Form_Load(object sender, EventArgs e)
        {
            // Thiết lập chuỗi kết nối (thay đổi tên Database của đề)
            DbHelper.SetDefaultConnectionString("Server=.;Database=TenDatabase;Trusted_Connection=True;");

            LoadComboBoxes();
            LoadData();
            SetInputState(false);
        }

        private void LoadComboBoxes()
        {
            // Nạp danh mục (ví dụ: Phòng ban, Lớp học, Thể loại, Loại hàng)
            DataTable dt = DbHelper.GetDataTable("SELECT MaDanhMuc, TenDanhMuc FROM DanhMuc ORDER BY TenDanhMuc");
            ComboBoxHelper.Bind(cboDanhMuc, dt, "MaDanhMuc", "TenDanhMuc");
            ComboBoxHelper.BindWithAll(cboTimDanhMuc, dt, "MaDanhMuc", "TenDanhMuc", "-- Tất cả --");
        }

        private void LoadData(string whereClause = "", SqlParameter[] parameters = null)
        {
            // Viết câu lệnh SELECT hiển thị dữ liệu
            string sql = "SELECT * FROM " + TABLE_NAME + whereClause;
            _dtData = DbHelper.GetDataTable(sql, parameters);

            // Cấu hình hiển thị cột trên Grid (hoặc chỉ dùng GridHelper.Bind(dgvDanhSach, _dtData);)
            GridHelper.Bind(dgvDanhSach, _dtData,
                new GridHelper.GridColumn("MaSP", "Mã SP", 100),
                new GridHelper.GridColumn("TenSP", "Tên SP", 200),
                new GridHelper.GridColumn("DonGia", "Đơn giá", 120, "N0")
            );

            CapNhatThongKe();
        }

        private void CapNhatThongKe()
        {
            lblTongSo.Text = $"Tổng: {DataHelper.Count(_dtData)}";
            lblTongTien.Text = $"Tổng tiền: {DataHelper.Sum(_dtData, "DonGia"):N0} đ";
        }

        #endregion

        #region 3. Ánh xạ và chuyển trạng thái

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (_isAdding) return;
            DataRowView row = GridHelper.GetSelectedRow(dgvDanhSach);
            if (row != null)
            {
                FormMapper.LoadRow(row,
                    FormMapper.Map(txtMa, "MaSP"),
                    FormMapper.Map(txtTen, "TenSP"),
                    FormMapper.Map(cboDanhMuc, "MaDanhMuc"),
                    FormMapper.Map(dtpNgay, "NgayNhap"),
                    FormMapper.Map(txtSoTien, "DonGia")
                );
            }
        }

        private void SetInputState(bool isEditing)
        {
            FormHelper.EnableControls(isEditing, txtMa, txtTen, cboDanhMuc, dtpNgay, txtSoTien);
            txtMa.ReadOnly = !isEditing || !_isAdding;
            FormHelper.ToggleButtons(isEditing, btnThem, btnSua, btnXoa, btnLuu, btnHuy);
            dgvDanhSach.Enabled = !isEditing;
        }

        #endregion

        #region 4. Các nút Thêm, Sửa, Xóa, Hủy

        private void BtnThem_Click(object sender, EventArgs e)
        {
            _isAdding = true;
            FormMapper.ClearControls(txtMa, txtTen, txtSoTien);
            SetInputState(true);
            FormHelper.FocusControl(txtMa);
        }

        private void BtnSua_Click(object sender, EventArgs e)
        {
            if (!GridHelper.HasSelection(dgvDanhSach))
            {
                MessageHelper.NoSelection();
                return;
            }
            _isAdding = false;
            SetInputState(true);
            FormHelper.FocusControl(txtTen);
        }

        private void BtnHuy_Click(object sender, EventArgs e)
        {
            _isAdding = false;
            SetInputState(false);
            Dgv_SelectionChanged(null, null);
        }

        private void BtnXoa_Click(object sender, EventArgs e)
        {
            if (!GridHelper.HasSelection(dgvDanhSach))
            {
                MessageHelper.NoSelection();
                return;
            }

            string key = txtMa.Text.Trim();
            if (MessageHelper.ConfirmDelete(key))
            {
                if (CrudHelper.Delete(TABLE_NAME, KEY_COLUMN, key) > 0)
                {
                    MessageHelper.Success("xóa");
                    LoadData();
                }
            }
        }

        #endregion

        #region 5. Lưu dữ liệu (Kiểm tra & CRUD)

        private void BtnLuu_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra dữ liệu đầu vào (Validation)
            var valResult = Validator.ValidateAll(
                Validator.Required(txtMa, "Mã"),
                Validator.Required(txtTen, "Tên"),
                Validator.DecimalRange(txtSoTien, "Đơn giá", 0m, 1000000000m)
            );

            if (!valResult.IsValid)
            {
                MessageHelper.Warning(valResult.ErrorMessage);
                FormHelper.FocusControl(valResult.ErrorControl);
                return;
            }

            string key = txtMa.Text.Trim();

            // 2. Gom dữ liệu bằng FormMapper
            var values = FormMapper.GetValues(
                FormMapper.Map(txtMa, "MaSP"),
                FormMapper.Map(txtTen, "TenSP"),
                FormMapper.Map(cboDanhMuc, "MaDanhMuc"),
                FormMapper.Map(dtpNgay, "NgayNhap"),
                FormMapper.Map(txtSoTien, "DonGia")
            );

            if (_isAdding)
            {
                // Kiểm tra trùng khóa
                if (CrudHelper.Exists(TABLE_NAME, KEY_COLUMN, key))
                {
                    MessageHelper.DuplicateKey("Mã", key);
                    FormHelper.FocusControl(txtMa);
                    return;
                }

                if (CrudHelper.Insert(TABLE_NAME, values) > 0)
                {
                    MessageHelper.Success("thêm mới");
                    _isAdding = false;
                    SetInputState(false);
                    LoadData();
                }
            }
            else
            {
                // Khi sửa: loại bỏ cột khóa chính khỏi danh sách update
                values.Remove(KEY_COLUMN);

                if (CrudHelper.Update(TABLE_NAME, values, KEY_COLUMN, key) > 0)
                {
                    MessageHelper.Success("cập nhật");
                    _isAdding = false;
                    SetInputState(false);
                    LoadData();
                }
            }
        }

        #endregion

        #region 6. Tìm kiếm với FilterBuilder

        private void BtnTim_Click(object sender, EventArgs e)
        {
            var (where, parms) = FilterBuilder.New()
                .SkipIfAllSelected(cboTimDanhMuc, "MaDanhMuc")
                .Contains("TenSP", txtTimKiem.Text.Trim())
                .Build();

            LoadData(where, parms);
        }

        private void BtnTatCa_Click(object sender, EventArgs e)
        {
            txtTimKiem.Clear();
            cboTimDanhMuc.SelectedIndex = 0;
            LoadData();
        }

        #endregion
    }
}
