# HƯỚNG DẪN CHI TIẾT: MODULE DATABASE & CRUD
> **Namespace:** `ExamHelper.Database`  
> **Các lớp chính:** `DbHelper`, `CrudHelper`, `FilterBuilder`

---

## 1. Lớp `DbHelper` - Kết nối & Thực thi SQL an toàn

### 1.1 Mục đích & Khi nào dùng
- Quản lý chuỗi kết nối SQL Server (hỗ trợ nhiều CSDL cùng lúc bằng cơ chế đặt tên).
- Thực thi các câu lệnh `SELECT`, `INSERT`, `UPDATE`, `DELETE`, `COUNT(*)`, `SUM()` với 100% tham số hóa (`SqlParameter`) chống tấn công SQL Injection.
- Cung cấp các hàm tạo nhanh tham số xử lý an toàn giá trị `null` → `DBNull.Value`.

### 1.2 Danh sách hàm quan trọng
```csharp
// Cấu hình chuỗi kết nối
void DbHelper.SetDefaultConnectionString(string connStr);
void DbHelper.SetConnectionString(string name, string connStr);

// Truy vấn đọc dữ liệu ra DataTable
DataTable DbHelper.GetDataTable(string sql, params SqlParameter[] parameters);
DataTable DbHelper.GetDataTable(string sql, string dbName, params SqlParameter[] parameters);

// Thực thi câu lệnh Thêm/Sửa/Xóa (trả về số dòng ảnh hưởng)
int DbHelper.ExecuteNonQuery(string sql, params SqlParameter[] parameters);

// Lấy 1 giá trị đơn (COUNT, MAX, SUM...)
T DbHelper.ExecuteScalar<T>(string sql, params SqlParameter[] parameters);

// Kiểm tra sự tồn tại của bản ghi
bool DbHelper.Exists(string sql, params SqlParameter[] parameters);

// Tạo tham số nhanh xử lý DBNull
SqlParameter DbHelper.Param(string name, object value);
SqlParameter DbHelper.ParamLike(string name, string value); // Tự bọc %value%
SqlParameter DbHelper.ParamDate(string name, DateTime? value);
SqlParameter DbHelper.ParamInt(string name, int? value);
SqlParameter DbHelper.ParamDecimal(string name, decimal? value);
```

### 1.3 Ví dụ áp dụng thực tế

#### Ví dụ 1: Cấu hình kết nối khi Form Load
```csharp
private void Form1_Load(object sender, EventArgs e)
{
    // CSDL chính
    DbHelper.SetDefaultConnectionString("Server=.;Database=QLSinhVien;Trusted_Connection=True;");

    // Nếu đề thi yêu cầu kết nối thêm CSDL phụ (ví dụ tra cứu học phí)
    DbHelper.SetConnectionString("HocPhiDb", "Server=.;Database=QLHocPhi;Trusted_Connection=True;");
}
```

#### Ví dụ 2: Đọc dữ liệu có điều kiện tham số
```csharp
string sql = "SELECT * FROM SinhVien WHERE MaLop = @malop AND DiemTB >= @diem";
DataTable dt = DbHelper.GetDataTable(sql,
    DbHelper.Param("@malop", "D19CNPM1"),
    DbHelper.Param("@diem", 7.0));
```

#### Ví dụ 3: Đếm số lượng sinh viên đạt loại Giỏi
```csharp
int countGioi = DbHelper.ExecuteScalar<int>(
    "SELECT COUNT(*) FROM SinhVien WHERE DiemTB >= @minDiem",
    DbHelper.Param("@minDiem", 8.0));
```

---

## 2. Lớp `CrudHelper` - CRUD Tổng quát an toàn

### 2.1 Mục đích & Khi nào dùng
- Thực hiện các thao tác Thêm (`Insert`), Sửa (`Update`), Xóa (`Delete`) mà **không cần viết tay các câu lệnh SQL dài dòng**.
- Tích hợp sẵn cơ chế **kiểm tra Whitelist định danh** (chống chèn mã độc vào tên bảng hoặc tên cột).
- Có hàm kiểm tra trùng khóa khi thêm mới (`Exists`) và kiểm tra trùng khi cập nhật ngoại trừ dòng hiện tại (`ExistsExcept`).

### 2.2 Danh sách hàm quan trọng
```csharp
// Thêm 1 dòng mới từ Dictionary
int CrudHelper.Insert(string table, Dictionary<string, object> values, string dbName = "default");

// Cập nhật dòng theo khóa chính
int CrudHelper.Update(string table, Dictionary<string, object> values, string keyColumn, object keyValue, string dbName = "default");

// Xóa dòng theo khóa chính
int CrudHelper.Delete(string table, string keyColumn, object keyValue, string dbName = "default");

// Kiểm tra trùng giá trị (thường dùng kiểm tra trùng Mã khi Thêm)
bool CrudHelper.Exists(string table, string column, object value, string dbName = "default");

// Kiểm tra trùng nhưng loại trừ dòng đang chọn (dùng khi Sửa)
bool CrudHelper.ExistsExcept(string table, string checkColumn, object checkValue, string keyColumn, object keyValue, string dbName = "default");
```

### 2.3 Ví dụ áp dụng thực tế

#### Ví dụ: Thêm, Sửa, Xóa bảng `SanPham`
```csharp
// 1. THÊM MỚI (Insert)
string maSP = txtMaSP.Text.Trim();
if (CrudHelper.Exists("SanPham", "MaSP", maSP))
{
    MessageBox.Show("Mã sản phẩm đã tồn tại!");
    return;
}

var values = new Dictionary<string, object>
{
    { "MaSP", maSP },
    { "TenSP", txtTenSP.Text.Trim() },
    { "DonGia", Convert.ToDecimal(txtDonGia.Text) },
    { "SoLuongTon", Convert.ToInt32(txtSoLuong.Text) }
};
CrudHelper.Insert("SanPham", values);

// 2. CẬP NHẬT (Update)
values.Remove("MaSP"); // Bỏ khóa chính không cập nhật
CrudHelper.Update("SanPham", values, "MaSP", maSP);

// 3. XÓA (Delete)
CrudHelper.Delete("SanPham", "MaSP", maSP);
```

---

## 3. Lớp `FilterBuilder` - Bộ lọc tìm kiếm thông minh

### 3.1 Mục đích & Khi nào dùng
- Dùng cho các thanh tìm kiếm đa điều kiện (Tìm theo Phòng ban, Giới tính, Tên, Khoảng lương...).
- **Tính năng độc quyền:** Tự động bỏ qua điều kiện nếu người dùng để trống ô nhập hoặc chọn `-- Tất cả --` trên ComboBox. Không cần viết chuỗi if/else lồng nhau phức tạp.
- Luôn trả về mệnh đề WHERE chuẩn kèm mảng `SqlParameter` an toàn.

### 3.2 Danh sách hàm quan trọng
```csharp
FilterBuilder FilterBuilder.New(); // Khởi tạo builder
FilterBuilder Equal(string col, object val); // col = @val (bỏ qua nếu val rỗng/0)
FilterBuilder NotEqual(string col, object val);
FilterBuilder Contains(string col, string val); // col LIKE '%val%' (bỏ qua nếu rỗng)
FilterBuilder StartsWith(string col, string val);
FilterBuilder Between(string col, object min, object max);
FilterBuilder SkipIfAllSelected(ComboBox combo, string col); // Tự bỏ qua nếu SelectedIndex == 0
(string WhereClause, SqlParameter[] Parameters) Build(); // Xuất kết quả
```

### 3.3 Ví dụ áp dụng thực tế

#### Ví dụ: Tìm kiếm kết hợp Phòng ban + Tên + Khoảng lương
```csharp
private void btnTimKiem_Click(object sender, EventArgs e)
{
    var (where, parms) = FilterBuilder.New()
        // Bỏ qua nếu chọn dòng đầu '-- Tất cả phòng ban --'
        .SkipIfAllSelected(cboTimPhong, "nv.MaPB")
        // Bỏ qua nếu để trống ô Họ tên
        .Contains("nv.HoTen", txtTimTen.Text.Trim())
        // Bỏ qua nếu không nhập khoảng lương
        .Between("nv.Luong", txtLuongMin.Text, txtLuongMax.Text)
        .Build();

    string sql = @"SELECT nv.*, pb.TenPB 
                   FROM NhanVien nv 
                   JOIN PhongBan pb ON nv.MaPB = pb.MaPB" + where;

    DataTable dt = DbHelper.GetDataTable(sql, parms);
    GridHelper.Bind(dgvNhanVien, dt);
}
```
*(Nếu người dùng không nhập gì cả, `where` sẽ tự động trả về `""` và nạp toàn bộ danh sách!)*
