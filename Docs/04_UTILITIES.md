# 04. MODULE UTILITIES, FORM TỔNG QUÁT & BÁO CÁO

> **Namespace:** `ExamHelper.Utilities`  
> **Thành phần:** `DataHelper`, `MessageHelper`, `FormHelper`, `FormGenericManager`, `ReportHelper`, `FormReportViewer`, `XlsxExporter` (internal), `ReportSummaryMode`, `ReportColumn`, `ReportDesignOptions`

---

## 1. `DataHelper` — Thống kê trên `DataTable` & Ép kiểu an toàn

### 1.1 Danh sách API chính
```csharp
// Thống kê trên DataTable đang hiển thị (không cần gửi lại truy vấn SQL)
int Count(DataTable table);
int Count(DataTable table, string filter);            // An toàn: filter sai cú pháp hoặc bảng rỗng trả 0
decimal Sum(DataTable table, string columnName, string filter = "");
decimal Average(DataTable table, string columnName, string filter = "");
T Max<T>(DataTable table, string columnName, string filter = "");
T Min<T>(DataTable table, string columnName, string filter = "");

// Phân tích số hỗ trợ cả dấu chấm & dấu phẩy thập phân VN/quốc tế ("2,34", "2.34", "1.500.000", "1,234.56")
bool TryParseDecimal(string value, out decimal result);

// Ép kiểu an toàn xử lý DBNull / null / Enum / Guid / Nullable<T>
T ChangeType<T>(object value, T defaultValue = default(T));
decimal ToDecimal(object value, decimal defaultValue = 0);
int ToInt(object value, int defaultValue = 0);
DateTime? ToDateTime(object value);
string ToString(object value, string defaultValue = "");
string EscapeFilter(string text); // Escape nháy đơn cho RowFilter/Compute/Select
```

---

## 2. `MessageHelper` — Chuẩn hóa hộp thoại tiếng Việt

```csharp
// Thuộc tính hỗ trợ chạy kiểm thử không bật MessageBox
bool SuppressDialogs { get; set; }
bool AutoConfirmResult { get; set; }
string LastMessage { get; }
string LastTitle { get; }

void Info(string message, string title = "Thông báo");
void Error(string message, string title = "Lỗi");
void Warning(string message, string title = "Cảnh báo");
bool Confirm(string message, string title = "Xác nhận");
bool ConfirmDelete(string itemName = "");           // "Bạn có chắc chắn muốn xóa {itemName} không?"
void Success(string action = "");                   // "Thao tác {action} thành công!"
void NoSelection(string itemName = "dòng");         // "Vui lòng chọn {itemName} cần thao tác"
void NoData();                                      // "Không tìm thấy dữ liệu"
void DuplicateKey(string fieldName, string value);  // "{fieldName} '{value}' đã tồn tại"
```

---

## 3. `FormHelper` — Điều khiển trạng thái Control & Định dạng nhanh

```csharp
void SetFormDefaults(Form form, string title = "");
void EnableControls(bool enabled, params Control[] controls);
void ToggleButtons(bool isEditing, Button btnAdd, Button btnEdit, Button btnDelete, Button btnSave, Button btnCancel);
void ResetForm(params Control[] controls);
void FocusControl(Control ctrl); // Tự Focus + SelectAll nếu là TextBox
void SetTabOrder(params Control[] controls);
void FormatCurrency(DataGridViewCellFormattingEventArgs e); // Định dạng "1.500.000 đ"
void SetTextBoxesReadOnly(bool readOnly, params TextBox[] textBoxes);
void EnableContainer(Control container, bool enabled);
void FormatDatePickers(params DateTimePicker[] pickers);
void FormatDatePickers(string customFormat, params DateTimePicker[] pickers);
```

---

## 4. `FormGenericManager` — Form CRUD tự sinh từ bất kỳ bảng CSDL nào

Dành cho các bảng danh mục phụ (ví dụ: Quản lý Phòng ban, Loại sản phẩm, Khoa...) hoặc khi cần mở nhanh giao diện xem/thêm/sửa/xóa/tìm kiếm/báo cáo cho **bất kỳ CSDL hay bảng nào** mà không viết code riêng từng màn hình.

```csharp
// Mở danh sách tất cả bảng trong CSDL và tự sinh Form cho bảng đầu tiên:
FormGenericManager.Open();

// Hoặc mở đích danh bảng bất kỳ trên kết nối mặc định / kết nối chỉ định:
FormGenericManager.Open("PhongBan");
FormGenericManager.Open("SanPham", dbName: "KhoHang", owner: this);
```

**Cơ chế tự động:**
- Gọi `DbHelper.GetTableNames` & `DbHelper.GetTableSchema` để đọc cột, khóa chính (`PrimaryKey`), `AutoIncrement`, `AllowDBNull`, `MaxLength`, `DataType`.
- Sinh `CheckBox` (`bool`), `DateTimePicker` (`DateTime`, bật `ShowCheckBox` nếu cho phép `NULL`), `TextBox` (giới hạn `MaxLength`, khóa `ReadOnly` nếu `AutoIncrement`).
- Hỗ trợ tìm kiếm đa cột qua `FilterBuilder.ForConnection`, kiểm tra qua `Validator.FromSchema`, thêm/sửa/xóa qua `CrudHelper` (kể cả khóa phức hợp), và mở `ReportHelper.ShowReport`.

> **Giới hạn thực tế của `FormGenericManager`:** Không tự JOIN bảng cha để biến cột khóa ngoại thành `ComboBox` chọn tên, không tự sinh cột tính toán công thức hay rule nghiệp vụ riêng (như kiểm tra tuổi theo ngày vào làm, Regex). Những form chính mang điểm cao vẫn nên dùng `FormMapper` + `Validator` + SQL `JOIN`.

---

## 5. `ReportHelper`, `FormReportViewer` & Xuất OpenXML `.xlsx`

### 5.1 Cấu hình cột và chế độ thống kê (`ReportColumn`, `ReportSummaryMode`)
```csharp
public enum ReportSummaryMode { None = 0, Sum = 1, Average = 2, Count = 3, Max = 4, Min = 5 }

// Khởi tạo cột tường minh
new ReportColumn("MaNV", "Mã NV", HorizontalAlignment.Center);
new ReportColumn("Luong", "Lương", "N0", HorizontalAlignment.Right, ReportSummaryMode.Sum);
new ReportColumn("Diem", "Điểm", "0.##", HorizontalAlignment.Right, ReportSummaryMode.Average);
```

### 5.2 Hiển thị Form báo cáo trực quan dùng chung (`FormReportViewer`)
`FormReportViewer` tự gom mọi trường dữ liệu vào 1 bảng, cho phép người dùng **kéo-thả ẩn/hiện/đổi thứ tự cột**, kéo cột vào vùng **gom nhóm**, đổi **Tiêu đề / Đơn vị / Điều kiện lọc / Người ký / Khổ giấy (`A4 Dọc` hoặc `A4 Ngang`) / Cỡ chữ**, đổi chế độ **Thống kê từng cột (`SUM`, `AVG`, `COUNT`, `MAX`, `MIN`)**, **In / Lưu PDF** qua `WebBrowser.ShowPrintDialog()`, và **Xuất Excel `.xlsx` chuẩn OpenXML** (tạo trực tiếp bằng `ZipArchive`, không cần cài Microsoft Excel).

```csharp
// 1. Truyền từ DataGridView hoặc câu SQL trực tiếp (tự nhận mọi cột):
ReportHelper.ShowReport(dgvKetQua, "BÁO CÁO THỐNG KÊ", owner: this);
ReportHelper.ShowReport("SELECT * FROM NhanVien", "DANH SÁCH NHÂN VIÊN", owner: this);

// 2. Truyền nhanh cột gom nhóm + cột tính tổng:
ReportHelper.ShowReport(dt, "BẢNG LƯƠNG", "TenPB", "Luong", "Phòng ban: Tất cả", this, "CÔNG TY ABC", "Phòng ban: ");

// 3. Khớp chính xác mẫu đề (tiêu đề, điều kiện, danh sách cột, khổ giấy, cỡ chữ):
ReportHelper.ShowReport(
    dataTable: _dtKetQua,
    reportTitle: "BẢNG LƯƠNG NHÂN VIÊN",
    filterCondition: $"Phòng ban: {cboPB.Text}; Giới tính: {cboGT.Text}",
    groupByColumn: "TenPB",
    groupPrefix: "Phòng ban: ",
    columns: new[] {
        new ReportColumn("MaNV", "Mã NV", HorizontalAlignment.Center),
        new ReportColumn("HoTen", "Họ tên"),
        new ReportColumn("NgaySinh", "Ngày sinh", "dd/MM/yyyy", HorizontalAlignment.Center),
        new ReportColumn("HeSoLuong", "Hệ số lương", "0.##", HorizontalAlignment.Right),
        new ReportColumn("Luong", "Lương", "N0", HorizontalAlignment.Right, ReportSummaryMode.Sum)
    },
    companyName: "CÔNG TY TNHH AN PHÁT",
    owner: this,
    signerTitle: "Người lập biểu",
    showGroupTotals: false,
    pageFooter: "Trang 1/1",
    openDesigner: true,
    landscape: false,
    fontSizePt: 13);
```

### 5.3 Sinh HTML / Xuất Excel `.xlsx` bằng code
```csharp
string html = ReportHelper.GenerateHtml(dt, "BÁO CÁO", columns: cols, landscape: true, fontSizePt: 14);
byte[] xlsxBytes = ReportHelper.BuildXlsxBytes(dt, "BÁO CÁO", columns: cols, landscape: true, fontSizePt: 14);
bool ok = ReportHelper.ExportXlsx(dt, "BÁO CÁO", columns: cols, landscape: true, fontSizePt: 14);
DataTable dtWithStt = ReportHelper.PrepareReportTable(dt); // Chèn cột STT (1..N) ở đầu DataTable
```

### 5.4 Hỗ trợ Microsoft RDLC (`ReportViewer`) & Crystal Reports (`CrystalReportViewer`)
Dùng Reflection nên không tạo phụ thuộc NuGet cứng vào `ExamHelper`. Khi đề thi bắt buộc nộp kèm file thiết kế `.rdlc` hoặc `.rpt`, bạn tự thiết kế file báo cáo trong Visual Studio rồi gọi:

```csharp
// RDLC ReportViewer:
DataTable dtReport = ReportHelper.PrepareReportTable(_dtKetQua);
var parameters = new Dictionary<string, string>
{
    { "pDieuKienLoc", dieuKien },
    { "pNgayLap", DateTime.Now.ToString("dd/MM/yyyy") }
};
ReportHelper.BindRdlc(reportViewer1, "dsNhanVien", dtReport, parameters, reportEmbeddedResource: "App.rptBangLuong.rdlc");
ReportHelper.ExportRdlc(reportViewer1, "PDF", "BangLuong");          // "PDF", "EXCELOPENXML", "EXCEL", "WORDOPENXML"

// Crystal Report:
ReportHelper.BindCrystalReport(crystalReportViewer1, rptDoc, dtReport, new Dictionary<string, object> { { "pDieuKienLoc", dieuKien } });
ReportHelper.ExportCrystalReport(rptDoc, "PDF", "BangLuong");       // "PDF", "EXCEL", "EXCELOPENXML"
```
