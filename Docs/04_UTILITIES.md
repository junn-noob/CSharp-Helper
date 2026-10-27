# HƯỚNG DẪN CHI TIẾT: MODULE UTILITIES & UI HELPERS
> **Namespace:** `ExamHelper.Utilities`  
> **Các lớp chính:** `DataHelper`, `MessageHelper`, `FormHelper`

---

## 1. Lớp `DataHelper` - Thống kê & Tính toán trên DataTable

### 1.1 Mục đích & Khi nào dùng
- Trong các đề thi có phần **Thống kê sau lọc** (Đếm số lượng, Tổng tiền, Điểm TB, Điểm cao nhất...).
- `DataHelper` tính toán trực tiếp trên tập dữ liệu `DataTable` đang hiển thị trên giao diện thông qua `DataTable.Compute()`. Tốc độ cực nhanh và **không cần gửi thêm truy vấn lại CSDL SQL Server**.
- Cung cấp các hàm ép kiểu an toàn xử lý triệt để giá trị `DBNull.Value`.

### 1.2 Danh sách hàm quan trọng
```csharp
// Đếm số dòng (có thể kèm biểu thức lọc)
int DataHelper.Count(DataTable table, string filter = "");

// Tính tổng giá trị cột số
decimal DataHelper.Sum(DataTable table, string columnName, string filter = "");

// Tính trung bình cộng cột số
decimal DataHelper.Average(DataTable table, string columnName, string filter = "");

// Tìm giá trị lớn nhất (MAX)
T DataHelper.Max<T>(DataTable table, string columnName, string filter = "");

// Tìm giá trị nhỏ nhất (MIN)
T DataHelper.Min<T>(DataTable table, string columnName, string filter = "");

// Ép kiểu an toàn (không bao giờ lỗi DBNull hay NullReferenceException)
decimal DataHelper.ToDecimal(object value, decimal defaultValue = 0);
int DataHelper.ToInt(object value, int defaultValue = 0);
DateTime? DataHelper.ToDateTime(object value);
string DataHelper.ToString(object value, string defaultValue = "");
```

### 1.3 Ví dụ áp dụng thực tế
```csharp
private void CapNhatThongKe(DataTable dt)
{
    // Đếm số dòng
    lblSoLuong.Text = $"Số lượng: {DataHelper.Count(dt)}";

    // Tổng tiền (định dạng phân cách hàng nghìn)
    decimal tongTien = DataHelper.Sum(dt, "ThanhTien");
    lblTongTien.Text = $"Tổng tiền: {tongTien:N0} đ";

    // Điểm trung bình chung
    decimal diemTB = DataHelper.Average(dt, "DiemTB");
    lblDiemTB.Text = $"Điểm TB: {diemTB:N2}";

    // Lương cao nhất
    decimal luongMax = DataHelper.Max<decimal>(dt, "Luong");
    lblLuongMax.Text = $"Lương cao nhất: {luongMax:N0} đ";
}
```

---

## 2. Lớp `MessageHelper` - Chuẩn hóa hộp thoại thông báo

### 2.1 Mục đích & Khi nào dùng
- Thay thế toàn bộ các lệnh `MessageBox.Show(...)` dài dòng với tiêu đề, biểu tượng và nút bấm lặp đi lặp lại.
- Thống nhất các thông báo tiếng Việt chuẩn cho kỳ thi: Xác nhận xóa, Thành công, Cảnh báo, Chưa chọn dòng, Trùng khóa.

### 2.2 Danh sách hàm quan trọng
```csharp
// Hộp thoại thông tin / thông báo thành công
void MessageHelper.Info(string message, string title = "Thông báo");
void MessageHelper.Success(string action = ""); // "Thao tác {action} thành công!"

// Hộp thoại cảnh báo & lỗi
void MessageHelper.Warning(string message, string title = "Cảnh báo");
void MessageHelper.Error(string message, string title = "Lỗi");

// Hộp thoại xác nhận Yes/No (trả về bool)
bool MessageHelper.Confirm(string message, string title = "Xác nhận");
bool MessageHelper.ConfirmDelete(string itemName = ""); // "Bạn có chắc chắn muốn xóa {itemName} không?"

// Thông báo thao tác khi chưa chọn dòng trên bảng
void MessageHelper.NoSelection(string itemName = "dòng"); // "Vui lòng chọn {itemName} cần thao tác!"

// Thông báo dữ liệu không tìm thấy
void MessageHelper.NoData(); // "Không tìm thấy dữ liệu phù hợp."

// Thông báo trùng khóa
void MessageHelper.DuplicateKey(string fieldName, string value); // "{fieldName} '{value}' đã tồn tại!"
```

### 2.3 Ví dụ áp dụng thực tế

#### Ví dụ nút Xóa:
```csharp
private void btnXoa_Click(object sender, EventArgs e)
{
    if (!GridHelper.HasSelection(dgvSinhVien))
    {
        MessageHelper.NoSelection("sinh viên");
        return;
    }

    string maSV = txtMaSV.Text.Trim();
    if (MessageHelper.ConfirmDelete($"sinh viên {maSV}"))
    {
        CrudHelper.Delete("SinhVien", "MaSV", maSV);
        MessageHelper.Success("xóa sinh viên");
        LoadData();
    }
}
```

---

## 3. Lớp `FormHelper` - Quản lý giao diện & Trạng thái Form

### 3.1 Mục đích & Khi nào dùng
- Cài đặt form hiển thị giữa màn hình (`StartPosition = CenterScreen`).
- Đóng/mở hàng loạt Control (`EnableControls`).
- Đổi trạng thái các nút bấm Thêm/Sửa/Xóa/Lưu/Hủy (`ToggleButtons`).
- Đưa con trỏ chuột về ô bị lỗi và tự động bôi đen toàn bộ chữ (`FocusControl`).

### 3.2 Danh sách hàm quan trọng
```csharp
// Đặt form hiển thị giữa màn hình và đổi tiêu đề
void FormHelper.SetFormDefaults(Form form, string title = "");

// Bật/tắt hàng loạt Control cùng lúc
void FormHelper.EnableControls(bool enabled, params Control[] controls);

// Tự động đảo trạng thái giữa bộ nút (Thêm/Sửa/Xóa) và (Lưu/Hủy)
void FormHelper.ToggleButtons(bool isEditing, Button btnAdd, Button btnEdit, Button btnDelete, Button btnSave, Button btnCancel);

// Đưa con trỏ chuột vào Control (nếu là TextBox sẽ tự bôi đen toàn bộ)
void FormHelper.FocusControl(Control ctrl);
```

### 3.3 Ví dụ áp dụng thực tế
```csharp
private void SetFormState(bool isEditing)
{
    // Bật/tắt các ô nhập liệu
    FormHelper.EnableControls(isEditing, txtMaSV, txtHoTen, dtpNgaySinh, cboLop);

    // Không cho sửa ô khóa chính khi đang ở chế độ Sửa
    txtMaSV.ReadOnly = !isEditing || !_isAdding;

    // Tự động bật/tắt các nút Thêm, Sửa, Xóa vs Lưu, Hủy
    FormHelper.ToggleButtons(isEditing, btnThem, btnSua, btnXoa, btnLuu, btnHuy);

    // Khóa bảng khi đang chỉnh sửa
    dgvSinhVien.Enabled = !isEditing;
}
```
