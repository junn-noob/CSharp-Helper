# 03. MODULE VALIDATION (KIỂM TRA DỮ LIỆU ĐẦU VÀO)

> **Namespace:** `ExamHelper.Validation`  
> **Thành phần:** `ValidationType` (enum), `ValidationRule`, `ValidationResult`, `Validator`

---

## 1. Kiểu cấu trúc & Kết quả (`ValidationRule`, `ValidationResult`)

- **`ValidationResult`:**
  - `IsValid` (`bool`): `true` nếu qua toàn bộ quy tắc.
  - `ErrorMessage` (`string`): nội dung thông báo lỗi ở quy tắc đầu tiên bị sai.
  - `ErrorControl` (`Control`): control gây lỗi để gọi `FormHelper.FocusControl(result.ErrorControl)`.
  - `Success()`, `Fail(message, control)`.

---

## 2. Danh sách đầy đủ các quy tắc kiểm tra (`Validator`)

| Phương thức | Chức năng & Hành vi chi tiết |
|---|---|
| `Required(ctrl, fieldName, msg = null)` | Kiểm tra chuỗi nhập không null/toàn khoảng trắng. |
| `Pattern(ctrl, fieldName, regex, msg = null)` | Kiểm tra khớp biểu thức chính quy sau khi `.Trim()` khoảng trắng hai đầu (chỉ kiểm tra khi có giá trị, ghép với `Required` nếu bắt buộc nhập). |
| `MinLength(ctrl, fieldName, min, msg = null)` | Độ dài tối thiểu `>= min`. |
| `MaxLength(ctrl, fieldName, max, msg = null)` | Độ dài tối đa `<= max`. |
| `IntRange(ctrl, fieldName, min, max, msg = null)` | Kiểm tra số nguyên hợp lệ trong `[min, max]`. |
| `DecimalRange(ctrl, fieldName, min, max, msg = null)` | Phân tích số thực qua `DataHelper.TryParseDecimal` (chấp nhận cả `"2,34"` và `"2.34"`) rồi kiểm trong `[min, max]`. |
| `DateRange(ctrl, fieldName, min, max, msg = null)` | Kiểm tra ngày (`.Date`) nằm trong `[min.Date, max.Date]`. |
| `NotFutureDate(ctrl, fieldName, msg = null)` | Ngày không vượt quá `DateTime.Today`. |
| `AgeRange(ctrl, fieldName, minAge, maxAge, msg = null)` | Tính tuổi tại `DateTime.Today` theo đúng ngày sinh nhật (`CalculateAge`). |
| `AgeRangeAtDate(ctrl, fieldName, minAge, maxAge, msg = null)` | Tính tuổi tại ngày tham chiếu truyền vào `ValidateAll(referenceDate, ...)`. |
| `AgeRangeAtDate(birthDateControl, referenceDateControl, fieldName, minAge, maxAge, msg = null)` | Tính tuổi của `birthDateControl` tại ngày đang chọn trên `referenceDateControl` (ví dụ `dtpNgayVaoLam`), không cần truyền lại ở đầu `ValidateAll`. |
| `DateBeforeOrEqual(ctrl, targetDateControl, fieldName, msg = null)` | Kiểm tra `ctrl.Date <= targetDateControl.Date` (ví dụ Ngày lập <= Ngày giao). |
| `Selected(cmb, fieldName, msg = null)` | Kiểm tra `ComboBox` đã chọn hợp lệ (chặn chưa chọn, rỗng, hoặc đang ở index `0` với giá trị `"0"` / `"-1"`). |
| `Custom(ctrl, fieldName, Func<string, bool> check, msg = null)` | Quy tắc logic tùy chỉnh bất kỳ. |

---

## 3. Kiểm tra tự động theo Schema CSDL (`Validator.FromSchema`)

Thay vì viết tay toàn bộ rule `Required` và `MaxLength` cho mọi ô nhập liệu, bạn có thể dùng `FromSchema` kết hợp thêm các rule nghiệp vụ đặc thù:

```csharp
ValidationResult FromSchema(Control container, DataTable schemaTable, params ValidationRule[] extraRules);
```

**Cơ chế của `FromSchema`:**
1. Duyệt các control nhập liệu trong `container` (bỏ qua `CheckBox`, `RadioButton`, `DateTimePicker`).
2. Khớp tên control với cột trong `schemaTable` (bỏ qua cột `AutoIncrement`).
3. Tự kiểm tra:
   - `!dc.AllowDBNull` → kiểm tra `Required` (hoặc `Selected` với `ComboBox`).
   - `dc.DataType == typeof(string)` & `dc.MaxLength > 0` → kiểm tra `MaxLength`.
   - Kiểu `int`/`short`/`long` → kiểm tra phải là số nguyên hợp lệ (nếu có nhập).
   - Kiểu `decimal`/`double`/`float` → kiểm tra phải là số hợp lệ (nếu có nhập).
4. Cuối cùng chạy tuần tự toàn bộ `extraRules` truyền kèm.

---

## 4. Hàm tính tuổi & Ví dụ sử dụng

```csharp
// Tính tuổi chính xác theo ngày và tháng sinh
int age = Validator.CalculateAge(dtpNgaySinh.Value, dtpNgayVaoLam.Value);

// Kiểm tra chuẩn kết hợp Schema + Nghiệp vụ đề thi
DataTable schema = DbHelper.GetTableSchema("NhanVien");
var val = Validator.FromSchema(this, schema,
    Validator.Pattern(txtMaNV, "Mã NV", @"^NV\d{3}$", "Mã NV phải có dạng NV + 3 chữ số!"),
    Validator.DecimalRange(txtHeSoLuong, "Hệ số lương", 2.34m, 8.0m, "Hệ số lương phải từ 2,34 – 8,0!"),
    Validator.AgeRangeAtDate(dtpNgaySinh, dtpNgayVaoLam, "Tuổi", 18, 120,
        "Nhân viên phải đủ 18 tuổi tính đến ngày vào làm!")
);

if (!val.IsValid)
{
    MessageHelper.Warning(val.ErrorMessage);
    FormHelper.FocusControl(val.ErrorControl);
    return;
}
```
