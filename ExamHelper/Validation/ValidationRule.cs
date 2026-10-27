using System;
using System.Windows.Forms;

namespace ExamHelper.Validation
{
    /// <summary>
    /// Các loại kiểm tra dữ liệu được hỗ trợ.
    /// </summary>
    public enum ValidationType
    {
        /// <summary>Bắt buộc nhập (không được để trống).</summary>
        Required,

        /// <summary>Kiểm tra theo biểu thức chính quy (Param1 = pattern string).</summary>
        Pattern,

        /// <summary>Độ dài tối thiểu (Param1 = int minLength).</summary>
        MinLength,

        /// <summary>Độ dài tối đa (Param1 = int maxLength).</summary>
        MaxLength,

        /// <summary>Khoảng số nguyên (Param1 = int min, Param2 = int max).</summary>
        IntRange,

        /// <summary>Khoảng số thực (Param1 = decimal min, Param2 = decimal max).</summary>
        DecimalRange,

        /// <summary>Khoảng ngày (Param1 = DateTime min, Param2 = DateTime max).</summary>
        DateRange,

        /// <summary>Khoảng tuổi tính tại ngày hiện tại (Param1 = int minAge, Param2 = int maxAge).</summary>
        AgeRange,

        /// <summary>
        /// Khoảng tuổi tính tại một ngày cụ thể (Param1 = int minAge, Param2 = int maxAge).
        /// Ngày tham chiếu sẽ được truyền khi gọi ValidateAll.
        /// </summary>
        AgeRangeAtDate,

        /// <summary>Hàm kiểm tra tùy chỉnh (Param1 = Func&lt;string, bool&gt;).</summary>
        Custom
    }

    /// <summary>
    /// Đại diện cho một quy tắc kiểm tra dữ liệu trên một Control.
    /// </summary>
    public class ValidationRule
    {
        /// <summary>Control cần kiểm tra.</summary>
        public Control Control { get; set; }

        /// <summary>Tên trường hiển thị trong thông báo lỗi.</summary>
        public string FieldName { get; set; }

        /// <summary>Loại kiểm tra.</summary>
        public ValidationType Type { get; set; }

        /// <summary>Tham số phụ thứ nhất (min, regex pattern, Func, v.v.).</summary>
        public object Param1 { get; set; }

        /// <summary>Tham số phụ thứ hai (max, v.v.).</summary>
        public object Param2 { get; set; }

        /// <summary>Thông báo lỗi tùy chỉnh. Nếu null, hệ thống sẽ tự sinh thông báo mặc định.</summary>
        public string CustomMessage { get; set; }
    }

    /// <summary>
    /// Kết quả của quá trình kiểm tra dữ liệu.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>Cho biết dữ liệu có hợp lệ hay không.</summary>
        public bool IsValid { get; set; }

        /// <summary>Thông báo lỗi (nếu không hợp lệ).</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Control gây ra lỗi (nếu không hợp lệ).</summary>
        public Control ErrorControl { get; set; }

        /// <summary>
        /// Tạo kết quả thành công (hợp lệ).
        /// </summary>
        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// Tạo kết quả thất bại (không hợp lệ).
        /// </summary>
        /// <param name="message">Thông báo lỗi.</param>
        /// <param name="control">Control gây ra lỗi.</param>
        public static ValidationResult Fail(string message, Control control)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = message,
                ErrorControl = control
            };
        }
    }
}
