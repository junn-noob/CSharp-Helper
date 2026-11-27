# Changelog

Tài liệu ghi nhận các thay đổi chính của package **ExamHelper** (`PackageId: ExamHelper`).

## [1.2.0] - 2026-11-28
### Added
- `ReportTemplate` & `ReportTemplateColumn` (`.examrpt` XML): lưu toàn bộ bố cục báo cáo (tiêu đề, đơn vị, nhóm, thứ tự cột hiện/ẩn, căn lề, định dạng, `ReportSummaryMode`, khổ A4, cỡ chữ) ra file và nạp lại qua `ReportHelper.LoadTemplate` / `ShowReportFromTemplate`.
- `RdlcGenerator`, `ReportHelper.GenerateRdlcXml` và `ReportHelper.ExportRdlcFile`: xuất trực tiếp file thiết kế Microsoft Report (`.rdlc` RDL 2008) từ chính bố cục kéo-thả, mở được trong Visual Studio Report Designer và chạy qua `ReportHelper.BindRdlc`.
- `ReportPreviewBridge` (`WebBrowser.ObjectForScripting`) cho phép bấm trực tiếp vào Đơn vị, Tiêu đề, Điều kiện lọc, Người ký hoặc tiêu đề từng cột ngay trên mặt giấy xem trước A4 để đổi tên, căn lề, định dạng, chọn thống kê, gom nhóm hoặc ẩn cột tại chỗ.

### Changed
- Tinh gọn bảng điều khiển trái của `FormReportViewer` thành 2 tab rõ ràng (`Cột & Nhóm`, `Tiêu đề & Trang`), đồng bộ menu ngữ cảnh trên từng chip cột và thanh công cụ `FlowLayoutPanel` gồm thêm `Xuất .rdlc`, `Lưu mẫu`, `Mở mẫu`.

## [1.1.0] - 2026-11-28
### Added
- Hỗ trợ đa hệ CSDL qua `DbDialect` (`SqlServer`, `Sqlite`, `MySql`, `PostgreSql`, `OleDb`, `Odbc`), tự động chuyển `@param` sang `?` cho `OleDb`/`Odbc` và fallback đọc `DbDataReader` khi `CreateDataAdapter() == null`.
- `DbHelper.GetTableNames`, `DbHelper.GetTableSchema` và `DbHelper.TestConnection` có timeout thực.
- `FormMapper.AutoLoad`, `FormMapper.AutoLoadSql`, `FormMapper.AutoGetValues(container, schemaTable)` và `FormMapper.AutoClear` nhận diện theo `Tag`, `ComboBox.ValueMember` và tiền tố control.
- `Validator.FromSchema` kiểm tra `Required`/`Selected`, `MaxLength`, kiểu số nguyên/thực tự động từ schema bảng, cùng `AgeRangeAtDate` và `DateBeforeOrEqual`.
- `FilterBuilder.ForConnection` tự bọc định danh theo dialect kết nối.
- `FormGenericManager` tự sinh Form CRUD + tìm kiếm + báo cáo cho bảng đơn bất kỳ.
- `FormReportViewer` kéo-thả ẩn/hiện/đổi thứ tự/gom nhóm cột, đổi tiêu đề, khổ giấy (`A4 Dọc`/`Ngang`), cỡ chữ, chế độ thống kê (`ReportSummaryMode`) và `XlsxExporter` xuất file Excel chuẩn OpenXML `.xlsx`.
- Bài mẫu đầy đủ `Samples/De01_QLNhanSu`, tài liệu module chi tiết `Docs/01`–`05` và workflow đóng gói `.github/workflows/package.yml`.

### Changed
- Làm sạch mã nguồn nội bộ (`ExecuteWithCommand<T>`, `RunTransaction<TConn, TTx>`, tách helper SQL cho `CrudHelper` và `ResolveDefaultColumns` cho `ComboBoxHelper`), bỏ `using` dư thừa và dọn cảnh báo biên dịch trên cả `net472`, `net8.0-windows`, `net10.0-windows`.

## [1.0.0] - 2026-10-27
### Added
- Phát hành bộ thư viện ban đầu (`DbHelper`, `CrudHelper`, `FilterBuilder`, `ComboBoxHelper`, `GridHelper`, `FormMapper`, `Validator`, `DataHelper`, `MessageHelper`, `FormHelper`, `ReportHelper`).
