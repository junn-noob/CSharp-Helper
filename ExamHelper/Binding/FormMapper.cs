using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using System.Windows.Forms;
using ExamHelper.Database;

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

            /// <summary>Control phụ (ví dụ: RadioButton đại diện cho giá trị false)</summary>
            public Control SecondaryControl { get; set; }

            /// <summary>Tên cột trong DataTable/DataRow</summary>
            public string ColumnName { get; set; }

            /// <summary>Kiểu dữ liệu đích khi đọc từ Control xuống Dictionary (tùy chọn, vd typeof(decimal), typeof(double), typeof(int))</summary>
            public Type TargetType { get; set; }

            /// <summary>
            /// Tạo ánh xạ giữa control và cột dữ liệu.
            /// </summary>
            /// <param name="control">Control trên Form</param>
            /// <param name="columnName">Tên cột trong DataTable</param>
            /// <param name="targetType">Kiểu dữ liệu đích khi đọc ra Dictionary (tùy chọn)</param>
            public FieldMapping(Control control, string columnName, Type targetType = null)
            {
                Control = control;
                ColumnName = columnName;
                TargetType = targetType;
            }

            /// <summary>
            /// Tạo ánh xạ cho cặp RadioButton đại diện cho giá trị đúng/sai (ví dụ: Nam/Nữ).
            /// </summary>
            /// <param name="trueRadio">RadioButton ứng với giá trị true / 1</param>
            /// <param name="falseRadio">RadioButton ứng với giá trị false / 0</param>
            /// <param name="columnName">Tên cột trong DataTable</param>
            public FieldMapping(RadioButton trueRadio, RadioButton falseRadio, string columnName)
            {
                Control = trueRadio;
                SecondaryControl = falseRadio;
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

        /// <summary>
        /// Tạo nhanh FieldMapping kèm ép kiểu dữ liệu đích khi đọc từ Form (ví dụ: decimal, double, int).
        /// </summary>
        public static FieldMapping Map(Control control, string columnName, Type targetType)
        {
            return new FieldMapping(control, columnName, targetType);
        }

        /// <summary>
        /// Tạo nhanh FieldMapping kiểu generic T (ví dụ: Map&lt;double&gt;(txtHeSoLuong, "HeSoLuong")).
        /// </summary>
        public static FieldMapping Map<T>(Control control, string columnName)
        {
            return new FieldMapping(control, columnName, typeof(T));
        }

        /// <summary>
        /// Ánh xạ TextBox/Control số tiền hoặc số thập phân (hỗ trợ cả dấu phẩy "2,34" và dấu chấm "2.34") về kiểu decimal chuẩn SQL.
        /// </summary>
        public static FieldMapping MapDecimal(Control control, string columnName, bool nullable = false)
        {
            return new FieldMapping(control, columnName, nullable ? typeof(decimal?) : typeof(decimal));
        }

        /// <summary>
        /// Ánh xạ TextBox/Control số thực (như Hệ số lương "2,34") về kiểu double chuẩn cột FLOAT trong SQL Server.
        /// </summary>
        public static FieldMapping MapDouble(Control control, string columnName, bool nullable = false)
        {
            return new FieldMapping(control, columnName, nullable ? typeof(double?) : typeof(double));
        }

        /// <summary>
        /// Ánh xạ TextBox/Control số nguyên (hỗ trợ cả "1.500.000") về kiểu int chuẩn cột INT trong SQL Server.
        /// </summary>
        public static FieldMapping MapInt(Control control, string columnName, bool nullable = false)
        {
            return new FieldMapping(control, columnName, nullable ? typeof(int?) : typeof(int));
        }

        /// <summary>
        /// Tạo nhanh FieldMapping cho cặp RadioButton (ví dụ: rdoNam là true, rdoNu là false).
        /// </summary>
        /// <param name="trueRadio">RadioButton ứng với giá trị true / 1</param>
        /// <param name="falseRadio">RadioButton ứng với giá trị false / 0</param>
        /// <param name="columnName">Tên cột trong DataTable</param>
        /// <returns>FieldMapping mới</returns>
        public static FieldMapping Map(RadioButton trueRadio, RadioButton falseRadio, string columnName)
        {
            return new FieldMapping(trueRadio, falseRadio, columnName);
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
                object raw = ReadControlValue(m.Control);
                result[m.ColumnName] = m.TargetType != null
                    ? ConvertToTargetType(raw, m.TargetType)
                    : raw;
            }
            return result;
        }

        private static object ConvertToTargetType(object raw, Type targetType)
        {
            bool isNullable = Nullable.GetUnderlyingType(targetType) != null;
            Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (raw == null || raw == DBNull.Value)
                return isNullable ? DBNull.Value : (underlying.IsValueType ? Activator.CreateInstance(underlying) : null);

            if (raw is string s)
            {
                if (string.IsNullOrWhiteSpace(s))
                    return isNullable ? DBNull.Value : (underlying.IsValueType ? Activator.CreateInstance(underlying) : (object)"");

                if (underlying == typeof(decimal))
                    return ExamHelper.Utilities.DataHelper.TryParseDecimal(s, out decimal d) ? d : 0m;
                if (underlying == typeof(double))
                    return ExamHelper.Utilities.DataHelper.TryParseDecimal(s, out decimal db) ? (double)db : 0.0;
                if (underlying == typeof(float))
                    return ExamHelper.Utilities.DataHelper.TryParseDecimal(s, out decimal fl) ? (float)fl : 0f;
                if (underlying == typeof(int))
                    return ExamHelper.Utilities.DataHelper.TryParseDecimal(s, out decimal i) ? Convert.ToInt32(i) : 0;
                if (underlying == typeof(long))
                    return ExamHelper.Utilities.DataHelper.TryParseDecimal(s, out decimal l) ? Convert.ToInt64(l) : 0L;
            }

            try
            {
                return Convert.ChangeType(raw, underlying, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return raw;
            }
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
                if (!row.Table.Columns.Contains(m.ColumnName))
                {
                    // Fallback thông minh cho ComboBox: nếu quên SELECT cột ValueMember (MaPB) nhưng có cột DisplayMember (TenPB)
                    if (m.Control is ComboBox cmbFallback &&
                        !string.IsNullOrEmpty(cmbFallback.DisplayMember) &&
                        row.Table.Columns.Contains(cmbFallback.DisplayMember))
                    {
                        ComboBoxHelper.SelectByText(cmbFallback, row[cmbFallback.DisplayMember]?.ToString());
                    }
                    continue;
                }

                object value = row[m.ColumnName];

                if (m.Control is RadioButton rbTrue && m.SecondaryControl is RadioButton rbFalse)
                {
                    bool isTrue = ParseBoolSafe(value);
                    rbTrue.Checked = isTrue;
                    rbFalse.Checked = !isTrue;
                    continue;
                }

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
        /// Nạp dòng đầu tiên của DataTable lên các control trên Form.
        /// </summary>
        /// <returns>True nếu bảng có dữ liệu và đã nạp, False nếu bảng null hoặc rỗng.</returns>
        public static bool LoadRow(DataTable table, params FieldMapping[] mappings)
        {
            if (table == null || table.Rows.Count == 0)
                return false;

            LoadRow(table.Rows[0], mappings);
            return true;
        }

        /// <summary>
        /// Thực thi trực tiếp câu lệnh SQL query và đổ dòng kết quả đầu tiên lên các control trên Form.
        /// </summary>
        /// <returns>True nếu tìm thấy dữ liệu và đã nạp lên Form, False nếu không có dòng nào.</returns>
        public static bool LoadSql(string sql, params FieldMapping[] mappings)
        {
            DataTable dt = DbHelper.GetDataTable(sql);
            return LoadRow(dt, mappings);
        }

        /// <summary>
        /// Thực thi trực tiếp câu lệnh SQL query có tham số và đổ dòng kết quả đầu tiên lên các control trên Form.
        /// </summary>
        public static bool LoadSql(string sql, DbParameter[] parameters, params FieldMapping[] mappings)
        {
            return LoadSql(sql, parameters, null, mappings);
        }

        /// <summary>
        /// Thực thi trực tiếp câu lệnh SQL query (hỗ trợ tham số và connectionString/dbName) và đổ lên Form.
        /// </summary>
        public static bool LoadSql(string sql, DbParameter[] parameters, string connection, params FieldMapping[] mappings)
        {
            DataTable dt = string.IsNullOrWhiteSpace(connection)
                ? DbHelper.GetDataTable(sql, parameters ?? Array.Empty<DbParameter>())
                : DbHelper.GetDataTable(sql, connection, parameters ?? Array.Empty<DbParameter>());
            return LoadRow(dt, mappings);
        }

        #endregion

        #region AutoMap theo Control trên Form (Không cần liệt kê Map thủ công)

        private static readonly string[] ControlPrefixes = { "txt", "cbo", "cmb", "dtp", "chk", "nud", "rtb", "mtb", "rdo" };

        /// <summary>
        /// Tự động đổ dữ liệu từ một <see cref="DataRow"/> lên toàn bộ control nhập liệu trong Form/Panel/GroupBox
        /// dựa trên Tag, ComboBox.ValueMember hoặc tên control (bỏ tiền tố txt/cbo/dtp/chk/nud/rtb/mtb/rdo).
        /// </summary>
        public static void AutoLoad(Control container, DataRow row)
        {
            if (container == null || row == null) return;

            DataColumnCollection cols = row.Table.Columns;
            foreach (Control ctrl in EnumerateInputControls(container))
            {
                if (!TryResolveField(ctrl, cols, out string colName, out string radioVal))
                    continue;
                if (!cols.Contains(colName))
                    continue;

                object value = row[colName];
                if (ctrl is RadioButton rb && radioVal != null)
                {
                    rb.Checked = MatchesRadioValue(value, radioVal);
                }
                else
                {
                    WriteControlValue(ctrl, value);
                }
            }
        }

        /// <summary>Tự động đổ dữ liệu từ <see cref="DataRowView"/> lên các control trong container.</summary>
        public static void AutoLoad(Control container, DataRowView rowView)
        {
            if (rowView != null) AutoLoad(container, rowView.Row);
        }

        /// <summary>Tự động đổ dòng đầu tiên của <see cref="DataTable"/> lên các control trong container.</summary>
        public static bool AutoLoad(Control container, DataTable table)
        {
            if (table == null || table.Rows.Count == 0) return false;
            AutoLoad(container, table.Rows[0]);
            return true;
        }

        /// <summary>
        /// Chạy câu lệnh SQL query và tự động đổ dòng kết quả đầu tiên lên toàn bộ control khớp tên trên Form/container.
        /// </summary>
        public static bool AutoLoadSql(Control container, string sql, params DbParameter[] parameters)
        {
            return AutoLoadSql(container, sql, parameters, null);
        }

        /// <summary>
        /// Chạy câu lệnh SQL query (hỗ trợ connection riêng) và tự động đổ dòng đầu tiên lên các control trên Form/container.
        /// </summary>
        public static bool AutoLoadSql(Control container, string sql, DbParameter[] parameters, string connection)
        {
            DataTable dt = string.IsNullOrWhiteSpace(connection)
                ? DbHelper.GetDataTable(sql, parameters ?? Array.Empty<DbParameter>())
                : DbHelper.GetDataTable(sql, connection, parameters ?? Array.Empty<DbParameter>());
            return AutoLoad(container, dt);
        }

        /// <summary>
        /// Tự động thu thập giá trị từ tất cả control nhập liệu trong container thành Dictionary dùng cho CrudHelper.
        /// Nếu truyền <paramref name="schemaTable"/>, chỉ lấy các cột có trong bảng và tự ép kiểu số (double/decimal/int) chuẩn SQL.
        /// </summary>
        public static Dictionary<string, object> AutoGetValues(Control container, DataTable schemaTable = null)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (container == null) return result;

            DataColumnCollection cols = schemaTable?.Columns;
            foreach (Control ctrl in EnumerateInputControls(container))
            {
                if (!TryResolveField(ctrl, cols, out string colName, out string radioVal))
                    continue;
                if (cols != null && !cols.Contains(colName))
                    continue;

                Type targetType = cols != null ? cols[colName].DataType : null;

                if (ctrl is RadioButton rb)
                {
                    if (radioVal == null) continue;
                    if (rb.Checked)
                    {
                        result[colName] = ParseRadioValue(radioVal, targetType);
                    }
                    else if (!result.ContainsKey(colName) && radioVal == "1" && targetType == typeof(bool))
                    {
                        result[colName] = false;
                    }
                    continue;
                }

                object raw = ReadControlValue(ctrl);
                result[colName] = targetType != null ? ConvertToTargetType(raw, targetType) : raw;
            }

            return result;
        }

        /// <summary>Xóa/đặt lại mặc định tất cả control nhập liệu bên trong container (Form, GroupBox, Panel).</summary>
        public static void AutoClear(Control container)
        {
            if (container == null) return;
            foreach (Control ctrl in EnumerateInputControls(container))
            {
                ClearControls(ctrl);
            }
        }

        /// <summary>
        /// Xóa/đặt lại mặc định tất cả control nhập liệu trong container và tự tick lại các <see cref="RadioButton"/> mặc định (ví dụ <c>rdoNam</c>).
        /// </summary>
        public static void AutoClear(Control container, params RadioButton[] defaultCheckedRadios)
        {
            AutoClear(container);
            if (defaultCheckedRadios == null) return;
            foreach (var rb in defaultCheckedRadios)
            {
                if (rb != null) rb.Checked = true;
            }
        }

        internal static IEnumerable<Control> EnumerateInputControls(Control root)
        {
            foreach (Control ctrl in root.Controls)
            {
                if (IsInputControl(ctrl))
                {
                    yield return ctrl;
                }
                else if (ctrl.HasChildren && !(ctrl is DataGridView) && !(ctrl is WebBrowser))
                {
                    foreach (Control child in EnumerateInputControls(ctrl))
                        yield return child;
                }
            }
        }

        private static bool IsInputControl(Control ctrl)
        {
            return ctrl is TextBox
                || ctrl is ComboBox
                || ctrl is DateTimePicker
                || ctrl is CheckBox
                || ctrl is NumericUpDown
                || ctrl is RichTextBox
                || ctrl is MaskedTextBox
                || ctrl is RadioButton;
        }

        internal static bool TryResolveField(Control ctrl, DataColumnCollection cols, out string colName, out string radioValue)
        {
            colName = null;
            radioValue = null;

            if (ctrl.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
            {
                string trimmedTag = tag.Trim();
                int colon = trimmedTag.IndexOf(':');
                if (colon > 0)
                {
                    colName = trimmedTag.Substring(0, colon).Trim();
                    radioValue = trimmedTag.Substring(colon + 1).Trim();
                }
                else
                {
                    colName = trimmedTag;
                }

                if (cols != null && cols.Contains(colName))
                    colName = cols[colName].ColumnName;
                return !string.IsNullOrWhiteSpace(colName);
            }

            if (ctrl is ComboBox cmb && !string.IsNullOrWhiteSpace(cmb.ValueMember))
            {
                if (cols == null || cols.Contains(cmb.ValueMember))
                {
                    colName = cols != null ? cols[cmb.ValueMember].ColumnName : cmb.ValueMember;
                    return true;
                }
            }

            string name = ctrl.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (cols != null && cols.Contains(name))
            {
                colName = cols[name].ColumnName;
                return true;
            }

            string stripped = StripPrefix(name);
            if (cols != null)
            {
                if (cols.Contains(stripped))
                {
                    colName = cols[stripped].ColumnName;
                    return true;
                }
                return false;
            }

            colName = stripped;
            return !string.IsNullOrWhiteSpace(colName);
        }

        private static string StripPrefix(string controlName)
        {
            foreach (string prefix in ControlPrefixes)
            {
                if (controlName.Length > prefix.Length &&
                    controlName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    (controlName[prefix.Length] == '_' || char.IsUpper(controlName[prefix.Length])))
                {
                    return controlName.Substring(prefix.Length).TrimStart('_');
                }
            }
            return controlName;
        }

        private static bool MatchesRadioValue(object rowVal, string expected)
        {
            if (rowVal == null || rowVal == DBNull.Value) return false;
            string sExp = (expected ?? "").Trim();
            if (string.Equals(sExp, "1", StringComparison.Ordinal) || string.Equals(sExp, "true", StringComparison.OrdinalIgnoreCase))
                return ParseBoolSafe(rowVal);
            if (string.Equals(sExp, "0", StringComparison.Ordinal) || string.Equals(sExp, "false", StringComparison.OrdinalIgnoreCase))
                return !ParseBoolSafe(rowVal);
            return string.Equals(rowVal.ToString().Trim(), sExp, StringComparison.OrdinalIgnoreCase);
        }

        private static object ParseRadioValue(string raw, Type targetType)
        {
            if (targetType == typeof(bool))
                return raw == "1" || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
            if (targetType == typeof(int) || targetType == typeof(short) || targetType == typeof(byte))
                return int.TryParse(raw, out int i) ? Convert.ChangeType(i, targetType) : raw;
            if (raw == "1") return true;
            if (raw == "0") return false;
            return raw;
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
                    else if (!ComboBoxHelper.SelectByText(cmb, value.ToString()))
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
                        DateTime dt = Convert.ToDateTime(value);
                        if (dt < dtp.MinDate) dt = dtp.MinDate;
                        if (dt > dtp.MaxDate) dt = dtp.MaxDate;
                        dtp.Value = dt;
                        if (dtp.ShowCheckBox) dtp.Checked = true;
                    }
                    catch
                    {
                        DateTime fallback = DateTime.Now;
                        if (fallback < dtp.MinDate) fallback = dtp.MinDate;
                        if (fallback > dtp.MaxDate) fallback = dtp.MaxDate;
                        dtp.Value = fallback;
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
                rb.Checked = ParseBoolSafe(value);
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

        private static bool ParseBoolSafe(object value)
        {
            if (value == null || value == DBNull.Value) return false;
            if (value is bool b) return b;

            string s = value.ToString().Trim();
            if (s == "1") return true;
            if (s == "0") return false;

            try { return Convert.ToBoolean(value); }
            catch { return false; }
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
