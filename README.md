# ExamHelper Pro — Bộ Thư Viện C# WinForms Hỗ Trợ Thực Hành & Làm Nhanh Đề Thi

**ExamHelper Pro** là thư viện dùng chung cho WinForms (`net472`, `net8.0-windows`, `net10.0-windows`) giúp rút ngắn thời gian viết code lặp lại ở các bài toán **Kết nối CSDL → CRUD → Ràng buộc dữ liệu → Lọc tìm kiếm → Thống kê → Báo cáo & Xuất Excel**.

Thư viện hỗ trợ SQL Server mặc định (`System.Data.SqlClient`) và mở rộng đa hệ CSDL (`SqlServer`, `Sqlite`, `MySql`, `PostgreSql`, `OleDb`, `Odbc`) thông qua `DbDialect` và `DbProviderFactory`, không gắn cứng tên bảng hay cấu trúc đề cố định.

---

## 🧭 Tài liệu chi tiết từng phần & Hỗ trợ làm bài nhanh

| Tài liệu | Nội dung chính |
|---|---|
| **[🚀 05. Hỗ trợ làm nhanh bài thi theo cấu trúc đề](Docs/05_EXAM_QUICKSTART.md)** | **Đi thẳng vào cấu trúc đề 4 phần (CSDL → Form CRUD → Form Lọc/Thống kê → Báo cáo) kèm bảng "Gặp yêu cầu X → Gọi hàm Y"** |
| [📖 01. Database, Đa CSDL, CRUD & FilterBuilder](Docs/01_DATABASE_CRUD.md) | `DbHelper`, `DbDialect`, đọc Schema (`GetTableNames`/`GetTableSchema`), `CrudHelper`, `FilterBuilder` |
| [📖 02. Binding & Form Mapping 2 chiều](Docs/02_BINDING_MAPPING.md) | `ComboBoxHelper`, `GridHelper`, `FormMapper` (Mapping tường minh & `AutoLoad`/`AutoGetValues`/`AutoClear`) |
| [📖 03. Kiểm tra dữ liệu (Validation)](Docs/03_VALIDATION.md) | `Validator`, `ValidationRule`, `ValidationResult`, kiểm tra tự động `Validator.FromSchema`, tính tuổi tại ngày mốc |
| [📖 04. Utilities, Generic CRUD & Báo cáo](Docs/04_UTILITIES.md) | `DataHelper`, `MessageHelper`, `FormHelper`, `FormGenericManager`, `ReportHelper`, `FormReportViewer`, `XlsxExporter`, RDLC/Crystal |
| [⚡ Cheat Sheet phòng thi](Docs/EXAM_CHEATSHEET.md) | Bộ snippet ngắn gọn copy-paste theo từng thao tác nút bấm |
| [📘 Cẩm nang tra cứu API (Newbie)](Docs/API_GUIDE.md) | Bảng tra nhanh "Tôi muốn làm gì?" kèm ví dụ ngắn |
| [⚠️ Các lỗi phòng thi thường gặp](Docs/COMMON_ERRORS.md) | Xử lý bẫy mất điểm: `App.config` trên .NET 8/10, trùng mã khi sửa, sai tuổi, cột JOIN khi lưu, RDLC vs HTML |
| [🎯 Bộ đề luyện tập](Docs/PRACTICE_EXAMS.md) | Đề luyện tập đa nghiệp vụ kèm script SQL Server |
| [📋 Hướng dẫn chi tiết Bài 1 (Phòng khám)](Docs/HUONG_DAN_CHI_TIET_BAI_1.md) | Giải từng ý tham khảo theo đề Quản lý phòng khám |

---

## 🏗️ Cấu trúc gói thư viện

```text
C# Helper/
├── .github/workflows/package.yml        # CI build, test (74 tests), pack & publish GitHub Packages
├── .gitignore
├── CHANGELOG.md
├── LICENSE
├── README.md
├── ExamHelper.sln
│
├── ExamHelper/                          # Thư viện lõi (net472; net8.0-windows; net10.0-windows)
│   ├── ExamHelper.csproj
│   ├── Database/
│   │   ├── DbHelper.cs                  # Đa CSDL (DbDialect), auto App.config, TestConnection timeout, GetTableNames/Schema
│   │   ├── CrudHelper.cs                # Insert, InsertAndGetId, Update, Delete, Exists, GetAll, GetById
│   │   └── FilterBuilder.cs             # Tạo WHERE + tham số an toàn (SqlServer/MySQL/SQLite/PostgreSQL...)
│   ├── Binding/
│   │   ├── ComboBoxHelper.cs            # Bind DataTable/SQL, BindWithAll, BindItems, SelectByValue
│   │   ├── GridHelper.cs                # Bind DataTable/SQL, GridColumn, lấy giá trị cả cột ẩn
│   │   └── FormMapper.cs                # LoadRow/GetValues + AutoLoad/AutoGetValues/AutoClear theo tên/Tag/ValueMember
│   ├── Validation/
│   │   ├── ValidationRule.cs            # ValidationType, ValidationRule, ValidationResult
│   │   └── Validator.cs                 # Fluent rules, AgeRangeAtDate, DateBeforeOrEqual, FromSchema
│   └── Utilities/
│       ├── DataHelper.cs                # Count, Sum, Average, Max, Min, TryParseDecimal (hỗ trợ cả dấu phẩy/chấm), ChangeType
│       ├── MessageHelper.cs             # Hộp thoại chuẩn tiếng Việt + SuppressDialogs phục vụ test
│       ├── FormHelper.cs                # ToggleButtons, FocusControl, FormatCurrency, FormatDatePickers
│       ├── CrudBinder.cs                # Điều phối trọn bộ Form 1 CRUD + Validation + Lookup chỉ trong 1 khối cấu hình
│       ├── FormGenericManager.cs        # Form CRUD + tìm kiếm + lookupQueries + báo cáo tự sinh từ schema bảng bất kỳ
│       ├── ReportHelper.cs              # ReportColumn, ReportSummaryMode, GenerateHtml, ShowReport/FromTemplate, RDLC/Crystal
│       ├── FormReportViewer.cs          # Thiết kế báo cáo 2 tab, kéo-thả cột, bấm sửa trên preview, lưu .examrpt & xuất .rdlc
│       ├── ReportTemplate.cs            # Lưu/nạp file mẫu báo cáo (.examrpt XML) dùng chung nhiều form/bài thi
│       ├── RdlcGenerator.cs             # Sinh file thiết kế Microsoft Report (.rdlc RDL 2008) từ bố cục kéo-thả
│       ├── SafeSplitContainer.cs        # SplitContainer xử lý an toàn lỗi GDI+ trên môi trường non-interactive
│       └── XlsxExporter.cs              # Xuất OpenXML .xlsx trực tiếp bằng ZipArchive (không cần cài Excel)
│
├── ExamHelper.Tests/                    # Kiểm thử tự động xUnit (74 unit tests)
│   └── ExamHelperTests.cs
│
├── Samples/
│   └── De01_QLNhanSu/                   # Bài giải mẫu hoàn chỉnh Đề 01 trên (localdb)\MSSQLLocalDB
│
├── Templates/                           # Khung code tham khảo cho phòng thi
│   ├── FormCrudTemplate.cs
│   └── FormExamSnippets.cs
│
└── Docs/                                # Tài liệu chi tiết từng phần & hướng dẫn làm bài nhanh
```

---

## 📦 Cài đặt & Sử dụng chung như một Package

Dự án được đóng gói chuẩn NuGet SDK (`PackageId: ExamHelper`, hỗ trợ `net472`, `net8.0-windows`, `net10.0-windows`) để nhiều người hoặc nhiều project dùng chung qua một trong 3 cách:

### Cách 1: Cài qua GitHub Packages (khuyên dùng cho nhóm / nhiều máy)
Khi đẩy tag phiên bản (ví dụ `v1.3.0`), GitHub Actions tự động build, chạy test và xuất bản gói lên GitHub Packages + GitHub Releases.

```powershell
# Thêm nguồn NuGet GitHub Packages (cần GitHub PAT có quyền read:packages)
dotnet nuget add source --username <GITHUB_USERNAME> --password <GITHUB_TOKEN> --store-password-in-clear-text --name github-examhelper "https://nuget.pkg.github.com/junn-noob/index.json"

# Cài vào project WinForms của bạn
dotnet add package ExamHelper --version 1.3.0
```

### Cách 2: Tự đóng gói `.nupkg` & chia sẻ qua thư mục NuGet nội bộ / USB / Releases
```powershell
# Tạo file ExamHelper.1.3.0.nupkg
dotnet pack ExamHelper/ExamHelper.csproj -c Release -o ./nupkgs

# Trỏ project dùng file .nupkg từ thư mục cục bộ hoặc thư mục dùng chung của nhóm
dotnet nuget add source "D:\SharedPackages" --name local-examhelper
dotnet add package ExamHelper --source local-examhelper
```
*(Hoặc trong Visual Studio: **Tools → NuGet Package Manager → Package Manager Settings → Package Sources** → thêm đường dẫn thư mục chứa `.nupkg`.)*

### Cách 3: Dùng trực tiếp từ Git (`git submodule` + `ProjectReference`)
Phù hợp khi cả nhóm vừa dùng thư viện vừa cần theo dõi hoặc sửa code chung:

```powershell
git submodule add https://github.com/junn-noob/CSharp-Helper.git libs/CSharp-Helper
dotnet add <YourApp.csproj> reference libs/CSharp-Helper/ExamHelper/ExamHelper.csproj
```

---

## 🚀 Hướng dẫn build & kiểm thử

```powershell
# Build toàn bộ thư viện (net472, net8.0-windows, net10.0-windows) và project test
dotnet build ExamHelper.sln

# Chạy toàn bộ bộ kiểm thử tự động (74 tests)
dotnet test ExamHelper.sln --no-restore

# Build bài mẫu Đề 01
dotnet build Samples/De01_QLNhanSu/De01_QLNhanSu.csproj

# Đóng gói NuGet (.nupkg + .snupkg)
dotnet pack ExamHelper/ExamHelper.csproj -c Release -o ./nupkgs
```

> **Lưu ý thực tế khi đổi hệ CSDL ngoài SQL Server:** Thư viện chỉ tham chiếu sẵn `System.Data.SqlClient` để giữ nhẹ. Nếu bài làm dùng SQLite, MySQL, PostgreSQL hoặc Access/ODBC trên .NET 8/10, project của bạn cần cài thêm package driver tương ứng (hoặc truyền `DbProviderFactory` vào `DbHelper.SetDefaultConnectionString`).
