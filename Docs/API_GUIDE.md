# 📘 CẨM NANG TRA CỨU API EXAMHELPER PRO
> **Dành cho người mới bắt đầu (Newbie Guide)**  
> *Ngắn gọn – Dễ hiểu – Cần làm gì tra đúng mục đó – Copy & Chạy ngay!*

---

## 🧭 BẢN ĐỒ DÀNH CHO NEWBIE: "TÔI MUỐN LÀM GÌ?"

Nếu bạn mới học WinForms và cảm thấy bối rối trước hàng đống code, **hãy tra cứu theo nhu cầu của bạn ở bảng dưới đây**:

| Khi bạn muốn... | Dùng Lớp / Hàm này | Nhảy đến mục |
|---|---|:---:|
| Kết nối SQL Server hoặc chạy câu lệnh `SELECT` | `DbHelper` | [Mục 1](#1-dbhelper---kết-nối--truy-vấn-sql-server) |
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

### 1.1 Khai báo kết nối trong `Form_Load` (Bắt buộc làm đầu tiên!)
```csharp
private void Form1_Load(object sender, EventArgs e)
{
    // Cú pháp kết nối máy cục bộ (Dấu chấm '.' đại diện cho máy của bạn)
    DbHelper.SetDefaultConnectionString("Server=.;Database=Tên_Database_Của_Đề;Trusted_Connection=True;");
    
    // Nếu máy phòng thi dùng SQL Express có dấu gạch chéo:
    // DbHelper.SetDefaultConnectionString(@"Server=.\SQLEXPRESS;Database=Tên_DB;Trusted_Connection=True;");
}
```

### 1.2 Lấy dữ liệu ra bảng (`GetDataTable`)
Dùng khi bạn muốn chạy câu lệnh `SELECT` để lấy dữ liệu nạp lên Grid hoặc ComboBox.

```csharp
// Trường hợp 1: SELECT toàn bộ (không có điều kiện WHERE)
DataTable dt = DbHelper.GetDataTable("SELECT * FROM NhanVien");

// Trường hợp 2: SELECT có điều kiện tham số an toàn (Dùng DbHelper.Param)
string sql = "SELECT * FROM NhanVien WHERE MaPB = @mapb AND Luong >= @luong";
DataTable dt = DbHelper.GetDataTable(sql,
    DbHelper.Param("@mapb", "PB01"),
    DbHelper.Param("@luong", 5000000m)
);
```

> [!TIP]
> **DbHelper.Param có gì đặc biệt?**  
> Khi giá trị bị `null`, nó tự đổi thành `DBNull.Value` của SQL Server. Bạn không bao giờ bị lỗi `ArgumentNullException`.

---

## 2. `ComboBoxHelper` - Nạp ComboBox cực dễ

> **Nhiệm vụ:** Đổ danh mục (Phòng ban, Chuyên khoa, Lớp...) vào ô ComboBox.

### 2.1 Dùng cho Form nhập liệu (Form 1)
```csharp
// Đọc bảng danh mục từ CSDL
DataTable dtPB = DbHelper.GetDataTable("SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB");

// Nạp vào ComboBox:
// Tham số 1: Control ComboBox
// Tham số 2: DataTable chứa dữ liệu
// Tham số 3: Tên cột LƯU TRỮ (khóa ngoại ẩn - ValueMember)
// Tham số 4: Tên cột HIỂN THỊ (chữ nhìn thấy - DisplayMember)
ComboBoxHelper.Bind(cboPhongBan, dtPB, "MaPB", "TenPB");
```

### 2.2 Dùng cho Form Tìm kiếm / Thống kê (Form 2)
Cần có thêm dòng `-- Tất cả --` ở đầu danh sách để khi người dùng chọn thì tìm tất cả.

```csharp
DataTable dtPB = DbHelper.GetDataTable("SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB");

// Tự động chèn thêm dòng "-- Tất cả --" ở dòng đầu tiên (Index = 0)
ComboBoxHelper.BindWithAll(cboTimPhongBan, dtPB, "MaPB", "TenPB", "-- Tất cả phòng ban --");
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
    // 1. Viết câu SQL (nhớ SELECT cả cột mã nếu muốn đổ lên ComboBox)
    string sql = @"SELECT nv.MaNV, nv.HoTen, 
                          CASE nv.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END AS GioiTinhText,
                          nv.NgaySinh, pb.TenPB, nv.Luong, nv.MaPB
                   FROM NhanVien nv 
                   JOIN PhongBan pb ON nv.MaPB = pb.MaPB";

    DataTable dt = DbHelper.GetDataTable(sql);

    // 2. Nạp lên Grid kèm định dạng từng cột:
    // new GridColumn("Tên_Cột_Trong_SQL", "Tiêu_Đề_Hiển_Thị", Độ_Rộng, "Định_Dạng")
    GridHelper.Bind(dgvNhanVien, dt,
        new GridHelper.GridColumn("MaNV", "Mã nhân viên", 100),
        new GridHelper.GridColumn("HoTen", "Họ và tên", 180),
        new GridHelper.GridColumn("GioiTinhText", "Giới tính", 80),
        new GridHelper.GridColumn("NgaySinh", "Ngày sinh", 100, "dd/MM/yyyy"), // Format ngày
        new GridHelper.GridColumn("TenPB", "Phòng ban", 140),
        new GridHelper.GridColumn("Luong", "Mức lương", 120, "N0")            // N0: Format tiền 15.000.000
    );

    // 3. Khóa không cho người dùng sửa trực tiếp trên ô, chọn cả dòng
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

### 4.1 Chiều 1: Bấm vào dòng DataGridView ➔ Đổ lên Form (`LoadRow`)
Tạo sự kiện `SelectionChanged` của `DataGridView` (click đúp vào DataGridView trong Designer):

```csharp
private void dgvNhanVien_SelectionChanged(object sender, EventArgs e)
{
    // 1. Lấy dòng đang chọn
    DataRowView row = GridHelper.GetSelectedRow(dgvNhanVien);
    if (row == null) return;

    // 2. Đổ tự động lên các control trong 1 lệnh duy nhất!
    FormMapper.LoadRow(row,
        FormMapper.Map(txtMaNV, "MaNV"),
        FormMapper.Map(txtHoTen, "HoTen"),
        FormMapper.Map(dtpNgaySinh, "NgaySinh"),
        FormMapper.Map(cboPhongBan, "MaPB"), // Tự động chọn đúng mục trong ComboBox!
        FormMapper.Map(txtLuong, "Luong")
    );

    // Xử lý RadioButton giới tính (nếu có):
    if (row["GioiTinh"] != DBNull.Value)
    {
        rdoNam.Checked = Convert.ToBoolean(row["GioiTinh"]);
        rdoNu.Checked = !rdoNam.Checked;
    }

    // Khóa ô mã khi đang xem/sửa dòng cũ
    txtMaNV.ReadOnly = true;
}
```

> [!WARNING]
> **Bẫy Newbie:** Để `cboPhongBan` nhảy đúng mục, trong câu `SELECT` nạp Grid bạn **BẮT BUỘC PHẢI CÓ CỘT `MaPB`**. Nếu chỉ SELECT `TenPB` thì `FormMapper` không có dữ liệu để map vào ComboBox!

### 4.2 Chiều 2: Đọc dữ liệu từ Form ➔ Chuẩn bị lưu xuống CSDL (`GetValues`)
Dùng khi Thêm mới hoặc Sửa:

```csharp
private Dictionary<string, object> LayDuLieuTuForm()
{
    var values = FormMapper.GetValues(
        FormMapper.Map(txtMaNV, "MaNV"),
        FormMapper.Map(txtHoTen, "HoTen"),
        FormMapper.Map(dtpNgaySinh, "NgaySinh"),
        FormMapper.Map(cboPhongBan, "MaPB"),
        FormMapper.Map(txtLuong, "Luong")
    );

    // Gán thêm RadioButton giới tính:
    values["GioiTinh"] = rdoNam.Checked ? 1 : 0;

    return values;
}
```

### 4.3 Xóa trắng Form (`ClearControls`)
Dùng cho nút **Làm mới**:
```csharp
private void btnLamMoi_Click(object sender, EventArgs e)
{
    // Tự động xóa trắng TextBox, ComboBox về đầu, DateTimePicker về hôm nay
    FormMapper.ClearControls(txtMaNV, txtHoTen, txtLuong);
    rdoNam.Checked = true;
    dtpNgaySinh.Value = DateTime.Today;
    if (cboPhongBan.Items.Count > 0) cboPhongBan.SelectedIndex = 0;

    // Mở khóa ô mã để nhập mới
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
| **Độ tuổi tại ngày vào làm** | `Validator.AgeRangeAtDate(dtpNgaySinh, "Tuổi", 24, 100)` | **Đặc sản đề thi!** |

### 5.2 Cách gọi trong nút Thêm / Sửa:
```csharp
private ValidationResult KiemTraHopLe()
{
    return Validator.ValidateAll(
        dtpNgayVaoLam.Value, // Ngày mốc tính tuổi (nếu đề thi yêu cầu)
        Validator.Required(txtMaNV, "Mã nhân viên"),
        Validator.Pattern(txtMaNV, "Mã nhân viên", @"^NV\d{3}$", "Mã nhân viên phải bắt đầu bằng NV và 3 chữ số!"),
        Validator.Required(txtHoTen, "Họ và tên"),
        Validator.Selected(cboPhongBan, "Phòng ban"),
        Validator.DecimalRange(txtLuong, "Mức lương", 100000m, 50000000m),
        Validator.AgeRangeAtDate(dtpNgaySinh, "Tuổi nhân viên", 18, 65, "Nhân viên phải từ đủ 18 tuổi tính đến ngày vào làm!")
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
| Báo trùng mã | `MessageHelper.DuplicateKey("Mã SV", "SV001");` | `"Mã SV 'SV001' đã tồn tại trong hệ thống!"` |
| Quên chọn dòng | `MessageHelper.NoSelection("dòng cần xóa");` | `"Vui lòng chọn dòng cần xóa cần thao tác!"` |
| Hỏi xóa Yes/No | `MessageHelper.ConfirmDelete("bác sĩ A");` | `"Bạn có chắc chắn muốn xóa bác sĩ A không?"` (2 nút Yes/No) |
| Hỏi thoát Yes/No | `MessageHelper.Confirm("Bạn có muốn thoát?", "Xác nhận");` | Trả về `true` nếu bấm Yes |

---

## 10. `ReportHelper` - Form Báo Cáo dùng chung (Chuẩn In & Excel)

> **Nhiệm vụ:** Hoàn thành câu hỏi Báo cáo (2.5 điểm) trong **30 giây**! Bạn không cần kéo thả Form mới hay viết hàng chục dòng nối chuỗi HTML. Chỉ cần gọi `ReportHelper.ShowReport` là có ngay Form xem trước chuyên nghiệp, nút **In ấn** và nút **Xuất Excel (.xls)**.

### 10.1 Cách 1: Gọi siêu tốc 1 dòng code (Tự động tất cả)
Nếu bạn cần làm nhanh trong phòng thi:
```csharp
private void btnXuatBaoCao_Click(object sender, EventArgs e)
{
    if (_dtKetQua == null || _dtKetQua.Rows.Count == 0)
    {
        MessageHelper.Warning("Không có dữ liệu để xuất báo cáo!");
        return;
    }

    // Tự động gom nhóm theo Chuyên khoa và tính tổng cột Đơn giá khám:
    ReportHelper.ShowReport(
        dataTable: _dtKetQua,
        reportTitle: "BÁO CÁO DANH SÁCH BÁC SĨ THEO CHUYÊN KHOA",
        groupByColumn: "TenCK",     // Cột gom nhóm (hoặc null nếu không gom)
        sumColumn: "DonGiaKham",    // Cột cần tính tổng cộng
        filterCondition: $"Chuyên khoa: {cboChuyenKhoa.Text}; Giới tính: {cboGioiTinh.Text}"
    );
}
```

### 10.2 Cách 2: Tùy chỉnh danh sách cột chi tiết (Chuẩn mẫu 10 điểm)
Khi đề thi yêu cầu hiển thị chính xác các cột chỉ định kèm căn lề và định dạng:

```csharp
private void btnXuatBaoCao_Click(object sender, EventArgs e)
{
    if (_dtKetQua == null || _dtKetQua.Rows.Count == 0)
    {
        MessageHelper.Warning("Không có dữ liệu để xuất báo cáo!");
        return;
    }

    ReportHelper.ShowReport(
        dataTable: _dtKetQua,
        reportTitle: "BÁO CÁO DANH SÁCH BÁC SĨ",
        filterCondition: $"Chuyên khoa: {cboChuyenKhoa.Text}; Giới tính: {cboGioiTinh.Text}",
        groupByColumn: "TenCK",
        groupPrefix: "🏥 Chuyên khoa: ",
        columns: new[] {
            new ReportColumn("MaBS", "Mã BS", HorizontalAlignment.Center),
            new ReportColumn("HoTen", "Họ và tên"),
            new ReportColumn("GioiTinhHienThi", "Giới tính", HorizontalAlignment.Center),
            new ReportColumn("NgaySinh", "Ngày sinh", "dd/MM/yyyy", HorizontalAlignment.Center),
            new ReportColumn("NgayVaoLam", "Ngày vào làm", "dd/MM/yyyy", HorizontalAlignment.Center),
            new ReportColumn("DonGiaKham", "Đơn giá khám", "N0", HorizontalAlignment.Right, isSum: true) // isSum = tính tổng
        }
    );
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
