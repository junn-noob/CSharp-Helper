using System;
using System.Windows.Forms;

namespace ExamHelper.Utilities
{
    /// <summary>
    /// Lớp tiện ích cung cấp các hộp thoại thông báo chuẩn cho ứng dụng WinForms.
    /// Hỗ trợ các thông báo tiếng Việt thường dùng trong đề thi.
    /// </summary>
    public static class MessageHelper
    {
        /// <summary>
        /// Khi đặt true, không bật MessageBox lên màn hình (phục vụ kiểm thử tự động). Mặc định: false.
        /// </summary>
        public static bool SuppressDialogs { get; set; } = false;

        /// <summary>
        /// Kết quả mặc định trả về cho Confirm/ConfirmDelete khi SuppressDialogs = true. Mặc định: true (Yes).
        /// </summary>
        public static bool AutoConfirmResult { get; set; } = true;

        /// <summary>
        /// Nội dung thông báo gần nhất được gọi qua MessageHelper.
        /// </summary>
        public static string LastMessage { get; private set; }

        /// <summary>
        /// Tiêu đề thông báo gần nhất được gọi qua MessageHelper.
        /// </summary>
        public static string LastTitle { get; private set; }

        private static DialogResult Show(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            LastMessage = message;
            LastTitle = title;

            if (SuppressDialogs)
            {
                if (buttons == MessageBoxButtons.YesNo)
                    return AutoConfirmResult ? DialogResult.Yes : DialogResult.No;
                return DialogResult.OK;
            }

            return MessageBox.Show(message, title, buttons, icon);
        }

        /// <summary>
        /// Hiển thị hộp thoại thông tin.
        /// </summary>
        /// <param name="message">Nội dung thông báo.</param>
        /// <param name="title">Tiêu đề hộp thoại (mặc định: "Thông báo").</param>
        public static void Info(string message, string title = "Thông báo")
        {
            Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Hiển thị hộp thoại lỗi.
        /// </summary>
        /// <param name="message">Nội dung thông báo lỗi.</param>
        /// <param name="title">Tiêu đề hộp thoại (mặc định: "Lỗi").</param>
        public static void Error(string message, string title = "Lỗi")
        {
            Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Hiển thị hộp thoại cảnh báo.
        /// </summary>
        /// <param name="message">Nội dung cảnh báo.</param>
        /// <param name="title">Tiêu đề hộp thoại (mặc định: "Cảnh báo").</param>
        public static void Warning(string message, string title = "Cảnh báo")
        {
            Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Hiển thị hộp thoại xác nhận xóa với nút Yes/No.
        /// </summary>
        /// <param name="itemName">Tên đối tượng cần xóa (ví dụ: "sinh viên này"). Để trống nếu không cần chỉ rõ.</param>
        /// <returns>True nếu người dùng chọn Yes, False nếu chọn No.</returns>
        public static bool ConfirmDelete(string itemName = "")
        {
            string message = string.IsNullOrWhiteSpace(itemName)
                ? "Bạn có chắc chắn muốn xóa không?"
                : $"Bạn có chắc chắn muốn xóa {itemName} không?";

            return Show(message, "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        /// <summary>
        /// Hiển thị hộp thoại xác nhận tùy chỉnh với nút Yes/No.
        /// </summary>
        /// <param name="message">Nội dung câu hỏi xác nhận.</param>
        /// <param name="title">Tiêu đề hộp thoại (mặc định: "Xác nhận").</param>
        /// <returns>True nếu người dùng chọn Yes, False nếu chọn No.</returns>
        public static bool Confirm(string message, string title = "Xác nhận")
        {
            return Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        /// <summary>
        /// Hiển thị thông báo thao tác thành công.
        /// </summary>
        /// <param name="action">Tên thao tác (ví dụ: "thêm", "xóa", "cập nhật"). Để trống để hiển thị "Thành công!".</param>
        public static void Success(string action = "")
        {
            string message = string.IsNullOrWhiteSpace(action)
                ? "Thành công!"
                : $"Thao tác {action} thành công!";

            Show(message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Hiển thị thông báo yêu cầu người dùng chọn một mục trước khi thao tác.
        /// </summary>
        /// <param name="itemName">Tên đối tượng cần chọn (mặc định: "dòng").</param>
        public static void NoSelection(string itemName = "dòng")
        {
            Show($"Vui lòng chọn {itemName} cần thao tác", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Hiển thị thông báo không tìm thấy dữ liệu.
        /// </summary>
        public static void NoData()
        {
            Show("Không tìm thấy dữ liệu", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Hiển thị thông báo trùng khóa/giá trị đã tồn tại.
        /// </summary>
        /// <param name="fieldName">Tên trường bị trùng (ví dụ: "Mã sinh viên").</param>
        /// <param name="value">Giá trị bị trùng.</param>
        public static void DuplicateKey(string fieldName, string value)
        {
            Show($"{fieldName} '{value}' đã tồn tại", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
