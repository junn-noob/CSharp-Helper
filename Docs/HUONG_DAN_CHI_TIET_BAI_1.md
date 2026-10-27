# HƯỚNG DẪN CHI TIẾT TỪNG Ý BÀI 1: QUẢN LÝ PHÒNG KHÁM & BÁC SĨ
> **Cơ sở dữ liệu:** `QLPhongKham`  
> **Áp dụng thư viện:** `ExamHelper Pro`  
> **Thời lượng đề xuất:** 75 - 90 phút (Thực tế áp dụng thư viện chỉ mất ~25 - 30 phút)

---

## 📑 MỤC LỤC
1. [Phân tích đề bài & Cấu trúc điểm](#1-phân-tích-đề-bài--cấu-trúc-điểm)
2. [Bước 0: Script CSDL & Bảng danh sách Control](#2-bước-0-script-csdl--bảng-danh-sách-control)
3. [Form 1: Quản lý Bác sĩ (frmQuanLyBacSi - 5.0 điểm)](#3-form-1-quản-lý-bác-sĩ-frmquanlybacsi---50-điểm)
   - [Ý 1: DataGridView danh sách & Cột tính toán Phụ cấp ngoài giờ](#ý-1-datagridview-danh-sách--cột-tính-toán-phụ-cấp-ngoài-giờ)
   - [Ý 2: Nạp ComboBox Chuyên khoa trong Form_Load](#ý-2-nạp-combobox-chuyên-khoa-trong-form_load)
   - [Ý 3: Click dòng trên Grid nạp lên Control & Khóa ô Mã BS](#ý-3-click-dòng-trên-grid-nạp-lên-control--khóa-ô-mã-bs)
   - [Ý 4: Nút Thêm (&T) - Ràng buộc tuổi & Đơn giá & Trùng mã](#ý-4-nút-thêm-t---ràng-buộc-tuổi--đơn-giá--trùng-mã)
   - [Ý 5: Nút Sửa (&S) - Cập nhật dữ liệu an toàn](#ý-5-nút-sửa-s---cập-nhật-dữ-liệu-an-toàn)
   - [Ý 6: Nút Xóa (&X) - Hộp thoại xác nhận Yes/No chuẩn](#ý-6-nút-xóa-x---hộp-thoại-xác-nhận-yesno-chuẩn)
   - [Ý 7: Nút Làm mới (&L), Thống kê (&K), Thoát (&H)](#ý-7-nút-làm-mới-l-thống-kê-k-thoát-h)
4. [Form 2: Thống kê & Tra cứu Bác sĩ (frmThongKeBacSi - 2.5 điểm)](#4-form-2-thống-kê--tra-cứu-bác-sĩ-frmthongkebacsi---25-điểm)
   - [Ý 1: ComboBox có dòng "Tất cả" & ComboBox Giới tính](#ý-1-combobox-có-dòng-tất-cả--combobox-giới-tính)
   - [Ý 2: Nút Tìm kiếm (&T) - Lọc AND kết hợp](#ý-2-nút-tìm-kiếm-t---lọc-and-kết-hợp)
   - [Ý 3: Cập nhật các chỉ số Thống kê sau lọc (Count, Sum, Max)](#ý-3-cập-nhật-các-chỉ-số-thống-kê-sau-lọc-count-sum-max)
   - [Ý 4: Nút Đóng (&N)](#ý-4-nút-đóng-n)
5. [Form 3: Báo cáo Gom nhóm theo Chuyên khoa & Xuất Excel (2.5 điểm)](#5-form-3-báo-cáo-gom-nhóm-theo-chuyên-khoa--xuất-excel-25-điểm)
   - [Ý 1: Thiết kế giao diện báo cáo HTML trên WebBrowser](#ý-1-thiết-kế-giao-diện-báo-cáo-html-trên-webbrowser)
   - [Ý 2: Gom nhóm theo Chuyên khoa & Dòng Tổng cộng đơn giá](#ý-2-gom-nhóm-theo-chuyên-khoa--dòng-tổng-cộng-đơn-giá)
   - [Ý 3: Chức năng In ấn & Xuất file Excel (.xls)](#ý-3-chức-năng-in-ấn--xuất-file-excel-xls)
6. [Tổng hợp mã nguồn hoàn chỉnh (Full Source Code)](#6-tổng-hợp-mã-nguồn-hoàn-chỉnh-full-source-code)

---

## 1. PHÂN TÍCH ĐỀ BÀI & CẤU TRÚC ĐIỂM

| Thành phần | Điểm | Trọng tâm kiểm tra |
|---|---|---|
| **Form 1: `frmQuanLyBacSi`** | **5.0 đ** | Kỹ năng CRUD, JOIN bảng, tính toán cột động, format tiền tệ/ngày tháng, kiểm tra độ tuổi phức tạp tại ngày vào làm. |
| **Form 2: `frmThongKeBacSi`** | **2.5 đ** | Nạp ComboBox kèm dòng "Tất cả", lọc AND đa điều kiện bỏ qua giá trị rỗng/tất cả, tính Count/Sum/Max trên DataTable sau lọc. |
| **Báo cáo: `frmBaoCaoBacSi`** | **2.5 đ** | Gom nhóm (Group By) danh mục Chuyên khoa, hiển thị bảng HTML trực quan trên WebBrowser, hỗ trợ In ấn và Xuất file Excel. |

---

## 2. BƯỚC 0: SCRIPT CSDL & BẢNG DANH SÁCH CONTROL

### 2.1 Script SQL Server (Chạy trên SSMS)
```sql
CREATE DATABASE QLPhongKham;
GO
USE QLPhongKham;
GO

CREATE TABLE ChuyenKhoa (
    MaCK NVARCHAR(10) PRIMARY KEY,
    TenCK NVARCHAR(100) NOT NULL
);

CREATE TABLE BacSi (
    MaBS NVARCHAR(10) PRIMARY KEY,
    HoTen NVARCHAR(50) NOT NULL,
    GioiTinh BIT NOT NULL, -- 1: Nam, 0: Nữ
    NgaySinh DATE NOT NULL,
    MaCK NVARCHAR(10) FOREIGN KEY REFERENCES ChuyenKhoa(MaCK),
    DonGiaKham DECIMAL(18,0) NOT NULL,
    NgayVaoLam DATE NOT NULL
);

-- Dữ liệu mẫu ban đầu
INSERT INTO ChuyenKhoa VALUES 
(N'CK01', N'Nội tổng quát'), 
(N'CK02', N'Răng Hàm Mặt'), 
(N'CK03', N'Tai Mũi Họng'),
(N'CK04', N'Nhi khoa');

INSERT INTO BacSi VALUES
(N'BS001', N'Nguyễn Hoài Nam', 1, '1985-04-12', N'CK01', 150000, '2015-01-10'),
(N'BS002', N'Trần Thu Hà', 0, '1992-08-20', N'CK02', 200000, '2019-06-01'),
(N'BS003', N'Lê Quốc Anh', 1, '1988-11-05', N'CK03', 180000, '2016-09-15'),
(N'BS004', N'Phạm Mỹ Linh', 0, '1995-02-14', N'CK04', 250000, '2021-03-01'),
(N'BS005', N'Đỗ Minh Trí', 1, '1990-10-30', N'CK01', 160000, '2018-05-20');
```

### 2.2 Quy ước đặt tên Control chuẩn (WinForms)

#### Form 1 (`frmQuanLyBacSi`):
- `txtMaBS` (TextBox Mã bác sĩ)
- `txtHoTen` (TextBox Họ tên)
- `rdoNam` (RadioButton Nam, Checked = true), `rdoNu` (RadioButton Nữ)
- `dtpNgaySinh` (DateTimePicker Ngày sinh, Format = `Custom`, CustomFormat = `dd/MM/yyyy`)
- `cboChuyenKhoa` (ComboBox Chuyên khoa, DropDownStyle = `DropDownList`)
- `txtDonGiaKham` (TextBox Đơn giá khám)
- `dtpNgayVaoLam` (DateTimePicker Ngày vào làm, Format = `Custom`, CustomFormat = `dd/MM/yyyy`)
- `dgvDanhSach` (DataGridView danh sách bác sĩ)
- Các nút bấm có phím tắt:
  - `btnThem` (Text: `&Thêm` -> Alt + T)
  - `btnSua` (Text: `&Sửa` -> Alt + S)
  - `btnXoa` (Text: `&Xóa` -> Alt + X)
  - `btnLamMoi` (Text: `&Làm mới` -> Alt + L)
  - `btnThongKe` (Text: `Thống &kê` -> Alt + K)
  - `btnThoat` (Text: `T&hoát` -> Alt + H)

#### Form 2 (`frmThongKeBacSi`):
- `cboChuyenKhoa` (ComboBox Chuyên khoa)
- `cboGioiTinh` (ComboBox Giới tính: Tất cả, Nam, Nữ)
- `btnTimKiem` (Text: `&Tìm kiếm` -> Alt + T)
- `btnXuatBaoCao` (Text: `&Báo cáo` -> Alt + B)
- `btnDong` (Text: `Đó&ng` -> Alt + N)
- `dgvKetQua` (DataGridView kết quả tra cứu)
- Label thống kê: `lblSoLuong`, `lblTongDonGia`, `lblDonGiaCaoNhat`

---

## 3. FORM 1: QUẢN LÝ BÁC SĨ (frmQuanLyBacSi - 5.0 ĐIỂM)

### Ý 1: DataGridView danh sách & Cột tính toán Phụ cấp ngoài giờ

#### Yêu cầu:
1. Hiển thị các cột: Mã BS, Họ tên, Giới tính (`Nam`/`Nữ`), Ngày sinh (`dd/MM/yyyy`), Chuyên khoa (`JOIN ChuyenKhoa` lấy `TenCK`), Đơn giá khám (`N0`), Ngày vào làm (`dd/MM/yyyy`).
2. **Cột tính toán:** `Phụ cấp ngoài giờ = Đơn giá khám × 1.5` (hiển thị định dạng tiền `N0`).
3. Đặt chế độ bảng là chỉ đọc (`ReadOnly`), cho phép chọn toàn bộ dòng (`SelectionMode = FullRowSelect`).

#### Giải pháp kỹ thuật:
- **Xử lý SQL:** Tính trực tiếp trong câu `SELECT` bằng `CAST(ROUND(bs.DonGiaKham * 1.5, 0) AS DECIMAL(18,0)) AS PhuCapNgoaiGio`. Đổi cột `GioiTinh` sang text bằng `CASE bs.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END`.
- **Dùng thư viện:** `GridHelper.Bind` để cấu hình nhanh tên cột, độ rộng và định dạng hiển thị. `GridHelper.SetReadOnly` để bật chỉ đọc và chọn nguyên dòng.

#### Đoạn mã cài đặt:
```csharp
private void LoadData()
{
    string sql = @"SELECT bs.MaBS, bs.HoTen,
                          CASE bs.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END AS GioiTinhHienThi,
                          bs.NgaySinh, ck.TenCK, bs.DonGiaKham, bs.NgayVaoLam,
                          CAST(ROUND(bs.DonGiaKham * 1.5, 0) AS DECIMAL(18,0)) AS PhuCapNgoaiGio,
                          bs.GioiTinh, bs.MaCK
                   FROM BacSi bs
                   JOIN ChuyenKhoa ck ON bs.MaCK = ck.MaCK
                   ORDER BY bs.MaBS";

    DataTable dt = DbHelper.GetDataTable(sql);

    GridHelper.Bind(dgvDanhSach, dt,
        new GridHelper.GridColumn("MaBS", "Mã BS", 90),
        new GridHelper.GridColumn("HoTen", "Họ tên", 180),
        new GridHelper.GridColumn("GioiTinhHienThi", "Giới tính", 80),
        new GridHelper.GridColumn("NgaySinh", "Ngày sinh", 100, "dd/MM/yyyy"),
        new GridHelper.GridColumn("TenCK", "Chuyên khoa", 140),
        new GridHelper.GridColumn("DonGiaKham", "Đơn giá khám", 110, "N0"),
        new GridHelper.GridColumn("NgayVaoLam", "Ngày vào làm", 100, "dd/MM/yyyy"),
        new GridHelper.GridColumn("PhuCapNgoaiGio", "Phụ cấp ngoài giờ", 130, "N0")
    );

    GridHelper.SetReadOnly(dgvDanhSach);
}
```

---

### Ý 2: Nạp ComboBox Chuyên khoa trong Form_Load

#### Yêu cầu:
Nạp danh mục chuyên khoa vào `cboChuyenKhoa`, trong đó:
- `DisplayMember = "TenCK"` (Hiển thị tên tiếng Việt)
- `ValueMember = "MaCK"` (Lưu trữ mã ẩn phía sau)

#### Giải pháp kỹ thuật:
- Sử dụng `DbHelper.SetDefaultConnectionString` để thiết lập chuỗi kết nối SQL Server.
- Dùng `ComboBoxHelper.Bind(cboChuyenKhoa, dt, "MaCK", "TenCK")` trong sự kiện `Form_Load`.

#### Đoạn mã cài đặt:
```csharp
private void frmQuanLyBacSi_Load(object sender, EventArgs e)
{
    // Thiết lập kết nối SQL Server
    DbHelper.SetDefaultConnectionString("Server=.;Database=QLPhongKham;Trusted_Connection=True;");

    // Nạp ComboBox Chuyên khoa
    DataTable dtCK = DbHelper.GetDataTable("SELECT MaCK, TenCK FROM ChuyenKhoa ORDER BY TenCK");
    ComboBoxHelper.Bind(cboChuyenKhoa, dtCK, "MaCK", "TenCK");

    // Tải dữ liệu ban đầu lên DataGridView
    LoadData();
}
```

---

### Ý 3: Click dòng trên Grid nạp lên Control & Khóa ô Mã BS

#### Yêu cầu:
Khi người dùng click chọn 1 dòng trên DataGridView:
1. Nạp đầy đủ thông tin dòng đó lên các control nhập liệu (TextBox, ComboBox, DateTimePicker, RadioButton).
2. Khóa ô `txtMaBS` sang chế độ chỉ đọc (`ReadOnly = true`) để chống sửa nhầm khóa chính.

#### Giải pháp kỹ thuật:
- Bắt sự kiện `SelectionChanged` (hoặc `CellClick`) của `dgvDanhSach`.
- Dùng `GridHelper.GetSelectedRow(dgvDanhSach)` lấy `DataRowView`.
- Dùng `FormMapper.LoadRow` để ánh xạ an toàn (tự xử lý `DBNull.Value`).
- Cập nhật RadioButton Giới tính và khóa `txtMaBS.ReadOnly = true`.

#### Đoạn mã cài đặt:
```csharp
private void dgvDanhSach_SelectionChanged(object sender, EventArgs e)
{
    DataRowView row = GridHelper.GetSelectedRow(dgvDanhSach);
    if (row == null) return;

    FormMapper.LoadRow(row,
        FormMapper.Map(txtMaBS, "MaBS"),
        FormMapper.Map(txtHoTen, "HoTen"),
        FormMapper.Map(dtpNgaySinh, "NgaySinh"),
        FormMapper.Map(cboChuyenKhoa, "MaCK"),
        FormMapper.Map(txtDonGiaKham, "DonGiaKham"),
        FormMapper.Map(dtpNgayVaoLam, "NgayVaoLam")
    );

    // Gán giới tính lên RadioButton
    if (row["GioiTinh"] != DBNull.Value)
    {
        rdoNam.Checked = Convert.ToBoolean(row["GioiTinh"]);
        rdoNu.Checked = !rdoNam.Checked;
    }

    // Khóa ô mã khi đang xem dòng có sẵn
    txtMaBS.ReadOnly = true;
}
```

---

### Ý 4: Nút Thêm (&T) - Ràng buộc tuổi & Đơn giá & Trùng mã

#### Yêu cầu:
1. Gán phím tắt Alt + T (`Text = "&Thêm"`).
2. **Kiểm tra tính hợp lệ dữ liệu:**
   - Mã BS: không được để trống, định dạng `BS\d{3}` (ví dụ: `BS006`, `BS010`).
   - Họ tên: không được để trống.
   - Chuyên khoa: bắt buộc chọn.
   - Tuổi: Bác sĩ phải từ **24 tuổi trở lên** tính đến ngày vào làm.
   - Đơn giá khám: số nguyên từ **100.000 đ đến 2.000.000 đ**.
3. **Kiểm tra trùng mã:** Báo lỗi nếu `MaBS` đã tồn tại trong CSDL.
4. **Thêm dữ liệu:** Chèn vào bảng `BacSi`, thông báo thành công, nạp lại bảng và reset form.

#### Giải pháp kỹ thuật:
- Sử dụng `Validator.AgeRangeAtDate(dtpNgaySinh, "Tuổi bác sĩ", 24, 100, ...)` và truyền ngày tham chiếu `dtpNgayVaoLam.Value` vào `Validator.ValidateAll(referenceDate, rules)`.
- Dùng `CrudHelper.Exists("BacSi", "MaBS", ma)` để kiểm tra trùng khóa chính.
- Dùng `FormMapper.GetValues` lấy dữ liệu dạng Dictionary và gọi `CrudHelper.Insert`.

#### Đoạn mã cài đặt:
```csharp
private ValidationResult ValidateForm()
{
    return Validator.ValidateAll(
        dtpNgayVaoLam.Value, // Ngày mốc để tính tuổi chính xác
        Validator.Required(txtMaBS, "Mã bác sĩ"),
        Validator.Pattern(txtMaBS, "Mã bác sĩ", @"^BS\d{3}$", "Mã BS phải có dạng 'BS' kèm 3 chữ số (ví dụ: BS006)!"),
        Validator.Required(txtHoTen, "Họ tên"),
        Validator.AgeRangeAtDate(dtpNgaySinh, "Tuổi bác sĩ", 24, 100, "Bác sĩ phải từ 24 tuổi trở lên tính đến ngày vào làm!"),
        Validator.Selected(cboChuyenKhoa, "Chuyên khoa"),
        Validator.DecimalRange(txtDonGiaKham, "Đơn giá khám", 100000m, 2000000m, "Đơn giá khám phải từ 100.000 đ đến 2.000.000 đ!")
    );
}

private Dictionary<string, object> GetFormValues()
{
    var dict = FormMapper.GetValues(
        FormMapper.Map(txtMaBS, "MaBS"),
        FormMapper.Map(txtHoTen, "HoTen"),
        FormMapper.Map(dtpNgaySinh, "NgaySinh"),
        FormMapper.Map(cboChuyenKhoa, "MaCK"),
        FormMapper.Map(txtDonGiaKham, "DonGiaKham"),
        FormMapper.Map(dtpNgayVaoLam, "NgayVaoLam")
    );
    dict["GioiTinh"] = rdoNam.Checked ? 1 : 0;
    return dict;
}

private void btnThem_Click(object sender, EventArgs e)
{
    // 1. Kiểm tra tính hợp lệ dữ liệu
    var val = ValidateForm();
    if (!val.IsValid)
    {
        MessageHelper.Warning(val.ErrorMessage);
        FormHelper.FocusControl(val.ErrorControl);
        return;
    }

    // 2. Kiểm tra trùng khóa chính
    string ma = txtMaBS.Text.Trim();
    if (CrudHelper.Exists("BacSi", "MaBS", ma))
    {
        MessageHelper.DuplicateKey("Mã bác sĩ", ma);
        FormHelper.FocusControl(txtMaBS);
        return;
    }

    // 3. Thực hiện Insert
    var values = GetFormValues();
    if (CrudHelper.Insert("BacSi", values) > 0)
    {
        MessageHelper.Success("thêm mới");
        LoadData();
        btnLamMoi_Click(null, null);
    }
}
```

---

### Ý 5: Nút Sửa (&S) - Cập nhật dữ liệu an toàn

#### Yêu cầu:
1. Gán phím tắt Alt + S (`Text = "&Sửa"`).
2. Kiểm tra người dùng đã chọn dòng trên DataGridView chưa.
3. Kiểm tra tính hợp lệ dữ liệu (giống khi Thêm, **không** kiểm tra trùng mã vì mã là khóa chính được giữ nguyên).
4. Cập nhật dữ liệu vào SQL Server và nạp lại lưới.

#### Đoạn mã cài đặt:
```csharp
private void btnSua_Click(object sender, EventArgs e)
{
    // 1. Kiểm tra chọn dòng
    if (!GridHelper.HasSelection(dgvDanhSach))
    {
        MessageHelper.NoSelection("dòng cần sửa");
        return;
    }

    // 2. Kiểm tra tính hợp lệ
    var val = ValidateForm();
    if (!val.IsValid)
    {
        MessageHelper.Warning(val.ErrorMessage);
        FormHelper.FocusControl(val.ErrorControl);
        return;
    }

    // 3. Thực hiện Update
    string ma = txtMaBS.Text.Trim();
    var values = GetFormValues();

    if (CrudHelper.Update("BacSi", values, "MaBS", ma) > 0)
    {
        MessageHelper.Success("cập nhật");
        LoadData();
    }
}
```

---

### Ý 6: Nút Xóa (&X) - Hộp thoại xác nhận Yes/No chuẩn

#### Yêu cầu:
1. Gán phím tắt Alt + X (`Text = "&Xóa"`).
2. Yêu cầu chọn dòng trên Grid.
3. Hiển thị hộp thoại xác nhận dạng: `"Bạn có chắc chắn muốn xóa bác sĩ … không?"` với 2 nút Yes/No và biểu tượng Warning.
4. Nếu chọn Yes: Xóa bản ghi trong SQL Server, thông báo thành công và nạp lại bảng.

#### Đoạn mã cài đặt:
```csharp
private void btnXoa_Click(object sender, EventArgs e)
{
    if (!GridHelper.HasSelection(dgvDanhSach))
    {
        MessageHelper.NoSelection("dòng cần xóa");
        return;
    }

    string ma = txtMaBS.Text.Trim();
    string ten = txtHoTen.Text.Trim();

    // Hộp thoại xác nhận Yes/No chuẩn tiếng Việt
    if (MessageHelper.ConfirmDelete($"bác sĩ {ten} (Mã: {ma})") 
        && CrudHelper.Delete("BacSi", "MaBS", ma) > 0)
    {
        MessageHelper.Success("xóa");
        LoadData();
        btnLamMoi_Click(null, null);
    }
}
```

---

### Ý 7: Nút Làm mới (&L), Thống kê (&K), Thoát (&H)

#### Yêu cầu:
- **Làm mới (&L):** Xóa trắng các TextBox, gán ngày hiện tại, reset ComboBox, mở khóa `txtMaBS` (`ReadOnly = false`) và focus con trỏ vào `txtMaBS`.
- **Thống kê (&K):** Mở cửa sổ Form 2 (`frmThongKeBacSi`).
- **Thoát (&H):** Hộp thoại xác nhận Yes/No trước khi đóng Form.

#### Đoạn mã cài đặt:
```csharp
private void btnLamMoi_Click(object sender, EventArgs e)
{
    FormMapper.ClearControls(txtMaBS, txtHoTen, txtDonGiaKham);
    rdoNam.Checked = true;
    dtpNgaySinh.Value = DateTime.Today;
    dtpNgayVaoLam.Value = DateTime.Today;
    if (cboChuyenKhoa.Items.Count > 0) cboChuyenKhoa.SelectedIndex = 0;

    txtMaBS.ReadOnly = false;
    txtMaBS.Focus();
}

private void btnThongKe_Click(object sender, EventArgs e)
{
    new frmThongKeBacSi().ShowDialog();
}

private void btnThoat_Click(object sender, EventArgs e)
{
    if (MessageHelper.Confirm("Bạn có chắc chắn muốn thoát khỏi chương trình?", "Xác nhận thoát"))
    {
        this.Close();
    }
}
```

---

## 4. FORM 2: THỐNG KÊ & TRA CỨU BÁC SĨ (frmThongKeBacSi - 2.5 ĐIỂM)

### Ý 1: ComboBox có dòng "Tất cả" & ComboBox Giới tính

#### Yêu cầu:
- `cboChuyenKhoa`: Nạp danh sách chuyên khoa và tự chèn thêm mục `-- Tất cả --` ở vị trí đầu tiên.
- `cboGioiTinh`: Có 3 mục lựa chọn: `-- Tất cả --`, `Nam`, `Nữ`.

#### Giải pháp kỹ thuật:
- Dùng `ComboBoxHelper.BindWithAll(cboChuyenKhoa, dtCK, "MaCK", "TenCK", "-- Tất cả --")`.
- Nạp cứng 3 mục cho `cboGioiTinh` và đặt `SelectedIndex = 0`.

```csharp
private void frmThongKeBacSi_Load(object sender, EventArgs e)
{
    // Nạp ComboBox Chuyên khoa có dòng Tất cả
    DataTable dtCK = DbHelper.GetDataTable("SELECT MaCK, TenCK FROM ChuyenKhoa ORDER BY TenCK");
    ComboBoxHelper.BindWithAll(cboChuyenKhoa, dtCK, "MaCK", "TenCK", "-- Tất cả --");

    // Nạp ComboBox Giới tính
    cboGioiTinh.Items.Clear();
    cboGioiTinh.Items.AddRange(new object[] { "-- Tất cả --", "Nam", "Nữ" });
    cboGioiTinh.SelectedIndex = 0;

    // Hiển thị toàn bộ dữ liệu lần đầu
    TimKiem(isInitial: true);
}
```

---

### Ý 2: Nút Tìm kiếm (&T) - Lọc AND kết hợp

#### Yêu cầu:
1. Gán phím tắt Alt + T (`Text = "&Tìm kiếm"`).
2. Lọc kết hợp điều kiện: Chuyên khoa AND Giới tính. Nếu người dùng chọn "Tất cả" ở mục nào thì tự động bỏ qua điều kiện đó.
3. Nếu không tìm thấy kết quả nào thì hiển thị hộp thoại: `"Không tìm thấy dữ liệu phù hợp"`.

#### Giải pháp kỹ thuật:
- Sử dụng `FilterBuilder.New()`:
  - `SkipIfAllSelected(cboChuyenKhoa, "bs.MaCK")`: Tự động bỏ qua điều kiện nếu người dùng chọn dòng `-- Tất cả --`.
  - Giới tính: Nếu index = 1 thì thêm `filter.Equal("bs.GioiTinh", 1)`, index = 2 thì `filter.Equal("bs.GioiTinh", 0)`.
- Thực thi qua `DbHelper.GetDataTable(sql, parms)`.

---

### Ý 3: Cập nhật các chỉ số Thống kê sau lọc (Count, Sum, Max)

#### Yêu cầu:
Hiển thị thống kê trên tập dữ liệu kết quả sau khi tìm kiếm:
1. **Số lượng bác sĩ:** Tổng số dòng tìm thấy.
2. **Tổng đơn giá khám:** Tổng cộng cột `DonGiaKham`.
3. **Đơn giá khám cao nhất:** Giá trị Max của cột `DonGiaKham`.

#### Giải pháp kỹ thuật:
- Dùng các hàm thống kê tối ưu của `DataHelper`:
  - `DataHelper.Count(dt)`
  - `DataHelper.Sum(dt, "DonGiaKham")`
  - `DataHelper.Max<object>(dt, "DonGiaKham")`

#### Đoạn mã cài đặt chung cho Ý 2 & Ý 3:
```csharp
private DataTable _dtKetQua;

private void btnTimKiem_Click(object sender, EventArgs e) => TimKiem(isInitial: false);

private void TimKiem(bool isInitial)
{
    var filter = FilterBuilder.New()
        .SkipIfAllSelected(cboChuyenKhoa, "bs.MaCK");

    if (cboGioiTinh.SelectedIndex == 1) filter.Equal("bs.GioiTinh", 1);
    else if (cboGioiTinh.SelectedIndex == 2) filter.Equal("bs.GioiTinh", 0);

    var (where, parms) = filter.Build();
    string sql = @"SELECT bs.MaBS, bs.HoTen,
                          CASE bs.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END AS GioiTinhHienThi,
                          bs.NgaySinh, ck.TenCK, bs.DonGiaKham, bs.NgayVaoLam
                   FROM BacSi bs
                   JOIN ChuyenKhoa ck ON bs.MaCK = ck.MaCK"
                   + where + " ORDER BY ck.TenCK, bs.MaBS";

    _dtKetQua = DbHelper.GetDataTable(sql, parms);

    // Nạp kết quả lên DataGridView
    GridHelper.Bind(dgvKetQua, _dtKetQua,
        new GridHelper.GridColumn("MaBS", "Mã BS", 90),
        new GridHelper.GridColumn("HoTen", "Họ tên", 180),
        new GridHelper.GridColumn("GioiTinhHienThi", "Giới tính", 80),
        new GridHelper.GridColumn("NgaySinh", "Ngày sinh", 100, "dd/MM/yyyy"),
        new GridHelper.GridColumn("TenCK", "Chuyên khoa", 140),
        new GridHelper.GridColumn("DonGiaKham", "Đơn giá khám", 120, "N0"),
        new GridHelper.GridColumn("NgayVaoLam", "Ngày vào làm", 100, "dd/MM/yyyy")
    );
    GridHelper.SetReadOnly(dgvKetQua);

    // Cập nhật thống kê bằng DataHelper
    int count = DataHelper.Count(_dtKetQua);
    lblSoLuong.Text = $"Số lượng bác sĩ: {count}";
    lblTongDonGia.Text = $"Tổng đơn giá: {DataHelper.Sum(_dtKetQua, "DonGiaKham"):N0} đ";
    lblDonGiaCaoNhat.Text = $"Đơn giá cao nhất: {Convert.ToDecimal(DataHelper.Max<object>(_dtKetQua, "DonGiaKham") ?? 0):N0} đ";

    // Thông báo nếu không tìm thấy
    if (!isInitial && count == 0)
    {
        MessageHelper.Info("Không tìm thấy dữ liệu phù hợp", "Kết quả tìm kiếm");
    }
}
```

---

### Ý 4: Nút Đóng (&N)
```csharp
private void btnDong_Click(object sender, EventArgs e) => this.Close();
```

---

## 5. FORM 3: BÁO CÁO GOM NHÓM THEO CHUYÊN KHOA & XUẤT EXCEL (2.5 ĐIỂM)

### Ý 1: Thiết kế giao diện báo cáo HTML trên WebBrowser
* **Lý do dùng WebBrowser:** Báo cáo dùng control `WebBrowser` của WinForms là cách **nhanh nhất, đẹp nhất và không bị lỗi phiên bản ReportViewer/Crystal Reports** trong phòng thi.
* Báo cáo hỗ trợ xem trước (Preview), In ấn trực tiếp qua hộp thoại Windows và Xuất sang file Excel `.xls`.

### Ý 2: Gom nhóm theo Chuyên khoa & Dòng Tổng cộng đơn giá
* Lấy danh sách các chuyên khoa duy nhất từ `DataTable`:  
  `DataTable distinctCK = _dtKetQua.DefaultView.ToTable(true, "TenCK");`
* Duyệt qua từng chuyên khoa:
  - Lọc ra các bác sĩ thuộc chuyên khoa đó: `DataRow[] rows = _dtKetQua.Select($"TenCK = '{tenCK.Replace("'", "''")}'");`
  - Tạo dòng tiêu đề nhóm: `Chuyên khoa: ... (X bác sĩ)`
  - Liệt kê chi tiết từng bác sĩ.
* Ở cuối bảng: Hiển thị dòng **TỔNG CỘNG ĐƠN GIÁ TOÀN VIỆN**.

### Ý 3: Chức năng In ấn & Xuất file Excel (.xls)
- **In ấn:** Gọi lệnh `webViewer.ShowPrintDialog()`.
- **Xuất Excel:** Mở `SaveFileDialog` và lưu toàn bộ chuỗi HTML bằng `File.WriteAllText(sfd.FileName, html, Encoding.UTF8)`. Excel mở file này chuẩn 100% không vỡ định dạng.

#### Mã nguồn Form Báo Cáo `frmBaoCaoBacSi.cs`:
```csharp
using System;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ExamHelper.Utilities;

namespace ExamHelper.Templates
{
    public class frmBaoCaoBacSi : Form
    {
        private readonly DataTable _dataTable;
        private readonly string _dieuKienLoc;
        private WebBrowser webViewer;
        private Button btnInBaoCao, btnXuatExcel, btnDong;

        public frmBaoCaoBacSi(DataTable dt, string dieuKienLoc)
        {
            _dataTable = dt;
            _dieuKienLoc = dieuKienLoc;
            InitForm();
        }

        private void InitForm()
        {
            this.Text = "BÁO CÁO THỐNG KÊ DANH SÁCH BÁC SĨ THEO CHUYÊN KHOA";
            this.Size = new System.Drawing.Size(950, 720);
            this.StartPosition = FormStartPosition.CenterScreen;

            var panelTop = new Panel { Dock = DockStyle.Top, Height = 45 };
            btnInBaoCao = new Button { Text = "🖨️ In báo cáo (&P)", Left = 15, Top = 8, Width = 130, Height = 30 };
            btnInBaoCao.Click += (s, e) => webViewer.ShowPrintDialog();

            btnXuatExcel = new Button { Text = "📊 Xuất Excel (&E)", Left = 155, Top = 8, Width = 130, Height = 30 };
            btnXuatExcel.Click += (s, e) => XuatExcel();

            btnDong = new Button { Text = "Đóng (&N)", Left = 295, Top = 8, Width = 100, Height = 30 };
            btnDong.Click += (s, e) => this.Close();

            panelTop.Controls.AddRange(new Control[] { btnInBaoCao, btnXuatExcel, btnDong });

            webViewer = new WebBrowser { Dock = DockStyle.Fill };
            this.Controls.Add(webViewer);
            this.Controls.Add(panelTop);

            this.Load += (s, e) => webViewer.DocumentText = TaoHtmlBaoCao();
        }

        private void XuatExcel()
        {
            using (var sfd = new SaveFileDialog { Filter = "Excel Workbook (*.xls)|*.xls", FileName = "BaoCaoBacSi.xls" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, TaoHtmlBaoCao(), Encoding.UTF8);
                    MessageHelper.Success("xuất file Excel thành công");
                }
            }
        }

        private string TaoHtmlBaoCao()
        {
            var sb = new StringBuilder();
            sb.Append(@"<html><head><meta charset='utf-8'><style>
                body { font-family: 'Times New Roman', serif; margin: 30px; font-size: 14px; }
                table { width: 100%; border-collapse: collapse; margin-top: 15px; }
                th, td { border: 1px solid #333; padding: 6px 8px; }
                th { background-color: #dbe5f1; text-align: center; }
                .nhom { font-weight: bold; background-color: #f2f2f2; color: #1f497d; }
                .tong { font-weight: bold; background-color: #fde9d9; }
                .c { text-align: center; } .r { text-align: right; }
            </style></head><body>");

            // Phần đầu bảng
            sb.Append("<table><tr><td style='border:none;font-weight:bold;'>PHÒNG KHÁM ĐA KHOA QUỐC TẾ</td><td style='border:none;text-align:right;font-style:italic;'>Ngày lập: " + DateTime.Now.ToString("dd/MM/yyyy") + "</td></tr></table>");
            sb.Append("<h2 style='text-align:center;margin:15px 0 5px;color:#1f497d;'>BÁO CÁO DANH SÁCH BÁC SĨ</h2>");
            sb.Append($"<p style='text-align:center;font-style:italic;'>Điều kiện lọc: {_dieuKienLoc}</p>");

            // Tiêu đề các cột
            sb.Append("<table><tr><th>STT</th><th>Mã BS</th><th>Họ và tên</th><th>Giới tính</th><th>Ngày sinh</th><th>Ngày vào làm</th><th>Đơn giá khám</th></tr>");

            // Gom nhóm theo cột "TenCK"
            DataTable distinctNhom = _dataTable.DefaultView.ToTable(true, "TenCK");
            int stt = 1;
            decimal tongCongTatCa = 0;

            foreach (DataRow n in distinctNhom.Rows)
            {
                string tenCK = n["TenCK"].ToString();
                DataRow[] rows = _dataTable.Select($"TenCK = '{tenCK.Replace("'", "''")}'");
                decimal tongNhom = 0;

                // Dòng hiển thị tiêu đề nhóm
                sb.Append($"<tr><td colspan='7' class='nhom'>🏥 Chuyên khoa: {tenCK} ({rows.Length} bác sĩ)</td></tr>");

                foreach (DataRow r in rows)
                {
                    decimal donGia = Convert.ToDecimal(r["DonGiaKham"]);
                    tongNhom += donGia;
                    tongCongTatCa += donGia;

                    sb.Append($"<tr><td class='c'>{stt++}</td><td class='c'>{r["MaBS"]}</td><td>{r["HoTen"]}</td><td class='c'>{r["GioiTinhHienThi"]}</td><td class='c'>{Convert.ToDateTime(r["NgaySinh"]):dd/MM/yyyy}</td><td class='c'>{Convert.ToDateTime(r["NgayVaoLam"]):dd/MM/yyyy}</td><td class='r'>{donGia:N0} đ</td></tr>");
                }

                // Dòng tổng theo từng nhóm
                sb.Append($"<tr style='font-style:italic;background-color:#fafafa;'><td colspan='6' class='r'>Tổng đơn giá khoa {tenCK}:</td><td class='r' style='font-weight:bold;'>{tongNhom:N0} đ</td></tr>");
            }

            // Dòng Tổng cộng toàn viện
            sb.Append($"<tr class='tong'><td colspan='2' class='c'>TỔNG CỘNG</td><td colspan='4'></td><td class='r'>{tongCongTatCa:N0} đ</td></tr>");
            sb.Append("</table>");

            // Chữ ký
            sb.Append("<table style='margin-top:35px;'><tr><td style='border:none;'></td><td style='border:none;text-align:right;padding-right:50px;'><b>Người lập biểu</b><br><i>(Ký, ghi rõ họ tên)</i></td></tr></table>");
            sb.Append("</body></html>");

            return sb.ToString();
        }
    }
}
```

---

## 6. GỌI FORM BÁO CÁO TỪ FORM 2

### 🌟 Cách 1 (Khuyên dùng - Dùng Form Báo Cáo Dùng Chung trong thư viện):
Bạn **không cần tạo thêm Form mới** trong Visual Studio! Chỉ cần gọi `ReportHelper.ShowReport` từ thư viện `ExamHelper`:

```csharp
private void btnXuatBaoCao_Click(object sender, EventArgs e)
{
    if (_dtKetQua == null || _dtKetQua.Rows.Count == 0)
    {
        MessageHelper.Warning("Không có dữ liệu để xuất báo cáo!");
        return;
    }

    string dieuKien = $"Chuyên khoa: {cboChuyenKhoa.Text}; Giới tính: {cboGioiTinh.Text}";

    // Tự động mở Form báo cáo dùng chung (có sẵn In ấn và Xuất Excel):
    ReportHelper.ShowReport(
        dataTable: _dtKetQua,
        reportTitle: "BÁO CÁO DANH SÁCH BÁC SĨ THEO CHUYÊN KHOA",
        filterCondition: dieuKien,
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

    // Hoặc gọi siêu tốc tự động tất cả cột:
    // ReportHelper.ShowReport(_dtKetQua, "BÁO CÁO DANH SÁCH BÁC SĨ", "TenCK", "DonGiaKham", dieuKien);
}
```

### 📋 Cách 2: Tự tạo Form riêng `frmBaoCaoBacSi`
Nếu giám khảo phòng thi yêu cầu phải nhìn thấy Form `frmBaoCaoBacSi` trong cây Solution Explorer:
```csharp
private void btnXuatBaoCao_Click(object sender, EventArgs e)
{
    if (_dtKetQua == null || _dtKetQua.Rows.Count == 0)
    {
        MessageHelper.Warning("Không có dữ liệu để xuất báo cáo!");
        return;
    }

    string dieuKien = $"Chuyên khoa: {cboChuyenKhoa.Text}; Giới tính: {cboGioiTinh.Text}";
    new frmBaoCaoBacSi(_dtKetQua, dieuKien).ShowDialog();
}
```


---

## 🏆 KINH NGHIỆM ĂN TRỌN ĐIỂM TRONG PHÒNG THI
1. **Kiểm tra tuổi tại ngày vào làm:** Luôn truyền `dtpNgayVaoLam.Value` làm tham số đầu tiên trong `Validator.ValidateAll(dtpNgayVaoLam.Value, ...)`. Nếu chỉ so sánh năm trừ năm thông thường (`Year - Year`), bạn sẽ bị trừ 0.5 - 1.0 điểm do sai ngày/tháng sinh.
2. **Khóa mã khi sửa:** Ô mã chính luôn đặt `ReadOnly = true` khi chọn dòng, và chỉ mở `ReadOnly = false` khi bấm **Làm mới**. Khi sửa tuyệt đối **không** kiểm tra trùng mã.
3. **Phím tắt Alt:** Luôn đặt dấu `&` trước ký tự phím tắt trên Text của nút bấm (`&Thêm`, `&Sửa`, `&Xóa`, `&Làm mới`, `Thống &kê`, `T&hoát`).
