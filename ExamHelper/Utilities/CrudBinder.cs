using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using ExamHelper.Binding;
using ExamHelper.Database;
using ExamHelper.Validation;

namespace ExamHelper.Utilities
{
    /// <summary>
    /// Trình điều phối Form CRUD chuẩn (Form 1) giúp nối toàn bộ ComboBox, DataGridView, SelectionChanged,
    /// Validation, Thêm/Sửa/Xóa/Làm mới/Thoát chỉ trong một khối cấu hình ngắn gọn.
    /// </summary>
    public sealed class CrudBinder
    {
        private sealed class ComboBindingSpec
        {
            public ComboBox Combo;
            public string Sql;
            public DataTable Table;
            public string ValueMember;
            public string DisplayMember;
        }

        private readonly Form _form;
        private readonly Control _container;
        private readonly string _tableName;
        private readonly string _keyColumn;
        private readonly Control _keyControl;
        private readonly DataGridView _grid;
        private readonly string _entityLabel;
        private readonly string _dbName;

        private readonly List<ComboBindingSpec> _lookups = new List<ComboBindingSpec>();
        private RadioButton[] _defaultRadios = Array.Empty<RadioButton>();

        private string _selectSql;
        private GridHelper.GridColumn[] _gridColumns;
        private string _orderBy;

        private bool _useSchemaValidation = true;
        private Func<ValidationRule[]> _extraRulesFactory;
        private Func<ValidationResult> _customValidator;
        private Action<Dictionary<string, object>, bool> _beforeSave;
        private Action<string, object> _afterSaved;
        private bool _resetAfterInsert;
        private string _keyDisplayName;
        private string _exitConfirmMessage = "Bạn có chắc chắn muốn thoát không?";

        private Button _btnAdd;
        private Button _btnUpdate;
        private Button _btnDelete;
        private Button _btnReset;
        private Button _btnExit;

        private DataTable _schema;
        private DataTable _currentData;
        private bool _started;
        private bool _suppressSelection;

        /// <summary>Schema bảng gốc đã nạp từ CSDL (dùng cho AutoGetValues và FromSchema).</summary>
        public DataTable Schema => _schema;

        /// <summary>DataTable dữ liệu mới nhất đang hiển thị trên DataGridView.</summary>
        public DataTable CurrentData => _currentData;

        private CrudBinder(Form form, Control container, string tableName, string keyColumn,
            Control keyControl, DataGridView grid, string entityLabel, string dbName)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _container = container ?? form;
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Tên bảng không được để trống.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(keyColumn))
                throw new ArgumentException("Tên cột khóa chính không được để trống.", nameof(keyColumn));

            _tableName = tableName.Trim();
            _keyColumn = keyColumn.Trim();
            _keyControl = keyControl;
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _entityLabel = string.IsNullOrWhiteSpace(entityLabel) ? "bản ghi" : entityLabel.Trim();
            _dbName = string.IsNullOrWhiteSpace(dbName) ? "default" : dbName;
            _keyDisplayName = _keyColumn;
        }

        /// <summary>Khởi tạo bộ điều phối CRUD cho một Form chuẩn.</summary>
        public static CrudBinder For(Form form, string tableName, string keyColumn,
            Control keyControl, DataGridView grid, string entityLabel = "bản ghi", string dbName = "default")
        {
            return new CrudBinder(form, form, tableName, keyColumn, keyControl, grid, entityLabel, dbName);
        }

        /// <summary>Khởi tạo bộ điều phối CRUD với vùng chứa control nhập liệu cụ thể (GroupBox/Panel).</summary>
        public static CrudBinder For(Form form, Control inputContainer, string tableName, string keyColumn,
            Control keyControl, DataGridView grid, string entityLabel = "bản ghi", string dbName = "default")
        {
            return new CrudBinder(form, inputContainer, tableName, keyColumn, keyControl, grid, entityLabel, dbName);
        }

        /// <summary>Đăng ký ComboBox danh mục nạp tự động từ SQL trước khi load Grid.</summary>
        public CrudBinder WithCombo(ComboBox combo, string sql, string valueMember = null, string displayMember = null)
        {
            if (combo != null && !string.IsNullOrWhiteSpace(sql))
            {
                _lookups.Add(new ComboBindingSpec
                {
                    Combo = combo,
                    Sql = sql,
                    ValueMember = valueMember,
                    DisplayMember = displayMember
                });
            }
            return this;
        }

        /// <summary>Đăng ký ComboBox danh mục nạp từ DataTable có sẵn.</summary>
        public CrudBinder WithCombo(ComboBox combo, DataTable table, string valueMember = null, string displayMember = null)
        {
            if (combo != null && table != null)
            {
                _lookups.Add(new ComboBindingSpec
                {
                    Combo = combo,
                    Table = table,
                    ValueMember = valueMember,
                    DisplayMember = displayMember
                });
            }
            return this;
        }

        /// <summary>Cấu hình câu truy vấn SELECT (kèm JOIN/cột tính toán) và các cột hiển thị trên DataGridView.</summary>
        public CrudBinder WithQuery(string selectSql, params GridHelper.GridColumn[] columns)
        {
            _selectSql = selectSql;
            _gridColumns = columns;
            return this;
        }

        /// <summary>Dùng khi bảng đơn giản không cần câu JOIN (tự SELECT * FROM bảng).</summary>
        public CrudBinder WithTable(string orderBy = null, params GridHelper.GridColumn[] columns)
        {
            _selectSql = null;
            _orderBy = orderBy;
            _gridColumns = columns;
            return this;
        }

        /// <summary>Đặt các RadioButton mặc định được tick lại khi bấm Làm mới (ví dụ rdoNam).</summary>
        public CrudBinder WithDefaultRadios(params RadioButton[] radios)
        {
            _defaultRadios = radios ?? Array.Empty<RadioButton>();
            return this;
        }

        /// <summary>Đặt nhãn hiển thị thân thiện cho khóa chính khi báo trùng mã (ví dụ "Mã NV").</summary>
        public CrudBinder WithKeyLabel(string keyLabel)
        {
            if (!string.IsNullOrWhiteSpace(keyLabel))
                _keyDisplayName = keyLabel.Trim();
            return this;
        }

        /// <summary>
        /// Bật kiểm tra tự động theo schema bảng (lấy nhãn tiếng Việt từ Grid) kèm thêm các quy tắc nghiệp vụ riêng.
        /// </summary>
        public CrudBinder WithSchemaValidation(params ValidationRule[] extraRules)
        {
            _useSchemaValidation = true;
            _extraRulesFactory = () => extraRules;
            return this;
        }

        /// <summary>Cấu hình hàm sinh quy tắc kiểm tra bổ sung sau khi kiểm tra schema.</summary>
        public CrudBinder WithExtraValidation(Func<ValidationRule[]> extraRulesFactory, bool includeSchemaRules = true)
        {
            _useSchemaValidation = includeSchemaRules;
            _extraRulesFactory = extraRulesFactory;
            return this;
        }

        /// <summary>Chỉ định hàm kiểm tra tùy chỉnh thay thế kiểm tra tự động.</summary>
        public CrudBinder WithValidation(Func<ValidationResult> validator)
        {
            _customValidator = validator;
            return this;
        }

        /// <summary>Hook chạy trước khi Thêm (<c>isInsert = true</c>) hoặc Sửa (<c>isInsert = false</c>) để tùy biến dictionary lưu xuống DB.</summary>
        public CrudBinder WithBeforeSave(Action<Dictionary<string, object>, bool> handler)
        {
            _beforeSave = handler;
            return this;
        }

        /// <summary>Hook chạy sau khi Thêm/Sửa/Xóa thành công (<c>action</c>: "insert", "update", "delete").</summary>
        public CrudBinder WithAfterSaved(Action<string, object> handler)
        {
            _afterSaved = handler;
            return this;
        }

        /// <summary>Tùy chọn tự xóa trắng Form sau khi Thêm mới thành công (mặc định giữ chọn dòng mới thêm trên Grid).</summary>
        public CrudBinder ResetAfterInsert(bool reset = true)
        {
            _resetAfterInsert = reset;
            return this;
        }

        /// <summary>Tùy chỉnh câu hỏi xác nhận khi bấm nút Thoát.</summary>
        public CrudBinder WithExitConfirm(string message)
        {
            _exitConfirmMessage = message;
            return this;
        }

        /// <summary>Gắn các nút chức năng chuẩn của Form CRUD.</summary>
        public CrudBinder BindButtons(Button btnAdd = null, Button btnUpdate = null, Button btnDelete = null,
            Button btnReset = null, Button btnExit = null)
        {
            _btnAdd = btnAdd;
            _btnUpdate = btnUpdate;
            _btnDelete = btnDelete;
            _btnReset = btnReset;
            _btnExit = btnExit;
            return this;
        }

        /// <summary>
        /// Khởi chạy bộ điều phối: nạp ComboBox, đọc Schema bảng gốc, tải dữ liệu lên Grid và gắn toàn bộ sự kiện.
        /// Gọi trong sự kiện <c>Form_Load</c>.
        /// </summary>
        public CrudBinder Start()
        {
            foreach (var spec in _lookups)
            {
                if (spec.Table != null)
                {
                    if (!string.IsNullOrWhiteSpace(spec.ValueMember) && !string.IsNullOrWhiteSpace(spec.DisplayMember))
                        ComboBoxHelper.Bind(spec.Combo, spec.Table, spec.ValueMember, spec.DisplayMember);
                    else
                        ComboBoxHelper.Bind(spec.Combo, spec.Table);
                }
                else if (!string.IsNullOrWhiteSpace(spec.Sql))
                {
                    ComboBoxHelper.Bind(spec.Combo, spec.Sql, spec.ValueMember, spec.DisplayMember, _dbName);
                }
            }

            _schema = DbHelper.GetTableSchema(_tableName, _dbName);
            LoadData();

            if (!_started)
            {
                _started = true;
                _grid.SelectionChanged += OnGridSelectionChanged;
                if (_btnAdd != null) _btnAdd.Click += (s, e) => Add();
                if (_btnUpdate != null) _btnUpdate.Click += (s, e) => Update();
                if (_btnDelete != null) _btnDelete.Click += (s, e) => Delete();
                if (_btnReset != null) _btnReset.Click += (s, e) => ResetForm();
                if (_btnExit != null) _btnExit.Click += (s, e) => ExitForm();
            }

            return this;
        }

        /// <summary>Nạp lại dữ liệu từ CSDL lên DataGridView.</summary>
        public void LoadData()
        {
            _suppressSelection = true;
            try
            {
                if (!string.IsNullOrWhiteSpace(_selectSql))
                {
                    _currentData = DbHelper.GetDataTable(_selectSql, _dbName);
                    if (_gridColumns != null && _gridColumns.Length > 0)
                        GridHelper.Bind(_grid, _currentData, _gridColumns);
                    else
                        GridHelper.BindFormatted(_grid, _currentData, readOnly: true);
                }
                else
                {
                    _currentData = CrudHelper.GetAll(_tableName, _orderBy, _dbName);
                    if (_gridColumns != null && _gridColumns.Length > 0)
                        GridHelper.Bind(_grid, _currentData, _gridColumns);
                    else
                        GridHelper.BindFormatted(_grid, _currentData, readOnly: true);
                }
                GridHelper.SetReadOnly(_grid);

                if (_grid.Rows.Count > 0 && GridHelper.GetSelectedRow(_grid) == null && !_grid.Rows[0].IsNewRow)
                {
                    foreach (DataGridViewCell cell in _grid.Rows[0].Cells)
                    {
                        if (cell.Visible)
                        {
                            _grid.CurrentCell = cell;
                            break;
                        }
                    }
                    _grid.Rows[0].Selected = true;
                }
            }
            finally
            {
                _suppressSelection = false;
            }

            LoadCurrentGridRow();
        }

        /// <summary>Thực hiện nghiệp vụ Thêm mới có kiểm tra hợp lệ và kiểm tra trùng khóa chính.</summary>
        public bool Add()
        {
            if (!ValidateInputs()) return false;

            Dictionary<string, object> values = CollectValues(forInsert: true);
            bool isAutoId = _schema != null && _schema.Columns.Contains(_keyColumn) && _schema.Columns[_keyColumn].AutoIncrement;

            if (!isAutoId)
            {
                object keyVal = ResolveInputKey(values);
                if (keyVal != null && keyVal != DBNull.Value && CrudHelper.Exists(_tableName, _keyColumn, keyVal, _dbName))
                {
                    MessageHelper.DuplicateKey(ResolveKeyLabel(), keyVal.ToString());
                    FormHelper.FocusControl(_keyControl);
                    return false;
                }

                if (CrudHelper.Insert(_tableName, values, _dbName) <= 0)
                    return false;

                MessageHelper.Success("thêm mới");
                ReloadAfterMutation(keyVal, _resetAfterInsert);
                _afterSaved?.Invoke("insert", keyVal);
                return true;
            }

            values.Remove(_keyColumn);
            object newId = CrudHelper.InsertAndGetId<object>(_tableName, values, _dbName);
            if (DbHelper.HasError)
                return false;

            MessageHelper.Success("thêm mới");
            ReloadAfterMutation(newId, _resetAfterInsert);
            _afterSaved?.Invoke("insert", newId);
            return true;
        }

        /// <summary>Thực hiện nghiệp vụ Cập nhật dòng đang chọn trên DataGridView.</summary>
        public bool Update()
        {
            if (!GridHelper.HasSelection(_grid))
            {
                MessageHelper.NoSelection(_entityLabel);
                return false;
            }

            if (!ValidateInputs()) return false;

            object keyVal = GridHelper.GetSelectedValue<object>(_grid, _keyColumn) ?? ReadKeyFromControl();
            if (keyVal == null || keyVal == DBNull.Value)
            {
                MessageHelper.NoSelection(_entityLabel);
                return false;
            }

            Dictionary<string, object> values = CollectValues(forInsert: false);
            if (CrudHelper.Update(_tableName, values, _keyColumn, keyVal, _dbName) <= 0)
                return false;

            MessageHelper.Success("cập nhật");
            ReloadAfterMutation(keyVal, resetForm: false);
            _afterSaved?.Invoke("update", keyVal);
            return true;
        }

        /// <summary>Thực hiện nghiệp vụ Xóa dòng đang chọn sau khi hỏi xác nhận Yes/No.</summary>
        public bool Delete()
        {
            if (!GridHelper.HasSelection(_grid))
            {
                MessageHelper.NoSelection(_entityLabel);
                return false;
            }

            object keyVal = GridHelper.GetSelectedValue<object>(_grid, _keyColumn) ?? ReadKeyFromControl();
            if (keyVal == null || keyVal == DBNull.Value)
            {
                MessageHelper.NoSelection(_entityLabel);
                return false;
            }

            if (!MessageHelper.ConfirmDelete($"{_entityLabel} {keyVal}"))
                return false;

            if (CrudHelper.Delete(_tableName, _keyColumn, keyVal, _dbName) <= 0)
                return false;

            MessageHelper.Success("xóa");
            LoadData();
            ResetForm();
            _afterSaved?.Invoke("delete", keyVal);
            return true;
        }

        /// <summary>Đưa các ô nhập về mặc định, bỏ chọn Grid, mở khóa ô mã chính và đặt con trỏ về ô mã.</summary>
        public void ResetForm()
        {
            _suppressSelection = true;
            try
            {
                FormMapper.AutoClear(_container, _defaultRadios);
                GridHelper.ClearSelection(_grid);
                SetKeyLocked(false);
                FormHelper.FocusControl(_keyControl);
            }
            finally
            {
                _suppressSelection = false;
            }
        }

        private void ExitForm()
        {
            if (string.IsNullOrWhiteSpace(_exitConfirmMessage) || MessageHelper.Confirm(_exitConfirmMessage))
            {
                _form.Close();
            }
        }

        private void OnGridSelectionChanged(object sender, EventArgs e)
        {
            if (_suppressSelection) return;
            LoadCurrentGridRow();
        }

        private void LoadCurrentGridRow()
        {
            DataRowView row = GridHelper.GetSelectedRow(_grid);
            if (row == null) return;

            FormMapper.AutoLoad(_container, row);
            SetKeyLocked(true);
        }

        private void ReloadAfterMutation(object keyVal, bool resetForm)
        {
            if (resetForm)
            {
                LoadData();
                ResetForm();
                return;
            }

            LoadData();
            if (keyVal != null && keyVal != DBNull.Value)
            {
                GridHelper.SelectRowByValue(_grid, _keyColumn, keyVal);
            }
        }

        private bool ValidateInputs()
        {
            if (_customValidator != null)
            {
                var customRes = _customValidator();
                if (customRes != null && !customRes.IsValid)
                {
                    MessageHelper.Warning(customRes.ErrorMessage);
                    FormHelper.FocusControl(customRes.ErrorControl);
                    return false;
                }
                return true;
            }

            ValidationRule[] extraRules = _extraRulesFactory != null ? _extraRulesFactory() : Array.Empty<ValidationRule>();
            return _useSchemaValidation && _schema != null && _schema.Columns.Count > 0
                ? Validator.CheckSchema(_container, _schema, _grid, extraRules)
                : Validator.Check(extraRules);
        }

        private Dictionary<string, object> CollectValues(bool forInsert)
        {
            var values = FormMapper.AutoGetValues(_container, _schema);
            _beforeSave?.Invoke(values, forInsert);
            return values;
        }

        private object ResolveInputKey(Dictionary<string, object> values)
        {
            if (values != null && values.TryGetValue(_keyColumn, out object fromDict))
                return fromDict;
            return ReadKeyFromControl();
        }

        private object ReadKeyFromControl()
        {
            return _keyControl != null ? (object)_keyControl.Text.Trim() : null;
        }

        private string ResolveKeyLabel()
        {
            if (!string.Equals(_keyDisplayName, _keyColumn, StringComparison.Ordinal))
                return _keyDisplayName;

            foreach (DataGridViewColumn col in _grid.Columns)
            {
                string field = !string.IsNullOrWhiteSpace(col.DataPropertyName) ? col.DataPropertyName : col.Name;
                if (string.Equals(field, _keyColumn, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(col.HeaderText))
                    return col.HeaderText.Trim();
            }
            return _keyDisplayName;
        }

        private void SetKeyLocked(bool locked)
        {
            if (_keyControl == null) return;
            if (_keyControl is TextBoxBase tb)
                tb.ReadOnly = locked;
            else
                _keyControl.Enabled = !locked;
        }
    }
}
