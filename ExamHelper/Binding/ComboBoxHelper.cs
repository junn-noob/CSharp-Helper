using System;
using System.Data;
using System.Windows.Forms;
using ExamHelper.Database;

namespace ExamHelper.Binding
{
    /// <summary>
    /// Lớp tiện ích hỗ trợ binding dữ liệu cho ComboBox trong các ứng dụng WinForms.
    /// Cung cấp các phương thức bind dữ liệu, thêm dòng "Tất cả", và truy xuất giá trị đã chọn.
    /// </summary>
    public static class ComboBoxHelper
    {
        /// <summary>
        /// Gán dữ liệu từ DataTable vào ComboBox.
        /// Tự động tạo bản sao DataTable để tránh lỗi chia sẻ tham chiếu giữa nhiều ComboBox.
        /// </summary>
        /// <param name="combo">ComboBox cần gán dữ liệu.</param>
        /// <param name="data">DataTable chứa dữ liệu nguồn.</param>
        /// <param name="valueMember">Tên cột chứa giá trị (Value).</param>
        /// <param name="displayMember">Tên cột hiển thị (Display).</param>
        /// <exception cref="ArgumentNullException">Khi combo hoặc data là null.</exception>
        public static void Bind(ComboBox combo, DataTable data, string valueMember, string displayMember)
        {
            if (combo == null)
                throw new ArgumentNullException(nameof(combo), "ComboBox không được null.");
            if (data == null)
                throw new ArgumentNullException(nameof(data), "DataTable không được null.");

            // Nếu DataTable hoàn toàn trống cột (thường do truy vấn SQL bị lỗi), tránh làm sập Form_Load
            if (data.Columns.Count == 0)
            {
                combo.DataSource = null;
                combo.Items.Clear();
                return;
            }

            if (!data.Columns.Contains(valueMember))
                throw new ArgumentException($"Cột valueMember '{valueMember}' không tồn tại trong DataTable.");
            if (!data.Columns.Contains(displayMember))
                throw new ArgumentException($"Cột displayMember '{displayMember}' không tồn tại trong DataTable.");

            ApplyBinding(combo, data.Copy(), valueMember, displayMember);
        }

        private static void ResolveDefaultColumns(DataTable data, ref string valueMember, ref string displayMember)
        {
            if (string.IsNullOrWhiteSpace(valueMember))
                valueMember = data.Columns.Count > 0 ? data.Columns[0].ColumnName : "Id";
            if (string.IsNullOrWhiteSpace(displayMember))
                displayMember = data.Columns.Count >= 2 ? data.Columns[1].ColumnName : valueMember;
        }

        /// <summary>
        /// Gán dữ liệu từ DataTable vào ComboBox, tự nhận diện cột 1 làm ValueMember và cột 2 làm DisplayMember
        /// (nếu bảng chỉ có 1 cột thì dùng chính cột đó cho cả hai).
        /// </summary>
        public static void Bind(ComboBox combo, DataTable data)
        {
            if (combo == null)
                throw new ArgumentNullException(nameof(combo), "ComboBox không được null.");
            if (data == null)
                throw new ArgumentNullException(nameof(data), "DataTable không được null.");

            if (data.Columns.Count == 0)
            {
                combo.DataSource = null;
                combo.Items.Clear();
                return;
            }

            string valCol = null;
            string dispCol = null;
            ResolveDefaultColumns(data, ref valCol, ref dispCol);
            Bind(combo, data, valCol, dispCol);
        }

        /// <summary>
        /// Thực thi trực tiếp câu lệnh SQL và nạp dữ liệu vào ComboBox.
        /// Nếu bỏ trống valueMember/displayMember sẽ tự lấy cột 1 làm Value và cột 2 làm Display.
        /// </summary>
        public static void Bind(ComboBox combo, string sql, string valueMember = null, string displayMember = null, string dbName = "default")
        {
            DataTable dt = DbHelper.GetDataTable(sql, dbName);
            if (!string.IsNullOrWhiteSpace(valueMember) && !string.IsNullOrWhiteSpace(displayMember))
                Bind(combo, dt, valueMember, displayMember);
            else
                Bind(combo, dt);
        }

        /// <summary>
        /// Thực thi trực tiếp câu lệnh SQL và nạp vào ComboBox kèm dòng "Tất cả" ở đầu danh sách.
        /// Nếu bỏ trống valueMember/displayMember sẽ tự lấy cột 1 làm Value và cột 2 làm Display.
        /// </summary>
        public static void BindWithAll(ComboBox combo, string sql, string allText = "-- Tất cả --", object allValue = null,
            string valueMember = null, string displayMember = null, string dbName = "default")
        {
            DataTable dt = DbHelper.GetDataTable(sql, dbName);
            ResolveDefaultColumns(dt, ref valueMember, ref displayMember);
            BindWithAll(combo, dt, valueMember, displayMember, allText, allValue);
        }

        /// <summary>
        /// Gán dữ liệu vào ComboBox kèm thêm dòng "Tất cả" ở đầu danh sách.
        /// Hữu ích cho các ComboBox dùng làm bộ lọc tìm kiếm trong đề thi.
        /// </summary>
        /// <param name="combo">ComboBox cần gán dữ liệu.</param>
        /// <param name="data">DataTable chứa dữ liệu nguồn.</param>
        /// <param name="valueMember">Tên cột chứa giá trị (Value).</param>
        /// <param name="displayMember">Tên cột hiển thị (Display).</param>
        /// <param name="allText">Văn bản hiển thị cho dòng "Tất cả". Mặc định: "-- Tất cả --".</param>
        /// <param name="allValue">Giá trị của dòng "Tất cả". Nếu null, tự động phát hiện: int → 0, chuỗi → "".</param>
        /// <exception cref="ArgumentNullException">Khi combo hoặc data là null.</exception>
        public static void BindWithAll(ComboBox combo, DataTable data, string valueMember, string displayMember,
            string allText = "-- Tất cả --", object allValue = null)
        {
            if (combo == null)
                throw new ArgumentNullException(nameof(combo), "ComboBox không được null.");
            if (data == null)
                throw new ArgumentNullException(nameof(data), "DataTable không được null.");

            DataTable clone;
            if (data.Columns.Count == 0)
            {
                // Nếu DataTable trống cột do lỗi truy vấn, tạo bảng tối thiểu chỉ chứa dòng "Tất cả" để Form không bị crash
                clone = new DataTable();
                clone.Columns.Add(valueMember, typeof(string));
                if (!string.Equals(displayMember, valueMember, StringComparison.OrdinalIgnoreCase))
                    clone.Columns.Add(displayMember, typeof(string));
            }
            else
            {
                if (!data.Columns.Contains(valueMember))
                    throw new ArgumentException($"Cột valueMember '{valueMember}' không tồn tại trong DataTable.");
                if (!data.Columns.Contains(displayMember))
                    throw new ArgumentException($"Cột displayMember '{displayMember}' không tồn tại trong DataTable.");

                clone = data.Copy();
            }

            // Cho phép tất cả các cột trong bản sao nhận DBNull để tránh NoNullAllowedException khi tạo dòng "Tất cả"
            foreach (DataColumn col in clone.Columns)
            {
                col.AllowDBNull = true;
            }

            // Tự động xác định giá trị cho dòng "Tất cả" nếu chưa chỉ định
            if (allValue == null)
            {
                Type colType = clone.Columns[valueMember].DataType;
                if (colType == typeof(int) || colType == typeof(long) || colType == typeof(short)
                    || colType == typeof(byte) || colType == typeof(decimal) || colType == typeof(double)
                    || colType == typeof(float))
                {
                    allValue = Convert.ChangeType(0, colType);
                }
                else if (colType == typeof(Guid))
                {
                    allValue = Guid.Empty;
                }
                else if (colType == typeof(DateTime))
                {
                    allValue = DateTime.MinValue;
                }
                else
                {
                    allValue = "";
                }
            }

            // Chèn dòng "Tất cả" vào đầu danh sách
            DataRow allRow = clone.NewRow();
            allRow[valueMember] = allValue;
            allRow[displayMember] = allText;
            clone.Rows.InsertAt(allRow, 0);

            ApplyBinding(combo, clone, valueMember, displayMember);
        }

        /// <summary>
        /// Nạp danh sách các mục tĩnh vào ComboBox (ví dụ: "Tất cả", "Nam", "Nữ") và chọn sẵn mục đầu tiên.
        /// </summary>
        public static void BindItems(ComboBox combo, params object[] items)
        {
            if (combo == null)
                throw new ArgumentNullException(nameof(combo), "ComboBox không được null.");

            combo.DataSource = null;
            combo.Items.Clear();
            if (items != null && items.Length > 0)
            {
                combo.Items.AddRange(items);
                combo.SelectedIndex = 0;
            }
        }

        private static void ApplyBinding(ComboBox combo, DataTable source, string valueMember, string displayMember)
        {
            if (combo.BindingContext == null)
            {
                combo.BindingContext = new BindingContext();
            }

            // Thiết lập DisplayMember & ValueMember TRƯỚC DataSource
            combo.DisplayMember = displayMember;
            combo.ValueMember = valueMember;
            combo.DataSource = source;

            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        /// <summary>
        /// Lấy giá trị đã chọn trong ComboBox và ép kiểu về kiểu T.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu cần trả về (int, string, Guid,...).</typeparam>
        /// <param name="combo">ComboBox cần lấy giá trị.</param>
        /// <returns>Giá trị đã chọn được ép kiểu về T, hoặc giá trị mặc định nếu chưa chọn.</returns>
        public static T GetSelectedValue<T>(ComboBox combo)
        {
            if (combo == null || combo.SelectedValue == null)
                return default(T);

            return ExamHelper.Utilities.DataHelper.ChangeType<T>(combo.SelectedValue);
        }

        /// <summary>
        /// Lấy văn bản đang hiển thị trên ComboBox (DisplayMember của mục đang chọn).
        /// </summary>
        /// <param name="combo">ComboBox cần lấy văn bản.</param>
        /// <returns>Chuỗi văn bản hiển thị, hoặc chuỗi rỗng nếu chưa chọn.</returns>
        public static string GetSelectedText(ComboBox combo)
        {
            if (combo == null)
                return string.Empty;

            return combo.Text ?? string.Empty;
        }

        /// <summary>
        /// Kiểm tra xem ComboBox có đang chọn dòng "Tất cả" hay không.
        /// Chỉ có ý nghĩa với ComboBox được bind bằng <see cref="BindWithAll(ComboBox, DataTable, string, string, string, object)"/>.
        /// </summary>
        /// <param name="combo">ComboBox cần kiểm tra.</param>
        /// <returns>True nếu đang chọn dòng đầu tiên (dòng "Tất cả"), ngược lại False.</returns>
        public static bool IsAllSelected(ComboBox combo)
        {
            if (combo == null)
                return false;

            return combo.SelectedIndex == 0;
        }

        /// <summary>
        /// Chọn mục trong ComboBox theo giá trị (ValueMember).
        /// Tự động chuyển đổi kiểu dữ liệu để đảm bảo khớp với DataType của cột khóa.
        /// </summary>
        /// <param name="combo">ComboBox cần thiết lập giá trị.</param>
        /// <param name="value">Giá trị cần chọn.</param>
        public static void SelectByValue(ComboBox combo, object value)
        {
            if (combo == null || value == null || value == DBNull.Value)
                return;

            try
            {
                if (combo.DataSource is DataTable dt && !string.IsNullOrEmpty(combo.ValueMember) && dt.Columns.Contains(combo.ValueMember))
                {
                    Type colType = dt.Columns[combo.ValueMember].DataType;
                    combo.SelectedValue = Convert.ChangeType(value, colType);

                    // Fallback cho cột CHAR/NCHAR có khoảng trắng thừa hoặc khác chữ hoa/thường
                    if (colType == typeof(string) && (combo.SelectedIndex < 0 || combo.SelectedValue == null))
                    {
                        string target = value.ToString().Trim();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string rowVal = dt.Rows[i][combo.ValueMember]?.ToString()?.Trim();
                            if (string.Equals(rowVal, target, StringComparison.OrdinalIgnoreCase))
                            {
                                combo.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    return;
                }
            }
            catch { }

            try
            {
                combo.SelectedValue = value;
            }
            catch { }
        }

        /// <summary>
        /// Chọn mục trong ComboBox theo chuỗi hiển thị (DisplayMember hoặc Items Text), bỏ qua khoảng trắng thừa và hoa/thường.
        /// </summary>
        public static bool SelectByText(ComboBox combo, string text)
        {
            if (combo == null || string.IsNullOrWhiteSpace(text))
                return false;

            string target = text.Trim();

            if (combo.DataSource is DataTable dt && !string.IsNullOrEmpty(combo.DisplayMember) && dt.Columns.Contains(combo.DisplayMember))
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string displayVal = dt.Rows[i][combo.DisplayMember]?.ToString()?.Trim();
                    if (string.Equals(displayVal, target, StringComparison.OrdinalIgnoreCase))
                    {
                        combo.SelectedIndex = i;
                        return true;
                    }
                }
            }

            for (int i = 0; i < combo.Items.Count; i++)
            {
                string itemText = combo.GetItemText(combo.Items[i])?.Trim();
                if (string.Equals(itemText, target, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Chọn mục đầu tiên trong ComboBox.
        /// </summary>
        /// <param name="combo">ComboBox cần thiết lập.</param>
        public static void SelectFirst(ComboBox combo)
        {
            if (combo == null)
                return;

            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }
    }
}
