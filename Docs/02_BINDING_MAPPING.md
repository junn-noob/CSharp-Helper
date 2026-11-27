# 02. MODULE BINDING & FORM MAPPING

> **Namespace:** `ExamHelper.Binding`  
> **Thành phần:** `ComboBoxHelper`, `GridHelper`, `FormMapper`

---

## 1. `ComboBoxHelper` — Nạp và đọc ComboBox

### 1.1 Điểm cần lưu ý
- Luôn thiết lập `DisplayMember` và `ValueMember` **trước** `DataSource` và gọi `.Copy()` trên `DataTable` nguồn để nhiều ComboBox không đụng con trỏ `BindingManager`.
- `BindWithAll` cho phép dòng `"-- Tất cả --"` nằm tại `SelectedIndex == 0` với `AllowDBNull = true` trên bản sao.
- `SelectByValue` tự ép kiểu và có cơ chế fallback `Trim().OrdinalIgnoreCase` cho cột `CHAR`/`NCHAR` trong SQL Server hay bị dư khoảng trắng cuối chuỗi.

### 1.2 Danh sách API của `ComboBoxHelper`
```csharp
// Bind từ DataTable (tường minh hoặc tự lấy cột 1 làm Value, cột 2 làm Display)
void Bind(ComboBox combo, DataTable data, string valueMember, string displayMember);
void Bind(ComboBox combo, DataTable data);

// Thực thi thẳng SQL query và nạp ComboBox
void Bind(ComboBox combo, string sql, string valueMember = null, string displayMember = null, string dbName = "default");

// Bind kèm dòng "Tất cả" ở đầu danh sách (cả DataTable và SQL trực tiếp)
void BindWithAll(ComboBox combo, DataTable data, string valueMember, string displayMember, string allText = "-- Tất cả --", object allValue = null);
void BindWithAll(ComboBox combo, string sql, string allText = "-- Tất cả --", object allValue = null, string valueMember = null, string displayMember = null, string dbName = "default");

// Nạp danh sách mục cố định và chọn sẵn mục đầu ("Tất cả", "Nam", "Nữ")
void BindItems(ComboBox combo, params object[] items);

// Đọc & chọn mục
T GetSelectedValue<T>(ComboBox combo);
string GetSelectedText(ComboBox combo);
bool IsAllSelected(ComboBox combo);
void SelectByValue(ComboBox combo, object value);
bool SelectByText(ComboBox combo, string text);
void SelectFirst(ComboBox combo);
```

### 1.3 Ví dụ
```csharp
// Combo nhập liệu
ComboBoxHelper.Bind(cboPhongBan, "SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB");

// Combo lọc có dòng "Tất cả"
ComboBoxHelper.BindWithAll(cboLocPhong, "SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB", "Tất cả");
ComboBoxHelper.BindItems(cboGioiTinh, "Tất cả", "Nam", "Nữ");
```

---

## 2. `GridHelper` — Hiển thị và truy xuất `DataGridView`

### 2.1 Lớp `GridHelper.GridColumn`
```csharp
new GridHelper.GridColumn(
    string dataField,                                  // Tên cột trong DataTable
    string headerText,                                 // Tiêu đề cột hiển thị
    int width = -1,                                    // Pixel cố định, hoặc -1 để Fill tự co giãn
    string format = null,                              // "N0", "0.##", "dd/MM/yyyy"
    DataGridViewContentAlignment? alignment = null)    // Căn lề trái/giữa/phải
```

### 2.2 Danh sách API của `GridHelper`
```csharp
// Bind dữ liệu
void Bind(DataGridView grid, DataTable data);
void Bind(DataGridView grid, DataTable data, params GridColumn[] columns);
void Bind(DataGridView grid, string sql, params GridColumn[] columns);
void Bind(DataGridView grid, string sql, FilterBuilder filter, string orderBy = null, params GridColumn[] columns);
void BindSql(DataGridView grid, string sql, params DbParameter[] parameters);
void BindSql(DataGridView grid, string sql, DbParameter[] parameters, string connection, params GridColumn[] columns);

// Cấu hình & Truy xuất dòng đang chọn
void SetReadOnly(DataGridView grid);
bool HasSelection(DataGridView grid);
DataRowView GetSelectedRow(DataGridView grid);
DataRow GetSelectedDataRow(DataGridView grid);
T GetSelectedKeyValue<T>(DataGridView grid, string keyColumnName = null);
T GetSelectedValue<T>(DataGridView grid, string columnName); // Đọc được cả cột ẩn trong DataRowView!
void ClearSelection(DataGridView grid);
bool SelectRowByValue(DataGridView grid, string columnName, object value);
```

> **Lưu ý:** Khi câu `SELECT` có `JOIN` (ví dụ lấy `pb.TenPB` để hiện lên Grid nhưng cần `nv.MaPB`, `nv.GioiTinh` để đổ ngược lên ComboBox/RadioButton), hãy cứ `SELECT` các cột gốc đó ở cuối câu truy vấn và chỉ khai báo `GridColumn` cho các cột muốn hiển thị. `GridHelper.GetSelectedValue<T>` và `FormMapper` đọc trực tiếp từ `DataRowView` nên vẫn lấy được đầy đủ giá trị các cột ẩn đó.

---

## 3. `FormMapper` — Ánh xạ 2 chiều Control ⇄ Dữ liệu

`FormMapper` hỗ trợ hai cách dùng: **tường minh (`Map` / `LoadRow` / `GetValues`)** và **tự động theo quy ước (`AutoLoad` / `AutoGetValues` / `AutoClear`)**.

### 3.1 Cách 1: Tự động nhận diện (`AutoLoad` / `AutoGetValues` / `AutoClear`)
Khi gọi `AutoLoad(container, row)` hoặc `AutoGetValues(container, schemaTable)`, thư viện duyệt toàn bộ control nhập liệu (`TextBox`, `ComboBox`, `DateTimePicker`, `CheckBox`, `NumericUpDown`, `RichTextBox`, `MaskedTextBox`, `RadioButton`) bên trong `container` theo thứ tự ưu tiên:
1. **`ctrl.Tag`**:
   - Nếu `Tag = "TenCot"` → ánh xạ thẳng tới cột `TenCot`.
   - Nếu `Tag = "GioiTinh:1"` hoặc `"GioiTinh:0"` (trên `RadioButton`) → ánh xạ giá trị `true/1` hoặc `false/0` cho cột `GioiTinh`.
2. **`ComboBox.ValueMember`**: nếu `ComboBox` đã bind với `ValueMember = "MaPB"`, tự map tới cột `MaPB` dù tên control là `cboPhongBan`.
3. **Tên control bỏ tiền tố**: tự cắt các tiền tố `txt`, `cbo`, `cmb`, `dtp`, `chk`, `nud`, `rtb`, `mtb`, `rdo` (có hoặc không có `_`). Ví dụ `txtMaNV` → `MaNV`, `dtp_NgaySinh` → `NgaySinh`.

```csharp
// Đổ dòng vừa chọn từ Grid lên Form trong 1 dòng
DataRowView row = GridHelper.GetSelectedRow(dgvDanhSach);
if (row != null) FormMapper.AutoLoad(this, row);

// Thu thập giá trị đúng kiểu SQL (truyền schema bảng gốc để tránh dính cột JOIN/tính toán)
DataTable schema = DbHelper.GetTableSchema("NhanVien");
var values = FormMapper.AutoGetValues(this, schema);

// Xóa trắng toàn bộ control nhập liệu
FormMapper.AutoClear(this);
```

### 3.2 Cách 2: Ánh xạ tường minh (`LoadRow` / `GetValues`)
Dùng khi tên control trên Form không theo chuẩn tiền tố hoặc cần ép kiểu tường minh từng trường:

```csharp
// Tạo FieldMapping
FieldMapping Map(Control control, string columnName);
FieldMapping Map(Control control, string columnName, Type targetType);
FieldMapping Map<T>(Control control, string columnName);
FieldMapping MapDecimal(Control control, string columnName, bool nullable = false);
FieldMapping MapDouble(Control control, string columnName, bool nullable = false);
FieldMapping MapInt(Control control, string columnName, bool nullable = false);
FieldMapping Map(RadioButton trueRadio, RadioButton falseRadio, string columnName);

// Đọc & Ghi
void LoadRow(DataRow row, params FieldMapping[] mappings);
void LoadRow(DataRowView rowView, params FieldMapping[] mappings);
bool LoadRow(DataTable table, params FieldMapping[] mappings);
bool LoadSql(string sql, params FieldMapping[] mappings);
bool LoadSql(string sql, DbParameter[] parameters, params FieldMapping[] mappings);
bool LoadSql(string sql, DbParameter[] parameters, string connection, params FieldMapping[] mappings);

bool AutoLoad(Control container, DataTable table);
void AutoLoad(Control container, DataRowView rowView);
void AutoLoad(Control container, DataRow row);
bool AutoLoadSql(Control container, string sql, params DbParameter[] parameters);
bool AutoLoadSql(Control container, string sql, DbParameter[] parameters, string connection);

Dictionary<string, object> GetValues(params FieldMapping[] mappings);
Dictionary<string, object> AutoGetValues(Control container, DataTable schemaTable = null);
void ClearControls(params Control[] controls);
void AutoClear(Control container);
```

> **Lưu ý kiểu số thực/tiền VN:** Khi dùng `AutoGetValues(this, schema)` (có schema `double`/`decimal`/`int`) hoặc `MapDouble`/`MapDecimal`/`MapInt`, chuỗi nhập `"2,34"` hay `"1.500.000"` đều được chuẩn hóa qua `DataHelper.TryParseDecimal` trước khi đưa xuống SQL.
