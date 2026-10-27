# HƯỚNG DẪN CHI TIẾT: MODULE BINDING & FORM MAPPING
> **Namespace:** `ExamHelper.Binding`  
> **Các lớp chính:** `ComboBoxHelper`, `GridHelper`, `FormMapper`

---

## 1. Lớp `ComboBoxHelper` - Nạp ComboBox tốc độ

### 1.1 Mục đích & Khi nào dùng
- Gán dữ liệu (`DataSource`, `ValueMember`, `DisplayMember`) từ `DataTable` vào `ComboBox` chỉ trong 1 dòng lệnh.
- Tự động tạo bản sao (`.Copy()`) để tránh lỗi xung đột tham chiếu khi bind cùng 1 bảng cho nhiều ComboBox.
- Hỗ trợ hàm `BindWithAll` chuyên dụng cho thanh tìm kiếm: tự động chèn mục `-- Tất cả --` vào vị trí đầu tiên (index 0).

### 1.2 Danh sách hàm quan trọng
```csharp
// Nạp ComboBox thông thường (form nhập liệu)
void ComboBoxHelper.Bind(ComboBox combo, DataTable data, string valueMember, string displayMember);

// Nạp ComboBox tìm kiếm kèm dòng "Tất cả"
void ComboBoxHelper.BindWithAll(ComboBox combo, DataTable data, string valueMember, string displayMember, 
                                string allText = "-- Tất cả --", object allValue = null);

// Lấy giá trị khóa của dòng đang chọn (ép kiểu an toàn, chống null)
T ComboBoxHelper.GetSelectedValue<T>(ComboBox combo);

// Lấy text hiển thị của dòng đang chọn
string ComboBoxHelper.GetSelectedText(ComboBox combo);

// Kiểm tra có đang chọn dòng "-- Tất cả --" không (SelectedIndex == 0)
bool ComboBoxHelper.IsAllSelected(ComboBox combo);
```

### 1.3 Ví dụ áp dụng thực tế
```csharp
private void LoadComboBoxes()
{
    DataTable dtLop = DbHelper.GetDataTable("SELECT MaLop, TenLop FROM LopHoc ORDER BY TenLop");

    // 1. Dành cho Form nhập liệu (Chỉ gồm các lớp thực)
    ComboBoxHelper.Bind(cboLopHoc, dtLop, "MaLop", "TenLop");

    // 2. Dành cho Form tìm kiếm (Có dòng '-- Tất cả lớp --' ở đầu)
    ComboBoxHelper.BindWithAll(cboTimLop, dtLop, "MaLop", "TenLop", "-- Tất cả lớp --");
}

// Khi cần lấy mã lớp đang chọn:
string maLop = ComboBoxHelper.GetSelectedValue<string>(cboLopHoc);
```

---

## 2. Lớp `GridHelper` - Nạp & Định dạng DataGridView

### 2.1 Mục đích & Khi nào dùng
- Cấu hình hiển thị cột cho `DataGridView` nhanh chóng: đặt tiêu đề tiếng Việt, chiều rộng pixel, định dạng ngày tháng (`dd/MM/yyyy`) hoặc tiền tệ (`N0`).
- Tự động gán thuộc tính `Name` bằng `DataField` giúp tránh triệt để lỗi không tìm thấy ô dữ liệu.
- Thiết lập chế độ bảng đọc chuẩn cho đề thi (`SetReadOnly`): chọn nguyên dòng, không cho thêm/xóa trực tiếp.
- Lấy dòng đang chọn (`GetSelectedRow`) hoặc kiểm tra có dòng được chọn hay không (`HasSelection`).

### 2.2 Danh sách hàm quan trọng
```csharp
// Khởi tạo cột hiển thị
new GridHelper.GridColumn(string dataField, string headerText, int width = -1, string format = null);

// Nạp DataGridView kèm cấu hình cột
void GridHelper.Bind(DataGridView grid, DataTable data, params GridColumn[] columns);

// Thiết lập DataGridView chuẩn đi thi (ReadOnly, FullRowSelect, SingleSelect)
void GridHelper.SetReadOnly(DataGridView grid);

// Lấy DataRowView của dòng đang chọn
DataRowView GridHelper.GetSelectedRow(DataGridView grid);

// Kiểm tra xem người dùng đã chọn dòng nào trên bảng chưa
bool GridHelper.HasSelection(DataGridView grid);

// Lấy giá trị tại 1 cột của dòng đang chọn
T GridHelper.GetSelectedValue<T>(DataGridView grid, string columnName);
```

### 2.3 Ví dụ áp dụng thực tế
```csharp
private void LoadData()
{
    DataTable dt = DbHelper.GetDataTable("SELECT * FROM SinhVien");

    // Nạp bảng kèm tùy chỉnh tiêu đề và định dạng
    GridHelper.Bind(dgvSinhVien, dt,
        new GridHelper.GridColumn("MaSV", "Mã sinh viên", 90),
        new GridHelper.GridColumn("HoTen", "Họ và tên", 180),
        new GridHelper.GridColumn("NgaySinh", "Ngày sinh", 100, "dd/MM/yyyy"),
        new GridHelper.GridColumn("DiemTB", "Điểm TB", 80, "N1"),
        new GridHelper.GridColumn("HocPhi", "Học phí", 120, "N0")
    );

    // Đặt chế độ chuẩn
    GridHelper.SetReadOnly(dgvSinhVien);
}
```

---

## 3. Lớp `FormMapper` - Ánh xạ 2 chiều Form ⇄ Dữ liệu

### 3.1 Mục đích & Khi nào dùng
- **Xóa bỏ 80% code gán từng control bằng tay.**
- Đọc dữ liệu từ `DataRowView` nạp lên toàn bộ các Control trên Form chỉ trong **1 dòng code** (hỗ trợ tự động: `TextBox`, `ComboBox`, `DateTimePicker`, `CheckBox`, `RadioButton`, `NumericUpDown`).
- Gom giá trị từ các Control trên Form thành một `Dictionary<string, object>` để nạp thẳng vào `CrudHelper.Insert` hoặc `CrudHelper.Update`.
- Reset trắng toàn bộ Form chỉ với `FormMapper.ClearControls(...)`.

### 3.2 Danh sách hàm quan trọng
```csharp
// Tạo cặp ánh xạ Control - Tên Cột trong CSDL
FieldMapping FormMapper.Map(Control control, string columnName);

// Đổ dữ liệu từ DataRow/DataRowView lên các Control
void FormMapper.LoadRow(DataRowView rowView, params FieldMapping[] mappings);

// Đọc toàn bộ giá trị từ các Control trả về Dictionary
Dictionary<string, object> FormMapper.GetValues(params FieldMapping[] mappings);

// Xóa trắng / reset giá trị mặc định cho danh sách Control
void FormMapper.ClearControls(params Control[] controls);
```

### 3.3 Ví dụ áp dụng thực tế

#### Khi Click chọn 1 dòng trên DataGridView:
```csharp
private void dgvSinhVien_SelectionChanged(object sender, EventArgs e)
{
    DataRowView row = GridHelper.GetSelectedRow(dgvSinhVien);
    if (row == null) return;

    // 1 DÒNG DUY NHẤT để nạp toàn bộ Form:
    FormMapper.LoadRow(row,
        FormMapper.Map(txtMaSV, "MaSV"),
        FormMapper.Map(txtHoTen, "HoTen"),
        FormMapper.Map(dtpNgaySinh, "NgaySinh"),
        FormMapper.Map(cboLop, "MaLop"),
        FormMapper.Map(chkGioiTinh, "GioiTinh"),
        FormMapper.Map(numDiem, "DiemTB")
    );
    txtMaSV.ReadOnly = true;
}
```

#### Khi Lưu dữ liệu (Thêm mới):
```csharp
private void btnLuu_Click(object sender, EventArgs e)
{
    // Gom dữ liệu từ Form:
    var values = FormMapper.GetValues(
        FormMapper.Map(txtMaSV, "MaSV"),
        FormMapper.Map(txtHoTen, "HoTen"),
        FormMapper.Map(dtpNgaySinh, "NgaySinh"),
        FormMapper.Map(cboLop, "MaLop")
    );

    CrudHelper.Insert("SinhVien", values);
}
```
