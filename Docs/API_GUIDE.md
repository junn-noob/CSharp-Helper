# 📘 CẨM NANG TRA CỨU API EXAMHELPER PRO
> **Dành cho người mới bắt đầu (Newbie Guide)**  
> *Ngắn gọn – Dễ hiểu – Cần làm gì tra đúng mục đó – Copy & Chạy ngay!*

---

## 🧭 BẢN ĐỒ DÀNH CHO NEWBIE: "TÔI MUỐN LÀM GÌ?"

Nếu bạn mới học WinForms và cảm thấy bối rối trước hàng đống code, **hãy tra cứu theo nhu cầu của bạn ở bảng dưới đây**:

| Khi bạn muốn... | Dùng Lớp / Hàm này | Nhảy đến mục |
|---|---|:---:|
| Kết nối đa CSDL (SQL Server, SQLite, MySQL, PostgreSQL, OleDb, Odbc) & `SELECT` | `DbHelper` | [Mục 1](#1-dbhelper---kết-nối--truy-vấn-sql-server) |
| Mở form CRUD + tìm kiếm + báo cáo **tự sinh từ bất kỳ bảng CSDL nào** | `FormGenericManager.Open` | [Mục 1.3](#13-tự-đọc-schema-và-mở-form-quản-lý-bất-kỳ-bảng-nào) |
| Đổ dữ liệu vào ComboBox (Phòng ban, Chuyên khoa, Lớp...) | `ComboBoxHelper.Bind` | [Mục 2](#2-comboboxhelper---nạp-combobox-cực-dễ) |
| Đổ danh mục có thêm dòng **"-- Tất cả --"** để tìm kiếm | `ComboBoxHelper.BindWithAll` | [Mục 2](#2-comboboxhelper---nạp-combobox-cực-dễ) |
| Hiển thị dữ liệu lên DataGridView kèm định dạng tiền `N0`, ngày `dd/MM/yyyy` | `GridHelper.Bind` | [Mục 3](#3-gridhelper---nạp-bảng-datagridview-đẹp-mắt) |
| Bấm vào 1 dòng trên DataGridView rồi **đổ ngược lên các ô nhập liệu** | `FormMapper.LoadRow` | [Mục 4](#4-formmapper---ánh-xạ-2-chiều-bảng--form) |
| Gom dữ liệu từ các ô nhập liệu thành Dictionary để lưu xuống CSDL | `FormMapper.GetValues` | [Mục 4](#4-formmapper---ánh-xạ-2-chiều-bảng--form) |
| Kiểm tra rỗng, đúng định dạng `NV001`, tuổi >= 24, khoảng tiền... | `Validator.ValidateAll` | [Mục 5](#5-validator---kiểm-tra-dữ-liệu-nhập-vào) |
| Kiểm tra xem Mã này đã tồn tại trong CSDL chưa (chống trùng) | `CrudHelper.Exists` | [Mục 6](#6-crudhelper---thêm-sửa-xóa-không-cần-viết-sql) |
| Thêm mới / Cập nhật / Xóa dữ liệu an toàn 100% | `CrudHelper.Insert / Update / Delete` | [Mục 6](#6-crudhelper---thêm-sửa-xóa-không-cần-viết-sql) |
| Làm Form Tìm kiếm kết hợp nhiều điều kiện (tự bỏ qua ô để trống) | `FilterBuilder` | [Mục 7](#7-filterbuilder---tạo-bộ-lọc-tìm-kiếm-linh-hoạt) |
| Đếm số bản ghi, Tính tổng tiền, Tìm giá cao nhất sau khi lọc | `DataHelper.Count / Sum / Max` | [Mục 8](#8-datahelper---thống-kê-dữ-liệu-sau-lọc) |
| Hiện thông báo tiếng Việt chuẩn: Thành công, Xác nhận Xóa Yes/No | `MessageHelper` | [Mục 9](#9-messagehelper---hộp-thoại-thông-báo-chuẩn) |
| Xuất Báo cáo gom nhóm, In ấn và Xuất Excel trong 1 dòng code | `ReportHelper.ShowReport` | [Mục 10](#10-reporthelper---form-báo-cáo-dùng-chung-chuẩn-in--excel) |

---


## 1. `DbHelper` - Kết nối & Truy vấn SQL Server

> **Nhiệm vụ:** Giúp bạn kết nối CSDL và đọc dữ liệu mà không sợ lỗi văng ứng dụng hay SQL Injection.

### 1.1 Khai báo kết nối (Chỉ cần gán 1 lần — tự mở & đóng sau mỗi truy vấn)
```csharp
private void Form1_Load(object sender, EventArgs e)
{
    // Cách A: Gán thẳng thuộc tính ConnectionString (đổi lúc nào tự kết nối DB đó ngay)
    DbHelper.ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=QLNhanSu;Trusted_Connection=True;";

    // Cách B: Đặt sẵn trong thẻ <connectionStrings> của App.config -> tự nhận, không cần viết code kết nối!
    // Hoặc chọn đích danh tên cấu hình: DbHelper.SetConnectionStringFromConfig("QLNhanSu");
}
```

### 1.2 Lấy dữ liệu chỉ bằng câu query
Mọi hàm đều tự mở kết nối trước khi chạy và tự đóng ngay khi xong:

```csharp
// Lấy nguyên bảng:
DataTable dt = DbHelper.GetDataTable("SELECT * FROM NhanVien");

// Lấy kèm tham số an toàn (bỏ @ cũng được):
DataTable dt2 = DbHelper.GetDataTable("SELECT * FROM NhanVien WHERE MaPB = @mapb",
    DbHelper.Param("mapb", "PB01"));

// Lấy nhanh 1 dòng đầu tiên:
DataRow row = DbHelper.GetDataRow("SELECT * FROM NhanVien WHERE MaNV = @ma", DbHelper.Param("ma", "NV001"));

// Hoặc truyền thẳng chuỗi kết nối khác riêng cho 1 câu query:
DataTable dtOther = DbHelper.GetDataTable("SELECT * FROM SanPham", "Server=.;Database=QLBanHang;Trusted_Connection=True;");

// Kết nối hệ CSDL khác (khi dự án có sẵn driver NuGet tương ứng):
// DbHelper.SetDefaultConnectionString("Data Source=dulieu.db", Microsoft.Data.Sqlite.SqliteFactory.Instance, DbDialect.Sqlite);
// DbHelper.SetDefaultConnectionString("Server=localhost;Database=app;Uid=root;Pwd=123;", MySqlConnector.MySqlConnectorFactory.Instance, DbDialect.MySql);
```

### 1.3 Tự đọc schema và mở Form quản lý bất kỳ bảng nào
```csharp
// Đọc danh sách bảng & cấu trúc cột từ bất kỳ CSDL nào đang kết nối:
List<string> tables = DbHelper.GetTableNames();
DataTable schema = DbHelper.GetTableSchema("SanPham");

// Kiểm tra hợp lệ tự động từ schema (NOT NULL, MaxLength, số nguyên/thực):
var v = Validator.FromSchema(this, schema);

// Mở thẳng Form CRUD + tìm kiếm + xuất báo cáo tự sinh không cần viết code riêng từng bảng:
FormGenericManager.Open(); // hoặc FormGenericManager.Open("SanPham");
```

---

## 2. `ComboBoxHelper` - Nạp ComboBox cực dễ

> **Nhiệm vụ:** Đổ danh mục (Phòng ban, Chuyên khoa, Lớp...) vào ô ComboBox.

### 2.1 Dùng cho Form nhập liệu (Form 1)
```csharp
// Cách nhanh nhất: truyền thẳng câu SQL (tự lấy cột 1 làm Value, cột 2 làm Display):
ComboBoxHelper.Bind(cboPhongBan, "SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB");

// Hoặc tự truyền DataTable và chỉ định tên cột rõ ràng:
DataTable dtPB = DbHelper.GetDataTable("SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB");
ComboBoxHelper.Bind(cboPhongBan, dtPB, "MaPB", "TenPB");
```

### 2.2 Dùng cho Form Tìm kiếm / Thống kê (Form 2)
Cần có thêm dòng `-- Tất cả --` ở đầu danh sách để khi người dùng chọn thì tìm tất cả.

```csharp
// Truyền thẳng câu SQL + tự thêm dòng "Tất cả" ở đầu:
ComboBoxHelper.BindWithAll(cboTimPhongBan, "SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB", "Tất cả");

// Nạp danh sách tĩnh (Giới tính / Trạng thái) sạch sẽ không trùng lặp:
ComboBoxHelper.BindItems(cboGioiTinh, "Tất cả", "Nam", "Nữ");
```

### 2.3 Cách lấy giá trị đang chọn trong ComboBox
```csharp
// Lấy mã đang chọn (kiểu string):
string maPB = ComboBoxHelper.GetSelectedValue<string>(cboPhongBan);

// Lấy chữ đang hiển thị trên ComboBox:
string tenPB = ComboBoxHelper.GetSelectedText(cboPhongBan);

// Kiểm tra xem người dùng có đang chọn dòng "-- Tất cả --" không:
if (ComboBoxHelper.IsAllSelected(cboTimPhongBan))
{
    // Đang chọn tất cả
}
```

---

## 3. `GridHelper` - Nạp bảng DataGridView đẹp mắt

> **Nhiệm vụ:** Hiển thị dữ liệu lên DataGridView, tự chỉnh độ rộng, đặt tiêu đề tiếng Việt và định dạng tiền tệ / ngày tháng.

### 3.1 Cú pháp chuẩn trong hàm `LoadData()`
```csharp
private void LoadData()
{
    // Cách siêu gọn (1 dòng): chỉ cần gõ câu query là lưới trên Form tự có dữ liệu!
    GridHelper.Bind(dgvNhanVien, "SELECT MaNV AS [Mã NV], HoTen AS [Họ tên], Luong AS [Lương] FROM NhanVien");
    GridHelper.SetReadOnly(dgvNhanVien);
}

// Hoặc tùy chỉnh từng cột chi tiết với GridColumn:
private void LoadDataChiTiet()
{
    string sql = @"SELECT nv.MaNV, nv.HoTen, 
                          CASE nv.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END AS GioiTinhText,
                          nv.NgaySinh, pb.TenPB, nv.Luong, nv.MaPB
                   FROM NhanVien nv 
                   JOIN PhongBan pb ON nv.MaPB = pb.MaPB";

    GridHelper.Bind(dgvNhanVien, sql,
        new GridHelper.GridColumn("MaNV", "Mã nhân viên", 100),
        new GridHelper.GridColumn("HoTen", "Họ và tên", 180),
        new GridHelper.GridColumn("GioiTinhText", "Giới tính", 80),
        new GridHelper.GridColumn("NgaySinh", "Ngày sinh", 100, "dd/MM/yyyy"), // Format ngày
        new GridHelper.GridColumn("TenPB", "Phòng ban", 140),
        new GridHelper.GridColumn("Luong", "Mức lương", 120, "N0")            // N0: Format tiền 15.000.000
    );
    GridHelper.SetReadOnly(dgvNhanVien);
}
```

### 3.2 Bảng tra cứu định dạng (Format) thường gặp:
- `"N0"`: Phân cách hàng nghìn cho số nguyên (ví dụ: `150000` ➔ `150.000`).
- `"N2"`: Số thực có 2 chữ số thập phân (ví dụ: `8.5` ➔ `8.50`).
- `"0.##"`: Số thực gọn (ví dụ: `2.34` giữ nguyên, `3.00` hiển thị `3`).
- `"dd/MM/yyyy"`: Ngày tháng năm chuẩn Việt Nam (ví dụ: `25/12/2024`).

---

## 4. `FormMapper` - Ánh xạ 2 chiều (Bảng ⇄ Form)

> **Nhiệm vụ:** Đây là "vũ khí tối thượng" giúp bạn không phải viết 20 dòng gán `txtTen.Text = row["Ten"].ToString()` thủ công!

### 4.1 Đổ tự động lên Form theo tên Control (`AutoLoad` - 1 dòng) hoặc `LoadRow`
```csharp
private void dgvNhanVien_SelectionChanged(object sender, EventArgs e)
{
    DataRowView row = GridHelper.GetSelectedRow(dgvNhanVien);
    if (row == null) return;

    // Cách A (Tự động): tự khớp txtMaNV -> MaNV, cboPhongBan (ValueMember) -> MaPB, rdoNam (Tag="GioiTinh:1") -> GioiTinh
    FormMapper.AutoLoad(this, row);

    // Cách B (Chỉ định tay từng cặp nếu tên control khác hẳn tên cột DB):
    // FormMapper.LoadRow(row, FormMapper.Map(txtMaNV, "MaNV"), FormMapper.Map(rdoNam, rdoNu, "GioiTinh"), ...);

    txtMaNV.ReadOnly = true;
}
```

> [!TIP]
> Có thể nạp thẳng từ câu query: `FormMapper.AutoLoadSql(this, "SELECT * FROM NhanVien WHERE MaNV = @ma", DbHelper.Param("ma", "NV001"));`.

### 4.2 Đọc dữ liệu từ Form ➔ Chuẩn bị lưu xuống CSDL (`AutoGetValues` / `GetValues`)
```csharp
// Cách A (Tự động): truyền DataTable nguồn làm schema để tự ép kiểu double/decimal/int/bool chuẩn SQL Server
var values = FormMapper.AutoGetValues(this, dgvNhanVien.DataSource as DataTable);

// Cách B (Chỉ định tay):
// var values = FormMapper.GetValues(FormMapper.Map(txtMaNV, "MaNV"), FormMapper.MapDouble(txtHeSoLuong, "HeSoLuong"), ...);
```

### 4.3 Xóa trắng Form (`AutoClear` / `ClearControls`) & Bỏ chọn Grid (`ClearSelection`)
```csharp
private void btnLamMoi_Click(object sender, EventArgs e)
{
    FormMapper.AutoClear(this);
    rdoNam.Checked = true;
    GridHelper.ClearSelection(dgvNhanVien);
    txtMaNV.ReadOnly = false;
    txtMaNV.Focus();
}
```

---

## 5. `Validator` - Kiểm tra dữ liệu nhập vào

> **Nhiệm vụ:** Kiểm tra người dùng nhập đúng hay sai. Nếu sai thì **báo lỗi bằng tiếng Việt** và **tự động nhảy con trỏ chuột đến đúng ô đó**.

### 5.1 Các quy tắc kiểm tra thông dụng nhất trong phòng thi:
| Quy tắc | Cú pháp | Ý nghĩa |
|---|---|---|
| **Bắt buộc nhập** | `Validator.Required(txtMaNV, "Mã nhân viên")` | Không được để trống |
| **Đúng định dạng (Regex)** | `Validator.Pattern(txtMaNV, "Mã nhân viên", @"^NV\d{3}$", "Mã phải có dạng NV001!")` | Ví dụ: `NV` kèm 3 chữ số |
| **Bắt buộc chọn ComboBox** | `Validator.Selected(cboPhongBan, "Phòng ban")` | Không được để chưa chọn |
| **Khoảng số thực / Tiền** | `Validator.DecimalRange(txtLuong, "Lương", 100000m, 50000000m)` | Lương từ 100k đến 50tr |
| **Khoảng số nguyên** | `Validator.IntRange(txtSoLuong, "Số lượng", 1, 100)` | Số từ 1 đến 100 |
| **Độ tuổi hôm nay** | `Validator.AgeRange(dtpNgaySinh, "Tuổi", 18, 65)` | Từ 18 đến 65 tuổi |
| **Độ tuổi tại ngày vào làm** | `Validator.AgeRangeAtDate(dtpNgaySinh, dtpNgayVaoLam, "Tuổi", 18, 120)` | **Đặc sản đề thi!** Truyền thẳng `dtpNgayVaoLam` |
| **So sánh 2 ngày** | `Validator.DateBeforeOrEqual(dtpNgaySinh, dtpNgayVaoLam, "Ngày sinh")` | Ngày 1 `<=` Ngày 2 |
| **Không vượt quá hôm nay** | `Validator.NotFutureDate(dtpNgayNhap, "Ngày nhập")` | Kiểm tra `<= DateTime.Today` |

### 5.2 Cách gọi trong nút Thêm / Sửa:
```csharp
private ValidationResult KiemTraHopLe()
{
    return Validator.ValidateAll(
        Validator.Required(txtMaNV, "Mã nhân viên"),
        Validator.Pattern(txtMaNV, "Mã nhân viên", @"^NV\d{3}$", "Mã nhân viên phải bắt đầu bằng NV và 3 chữ số!"),
        Validator.Required(txtHoTen, "Họ và tên"),
        Validator.Selected(cboPhongBan, "Phòng ban"),
        Validator.DecimalRange(txtHeSoLuong, "Hệ số lương", 2.34m, 8.0m),
        Validator.AgeRangeAtDate(dtpNgaySinh, dtpNgayVaoLam, "Tuổi nhân viên", 18, 120, "Nhân viên phải đủ 18 tuổi tính đến ngày vào làm!")
    );
}

// Khi bấm nút Thêm/Sửa:
var val = KiemTraHopLe();
if (!val.IsValid)
{
    MessageHelper.Warning(val.ErrorMessage); // Báo lỗi
    FormHelper.FocusControl(val.ErrorControl); // Nhảy con trỏ tới ô sai
    return; // Dừng lại không thực hiện tiếp
}
```

---

## 6. `CrudHelper` - Thêm, Sửa, Xóa không cần viết SQL

> **Nhiệm vụ:** Không cần ngồi nối chuỗi `INSERT INTO ... VALUES (...)` dài dòng và dễ sai cú pháp.

### 6.1 Nút Thêm mới hoàn chỉnh (Full mẫu)
```csharp
private void btnThem_Click(object sender, EventArgs e)
{
    // 1. Kiểm tra dữ liệu hợp lệ
    var val = KiemTraHopLe();
    if (!val.IsValid)
    {
        MessageHelper.Warning(val.ErrorMessage);
        FormHelper.FocusControl(val.ErrorControl);
        return;
    }

    // 2. Kiểm tra trùng khóa chính
    string ma = txtMaNV.Text.Trim();
    if (CrudHelper.Exists("NhanVien", "MaNV", ma))
    {
        MessageHelper.DuplicateKey("Mã nhân viên", ma); // Hiện: "Mã nhân viên 'NV001' đã tồn tại!"
        FormHelper.FocusControl(txtMaNV);
        return;
    }

    // 3. Lưu vào CSDL
    var values = LayDuLieuTuForm();
    if (CrudHelper.Insert("NhanVien", values) > 0)
    {
        MessageHelper.Success("thêm mới"); // Hiện: "Thao tác thêm mới thành công!"
        LoadData();                        // Nạp lại DataGridView
        btnLamMoi_Click(null, null);       // Xóa trắng form
    }
}
```

### 6.2 Nút Sửa hoàn chỉnh (Full mẫu)
```csharp
private void btnSua_Click(object sender, EventArgs e)
{
    // 1. Kiểm tra đã chọn dòng trên bảng chưa
    if (!GridHelper.HasSelection(dgvNhanVien))
    {
        MessageHelper.NoSelection("nhân viên cần sửa");
        return;
    }

    // 2. Kiểm tra dữ liệu hợp lệ
    var val = KiemTraHopLe();
    if (!val.IsValid)
    {
        MessageHelper.Warning(val.ErrorMessage);
        FormHelper.FocusControl(val.ErrorControl);
        return;
    }

    // 3. Cập nhật vào CSDL (Mã khóa chính được truyền ở tham số riêng)
    string ma = txtMaNV.Text.Trim();
    var values = LayDuLieuTuForm();

    // Cú pháp: Update(Tên_Bảng, Dữ_Liệu, Cột_Khóa, Giá_Trị_Khóa)
    if (CrudHelper.Update("NhanVien", values, "MaNV", ma) > 0)
    {
        MessageHelper.Success("cập nhật");
        LoadData();
    }
}
```

### 6.3 Nút Xóa hoàn chỉnh (Full mẫu)
```csharp
private void btnXoa_Click(object sender, EventArgs e)
{
    // 1. Kiểm tra đã chọn dòng chưa
    if (!GridHelper.HasSelection(dgvNhanVien))
    {
        MessageHelper.NoSelection("nhân viên cần xóa");
        return;
    }

    string ma = txtMaNV.Text.Trim();
    string ten = txtHoTen.Text.Trim();

    // 2. Hỏi xác nhận Yes/No và xóa:
    // Cú pháp: Delete(Tên_Bảng, Cột_Khóa, Giá_Trị_Khóa)
    if (MessageHelper.ConfirmDelete($"nhân viên {ten} (Mã: {ma})") 
        && CrudHelper.Delete("NhanVien", "MaNV", ma) > 0)
    {
        MessageHelper.Success("xóa");
        LoadData();
        btnLamMoi_Click(null, null);
    }
}
```

---

## 7. `FilterBuilder` - Tạo bộ lọc tìm kiếm linh hoạt

> **Nhiệm vụ:** Dùng cho **Form 2 (Tra cứu & Thống kê)**. Bạn có nhiều ô tìm kiếm (ComboBox, TextBox, Giá tiền...). Nếu người dùng **để trống ô nào hoặc chọn "Tất cả"**, `FilterBuilder` sẽ **tự động bỏ qua** điều kiện đó!

### Cách viết hàm Tìm kiếm kết hợp đa điều kiện:
```csharp
private void TimKiem()
{
    // 1. Khởi tạo bộ lọc:
    var filter = FilterBuilder.New()
        // Nếu chọn "-- Tất cả --" thì tự bỏ qua, nếu chọn phòng cụ thể thì thêm WHERE nv.MaPB = @p
        .SkipIfAllSelected(cboTimPhongBan, "nv.MaPB")
        
        // Nếu ô tên có chữ thì tìm gần đúng LIKE '%tên%', nếu để trống thì tự bỏ qua
        .Contains("nv.HoTen", txtTimHoTen.Text);

    // Xử lý ComboBox Giới tính (0: Tất cả, 1: Nam, 2: Nữ)
    if (cboGioiTinh.SelectedIndex == 1) filter.Equal("nv.GioiTinh", 1);
    else if (cboGioiTinh.SelectedIndex == 2) filter.Equal("nv.GioiTinh", 0);

    // 2. Build ra câu WHERE và mảng tham số SQL:
    var (where, parms) = filter.Build();

    // 3. Ghép vào câu SELECT:
    string sql = @"SELECT nv.MaNV, nv.HoTen, 
                          CASE nv.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END AS GioiTinhText,
                          nv.NgaySinh, pb.TenPB, nv.Luong
                   FROM NhanVien nv 
                   JOIN PhongBan pb ON nv.MaPB = pb.MaPB" 
                   + where;

    DataTable dtKetQua = DbHelper.GetDataTable(sql, parms);

    // 4. Nạp lên DataGridView kết quả:
    GridHelper.Bind(dgvKetQua, dtKetQua,
        new GridHelper.GridColumn("MaNV", "Mã NV", 100),
        new GridHelper.GridColumn("HoTen", "Họ tên", 180),
        new GridHelper.GridColumn("TenPB", "Phòng ban", 140),
        new GridHelper.GridColumn("Luong", "Lương", 120, "N0")
    );

    // 5. Tính toán các chỉ số thống kê (Xem Mục 8)
    CapNhatThongKe(dtKetQua);
}
```

---

## 8. `DataHelper` - Thống kê dữ liệu sau lọc

> **Nhiệm vụ:** Tính toán trực tiếp trên `DataTable` sau khi tìm kiếm (tốc độ siêu nhanh, không cần gọi lại SQL).

```csharp
private void CapNhatThongKe(DataTable dt)
{
    // 1. Đếm số lượng bản ghi tìm được:
    int soLuong = DataHelper.Count(dt);
    lblSoLuong.Text = $"Số lượng: {soLuong}";

    // 2. Tính tổng tiền một cột:
    decimal tongLuong = DataHelper.Sum(dt, "Luong");
    lblTongTien.Text = $"Tổng lương: {tongLuong:N0} đ";

    // 3. Tìm giá trị lớn nhất (Max):
    decimal luongCaoNhat = Convert.ToDecimal(DataHelper.Max<object>(dt, "Luong") ?? 0);
    lblCaoNhat.Text = $"Lương cao nhất: {luongCaoNhat:N0} đ";

    // 4. Thông báo nếu không có dữ liệu:
    if (soLuong == 0)
    {
        MessageHelper.Info("Không tìm thấy dữ liệu phù hợp!");
    }
}
```

---

## 9. `MessageHelper` - Hộp thoại thông báo chuẩn

> **Nhiệm vụ:** Hiển thị thông báo tiếng Việt có dấu, có icon đẹp mắt, đúng chuẩn barem thi cử.

| Bạn muốn báo gì? | Gọi hàm này | Kết quả hiển thị |
|---|---|---|
| Báo thành công | `MessageHelper.Success("thêm mới");` | `"Thao tác thêm mới thành công!"` (Icon Info) |
| Cảnh báo dữ liệu sai | `MessageHelper.Warning("Vui lòng nhập họ tên!");` | `"Vui lòng nhập họ tên!"` (Icon Warning) |
| Báo trùng mã | `MessageHelper.DuplicateKey("Mã SV", "SV001");` | `"Mã SV 'SV001' đã tồn tại"` |
| Quên chọn dòng | `MessageHelper.NoSelection("dòng cần sửa");` | `"Vui lòng chọn dòng cần sửa cần thao tác"` |
| Hỏi xóa Yes/No | `MessageHelper.ConfirmDelete("bác sĩ A");` | `"Bạn có chắc chắn muốn xóa bác sĩ A không?"` (2 nút Yes/No) |
| Hỏi thoát Yes/No | `MessageHelper.Confirm("Bạn có muốn thoát?", "Xác nhận");` | Trả về `true` nếu bấm Yes |

---

## 10. `ReportHelper` - Form Báo Cáo dùng chung & Hỗ trợ RDLC (In, PDF, Excel)

> **Nhiệm vụ:** Hoàn thành câu hỏi Báo cáo (2.5 điểm) nhanh nhất có thể! Bạn có 2 lựa chọn tùy theo yêu cầu giám khảo:
> - **Lựa chọn A (Siêu tốc - Tự gom bảng & Kéo thả trực tiếp):** Gọi `ReportHelper.ShowReport(_dtKetQua)` hoặc `ReportHelper.ShowReport(dgvKetQua, "TIÊU ĐỀ", owner: this)` để tự gom **toàn bộ trường dữ liệu** vào 1 bảng chuẩn A4; người dùng có thể **kéo-thả ẩn/hiện/đổi thứ tự/gom nhóm cột, sửa tiêu đề, đổi tên cột** ngay trên Form xem trước rồi nhấn **In / Lưu PDF**, **Xem trước** hoặc **Xuất Excel**.
> - **Lựa chọn B (Nếu đề bắt buộc dùng RDLC `ReportViewer`):** Gọi `ReportHelper.PrepareReportTable(...)`, `ReportHelper.BindRdlc(...)` và `ReportHelper.ExportRdlc(...)` để nạp DataSet, truyền Parameter và xuất PDF/Excel trực tiếp qua `SaveFileDialog`.

### 10.1 Cách 1: Gọi siêu tốc 1 dòng code (Tự gom toàn bộ cột + Kéo thả chỉnh trực tiếp)
Nếu không muốn khai báo cứng cấu trúc cột trong code, chỉ cần truyền thẳng `DataTable` hoặc `DataGridView`:
```csharp
private void btnXuatBaoCao_Click(object sender, EventArgs e)
{
    if (_dtKetQua == null || _dtKetQua.Rows.Count == 0)
    {
        MessageHelper.Warning("Không có dữ liệu để xuất báo cáo!");
        return;
    }

    // Cách gọn nhất: truyền DataTable, DataGridView hoặc câu SQL trực tiếp -> tự gom cột & kéo-thả chỉnh sửa:
    ReportHelper.ShowReport(_dtKetQua, "BÁO CÁO THỐNG KÊ", owner: this);
    // ReportHelper.ShowReport(dgvKetQua, "BÁO CÁO THỐNG KÊ", owner: this);
    // ReportHelper.ShowReport("SELECT * FROM NhanVien", "DANH SÁCH NHÂN VIÊN", owner: this);

    // Hoặc chỉ định sẵn cột gom nhóm + cột tính tổng:
    // ReportHelper.ShowReport(_dtKetQua, "BẢNG LƯƠNG", "TenPB", "Luong", "Tất cả", this, "CÔNG TY ABC", "Phòng ban: ");
}
```

### 10.2 Cách 2: Tùy chỉnh danh sách cột chi tiết (Khớp mẫu Đề 01 từng cột)
Khi đề thi yêu cầu hiển thị chính xác các cột chỉ định kèm căn lề, định dạng và tên công ty:

```csharp
private void btnXuatBaoCao_Click(object sender, EventArgs e)
{
    if (_dtKetQua == null || _dtKetQua.Rows.Count == 0)
    {
        MessageHelper.Warning("Không có dữ liệu để xuất báo cáo");
        return;
    }

    ReportHelper.ShowReport(
        dataTable: _dtKetQua,
        reportTitle: "BẢNG LƯƠNG NHÂN VIÊN",
        filterCondition: $"Phòng ban: {cboPhongBan.Text}; Giới tính: {cboGioiTinh.Text}",
        groupByColumn: "TenPB",
        groupPrefix: "Phòng ban: ",
        columns: new[] {
            new ReportColumn("MaNV", "Mã NV", HorizontalAlignment.Center),
            new ReportColumn("HoTen", "Họ tên"),
            new ReportColumn("NgaySinh", "Ngày sinh", "dd/MM/yyyy", HorizontalAlignment.Center),
            new ReportColumn("HeSoLuong", "Hệ số lương", "0.##", HorizontalAlignment.Right),
            new ReportColumn("Luong", "Lương", "N0", HorizontalAlignment.Right, ReportSummaryMode.Sum)
            // Hỗ trợ ReportSummaryMode: None, Sum, Average, Count, Max, Min; tùy chọn landscape: true, fontSizePt: 14
        },
        companyName: "CÔNG TY TNHH AN PHÁT"
    );
}
```

### 10.3 Cách 3: Hỗ trợ RDLC `ReportViewer` (Nạp bảng + Parameter + Xuất PDF/Excel)
Khi đề thi bắt buộc dùng control `ReportViewer` (`.rdlc`) và `LocalReport.Render`:

```csharp
// Trong Form chứa reportViewer1:
private void frmBaoCao_Load(object sender, EventArgs e)
{
    // Tự thêm cột STT (1..N) nếu cần dùng trong RDLC
    DataTable dtReport = ReportHelper.PrepareReportTable(_dtKetQua);

    var parameters = new Dictionary<string, string>
    {
        { "pDieuKienLoc", _dieuKienLoc },
        { "pNgayLap", DateTime.Now.ToString("dd/MM/yyyy") }
    };

    ReportHelper.BindRdlc(reportViewer1, "dsNhanVien", dtReport, parameters);
}

private void btnXuatPDF_Click(object sender, EventArgs e)
{
    ReportHelper.ExportRdlc(reportViewer1, "PDF", "BangLuongNhanVien");
}

private void btnXuatExcel_Click(object sender, EventArgs e)
{
    ReportHelper.ExportRdlc(reportViewer1, "EXCELOPENXML", "BangLuongNhanVien");
}
```

### 10.4 Cách 4: Hỗ trợ Crystal Report (`CrystalReportViewer` & `ExportToDisk`)
Khi đề thi dùng Crystal Report (`ReportDocument` / `CrystalReportViewer`):

```csharp
private rptBangLuong _rpt = new rptBangLuong();

private void frmBaoCao_Load(object sender, EventArgs e)
{
    var parms = new Dictionary<string, object> { { "pDieuKienLoc", _dieuKienLoc } };
    ReportHelper.BindCrystalReport(crystalReportViewer1, _rpt, _dtKetQua, parms);
}

private void btnXuatPDF_Click(object sender, EventArgs e)
{
    ReportHelper.ExportCrystalReport(_rpt, "PDF", "BangLuongNhanVien");
}
```

---

## 11. ⚠️ 5 BẪY "CHẾT NGƯỜI" NEWBIE HAY GẶP NHẤT


1. **Quên `SELECT` cột mã khóa ngoại:**
   - *Hiện tượng:* Click dòng trên Grid nhưng ComboBox không chịu nhảy.
   - *Cách sửa:* Trong câu SQL `SELECT` nạp Grid, luôn thêm cột mã (ví dụ `bs.MaCK` hoặc `nv.MaPB`).

2. **Kiểm tra trùng mã khi bấm Sửa:**
   - *Hiện tượng:* Bấm nút Sửa báo lỗi `"Mã đã tồn tại"`.
   - *Cách sửa:* Chỉ gọi `CrudHelper.Exists` ở nút **Thêm**. Nút **Sửa** tuyệt đối không kiểm tra trùng mã vì mã chính là dòng đang sửa!

3. **Tính sai tuổi:**
   - *Hiện tượng:* Bị trừ 1 điểm vì lấy năm trừ năm (`2024 - 2000 = 24`) trong khi người ta sinh vào tháng 12 thì đến tháng 3 mới 23 tuổi.
   - *Cách sửa:* Dùng `Validator.AgeRangeAtDate(...)` hoặc `Validator.CalculateAge(...)`.

4. **Quên nạp ComboBox trước khi LoadData:**
   - *Hiện tượng:* Dòng Grid chọn nhưng ComboBox trống trơn.
   - *Cách sửa:* Trong `Form_Load`, luôn gọi `ComboBoxHelper.Bind(...)` trước rồi mới gọi `LoadData()`.

5. **Lỗi chưa chọn dòng mà bấm Sửa/Xóa:**
   - *Hiện tượng:* Chương trình văng lỗi `NullReferenceException`.
   - *Cách sửa:* Luôn đặt dòng `if (!GridHelper.HasSelection(dgv)) return;` ở đầu sự kiện Sửa và Xóa.

---

## 12. Tài liệu chi tiết từng phần & Làm nhanh bài thi
- [01_DATABASE_CRUD.md](01_DATABASE_CRUD.md) — `DbHelper`, `CrudHelper`, `FilterBuilder`
- [02_BINDING_MAPPING.md](02_BINDING_MAPPING.md) — `ComboBoxHelper`, `GridHelper`, `FormMapper`
- [03_VALIDATION.md](03_VALIDATION.md) — `Validator`, `ValidationResult`, `FromSchema`
- [04_UTILITIES.md](04_UTILITIES.md) — `DataHelper`, `MessageHelper`, `FormHelper`, `ReportHelper`, `FormGenericManager`
- [05_EXAM_QUICKSTART.md](05_EXAM_QUICKSTART.md) — **Hỗ trợ làm nhanh bài thi theo cấu trúc đề (Form 1 / Form 2 / Báo cáo / Checklist)**
- [EXAM_CHEATSHEET.md](EXAM_CHEATSHEET.md) — Tờ phao tra cứu nhanh
- [COMMON_ERRORS.md](COMMON_ERRORS.md) — Các lỗi phòng thi thường gặp & cách sửa
