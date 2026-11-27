# 05. HỖ TRỢ LÀM NHANH BÀI THI THEO CẤU TRÚC ĐỀ

Tài liệu này gom thẳng **quy trình thực chiến** và **bản đồ ánh xạ đề bài → hàm thư viện** cho dạng đề WinForms 75–90 phút chuẩn (như [Đề mẫu 01](Samples/De01_QLNhanSu/) và các đề quản lý tương tự).

---

## 1. Nhìn nhanh cấu trúc đề thi chuẩn & phân bổ thời gian

| Phần của đề thi | Tỉ trọng thường gặp | Thư viện gánh giúp | Bạn tự làm gì |
|---|---:|---|---|
| **Phần I: CSDL & Kết nối** | Điều kiện bắt buộc | `DbHelper` tự đọc `App.config`, tự mở/đóng kết nối, hỗ trợ `SqlServer` / `Sqlite` / `MySql` / `PostgreSql` | Chạy file `.sql` đề cho và dán chuỗi kết nối vào `App.config` |
| **Phần II: Form 1 — Quản lý chính (CRUD + Validate)** | **~5.0 điểm** | `ComboBoxHelper.Bind`, `GridHelper.Bind`, `FormMapper.AutoLoad/AutoGetValues/AutoClear`, `Validator`, `CrudHelper`, `MessageHelper` | Kéo thả Form Designer, đặt tên control + `Tag`, gán phím tắt `&`, viết SQL `JOIN` + công thức tính cột |
| **Phần III: Form 2 — Tìm kiếm & Thống kê** | **~2.5 điểm** | `ComboBoxHelper.BindWithAll/BindItems`, `FilterBuilder`, `DbHelper.GetDataTable`, `DataHelper.Count/Sum/Average/Max/Min` | Dựng 2–3 ComboBox lọc + Grid + nhãn kết quả thống kê |
| **Phần IV: Báo cáo & Xuất Excel / PDF** | **~2.5 điểm** | `ReportHelper.ShowReport` (HTML + kéo thả + `.xlsx` OpenXML) hoặc `BindRdlc` / `ExportRdlc` / `BindCrystalReport` | Chọn `ShowReport` (nhanh) hoặc tự thiết kế file `.rdlc`/`.rpt` (nếu giám khảo chấm cứng công nghệ đó) |

> **Nếu đề có thêm Form quản lý bảng danh mục phụ** (chỉ xem/thêm/sửa/xóa/tìm 1 bảng đơn như `PhongBan`, `ChuyenKhoa`, `NhomHang` — không yêu cầu JOIN hay công thức tính riêng): gọi đúng **1 dòng** `FormGenericManager.Open("PhongBan");` thay vì vẽ cả một Form mới.

---

## 2. Quy trình 4 bước từ con số 0 đến chạy sạch đề thi

### Bước 1: Tạo DB + Kết nối 1 chỗ (2–3 phút)
1. Chạy script SQL của đề trên SQL Server (hoặc kiểm tra kết nối DB mà đề cấp).
2. Add Reference `ExamHelper.dll` (hoặc `ExamHelper.csproj`) vào Project WinForms.
3. Tạo `App.config` chứa chuỗi kết nối (nếu tạo project trên **.NET 8/10**, nhớ chỉnh `Copy to Output Directory = Copy if newer`):

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <connectionStrings>
    <add name="QLNhanSu"
         connectionString="Server=(localdb)\MSSQLLocalDB;Database=QLNhanSu;Trusted_Connection=True;"
         providerName="System.Data.SqlClient" />
  </connectionStrings>
</configuration>
```

*(Hoặc gán thẳng 1 dòng trong `Form_Load`: `DbHelper.ConnectionString = "...";`)*

---

### Bước 2: Kéo thả giao diện & Đặt tên theo quy tắc vàng của `FormMapper`
Để `FormMapper.AutoLoad` và `AutoGetValues` nhận diện tự động mà không cần map tay từng control:
- Đặt tên control = **tiền tố + tên cột DB**:
  - TextBox: `txtMaNV`, `txtHoTen`, `txtHeSoLuong`
  - DateTimePicker: `dtpNgaySinh`, `dtpNgayVaoLam` (nhớ bật `Format = Custom`, `CustomFormat = "dd/MM/yyyy"`)
  - ComboBox danh mục: `cboPhongBan` (khi `ComboBoxHelper.Bind` xong sẽ tự có `ValueMember = "MaPB"`)
  - Cụm `RadioButton` (ví dụ Giới tính `bit`): điền thuộc tính **`Tag`** của `rdoNam` là `GioiTinh:1`, của `rdoNu` là `GioiTinh:0`.
- Đặt chữ `&` vào thuộc tính `Text` của các nút để có phím tắt Alt (`&Thêm`, `&Sửa`, `&Xóa`, `&Làm mới`, `Thống &kê`, `T&hoát`, `&Tìm kiếm`, `&Xuất báo cáo`, `Đó&ng`).

---

### Bước 3: Gặp yêu cầu nào trên Form 1 → Làm đúng cách đó

| Yêu cầu trong đề | Đoạn code áp dụng ngay |
|---|---|
| **Nạp ComboBox nhập liệu khi mở Form** | `ComboBoxHelper.Bind(cboPhongBan, "SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB");` *(Gọi **trước** `LoadData()`!)* |
| **Hiển thị Grid có JOIN lấy tên + cột công thức (VD: Lương)** | Viết `SELECT nv.MaNV, nv.HoTen, ..., pb.TenPB, CAST(ROUND(nv.HeSoLuong * 2340000, 0) AS DECIMAL(18,0)) AS Luong, nv.GioiTinh, nv.MaPB FROM NhanVien nv JOIN PhongBan pb ON nv.MaPB = pb.MaPB`<br>Rồi gọi `GridHelper.Bind(dgvDanhSach, sql, new GridHelper.GridColumn(...), ...);`<br>*(Luôn giữ lại cột ẩn `nv.GioiTinh, nv.MaPB` ở cuối câu `SELECT` để click Grid tự nhảy đúng RadioButton & ComboBox.)* |
| **Chọn 1 dòng Grid → đổ lên ô nhập & khóa ô Mã** | `var row = GridHelper.GetSelectedRow(dgvDanhSach);`<br>`if (row == null) return;`<br>`FormMapper.AutoLoad(this, row);`<br>`txtMaNV.ReadOnly = true;` |
| **Kiểm tra dữ liệu trước khi Lưu + đưa con trỏ về ô sai** | Lấy schema bảng gốc ở `Form_Load`: `_schema = DbHelper.GetTableSchema("NhanVien");`<br>Khi Lưu gọi: `var val = Validator.FromSchema(this, _schema, Validator.Pattern(txtMaNV, "Mã NV", @"^NV\d{3}$"), Validator.DecimalRange(txtHeSoLuong, "Hệ số lương", 2.34m, 8.0m), Validator.AgeRangeAtDate(dtpNgaySinh, dtpNgayVaoLam, "Tuổi", 18, 120));`<br>`if (!val.IsValid) { MessageHelper.Warning(val.ErrorMessage); FormHelper.FocusControl(val.ErrorControl); return; }` |
| **Kiểm tra trùng mã ở nút Thêm** | `string ma = txtMaNV.Text.Trim();`<br>`if (CrudHelper.Exists("NhanVien", "MaNV", ma)) { MessageHelper.DuplicateKey("Mã NV", ma); FormHelper.FocusControl(txtMaNV); return; }` |
| **Thêm / Sửa xuống DB an toàn (`SqlParameter`)** | `var values = FormMapper.AutoGetValues(this, _schema);`<br>Thêm: `CrudHelper.Insert("NhanVien", values)`<br>Sửa: `CrudHelper.Update("NhanVien", values, "MaNV", ma)` *(Tự bỏ qua cột khóa không đổi)* |
| **Nút Xóa (kiểm tra chọn dòng + hỏi đúng câu đề)** | `if (!GridHelper.HasSelection(dgvDanhSach)) { MessageHelper.NoSelection("nhân viên"); return; }`<br>`string ma = txtMaNV.Text.Trim();`<br>`if (MessageHelper.ConfirmDelete($"nhân viên {ma}") && CrudHelper.Delete("NhanVien", "MaNV", ma) > 0) { MessageHelper.Success("xóa"); LoadData(); }` |
| **Nút Làm mới / Thống kê / Thoát** | Làm mới: `FormMapper.AutoClear(this); rdoNam.Checked = true; GridHelper.ClearSelection(dgvDanhSach); txtMaNV.ReadOnly = false; txtMaNV.Focus();`<br>Thống kê: `new frmThongKe().ShowDialog(this);`<br>Thoát: `if (MessageHelper.Confirm("Bạn có muốn thoát không?")) Application.Exit();` |

---

### Bước 4: Gặp yêu cầu Form 2 (Tìm kiếm, Thống kê) & Báo cáo

| Yêu cầu trong đề | Đoạn code áp dụng ngay |
|---|---|
| **ComboBox có mục `"Tất cả"` chọn sẵn ở Form 2** | `ComboBoxHelper.BindWithAll(cboPhongBan, "SELECT MaPB, TenPB FROM PhongBan ORDER BY TenPB", "Tất cả");`<br>`ComboBoxHelper.BindItems(cboGioiTinh, "Tất cả", "Nam", "Nữ");` |
| **Lọc AND kết hợp nhiều điều kiện, rỗng báo không tìm thấy** | `var f = FilterBuilder.ForConnection().SkipIfAllSelected(cboPhongBan, "nv.MaPB");`<br>`if (cboGioiTinh.SelectedIndex == 1) f.Equal("nv.GioiTinh", 1);`<br>`else if (cboGioiTinh.SelectedIndex == 2) f.Equal("nv.GioiTinh", 0);`<br>`_dtKetQua = DbHelper.GetDataTable(sqlBase, f, "pb.TenPB, nv.MaNV");`<br>`GridHelper.Bind(dgvKetQua, _dtKetQua, ...);`<br>`if (!isInitial && _dtKetQua.Rows.Count == 0) MessageHelper.Info("Không tìm thấy dữ liệu phù hợp");` |
| **Nhóm Thống kê: Đếm / Tổng / Trung bình / Max / Min** | `lblSoNV.Text = $"Số nhân viên: {DataHelper.Count(_dtKetQua)}";`<br>`lblTongLuong.Text = $"Tổng lương: {DataHelper.Sum(_dtKetQua, "Luong"):N0}";`<br>`lblHeSoMax.Text = $"Hệ số lương cao nhất: {DataHelper.Max<double>(_dtKetQua, "HeSoLuong"):0.##}";` |
| **Báo cáo (tổng quát, nhóm theo cột, xuất `.xlsx` / In)** | `if (_dtKetQua == null \|\| _dtKetQua.Rows.Count == 0) { MessageHelper.Warning("Không có dữ liệu để xuất báo cáo"); return; }`<br>`ReportHelper.ShowReport(_dtKetQua, "BẢNG LƯƠNG NHÂN VIÊN", $"Phòng ban: {cboPhongBan.Text}; Giới tính: {cboGioiTinh.Text}", "TenPB", "Phòng ban: ", cols, "CÔNG TY TNHH AN PHÁT", this);` |
| **Báo cáo khi đề chấm bắt buộc RDLC / Crystal** | Tự tạo `.rdlc` hoặc `.rpt`, rồi gọi:<br>`ReportHelper.BindRdlc(reportViewer1, "dsNhanVien", ReportHelper.PrepareReportTable(_dtKetQua), parms);`<br>`ReportHelper.ExportRdlc(reportViewer1, "PDF", "BangLuong");` |

---

## 3. Checklist kiểm tra 5 phút trước khi nộp bài

- [ ] Đã kiểm tra nút **Thêm**: nhập sai định dạng, trùng khóa, dưới tuổi tối thiểu, sai khoảng số thập phân (cả `,` và `.`) → báo đúng lỗi và trỏ chuột vào đúng ô.
- [ ] Đã kiểm tra nút **Làm mới**: mở khóa ô mã (`ReadOnly = false`), bỏ chọn dòng trên Grid để chọn lại dòng đầu tiên vẫn nạp bình thường.
- [ ] Đã kiểm tra nút **Sửa** và **Xóa** khi chưa chọn dòng → không bị văng lỗi `NullReferenceException`.
- [ ] Đã kiểm tra Form 2 ở trạng thái **0 kết quả lọc** → hiện đúng thông báo và các nhãn thống kê về `0`.
- [ ] Đã kiểm tra nút **Xuất báo cáo** cả khi Grid rỗng và khi có dữ liệu đã lọc.
- [ ] Các phím tắt `Alt + ký tự` của mọi nút bấm hoạt động đúng như đề mô tả.
