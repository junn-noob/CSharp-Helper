# Changelog

Tài liệu ghi nhận các thay đổi chính của package **ExamHelper** (`PackageId: ExamHelper`).

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
