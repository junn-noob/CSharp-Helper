# 01. MODULE DATABASE, ĐA CSDL, CRUD & FILTERBUILDER

> **Namespace:** `ExamHelper.Database`  
> **Thành phần:** `DbDialect` (enum), `DbHelper`, `CrudHelper`, `FilterBuilder`

---

## 1. `DbHelper` & `DbDialect` — Kết nối và Truy vấn đa CSDL

### 1.1 Cơ chế kết nối tự động (không cần tự `Open()` / `Close()`)
Mọi lệnh truy vấn trong `DbHelper` đều tự động tạo kết nối, mở kết nối, gán tham số an toàn và đóng/giải phóng (`using`) ngay sau khi chạy xong.

**Cách nhận diện chuỗi kết nối khi gọi bất kỳ hàm nào với `dbName = "default"`:**
1. Tìm trong bộ nhớ đã đăng ký qua `DbHelper.ConnectionString` hoặc `SetConnectionString(...)`.
2. Nếu chuỗi truyền vào giống một Connection String trực tiếp (chứa `=` và `;`), tự nhận diện `DbDialect` và thực thi thẳng không cần đăng ký trước.
3. Nếu chưa đăng ký, tự đọc file `App.config` (`<connectionStrings>`), ưu tiên mục tên `"default"`, sau đó lấy mục hợp lệ đầu tiên (bỏ qua `LocalSqlServer`).

### 1.2 Cấu hình đa CSDL (`DbDialect`)
Thư viện hỗ trợ 6 dialect: `SqlServer`, `Sqlite`, `MySql`, `PostgreSql`, `OleDb`, `Odbc`.

```csharp
// Cách 1: Gán trực tiếp (phù hợp SQL Server hoặc đổi DB nhanh khi runtime)
DbHelper.ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=QLNhanSu;Trusted_Connection=True;";

// Cách 2: Đọc đích danh cấu hình từ App.config
bool ok = DbHelper.SetConnectionStringFromConfig("QLNhanSu");

// Cách 3: Đăng ký nhiều kết nối theo tên
DbHelper.SetConnectionString("KhoHang", "Server=.;Database=QLKho;Trusted_Connection=True;");

// Cách 4: Kết nối hệ CSDL khác khi project đã cài driver NuGet (truyền DbProviderFactory)
DbHelper.SetDefaultConnectionString(
    "Data Source=dulieu.db",
    Microsoft.Data.Sqlite.SqliteFactory.Instance,
    DbDialect.Sqlite);
```

> **Cơ chế tương thích đặc biệt:**
> - Khi dùng driver có `CreateDataAdapter() == null` (như `Microsoft.Data.Sqlite`), `GetDataTable` và `GetTableSchema` tự động chuyển sang đọc qua `DbDataReader`.
> - Khi dùng `DbDialect.OleDb` hoặc `DbDialect.Odbc`, `DbHelper` tự động chuyển các tham số dạng `@param` thành `?` đúng thứ tự xuất hiện (bỏ qua ký tự `@` nằm trong chuỗi literal `'...'`).

### 1.3 Danh sách API chính của `DbHelper`

#### Kết nối, chẩn đoán lỗi & Dialect
```csharp
string DbHelper.ConnectionString { get; set; }
Exception DbHelper.LastError { get; set; }
bool DbHelper.HasError { get; }
bool DbHelper.ThrowOnError { get; set; }     // Mặc định: false
bool DbHelper.ShowErrorDialog { get; set; }  // Mặc định: true
Action<string, Exception> DbHelper.OnError { get; set; }

void SetConnectionString(string name, string connectionString, DbProviderFactory factory = null, DbDialect? dialect = null);
void SetDefaultConnectionString(string connectionString, DbProviderFactory factory = null, DbDialect? dialect = null);
bool SetConnectionStringFromConfig(string configName, string dbName = "default", string configFilePath = null);

DbDialect DetectDialect(string connStr, string providerName = null);
DbDialect GetDialect(string dbName = "default");
string QuoteIdentifier(string identifier, string dbName = "default");
string QuoteIdentifier(string identifier, DbDialect dialect);
string GetLastInsertIdSql(DbDialect dialect);

bool TestConnection(string dbName = "default", bool showError = false, int timeoutSeconds = 3);
SqlConnection GetConnection(string dbName = "default");   // Chỉ SQL Server
DbConnection OpenConnection(string dbName = "default");   // Mọi loại CSDL
```

#### Đọc Schema & Danh sách bảng tự động
```csharp
// Lấy tên các bảng dữ liệu trong CSDL đang kết nối (bỏ qua bảng hệ thống)
List<string> GetTableNames(string dbName = "default");

// Đọc cấu trúc bảng (PrimaryKey, AllowDBNull, MaxLength, AutoIncrement, DataType)
DataTable GetTableSchema(string tableName, string dbName = "default");
```

#### Truy vấn & Thực thi tham số hóa
```csharp
// Đọc bảng / dòng đầu tiên
DataTable GetDataTable(string sql, params DbParameter[] parameters);
DataTable GetDataTable(string sql, string dbName, params DbParameter[] parameters);
DataTable GetDataTable(string sql, FilterBuilder filter, string orderBy = null, string dbName = "default");
DataRow GetDataRow(string sql, params DbParameter[] parameters);
DataRow GetDataRow(string sql, string dbName, params DbParameter[] parameters);

// Thực thi INSERT / UPDATE / DELETE
int ExecuteNonQuery(string sql, params DbParameter[] parameters);
int ExecuteNonQuery(string sql, string dbName, params DbParameter[] parameters);
int ExecuteNonQuery(string sql, SqlTransaction transaction, params SqlParameter[] parameters);
int ExecuteNonQuery(string sql, DbTransaction transaction, params DbParameter[] parameters);

// Lấy giá trị đơn / Kiểm tra tồn tại
T ExecuteScalar<T>(string sql, params DbParameter[] parameters);
T ExecuteScalar<T>(string sql, string dbName, params DbParameter[] parameters);
T ExecuteScalar<T>(string sql, SqlTransaction transaction, params SqlParameter[] parameters);
T ExecuteScalar<T>(string sql, DbTransaction transaction, params DbParameter[] parameters);
bool Exists(string sql, params DbParameter[] parameters);
bool Exists(string sql, string dbName, params DbParameter[] parameters);

// Transaction tự Commit / Rollback
bool ExecuteTransaction(Action<SqlTransaction> action, string dbName = "default");
bool ExecuteDbTransaction(Action<DbTransaction> action, string dbName = "default");

// Tạo tham số nhanh (tự đổi null -> DBNull.Value, bỏ dấu @ cũng hợp lệ)
SqlParameter Param(string name, object value);
SqlParameter ParamDate(string name, DateTime? value);
SqlParameter ParamInt(string name, int? value);
SqlParameter ParamDecimal(string name, decimal? value);
SqlParameter ParamLike(string name, string value); // Tự bọc %value%
```

### 1.4 Ví dụ thực tế
```csharp
// Kiểm tra kết nối có timeout thực 3 giây (tránh treo giao diện khi sai IP/Server)
if (!DbHelper.TestConnection("default", showError: true, timeoutSeconds: 3))
    return;

// Đọc dữ liệu có tham số
DataTable dt = DbHelper.GetDataTable(
    "SELECT * FROM NhanVien WHERE MaPB = @pb AND Luong >= @min",
    DbHelper.Param("pb", "PB01"),
    DbHelper.ParamDecimal("min", 5000000m));

// Phân biệt bảng rỗng với lỗi SQL thật
if (DbHelper.HasError)
{
    MessageBox.Show("Lỗi SQL: " + DbHelper.LastError.Message);
}

// Giao dịch thêm Hóa đơn + Chi tiết hóa đơn (tự Rollback nếu lỗi)
bool saved = DbHelper.ExecuteDbTransaction(tx =>
{
    int maHD = DbHelper.ExecuteScalar<int>(
        "INSERT INTO HoaDon(NgayLap) VALUES(@d); SELECT SCOPE_IDENTITY();",
        tx, DbHelper.ParamDate("d", DateTime.Now));
    DbHelper.ExecuteNonQuery(
        "INSERT INTO CTHD(MaHD, MaSP, SL) VALUES(@hd, @sp, @sl)",
        tx, DbHelper.Param("hd", maHD), DbHelper.Param("sp", "SP01"), DbHelper.ParamInt("sl", 2));
});
```

---

## 2. `CrudHelper` — Thêm, Sửa, Xóa tổng quát đa CSDL

### 2.1 Đặc điểm chính
- Không phải tự viết chuỗi `INSERT` / `UPDATE` / `DELETE` khi đã có `Dictionary<string, object>` (thường lấy từ `FormMapper.AutoGetValues` hoặc `FormMapper.GetValues`).
- Tự kiểm tra whitelist tên bảng/cột (chặn SQL Injection qua tên định danh) và bọc ngoặc đúng `DbDialect` đang kết nối.
- Hàm `Update(...)` tự bỏ qua cột khóa chính nếu giá trị không thay đổi (tránh lỗi cập nhật cột khóa/IDENTITY).
- `GetAll(table, orderBy)` kiểm tra whitelist từng phần trong `ORDER BY` (chỉ cho phép `TênCột [ASC|DESC]`).

### 2.2 Danh sách API của `CrudHelper`
```csharp
int Insert(string table, Dictionary<string, object> values, string dbName = "default");
T InsertAndGetId<T>(string table, Dictionary<string, object> values, string dbName = "default");

int Update(string table, Dictionary<string, object> values, string keyColumn, object keyValue, string dbName = "default");
int Update(string table, Dictionary<string, object> values, Dictionary<string, object> keys, string dbName = "default");

int Delete(string table, string keyColumn, object keyValue, string dbName = "default");
int Delete(string table, Dictionary<string, object> keys, string dbName = "default");

bool Exists(string table, string column, object value, string dbName = "default");
bool Exists(string table, Dictionary<string, object> keys, string dbName = "default");
bool ExistsExcept(string table, string checkColumn, object checkValue, string keyColumn, object keyValue, string dbName = "default");

DataTable GetAll(string table, string orderBy = null, string dbName = "default");
DataRow GetById(string table, string keyColumn, object keyValue, string dbName = "default");
```

### 2.3 Ví dụ
```csharp
var values = FormMapper.AutoGetValues(this, DbHelper.GetTableSchema("NhanVien"));

// Thêm mới (kiểm tra trùng mã trước)
if (CrudHelper.Exists("NhanVien", "MaNV", maNV)) return;
CrudHelper.Insert("NhanVien", values);

// Cập nhật (theo khóa đơn hoặc khóa phức hợp)
CrudHelper.Update("NhanVien", values, "MaNV", maNV);

// Xóa
CrudHelper.Delete("NhanVien", "MaNV", maNV);
```

---

## 3. `FilterBuilder` — Dựng mệnh đề `WHERE` động an toàn

### 3.1 Nguyên tắc hoạt động
- Mọi điều kiện (`Equal`, `Contains`, `Between`, `SkipIfAllSelected`...) tự động **bỏ qua** nếu giá trị đầu vào là `null`, rỗng hoặc ComboBox đang chọn dòng `"Tất cả"` (`SelectedIndex <= 0`).
- Tự động đánh tên tham số (`@f0`, `@f1`, ...) tránh trùng khi kết hợp nhiều điều kiện `AND` / `OR`.
- Dùng `FilterBuilder.ForConnection(dbName)` để tự bọc tên cột theo đúng dialect đang kết nối (`[col]` cho SQL Server, `` `col` `` cho MySQL, `"col"` cho SQLite/PostgreSQL).

### 3.2 Danh sách API của `FilterBuilder`
```csharp
FilterBuilder FilterBuilder.New();
FilterBuilder FilterBuilder.New(DbDialect dialect);
FilterBuilder FilterBuilder.ForConnection(string dbName = "default");

FilterBuilder Equal(string column, object value, string paramName = null, bool skipZero = false);
FilterBuilder SkipIfZero(string column, object value, string paramName = null);
FilterBuilder NotEqual(string column, object value, string paramName = null, bool skipZero = false);
FilterBuilder Contains(string column, string value, string paramName = null);
FilterBuilder StartsWith(string column, string value, string paramName = null);
FilterBuilder EndsWith(string column, string value, string paramName = null);
FilterBuilder GreaterThan(string column, object value, string paramName = null);
FilterBuilder GreaterThanOrEqual(string column, object value, string paramName = null);
FilterBuilder LessThan(string column, object value, string paramName = null);
FilterBuilder LessThanOrEqual(string column, object value, string paramName = null);
FilterBuilder Between(string column, object min, object max, string paramMin = null, string paramMax = null);
FilterBuilder DateBetween(string column, DateTime? fromDate, DateTime? toDate);
FilterBuilder In(string column, params object[] values);
FilterBuilder NotIn(string column, params object[] values);
FilterBuilder IsNull(string column);
FilterBuilder IsNotNull(string column);
FilterBuilder SkipIfAllSelected(ComboBox combo, string column, string paramName = null);
FilterBuilder Or(Action<FilterBuilder> configure);
FilterBuilder ContainsAny(string[] columns, string value);
FilterBuilder Raw(string condition, params SqlParameter[] rawParams);

bool HasConditions { get; }
string ToWhereClause();
SqlParameter[] ToParameters();
(string WhereClause, SqlParameter[] Parameters) Build();
```

### 3.3 Ví dụ tìm kiếm đa điều kiện
```csharp
var filter = FilterBuilder.ForConnection()
    .SkipIfAllSelected(cboPhongBan, "nv.MaPB")
    .ContainsAny(new[] { "nv.MaNV", "nv.HoTen" }, txtTuKhoa.Text)
    .DateBetween("nv.NgayVaoLam", dtpTuNgay.Value, dtpDenNgay.Value);

if (cboGioiTinh.SelectedIndex == 1) filter.Equal("nv.GioiTinh", 1);
else if (cboGioiTinh.SelectedIndex == 2) filter.Equal("nv.GioiTinh", 0);

string sql = "SELECT nv.*, pb.TenPB FROM NhanVien nv JOIN PhongBan pb ON nv.MaPB = pb.MaPB";
DataTable dt = DbHelper.GetDataTable(sql, filter, "pb.TenPB, nv.MaNV");
```
