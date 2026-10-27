using System;
using System.Windows.Forms;
using System.Drawing;

namespace ExamHelper.Utilities
{
    /// <summary>
    /// Lớp tiện ích cung cấp các phương thức hỗ trợ thao tác với Form và Control trong WinForms.
    /// Bao gồm các mẫu thường gặp trong đề thi thực hành C#.
    /// </summary>
    public static class FormHelper
    {
        /// <summary>
        /// Thiết lập các thuộc tính mặc định cho Form.
        /// Đặt vị trí khởi động ở giữa màn hình và tiêu đề nếu được cung cấp.
        /// </summary>
        /// <param name="form">Form cần thiết lập.</param>
        /// <param name="title">Tiêu đề form (để trống nếu không muốn thay đổi).</param>
        public static void SetFormDefaults(Form form, string title = "")
        {
            if (form == null)
                return;

            form.StartPosition = FormStartPosition.CenterScreen;

            if (!string.IsNullOrWhiteSpace(title))
                form.Text = title;
        }

        /// <summary>
        /// Bật hoặc tắt nhiều control cùng lúc.
        /// </summary>
        /// <param name="enabled">True để bật, False để tắt.</param>
        /// <param name="controls">Danh sách các control cần thay đổi trạng thái.</param>
        public static void EnableControls(bool enabled, params Control[] controls)
        {
            if (controls == null)
                return;

            foreach (Control ctrl in controls)
            {
                if (ctrl != null)
                    ctrl.Enabled = enabled;
            }
        }

        /// <summary>
        /// Chuyển đổi trạng thái các nút giữa chế độ xem và chế độ chỉnh sửa.
        /// Đây là mẫu phổ biến trong đề thi: khi đang chỉnh sửa thì tắt Thêm/Sửa/Xóa, bật Lưu/Hủy và ngược lại.
        /// </summary>
        /// <param name="isEditing">True nếu đang ở chế độ chỉnh sửa, False nếu ở chế độ xem.</param>
        /// <param name="btnAdd">Nút Thêm.</param>
        /// <param name="btnEdit">Nút Sửa.</param>
        /// <param name="btnDelete">Nút Xóa.</param>
        /// <param name="btnSave">Nút Lưu.</param>
        /// <param name="btnCancel">Nút Hủy.</param>
        public static void ToggleButtons(bool isEditing, Button btnAdd, Button btnEdit,
            Button btnDelete, Button btnSave, Button btnCancel)
        {
            // Chế độ xem: bật Thêm/Sửa/Xóa, tắt Lưu/Hủy
            // Chế độ chỉnh sửa: tắt Thêm/Sửa/Xóa, bật Lưu/Hủy
            EnableControls(!isEditing, btnAdd, btnEdit, btnDelete);
            EnableControls(isEditing, btnSave, btnCancel);
        }

        /// <summary>
        /// Xóa/đặt lại giá trị mặc định cho nhiều control cùng lúc.
        /// Hỗ trợ TextBox (xóa text), ComboBox (chọn mục đầu tiên), 
        /// DateTimePicker (đặt về ngày hiện tại), CheckBox (bỏ chọn),
        /// NumericUpDown (đặt về giá trị nhỏ nhất), RichTextBox (xóa text).
        /// </summary>
        /// <param name="controls">Danh sách các control cần reset.</param>
        public static void ResetForm(params Control[] controls)
        {
            if (controls == null)
                return;

            foreach (Control ctrl in controls)
            {
                if (ctrl == null)
                    continue;

                if (ctrl is TextBox textBox)
                {
                    textBox.Clear();
                }
                else if (ctrl is ComboBox comboBox)
                {
                    if (comboBox.Items.Count > 0)
                        comboBox.SelectedIndex = 0;
                    else
                        comboBox.SelectedIndex = -1;
                }
                else if (ctrl is DateTimePicker dateTimePicker)
                {
                    dateTimePicker.Value = DateTime.Now;
                    if (dateTimePicker.ShowCheckBox)
                        dateTimePicker.Checked = false;
                }
                else if (ctrl is CheckBox checkBox)
                {
                    checkBox.Checked = false;
                }
                else if (ctrl is NumericUpDown numericUpDown)
                {
                    numericUpDown.Value = numericUpDown.Minimum;
                }
                else if (ctrl is RichTextBox richTextBox)
                {
                    richTextBox.Clear();
                }
            }
        }

        /// <summary>
        /// Đặt focus vào control được chỉ định.
        /// Nếu control là TextBox, tự động chọn toàn bộ text.
        /// </summary>
        /// <param name="ctrl">Control cần đặt focus.</param>
        public static void FocusControl(Control ctrl)
        {
            if (ctrl == null)
                return;

            ctrl.Focus();

            if (ctrl is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        /// <summary>
        /// Thiết lập thứ tự Tab cho danh sách các control, bắt đầu từ 0.
        /// </summary>
        /// <param name="controls">Danh sách control theo thứ tự Tab mong muốn.</param>
        public static void SetTabOrder(params Control[] controls)
        {
            if (controls == null)
                return;

            for (int i = 0; i < controls.Length; i++)
            {
                if (controls[i] != null)
                    controls[i].TabIndex = i;
            }
        }

        /// <summary>
        /// Định dạng giá trị ô trong DataGridView theo kiểu tiền tệ Việt Nam (ví dụ: "1.500.000 đ").
        /// Sử dụng trong sự kiện CellFormatting của DataGridView.
        /// </summary>
        /// <param name="e">Đối tượng sự kiện CellFormatting.</param>
        /// <example>
        /// <code>
        /// private void dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        /// {
        ///     if (e.ColumnIndex == dgv.Columns["GiaTien"].Index)
        ///         FormHelper.FormatCurrency(e);
        /// }
        /// </code>
        /// </example>
        public static void FormatCurrency(DataGridViewCellFormattingEventArgs e)
        {
            if (e == null || e.Value == null || e.Value == DBNull.Value)
                return;

            if (decimal.TryParse(e.Value.ToString(), out decimal value))
            {
                e.Value = value.ToString("N0") + " đ";
                e.FormattingApplied = true;
            }
        }

        /// <summary>
        /// Đặt trạng thái chỉ đọc (ReadOnly) cho một hoặc nhiều TextBox.
        /// Mẫu phổ biến: khóa TextBox mã chính khi chỉnh sửa để tránh sửa khóa.
        /// </summary>
        /// <param name="readOnly">True để khóa, False để mở khóa.</param>
        /// <param name="textBoxes">Danh sách TextBox cần thiết lập.</param>
        public static void SetTextBoxesReadOnly(bool readOnly, params TextBox[] textBoxes)
        {
            if (textBoxes == null)
                return;

            foreach (var txt in textBoxes)
            {
                if (txt != null)
                    txt.ReadOnly = readOnly;
            }
        }

        /// <summary>
        /// Bật hoặc tắt trạng thái Enabled cho toàn bộ các control bên trong một container (GroupBox, Panel,...).
        /// </summary>
        /// <param name="container">Control chứa (ví dụ: grpThongTin, pnlNhapLieu).</param>
        /// <param name="enabled">True để bật, False để tắt.</param>
        public static void EnableContainer(Control container, bool enabled)
        {
            if (container == null)
                return;

            foreach (Control child in container.Controls)
            {
                child.Enabled = enabled;
            }
        }
    }
}
