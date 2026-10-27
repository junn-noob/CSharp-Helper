# HƯỚNG DẪN CHI TIẾT: MODULE VALIDATION (KIỂM TRA DỮ LIỆU)
> **Namespace:** `ExamHelper.Validation`  
> **Các lớp chính:** `Validator`, `ValidationRule`, `ValidationResult`

---

## 1. Mục đích & Khi nào dùng
- Đề thi WinForms luôn chiếm **1.5 đến 2.0 điểm** cho phần kiểm tra dữ liệu đầu vào.
- Nếu viết kiểm tra bằng tay bằng các lệnh `if/else`, code sẽ rất dài, rối và dễ sót trường hợp biên.
- `Validator` cung cấp cơ chế kiểm tra theo chuỗi (Fluent API) cực kỳ ngắn gọn.
- Khi gặp lỗi đầu tiên, hàm tự động dừng, trả về thông báo lỗi tiếng Việt rõ ràng và **trả về chính xác Control đang bị sai** để bạn đưa con trỏ chuột về đó (`FocusControl`).

---

## 2. Danh sách các quy tắc kiểm tra (Rules)

| Quy tắc | Cú pháp khai báo | Ý nghĩa & Ví dụ |
|---|---|---|
| **Bắt buộc nhập** | `Validator.Required(ctrl, name)` | Không được để trống. |
| **Định dạng Regex** | `Validator.Pattern(ctrl, name, regex, msg)` | Kiểm tra theo định dạng (VD: Mã NV `^NV\d{3}$`, Email, SĐT). |
| **Khoảng số nguyên** | `Validator.IntRange(ctrl, name, min, max)` | Kiểm tra số nguyên trong đoạn `[min, max]`. |
| **Khoảng số thực** | `Validator.DecimalRange(ctrl, name, min, max)` | Kiểm tra số thực (hệ số lương, điểm số, giá tiền). |
| **Khoảng ngày tháng** | `Validator.DateRange(ctrl, name, minDate, maxDate)` | Kiểm tra ngày nằm trong khoảng. |
| **Khoảng tuổi hiện tại** | `Validator.AgeRange(ctrl, name, minAge, maxAge)` | Tính tuổi tại ngày hiện tại. |
| **Khoảng tuổi tại ngày mốc** | `Validator.AgeRangeAtDate(ctrl, name, minAge, maxAge)` | **Đặc sản đề thi:** Tính tuổi tại một ngày mốc cụ thể (như Ngày vào làm, Ngày nhập học). |
| **Quy tắc tùy biến** | `Validator.Custom(ctrl, name, Func<string, bool>, msg)` | Tự viết hàm kiểm tra logic riêng. |
| **Tính tuổi chính xác** | `Validator.CalculateAge(birthDate, refDate)` | Hàm tính tuổi chuẩn xác theo năm và ngày sinh nhật. |

---

## 3. Quy trình thực hiện kiểm tra với `ValidateAll`

Hàm `Validator.ValidateAll(...)` nhận vào danh sách các quy tắc và thực hiện kiểm tra tuần tự từ trên xuống dưới:

```csharp
var result = Validator.ValidateAll(
    // (Tùy chọn) Truyền ngày mốc nếu đề yêu cầu tính tuổi tại ngày vào làm
    dtpNgayVaoLam.Value,

    Validator.Required(txtMaNV, "Mã nhân viên"),
    Validator.Pattern(txtMaNV, "Mã nhân viên", @"^NV\d{3}$", "Mã phải có dạng NV001!"),
    Validator.Required(txtHoTen, "Họ tên"),
    Validator.DecimalRange(txtHeSoLuong, "Hệ số lương", 2.34m, 8.0m, "Hệ số lương phải từ 2.34 đến 8.0!"),
    Validator.AgeRangeAtDate(dtpNgaySinh, "Tuổi nhân viên", 18, 100, "Nhân viên phải đủ 18 tuổi tính đến ngày vào làm!")
);

// Xử lý kết quả chỉ trong 4 dòng:
if (!result.IsValid)
{
    MessageHelper.Warning(result.ErrorMessage);
    FormHelper.FocusControl(result.ErrorControl); // Tự động trỏ chuột vào ô sai
    return;
}
```

---

## 4. Các ví dụ áp dụng thường gặp trong đề thi

### Ví dụ 1: Đề thi Quản lý Sinh viên
- Mã SV bắt buộc, dạng `SV` + 4 số (`^SV\d{4}$`).
- Họ tên không để trống.
- Điểm trung bình từ `0.0` đến `10.0`.
- Tuổi sinh viên từ `18` đến `30`.

```csharp
var val = Validator.ValidateAll(
    Validator.Required(txtMaSV, "Mã sinh viên"),
    Validator.Pattern(txtMaSV, "Mã sinh viên", @"^SV\d{4}$", "Mã sinh viên phải có dạng SV0001"),
    Validator.Required(txtHoTen, "Họ tên sinh viên"),
    Validator.DecimalRange(txtDiemTB, "Điểm trung bình", 0.0m, 10.0m),
    Validator.AgeRange(dtpNgaySinh, "Tuổi sinh viên", 18, 30)
);

if (!val.IsValid)
{
    MessageHelper.Warning(val.ErrorMessage);
    FormHelper.FocusControl(val.ErrorControl);
    return;
}
```

---

### Ví dụ 2: Đề thi Quản lý Sách / Sản phẩm
- Mã sách bắt buộc.
- Giá bán từ `1,000` đến `10,000,000` đ.
- Số lượng tồn từ `0` đến `5,000`.
- Năm xuất bản từ `1900` đến năm hiện tại.

```csharp
var val = Validator.ValidateAll(
    Validator.Required(txtMaSach, "Mã sách"),
    Validator.Required(txtTieuDe, "Tiêu đề sách"),
    Validator.DecimalRange(txtGiaBan, "Giá bán", 1000m, 10000000m),
    Validator.IntRange(txtSoLuongTon, "Số lượng tồn", 0, 5000),
    Validator.IntRange(txtNamXB, "Năm xuất bản", 1900, DateTime.Now.Year)
);
```

---

### Ví dụ 3: Logic tùy biến với `Validator.Custom`
Kiểm tra số điện thoại phải bắt đầu bằng `0` và có đúng `10` chữ số:

```csharp
var val = Validator.ValidateAll(
    Validator.Custom(txtSDT, "Số điện thoại", 
        text => text.StartsWith("0") && text.Length == 10 && long.TryParse(text, out _),
        "Số điện thoại phải bắt đầu bằng số 0 và gồm đúng 10 chữ số!")
);
```
