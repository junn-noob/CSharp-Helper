using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ExamHelper.Validation
{
    /// <summary>
    /// Lớp tiện ích tĩnh hỗ trợ kiểm tra dữ liệu trên các Control WinForms.
    /// Cung cấp các phương thức tạo quy tắc kiểm tra (fluent) và xác thực hàng loạt.
    /// </summary>
    public static class Validator
    {
        #region Factory Methods - Tạo quy tắc kiểm tra

        /// <summary>
        /// Tạo quy tắc bắt buộc nhập (không được để trống).
        /// </summary>
        /// <param name="ctrl">Control cần kiểm tra.</param>
        /// <param name="fieldName">Tên trường hiển thị trong thông báo lỗi.</param>
        /// <param name="msg">Thông báo lỗi tùy chỉnh (null = tự sinh).</param>
        public static ValidationRule Required(Control ctrl, string fieldName, string msg = null)
        {
            return new ValidationRule
            {
                Control = ctrl,
                FieldName = fieldName,
                Type = ValidationType.Required,
                CustomMessage = msg
            };
        }

        /// <summary>
        /// Tạo quy tắc kiểm tra theo biểu thức chính quy (Regex).
        /// </summary>
        /// <param name="ctrl">Control cần kiểm tra.</param>
        /// <param name="fieldName">Tên trường hiển thị trong thông báo lỗi.</param>
        /// <param name="regex">Chuỗi biểu thức chính quy.</param>
        /// <param name="msg">Thông báo lỗi tùy chỉnh (null = tự sinh).</param>
        public static ValidationRule Pattern(Control ctrl, string fieldName, string regex, string msg = null)
        {
            return new ValidationRule
            {
                Control = ctrl,
                FieldName = fieldName,
                Type = ValidationType.Pattern,
                Param1 = regex,
                CustomMessage = msg
            };
        }

        /// <summary>
        /// Tạo quy tắc kiểm tra độ dài tối thiểu.
        /// </summary>
        /// <param name="ctrl">Control cần kiểm tra.</param>
        /// <param name="fieldName">Tên trường hiển thị trong thông báo lỗi.</param>
        /// <param name="min">Độ dài tối thiểu.</param>
        /// <param name="msg">Thông báo lỗi tùy chỉnh (null = tự sinh).</param>
        public static ValidationRule MinLength(Control ctrl, string fieldName, int min, string msg = null)
        {
            return new ValidationRule
            {
                Control = ctrl,
                FieldName = fieldName,
                Type = ValidationType.MinLength,
                Param1 = min,
                CustomMessage = msg
            };
        }

        /// <summary>
        /// Tạo quy tắc kiểm tra độ dài tối đa.
        /// </summary>
        /// <param name="ctrl">Control cần kiểm tra.</param>
        /// <param name="fieldName">Tên trường hiển thị trong thông báo lỗi.</param>
        /// <param name="max">Độ dài tối đa.</param>
        /// <param name="msg">Thông báo lỗi tùy chỉnh (null = tự sinh).</param>
        public static ValidationRule MaxLength(Control ctrl, string fieldName, int max, string msg = null)
        {
            return new ValidationRule
            {
                Control = ctrl,
                FieldName = fieldName,
                Type = ValidationType.MaxLength,
                Param1 = max,
                CustomMessage = msg
            };
        }

        /// <summary>
        /// Tạo quy tắc kiểm tra khoảng số nguyên.
        /// </summary>
        /// <param name="ctrl">Control cần kiểm tra.</param>
        /// <param name="fieldName">Tên trường hiển thị trong thông báo lỗi.</param>
        /// <param name="min">Giá trị tối thiểu.</param>
        /// <param name="max">Giá trị tối đa.</param>
        /// <param name="msg">Thông báo lỗi tùy chỉnh (null = tự sinh).</param>
        public static ValidationRule IntRange(Control ctrl, string fieldName, int min, int max, string msg = null)
        {
            return new ValidationRule
            {
                Control = ctrl,
                FieldName = fieldName,
                Type = ValidationType.IntRange,
                Param1 = min,
                Param2 = max,
                CustomMessage = msg
            };
        }

        /// <summary>
        /// Tạo quy tắc kiểm tra khoảng số thực.
        /// </summary>
        /// <param name="ctrl">Control cần kiểm tra.</param>
        /// <param name="fieldName">Tên trường hiển thị trong thông báo lỗi.</param>
        /// <param name="min">Giá trị tối thiểu.</param>
        /// <param name="max">Giá trị tối đa.</param>
        /// <param name="msg">Thông báo lỗi tùy chỉnh (null = tự sinh).</param>
        public static ValidationRule DecimalRange(Control ctrl, string fieldName, decimal min, decimal max, string msg = null)
        {
            return new ValidationRule
            {
                Control = ctrl,
                FieldName = fieldName,
                Type = ValidationType.DecimalRange,
                Param1 = min,
                Param2 = max,
                CustomMessage = msg
            };
        }

        /// <summary>
        /// Tạo quy tắc kiểm tra khoảng ngày.
        /// </summary>
        /// <param name="ctrl">Control cần kiểm tra.</param>
        /// <param name="fieldName">Tên trường hiển thị trong thông báo lỗi.</param>
        /// <param name="min">Ngày tối thiểu.</param>
        /// <param name="max">Ngày tối đa.</param>
        /// <param name="msg">Thông báo lỗi tùy chỉnh (null = tự sinh).</param>
        public static ValidationRule DateRange(Control ctrl, string fieldName, DateTime min, DateTime max, string msg = null)
        {
            return new ValidationRule
            {
                Control = ctrl,
                FieldName = fieldName,
                Type = ValidationType.DateRange,
                Param1 = min,
                Param2 = max,
                CustomMessage = msg
            };
        }

        /// <summary>
        /// Tạo quy tắc kiểm tra khoảng tuổi (tính tại ngày hiện tại).
        /// </summary>
        /// <param name="ctrl">Control chứa ngày sinh.</param>
        /// <param name="fieldName">Tên trường hiển thị trong thông báo lỗi.</param>
        /// <param name="minAge">Tuổi tối thiểu.</param>
        /// <param name="maxAge">Tuổi tối đa.</param>
        /// <param name="msg">Thông báo lỗi tùy chỉnh (null = tự sinh).</param>
        public static ValidationRule AgeRange(Control ctrl, string fieldName, int minAge, int maxAge, string msg = null)
        {
            return new ValidationRule
            {
                Control = ctrl,
                FieldName = fieldName,
                Type = ValidationType.AgeRange,
                Param1 = minAge,
                Param2 = maxAge,
                CustomMessage = msg
            };
        }

        /// <summary>
        /// Tạo quy tắc kiểm tra khoảng tuổi tính tại một ngày cụ thể.
        /// Ngày tham chiếu sẽ được truyền khi gọi <see cref="ValidateAll(DateTime, ValidationRule[])"/>.
        /// </summary>
        /// <param name="ctrl">Control chứa ngày sinh.</param>
        /// <param name="fieldName">Tên trường hiển thị trong thông báo lỗi.</param>
        /// <param name="minAge">Tuổi tối thiểu.</param>
        /// <param name="maxAge">Tuổi tối đa.</param>
        /// <param name="msg">Thông báo lỗi tùy chỉnh (null = tự sinh).</param>
        public static ValidationRule AgeRangeAtDate(Control ctrl, string fieldName, int minAge, int maxAge, string msg = null)
        {
            return new ValidationRule
            {
                Control = ctrl,
                FieldName = fieldName,
                Type = ValidationType.AgeRangeAtDate,
                Param1 = minAge,
                Param2 = maxAge,
                CustomMessage = msg
            };
        }

        /// <summary>
        /// Tạo quy tắc kiểm tra tùy chỉnh bằng hàm do người dùng cung cấp.
        /// </summary>
        /// <param name="ctrl">Control cần kiểm tra.</param>
        /// <param name="fieldName">Tên trường hiển thị trong thông báo lỗi.</param>
        /// <param name="check">
        /// Hàm kiểm tra nhận giá trị chuỗi của Control và trả về true nếu hợp lệ.
        /// </param>
        /// <param name="msg">Thông báo lỗi tùy chỉnh (null = tự sinh).</param>
        public static ValidationRule Custom(Control ctrl, string fieldName, Func<string, bool> check, string msg = null)
        {
            return new ValidationRule
            {
                Control = ctrl,
                FieldName = fieldName,
                Type = ValidationType.Custom,
                Param1 = check,
                CustomMessage = msg
            };
        }

        /// <summary>
        /// Tạo quy tắc kiểm tra ComboBox đã được chọn (SelectedIndex &gt;= 0 và giá trị không rỗng, không phải dòng nhắc 0).
        /// </summary>
        /// <param name="cmb">ComboBox cần kiểm tra.</param>
        /// <param name="fieldName">Tên trường hiển thị trong thông báo lỗi.</param>
        /// <param name="msg">Thông báo lỗi tùy chỉnh (null = "Vui lòng chọn {fieldName}").</param>
        public static ValidationRule Selected(ComboBox cmb, string fieldName, string msg = null)
        {
            return Custom(cmb, fieldName, val =>
            {
                if (cmb == null || cmb.SelectedIndex < 0) return false;
                if (string.IsNullOrWhiteSpace(val)) return false;
                if (val == "0" && cmb.SelectedIndex == 0) return false;
                return true;
            }, msg ?? $"Vui lòng chọn {fieldName}");
        }

        #endregion

        #region ValidateAll - Xác thực hàng loạt

        /// <summary>
        /// Kiểm tra tất cả các quy tắc theo thứ tự. Dừng lại ở quy tắc đầu tiên không hợp lệ.
        /// Đối với <see cref="ValidationType.AgeRange"/>, tuổi được tính tại ngày hiện tại.
        /// Đối với <see cref="ValidationType.AgeRangeAtDate"/>, sử dụng ngày hiện tại làm ngày tham chiếu.
        /// </summary>
        /// <param name="rules">Danh sách các quy tắc kiểm tra.</param>
        /// <returns>Kết quả kiểm tra: thành công hoặc thất bại với thông báo lỗi và Control tương ứng.</returns>
        public static ValidationResult ValidateAll(params ValidationRule[] rules)
        {
            return ValidateAll(DateTime.Today, rules);
        }

        /// <summary>
        /// Kiểm tra tất cả các quy tắc theo thứ tự với ngày tham chiếu cụ thể.
        /// Dừng lại ở quy tắc đầu tiên không hợp lệ.
        /// </summary>
        /// <param name="referenceDate">Ngày tham chiếu dùng cho <see cref="ValidationType.AgeRangeAtDate"/>.</param>
        /// <param name="rules">Danh sách các quy tắc kiểm tra.</param>
        /// <returns>Kết quả kiểm tra: thành công hoặc thất bại với thông báo lỗi và Control tương ứng.</returns>
        public static ValidationResult ValidateAll(DateTime referenceDate, params ValidationRule[] rules)
        {
            if (rules == null || rules.Length == 0)
                return ValidationResult.Success();

            foreach (var rule in rules)
            {
                if (rule == null) continue;

                string value = GetControlValue(rule.Control);
                string error = ValidateRule(rule, value, referenceDate);

                if (error != null)
                {
                    return ValidationResult.Fail(error, rule.Control);
                }
            }

            return ValidationResult.Success();
        }

        #endregion

        #region Tính tuổi

        /// <summary>
        /// Tính tuổi chính xác dựa trên ngày sinh và ngày tham chiếu.
        /// Tuổi = referenceDate.Year - birthDate.Year, trừ 1 nếu sinh nhật chưa đến trong năm tham chiếu.
        /// </summary>
        /// <param name="birthDate">Ngày sinh.</param>
        /// <param name="referenceDate">Ngày tham chiếu để tính tuổi.</param>
        /// <returns>Tuổi (số nguyên).</returns>
        public static int CalculateAge(DateTime birthDate, DateTime referenceDate)
        {
            int age = referenceDate.Year - birthDate.Year;

            // Nếu sinh nhật chưa đến trong năm tham chiếu thì trừ 1
            if (referenceDate.Month < birthDate.Month ||
                (referenceDate.Month == birthDate.Month && referenceDate.Day < birthDate.Day))
            {
                age--;
            }

            return age;
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Lấy giá trị chuỗi từ Control tùy theo loại Control.
        /// An toàn với control null.
        /// </summary>
        private static string GetControlValue(Control ctrl)
        {
            if (ctrl == null)
                return string.Empty;

            if (ctrl is DateTimePicker dtp)
            {
                return dtp.Value.ToString();
            }

            if (ctrl is ComboBox cmb)
            {
                // Ưu tiên SelectedValue, nếu không có thì dùng Text
                return cmb.SelectedValue?.ToString() ?? cmb.Text;
            }

            if (ctrl is CheckBox chk)
            {
                return chk.Checked.ToString();
            }

            // TextBox, MaskedTextBox, RichTextBox và các Control khác
            return ctrl.Text;
        }

        /// <summary>
        /// Lấy giá trị DateTime từ Control (hỗ trợ DateTimePicker và parse từ Text).
        /// </summary>
        private static bool TryGetDateValue(Control ctrl, out DateTime date)
        {
            if (ctrl == null)
            {
                date = DateTime.MinValue;
                return false;
            }

            if (ctrl is DateTimePicker dtp)
            {
                date = dtp.Value.Date;
                return true;
            }

            return DateTime.TryParse(ctrl.Text, out date);
        }

        /// <summary>
        /// Kiểm tra một quy tắc đơn lẻ. Trả về thông báo lỗi nếu không hợp lệ, hoặc null nếu hợp lệ.
        /// </summary>
        private static string ValidateRule(ValidationRule rule, string value, DateTime referenceDate)
        {
            switch (rule.Type)
            {
                case ValidationType.Required:
                    return ValidateRequired(rule, value);

                case ValidationType.Pattern:
                    return ValidatePattern(rule, value);

                case ValidationType.MinLength:
                    return ValidateMinLength(rule, value);

                case ValidationType.MaxLength:
                    return ValidateMaxLength(rule, value);

                case ValidationType.IntRange:
                    return ValidateIntRange(rule, value);

                case ValidationType.DecimalRange:
                    return ValidateDecimalRange(rule, value);

                case ValidationType.DateRange:
                    return ValidateDateRange(rule);

                case ValidationType.AgeRange:
                    return ValidateAgeRange(rule, DateTime.Today);

                case ValidationType.AgeRangeAtDate:
                    return ValidateAgeRange(rule, referenceDate);

                case ValidationType.Custom:
                    return ValidateCustom(rule, value);

                default:
                    return null;
            }
        }

        /// <summary>Kiểm tra bắt buộc nhập.</summary>
        private static string ValidateRequired(ValidationRule rule, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return rule.CustomMessage ?? $"{rule.FieldName} không được để trống";
            }
            return null;
        }

        /// <summary>Kiểm tra biểu thức chính quy.</summary>
        private static string ValidatePattern(ValidationRule rule, string value)
        {
            string pattern = (string)rule.Param1;
            if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, pattern))
            {
                return rule.CustomMessage ?? $"{rule.FieldName} không đúng định dạng";
            }
            return null;
        }

        /// <summary>Kiểm tra độ dài tối thiểu.</summary>
        private static string ValidateMinLength(ValidationRule rule, string value)
        {
            int min = (int)rule.Param1;
            if (value == null || value.Length < min)
            {
                return rule.CustomMessage ?? $"{rule.FieldName} phải có ít nhất {min} ký tự";
            }
            return null;
        }

        /// <summary>Kiểm tra độ dài tối đa.</summary>
        private static string ValidateMaxLength(ValidationRule rule, string value)
        {
            int max = (int)rule.Param1;
            if (value != null && value.Length > max)
            {
                return rule.CustomMessage ?? $"{rule.FieldName} không được vượt quá {max} ký tự";
            }
            return null;
        }

        /// <summary>Kiểm tra khoảng số nguyên.</summary>
        private static string ValidateIntRange(ValidationRule rule, string value)
        {
            int min = (int)rule.Param1;
            int max = (int)rule.Param2;

            string clean = (value ?? "").Trim().Replace(" ", "");
            if (!int.TryParse(clean, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int intVal) &&
                !int.TryParse(clean, out intVal))
            {
                return rule.CustomMessage ?? $"{rule.FieldName} phải là số nguyên hợp lệ";
            }

            if (intVal < min || intVal > max)
            {
                return rule.CustomMessage ?? $"{rule.FieldName} phải từ {min} đến {max}";
            }

            return null;
        }

        /// <summary>Kiểm tra khoảng số thực (chấp nhận cả dấu chấm và dấu phẩy thập phân).</summary>
        private static string ValidateDecimalRange(ValidationRule rule, string value)
        {
            decimal min = (decimal)rule.Param1;
            decimal max = (decimal)rule.Param2;

            if (!ExamHelper.Utilities.DataHelper.TryParseDecimal(value, out decimal decVal))
            {
                return rule.CustomMessage ?? $"{rule.FieldName} phải là số hợp lệ";
            }

            if (decVal < min || decVal > max)
            {
                return rule.CustomMessage ?? $"{rule.FieldName} phải từ {min} đến {max}";
            }

            return null;
        }

        /// <summary>Kiểm tra khoảng ngày.</summary>
        private static string ValidateDateRange(ValidationRule rule)
        {
            DateTime min = (DateTime)rule.Param1;
            DateTime max = (DateTime)rule.Param2;

            if (!TryGetDateValue(rule.Control, out DateTime dateVal))
            {
                return rule.CustomMessage ?? $"{rule.FieldName} phải là ngày hợp lệ";
            }

            if (dateVal.Date < min.Date || dateVal.Date > max.Date)
            {
                return rule.CustomMessage ?? $"{rule.FieldName} phải từ {min:dd/MM/yyyy} đến {max:dd/MM/yyyy}";
            }

            return null;
        }

        /// <summary>Kiểm tra khoảng tuổi (dùng chung cho AgeRange và AgeRangeAtDate).</summary>
        private static string ValidateAgeRange(ValidationRule rule, DateTime refDate)
        {
            int minAge = (int)rule.Param1;
            int maxAge = (int)rule.Param2;

            if (!TryGetDateValue(rule.Control, out DateTime birthDate))
            {
                return rule.CustomMessage ?? $"{rule.FieldName} phải là ngày hợp lệ";
            }

            int age = CalculateAge(birthDate, refDate);

            if (age < minAge || age > maxAge)
            {
                return rule.CustomMessage ?? $"Tuổi phải từ {minAge} đến {maxAge}";
            }

            return null;
        }

        /// <summary>Kiểm tra tùy chỉnh bằng hàm do người dùng cung cấp.</summary>
        private static string ValidateCustom(ValidationRule rule, string value)
        {
            var check = (Func<string, bool>)rule.Param1;

            if (check != null && !check(value))
            {
                return rule.CustomMessage ?? $"{rule.FieldName} không hợp lệ";
            }

            return null;
        }

        #endregion
    }
}
