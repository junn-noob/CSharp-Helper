using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

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

            grid.DataSource = data;
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

        /// <summary>
        /// Lấy giá trị từ một cột cụ thể của dòng đang chọn trong DataGridView.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu cần trả về.</typeparam>
        /// <param name="grid">DataGridView cần lấy giá trị.</param>
        /// <param name="columnName">Tên cột (DataPropertyName) cần lấy giá trị.</param>
        /// <returns>Giá trị ép kiểu về T, hoặc giá trị mặc định nếu không có dòng nào được chọn.</returns>
        public static T GetSelectedValue<T>(DataGridView grid, string columnName)
        {
            if (grid == null || grid.CurrentRow == null || grid.CurrentRow.IsNewRow || !grid.Columns.Contains(columnName))
                return default(T);

            try
            {
                object value = grid.CurrentRow.Cells[columnName].Value;
                return ExamHelper.Utilities.DataHelper.ChangeType<T>(value);
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
            if (grid == null || grid.CurrentRow == null || grid.CurrentRow.IsNewRow || grid.Columns.Count == 0)
                return default(T);

            try
            {
                object value;
                if (!string.IsNullOrEmpty(keyColumnName) && grid.Columns.Contains(keyColumnName))
                {
                    value = grid.CurrentRow.Cells[keyColumnName].Value;
                }
                else
                {
                    value = grid.CurrentRow.Cells[0].Value;
                }

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
            if (grid == null || grid.CurrentRow == null || grid.CurrentRow.IsNewRow)
                return null;

            try
            {
                return grid.CurrentRow.DataBoundItem as DataRowView;
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
            if (grid == null)
                return false;

            return grid.CurrentRow != null && grid.Rows.Count > 0 && !grid.CurrentRow.IsNewRow;
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
