using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ExamHelper.Binding;
using ExamHelper.Database;
using ExamHelper.Validation;

namespace ExamHelper.Utilities
{
    /// <summary>
    /// Form quản lý CRUD + tìm kiếm + báo cáo tổng quát tự động sinh từ cấu trúc (schema) bất kỳ bảng CSDL nào.
    /// Không phụ thuộc tên bảng hay cột cố định; dùng được ngay khi đổi sang CSDL khác.
    /// </summary>
    public class FormGenericManager : Form
    {
        private readonly string _dbName;
        private readonly Dictionary<string, string> _lookupQueries;
        private readonly Dictionary<string, Control> _fieldControls = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase);

        private string _currentTable;
        private DataTable _schema;
        private DataTable _currentData;
        private List<string> _primaryKeys = new List<string>();
        private bool _isSyncing;

        private ComboBox _cboTables;
        private TextBox _txtSearch;
        private Button _btnSearch;
        private Button _btnReload;
        private Button _btnReport;

        private SplitContainer _splitMain;
        private Panel _fieldsHost;
        private TableLayoutPanel _fieldsLayout;
        private DataGridView _grid;

        private Button _btnAdd;
        private Button _btnUpdate;
        private Button _btnDelete;
        private Button _btnClear;
        private Label _lblStatus;

        /// <summary>Tên bảng đang thao tác.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentTable => _currentTable;

        /// <summary>Cấu trúc cột (schema) của bảng đang mở.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataTable CurrentSchema => _schema;

        /// <summary>Danh sách cột khóa chính được nhận diện từ schema (hoặc cột đầu tiên làm khóa fallback).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<string> PrimaryKeys => _primaryKeys.AsReadOnly();

        /// <summary>Lưới dữ liệu chính của form.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataGridView Grid => _grid;

        /// <summary>Mở nhanh form quản lý tổng quát cho bất kỳ bảng nào trong CSDL.</summary>
        public static DialogResult Open(string tableName = null, string dbName = "default", IWin32Window owner = null)
        {
            return Open(tableName, dbName, owner, null);
        }

        /// <summary>
        /// Mở nhanh form quản lý tổng quát cho bảng CSDL kèm tùy chọn <paramref name="lookupQueries"/>
        /// (tên cột khóa ngoại → câu SQL SELECT Ma, Ten) để tự biến cột đó thành ComboBox chọn tên.
        /// </summary>
        public static DialogResult Open(string tableName, string dbName, IWin32Window owner,
            IDictionary<string, string> lookupQueries)
        {
            using (var form = new FormGenericManager(tableName, dbName, lookupQueries))
            {
                return owner != null ? form.ShowDialog(owner) : form.ShowDialog();
            }
        }

        /// <summary>
        /// Khởi tạo Form quản lý tự động theo schema bảng.
        /// Nếu <paramref name="tableName"/> để trống, Form tự đọc danh sách bảng từ CSDL và mở bảng đầu tiên.
        /// </summary>
        public FormGenericManager(string tableName = null, string dbName = "default",
            IDictionary<string, string> lookupQueries = null)
        {
            _dbName = string.IsNullOrWhiteSpace(dbName) ? "default" : dbName;
            _lookupQueries = lookupQueries != null
                ? new Dictionary<string, string>(lookupQueries, StringComparer.OrdinalIgnoreCase)
                : null;
            InitializeUi();
            LoadTableList(tableName);
        }

        /// <summary>Tìm control nhập liệu được sinh tự động theo tên cột.</summary>
        public Control FindFieldControl(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return null;
            return _fieldControls.TryGetValue(columnName, out Control ctrl) ? ctrl : null;
        }

        /// <summary>Chuyển sang quản lý một bảng khác trong cùng CSDL và dựng lại các ô nhập theo schema mới.</summary>
        public bool SelectTable(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            EnsureTableInDropdown(tableName);
            _currentTable = tableName;
            Text = $"Quản lý dữ liệu - {tableName}";
            _txtSearch.Text = "";

            BuildSchemaFields(tableName);
            ReloadData();
            return _schema != null && _schema.Columns.Count > 0;
        }

        /// <summary>Tải lại dữ liệu từ CSDL vào lưới (có áp dụng từ khóa tìm kiếm nếu có).</summary>
        public void ReloadData()
        {
            if (string.IsNullOrWhiteSpace(_currentTable))
            {
                _currentData = new DataTable();
                GridHelper.BindFormatted(_grid, _currentData);
                UpdateStatus("Chưa có bảng nào được chọn.");
                return;
            }

            try
            {
                string quotedTable = DbHelper.QuoteIdentifier(_currentTable, _dbName);
                string sql = $"SELECT * FROM {quotedTable}";
                FilterBuilder filter = BuildSearchFilter(_txtSearch.Text);
                string orderBy = _primaryKeys.Count > 0 ? DbHelper.QuoteIdentifier(_primaryKeys[0], _dbName) : null;

                _currentData = DbHelper.GetDataTable(sql, filter, orderBy, _dbName);
                GridHelper.BindFormatted(_grid, _currentData, readOnly: true);

                if (DbHelper.HasError)
                {
                    UpdateStatus("Lỗi tải dữ liệu: " + DbHelper.LastError.Message);
                    return;
                }

                string pkLabel = _primaryKeys.Count > 0 ? string.Join(", ", _primaryKeys) : "(không rõ PK)";
                UpdateStatus($"Bảng: {_currentTable} | Khóa: {pkLabel} | {_currentData.Rows.Count} dòng");
                ClearInputs();
            }
            catch (Exception ex)
            {
                UpdateStatus("Lỗi: " + ex.Message);
            }
        }

        private void InitializeUi()
        {
            Text = "Quản lý dữ liệu tổng quát";
            Size = new Size(1080, 680);
            MinimumSize = new Size(820, 520);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            // 1. Thanh công cụ trên cùng
            var topBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(10, 7, 10, 5)
            };

            topBar.Controls.Add(new Label { Text = "Bảng:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            _cboTables = new ComboBox
            {
                Width = 190,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 2, 12, 0)
            };
            _cboTables.SelectedIndexChanged += (s, e) =>
            {
                if (_isSyncing || _cboTables.SelectedItem == null) return;
                SelectTable(_cboTables.SelectedItem.ToString());
            };
            topBar.Controls.Add(_cboTables);

            topBar.Controls.Add(new Label { Text = "Tìm nhanh:", AutoSize = true, Margin = new Padding(4, 6, 4, 0) });
            _txtSearch = new TextBox { Width = 220, Margin = new Padding(0, 2, 6, 0) };
            _txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ReloadData();
                }
            };
            topBar.Controls.Add(_txtSearch);

            _btnSearch = new Button { Text = "Tìm", Width = 72, Height = 27, Margin = new Padding(0, 1, 6, 0) };
            _btnSearch.Click += (s, e) => ReloadData();
            topBar.Controls.Add(_btnSearch);

            _btnReload = new Button { Text = "↻ Tải lại", Width = 86, Height = 27, Margin = new Padding(0, 1, 10, 0) };
            _btnReload.Click += (s, e) =>
            {
                _txtSearch.Text = "";
                ReloadData();
            };
            topBar.Controls.Add(_btnReload);

            _btnReport = new Button
            {
                Text = "Xuất báo cáo",
                Width = 128,
                Height = 27,
                Margin = new Padding(0, 1, 0, 0)
            };
            _btnReport.Click += (s, e) => OpenReportForCurrentData();
            topBar.Controls.Add(_btnReport);

            // 2. Thanh nút thao tác dưới cùng
            var bottomBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(10, 8, 10, 6)
            };

            _btnAdd = new Button { Text = "+ Thêm mới", Width = 104, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnAdd.Click += (s, e) => HandleInsert();

            _btnUpdate = new Button { Text = "✎ Cập nhật", Width = 104, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnUpdate.Click += (s, e) => HandleUpdate();

            _btnDelete = new Button { Text = "✕ Xóa", Width = 86, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnDelete.Click += (s, e) => HandleDelete();

            _btnClear = new Button { Text = "↺ Nhập lại", Width = 96, Height = 30, Margin = new Padding(0, 0, 16, 0) };
            _btnClear.Click += (s, e) => ClearInputs();

            _lblStatus = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(51, 65, 85),
                Margin = new Padding(4, 6, 0, 0)
            };

            bottomBar.Controls.AddRange(new Control[] { _btnAdd, _btnUpdate, _btnDelete, _btnClear, _lblStatus });

            // 3. Khu vực chính: Form nhập sinh tự động (trái) + Lưới dữ liệu (phải)
            _splitMain = new SafeSplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Width = 1080,
                Panel1MinSize = 260,
                Panel2MinSize = 300,
                SplitterDistance = 350
            };

            _fieldsHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.White
            };
            _splitMain.Panel1.Controls.Add(_fieldsHost);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White
            };
            _grid.CellClick += (s, e) => LoadSelectedRowToInputs();
            _grid.SelectionChanged += (s, e) => LoadSelectedRowToInputs();
            _splitMain.Panel2.Controls.Add(_grid);

            Controls.Add(_splitMain);
            Controls.Add(topBar);
            Controls.Add(bottomBar);
        }

        private void LoadTableList(string preferredTable)
        {
            _isSyncing = true;
            try
            {
                _cboTables.Items.Clear();
                List<string> tables = DbHelper.GetTableNames(_dbName);
                foreach (string tbl in tables)
                    _cboTables.Items.Add(tbl);

                if (!string.IsNullOrWhiteSpace(preferredTable))
                {
                    EnsureTableInDropdown(preferredTable);
                }
                else if (_cboTables.Items.Count > 0)
                {
                    _cboTables.SelectedIndex = 0;
                    preferredTable = _cboTables.SelectedItem.ToString();
                }
            }
            finally
            {
                _isSyncing = false;
            }

            if (!string.IsNullOrWhiteSpace(preferredTable))
            {
                SelectTable(preferredTable);
            }
            else
            {
                UpdateStatus(DbHelper.HasError
                    ? "Không đọc được danh sách bảng: " + DbHelper.LastError.Message
                    : "CSDL hiện không có bảng nào.");
            }
        }

        private void EnsureTableInDropdown(string tableName)
        {
            _isSyncing = true;
            try
            {
                int foundIdx = -1;
                for (int i = 0; i < _cboTables.Items.Count; i++)
                {
                    if (string.Equals(_cboTables.Items[i].ToString(), tableName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundIdx = i;
                        break;
                    }
                }
                if (foundIdx < 0)
                    foundIdx = _cboTables.Items.Add(tableName);
                _cboTables.SelectedIndex = foundIdx;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void BuildSchemaFields(string tableName)
        {
            _fieldControls.Clear();
            _primaryKeys.Clear();

            _fieldsHost.SuspendLayout();
            try
            {
                for (int i = _fieldsHost.Controls.Count - 1; i >= 0; i--)
                {
                    Control old = _fieldsHost.Controls[i];
                    _fieldsHost.Controls.RemoveAt(i);
                    old.Dispose();
                }

                _schema = DbHelper.GetTableSchema(tableName, _dbName);
                if (_schema == null || _schema.Columns.Count == 0)
                    return;

                foreach (DataColumn pk in _schema.PrimaryKey)
                    _primaryKeys.Add(pk.ColumnName);
                if (_primaryKeys.Count == 0 && _schema.Columns.Count > 0)
                    _primaryKeys.Add(_schema.Columns[0].ColumnName);

                _fieldsLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    ColumnCount = 2,
                    Padding = new Padding(2)
                };
                _fieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
                _fieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));

                int rowIdx = 0;
                foreach (DataColumn dc in _schema.Columns)
                {
                    if (dc.DataType == typeof(byte[]))
                        continue;

                    string caption = dc.ColumnName;
                    if (dc.AutoIncrement) caption += " (tự tăng)";
                    else if (!dc.AllowDBNull) caption += " *";

                    var lbl = new Label
                    {
                        Text = caption,
                        AutoSize = true,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right,
                        Margin = new Padding(0, 7, 6, 4)
                    };

                    Control input = CreateEditorForColumn(dc);
                    input.Name = dc.ColumnName;
                    input.Tag = dc.ColumnName;
                    input.Margin = new Padding(0, 3, 0, 5);

                    _fieldsLayout.RowCount = rowIdx + 1;
                    _fieldsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    _fieldsLayout.Controls.Add(lbl, 0, rowIdx);
                    _fieldsLayout.Controls.Add(input, 1, rowIdx);
                    _fieldControls[dc.ColumnName] = input;
                    rowIdx++;
                }

                _fieldsHost.Controls.Add(_fieldsLayout);
            }
            finally
            {
                _fieldsHost.ResumeLayout(true);
            }
        }

        private Control CreateEditorForColumn(DataColumn dc)
        {
            if (!dc.AutoIncrement && _lookupQueries != null &&
                _lookupQueries.TryGetValue(dc.ColumnName, out string lookupSql) &&
                !string.IsNullOrWhiteSpace(lookupSql))
            {
                var cbo = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                DataTable lookupDt = DbHelper.GetDataTable(lookupSql, _dbName);
                if (lookupDt != null && lookupDt.Columns.Count >= 2)
                {
                    string valCol = lookupDt.Columns.Contains(dc.ColumnName) ? dc.ColumnName : lookupDt.Columns[0].ColumnName;
                    string dispCol = lookupDt.Columns.Count > 1 && !string.Equals(lookupDt.Columns[1].ColumnName, valCol, StringComparison.OrdinalIgnoreCase)
                        ? lookupDt.Columns[1].ColumnName
                        : lookupDt.Columns[0].ColumnName;
                    ComboBoxHelper.Bind(cbo, lookupDt, valCol, dispCol);
                }
                return cbo;
            }

            if (dc.DataType == typeof(bool))
            {
                return new CheckBox
                {
                    Text = "",
                    AutoSize = true,
                    Anchor = AnchorStyles.Left
                };
            }

            if (dc.DataType == typeof(DateTime))
            {
                return new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    ShowCheckBox = dc.AllowDBNull,
                    Checked = !dc.AllowDBNull,
                    Dock = DockStyle.Fill
                };
            }

            var txt = new TextBox { Dock = DockStyle.Fill };
            if (dc.DataType == typeof(string) && dc.MaxLength > 0)
            {
                txt.MaxLength = dc.MaxLength;
            }
            if (dc.AutoIncrement)
            {
                txt.ReadOnly = true;
                txt.BackColor = SystemColors.Control;
            }
            return txt;
        }

        private FilterBuilder BuildSearchFilter(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword) || _schema == null)
                return null;

            string trimmed = keyword.Trim();
            var fb = FilterBuilder.ForConnection(_dbName);
            bool hasClause = false;

            fb.Or(or =>
            {
                foreach (DataColumn dc in _schema.Columns)
                {
                    if (dc.DataType == typeof(string))
                    {
                        or.Contains(dc.ColumnName, trimmed);
                        hasClause = true;
                    }
                    else if ((dc.DataType == typeof(int) || dc.DataType == typeof(long) || dc.DataType == typeof(short))
                             && long.TryParse(trimmed, out long intVal))
                    {
                        or.Equal(dc.ColumnName, intVal);
                        hasClause = true;
                    }
                    else if ((dc.DataType == typeof(decimal) || dc.DataType == typeof(double) || dc.DataType == typeof(float))
                             && DataHelper.TryParseDecimal(trimmed, out decimal decVal))
                    {
                        or.Equal(dc.ColumnName, decVal);
                        hasClause = true;
                    }
                }
            });

            return hasClause && fb.HasConditions ? fb : null;
        }

        private void LoadSelectedRowToInputs()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.IsNewRow)
                return;

            if (_grid.CurrentRow.DataBoundItem is DataRowView drv)
            {
                FormMapper.AutoLoad(_fieldsHost, drv.Row);
            }
        }

        private void ClearInputs()
        {
            FormMapper.AutoClear(_fieldsHost);
            if (_grid != null) GridHelper.ClearSelection(_grid);
        }

        private Dictionary<string, object> CollectEditableValues(bool forInsert)
        {
            var rawValues = FormMapper.AutoGetValues(_fieldsHost, _schema);
            if (_schema == null) return rawValues;

            var cleaned = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in rawValues)
            {
                if (!_schema.Columns.Contains(kvp.Key)) continue;
                DataColumn dc = _schema.Columns[kvp.Key];
                if (dc.AutoIncrement || dc.ReadOnly) continue;
                if (dc.DataType == typeof(byte[])) continue;

                object val = kvp.Value;
                if (val == null || (val is string s && string.IsNullOrWhiteSpace(s) && dc.AllowDBNull))
                    val = DBNull.Value;

                cleaned[kvp.Key] = val;
            }
            return cleaned;
        }

        private Dictionary<string, object> GetSelectedPrimaryKeys()
        {
            var keys = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (string pk in _primaryKeys)
            {
                object val = GridHelper.GetSelectedValue<object>(_grid, pk);
                if (val == null || val == DBNull.Value)
                    return new Dictionary<string, object>();
                keys[pk] = val;
            }
            return keys;
        }

        private void HandleInsert()
        {
            if (string.IsNullOrWhiteSpace(_currentTable) || _schema == null) return;

            var valid = Validator.FromSchema(_fieldsHost, _schema);
            if (!valid.IsValid)
            {
                ShowValidationError(valid);
                return;
            }

            Dictionary<string, object> values = CollectEditableValues(forInsert: true);
            if (values.Count == 0)
            {
                MessageHelper.Warning("Không có dữ liệu hợp lệ để thêm mới.");
                return;
            }

            bool allKeysProvided = _primaryKeys.Count > 0 && _primaryKeys.All(pk =>
                _schema.Columns.Contains(pk) && !_schema.Columns[pk].AutoIncrement && values.ContainsKey(pk));
            if (allKeysProvided)
            {
                var keyCheck = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (string pk in _primaryKeys) keyCheck[pk] = values[pk];

                if (CrudHelper.Exists(_currentTable, keyCheck, _dbName))
                {
                    MessageHelper.Warning("Khóa chính đã tồn tại trong bảng!");
                    return;
                }
            }

            int affected = CrudHelper.Insert(_currentTable, values, _dbName);
            if (DbHelper.HasError)
            {
                MessageHelper.Error("Không thể thêm mới: " + DbHelper.LastError.Message);
                return;
            }

            if (affected > 0)
            {
                MessageHelper.Success("thêm mới");
                ReloadData();
            }
        }

        private void HandleUpdate()
        {
            if (string.IsNullOrWhiteSpace(_currentTable) || _schema == null) return;

            Dictionary<string, object> keys = GetSelectedPrimaryKeys();
            if (keys.Count == 0)
            {
                MessageHelper.Warning("Vui lòng chọn một dòng trên lưới để cập nhật.");
                return;
            }

            var valid = Validator.FromSchema(_fieldsHost, _schema);
            if (!valid.IsValid)
            {
                ShowValidationError(valid);
                return;
            }

            Dictionary<string, object> values = CollectEditableValues(forInsert: false);
            int affected = keys.Count == 1
                ? CrudHelper.Update(_currentTable, values, _primaryKeys[0], keys[_primaryKeys[0]], _dbName)
                : CrudHelper.Update(_currentTable, values, keys, _dbName);

            if (DbHelper.HasError)
            {
                MessageHelper.Error("Không thể cập nhật: " + DbHelper.LastError.Message);
                return;
            }

            if (affected >= 0)
            {
                MessageHelper.Success("cập nhật");
                ReloadData();
            }
        }

        private void HandleDelete()
        {
            if (string.IsNullOrWhiteSpace(_currentTable) || _schema == null) return;

            Dictionary<string, object> keys = GetSelectedPrimaryKeys();
            if (keys.Count == 0)
            {
                MessageHelper.Warning("Vui lòng chọn một dòng trên lưới để xóa.");
                return;
            }

            string keySummary = string.Join(", ", keys.Select(k => $"{k.Key}={k.Value}"));
            if (!MessageHelper.Confirm($"Bạn có chắc muốn xóa bản ghi ({keySummary})?"))
                return;

            int affected = keys.Count == 1
                ? CrudHelper.Delete(_currentTable, _primaryKeys[0], keys[_primaryKeys[0]], _dbName)
                : CrudHelper.Delete(_currentTable, keys, _dbName);

            if (DbHelper.HasError)
            {
                MessageHelper.Error("Không thể xóa (có thể bản ghi đang được bảng khác tham chiếu): " + DbHelper.LastError.Message);
                return;
            }

            if (affected > 0)
            {
                MessageHelper.Success("xóa");
                ReloadData();
            }
        }

        private void OpenReportForCurrentData()
        {
            if (_currentData == null || _currentData.Columns.Count == 0)
            {
                MessageHelper.Warning("Chưa có dữ liệu để xuất báo cáo.");
                return;
            }

            string filterDesc = string.IsNullOrWhiteSpace(_txtSearch.Text)
                ? null
                : $"Từ khóa: {_txtSearch.Text.Trim()}";
            string title = $"BÁO CÁO {(_currentTable ?? "DỮ LIỆU").ToUpperInvariant()}";

            ReportHelper.ShowReport(_grid, title, filterDesc, owner: this);
        }

        private static void ShowValidationError(ValidationResult valid)
        {
            MessageHelper.Warning(valid.ErrorMessage);
            valid.ErrorControl?.Focus();
        }

        private void UpdateStatus(string text)
        {
            if (_lblStatus != null)
                _lblStatus.Text = text ?? "";
        }
    }
}
