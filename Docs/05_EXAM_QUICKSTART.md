# HƯỚNG DẪN NHANH: HOÀN THÀNH BÀI THI 75 PHÚT TRONG 5 BƯỚC
> **Bí quyết thi thực hành WinForms & SQL Server đạt điểm tối đa**

---

## ⏱️ Phân bổ thời gian trong phòng thi (75 phút)

- **00 - 10 phút:** Chạy script tạo CSDL trong SQL Server, tạo Project WinForms và kéo thả giao diện.
- **10 - 15 phút:** Add reference thư viện `ExamHelper.dll` hoặc nhúng source `ExamHelper`.
- **15 - 35 phút:** Hoàn thành Form 1 (Nạp DB, Đổ dữ liệu dòng chọn, Thêm, Sửa, Xóa).
- **35 - 55 phút:** Hoàn thành Form 2 (Tìm kiếm đa điều kiện, Thống kê dữ liệu sau lọc).
- **55 - 70 phút:** Hoàn thành Form Báo cáo hoặc các câu hỏi phụ.
- **70 - 75 phút:** Kiểm tra lại tất cả trường hợp biên (bảng rỗng, dữ liệu sai, phím tắt Alt).

---

## 🚀 5 Bước làm bài thi chuẩn mẫu với ExamHelper

### Bước 1: Kết nối CSDL trong sự kiện `Form_Load` (1 phút)
Mở file code của Form chính (ví dụ `frmQuanLy.cs`), thêm sự kiện `Form_Load`:

```csharp
private void Form1_Load(object sender, EventArgs e)
{
    // Cấu hình chuỗi kết nối
    DbHelper.SetDefaultConnectionString("Server=.;Database=TenCsdlCuaDe;Trusted_Connection=True;");

    // Nạp ComboBox danh mục (Lớp, Phòng ban, Thể loại...)
    ComboBoxHelper.Bind(cboDanhMuc, DbHelper.GetDataTable("SELECT MaDM, TenDM FROM DanhMuc"), "MaDM", "TenDM");

    LoadData();
}

private void LoadData()
{
    string sql = "SELECT * FROM SinhVien";
    GridHelper.Bind(dgvSinhVien, DbHelper.GetDataTable(sql),
        new GridHelper.GridColumn("MaSV", "Mã sinh viên", 100),
        new GridHelper.GridColumn("HoTen", "Họ và tên", 180),
        new GridHelper.GridColumn("DiemTB", "Điểm TB", 80, "N1")
    );
    GridHelper.SetReadOnly(dgvSinhVien);
}
```

---

### Bước 2: Đổ dữ liệu từ DataGridView lên Form (1 phút)
Click đúp vào `DataGridView` để tạo sự kiện `SelectionChanged`:

```csharp
private void dgvSinhVien_SelectionChanged(object sender, EventArgs e)
{
    DataRowView row = GridHelper.GetSelectedRow(dgvSinhVien);
    if (row == null) return;

    FormMapper.LoadRow(row,
        FormMapper.Map(txtMaSV, "MaSV"),
        FormMapper.Map(txtHoTen, "HoTen"),
        FormMapper.Map(cboDanhMuc, "MaDM"),
        FormMapper.Map(dtpNgaySinh, "NgaySinh")
    );
    txtMaSV.ReadOnly = true; // Khóa ô mã khi đang xem/sửa
}
```

---

### Bước 3: Nút Thêm mới (2 phút)
Click đúp vào nút Thêm:

```csharp
private void btnThem_Click(object sender, EventArgs e)
{
    // 1. Kiểm tra tính hợp lệ
    var val = Validator.ValidateAll(
        Validator.Required(txtMaSV, "Mã sinh viên"),
        Validator.Pattern(txtMaSV, "Mã sinh viên", @"^SV\d{3}$", "Mã phải có dạng SV001!"),
        Validator.Required(txtHoTen, "Họ tên"),
        Validator.DecimalRange(txtDiem, "Điểm", 0m, 10m)
    );

    if (!val.IsValid)
    {
        MessageHelper.Warning(val.ErrorMessage);
        FormHelper.FocusControl(val.ErrorControl);
        return;
    }

    // 2. Kiểm tra trùng mã
    string ma = txtMaSV.Text.Trim();
    if (CrudHelper.Exists("SinhVien", "MaSV", ma))
    {
        MessageHelper.DuplicateKey("Mã sinh viên", ma);
        FormHelper.FocusControl(txtMaSV);
        return;
    }

    // 3. Thêm vào CSDL
    var values = FormMapper.GetValues(
        FormMapper.Map(txtMaSV, "MaSV"),
        FormMapper.Map(txtHoTen, "HoTen"),
        FormMapper.Map(cboDanhMuc, "MaDM")
    );

    if (CrudHelper.Insert("SinhVien", values) > 0)
    {
        MessageHelper.Success("thêm mới");
        LoadData();
    }
}
```

---

### Bước 4: Nút Sửa & Xóa (2 phút)
Click đúp vào nút Sửa và nút Xóa:

```csharp
// SỬA:
private void btnSua_Click(object sender, EventArgs e)
{
    if (!GridHelper.HasSelection(dgvSinhVien)) { MessageHelper.NoSelection(); return; }

    var values = FormMapper.GetValues(
        FormMapper.Map(txtHoTen, "HoTen"),
        FormMapper.Map(cboDanhMuc, "MaDM")
    );

    string ma = txtMaSV.Text.Trim();
    if (CrudHelper.Update("SinhVien", values, "MaSV", ma) > 0)
    {
        MessageHelper.Success("cập nhật");
        LoadData();
    }
}

// XÓA:
private void btnXoa_Click(object sender, EventArgs e)
{
    if (!GridHelper.HasSelection(dgvSinhVien)) { MessageHelper.NoSelection(); return; }

    string ma = txtMaSV.Text.Trim();
    if (MessageHelper.ConfirmDelete(ma) && CrudHelper.Delete("SinhVien", "MaSV", ma) > 0)
    {
        MessageHelper.Success("xóa");
        LoadData();
    }
}
```

---

### Bước 5: Tìm kiếm đa điều kiện & Thống kê sau lọc (3 phút)
Trong Form Tìm kiếm / Thống kê:

```csharp
private void btnTimKiem_Click(object sender, EventArgs e)
{
    var (where, parms) = FilterBuilder.New()
        .SkipIfAllSelected(cboTimDanhMuc, "MaDM")
        .Contains("HoTen", txtTimTen.Text.Trim())
        .Build();

    DataTable dt = DbHelper.GetDataTable("SELECT * FROM SinhVien" + where, parms);
    GridHelper.Bind(dgvKetQua, dt);

    // Cập nhật thống kê tức thì trên dữ liệu đang hiển thị:
    lblTongSo.Text = $"Tổng số: {DataHelper.Count(dt)}";
    lblDiemTB.Text = $"Điểm TB: {DataHelper.Average(dt, "DiemTB"):N1}";
    lblCaoNhat.Text = $"Điểm cao nhất: {DataHelper.Max<decimal>(dt, "DiemTB"):N1}";
}
```

---

## 🎯 Mẫu kiểm tra Checklist trước khi nộp bài
- [ ] Bật/tắt các nút đúng logic (Đang sửa thì không bấm Thêm, không sửa khóa).
- [ ] Các nút có phím tắt Alt (`&Thêm`, `&Sửa`, `&Xóa`, `&Tìm kiếm`, `Th&oát`).
- [ ] Khi nhập sai dữ liệu, con trỏ chuột tự động focus vào ô sai.
- [ ] Khi xóa có hộp thoại xác nhận Yes/No.
- [ ] DataGridView hiển thị định dạng chuẩn (`dd/MM/yyyy` cho ngày, `N0` cho tiền tệ).
- [ ] Bảng rỗng không bị văng lỗi `NullReferenceException`.
