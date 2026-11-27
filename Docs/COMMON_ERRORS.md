# CÁC LỖI THƯỜNG GẶP TRONG PHÒNG THI & CÁCH XỬ LÝ NHANH

Tổng hợp các lỗi thực tế dễ gặp khi làm bài thi C# WinForms và cách tránh khi dùng `ExamHelper Pro`.

---

### Lỗi 1: Kết nối `App.config` chạy được trên `.NET Framework` nhưng báo thiếu trên `.NET 8/10`
* **Nguyên nhân:** Project WinForms SDK mới (`.NET 8`/`10`) không tự copy file `App.config` ra thư mục `bin\Debug\net10.0-windows\AppName.dll.config`.
* **Cách khắc phục:** Mở Properties của `App.config` chọn `Copy to Output Directory = Copy if newer` (hoặc thêm `<None Update="App.config"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>` trong `.csproj`), hoặc gán thẳng `DbHelper.ConnectionString = "...";`.

---

### Lỗi 2: `FormMapper.AutoGetValues` gửi nhầm cột tính toán xuống lệnh `INSERT`/`UPDATE`
* **Nguyên nhân:** Lấy `dgvDanhSach.DataSource as DataTable` (chứa cột JOIN như `TenPB`, hoặc cột tính `Luong`) truyền vào `AutoGetValues` khi trên Form lại có ô hiển thị tên trùng cột ảo.
* **Cách khắc phục:** Lấy schema bảng gốc một lần trong `Form_Load`:
  ```csharp
  _schema = DbHelper.GetTableSchema("NhanVien");
  var values = FormMapper.AutoGetValues(this, _schema);
  ```

---

### Lỗi 3: `ComboBox` / `RadioButton` không tự nhảy khi bấm dòng trên `DataGridView`
* **Nguyên nhân:**
  1. Câu `SELECT` nạp Grid quên lấy kèm cột khóa ngoại/bit (`nv.MaPB`, `nv.GioiTinh`).
  2. Gọi `LoadData()` trước khi gọi `ComboBoxHelper.Bind(cboPhongBan, ...)`.
  3. Quên đặt `Tag = "GioiTinh:1"` / `"GioiTinh:0"` trên hai `RadioButton`.
* **Cách khắc phục:** Luôn nạp `ComboBox` trước `LoadData()`, `SELECT` đủ các cột khóa gốc ở cuối câu truy vấn và gắn đúng `Tag` cho `RadioButton`.

---

### Lỗi 4: Bấm `Làm mới` xong bấm lại dòng đầu tiên trên Grid không ăn
* **Nguyên nhân:** `DataGridView.ClearSelection()` đôi khi giữ nguyên `CurrentCell` nên click lại dòng 0 không phát sự kiện `SelectionChanged`.
* **Cách khắc phục:** Dùng `GridHelper.ClearSelection(dgvDanhSach);` (đã xử lý xóa cả `CurrentCell = null`).

---

### Lỗi 5: Bấm `Sửa` báo `"Mã đã tồn tại"` hoặc văng `NullReferenceException` khi chưa chọn dòng
* **Cách khắc phục:**
  - Đầu `btnSua_Click` / `btnXoa_Click` luôn đặt: `if (!GridHelper.HasSelection(dgv)) { MessageHelper.NoSelection(...); return; }`
  - Chỉ gọi `CrudHelper.Exists(...)` trong `btnThem_Click`, tuyệt đối không gọi trong `btnSua_Click` (hoặc dùng `ExistsExcept` nếu kiểm tra trùng cột unique phụ).

---

### Lỗi 6: Tính sai tuổi biên (chưa tới ngày sinh nhật) hoặc lỗi nhập dấu phẩy `,` ở số thập phân
* **Nguyên nhân:** Trừ năm thô (`2024 - 2000`) hoặc dùng `double.Parse` mặc định không nhận `"2,34"`.
* **Cách khắc phục:**
  - Dùng `Validator.AgeRangeAtDate(dtpNgaySinh, dtpNgayVaoLam, ...)` hoặc `Validator.CalculateAge(birth, refDate)`.
  - Dùng `Validator.DecimalRange(...)` và `FormMapper.AutoGetValues(this, _schema)` (hoặc `MapDouble`/`DataHelper.TryParseDecimal`) để hỗ trợ cả `"2,34"` và `"2.34"`.

---

### Lỗi 7: Lỗi SQL âm thầm trả bảng rỗng nhưng không biết vì sao
* **Nguyên nhân:** Mặc định `DbHelper.ThrowOnError = false` để app không bị sập giữa chừng; nếu tắt `ShowErrorDialog`, bảng rỗng do không có dòng rất dễ nhầm với lỗi sai SQL.
* **Cách khắc phục:** Kiểm tra `if (DbHelper.HasError) { MessageBox.Show(DbHelper.LastError.Message); }` hoặc gọi `DbHelper.TestConnection("default", showError: true, timeoutSeconds: 3)`.

---

### Lỗi 8: Đề bài yêu cầu RDLC / Crystal Report nhưng chỉ dùng `ShowReport`
* **Lưu ý chấm thi:** `ReportHelper.ShowReport(...)` dùng `FormReportViewer` (HTML trên `WebBrowser` + xuất OpenXML `.xlsx`), **không phải** file `.rdlc` hay `.rpt`. Nếu đề hoặc giám khảo bắt buộc kiểm tra file `.rdlc`/`.rpt`, bạn phải tự thiết kế file đó trong Visual Studio rồi nạp dữ liệu bằng `ReportHelper.BindRdlc`/`ExportRdlc` hoặc `BindCrystalReport`/`ExportCrystalReport`.
