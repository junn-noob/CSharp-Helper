# ExamHelper Pro - Bộ Thư Viện WinForms Tối Ưu Cho Kỳ Thi Thực Hành

ExamHelper Pro là thư viện C# WinForms mã nguồn mở được thiết kế đặc thù để hoàn thành các bài thi thực hành WinForms và SQL Server trong thời gian ngắn nhất (tối ưu cho đề 75 - 90 phút). Thư viện được đóng gói thành một lớp dùng chung độc lập, **không phụ thuộc tên bảng hay nghiệp vụ cụ thể**, dễ đọc, dễ sửa và tái sử dụng cho mọi dạng đề thi.

---

## 🎯 9 Mục tiêu thiết kế (100% Hoàn thành)

- [x] **Kết nối nhiều database**: Quản lý nhiều connection string riêng bằng tên (`DbHelper.SetConnectionString`).
- [x] **CRUD tham số hóa SQL**: Tham số hóa 100% qua `SqlParameter`, hỗ trợ cả SQL tự do và CRUD tổng quát (`CrudHelper`).
- [x] **Nạp ComboBox linh hoạt**: Hỗ trợ cột khóa/hiển thị tùy chọn, tự động chèn dòng `-- Tất cả --` cho tìm kiếm (`ComboBoxHelper`).
- [x] **Nạp DataGridView thông minh**: Tự đặt tiêu đề, độ rộng, định dạng tiền tệ `N0`, ngày tháng `dd/MM/yyyy` (`GridHelper`).
- [x] **Ánh xạ Form hai chiều**: Nạp dòng được chọn lên Form và lấy ngược giá trị từ Form ra Dictionary trong 1 dòng (`FormMapper`).
- [x] **Khai báo bộ quy tắc kiểm tra (Validator)**: Kiểm tra Required, Regex, Khoảng số, Tuổi tính tại ngày vào làm (`Validator`).
- [x] **Tìm kiếm đa điều kiện an toàn**: Tự động bỏ qua các trường người dùng để trống hoặc chọn "Tất cả" (`FilterBuilder`).
- [x] **Thống kê và tiện ích giao diện**: Count, Sum, Average, Max, Min trên tập dữ liệu sau lọc; hộp thoại chuẩn tiếng Việt (`DataHelper`, `MessageHelper`, `FormHelper`).
- [x] **Tài liệu & Đề thi mẫu luyện tập**: Đầy đủ tài liệu tra cứu API, Cheat sheet, mẫu form CRUD 5 bước và 3 đề thi mẫu (Phòng khám, Điện máy, Khách sạn); Unit test suite đạt 45/45 test pass.

---

## 📚 Bộ tài liệu hướng dẫn chuyên sâu theo từng phần

Mỗi module đều có file hướng dẫn riêng biệt kèm giải thích chi tiết và các ví dụ thực tế:

| Tài liệu | Nội dung chi tiết |
|---|---|
| [📖 01. Database & CRUD](Docs/01_DATABASE_CRUD.md) | Quản lý kết nối nhiều CSDL, truy vấn tham số, thêm/sửa/xóa tổng quát, bộ lọc tìm kiếm `FilterBuilder` |
| [📖 02. Binding & Form Mapping](Docs/02_BINDING_MAPPING.md) | Nạp ComboBox kèm dòng "Tất cả", cấu hình cột DataGridView, ánh xạ 2 chiều Form ⇄ DataTable |
| [📖 03. Validation (Kiểm tra dữ liệu)](Docs/03_VALIDATION.md) | Kiểm tra bắt buộc, định dạng Regex, khoảng số/ngày, tính tuổi chính xác tại ngày vào làm |
| [📖 04. Utilities & UI Helpers](Docs/04_UTILITIES.md) | Thống kê DataTable sau lọc (Count, Sum, Avg, Max), chuẩn hóa hộp thoại, bật/tắt nút giao diện |
| [🚀 05. Hướng dẫn nhanh thi 75 phút](Docs/05_EXAM_QUICKSTART.md) | 5 bước hoàn thành bài thi thực hành WinForms & SQL Server trong thời gian kỷ lục |
| [⚡ Cheat Sheet tra cứu nhanh](Docs/EXAM_CHEATSHEET.md) | Tổng hợp các đoạn code ngắn dùng ngay trong phòng thi |
| [⚠️ 7 Lỗi thường gặp & Cách khắc phục](Docs/COMMON_ERRORS.md) | Xử lý lỗi kết nối, trùng mã khi sửa, tính sai tuổi, nạp chồng cột... |
| [🎯 3 Đề thi mẫu luyện tập](Docs/PRACTICE_EXAMS.md) | 3 bộ đề luyện tập (Phòng khám, Điện máy, Khách sạn) kèm full script SQL Server |
| [📋 Hướng dẫn chi tiết Bài 1 (Phòng khám)](Docs/HUONG_DAN_CHI_TIET_BAI_1.md) | Giải chi tiết từng ý từ Form 1 (CRUD), Form 2 (Thống kê) đến Form Báo cáo xuất Excel |

---

## 🏗️ Cấu trúc thư mục

```text
C# Helper/
├── .gitignore                       # File lọc bỏ rác build chuẩn của Visual Studio & .NET
├── LICENSE                          # Giấy phép MIT mã nguồn mở cho cộng đồng
├── README.md                        # Hướng dẫn tổng quan, bảng tra cứu và hướng dẫn build
├── ExamHelper.sln                   # Visual Studio Solution chuẩn (ExamHelper + ExamHelper.Tests)
│
├── ExamHelper/                      # 📦 THƯ VIỆN LÕI DÙNG CHUNG (Class Library)
│   ├── Database/
│   │   ├── DbHelper.cs              # Quản lý kết nối, GetDataTable, ExecuteNonQuery, Param
│   │   ├── CrudHelper.cs            # Insert, Update, Delete, Exists tổng quát
│   │   └── FilterBuilder.cs         # Tạo mệnh đề WHERE động an toàn
│   ├── Binding/
│   │   ├── ComboBoxHelper.cs        # Bind, BindWithAll, GetSelectedValue
│   │   ├── GridHelper.cs            # Bind kèm GridColumn, SetReadOnly
│   │   └── FormMapper.cs            # Ánh xạ Control ⇄ DataTable/DataRow
│   ├── Validation/
│   │   ├── Validator.cs             # Fluent validation engine, CalculateAge
│   │   └── ValidationRule.cs        # Cấu trúc quy tắc và kết quả kiểm tra
│   └── Utilities/
│       ├── DataHelper.cs            # Thống kê DataTable: Count, Sum, Avg, Max, Min, TryParseDecimal
│       ├── MessageHelper.cs         # Hộp thoại chuẩn: ConfirmDelete, Success, Warning
│       └── FormHelper.cs            # Bật/tắt nút (ToggleButtons), reset form, focus, SetTextBoxesReadOnly
│
├── ExamHelper.Tests/                # 🧪 BỘ KIỂM THỬ TỰ ĐỘNG (45 tests xUnit)
│   └── ExamHelperTests.cs           # Kiểm chứng toàn diện: Filter, Age, Validation, Whitelist, Nullable, Transactions
│
├── Templates/                       # ⚡ MẪU FORM CRUD CHUẨN 5 BƯỚC CHO PHÒNG THI
│   └── FormCrudTemplate.cs          # Mẫu Form CRUD để chép và hoàn thiện bài thi trong 5 phút
│
└── Docs/                            # 📚 THƯ MỤC TÀI LIỆU HƯỚNG DẪN CHI TIẾT
    ├── 01_DATABASE_CRUD.md          # Chi tiết kết nối và CRUD
    ├── 02_BINDING_MAPPING.md        # Binding ComboBox, Grid, FormMapper
    ├── 03_VALIDATION.md             # Bộ kiểm tra tính hợp lệ dữ liệu
    ├── 04_UTILITIES.md              # Thống kê và tiện ích WinForms
    ├── 05_EXAM_QUICKSTART.md        # Hướng dẫn 5 bước thi 75 phút
    ├── API_GUIDE.md                 # Tra cứu API toàn diện
    ├── COMMON_ERRORS.md             # 7 Lỗi thường gặp và cách khắc phục
    ├── EXAM_CHEATSHEET.md           # Cheat sheet ngắn gọn dùng trong phòng thi
    └── PRACTICE_EXAMS.md            # 3 đề thi mẫu (Phòng khám, Điện máy, Khách sạn)
```

---

## 🚀 Hướng dẫn build & kiểm thử

Mở terminal trực tiếp tại thư mục dự án và chạy:

```powershell
# 1. Biên dịch toàn bộ thư viện (0 lỗi, 0 cảnh báo)
dotnet build

# 2. Chạy bộ kiểm thử tự động (45/45 tests passed)
dotnet test
```

---

## 📄 Bản quyền
Mã nguồn phát hành theo giấy phép **MIT License** - Hoàn toàn miễn phí sử dụng cho học tập, giảng dạy và phát triển dự án thực tế.
