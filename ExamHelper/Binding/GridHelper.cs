using System;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Windows.Forms;
using ExamHelper.Database;

namespace ExamHelper.Binding
{
    /// <summary>
    /// Lớp tiện ích hỗ trợ binding dữ liệu và cấu hình DataGridView trong các ứng dụng WinForms.
    /// Cung cấp các phương thức gán dữ liệu, thiết lập cột, truy xuất giá trị dòng đã chọn.
    /// </summary>
    public static class GridHelper
    {
        /// <summary>
        /// Đại diện cho cấu hình một cột trong DataGridView.
        /// </summary>
        public class GridColumn
        {
            /// <summary>
            /// Tên trường dữ liệu (tên cột trong DataTable) liên kết với cột này.
            /// </summary>
            public string DataField { get; set; }

            /// <summary>
            /// Tiêu đề hiển thị của cột trên DataGridView.
            /// </summary>
            public string HeaderText { get; set; }

            /// <summary>
            /// Chiều rộng cột (pixel). Giá trị -1 để tự động co giãn.
            /// </summary>
            public int Width { get; set; }

            /// <summary>
            /// Chuỗi định dạng hiển thị (ví dụ: "N0", "dd/MM/yyyy"). Null nếu không cần định dạng.
            /// </summary>
            public string Format { get; set; }

            /// <summary>
            /// Căn lề nội dung ô (trái, giữa, phải). Null nếu dùng mặc định.
            /// </summary>
            public DataGridViewContentAlignment? Alignment { get; set; }

            /// <summary>
            /// Khởi tạo cấu hình cột cho DataGridView.
            /// </summary>
            /// <param name="dataField">Tên trường dữ liệu (DataPropertyName).</param>
            /// <param name="headerText">Tiêu đề cột hiển thị.</param>
            /// <param name="width">Chiều rộng cột. Mặc định -1 (tự động).</param>
            /// <param name="format">Chuỗi định dạng hiển thị. Mặc định null.</param>
            /// <param name="alignment">Căn lề hiển thị nội dung ô. Mặc định null.</param>
            public GridColumn(string dataField, string headerText, int width = -1, string format = null, DataGridViewContentAlignment? alignment = null)
            {
                DataField = dataField;
                HeaderText = headerText;
                Width = width;
                Format = format;
                Alignment = alignment;
            }
        }

        /// <summary>
        /// Gán dữ liệu từ DataTable vào DataGridView (binding đơn giản).
        /// </summary>
        /// <param name="grid">DataGridView cần gán dữ liệu.</param>
        /// <param name="data">DataTable chứa dữ liệu nguồn.</param>
        /// <exception cref="ArgumentNullException">Khi grid hoặc data là null.</exception>
        public static void Bind(DataGridView grid, DataTable data)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid), "DataGridView không được null.");
            if (data == null)
                throw new ArgumentNullException(nameof(data), "DataTable không được null.");

            if (grid.BindingContext == null)
                grid.BindingContext = new BindingContext();

            if (grid.Columns.Count == 0)
                grid.AutoGenerateColumns = true;

            grid.DataSource = data;
        }

        /// <summary>
        /// Chạy trực tiếp câu lệnh SQL query và đổ toàn bộ dữ liệu lên DataGridView
        /// (tự động căn lề/định dạng ngày N0 và đặt chế độ ReadOnly nếu không truyền danh sách cột riêng).
        /// </summary>
        public static void Bind(DataGridView grid, string sql, params GridColumn[] columns)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid), "DataGridView không được null.");

            DataTable dt = DbHelper.GetDataTable(sql);
            BindQueryInternal(grid, dt, columns, readOnly: true);
        }

        /// <summary>
        /// Chạy câu lệnh SQL kết hợp điều kiện <see cref="FilterBuilder"/> và ORDER BY rồi đổ dữ liệu lên DataGridView.
        /// </summary>
        public static void Bind(DataGridView grid, string sql, FilterBuilder filter, string orderBy = null, params GridColumn[] columns)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid), "DataGridView không được null.");

            DataTable dt = DbHelper.GetDataTable(sql, filter, orderBy);
            BindQueryInternal(grid, dt, columns, readOnly: true);
        }

        /// <summary>
        /// Chạy câu lệnh SQL có tham số (<see cref="DbParameter"/>) và đổ dữ liệu lên DataGridView.
        /// </summary>
        public static void BindSql(DataGridView grid, string sql, params DbParameter[] parameters)
        {
            BindSql(grid, sql, parameters, null);
        }

        /// <summary>
        /// Chạy câu lệnh SQL có tham số, tùy chọn tên kết nối / connectionString riêng và danh sách cột hiển thị.
        /// </summary>
        public static void BindSql(DataGridView grid, string sql, DbParameter[] parameters, string connection, params GridColumn[] columns)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid), "DataGridView không được null.");

            DataTable dt = string.IsNullOrWhiteSpace(connection)
                ? DbHelper.GetDataTable(sql, parameters ?? Array.Empty<DbParameter>())
                : DbHelper.GetDataTable(sql, connection, parameters ?? Array.Empty<DbParameter>());
            BindQueryInternal(grid, dt, columns, readOnly: true);
        }

        internal static void BindFormatted(DataGridView grid, DataTable data, bool readOnly = true)
        {
            BindQueryInternal(grid, data, null, readOnly);
        }

        private static void BindQueryInternal(DataGridView grid, DataTable data, GridColumn[] columns, bool readOnly)
        {
            if (columns != null && columns.Length > 0)
            {
                Bind(grid, data, columns);
            }
            else
            {
                grid.Columns.Clear();
                grid.AutoGenerateColumns = true;
                Bind(grid, data);
                ApplyAutoFormatting(grid, data);
            }

            if (readOnly)
            {
                SetReadOnly(grid);
            }
        }

        private static void ApplyAutoFormatting(DataGridView grid, DataTable data)
        {
            if (grid == null || data == null || grid.Columns.Count == 0)
                return;

            foreach (DataGridViewColumn gCol in grid.Columns)
            {
                string field = !string.IsNullOrEmpty(gCol.DataPropertyName) ? gCol.DataPropertyName : gCol.Name;
                if (string.IsNullOrEmpty(field) || !data.Columns.Contains(field))
                    continue;

                Type colType = data.Columns[field].DataType;
                if (colType == typeof(DateTime))
                {
                    gCol.DefaultCellStyle.Format = "dd/MM/yyyy";
                    gCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                else if (colType == typeof(decimal) || colType == typeof(int) || colType == typeof(long))
                {
                    gCol.DefaultCellStyle.Format = "N0";
                    gCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (colType == typeof(double) || colType == typeof(float))
                {
                    gCol.DefaultCellStyle.Format = "0.##";
                    gCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (colType == typeof(bool))
                {
                    gCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }

            grid.AutoSizeColumnsMode = grid.Columns.Count <= 8
                ? DataGridViewAutoSizeColumnsMode.Fill
                : DataGridViewAutoSizeColumnsMode.DisplayedCells;
        }

        /// <summary>
        /// Gán dữ liệu vào DataGridView và cấu hình các cột hiển thị theo danh sách GridColumn.
        /// Tự động tắt AutoGenerateColumns, xóa cột cũ, và thêm cột mới theo cấu hình.
        /// </summary>
        /// <param name="grid">DataGridView cần gán dữ liệu.</param>
        /// <param name="data">DataTable chứa dữ liệu nguồn.</param>
        /// <param name="columns">Danh sách cấu hình cột cần hiển thị.</param>
        /// <exception cref="ArgumentNullException">Khi grid hoặc data là null.</exception>
        public static void Bind(DataGridView grid, DataTable data, params GridColumn[] columns)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid), "DataGridView không được null.");
            if (data == null)
                throw new ArgumentNullException(nameof(data), "DataTable không được null.");

            if (grid.BindingContext == null)
                grid.BindingContext = new BindingContext();

            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();

            if (columns != null)
            {
                foreach (var col in columns)
                {
                    var dgvCol = new DataGridViewTextBoxColumn
                    {
                        Name = col.DataField,
                        DataPropertyName = col.DataField,
                        HeaderText = col.HeaderText
                    };

                    // Thiết lập chiều rộng cột
                    if (col.Width > 0)
                    {
                        dgvCol.Width = col.Width;
                    }
                    else
                    {
                        dgvCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    }

                    // Áp dụng chuỗi định dạng nếu có
                    if (!string.IsNullOrEmpty(col.Format))
                    {
                        dgvCol.DefaultCellStyle.Format = col.Format;
                    }

                    // Căn lề ô nếu có cấu hình
                    if (col.Alignment.HasValue)
                    {
                        dgvCol.DefaultCellStyle.Alignment = col.Alignment.Value;
                    }

                    grid.Columns.Add(dgvCol);
                }
            }

            grid.DataSource = data;
        }

        private static DataGridViewRow GetActiveRow(DataGridView grid)
        {
            if (grid == null || grid.Rows.Count == 0)
                return null;

            if (grid.SelectedRows.Count > 0)
            {
                var sel = grid.SelectedRows[0];
                return sel.IsNewRow ? null : sel;
            }

            if (grid.IsHandleCreated && grid.SelectionMode == DataGridViewSelectionMode.FullRowSelect)
                return null;

            if (grid.CurrentRow != null && !grid.CurrentRow.IsNewRow)
                return grid.CurrentRow;

            return null;
        }

        private static string FindGridColumnKey(DataGridView grid, string columnName)
        {
            if (grid == null || string.IsNullOrWhiteSpace(columnName))
                return null;
            if (grid.Columns.Contains(columnName))
                return columnName;

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (string.Equals(col.DataPropertyName, columnName, StringComparison.OrdinalIgnoreCase))
                    return col.Name;
            }
            return null;
        }

        /// <summary>
        /// Lấy giá trị từ một cột cụ thể của dòng đang chọn trong DataGridView
        /// (hỗ trợ cả cột ẩn chỉ có trong DataTable nguồn như MaPB, GioiTinh).
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu cần trả về.</typeparam>
        /// <param name="grid">DataGridView cần lấy giá trị.</param>
        /// <param name="columnName">Tên cột (Name, DataPropertyName hoặc tên cột trong DataTable) cần lấy giá trị.</param>
        /// <returns>Giá trị ép kiểu về T, hoặc giá trị mặc định nếu không có dòng nào được chọn.</returns>
        public static T GetSelectedValue<T>(DataGridView grid, string columnName)
        {
            var row = GetActiveRow(grid);
            if (row == null || string.IsNullOrWhiteSpace(columnName))
                return default(T);

            try
            {
                string colKey = FindGridColumnKey(grid, columnName);
                if (colKey != null)
                {
                    object value = row.Cells[colKey].Value;
                    return ExamHelper.Utilities.DataHelper.ChangeType<T>(value);
                }

                if (row.DataBoundItem is DataRowView drv && drv.DataView?.Table?.Columns.Contains(columnName) == true)
                {
                    return ExamHelper.Utilities.DataHelper.ChangeType<T>(drv.Row[columnName]);
                }

                return default(T);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Lấy giá trị từ cột khóa của dòng đang chọn trong DataGridView.
        /// Mặc định lấy từ cột đầu tiên (index 0) hoặc từ cột chỉ định theo tên nếu được cung cấp.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu cần trả về (thường là int hoặc string).</typeparam>
        /// <param name="grid">DataGridView cần lấy giá trị.</param>
        /// <param name="keyColumnName">Tên cột khóa (tùy chọn, nếu null/rỗng sẽ lấy cột đầu tiên).</param>
        /// <returns>Giá trị khóa ép kiểu về T, hoặc giá trị mặc định nếu không có dòng nào được chọn.</returns>
        public static T GetSelectedKeyValue<T>(DataGridView grid, string keyColumnName = null)
        {
            var row = GetActiveRow(grid);
            if (row == null || grid.Columns.Count == 0)
                return default(T);

            try
            {
                if (!string.IsNullOrEmpty(keyColumnName))
                    return GetSelectedValue<T>(grid, keyColumnName);

                object value = row.Cells[0].Value;
                return ExamHelper.Utilities.DataHelper.ChangeType<T>(value);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Lấy DataRowView của dòng đang chọn trong DataGridView.
        /// Hữu ích khi cần truy cập nhiều cột cùng lúc từ dòng được chọn.
        /// </summary>
        /// <param name="grid">DataGridView cần lấy dòng.</param>
        /// <returns>DataRowView của dòng đang chọn, hoặc null nếu không có dòng nào được chọn.</returns>
        public static DataRowView GetSelectedRow(DataGridView grid)
        {
            var row = GetActiveRow(grid);
            if (row == null)
                return null;

            try
            {
                return row.DataBoundItem as DataRowView;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lấy trực tiếp đối tượng DataRow của dòng đang chọn trong DataGridView.
        /// </summary>
        /// <param name="grid">DataGridView cần lấy dòng.</param>
        /// <returns>DataRow của dòng đang chọn, hoặc null nếu không có dòng nào được chọn.</returns>
        public static DataRow GetSelectedDataRow(DataGridView grid)
        {
            return GetSelectedRow(grid)?.Row;
        }

        /// <summary>
        /// Kiểm tra xem DataGridView có dòng nào đang được chọn hay không.
        /// </summary>
        /// <param name="grid">DataGridView cần kiểm tra.</param>
        /// <returns>True nếu có ít nhất một dòng được chọn, ngược lại False.</returns>
        public static bool HasSelection(DataGridView grid)
        {
            return GetActiveRow(grid) != null;
        }

        /// <summary>
        /// Bỏ chọn toàn bộ dòng và xóa CurrentCell trên DataGridView.
        /// Dùng trong nút Làm mới để cho phép click lại dòng đầu tiên kích hoạt SelectionChanged.
        /// </summary>
        public static void ClearSelection(DataGridView grid)
        {
            if (grid == null)
                return;

            grid.ClearSelection();
            try
            {
                grid.CurrentCell = null;
            }
            catch { }
        }

        /// <summary>
        /// Tìm và chọn dòng trên DataGridView theo tên cột và giá trị (rất hữu ích để giữ nguyên vị trí dòng sau khi Sửa).
        /// </summary>
        public static bool SelectRowByValue(DataGridView grid, string columnName, object value)
        {
            if (grid == null || grid.Rows.Count == 0 || string.IsNullOrWhiteSpace(columnName) || value == null)
                return false;

            string colKey = FindGridColumnKey(grid, columnName);
            string target = value.ToString().Trim();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                object rawVal = null;
                if (colKey != null)
                {
                    rawVal = row.Cells[colKey].Value;
                }
                else if (row.DataBoundItem is DataRowView drv && drv.Row.Table.Columns.Contains(columnName))
                {
                    rawVal = drv.Row[columnName];
                }

                string cellVal = rawVal?.ToString()?.Trim();
                if (string.Equals(cellVal, target, StringComparison.OrdinalIgnoreCase))
                {
                    // Đặt CurrentCell TRƯỚC row.Selected để CurrentRow cập nhật đúng trước khi SelectionChanged kích hoạt
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (cell.Visible)
                        {
                            if (grid.CurrentCell != cell)
                                grid.CurrentCell = cell;
                            break;
                        }
                    }

                    if (!row.Selected)
                    {
                        grid.ClearSelection();
                        row.Selected = true;
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Thiết lập DataGridView ở chế độ chỉ đọc với các cài đặt phổ biến cho ứng dụng thi.
        /// Bao gồm: ReadOnly, chọn cả dòng, không cho thêm/xóa dòng, chỉ chọn một dòng.
        /// </summary>
        /// <param name="grid">DataGridView cần thiết lập.</param>
        public static void SetReadOnly(DataGridView grid)
        {
            if (grid == null)
                return;

            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.MultiSelect = false;
        }
    }
}
