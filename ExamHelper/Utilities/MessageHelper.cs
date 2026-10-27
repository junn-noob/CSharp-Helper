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
        /// Hiển thị hộp thoại thông tin.
        /// </summary>
        /// <param name="message">Nội dung thông báo.</param>
        /// <param name="title">Tiêu đề hộp thoại (mặc định: "Thông báo").</param>
        public static void Info(string message, string title = "Thông báo")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Hiển thị hộp thoại lỗi.
        /// </summary>
        /// <param name="message">Nội dung thông báo lỗi.</param>
        /// <param name="title">Tiêu đề hộp thoại (mặc định: "Lỗi").</param>
        public static void Error(string message, string title = "Lỗi")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Hiển thị hộp thoại cảnh báo.
        /// </summary>
        /// <param name="message">Nội dung cảnh báo.</param>
        /// <param name="title">Tiêu đề hộp thoại (mặc định: "Cảnh báo").</param>
        public static void Warning(string message, string title = "Cảnh báo")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            DialogResult result = MessageBox.Show(
                message,
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            return result == DialogResult.Yes;
        }

        /// <summary>
        /// Hiển thị hộp thoại xác nhận tùy chỉnh với nút Yes/No.
        /// </summary>
        /// <param name="message">Nội dung câu hỏi xác nhận.</param>
        /// <param name="title">Tiêu đề hộp thoại (mặc định: "Xác nhận").</param>
        /// <returns>True nếu người dùng chọn Yes, False nếu chọn No.</returns>
        public static bool Confirm(string message, string title = "Xác nhận")
        {
            DialogResult result = MessageBox.Show(
                message,
                title,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            return result == DialogResult.Yes;
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

            MessageBox.Show(message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Hiển thị thông báo yêu cầu người dùng chọn một mục trước khi thao tác.
        /// </summary>
        /// <param name="itemName">Tên đối tượng cần chọn (mặc định: "dòng").</param>
        public static void NoSelection(string itemName = "dòng")
        {
            MessageBox.Show(
                $"Vui lòng chọn {itemName} cần thao tác",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Hiển thị thông báo không tìm thấy dữ liệu.
        /// </summary>
        public static void NoData()
        {
            MessageBox.Show(
                "Không tìm thấy dữ liệu",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        /// <summary>
        /// Hiển thị thông báo trùng khóa/giá trị đã tồn tại.
        /// </summary>
        /// <param name="fieldName">Tên trường bị trùng (ví dụ: "Mã sinh viên").</param>
        /// <param name="value">Giá trị bị trùng.</param>
        public static void DuplicateKey(string fieldName, string value)
        {
            MessageBox.Show(
                $"{fieldName} '{value}' đã tồn tại",
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
