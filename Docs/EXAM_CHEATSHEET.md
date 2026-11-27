# ⚡ CHEAT SHEET PHÒNG THI — EXAMHELPER PRO

Bảng snippet rút gọn tối đa để tra cứu nhanh theo từng phần trong bài thi.

---

## 1. Kết nối CSDL (chọn 1 cách)
```csharp
// Cách A: Đặt <connectionStrings> trong App.config (trên .NET 8/10 bật Copy if newer) -> không cần code kết nối!
// Cách B: Gán trực tiếp 1 dòng trong Form_Load:
DbHelper.ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=TenDB;Trusted_Connection=True;";
```

---

## 2. Form 1 — Quản lý chính (CRUD + Validate)
```csharp
private DataTable _schema;

private void Form1_Load(object sender, EventArgs e)
{
    _schema = DbHelper.GetTableSchema("NhanVien");
    ComboBoxHelper.Bind(cboPhongBan, "SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB");
    LoadData();
}

private void LoadData()
{
    string sql = @"SELECT nv.MaNV, nv.HoTen, CASE nv.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END AS GioiTinhHT,
                          nv.NgaySinh, pb.TenPB, nv.HeSoLuong, nv.NgayVaoLam,
                          CAST(ROUND(nv.HeSoLuong * 2340000, 0) AS DECIMAL(18,0)) AS Luong,
                          nv.GioiTinh, nv.MaPB
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
    var row = GridHelper.GetSelectedRow(dgvDanhSach);
    if (row == null) return;
    FormMapper.AutoLoad(this, row); // Tự nhận Tag="GioiTinh:1/0", ValueMember="MaPB" và tiền tố txt/dtp/cbo
    txtMaNV.ReadOnly = true;
}

private bool IsInputValid()
{
    var val = Validator.FromSchema(this, _schema,
        Validator.Pattern(txtMaNV, "Mã NV", @"^NV\d{3}$", "Mã NV phải có dạng NV + 3 chữ số!"),
        Validator.DecimalRange(txtHeSoLuong, "Hệ số lương", 2.34m, 8.0m),
        Validator.AgeRangeAtDate(dtpNgaySinh, dtpNgayVaoLam, "Tuổi", 18, 120, "Phải đủ 18 tuổi khi vào làm!"));
    if (!val.IsValid) { MessageHelper.Warning(val.ErrorMessage); FormHelper.FocusControl(val.ErrorControl); }
    return val.IsValid;
}

private void btnThem_Click(object sender, EventArgs e)
{
    if (!IsInputValid()) return;
    string ma = txtMaNV.Text.Trim();
    if (CrudHelper.Exists("NhanVien", "MaNV", ma)) { MessageHelper.DuplicateKey("Mã NV", ma); FormHelper.FocusControl(txtMaNV); return; }
    if (CrudHelper.Insert("NhanVien", FormMapper.AutoGetValues(this, _schema)) > 0) { MessageHelper.Success("thêm mới"); LoadData(); }
}

private void btnSua_Click(object sender, EventArgs e)
{
    if (!GridHelper.HasSelection(dgvDanhSach)) { MessageHelper.NoSelection("nhân viên"); return; }
    if (!IsInputValid()) return;
    if (CrudHelper.Update("NhanVien", FormMapper.AutoGetValues(this, _schema), "MaNV", txtMaNV.Text.Trim()) > 0)
    { MessageHelper.Success("cập nhật"); LoadData(); }
}

private void btnXoa_Click(object sender, EventArgs e)
{
    if (!GridHelper.HasSelection(dgvDanhSach)) { MessageHelper.NoSelection("nhân viên"); return; }
    string ma = txtMaNV.Text.Trim();
    if (MessageHelper.ConfirmDelete($"nhân viên {ma}") && CrudHelper.Delete("NhanVien", "MaNV", ma) > 0)
    { MessageHelper.Success("xóa"); LoadData(); }
}

private void btnLamMoi_Click(object sender, EventArgs e)
{
    FormMapper.AutoClear(this);
    rdoNam.Checked = true;
    GridHelper.ClearSelection(dgvDanhSach);
    txtMaNV.ReadOnly = false;
    txtMaNV.Focus();
}
```

---

## 3. Form phụ quản lý nhanh bất kỳ bảng đơn nào (1 dòng)
```csharp
FormGenericManager.Open("PhongBan");
```

---

## 4. Form 2 — Tìm kiếm đa điều kiện & Thống kê sau lọc
```csharp
private void Form2_Load(object sender, EventArgs e)
{
    ComboBoxHelper.BindWithAll(cboPhongBan, "SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB", "Tất cả");
    ComboBoxHelper.BindItems(cboGioiTinh, "Tất cả", "Nam", "Nữ");
    TimKiem(true);
}

private void TimKiem(bool isInitial)
{
    var filter = FilterBuilder.ForConnection().SkipIfAllSelected(cboPhongBan, "nv.MaPB");
    if (cboGioiTinh.SelectedIndex == 1) filter.Equal("nv.GioiTinh", 1);
    else if (cboGioiTinh.SelectedIndex == 2) filter.Equal("nv.GioiTinh", 0);

    _dtKetQua = DbHelper.GetDataTable(sqlBase, filter, "pb.TenPB, nv.MaNV");
    GridHelper.Bind(dgvKetQua, _dtKetQua, /* GridColumn... */);

    lblSoNV.Text = $"Số nhân viên: {DataHelper.Count(_dtKetQua)}";
    lblTongLuong.Text = $"Tổng lương: {DataHelper.Sum(_dtKetQua, "Luong"):N0}";
    lblHeSoMax.Text = $"Hệ số cao nhất: {DataHelper.Max<double>(_dtKetQua, "HeSoLuong"):0.##}";
    if (!isInitial && _dtKetQua.Rows.Count == 0) MessageHelper.Info("Không tìm thấy dữ liệu phù hợp");
}
```

---

## 5. Báo cáo & Xuất Excel / PDF
```csharp
// Cách 1: Mở FormReportViewer dùng chung (kéo-thả cột, gom nhóm, A4 Dọc/Ngang, xuất .xlsx OpenXML thật)
ReportHelper.ShowReport(_dtKetQua, "BẢNG LƯƠNG NHÂN VIÊN",
    $"Phòng ban: {cboPhongBan.Text}; Giới tính: {cboGioiTinh.Text}", "TenPB", "Phòng ban: ",
    new[] {
        new ReportColumn("MaNV", "Mã NV", HorizontalAlignment.Center),
        new ReportColumn("HoTen", "Họ tên"),
        new ReportColumn("NgaySinh", "Ngày sinh", "dd/MM/yyyy", HorizontalAlignment.Center),
        new ReportColumn("HeSoLuong", "Hệ số lương", "0.##", HorizontalAlignment.Right),
        new ReportColumn("Luong", "Lương", "N0", HorizontalAlignment.Right, ReportSummaryMode.Sum)
    }, "CÔNG TY TNHH AN PHÁT", this);

// Cách 2: Nếu đề bắt buộc RDLC / Crystal Report
DataTable dtRpt = ReportHelper.PrepareReportTable(_dtKetQua);
ReportHelper.BindRdlc(reportViewer1, "dsNhanVien", dtRpt, new Dictionary<string, string> { { "pDieuKienLoc", dieuKien } });
ReportHelper.ExportRdlc(reportViewer1, "PDF", "BangLuong");
```
