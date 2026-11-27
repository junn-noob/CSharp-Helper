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

### Cách A (Siêu tốc dùng `CrudBinder` — 1 khối trong `Form_Load`)
```csharp
private void Form1_Load(object sender, EventArgs e)
{
    string sql = @"SELECT nv.MaNV, nv.HoTen, CASE nv.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END AS GioiTinhHT,
                          nv.NgaySinh, pb.TenPB, nv.HeSoLuong, nv.NgayVaoLam,
                          CAST(ROUND(nv.HeSoLuong * 2340000, 0) AS DECIMAL(18,0)) AS Luong,
                          nv.GioiTinh, nv.MaPB
                   FROM NhanVien nv JOIN PhongBan pb ON nv.MaPB = pb.MaPB ORDER BY nv.MaNV";

    CrudBinder.For(this, "NhanVien", "MaNV", txtMaNV, dgvDanhSach, "nhân viên")
        .WithCombo(cboPhongBan, "SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB")
        .WithQuery(sql, /* GridColumn... */)
        .WithDefaultRadios(rdoNam)
        .WithExtraValidation(() => new[]
        {
            Validator.Pattern(txtMaNV, "Mã NV", @"^NV\d{3}$", "Mã NV phải có dạng NV + 3 chữ số!"),
            Validator.DecimalRange(txtHeSoLuong, "Hệ số lương", 2.34m, 8.0m),
            Validator.AgeRangeAtDate(dtpNgaySinh, dtpNgayVaoLam, "Tuổi", 18, 120, "Phải đủ 18 tuổi khi vào làm!")
        })
        .BindButtons(btnThem, btnSua, btnXoa, btnLamMoi, btnThoat)
        .Start();
}
```

### Cách B (Viết tách từng nút nếu muốn kiểm soát riêng)
```csharp
private DataTable _schema;

private void Form1_Load(object sender, EventArgs e)
{
    _schema = DbHelper.GetTableSchema("NhanVien");
    ComboBoxHelper.Bind(cboPhongBan, "SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB");
    LoadData();
}

private bool IsInputValid() => Validator.CheckSchema(this, _schema, dgvDanhSach,
    Validator.Pattern(txtMaNV, "Mã NV", @"^NV\d{3}$", "Mã NV phải có dạng NV + 3 chữ số!"),
    Validator.DecimalRange(txtHeSoLuong, "Hệ số lương", 2.34m, 8.0m),
    Validator.AgeRangeAtDate(dtpNgaySinh, dtpNgayVaoLam, "Tuổi", 18, 120, "Phải đủ 18 tuổi khi vào làm!"));

private void btnLamMoi_Click(object sender, EventArgs e)
{
    FormMapper.AutoClear(this, rdoNam);
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
    var filter = FilterBuilder.ForConnection()
        .SkipIfAllSelected(cboPhongBan, "nv.MaPB")
        .MapIndex(cboGioiTinh, "nv.GioiTinh", 1, 0);

    _dtKetQua = DbHelper.GetDataTable(sqlBase, filter, "pb.TenPB, nv.MaNV");
    GridHelper.Bind(dgvKetQua, _dtKetQua, /* GridColumn... */);

    lblSoNV.Text = $"Số nhân viên: {DataHelper.Count(_dtKetQua)}";
    lblTongLuong.Text = $"Tổng lương: {DataHelper.Sum(_dtKetQua, "Luong"):N0}";
    lblHeSoMax.Text = $"Hệ số cao nhất: {DataHelper.Max<double>(_dtKetQua, "HeSoLuong"):0.##}";
    if (!isInitial && _dtKetQua.Rows.Count == 0) MessageHelper.Info("Không tìm thấy dữ liệu phù hợp");
}
```

---

## 5. Báo cáo & Mẫu báo cáo (`.examrpt` / `.rdlc` / Excel `.xlsx` / PDF)
```csharp
// Cách 1: Mở FormReportViewer trực quan (bấm sửa thẳng trên preview, kéo-thả, lưu mẫu .examrpt, xuất .rdlc/.xlsx)
ReportHelper.ShowReport(_dtKetQua, "BẢNG LƯƠNG NHÂN VIÊN",
    $"Phòng ban: {cboPhongBan.Text}; Giới tính: {cboGioiTinh.Text}", "TenPB", "Phòng ban: ",
    new[] {
        new ReportColumn("MaNV", "Mã NV", HorizontalAlignment.Center),
        new ReportColumn("HoTen", "Họ tên"),
        new ReportColumn("NgaySinh", "Ngày sinh", "dd/MM/yyyy", HorizontalAlignment.Center),
        new ReportColumn("HeSoLuong", "Hệ số lương", "0.##", HorizontalAlignment.Right),
        new ReportColumn("Luong", "Lương", "N0", HorizontalAlignment.Right, ReportSummaryMode.Sum)
    }, "CÔNG TY TNHH AN PHÁT", this);

// Cách 2: Mở từ file mẫu .examrpt đã kéo-thả & lưu sẵn
ReportHelper.ShowReportFromTemplate(_dtKetQua, "MauBangLuong.examrpt", overrideFilterCondition: dieuKien, owner: this);

// Cách 3: Sinh file .rdlc tự động hoặc bind vào ReportViewer / Crystal Report
ReportHelper.ExportRdlcFile(_dtKetQua, "BẢNG LƯƠNG", groupByColumn: "TenPB", dataSetName: "dsNhanVien", filePath: "rptBangLuong.rdlc");
DataTable dtRpt = ReportHelper.PrepareReportTable(_dtKetQua);
ReportHelper.BindRdlc(reportViewer1, "dsNhanVien", dtRpt, new Dictionary<string, string> { { "pDieuKienLoc", dieuKien } });
ReportHelper.ExportRdlc(reportViewer1, "PDF", "BangLuong");
```
