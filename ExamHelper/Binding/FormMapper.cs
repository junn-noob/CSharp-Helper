using System;
using System.Data;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ExamHelper.Binding
{
    /// <summary>
    /// Ánh xạ dữ liệu giữa các control trên Form và DataTable/DataRow.
    /// Hỗ trợ TextBox, ComboBox, DateTimePicker, CheckBox, NumericUpDown, RichTextBox.
    /// </summary>
    public static class FormMapper
    {
        #region Lớp FieldMapping

        /// <summary>
        /// Định nghĩa ánh xạ giữa một control trên Form và một cột trong DataTable.
        /// </summary>
        public class FieldMapping
        {
            /// <summary>Control trên Form</summary>
            public Control Control { get; set; }

            /// <summary>Tên cột trong DataTable/DataRow</summary>
            public string ColumnName { get; set; }

            /// <summary>
            /// Tạo ánh xạ giữa control và cột dữ liệu.
            /// </summary>
            /// <param name="control">Control trên Form</param>
            /// <param name="columnName">Tên cột trong DataTable</param>
            public FieldMapping(Control control, string columnName)
            {
                Control = control;
                ColumnName = columnName;
            }
        }

        #endregion

        #region Factory Method

        /// <summary>
        /// Tạo nhanh FieldMapping.
        /// </summary>
        /// <param name="control">Control trên Form</param>
        /// <param name="columnName">Tên cột trong DataTable</param>
        /// <returns>FieldMapping mới</returns>
        public static FieldMapping Map(Control control, string columnName)
        {
            return new FieldMapping(control, columnName);
        }

        #endregion

        #region GetValues

        /// <summary>
        /// Đọc giá trị từ các control theo danh sách ánh xạ, trả về Dictionary để dùng với CrudHelper.
        /// </summary>
        /// <param name="mappings">Danh sách ánh xạ control-cột</param>
        /// <returns>Dictionary với key là tên cột, value là giá trị từ control</returns>
        public static Dictionary<string, object> GetValues(params FieldMapping[] mappings)
        {
            var result = new Dictionary<string, object>();
            foreach (var m in mappings)
            {
                if (m == null || m.Control == null) continue;
                result[m.ColumnName] = ReadControlValue(m.Control);
            }
            return result;
        }

        /// <summary>
        /// Đọc giá trị từ một control cụ thể.
        /// </summary>
        private static object ReadControlValue(Control ctrl)
        {
            if (ctrl is TextBox txt)
                return txt.Text.Trim();
            if (ctrl is ComboBox cmb)
                return cmb.SelectedValue ?? cmb.Text;
            if (ctrl is DateTimePicker dtp)
            {
                if (dtp.ShowCheckBox && !dtp.Checked)
                    return DBNull.Value;
                return dtp.Value;
            }
            if (ctrl is CheckBox chk)
                return chk.Checked;
            if (ctrl is NumericUpDown nud)
                return nud.Value;
            if (ctrl is RichTextBox rtb)
                return rtb.Text.Trim();
            if (ctrl is RadioButton rb)
                return rb.Checked;
            if (ctrl is MaskedTextBox mtb)
                return mtb.Text.Trim();
            return ctrl.Text;
        }

        #endregion

        #region LoadRow

        /// <summary>
        /// Nạp dữ liệu từ DataRow lên các control theo danh sách ánh xạ.
        /// Xử lý DBNull an toàn.
        /// </summary>
        /// <param name="row">Dòng dữ liệu nguồn</param>
        /// <param name="mappings">Danh sách ánh xạ</param>
        public static void LoadRow(DataRow row, params FieldMapping[] mappings)
        {
            if (row == null) return;
            foreach (var m in mappings)
            {
                if (m == null || m.Control == null) continue;
                if (!row.Table.Columns.Contains(m.ColumnName)) continue;

                object value = row[m.ColumnName];
                WriteControlValue(m.Control, value);
            }
        }

        /// <summary>
        /// Nạp dữ liệu từ DataRowView lên các control.
        /// </summary>
        /// <param name="rowView">DataRowView nguồn (từ DataGridView)</param>
        /// <param name="mappings">Danh sách ánh xạ</param>
        public static void LoadRow(DataRowView rowView, params FieldMapping[] mappings)
        {
            if (rowView == null) return;
            LoadRow(rowView.Row, mappings);
        }

        /// <summary>
        /// Ghi giá trị vào control dựa trên loại control.
        /// </summary>
        private static void WriteControlValue(Control ctrl, object value)
        {
            bool isNull = value == null || value == DBNull.Value;

            if (ctrl is TextBox txt)
            {
                txt.Text = isNull ? "" : value.ToString();
            }
            else if (ctrl is ComboBox cmb)
            {
                if (isNull)
                {
                    if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
                    else cmb.SelectedIndex = -1;
                }
                else
                {
                    if (cmb.DataSource != null)
                    {
                        ComboBoxHelper.SelectByValue(cmb, value);
                    }
                    else
                    {
                        cmb.Text = value.ToString();
                    }
                }
            }
            else if (ctrl is DateTimePicker dtp)
            {
                if (!isNull)
                {
                    try
                    {
                        dtp.Value = Convert.ToDateTime(value);
                        if (dtp.ShowCheckBox) dtp.Checked = true;
                    }
                    catch
                    {
                        dtp.Value = DateTime.Now;
                    }
                }
                else
                {
                    if (dtp.ShowCheckBox) dtp.Checked = false;
                    dtp.Value = DateTime.Now;
                }
            }
            else if (ctrl is CheckBox chk)
            {
                if (!isNull)
                {
                    try { chk.Checked = Convert.ToBoolean(value); }
                    catch { chk.Checked = false; }
                }
                else
                {
                    chk.Checked = false;
                }
            }
            else if (ctrl is NumericUpDown nud)
            {
                if (!isNull)
                {
                    try
                    {
                        decimal val = Convert.ToDecimal(value);
                        if (val < nud.Minimum) val = nud.Minimum;
                        if (val > nud.Maximum) val = nud.Maximum;
                        nud.Value = val;
                    }
                    catch { nud.Value = nud.Minimum; }
                }
                else
                {
                    nud.Value = nud.Minimum;
                }
            }
            else if (ctrl is RichTextBox rtb)
            {
                rtb.Text = isNull ? "" : value.ToString();
            }
            else if (ctrl is RadioButton rb)
            {
                if (!isNull)
                {
                    try { rb.Checked = Convert.ToBoolean(value); }
                    catch { rb.Checked = false; }
                }
                else
                {
                    rb.Checked = false;
                }
            }
            else if (ctrl is MaskedTextBox mtb)
            {
                mtb.Text = isNull ? "" : value.ToString();
            }
            else
            {
                ctrl.Text = isNull ? "" : value.ToString();
            }
        }

        #endregion

        #region ClearControls

        /// <summary>
        /// Xóa/đặt lại giá trị mặc định cho danh sách control.
        /// TextBox → rỗng, ComboBox → index 0, DateTimePicker → hôm nay,
        /// CheckBox → false, NumericUpDown → giá trị minimum.
        /// </summary>
        /// <param name="controls">Danh sách control cần xóa</param>
        public static void ClearControls(params Control[] controls)
        {
            foreach (var ctrl in controls)
            {
                if (ctrl == null) continue;

                if (ctrl is TextBox txt)
                    txt.Text = "";
                else if (ctrl is ComboBox cmb)
                {
                    if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
                    else cmb.SelectedIndex = -1;
                }
                else if (ctrl is DateTimePicker dtp)
                {
                    dtp.Value = DateTime.Now;
                    if (dtp.ShowCheckBox) dtp.Checked = false;
                }
                else if (ctrl is CheckBox chk)
                    chk.Checked = false;
                else if (ctrl is NumericUpDown nud)
                    nud.Value = nud.Minimum;
                else if (ctrl is RichTextBox rtb)
                    rtb.Text = "";
                else if (ctrl is RadioButton rb)
                    rb.Checked = false;
                else if (ctrl is MaskedTextBox mtb)
                    mtb.Text = "";
                else
                    ctrl.Text = "";
            }
        }

        #endregion
    }
}
