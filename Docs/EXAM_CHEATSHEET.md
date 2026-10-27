# CẨM NANG ĐI THI WINFORMS - EXAMHELPER CHEATSHEET

Tập hợp những đoạn code ngắn (Snippets) dùng nhiều nhất trong đề thi, giúp bạn viết code trong 2-3 dòng thay vì 20-30 dòng.

---

## 1. Kết nối CSDL trong `Form_Load`
```csharp
private void Form1_Load(object sender, EventArgs e)
{
    // 1 dòng duy nhất để cấu hình kết nối
    DbHelper.SetDefaultConnectionString("Server=.;Database=TenDatabaseCuaDe;Trusted_Connection=True;");
    
    LoadComboBox();
    LoadData();
    SetState(false);
}
```

---

## 2. Nạp ComboBox (Nhập liệu & Tìm kiếm)
```csharp
// 1. Combo trên Form nhập liệu (Chỉ có danh sách thực)
DataTable dtPhong = DbHelper.GetDataTable("SELECT MaPhong, TenPhong FROM PhongBan");
ComboBoxHelper.Bind(cboPhong, dtPhong, "MaPhong", "TenPhong");

// 2. Combo trên Thanh tìm kiếm (Có thêm lựa chọn '-- Tất cả --' ở dòng đầu)
ComboBoxHelper.BindWithAll(cboTimPhong, dtPhong, "MaPhong", "TenPhong", "-- Tất cả phòng ban --");
```

---

## 3. Nạp DataGridView có định dạng
```csharp
DataTable dt = DbHelper.GetDataTable("SELECT * FROM NhanVien");

// Nạp kèm tiêu đề cột, độ rộng và định dạng ngày/tiền
GridHelper.Bind(dgvNhanVien, dt,
    new GridHelper.GridColumn("MaNV", "Mã nhân viên", 90),
    new GridHelper.GridColumn("HoTen", "Họ và tên", 180),
    new GridHelper.GridColumn("NgaySinh", "Ngày sinh", 100, "dd/MM/yyyy"),
    new GridHelper.GridColumn("Luong", "Lương cơ bản", 120, "N0")
);
```

---

## 4. Click dòng DataGridView đổ lên Form (SelectionChanged)
```csharp
private void dgv_SelectionChanged(object sender, EventArgs e)
{
    if (_isAdding) return;
    
    DataRowView row = GridHelper.GetSelectedRow(dgvNhanVien);
    if (row != null)
    {
        FormMapper.LoadRow(row,
            FormMapper.Map(txtMaNV, "MaNV"),
            FormMapper.Map(txtHoTen, "HoTen"),
            FormMapper.Map(cboPhong, "MaPhong"),
            FormMapper.Map(dtpNgaySinh, "NgaySinh"),
            FormMapper.Map(txtLuong, "Luong")
        );
    }
}
```

---

## 5. Nút Thêm mới
```csharp
private void btnThem_Click(object sender, EventArgs e)
{
    _isAdding = true;
    FormMapper.ClearControls(txtMaNV, txtHoTen, txtLuong);
    SetState(true);
    FormHelper.FocusControl(txtMaNV);
}
```

---

## 6. Nút Sửa
```csharp
private void btnSua_Click(object sender, EventArgs e)
{
    if (!GridHelper.HasSelection(dgvNhanVien))
    {
        MessageHelper.NoSelection("nhân viên");
        return;
    }
    _isAdding = false;
    SetState(true);
    FormHelper.FocusControl(txtHoTen);
}
```

---

## 7. Nút Xóa có xác nhận
```csharp
private void btnXoa_Click(object sender, EventArgs e)
{
    if (!GridHelper.HasSelection(dgvNhanVien))
    {
        MessageHelper.NoSelection();
        return;
    }

    string maNV = txtMaNV.Text.Trim();
    if (MessageHelper.ConfirmDelete($"nhân viên '{maNV}'"))
    {
        if (CrudHelper.Delete("NhanVien", "MaNV", maNV) > 0)
        {
            MessageHelper.Success("xóa");
            LoadData();
        }
    }
}
```

---

## 8. Nút Lưu (Kiểm tra dữ liệu + Thêm / Sửa)
```csharp
private void btnLuu_Click(object sender, EventArgs e)
{
    // 1. Kiểm tra toàn bộ điều kiện đề bài yêu cầu
    var val = Validator.ValidateAll(
        dtpNgayVaoLam.Value, // Truyền ngày tham chiếu nếu đề yêu cầu tính tuổi tại ngày vào làm
        Validator.Required(txtMaNV, "Mã nhân viên"),
        Validator.Pattern(txtMaNV, "Mã nhân viên", @"^NV\d{3}$", "Mã phải có định dạng NV001"),
        Validator.Required(txtHoTen, "Họ tên"),
        Validator.DecimalRange(txtHeSoLuong, "Hệ số lương", 1.0m, 15.0m),
        Validator.AgeRangeAtDate(dtpNgaySinh, "Tuổi nhân viên", 18, 60, "Phải từ 18-60 tuổi khi vào làm")
    );

    if (!val.IsValid)
    {
        MessageHelper.Warning(val.ErrorMessage);
        FormHelper.FocusControl(val.ErrorControl);
        return;
    }

    string ma = txtMaNV.Text.Trim();

    // 2. Gom dữ liệu từ Form thành Dictionary
    var values = FormMapper.GetValues(
        FormMapper.Map(txtMaNV, "MaNV"),
        FormMapper.Map(txtHoTen, "HoTen"),
        FormMapper.Map(cboPhong, "MaPhong"),
        FormMapper.Map(dtpNgaySinh, "NgaySinh"),
        FormMapper.Map(txtHeSoLuong, "HeSoLuong")
    );

    if (_isAdding)
    {
        // Kiểm tra trùng mã khi THÊM
        if (CrudHelper.Exists("NhanVien", "MaNV", ma))
        {
            MessageHelper.DuplicateKey("Mã nhân viên", ma);
            FormHelper.FocusControl(txtMaNV);
            return;
        }

        CrudHelper.Insert("NhanVien", values);
        MessageHelper.Success("thêm mới");
    }
    else
    {
        // Khi SỬA: bỏ cột khóa chính ra khỏi set values
        values.Remove("MaNV");
        CrudHelper.Update("NhanVien", values, "MaNV", ma);
        MessageHelper.Success("cập nhật");
    }

    _isAdding = false;
    SetState(false);
    LoadData();
}
```

---

## 9. Tìm kiếm đa điều kiện với `FilterBuilder`
```csharp
private void btnTimKiem_Click(object sender, EventArgs e)
{
    var (where, parms) = FilterBuilder.New()
        // Bỏ qua nếu chọn '-- Tất cả --' (SelectedIndex = 0)
        .SkipIfAllSelected(cboTimPhong, "MaPhong")
        // Bỏ qua nếu để trống ô họ tên
        .Contains("HoTen", txtTimTen.Text.Trim())
        .Build();

    // Nối trực tiếp mệnh đề WHERE an toàn
    string sql = "SELECT * FROM NhanVien" + where;
    DataTable dt = DbHelper.GetDataTable(sql, parms);
    GridHelper.Bind(dgvNhanVien, dt);
}
```

---

## 10. Thống kê dữ liệu sau lọc
```csharp
private void ThongKe(DataTable dt)
{
    lblSoLuong.Text = $"Tổng số: {DataHelper.Count(dt)}";
    lblTongTien.Text = $"Tổng tiền: {DataHelper.Sum(dt, "Luong"):N0} đ";
    lblTrungBinh.Text = $"Trung bình: {DataHelper.Average(dt, "Luong"):N0} đ";
    lblCaoNhat.Text = $"Cao nhất: {DataHelper.Max<decimal>(dt, "Luong"):N0} đ";
}
```

---

## 11. Hàm quản lý bật/tắt nút và ô nhập liệu
```csharp
private void SetState(bool isEditing)
{
    // Bật/tắt các ô nhập
    FormHelper.EnableControls(isEditing, txtMaNV, txtHoTen, cboPhong, dtpNgaySinh, txtLuong);
    
    // Khóa ô Mã khi đang ở chế độ Sửa
    txtMaNV.ReadOnly = !isEditing || !_isAdding;
    
    // Tự động bật Lưu/Hủy khi đang sửa, bật Thêm/Sửa/Xóa khi bình thường
    FormHelper.ToggleButtons(isEditing, btnThem, btnSua, btnXoa, btnLuu, btnHuy);
    
    dgvNhanVien.Enabled = !isEditing;
}
```
