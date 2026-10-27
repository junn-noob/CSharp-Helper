# HƯỚNG DẪN TRA CỨU API - EXAMHELPER PRO

Tài liệu chi tiết toàn bộ các lớp, hàm và cách dùng trong thư viện `ExamHelper`.

---

## 1. ExamHelper.Database

### 1.1 `DbHelper` (Thao tác cơ sở dữ liệu SQL Server)

Quản lý kết nối và thực thi các truy vấn SQL an toàn với tham số (`SqlParameter`).

| Hàm | Kiểu trả về | Mô tả |
|---|---|---|
| `SetDefaultConnectionString(string connStr)` | `void` | Đăng ký chuỗi kết nối mặc định (khóa `"default"`). |
| `SetConnectionString(string name, string connStr)` | `void` | Đăng ký chuỗi kết nối với tên định danh riêng (hỗ trợ nhiều database). |
| `GetConnection(string dbName = "default")` | `SqlConnection` | Tạo và mở một kết nối mới. |
| `GetDataTable(string sql, params SqlParameter[] parameters)` | `DataTable` | Đọc dữ liệu ra DataTable bằng câu lệnh SELECT. |
| `GetDataTable(string sql, string dbName, params SqlParameter[] parameters)` | `DataTable` | Đọc dữ liệu từ database chỉ định. |
| `ExecuteNonQuery(string sql, params SqlParameter[] parameters)` | `int` | Thực thi INSERT/UPDATE/DELETE. Trả về số dòng bị ảnh hưởng. |
| `ExecuteScalar<T>(string sql, params SqlParameter[] parameters)` | `T` | Lấy giá trị đầu tiên của dòng đầu tiên (chuyển đổi kiểu an toàn). |
| `Exists(string sql, params SqlParameter[] parameters)` | `bool` | Kiểm tra có ít nhất 1 dòng thỏa mãn truy vấn hay không. |
| `Param(string name, object value)` | `SqlParameter` | Tạo tham số SQL, tự động chuyển `null` thành `DBNull.Value`. |
| `ParamLike(string name, string value)` | `SqlParameter` | Tạo tham số dạng `%{value}%` cho phép tìm kiếm LIKE. |
| `ParamInt(string name, int? value)` | `SqlParameter` | Tạo tham số số nguyên (an toàn với null). |
| `ParamDecimal(string name, decimal? value)` | `SqlParameter` | Tạo tham số số thực (an toàn với null). |
| `ParamDate(string name, DateTime? value)` | `SqlParameter` | Tạo tham số ngày tháng (an toàn với null). |

**Ví dụ:**
```csharp
// Đăng ký kết nối
DbHelper.SetDefaultConnectionString("Server=.;Database=QLNhanVien;Trusted_Connection=True;");

// Đọc dữ liệu có tham số
var dt = DbHelper.GetDataTable("SELECT * FROM NhanVien WHERE MaPhong = @p", 
    DbHelper.Param("@p", "IT"));

// Lấy 1 giá trị
int count = DbHelper.ExecuteScalar<int>("SELECT COUNT(*) FROM NhanVien");
```

---

### 1.2 `CrudHelper` (CRUD tổng quát an toàn)

Thực hiện Thêm, Sửa, Xóa và kiểm tra trùng khóa không cần viết tay SQL. Có bộ lọc whitelist chống SQL Injection qua tên bảng/cột.

| Hàm | Kiểu trả về | Mô tả |
|---|---|---|
| `Insert(table, valuesDict, dbName = "default")` | `int` | Thêm mới một dòng dữ liệu từ `Dictionary<string, object>`. |
| `Update(table, valuesDict, keyCol, keyVal, dbName = "default")` | `int` | Cập nhật dòng theo khóa chính `keyCol = keyVal`. |
| `Delete(table, keyCol, keyVal, dbName = "default")` | `int` | Xóa dòng dữ liệu theo khóa chính. |
| `Exists(table, col, val, dbName = "default")` | `bool` | Kiểm tra giá trị đã tồn tại trong cột chưa (dùng khi Thêm mới). |
| `ExistsExcept(table, checkCol, checkVal, keyCol, keyVal, dbName = "default")` | `bool` | Kiểm tra trùng giá trị nhưng loại trừ dòng hiện tại (dùng khi Sửa). |

**Ví dụ:**
```csharp
// Thêm mới
var values = new Dictionary<string, object> {
    { "MaNV", "NV001" },
    { "HoTen", "Nguyễn Văn A" },
    { "Luong", 15000000m }
};
CrudHelper.Insert("NhanVien", values);

// Cập nhật
values.Remove("MaNV"); // Không sửa khóa
CrudHelper.Update("NhanVien", values, "MaNV", "NV001");

// Xóa
CrudHelper.Delete("NhanVien", "MaNV", "NV001");
```

---

### 1.3 `FilterBuilder` (Xây dựng bộ lọc tìm kiếm linh hoạt)

Tạo chuỗi `WHERE` và mảng `SqlParameter` tự động. **Tự động bỏ qua điều kiện** nếu người dùng để trống hoặc chọn "-- Tất cả --".

| Hàm | Trả về | Mô tả |
|---|---|---|
| `FilterBuilder.New()` | `FilterBuilder` | Khởi tạo đối tượng builder mới. |
| `Equal(column, value)` | `this` | Thêm điều kiện `[column] = @param` (bỏ qua nếu value null/rỗng/0). |
| `NotEqual(column, value)` | `this` | Thêm điều kiện `<>`. |
| `Contains(column, value)` | `this` | Thêm điều kiện `LIKE %@param%`. |
| `StartsWith(column, value)` | `this` | Thêm điều kiện `LIKE @param%`. |
| `EndsWith(column, value)` | `this` | Thêm điều kiện `LIKE %@param`. |
| `GreaterThan(column, value)` | `this` | Thêm điều kiện `>`. |
| `GreaterThanOrEqual(column, value)` | `this` | Thêm điều kiện `>=`. |
| `LessThan(column, value)` | `this` | Thêm điều kiện `<`. |
| `LessThanOrEqual(column, value)` | `this` | Thêm điều kiện `<=`. |
| `Between(column, min, max)` | `this` | Thêm điều kiện `BETWEEN @min AND @max`. |
| `In(column, params values)` | `this` | Thêm điều kiện `IN (@p1, @p2, ...)`. |
| `IsNull(column)` / `IsNotNull(column)` | `this` | Thêm điều kiện `IS NULL` / `IS NOT NULL`. |
| `SkipIfAllSelected(combo, column)` | `this` | Tự động thêm `Equal` chỉ khi `combo.SelectedIndex > 0`. |
| `Raw(condition, params sqlParams)` | `this` | Thêm biểu thức SQL tùy ý (phục vụ điều kiện OR phức tạp). |
| `Build()` | `(string WhereClause, SqlParameter[] Parameters)` | Trả về bộ tuple sẵn sàng nạp vào câu SELECT. |

**Ví dụ:**
```csharp
var (where, parms) = FilterBuilder.New()
    .SkipIfAllSelected(cboPhongBan, "nv.MaPhong")
    .Contains("nv.HoTen", txtHoTen.Text.Trim())
    .Between("nv.Luong", numMin.Value, numMax.Value)
    .Build();

string sql = "SELECT * FROM NhanVien nv" + where;
DataTable dt = DbHelper.GetDataTable(sql, parms);
```

---

## 2. ExamHelper.Binding

### 2.1 `ComboBoxHelper` (Nạp ComboBox nhanh)

| Hàm | Mô tả |
|---|---|
| `Bind(combo, dataTable, valueCol, displayCol)` | Nạp ComboBox chuẩn với cột Khóa và cột Hiển thị. |
| `BindWithAll(combo, dataTable, valueCol, displayCol, allText = "-- Tất cả --")` | Tự động nhân bản bảng và chèn dòng `"Tất cả"` tại vị trí 0. |
| `GetSelectedValue<T>(combo)` | Lấy giá trị khóa của dòng đang chọn (ép kiểu an toàn). |
| `GetSelectedText(combo)` | Lấy chuỗi văn bản đang hiển thị. |
| `IsAllSelected(combo)` | Kiểm tra có đang chọn dòng đầu tiên ("-- Tất cả --") hay không. |
| `SelectByValue(combo, value)` | Đặt `SelectedValue` an toàn. |

**Ví dụ:**
```csharp
// Nạp combo nhập liệu
ComboBoxHelper.Bind(cboPhongBan, dtPhong, "MaPhong", "TenPhong");

// Nạp combo tìm kiếm có "-- Tất cả --"
ComboBoxHelper.BindWithAll(cboTimPhong, dtPhong, "MaPhong", "TenPhong", "-- Tất cả phòng ban --");
```

---

### 2.2 `GridHelper` (Nạp và định dạng DataGridView)

| Hàm | Mô tả |
|---|---|
| `Bind(grid, dataTable)` | Nạp nhanh toàn bộ DataTable vào DataGridView. |
| `Bind(grid, dataTable, params GridColumn[] cols)` | Nạp và tự động cấu hình cột: Tiêu đề, Độ rộng, Định dạng ngày/số. |
| `SetReadOnly(grid)` | Áp dụng cấu hình chuẩn đi thi: ReadOnly=true, FullRowSelect, không cho thêm/xóa trực tiếp trên grid. |
| `GetSelectedRow(grid)` | Trả về `DataRowView` của dòng đang chọn (trả về `null` nếu chưa chọn). |
| `GetSelectedValue<T>(grid, colName)` | Lấy giá trị của một ô trên dòng được chọn. |
| `GetSelectedKeyValue<T>(grid)` | Lấy giá trị ô đầu tiên của dòng được chọn. |
| `HasSelection(grid)` | Kiểm tra có dòng nào đang được chọn không. |

**Cấu trúc GridColumn:**
```csharp
new GridHelper.GridColumn(dataField, headerText, width = -1, format = null)
// format: "dd/MM/yyyy" cho ngày tháng, "N0" cho tiền, "N2" cho số thập phân
```

---

### 2.3 `FormMapper` (Ánh xạ 2 chiều Form ⇄ Dữ liệu)

Giúp giảm 80% code gán từng control bằng tay.

| Hàm | Mô tả |
|---|---|
| `Map(control, columnName)` | Tạo một cặp ánh xạ Control - Tên Cột trong DataTable. |
| `GetValues(params FieldMapping[] mappings)` | Đọc toàn bộ giá trị từ các Control trả về `Dictionary<string, object>` để nạp thẳng vào `CrudHelper.Insert/Update`. |
| `LoadRow(row/rowView, params FieldMapping[] mappings)` | Đọc dữ liệu từ `DataRow`/`DataRowView` nạp ngược lên Form (tự xử lý `DBNull`). |
| `ClearControls(params Control[] controls)` | Reset trắng TextBox, NumericUpDown về min, ComboBox về 0, CheckBox về false. |

**Hỗ trợ tự động các loại Control:**
- `TextBox`, `RichTextBox`, `MaskedTextBox`: Đọc và gán chuỗi (đã Trim).
- `ComboBox`: Đọc và gán `SelectedValue`.
- `DateTimePicker`: Đọc và gán `Value`.
- `CheckBox`, `RadioButton`: Đọc và gán `Checked`.
- `NumericUpDown`: Đọc và gán `Value` (kiểm soát trong min/max).

---

## 3. ExamHelper.Validation

### 3.1 `Validator` (Kiểm tra dữ liệu đầu vào)

| Hàm tạo quy tắc | Mô tả |
|---|---|
| `Validator.Required(ctrl, name, msg = null)` | Bắt buộc nhập (không được rỗng). |
| `Validator.Pattern(ctrl, name, regex, msg = null)` | Kiểm tra định dạng Regex (VD: `^NV\d{3}$`). |
| `Validator.IntRange(ctrl, name, min, max, msg = null)` | Khoảng số nguyên. |
| `Validator.DecimalRange(ctrl, name, min, max, msg = null)` | Khoảng số thực / tiền tệ / hệ số lương. |
| `Validator.DateRange(ctrl, name, min, max, msg = null)` | Khoảng ngày tháng. |
| `Validator.AgeRange(ctrl, name, minAge, maxAge, msg = null)` | Tuổi tại ngày hiện tại. |
| `Validator.AgeRangeAtDate(ctrl, name, minAge, maxAge, msg = null)` | **Đặc sản đề thi:** Tuổi được tính tại một ngày cụ thể (như Ngày vào làm). |
| `Validator.Custom(ctrl, name, Func<string, bool> check, msg)` | Quy tắc logic tự viết. |
| `Validator.CalculateAge(birthDate, refDate)` | Hàm static tính tuổi chính xác theo năm và ngày sinh nhật. |

### 3.2 `Validator.ValidateAll(...)`

Thực thi tuần tự các quy tắc. Khi gặp lỗi đầu tiên, dừng lại và trả về:
- `IsValid = false`
- `ErrorMessage` (tiếng Việt rõ ràng)
- `ErrorControl` (control đang sai để focus ngay)

**Ví dụ:**
```csharp
var result = Validator.ValidateAll(
    dtpNgayVaoLam.Value, // Ngày mốc tính tuổi
    Validator.Required(txtMaNV, "Mã nhân viên"),
    Validator.Pattern(txtMaNV, "Mã nhân viên", @"^NV\d{3}$", "Mã phải có dạng NV001"),
    Validator.Required(txtHoTen, "Họ tên"),
    Validator.DecimalRange(txtHeSoLuong, "Hệ số lương", 1.0m, 15.0m),
    Validator.AgeRangeAtDate(dtpNgaySinh, "Tuổi nhân viên", 18, 60, "Nhân viên phải từ 18-60 tuổi khi vào làm")
);

if (!result.IsValid)
{
    MessageHelper.Warning(result.ErrorMessage);
    FormHelper.FocusControl(result.ErrorControl);
    return;
}
```

---

## 4. ExamHelper.Utilities

### 4.1 `DataHelper` (Tính toán & Thống kê DataTable)

Tính toán trực tiếp trên DataTable (rất nhanh, không cần truy vấn lại CSDL).

| Hàm | Mô tả |
|---|---|
| `Count(table, filter = "")` | Đếm số dòng (có điều kiện lọc tùy chọn). |
| `Sum(table, colName, filter = "")` | Tính tổng giá trị cột số. |
| `Average(table, colName, filter = "")` | Tính trung bình cộng. |
| `Max<T>(table, colName, filter = "")` | Lấy giá trị lớn nhất. |
| `Min<T>(table, colName, filter = "")` | Lấy giá trị nhỏ nhất. |
| `ToDecimal(obj, default = 0)` | Ép kiểu an toàn từ object/DBNull sang decimal. |
| `ToInt(obj, default = 0)` | Ép kiểu an toàn sang int. |
| `ToDateTime(obj)` | Ép kiểu an toàn sang `DateTime?`. |

---

### 4.2 `MessageHelper` (Chuẩn hóa hộp thoại thông báo)

| Hàm | Mô tả |
|---|---|
| `Info(msg, title = "Thông báo")` | Hộp thoại thông tin. |
| `Warning(msg, title = "Cảnh báo")` | Hộp thoại cảnh báo. |
| `Error(msg, title = "Lỗi")` | Hộp thoại báo lỗi. |
| `ConfirmDelete(itemName)` | Hỏi xác nhận: "Bạn có chắc chắn muốn xóa {itemName} không?". |
| `Confirm(msg, title = "Xác nhận")` | Hộp thoại xác nhận Yes/No tùy chọn. |
| `Success(action)` | Thông báo: "Thao tác {action} thành công!". |
| `NoSelection(itemName = "dòng")` | Thông báo: "Vui lòng chọn {itemName} cần thao tác!". |
| `DuplicateKey(fieldName, value)` | Thông báo: "{fieldName} '{value}' đã tồn tại trong hệ thống!". |

---

### 4.3 `FormHelper` (Quản lý trạng thái Form)

| Hàm | Mô tả |
|---|---|
| `SetFormDefaults(form, title)` | Đặt form ra giữa màn hình (`CenterScreen`) và gán tiêu đề. |
| `EnableControls(enabled, params controls)` | Bật/tắt hàng loạt control cùng lúc. |
| `ToggleButtons(isEditing, btnAdd, btnEdit, btnDelete, btnSave, btnCancel)` | Chuyển đổi trạng thái nút: Đang sửa thì tắt Thêm/Sửa/Xóa, bật Lưu/Hủy và ngược lại. |
| `FocusControl(control)` | Focus vào control, tự động bôi đen toàn bộ chữ nếu là TextBox. |
| `ResetForm(params controls)` | Xóa trắng form chuẩn bị nhập mới. |
