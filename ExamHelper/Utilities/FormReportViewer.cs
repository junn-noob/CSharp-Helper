using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ExamHelper.Utilities
{
    /// <summary>
    /// Cấu hình ban đầu khi mở Form báo cáo trực quan (FormReportViewer).
    /// </summary>
    public sealed class ReportDesignOptions
    {
        public string ReportTitle { get; set; } = "BÁO CÁO THỐNG KÊ";
        public string FilterCondition { get; set; }
        public string GroupByColumn { get; set; }
        public string GroupPrefix { get; set; }
        public IEnumerable<ReportColumn> Columns { get; set; }
        public IEnumerable<ReportColumn> HiddenColumns { get; set; }
        public string CompanyName { get; set; } = "ĐƠN VỊ QUẢN LÝ";
        public string SignerTitle { get; set; } = "Người lập biểu";
        public bool ShowGroupTotals { get; set; }
        public string PageFooter { get; set; } = "Trang 1/1";
        public string DateFormat { get; set; } = "dd/MM/yyyy";
        public bool ShowStt { get; set; } = true;
        public bool Landscape { get; set; }
        public int FontSizePt { get; set; } = 13;
        public bool OpenDesigner { get; set; } = true;
    }

    /// <summary>
    /// Cầu nối COM cho sự kiện bấm trực tiếp trên vùng xem trước HTML của <see cref="FormReportViewer"/>.
    /// </summary>
    [ComVisible(true)]
    public sealed class ReportPreviewBridge
    {
        private readonly FormReportViewer _owner;

        internal ReportPreviewBridge(FormReportViewer owner)
        {
            _owner = owner;
        }

        public void OnPreviewClick(string action)
        {
            if (_owner == null || _owner.IsDisposed || string.IsNullOrWhiteSpace(action)) return;

            if (_owner.InvokeRequired)
                _owner.BeginInvoke((Action)(() => _owner.HandlePreviewClick(action)));
            else
                _owner.HandlePreviewClick(action);
        }
    }

    /// <summary>
    /// Form xem trước và thiết kế báo cáo trực quan: hỗ trợ kéo-thả ẩn/hiện/sắp xếp/gom nhóm cột,
    /// bấm trực tiếp vào tiêu đề hoặc cột trên bản xem trước để sửa, lưu/mở file mẫu (.examrpt)
    /// và xuất trực tiếp file thiết kế .rdlc / Excel (.xlsx) / In PDF.
    /// </summary>
    public class FormReportViewer : Form
    {
        private sealed class ColumnDragPayload
        {
            public ReportColumn Column { get; }
            public ListBox SourceList { get; }
            public int SourceIndex { get; }

            public ColumnDragPayload(ReportColumn column, ListBox sourceList, int sourceIndex)
            {
                Column = column;
                SourceList = sourceList;
                SourceIndex = sourceIndex;
            }
        }

        private sealed class GroupOptionItem
        {
            public string ColumnName { get; }
            public string DisplayText { get; }

            public GroupOptionItem(string columnName, string displayText)
            {
                ColumnName = columnName;
                DisplayText = displayText;
            }

            public override string ToString() => DisplayText;
        }

        private readonly bool _isDynamicMode;
        private readonly DataTable _sourceTable;
        private readonly ReportDesignOptions _initialOptions;
        private readonly List<ReportColumn> _initialVisibleColumns = new List<ReportColumn>();
        private readonly List<ReportColumn> _initialHiddenColumns = new List<ReportColumn>();

        private readonly List<ReportColumn> _visibleColumns = new List<ReportColumn>();
        private readonly List<ReportColumn> _hiddenColumns = new List<ReportColumn>();

        private string _reportTitle;
        private string _companyName;
        private string _filterCondition;
        private string _signerTitle;
        private string _pageFooter;
        private string _dateFormat;
        private string _groupByColumn;
        private string _groupPrefix;
        private bool _showGroupTotals;
        private bool _showStt;
        private bool _landscape;
        private int _fontSizePt = 13;
        private string _currentHtml;

        private bool _isSyncingUi;
        private bool _previewDirty;
        private ReportColumn _selectedVisibleColumn;

        private Point _dragStartPoint;
        private ListBox _pendingDragList;
        private int _pendingDragIndex = -1;
        private Control _pendingDragChipCtrl;
        private ReportColumn _pendingDragChipCol;
        private int _pendingDragChipIdx = -1;

        private Timer _debounceTimer;
        private Font _boldFont;
        private WebBrowser _webViewer;
        private Panel _previewHost;
        private SplitContainer _splitMain;
        private TabControl _designerTabs;

        private TextBox _txtQuickTitle;
        private Panel _pnlQuickGroupDrop;
        private Label _lblQuickGroupDrop;
        private Button _btnQuickClearGroup;
        private FlowLayoutPanel _chipStrip;
        private Button _btnQuickShowHidden;
        private ContextMenuStrip _hiddenColumnsMenu;
        private ContextMenuStrip _columnActionMenu;

        private Button _btnIn;
        private Button _btnXemTruoc;
        private Button _btnXuatExcel;
        private Button _btnXuatRdlc;
        private Button _btnLuuMau;
        private Button _btnMoMau;
        private Button _btnThietKe;
        private Button _btnDatLai;
        private Button _btnDong;

        private TextBox _txtTitle;
        private TextBox _txtCompany;
        private TextBox _txtFilter;
        private TextBox _txtSigner;
        private TextBox _txtFooter;
        private CheckBox _chkShowStt;
        private CheckBox _chkGroupTotals;
        private ComboBox _cboPaperOrientation;
        private NumericUpDown _nudFontSize;

        private Panel _pnlGroupDrop;
        private Label _lblGroupDrop;
        private ComboBox _cboGroupBy;
        private Button _btnClearGroup;
        private TextBox _txtGroupPrefix;

        private ListBox _lstVisible;
        private ListBox _lstHidden;
        private Button _btnMoveUp;
        private Button _btnMoveDown;
        private Button _btnHideCol;
        private Button _btnShowCol;

        private Label _lblEditingCol;
        private TextBox _txtColHeader;
        private ComboBox _cboColAlign;
        private ComboBox _cboColFormat;
        private ComboBox _cboColSummary;

        /// <summary>Mã HTML thuần (in ấn/lưu) phản ánh các chỉnh sửa thiết kế mới nhất.</summary>
        public string CurrentHtml => _currentHtml;

        /// <summary>Danh sách cột đang hiển thị trên báo cáo.</summary>
        public IReadOnlyList<ReportColumn> VisibleColumns => _visibleColumns.AsReadOnly();

        /// <summary>Danh sách cột đang bị ẩn khỏi báo cáo.</summary>
        public IReadOnlyList<ReportColumn> HiddenColumns => _hiddenColumns.AsReadOnly();

        /// <summary>Tiêu đề lớn của báo cáo.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ReportTitle
        {
            get => _reportTitle;
            set
            {
                _reportTitle = value ?? "";
                Text = string.IsNullOrWhiteSpace(_reportTitle) ? "Báo cáo thống kê" : _reportTitle;
                if (_txtTitle != null && _txtTitle.Text != _reportTitle)
                    SetControlText(_txtTitle, _reportTitle);
                if (_txtQuickTitle != null && _txtQuickTitle.Text != _reportTitle)
                    SetControlText(_txtQuickTitle, _reportTitle);
                RebuildHtml(immediate: true);
            }
        }

        /// <summary>Tên cơ quan / doanh nghiệp trên đầu báo cáo.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ReportCompanyName
        {
            get => _companyName;
            set
            {
                _companyName = value ?? "";
                if (_txtCompany != null && _txtCompany.Text != _companyName)
                    SetControlText(_txtCompany, _companyName);
                RebuildHtml(immediate: true);
            }
        }

        /// <summary>Chuỗi mô tả điều kiện lọc hiển thị dưới tiêu đề.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FilterCondition
        {
            get => _filterCondition;
            set
            {
                _filterCondition = value ?? "";
                if (_txtFilter != null && _txtFilter.Text != _filterCondition)
                    SetControlText(_txtFilter, _filterCondition);
                RebuildHtml(immediate: true);
            }
        }

        /// <summary>Tên cột đang dùng để gom nhóm (null/rỗng nếu không gom nhóm).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string GroupByColumn
        {
            get => _groupByColumn;
            set => SetGroupByColumn(value);
        }

        /// <summary>Có hiển thị cột STT hay không.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowStt
        {
            get => _showStt;
            set
            {
                _showStt = value;
                if (_chkShowStt != null && _chkShowStt.Checked != value)
                {
                    _isSyncingUi = true;
                    _chkShowStt.Checked = value;
                    _isSyncingUi = false;
                }
                RebuildHtml(immediate: true);
            }
        }

        /// <summary>In khổ giấy ngang (A4 Landscape) nếu true, khổ dọc (Portrait) nếu false.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Landscape
        {
            get => _landscape;
            set
            {
                _landscape = value;
                if (_cboPaperOrientation != null)
                {
                    int targetIdx = value ? 1 : 0;
                    if (_cboPaperOrientation.SelectedIndex != targetIdx)
                    {
                        _isSyncingUi = true;
                        _cboPaperOrientation.SelectedIndex = targetIdx;
                        _isSyncingUi = false;
                    }
                }
                RebuildHtml(immediate: true);
            }
        }

        /// <summary>Cỡ chữ bảng báo cáo (9..22).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int FontSizePt
        {
            get => _fontSizePt;
            set
            {
                _fontSizePt = Clamp(value <= 0 ? 13 : value, 9, 22);
                if (_nudFontSize != null && (int)_nudFontSize.Value != _fontSizePt)
                {
                    _isSyncingUi = true;
                    _nudFontSize.Value = _fontSizePt;
                    _isSyncingUi = false;
                }
                RebuildHtml(immediate: true);
            }
        }

        /// <summary>Khởi tạo form xem báo cáo từ mã HTML tạo sẵn (chế độ chỉ xem/in/xuất).</summary>
        public FormReportViewer(string htmlContent, string title = "Báo cáo thống kê")
        {
            _isDynamicMode = false;
            _reportTitle = string.IsNullOrWhiteSpace(title) ? "Báo cáo thống kê" : title;
            _currentHtml = htmlContent ?? "";

            InitializeShellUi(openDesigner: false);
        }

        /// <summary>
        /// Khởi tạo form báo cáo trực quan từ DataTable, tự gom tất cả trường dữ liệu
        /// và cho phép kéo-thả chỉnh sửa, lưu file mẫu (.examrpt) hoặc xuất .rdlc.
        /// </summary>
        public FormReportViewer(DataTable dataTable, ReportDesignOptions options = null)
        {
            _isDynamicMode = true;
            _sourceTable = dataTable != null ? dataTable.Copy() : new DataTable();
            _initialOptions = options ?? new ReportDesignOptions();

            InitializeStateFromOptions();
            InitializeShellUi(_initialOptions.OpenDesigner);
            SyncAllUiFromState();
            RebuildHtml(immediate: true);
        }

        private void InitializeStateFromOptions()
        {
            _reportTitle = string.IsNullOrWhiteSpace(_initialOptions.ReportTitle) ? "BÁO CÁO THỐNG KÊ" : _initialOptions.ReportTitle;
            _companyName = _initialOptions.CompanyName ?? "ĐƠN VỊ QUẢN LÝ";
            _filterCondition = _initialOptions.FilterCondition ?? "";
            _signerTitle = _initialOptions.SignerTitle ?? "Người lập biểu";
            _pageFooter = _initialOptions.PageFooter ?? "Trang 1/1";
            _dateFormat = string.IsNullOrWhiteSpace(_initialOptions.DateFormat) ? "dd/MM/yyyy" : _initialOptions.DateFormat;
            _groupByColumn = string.IsNullOrWhiteSpace(_initialOptions.GroupByColumn) ? null : _initialOptions.GroupByColumn.Trim();
            _groupPrefix = _initialOptions.GroupPrefix ?? "";
            _showGroupTotals = _initialOptions.ShowGroupTotals;
            _showStt = _initialOptions.ShowStt;
            _landscape = _initialOptions.Landscape;
            _fontSizePt = Clamp(_initialOptions.FontSizePt <= 0 ? 13 : _initialOptions.FontSizePt, 9, 22);

            _initialVisibleColumns.Clear();
            _initialHiddenColumns.Clear();

            var autoCols = ReportHelper.BuildAutoColumns(_sourceTable, null, null);
            var explicitCols = _initialOptions.Columns?.Where(c => c != null && !string.IsNullOrWhiteSpace(c.ColumnName)).ToList();
            var explicitHiddenCols = _initialOptions.HiddenColumns?.Where(c => c != null && !string.IsNullOrWhiteSpace(c.ColumnName)).ToList();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (explicitCols != null && explicitCols.Count > 0)
            {
                foreach (var col in explicitCols)
                {
                    if ((_sourceTable.Columns.Count == 0 || _sourceTable.Columns.Contains(col.ColumnName))
                        && usedNames.Add(col.ColumnName))
                    {
                        _initialVisibleColumns.Add(col.Clone());
                    }
                }
            }
            else
            {
                foreach (var col in autoCols)
                {
                    if (!string.IsNullOrEmpty(_groupByColumn) &&
                        string.Equals(col.ColumnName, _groupByColumn, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (usedNames.Add(col.ColumnName))
                    {
                        _initialVisibleColumns.Add(col.Clone());
                    }
                }
            }

            if (explicitHiddenCols != null)
            {
                foreach (var col in explicitHiddenCols)
                {
                    if ((_sourceTable.Columns.Count == 0 || _sourceTable.Columns.Contains(col.ColumnName))
                        && usedNames.Add(col.ColumnName))
                    {
                        _initialHiddenColumns.Add(col.Clone());
                    }
                }
            }

            foreach (var col in autoCols)
            {
                if (!usedNames.Contains(col.ColumnName))
                {
                    _initialHiddenColumns.Add(col.Clone());
                }
            }

            RestoreColumnListsFromInitial();
        }

        private void RestoreColumnListsFromInitial()
        {
            _visibleColumns.Clear();
            _hiddenColumns.Clear();

            foreach (var col in _initialVisibleColumns)
                _visibleColumns.Add(col.Clone());
            foreach (var col in _initialHiddenColumns)
                _hiddenColumns.Add(col.Clone());
        }

        /// <summary>Khôi phục toàn bộ tiêu đề, nhóm và thứ tự cột về trạng thái lúc mới mở báo cáo.</summary>
        public void ResetDesign()
        {
            if (!_isDynamicMode) return;

            _reportTitle = string.IsNullOrWhiteSpace(_initialOptions.ReportTitle) ? "BÁO CÁO THỐNG KÊ" : _initialOptions.ReportTitle;
            _companyName = _initialOptions.CompanyName ?? "ĐƠN VỊ QUẢN LÝ";
            _filterCondition = _initialOptions.FilterCondition ?? "";
            _signerTitle = _initialOptions.SignerTitle ?? "Người lập biểu";
            _pageFooter = _initialOptions.PageFooter ?? "Trang 1/1";
            _dateFormat = string.IsNullOrWhiteSpace(_initialOptions.DateFormat) ? "dd/MM/yyyy" : _initialOptions.DateFormat;
            _groupByColumn = string.IsNullOrWhiteSpace(_initialOptions.GroupByColumn) ? null : _initialOptions.GroupByColumn.Trim();
            _groupPrefix = _initialOptions.GroupPrefix ?? "";
            _showGroupTotals = _initialOptions.ShowGroupTotals;
            _showStt = _initialOptions.ShowStt;
            _landscape = _initialOptions.Landscape;
            _fontSizePt = Clamp(_initialOptions.FontSizePt <= 0 ? 13 : _initialOptions.FontSizePt, 9, 22);

            RestoreColumnListsFromInitial();
            _selectedVisibleColumn = _visibleColumns.FirstOrDefault();

            Text = _reportTitle;
            SyncAllUiFromState();
            RebuildHtml(immediate: true);
        }

        /// <summary>Trích xuất thiết kế hiện tại thành đối tượng <see cref="ReportTemplate"/> để lưu thành file.</summary>
        public ReportTemplate ToTemplate()
        {
            var template = new ReportTemplate
            {
                ReportTitle = _reportTitle ?? "BÁO CÁO THỐNG KÊ",
                CompanyName = _companyName ?? "ĐƠN VỊ QUẢN LÝ",
                FilterCondition = _filterCondition ?? "",
                GroupByColumn = _groupByColumn ?? "",
                GroupPrefix = _groupPrefix ?? "",
                SignerTitle = _signerTitle ?? "Người lập biểu",
                PageFooter = _pageFooter ?? "Trang 1/1",
                DateFormat = _dateFormat ?? "dd/MM/yyyy",
                ShowGroupTotals = _showGroupTotals,
                ShowStt = _showStt,
                Landscape = _landscape,
                FontSizePt = _fontSizePt
            };

            foreach (var col in _visibleColumns)
                template.Columns.Add(new ReportTemplateColumn(col, visible: true));
            foreach (var col in _hiddenColumns)
                template.Columns.Add(new ReportTemplateColumn(col, visible: false));

            return template;
        }

        /// <summary>Áp dụng một đối tượng <see cref="ReportTemplate"/> lên giao diện báo cáo đang mở.</summary>
        public void ApplyTemplate(ReportTemplate template, bool preserveCurrentFilterIfEmpty = false)
        {
            if (!_isDynamicMode || template == null) return;

            string effectiveFilter = preserveCurrentFilterIfEmpty && string.IsNullOrWhiteSpace(template.FilterCondition)
                ? _filterCondition
                : (template.FilterCondition ?? "");

            ReportDesignOptions opts = template.ToDesignOptions(_sourceTable, effectiveFilter, openDesigner: true);
            _reportTitle = opts.ReportTitle;
            _companyName = opts.CompanyName;
            _filterCondition = opts.FilterCondition;
            _signerTitle = opts.SignerTitle;
            _pageFooter = opts.PageFooter;
            _dateFormat = opts.DateFormat;
            _groupByColumn = opts.GroupByColumn;
            _groupPrefix = opts.GroupPrefix;
            _showGroupTotals = opts.ShowGroupTotals;
            _showStt = opts.ShowStt;
            _landscape = opts.Landscape;
            _fontSizePt = Clamp(opts.FontSizePt, 9, 22);

            _visibleColumns.Clear();
            _hiddenColumns.Clear();

            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (opts.Columns != null)
            {
                foreach (var c in opts.Columns)
                {
                    if ((_sourceTable.Columns.Count == 0 || _sourceTable.Columns.Contains(c.ColumnName))
                        && used.Add(c.ColumnName))
                    {
                        _visibleColumns.Add(c.Clone());
                    }
                }
            }
            if (opts.HiddenColumns != null)
            {
                foreach (var c in opts.HiddenColumns)
                {
                    if ((_sourceTable.Columns.Count == 0 || _sourceTable.Columns.Contains(c.ColumnName))
                        && used.Add(c.ColumnName))
                    {
                        _hiddenColumns.Add(c.Clone());
                    }
                }
            }
            foreach (var autoCol in ReportHelper.BuildAutoColumns(_sourceTable, null, null))
            {
                if (used.Add(autoCol.ColumnName))
                {
                    _hiddenColumns.Add(autoCol);
                }
            }

            _selectedVisibleColumn = _visibleColumns.FirstOrDefault();
            Text = string.IsNullOrWhiteSpace(_reportTitle) ? "Báo cáo thống kê" : _reportTitle;
            SyncAllUiFromState();
            RebuildHtml(immediate: true);
        }

        /// <summary>Lưu bố cục thiết kế hiện tại ra file mẫu (.examrpt / .xml).</summary>
        public bool SaveTemplateFile(string filePath = null)
        {
            if (!_isDynamicMode) return false;
            if (!EnsurePrintable()) return false;

            string targetPath = filePath;
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                string safeName = ReportHelper.SanitizeFileName(_reportTitle, "MauBaoCao");
                if (!safeName.EndsWith(".examrpt", StringComparison.OrdinalIgnoreCase))
                    safeName += ".examrpt";

                using (var sfd = new SaveFileDialog
                {
                    Filter = "Mẫu báo cáo ExamHelper (*.examrpt)|*.examrpt|XML File (*.xml)|*.xml",
                    FileName = safeName
                })
                {
                    if (sfd.ShowDialog() != DialogResult.OK)
                        return false;
                    targetPath = sfd.FileName;
                }
            }

            try
            {
                ToTemplate().Save(targetPath);
                if (string.IsNullOrWhiteSpace(filePath))
                    MessageHelper.Success("lưu mẫu báo cáo");
                return true;
            }
            catch (Exception ex)
            {
                MessageHelper.Error("Lỗi khi lưu mẫu báo cáo: " + ex.Message);
                return false;
            }
        }

        /// <summary>Nạp bố cục từ file mẫu (.examrpt / .xml) lên báo cáo đang mở.</summary>
        public bool LoadTemplateFile(string filePath = null)
        {
            if (!_isDynamicMode) return false;

            string sourcePath = filePath;
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                using (var ofd = new OpenFileDialog
                {
                    Filter = "Mẫu báo cáo ExamHelper (*.examrpt;*.xml)|*.examrpt;*.xml|Tất cả (*.*)|*.*"
                })
                {
                    if (ofd.ShowDialog() != DialogResult.OK)
                        return false;
                    sourcePath = ofd.FileName;
                }
            }

            try
            {
                ReportTemplate template = ReportTemplate.Load(sourcePath);
                ApplyTemplate(template, preserveCurrentFilterIfEmpty: true);
                if (string.IsNullOrWhiteSpace(filePath))
                    MessageHelper.Success("nạp mẫu báo cáo");
                return true;
            }
            catch (Exception ex)
            {
                MessageHelper.Error("Lỗi khi đọc mẫu báo cáo: " + ex.Message);
                return false;
            }
        }

        /// <summary>Sinh chuỗi XML .rdlc theo bố cục thiết kế hiện tại.</summary>
        public string BuildRdlcXml(string dataSetName = "dsBaoCao")
        {
            if (!_isDynamicMode)
                throw new InvalidOperationException("Chế độ HTML tĩnh không hỗ trợ sinh file .rdlc.");

            return ReportHelper.GenerateRdlcXml(
                _sourceTable, _reportTitle, _filterCondition, _groupByColumn, _groupPrefix,
                _visibleColumns, _companyName, _signerTitle, _showGroupTotals, _pageFooter,
                _dateFormat, _showStt, _landscape, _fontSizePt, dataSetName);
        }

        /// <summary>Xuất thiết kế báo cáo hiện tại thành file .rdlc chuẩn Microsoft Report.</summary>
        public bool ExportRdlcFile(string filePath = null, string dataSetName = "dsBaoCao")
        {
            if (!_isDynamicMode) return false;
            if (!EnsurePrintable()) return false;

            return ReportHelper.ExportRdlcFile(
                _sourceTable, _reportTitle, _filterCondition, _groupByColumn, _groupPrefix,
                _visibleColumns, _companyName, _signerTitle, _showGroupTotals, _pageFooter,
                _dateFormat, _showStt, _landscape, _fontSizePt, dataSetName, filePath);
        }

        /// <summary>Ẩn một cột đang hiển thị sang danh sách cột bị ẩn.</summary>
        public bool HideColumn(string columnName, int? insertIndex = null)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return false;
            int idx = _visibleColumns.FindIndex(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return false;

            ReportColumn col = _visibleColumns[idx];
            _visibleColumns.RemoveAt(idx);

            if (!_hiddenColumns.Any(c => string.Equals(c.ColumnName, col.ColumnName, StringComparison.OrdinalIgnoreCase)))
            {
                int target = insertIndex.HasValue ? Clamp(insertIndex.Value, 0, _hiddenColumns.Count) : _hiddenColumns.Count;
                _hiddenColumns.Insert(target, col);
            }

            if (ReferenceEquals(_selectedVisibleColumn, col))
            {
                _selectedVisibleColumn = _visibleColumns.Count > 0
                    ? _visibleColumns[Math.Min(idx, _visibleColumns.Count - 1)]
                    : null;
            }

            RefreshColumnListsUi(col.ColumnName, selectInVisible: false);
            RebuildHtml(immediate: true);
            return true;
        }

        /// <summary>Hiện lại một cột từ danh sách ẩn vào vị trí chỉ định trong bảng báo cáo.</summary>
        public bool ShowColumn(string columnName, int? insertIndex = null)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return false;
            int idx = _hiddenColumns.FindIndex(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return false;

            ReportColumn col = _hiddenColumns[idx];
            _hiddenColumns.RemoveAt(idx);

            int target = insertIndex.HasValue ? Clamp(insertIndex.Value, 0, _visibleColumns.Count) : _visibleColumns.Count;
            _visibleColumns.Insert(target, col);
            _selectedVisibleColumn = col;

            RefreshColumnListsUi(col.ColumnName, selectInVisible: true);
            RebuildHtml(immediate: true);
            return true;
        }

        /// <summary>Di chuyển thứ tự của một cột đang hiển thị sang vị trí mới.</summary>
        public bool MoveVisibleColumn(string columnName, int newIndex)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return false;
            int oldIdx = _visibleColumns.FindIndex(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
            if (oldIdx < 0) return false;

            ReportColumn col = _visibleColumns[oldIdx];
            _visibleColumns.RemoveAt(oldIdx);

            int clamped = Clamp(newIndex, 0, _visibleColumns.Count);
            _visibleColumns.Insert(clamped, col);
            _selectedVisibleColumn = col;

            RefreshColumnListsUi(col.ColumnName, selectInVisible: true);
            RebuildHtml(immediate: true);
            return true;
        }

        private bool MoveHiddenColumn(string columnName, int newIndex)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return false;
            int oldIdx = _hiddenColumns.FindIndex(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
            if (oldIdx < 0) return false;

            ReportColumn col = _hiddenColumns[oldIdx];
            _hiddenColumns.RemoveAt(oldIdx);

            int clamped = Clamp(newIndex, 0, _hiddenColumns.Count);
            _hiddenColumns.Insert(clamped, col);
            RefreshColumnListsUi(col.ColumnName, selectInVisible: false);
            return true;
        }

        /// <summary>Đặt cột dùng để gom nhóm dữ liệu (truyền null hoặc chuỗi rỗng để bỏ gom nhóm).</summary>
        public bool SetGroupByColumn(string columnName)
        {
            if (!_isDynamicMode) return false;

            string normalized = string.IsNullOrWhiteSpace(columnName) ? null : columnName.Trim();
            if (normalized != null && !_sourceTable.Columns.Contains(normalized))
                return false;

            string previousGroup = _groupByColumn;
            _groupByColumn = normalized;

            if (!string.IsNullOrEmpty(normalized))
            {
                int visIdx = _visibleColumns.FindIndex(c => string.Equals(c.ColumnName, normalized, StringComparison.OrdinalIgnoreCase));
                if (visIdx >= 0)
                {
                    var moved = _visibleColumns[visIdx];
                    _visibleColumns.RemoveAt(visIdx);
                    if (!_hiddenColumns.Any(c => string.Equals(c.ColumnName, moved.ColumnName, StringComparison.OrdinalIgnoreCase)))
                        _hiddenColumns.Add(moved);
                    if (ReferenceEquals(_selectedVisibleColumn, moved))
                        _selectedVisibleColumn = _visibleColumns.FirstOrDefault();
                }

                if (string.IsNullOrWhiteSpace(_groupPrefix) && !string.Equals(previousGroup, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    string displayHeader = FindColumnHeader(normalized);
                    _groupPrefix = string.IsNullOrWhiteSpace(displayHeader) ? normalized + ": " : displayHeader + ": ";
                    if (_txtGroupPrefix != null)
                        SetControlText(_txtGroupPrefix, _groupPrefix);
                }
            }

            SyncGroupControls();
            RefreshColumnListsUi(_selectedVisibleColumn?.ColumnName, selectInVisible: true);
            RebuildHtml(immediate: true);
            return true;
        }

        private string FindColumnHeader(string columnName)
        {
            var match = _visibleColumns.FirstOrDefault(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                ?? _hiddenColumns.FirstOrDefault(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
            return match != null ? match.HeaderText : columnName;
        }

        internal void HandlePreviewClick(string action)
        {
            if (!_isDynamicMode || string.IsNullOrWhiteSpace(action)) return;

            if (action == "company")
            {
                if (TryPromptText("Sửa tên đơn vị", "Đơn vị / Công ty:", _companyName, out string newVal))
                    ReportCompanyName = newVal;
            }
            else if (action == "title")
            {
                if (TryPromptText("Sửa tiêu đề báo cáo", "Tiêu đề báo cáo:", _reportTitle, out string newVal))
                    ReportTitle = newVal;
            }
            else if (action == "filter")
            {
                if (TryPromptText("Sửa điều kiện lọc", "Điều kiện lọc (để trống nếu muốn ẩn):", _filterCondition, out string newVal))
                    FilterCondition = newVal;
            }
            else if (action == "signer")
            {
                if (TryPromptText("Sửa chức danh người ký", "Chức danh người ký:", _signerTitle, out string newVal))
                {
                    _signerTitle = newVal ?? "";
                    if (_txtSigner != null) SetControlText(_txtSigner, _signerTitle);
                    RebuildHtml(immediate: true);
                }
            }
            else if (action.StartsWith("col:", StringComparison.OrdinalIgnoreCase))
            {
                string colName = action.Substring(4);
                var target = _visibleColumns.FirstOrDefault(c => string.Equals(c.ColumnName, colName, StringComparison.OrdinalIgnoreCase));
                if (target != null)
                {
                    SelectVisibleColumn(target);
                    ShowColumnActionMenu(target, Cursor.Position);
                }
            }
        }

        private void SelectVisibleColumn(ReportColumn col)
        {
            _selectedVisibleColumn = col;
            if (_lstVisible != null)
            {
                _isSyncingUi = true;
                _lstVisible.SelectedIndex = _visibleColumns.IndexOf(col);
                _isSyncingUi = false;
            }
            SyncSelectedColumnEditor();
            RebuildColumnChips();
        }

        private void ShowColumnActionMenu(ReportColumn col, Point screenPoint)
        {
            if (col == null) return;
            _columnActionMenu?.Dispose();
            _columnActionMenu = new ContextMenuStrip();

            int idx = _visibleColumns.IndexOf(col);

            var miRename = new ToolStripMenuItem($"Đổi tên cột \"{col.HeaderText}\"...");
            miRename.Click += (s, e) =>
            {
                if (TryPromptText("Đổi tiêu đề cột", $"Tiêu đề hiển thị cho cột '{col.ColumnName}':", col.HeaderText, out string newHeader))
                {
                    col.HeaderText = string.IsNullOrWhiteSpace(newHeader) ? col.ColumnName : newHeader.Trim();
                    SyncSelectedColumnEditor();
                    _lstVisible?.Invalidate();
                    RebuildColumnChips();
                    RebuildHtml(immediate: true);
                }
            };
            _columnActionMenu.Items.Add(miRename);
            _columnActionMenu.Items.Add(new ToolStripSeparator());

            var miLeft = new ToolStripMenuItem("Di chuyển sang trái ◀") { Enabled = idx > 0 };
            miLeft.Click += (s, e) => MoveVisibleColumn(col.ColumnName, idx - 1);
            var miRight = new ToolStripMenuItem("Di chuyển sang phải ▶") { Enabled = idx >= 0 && idx < _visibleColumns.Count - 1 };
            miRight.Click += (s, e) => MoveVisibleColumn(col.ColumnName, idx + 1);
            _columnActionMenu.Items.Add(miLeft);
            _columnActionMenu.Items.Add(miRight);
            _columnActionMenu.Items.Add(new ToolStripSeparator());

            var miAlign = new ToolStripMenuItem("Căn lề");
            miAlign.DropDownItems.Add(CreateAlignMenuItem("Trái", col, HorizontalAlignment.Left));
            miAlign.DropDownItems.Add(CreateAlignMenuItem("Giữa", col, HorizontalAlignment.Center));
            miAlign.DropDownItems.Add(CreateAlignMenuItem("Phải", col, HorizontalAlignment.Right));
            _columnActionMenu.Items.Add(miAlign);

            var miFormat = new ToolStripMenuItem("Định dạng hiển thị");
            miFormat.DropDownItems.Add(CreateFormatMenuItem("Mặc định (không định dạng)", col, null));
            miFormat.DropDownItems.Add(CreateFormatMenuItem("Số nguyên (N0 - vd: 15.000.000)", col, "N0"));
            miFormat.DropDownItems.Add(CreateFormatMenuItem("Số thập phân gọn (0.##)", col, "0.##"));
            miFormat.DropDownItems.Add(CreateFormatMenuItem("Ngày (dd/MM/yyyy)", col, "dd/MM/yyyy"));
            miFormat.DropDownItems.Add(CreateFormatMenuItem("Ngày giờ (dd/MM/yyyy HH:mm)", col, "dd/MM/yyyy HH:mm"));
            _columnActionMenu.Items.Add(miFormat);

            var miSummary = new ToolStripMenuItem("Thống kê cuối bảng");
            miSummary.DropDownItems.Add(CreateSummaryMenuItem("Không thống kê", col, ReportSummaryMode.None));
            miSummary.DropDownItems.Add(CreateSummaryMenuItem("Tổng (SUM)", col, ReportSummaryMode.Sum));
            miSummary.DropDownItems.Add(CreateSummaryMenuItem("Trung bình (AVG)", col, ReportSummaryMode.Average));
            miSummary.DropDownItems.Add(CreateSummaryMenuItem("Đếm dòng (COUNT)", col, ReportSummaryMode.Count));
            miSummary.DropDownItems.Add(CreateSummaryMenuItem("Lớn nhất (MAX)", col, ReportSummaryMode.Max));
            miSummary.DropDownItems.Add(CreateSummaryMenuItem("Nhỏ nhất (MIN)", col, ReportSummaryMode.Min));
            _columnActionMenu.Items.Add(miSummary);

            _columnActionMenu.Items.Add(new ToolStripSeparator());

            var miGroup = new ToolStripMenuItem($"Gom nhóm theo \"{col.HeaderText}\"");
            miGroup.Click += (s, e) => SetGroupByColumn(col.ColumnName);
            _columnActionMenu.Items.Add(miGroup);

            var miHide = new ToolStripMenuItem("Ẩn cột này khỏi báo cáo") { ForeColor = Color.FromArgb(185, 28, 28) };
            miHide.Click += (s, e) => HideColumn(col.ColumnName);
            _columnActionMenu.Items.Add(miHide);

            _columnActionMenu.Show(screenPoint);
        }

        private ToolStripMenuItem CreateAlignMenuItem(string text, ReportColumn col, HorizontalAlignment align)
        {
            var item = new ToolStripMenuItem(text) { Checked = col.Alignment == align };
            item.Click += (s, e) =>
            {
                col.Alignment = align;
                SyncSelectedColumnEditor();
                RebuildHtml(immediate: true);
            };
            return item;
        }

        private ToolStripMenuItem CreateFormatMenuItem(string text, ReportColumn col, string format)
        {
            string current = string.IsNullOrWhiteSpace(col.Format) ? null : col.Format.Trim();
            var item = new ToolStripMenuItem(text) { Checked = string.Equals(current, format, StringComparison.OrdinalIgnoreCase) };
            item.Click += (s, e) =>
            {
                col.Format = format;
                SyncSelectedColumnEditor();
                RebuildHtml(immediate: true);
            };
            return item;
        }

        private ToolStripMenuItem CreateSummaryMenuItem(string text, ReportColumn col, ReportSummaryMode mode)
        {
            var item = new ToolStripMenuItem(text) { Checked = col.SummaryMode == mode };
            item.Click += (s, e) =>
            {
                col.SummaryMode = mode;
                SyncSelectedColumnEditor();
                _lstVisible?.Invalidate();
                RebuildColumnChips();
                RebuildHtml(immediate: true);
            };
            return item;
        }

        private bool TryPromptText(string title, string promptLabel, string initialValue, out string result)
        {
            result = initialValue;
            using (var dlg = new Form
            {
                Text = title,
                Size = new Size(440, 155),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                Font = Font
            })
            {
                var lbl = new Label { Text = promptLabel, Left = 14, Top = 12, Width = 395 };
                var txt = new TextBox { Text = initialValue ?? "", Left = 14, Top = 36, Width = 395 };
                var btnOk = new Button { Text = "Áp dụng", DialogResult = DialogResult.OK, Left = 240, Top = 74, Width = 84, Height = 28 };
                var btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Left = 330, Top = 74, Width = 78, Height = 28 };
                dlg.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;
                dlg.Shown += (s, e) => { txt.Focus(); txt.SelectAll(); };

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    result = txt.Text;
                    return true;
                }
            }
            return false;
        }

        private void InitializeShellUi(bool openDesigner)
        {
            Text = string.IsNullOrWhiteSpace(_reportTitle) ? "Báo cáo thống kê" : _reportTitle;
            Size = _isDynamicMode ? new Size(1180, 770) : new Size(960, 720);
            MinimumSize = new Size(860, 600);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);
            _boldFont = new Font(Font, FontStyle.Bold);
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            _debounceTimer = new Timer { Interval = 220 };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop();
                if (_previewDirty) RebuildHtml(immediate: true);
            };

            // 1. Thanh công cụ trên cùng dạng FlowLayout tự dàn hàng sạch sẽ
            var topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(8, 6, 8, 6)
            };

            _btnIn = CreateToolbarButton("In / PDF (&P)", 106, bold: true);
            _btnIn.Click += (s, e) =>
            {
                if (!EnsurePrintable()) return;
                _webViewer?.ShowPrintDialog();
            };

            _btnXemTruoc = CreateToolbarButton("Xem trước (&V)", 108);
            _btnXemTruoc.Click += (s, e) =>
            {
                if (!EnsurePrintable()) return;
                _webViewer?.ShowPrintPreviewDialog();
            };

            _btnXuatExcel = CreateToolbarButton("Xuất Excel (&E)", 110);
            _btnXuatExcel.Click += (s, e) =>
            {
                if (!EnsurePrintable()) return;
                if (_isDynamicMode)
                {
                    ReportHelper.ExportXlsx(_sourceTable, _reportTitle, _currentHtml,
                        _filterCondition, _groupByColumn, _groupPrefix, _visibleColumns,
                        _companyName, _signerTitle, _showGroupTotals, _pageFooter, _dateFormat,
                        _showStt, Text, _landscape, _fontSizePt);
                }
                else
                {
                    ReportHelper.ExportExcel(_currentHtml, ReportHelper.SanitizeFileName(Text) + ".xls");
                }
            };

            _btnXuatRdlc = CreateToolbarButton("Xuất .rdlc (&R)", 104, visible: _isDynamicMode);
            _btnXuatRdlc.Click += (s, e) => ExportRdlcFile();

            _btnLuuMau = CreateToolbarButton("Lưu mẫu (&S)", 98, visible: _isDynamicMode);
            _btnLuuMau.Click += (s, e) => SaveTemplateFile();

            _btnMoMau = CreateToolbarButton("Mở mẫu (&O)", 96, visible: _isDynamicMode);
            _btnMoMau.Click += (s, e) => LoadTemplateFile();

            _btnThietKe = CreateToolbarButton(openDesigner ? "Ẩn thiết kế (&D)" : "Thiết kế (&D)", 114, visible: _isDynamicMode);
            _btnThietKe.Click += (s, e) =>
            {
                _splitMain.Panel1Collapsed = !_splitMain.Panel1Collapsed;
                _btnThietKe.Text = _splitMain.Panel1Collapsed ? "Thiết kế (&D)" : "Ẩn thiết kế (&D)";
            };

            _btnDatLai = CreateToolbarButton("↺ Mặc định", 88, visible: _isDynamicMode);
            _btnDatLai.Click += (s, e) => ResetDesign();

            _btnDong = CreateToolbarButton("Đóng (&N)", 84);
            _btnDong.Click += (s, e) => Close();

            topPanel.Controls.AddRange(new Control[]
            {
                _btnIn, _btnXemTruoc, _btnXuatExcel, _btnXuatRdlc,
                _btnLuuMau, _btnMoMau, _btnThietKe, _btnDatLai, _btnDong
            });

            // 2. Vùng chính: Panel thiết kế 2 tab gọn gàng bên trái + Xem trước tương tác bên phải
            _splitMain = new SafeSplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Size = new Size(1160, 670),
                SplitterWidth = 5
            };
            ((ISupportInitialize)_splitMain).BeginInit();
            _splitMain.SuspendLayout();

            if (_isDynamicMode)
            {
                BuildDesignerPanel(_splitMain.Panel1);
            }

            _previewHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _splitMain.Panel2.Controls.Add(_previewHost);
            if (_isDynamicMode)
            {
                BuildQuickPreviewStrip(_splitMain.Panel2);
            }

            Controls.Add(_splitMain);
            Controls.Add(topPanel);

            _splitMain.Panel1MinSize = 330;
            _splitMain.Panel2MinSize = 360;
            _splitMain.SplitterDistance = 370;
            _splitMain.Panel1Collapsed = !_isDynamicMode || !openDesigner;
            _splitMain.ResumeLayout(false);
            ((ISupportInitialize)_splitMain).EndInit();

            Load += (s, e) =>
            {
                EnsureWebViewer();
                RenderCurrentHtmlToBrowser();
            };
        }

        private Button CreateToolbarButton(string text, int width, bool bold = false, bool visible = true)
        {
            return new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                Margin = new Padding(3, 2, 3, 2),
                Font = bold ? _boldFont : Font,
                Visible = visible
            };
        }

        private void BuildQuickPreviewStrip(SplitterPanel host)
        {
            _hiddenColumnsMenu = new ContextMenuStrip();

            _chipStrip = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                AutoScroll = true,
                WrapContents = false,
                AllowDrop = true,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(8, 4, 8, 4)
            };
            _chipStrip.DragOver += (s, e) =>
            {
                if (e.Data.GetData(typeof(ColumnDragPayload)) is ColumnDragPayload)
                {
                    e.Effect = DragDropEffects.Move;
                    _chipStrip.BackColor = Color.FromArgb(219, 234, 254);
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            };
            _chipStrip.DragLeave += (s, e) => _chipStrip.BackColor = Color.FromArgb(241, 245, 249);
            _chipStrip.DragDrop += (s, e) => HandleChipStripDrop(e);

            _btnQuickShowHidden = new Button
            {
                Text = "+ Hiện cột",
                AutoSize = true,
                Height = 26,
                Margin = new Padding(6, 1, 2, 1),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 64, 175)
            };
            _btnQuickShowHidden.FlatAppearance.BorderColor = Color.FromArgb(147, 197, 253);
            _btnQuickShowHidden.Click += (s, e) =>
            {
                _hiddenColumnsMenu.Items.Clear();
                if (_hiddenColumns.Count == 0)
                {
                    var emptyItem = new ToolStripMenuItem("(Không có cột nào đang ẩn)") { Enabled = false };
                    _hiddenColumnsMenu.Items.Add(emptyItem);
                }
                else
                {
                    foreach (var hCol in _hiddenColumns.ToList())
                    {
                        var item = new ToolStripMenuItem($"Hiện: {hCol.HeaderText} ({hCol.ColumnName})");
                        item.Click += (ms, me) => ShowColumn(hCol.ColumnName);
                        _hiddenColumnsMenu.Items.Add(item);
                    }
                }
                _hiddenColumnsMenu.Show(_btnQuickShowHidden, new Point(0, _btnQuickShowHidden.Height));
            };

            var inlineBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(10, 5, 10, 5),
                Width = 720
            };

            var lblQuickTitle = new Label { Text = "Tiêu đề:", Left = 10, Top = 9, Width = 54 };
            _txtQuickTitle = new TextBox
            {
                Left = 66,
                Top = 6,
                Width = 364,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _txtQuickTitle.TextChanged += (s, e) =>
            {
                if (_isSyncingUi) return;
                _reportTitle = _txtQuickTitle.Text;
                Text = string.IsNullOrWhiteSpace(_reportTitle) ? "Báo cáo thống kê" : _reportTitle;
                if (_txtTitle != null) SetControlText(_txtTitle, _reportTitle);
                SchedulePreviewRefresh();
            };

            _pnlQuickGroupDrop = new Panel
            {
                Left = 440,
                Top = 5,
                Width = 240,
                Height = 25,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(239, 246, 255),
                AllowDrop = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _lblQuickGroupDrop = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "» Kéo cột vào đây để gom nhóm",
                ForeColor = Color.FromArgb(30, 64, 175),
                AllowDrop = true
            };
            _pnlQuickGroupDrop.Controls.Add(_lblQuickGroupDrop);
            AttachGroupDropEvents(_pnlQuickGroupDrop);
            AttachGroupDropEvents(_lblQuickGroupDrop);

            _btnQuickClearGroup = new Button
            {
                Text = "✕",
                Left = 684,
                Top = 5,
                Width = 26,
                Height = 25,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnQuickClearGroup.Click += (s, e) => SetGroupByColumn(null);

            inlineBar.Controls.AddRange(new Control[] { lblQuickTitle, _txtQuickTitle, _pnlQuickGroupDrop, _btnQuickClearGroup });

            host.Controls.Add(_chipStrip);
            host.Controls.Add(inlineBar);
        }

        private void RebuildColumnChips()
        {
            if (_chipStrip == null) return;

            _chipStrip.SuspendLayout();
            try
            {
                var oldControls = _chipStrip.Controls.Cast<Control>().Where(c => c != _btnQuickShowHidden).ToList();
                _chipStrip.Controls.Clear();
                foreach (var old in oldControls) old.Dispose();

                _chipStrip.Controls.Add(new Label
                {
                    Text = "Cột (kéo xếp / bấm chỉnh):",
                    AutoSize = true,
                    Padding = new Padding(0, 6, 4, 0),
                    Font = _boldFont,
                    ForeColor = Color.FromArgb(51, 65, 85)
                });

                for (int i = 0; i < _visibleColumns.Count; i++)
                {
                    ReportColumn col = _visibleColumns[i];
                    bool isSelected = ReferenceEquals(col, _selectedVisibleColumn);

                    var chip = new Panel
                    {
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        Height = 26,
                        Margin = new Padding(2, 1, 4, 1),
                        Padding = new Padding(6, 3, 4, 3),
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = isSelected ? Color.FromArgb(37, 99, 235) : Color.White,
                        Tag = col,
                        AllowDrop = true,
                        Cursor = Cursors.Hand
                    };

                    var flow = new FlowLayoutPanel
                    {
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        FlowDirection = FlowDirection.LeftToRight,
                        WrapContents = false,
                        Margin = new Padding(0),
                        Padding = new Padding(0),
                        BackColor = Color.Transparent,
                        AllowDrop = true
                    };

                    string sumTag = col.SummaryBadge;
                    var lbl = new Label
                    {
                        Text = $"⋮⋮ {col.HeaderText}{sumTag}",
                        AutoSize = true,
                        ForeColor = isSelected ? Color.White : Color.FromArgb(15, 23, 42),
                        Padding = new Padding(0, 2, 2, 0),
                        AllowDrop = true,
                        Cursor = Cursors.Hand
                    };

                    var btnMenu = new Label
                    {
                        Text = "▾",
                        AutoSize = true,
                        ForeColor = isSelected ? Color.FromArgb(226, 232, 240) : Color.FromArgb(71, 85, 105),
                        Padding = new Padding(2, 1, 2, 0),
                        Cursor = Cursors.Hand
                    };
                    btnMenu.Click += (s, e) =>
                    {
                        SelectVisibleColumn(col);
                        ShowColumnActionMenu(col, btnMenu.PointToScreen(new Point(0, btnMenu.Height)));
                    };

                    var btnHide = new Label
                    {
                        Text = "×",
                        AutoSize = true,
                        ForeColor = isSelected ? Color.FromArgb(254, 226, 226) : Color.FromArgb(185, 28, 28),
                        Font = _boldFont,
                        Padding = new Padding(2, 1, 2, 0),
                        Cursor = Cursors.Hand
                    };
                    btnHide.Click += (s, e) => HideColumn(col.ColumnName);

                    AttachChipEvents(chip, col, i);
                    AttachChipEvents(flow, col, i);
                    AttachChipEvents(lbl, col, i);

                    flow.Controls.Add(lbl);
                    flow.Controls.Add(btnMenu);
                    flow.Controls.Add(btnHide);
                    chip.Controls.Add(flow);
                    _chipStrip.Controls.Add(chip);
                }

                _btnQuickShowHidden.Text = _hiddenColumns.Count > 0
                    ? $"+ Hiện cột ({_hiddenColumns.Count})"
                    : "+ Hiện cột";
                _btnQuickShowHidden.Enabled = _hiddenColumns.Count > 0;
                _chipStrip.Controls.Add(_btnQuickShowHidden);
            }
            finally
            {
                _chipStrip.ResumeLayout(true);
            }
        }

        private void AttachChipEvents(Control ctrl, ReportColumn col, int index)
        {
            ctrl.MouseUp += (s, e) =>
            {
                _pendingDragChipCtrl = null;
                _pendingDragChipCol = null;
                _pendingDragChipIdx = -1;
                if (e.Button == MouseButtons.Right)
                {
                    SelectVisibleColumn(col);
                    ShowColumnActionMenu(col, ctrl.PointToScreen(e.Location));
                }
            };

            ctrl.Click += (s, e) => SelectVisibleColumn(col);

            ctrl.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                _dragStartPoint = e.Location;
                _pendingDragChipCtrl = ctrl;
                _pendingDragChipCol = col;
                _pendingDragChipIdx = index;
            };

            ctrl.MouseMove += (s, e) =>
            {
                if ((e.Button & MouseButtons.Left) != MouseButtons.Left || _pendingDragChipCtrl != ctrl || _pendingDragChipCol == null)
                    return;

                if (Math.Abs(e.X - _dragStartPoint.X) < 5 && Math.Abs(e.Y - _dragStartPoint.Y) < 5)
                    return;

                var dragCol = _pendingDragChipCol;
                int dragIdx = _pendingDragChipIdx;
                _pendingDragChipCtrl = null;
                _pendingDragChipCol = null;
                _pendingDragChipIdx = -1;

                ctrl.DoDragDrop(new ColumnDragPayload(dragCol, _lstVisible, dragIdx), DragDropEffects.Move);
            };

            ctrl.DragOver += (s, e) =>
            {
                e.Effect = e.Data.GetData(typeof(ColumnDragPayload)) is ColumnDragPayload
                    ? DragDropEffects.Move
                    : DragDropEffects.None;
            };

            ctrl.DragDrop += (s, e) => HandleChipStripDrop(e);
        }

        private void HandleChipStripDrop(DragEventArgs e)
        {
            if (_chipStrip != null) _chipStrip.BackColor = Color.FromArgb(241, 245, 249);
            if (!(e.Data.GetData(typeof(ColumnDragPayload)) is ColumnDragPayload payload))
                return;

            Point pt = _chipStrip.PointToClient(new Point(e.X, e.Y));
            int insertIdx = _visibleColumns.Count;
            int chipOrdinal = 0;
            foreach (Control c in _chipStrip.Controls)
            {
                if (c is Panel && c.Tag is ReportColumn)
                {
                    if (pt.X < c.Left + c.Width / 2)
                    {
                        insertIdx = chipOrdinal;
                        break;
                    }
                    chipOrdinal++;
                }
            }

            if (_visibleColumns.Any(c => string.Equals(c.ColumnName, payload.Column.ColumnName, StringComparison.OrdinalIgnoreCase)))
                MoveVisibleColumn(payload.Column.ColumnName, insertIdx);
            else
                ShowColumn(payload.Column.ColumnName, insertIdx);
        }

        private void BuildDesignerPanel(SplitterPanel host)
        {
            host.BackColor = Color.FromArgb(249, 250, 252);
            host.Padding = new Padding(6);

            _designerTabs = new TabControl { Dock = DockStyle.Fill };

            var tabColumns = new TabPage("Cột & Nhóm")
            {
                BackColor = Color.FromArgb(249, 250, 252),
                Padding = new Padding(6)
            };
            BuildColumnsAndGroupTab(tabColumns);

            var tabHeaderPage = new TabPage("Tiêu đề & Trang")
            {
                BackColor = Color.FromArgb(249, 250, 252),
                Padding = new Padding(8),
                AutoScroll = true
            };
            BuildHeaderAndPageTab(tabHeaderPage);

            _designerTabs.TabPages.Add(tabColumns);
            _designerTabs.TabPages.Add(tabHeaderPage);
            host.Controls.Add(_designerTabs);
        }

        private void BuildColumnsAndGroupTab(TabPage host)
        {
            // A. Gom nhóm
            var grpGroup = new GroupBox
            {
                Text = "Gom nhóm (Kéo thả cột vào đây)",
                Dock = DockStyle.Top,
                Height = 96,
                Padding = new Padding(8, 6, 8, 6)
            };

            _pnlGroupDrop = new Panel
            {
                Left = 10,
                Top = 20,
                Width = 315,
                Height = 32,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(239, 246, 255),
                AllowDrop = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _lblGroupDrop = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "» Kéo 1 cột thả vào đây để gom nhóm",
                ForeColor = Color.FromArgb(30, 64, 175),
                AllowDrop = true
            };
            _pnlGroupDrop.Controls.Add(_lblGroupDrop);
            AttachGroupDropEvents(_pnlGroupDrop);
            AttachGroupDropEvents(_lblGroupDrop);

            _cboGroupBy = new ComboBox
            {
                Left = 10,
                Top = 59,
                Width = 128,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboGroupBy.SelectedIndexChanged += (s, e) =>
            {
                if (_isSyncingUi) return;
                var item = _cboGroupBy.SelectedItem as GroupOptionItem;
                SetGroupByColumn(item?.ColumnName);
            };

            _btnClearGroup = new Button { Text = "✕", Left = 142, Top = 58, Width = 28, Height = 24 };
            _btnClearGroup.Click += (s, e) => SetGroupByColumn(null);

            grpGroup.Controls.Add(new Label { Text = "Nhãn:", Left = 175, Top = 62, Width = 40 });
            _txtGroupPrefix = new TextBox { Left = 216, Top = 59, Width = 109, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _txtGroupPrefix.TextChanged += (s, e) =>
            {
                if (_isSyncingUi) return;
                _groupPrefix = _txtGroupPrefix.Text;
                SchedulePreviewRefresh();
            };

            grpGroup.Controls.AddRange(new Control[] { _pnlGroupDrop, _cboGroupBy, _btnClearGroup, _txtGroupPrefix });

            // B. Sửa cột đang chọn (Dock Bottom)
            var grpColProps = new GroupBox
            {
                Text = "Cột đang chọn",
                Dock = DockStyle.Bottom,
                Height = 134,
                Padding = new Padding(8, 6, 8, 6)
            };

            _lblEditingCol = new Label
            {
                Left = 10,
                Top = 20,
                Width = 315,
                Text = "Chọn 1 cột bên 'Hiện' để sửa",
                Font = new Font(Font, FontStyle.Italic),
                ForeColor = Color.FromArgb(75, 85, 99),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            grpColProps.Controls.Add(_lblEditingCol);

            grpColProps.Controls.Add(new Label { Text = "Tên cột:", Left = 10, Top = 47, Width = 68 });
            _txtColHeader = new TextBox { Left = 82, Top = 44, Width = 243, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _txtColHeader.TextChanged += (s, e) =>
            {
                if (_isSyncingUi || _selectedVisibleColumn == null) return;
                _selectedVisibleColumn.HeaderText = _txtColHeader.Text;
                _lstVisible.Invalidate();
                RebuildColumnChips();
                SchedulePreviewRefresh();
            };
            grpColProps.Controls.Add(_txtColHeader);

            grpColProps.Controls.Add(new Label { Text = "Căn lề:", Left = 10, Top = 76, Width = 50 });
            _cboColAlign = new ComboBox { Left = 62, Top = 73, Width = 84, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboColAlign.Items.AddRange(new object[] { "Trái", "Giữa", "Phải" });
            _cboColAlign.SelectedIndexChanged += (s, e) =>
            {
                if (_isSyncingUi || _selectedVisibleColumn == null) return;
                _selectedVisibleColumn.Alignment = _cboColAlign.SelectedIndex == 1
                    ? HorizontalAlignment.Center
                    : (_cboColAlign.SelectedIndex == 2 ? HorizontalAlignment.Right : HorizontalAlignment.Left);
                RebuildHtml(immediate: true);
            };
            grpColProps.Controls.Add(_cboColAlign);

            grpColProps.Controls.Add(new Label { Text = "Định dạng:", Left = 152, Top = 76, Width = 66 });
            _cboColFormat = new ComboBox { Left = 220, Top = 73, Width = 105, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _cboColFormat.Items.AddRange(new object[] { "", "N0", "N2", "0.##", "dd/MM/yyyy", "dd/MM/yyyy HH:mm" });
            _cboColFormat.TextChanged += (s, e) =>
            {
                if (_isSyncingUi || _selectedVisibleColumn == null) return;
                _selectedVisibleColumn.Format = string.IsNullOrWhiteSpace(_cboColFormat.Text) ? null : _cboColFormat.Text.Trim();
                SchedulePreviewRefresh();
            };
            grpColProps.Controls.Add(_cboColFormat);

            grpColProps.Controls.Add(new Label { Text = "Thống kê:", Left = 10, Top = 104, Width = 68 });
            _cboColSummary = new ComboBox
            {
                Left = 82,
                Top = 101,
                Width = 243,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _cboColSummary.Items.AddRange(new object[]
            {
                "Không thống kê",
                "Tổng (SUM)",
                "Trung bình (AVG)",
                "Đếm dòng (COUNT)",
                "Lớn nhất (MAX)",
                "Nhỏ nhất (MIN)"
            });
            _cboColSummary.SelectedIndexChanged += (s, e) =>
            {
                if (_isSyncingUi || _selectedVisibleColumn == null || _cboColSummary.SelectedIndex < 0) return;
                _selectedVisibleColumn.SummaryMode = (ReportSummaryMode)_cboColSummary.SelectedIndex;
                _lstVisible.Invalidate();
                RebuildColumnChips();
                RebuildHtml(immediate: true);
            };
            grpColProps.Controls.Add(_cboColSummary);

            // C. Danh sách Cột Hiện / Ẩn (Dock Fill — thoáng chiều cao tối đa)
            var grpColumns = new GroupBox
            {
                Text = "Kéo thả cột (Ẩn / Hiện / Đổi thứ tự)",
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 6, 8, 6)
            };

            var colLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };
            colLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56f));
            colLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f));
            colLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            colLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var barVis = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
            barVis.Controls.Add(new Label { Text = "Hiện:", AutoSize = true, Padding = new Padding(0, 5, 3, 0), Font = _boldFont, ForeColor = Color.FromArgb(22, 101, 52) });
            _btnMoveUp = new Button { Text = "▲", Width = 26, Height = 23, Margin = new Padding(2, 2, 1, 0) };
            _btnMoveDown = new Button { Text = "▼", Width = 26, Height = 23, Margin = new Padding(1, 2, 1, 0) };
            _btnHideCol = new Button { Text = "Ẩn →", Width = 48, Height = 23, Margin = new Padding(3, 2, 0, 0) };
            _btnMoveUp.Click += (s, e) => MoveSelectedVisible(-1);
            _btnMoveDown.Click += (s, e) => MoveSelectedVisible(1);
            _btnHideCol.Click += (s, e) =>
            {
                if (_lstVisible.SelectedItem is ReportColumn col) HideColumn(col.ColumnName);
            };
            barVis.Controls.AddRange(new Control[] { _btnMoveUp, _btnMoveDown, _btnHideCol });

            var barHid = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(4, 0, 0, 0) };
            barHid.Controls.Add(new Label { Text = "Ẩn:", AutoSize = true, Padding = new Padding(0, 5, 3, 0), Font = _boldFont, ForeColor = Color.FromArgb(153, 27, 27) });
            _btnShowCol = new Button { Text = "← Hiện", Width = 58, Height = 23, Margin = new Padding(2, 2, 0, 0) };
            _btnShowCol.Click += (s, e) =>
            {
                if (_lstHidden.SelectedItem is ReportColumn col) ShowColumn(col.ColumnName);
            };
            barHid.Controls.Add(_btnShowCol);

            _lstVisible = CreateColumnListBox(isVisibleList: true);
            _lstVisible.Margin = new Padding(0, 2, 4, 0);
            _lstVisible.SelectedIndexChanged += (s, e) =>
            {
                if (_isSyncingUi) return;
                _selectedVisibleColumn = _lstVisible.SelectedItem as ReportColumn;
                SyncSelectedColumnEditor();
                RebuildColumnChips();
            };
            _lstVisible.DoubleClick += (s, e) =>
            {
                if (_lstVisible.SelectedItem is ReportColumn col) HideColumn(col.ColumnName);
            };

            _lstHidden = CreateColumnListBox(isVisibleList: false);
            _lstHidden.Margin = new Padding(4, 2, 0, 0);
            _lstHidden.DoubleClick += (s, e) =>
            {
                if (_lstHidden.SelectedItem is ReportColumn col) ShowColumn(col.ColumnName);
            };

            colLayout.Controls.Add(barVis, 0, 0);
            colLayout.Controls.Add(barHid, 1, 0);
            colLayout.Controls.Add(_lstVisible, 0, 1);
            colLayout.Controls.Add(_lstHidden, 1, 1);
            grpColumns.Controls.Add(colLayout);

            host.Controls.Add(grpColumns);
            host.Controls.Add(grpGroup);
            host.Controls.Add(grpColProps);
        }

        private void BuildHeaderAndPageTab(TabPage host)
        {
            var grpInfo = new GroupBox
            {
                Text = "Thông tin đầu & cuối trang",
                Dock = DockStyle.Top,
                Height = 205,
                Padding = new Padding(10, 8, 10, 8)
            };

            int y = 26;
            grpInfo.Controls.Add(new Label { Text = "Tiêu đề:", Left = 10, Top = y + 3, Width = 82 });
            _txtTitle = new TextBox { Left = 96, Top = y, Width = 225, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _txtTitle.TextChanged += (s, e) =>
            {
                if (_isSyncingUi) return;
                _reportTitle = _txtTitle.Text;
                Text = string.IsNullOrWhiteSpace(_reportTitle) ? "Báo cáo thống kê" : _reportTitle;
                if (_txtQuickTitle != null) SetControlText(_txtQuickTitle, _reportTitle);
                SchedulePreviewRefresh();
            };
            grpInfo.Controls.Add(_txtTitle);

            y += 32;
            grpInfo.Controls.Add(new Label { Text = "Đơn vị:", Left = 10, Top = y + 3, Width = 82 });
            _txtCompany = new TextBox { Left = 96, Top = y, Width = 225, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _txtCompany.TextChanged += (s, e) =>
            {
                if (_isSyncingUi) return;
                _companyName = _txtCompany.Text;
                SchedulePreviewRefresh();
            };
            grpInfo.Controls.Add(_txtCompany);

            y += 32;
            grpInfo.Controls.Add(new Label { Text = "Điều kiện lọc:", Left = 10, Top = y + 3, Width = 82 });
            _txtFilter = new TextBox { Left = 96, Top = y, Width = 225, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _txtFilter.TextChanged += (s, e) =>
            {
                if (_isSyncingUi) return;
                _filterCondition = _txtFilter.Text;
                SchedulePreviewRefresh();
            };
            grpInfo.Controls.Add(_txtFilter);

            y += 32;
            grpInfo.Controls.Add(new Label { Text = "Người ký:", Left = 10, Top = y + 3, Width = 82 });
            _txtSigner = new TextBox { Left = 96, Top = y, Width = 225, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _txtSigner.TextChanged += (s, e) =>
            {
                if (_isSyncingUi) return;
                _signerTitle = _txtSigner.Text;
                SchedulePreviewRefresh();
            };
            grpInfo.Controls.Add(_txtSigner);

            y += 32;
            grpInfo.Controls.Add(new Label { Text = "Chân trang:", Left = 10, Top = y + 3, Width = 82 });
            _txtFooter = new TextBox { Left = 96, Top = y, Width = 225, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _txtFooter.TextChanged += (s, e) =>
            {
                if (_isSyncingUi) return;
                _pageFooter = _txtFooter.Text;
                SchedulePreviewRefresh();
            };
            grpInfo.Controls.Add(_txtFooter);

            var grpPage = new GroupBox
            {
                Text = "Khổ giấy & Tùy chọn hiển thị",
                Dock = DockStyle.Top,
                Height = 140,
                Padding = new Padding(10, 8, 10, 8)
            };

            grpPage.Controls.Add(new Label { Text = "Khổ giấy:", Left = 10, Top = 29, Width = 68 });
            _cboPaperOrientation = new ComboBox { Left = 82, Top = 26, Width = 112, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboPaperOrientation.Items.AddRange(new object[] { "A4 Dọc", "A4 Ngang" });
            _cboPaperOrientation.SelectedIndex = 0;
            _cboPaperOrientation.SelectedIndexChanged += (s, e) =>
            {
                if (_isSyncingUi) return;
                _landscape = _cboPaperOrientation.SelectedIndex == 1;
                RebuildHtml(immediate: true);
            };
            grpPage.Controls.Add(_cboPaperOrientation);

            grpPage.Controls.Add(new Label { Text = "Cỡ chữ:", Left = 204, Top = 29, Width = 52 });
            _nudFontSize = new NumericUpDown
            {
                Left = 260,
                Top = 26,
                Width = 60,
                Minimum = 9,
                Maximum = 22,
                Value = 13
            };
            _nudFontSize.ValueChanged += (s, e) =>
            {
                if (_isSyncingUi) return;
                _fontSizePt = (int)_nudFontSize.Value;
                SchedulePreviewRefresh();
            };
            grpPage.Controls.Add(_nudFontSize);

            _chkShowStt = new CheckBox { Text = "Hiển thị cột số thứ tự (STT)", Left = 12, Top = 64, Width = 290, Checked = true };
            _chkShowStt.CheckedChanged += (s, e) =>
            {
                if (_isSyncingUi) return;
                _showStt = _chkShowStt.Checked;
                RebuildHtml(immediate: true);
            };

            _chkGroupTotals = new CheckBox { Text = "Hiển thị dòng tổng phụ ở cuối mỗi nhóm", Left = 12, Top = 92, Width = 295 };
            _chkGroupTotals.CheckedChanged += (s, e) =>
            {
                if (_isSyncingUi) return;
                _showGroupTotals = _chkGroupTotals.Checked;
                RebuildHtml(immediate: true);
            };
            grpPage.Controls.AddRange(new Control[] { _chkShowStt, _chkGroupTotals });

            host.Controls.Add(grpPage);
            host.Controls.Add(grpInfo);
        }

        private ListBox CreateColumnListBox(bool isVisibleList)
        {
            var lb = new ListBox
            {
                Dock = DockStyle.Fill,
                AllowDrop = true,
                IntegralHeight = false,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 24
            };

            lb.DrawItem += (s, e) =>
            {
                if (e.Index < 0 || e.Index >= lb.Items.Count) return;
                var col = lb.Items[e.Index] as ReportColumn;
                if (col == null) return;

                bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color bg = selected ? Color.FromArgb(37, 99, 235) : lb.BackColor;
                Color fg = selected ? Color.White : (isVisibleList ? Color.FromArgb(17, 24, 39) : Color.FromArgb(107, 114, 128));

                using (var bgBrush = new SolidBrush(bg))
                {
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);
                }

                string sumTag = col.SummaryBadge;
                string text = isVisibleList
                    ? $"⋮⋮ {col.HeaderText} ({col.ColumnName}){sumTag}"
                    : $"⋮⋮ {col.ColumnName} ({col.HeaderText})";

                TextRenderer.DrawText(e.Graphics, text, lb.Font,
                    new Rectangle(e.Bounds.X + 2, e.Bounds.Y + 3, e.Bounds.Width - 4, e.Bounds.Height - 4),
                    fg, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

                e.DrawFocusRectangle();
            };

            lb.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                int idx = lb.IndexFromPoint(e.Location);
                if (idx >= 0 && idx < lb.Items.Count)
                {
                    _dragStartPoint = e.Location;
                    _pendingDragList = lb;
                    _pendingDragIndex = idx;
                }
                else
                {
                    _pendingDragIndex = -1;
                }
            };

            lb.MouseMove += (s, e) =>
            {
                if ((e.Button & MouseButtons.Left) != MouseButtons.Left || _pendingDragIndex < 0 || _pendingDragList != lb)
                    return;

                if (Math.Abs(e.X - _dragStartPoint.X) < 5 && Math.Abs(e.Y - _dragStartPoint.Y) < 5)
                    return;

                int idx = _pendingDragIndex;
                _pendingDragIndex = -1;

                if (idx < 0 || idx >= lb.Items.Count) return;
                if (lb.Items[idx] is ReportColumn col)
                {
                    lb.SelectedIndex = idx;
                    lb.DoDragDrop(new ColumnDragPayload(col, lb, idx), DragDropEffects.Move);
                }
            };

            lb.MouseUp += (s, e) => _pendingDragIndex = -1;

            lb.DragOver += (s, e) =>
            {
                if (e.Data.GetData(typeof(ColumnDragPayload)) is ColumnDragPayload)
                {
                    e.Effect = DragDropEffects.Move;
                    lb.BackColor = Color.FromArgb(240, 249, 255);
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            };

            lb.DragLeave += (s, e) => lb.BackColor = SystemColors.Window;

            lb.DragDrop += (s, e) =>
            {
                lb.BackColor = SystemColors.Window;
                if (!(e.Data.GetData(typeof(ColumnDragPayload)) is ColumnDragPayload payload))
                    return;

                Point pt = lb.PointToClient(new Point(e.X, e.Y));
                int dropIdx = lb.IndexFromPoint(pt);
                if (dropIdx == ListBox.NoMatches || dropIdx < 0)
                    dropIdx = lb.Items.Count;

                if (isVisibleList)
                {
                    if (payload.SourceList == _lstVisible)
                        MoveVisibleColumn(payload.Column.ColumnName, dropIdx);
                    else
                        ShowColumn(payload.Column.ColumnName, dropIdx);
                }
                else
                {
                    if (payload.SourceList == _lstVisible)
                        HideColumn(payload.Column.ColumnName, dropIdx);
                    else
                        MoveHiddenColumn(payload.Column.ColumnName, dropIdx);
                }
            };

            return lb;
        }

        private void AttachGroupDropEvents(Control ctrl)
        {
            ctrl.DragOver += (s, e) =>
            {
                if (e.Data.GetData(typeof(ColumnDragPayload)) is ColumnDragPayload payload)
                {
                    e.Effect = DragDropEffects.Move;
                    if (_pnlGroupDrop != null)
                    {
                        _pnlGroupDrop.BackColor = Color.FromArgb(191, 219, 254);
                        _lblGroupDrop.Text = $"» Thả để nhóm theo: {payload.Column.HeaderText}";
                    }
                    if (_pnlQuickGroupDrop != null)
                    {
                        _pnlQuickGroupDrop.BackColor = Color.FromArgb(191, 219, 254);
                        _lblQuickGroupDrop.Text = $"» Thả nhóm: {payload.Column.HeaderText}";
                    }
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            };

            ctrl.DragLeave += (s, e) => SyncGroupDropVisual();

            ctrl.DragDrop += (s, e) =>
            {
                if (e.Data.GetData(typeof(ColumnDragPayload)) is ColumnDragPayload payload)
                {
                    SetGroupByColumn(payload.Column.ColumnName);
                }
                SyncGroupDropVisual();
            };
        }

        private void MoveSelectedVisible(int delta)
        {
            if (_lstVisible?.SelectedItem is ReportColumn col)
            {
                int currentIdx = _visibleColumns.IndexOf(col);
                if (currentIdx >= 0)
                {
                    MoveVisibleColumn(col.ColumnName, currentIdx + delta);
                }
            }
        }

        private void SyncAllUiFromState()
        {
            if (_txtTitle == null) return;

            _isSyncingUi = true;
            try
            {
                _txtTitle.Text = _reportTitle ?? "";
                if (_txtQuickTitle != null) _txtQuickTitle.Text = _reportTitle ?? "";
                _txtCompany.Text = _companyName ?? "";
                _txtFilter.Text = _filterCondition ?? "";
                _txtSigner.Text = _signerTitle ?? "";
                _txtFooter.Text = _pageFooter ?? "";
                _chkShowStt.Checked = _showStt;
                _chkGroupTotals.Checked = _showGroupTotals;
                if (_cboPaperOrientation != null)
                    _cboPaperOrientation.SelectedIndex = _landscape ? 1 : 0;
                if (_nudFontSize != null)
                    _nudFontSize.Value = Clamp(_fontSizePt, (int)_nudFontSize.Minimum, (int)_nudFontSize.Maximum);
                _txtGroupPrefix.Text = _groupPrefix ?? "";

                _cboGroupBy.Items.Clear();
                _cboGroupBy.Items.Add(new GroupOptionItem(null, "(Không gom nhóm)"));
                foreach (DataColumn dc in _sourceTable.Columns)
                {
                    _cboGroupBy.Items.Add(new GroupOptionItem(dc.ColumnName, dc.ColumnName));
                }
            }
            finally
            {
                _isSyncingUi = false;
            }

            SyncGroupControls();
            if (_selectedVisibleColumn == null || !_visibleColumns.Contains(_selectedVisibleColumn))
                _selectedVisibleColumn = _visibleColumns.FirstOrDefault();

            RefreshColumnListsUi(_selectedVisibleColumn?.ColumnName, selectInVisible: true);
        }

        private void SyncGroupControls()
        {
            if (_cboGroupBy == null) return;

            _isSyncingUi = true;
            try
            {
                int selectIdx = 0;
                if (!string.IsNullOrEmpty(_groupByColumn))
                {
                    for (int i = 1; i < _cboGroupBy.Items.Count; i++)
                    {
                        if (_cboGroupBy.Items[i] is GroupOptionItem opt &&
                            string.Equals(opt.ColumnName, _groupByColumn, StringComparison.OrdinalIgnoreCase))
                        {
                            selectIdx = i;
                            break;
                        }
                    }
                }
                _cboGroupBy.SelectedIndex = selectIdx;
                if (_txtGroupPrefix != null && _txtGroupPrefix.Text != (_groupPrefix ?? ""))
                    _txtGroupPrefix.Text = _groupPrefix ?? "";
                if (_btnClearGroup != null)
                    _btnClearGroup.Enabled = !string.IsNullOrEmpty(_groupByColumn);
                if (_btnQuickClearGroup != null)
                    _btnQuickClearGroup.Enabled = !string.IsNullOrEmpty(_groupByColumn);
            }
            finally
            {
                _isSyncingUi = false;
            }

            SyncGroupDropVisual();
        }

        private void SyncGroupDropVisual()
        {
            bool empty = string.IsNullOrEmpty(_groupByColumn);
            Color bg = empty ? Color.FromArgb(239, 246, 255) : Color.FromArgb(220, 252, 231);
            Color fg = empty ? Color.FromArgb(30, 64, 175) : Color.FromArgb(22, 101, 52);

            if (_pnlGroupDrop != null && _lblGroupDrop != null)
            {
                _pnlGroupDrop.BackColor = bg;
                _lblGroupDrop.ForeColor = fg;
                _lblGroupDrop.Text = empty
                    ? "» Kéo 1 cột thả vào đây để gom nhóm"
                    : $"✓ Đang gom nhóm theo: {FindColumnHeader(_groupByColumn)} ({_groupByColumn})";
            }

            if (_pnlQuickGroupDrop != null && _lblQuickGroupDrop != null)
            {
                _pnlQuickGroupDrop.BackColor = bg;
                _lblQuickGroupDrop.ForeColor = fg;
                _lblQuickGroupDrop.Text = empty
                    ? "» Kéo cột vào đây để gom nhóm"
                    : $"✓ Nhóm: {FindColumnHeader(_groupByColumn)}";
            }
        }

        private void RefreshColumnListsUi(string focusColumnName, bool selectInVisible)
        {
            if (_lstVisible == null || _lstHidden == null) return;

            _isSyncingUi = true;
            try
            {
                _lstVisible.BeginUpdate();
                _lstVisible.Items.Clear();
                foreach (var col in _visibleColumns) _lstVisible.Items.Add(col);
                _lstVisible.EndUpdate();

                _lstHidden.BeginUpdate();
                _lstHidden.Items.Clear();
                foreach (var col in _hiddenColumns) _lstHidden.Items.Add(col);
                _lstHidden.EndUpdate();

                if (!string.IsNullOrEmpty(focusColumnName))
                {
                    if (selectInVisible)
                    {
                        int idx = _visibleColumns.FindIndex(c => string.Equals(c.ColumnName, focusColumnName, StringComparison.OrdinalIgnoreCase));
                        if (idx >= 0)
                        {
                            _lstVisible.SelectedIndex = idx;
                            _selectedVisibleColumn = _visibleColumns[idx];
                        }
                    }
                    else
                    {
                        int idx = _hiddenColumns.FindIndex(c => string.Equals(c.ColumnName, focusColumnName, StringComparison.OrdinalIgnoreCase));
                        if (idx >= 0) _lstHidden.SelectedIndex = idx;
                    }
                }
                else if (_visibleColumns.Count > 0)
                {
                    _lstVisible.SelectedIndex = 0;
                    _selectedVisibleColumn = _visibleColumns[0];
                }
            }
            finally
            {
                _isSyncingUi = false;
            }

            SyncSelectedColumnEditor();
            RebuildColumnChips();
        }

        private void SyncSelectedColumnEditor()
        {
            if (_lblEditingCol == null) return;

            bool hasCol = _selectedVisibleColumn != null;
            _txtColHeader.Enabled = hasCol;
            _cboColAlign.Enabled = hasCol;
            _cboColFormat.Enabled = hasCol;
            _cboColSummary.Enabled = hasCol;

            _isSyncingUi = true;
            try
            {
                if (!hasCol)
                {
                    _lblEditingCol.Text = "Chọn 1 cột bên 'Hiện' để sửa";
                    _txtColHeader.Text = "";
                    _cboColAlign.SelectedIndex = 0;
                    _cboColFormat.Text = "";
                    _cboColSummary.SelectedIndex = 0;
                    return;
                }

                _lblEditingCol.Text = $"Đang sửa cột: {_selectedVisibleColumn.ColumnName}";
                _txtColHeader.Text = _selectedVisibleColumn.HeaderText ?? "";
                _cboColAlign.SelectedIndex = _selectedVisibleColumn.Alignment == HorizontalAlignment.Center
                    ? 1
                    : (_selectedVisibleColumn.Alignment == HorizontalAlignment.Right ? 2 : 0);
                _cboColFormat.Text = _selectedVisibleColumn.Format ?? "";
                _cboColSummary.SelectedIndex = Clamp((int)_selectedVisibleColumn.SummaryMode, 0, _cboColSummary.Items.Count - 1);
            }
            finally
            {
                _isSyncingUi = false;
            }
        }

        private void SetControlText(TextBox box, string value)
        {
            _isSyncingUi = true;
            box.Text = value ?? "";
            _isSyncingUi = false;
        }

        private void SchedulePreviewRefresh()
        {
            _previewDirty = true;
            if (_debounceTimer != null)
            {
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
            else
            {
                RebuildHtml(immediate: true);
            }
        }

        private void RebuildHtml(bool immediate)
        {
            if (!_isDynamicMode) return;
            _previewDirty = false;

            if (_visibleColumns.Count == 0)
            {
                _currentHtml = "<!DOCTYPE html><html><head><meta charset='utf-8'></head>" +
                    "<body style=\"font-family:'Segoe UI',sans-serif;padding:40px;text-align:center;color:#475569;\">" +
                    "<h3>Chưa chọn cột hiển thị</h3>" +
                    "<p>Hãy kéo thả cột từ khung <b>Ẩn</b> sang khung <b>Hiện</b> (hoặc bấm <b>+ Hiện cột</b>) để xem báo cáo.</p>" +
                    "</body></html>";
            }
            else
            {
                _currentHtml = ReportHelper.GenerateHtmlCore(
                    table: _sourceTable,
                    reportTitle: _reportTitle,
                    filterCondition: _filterCondition,
                    groupByColumn: _groupByColumn,
                    groupPrefix: _groupPrefix,
                    columns: _visibleColumns,
                    companyName: _companyName,
                    signerTitle: _signerTitle,
                    showGroupTotals: _showGroupTotals,
                    pageFooter: _pageFooter,
                    dateFormat: _dateFormat,
                    showStt: _showStt,
                    landscape: _landscape,
                    fontSizePt: _fontSizePt,
                    interactivePreview: false);
            }

            if (immediate)
            {
                RenderCurrentHtmlToBrowser();
            }
        }

        private bool EnsurePrintable()
        {
            if (_previewDirty)
            {
                _debounceTimer?.Stop();
                RebuildHtml(immediate: true);
            }

            if (_isDynamicMode && _visibleColumns.Count == 0)
            {
                MessageHelper.Warning("Vui lòng chọn ít nhất 1 cột hiển thị trước khi in hoặc xuất báo cáo.");
                return false;
            }
            return true;
        }

        private void EnsureWebViewer()
        {
            if (_webViewer != null || _previewHost == null) return;
            if (System.Threading.Thread.CurrentThread.GetApartmentState() != System.Threading.ApartmentState.STA) return;

            _webViewer = new WebBrowser
            {
                Dock = DockStyle.Fill,
                IsWebBrowserContextMenuEnabled = true
            };

            if (_isDynamicMode)
            {
                try
                {
                    _webViewer.ObjectForScripting = new ReportPreviewBridge(this);
                }
                catch
                {
                    // Bỏ qua nếu môi trường giới hạn ActiveX scripting; thanh chip và tab thiết kế vẫn hoạt động đầy đủ.
                }
            }

            _previewHost.Controls.Add(_webViewer);
        }

        private void RenderCurrentHtmlToBrowser()
        {
            if (_webViewer == null) return;

            if (_isDynamicMode && _visibleColumns.Count > 0)
            {
                _webViewer.DocumentText = ReportHelper.GenerateHtmlCore(
                    table: _sourceTable,
                    reportTitle: _reportTitle,
                    filterCondition: _filterCondition,
                    groupByColumn: _groupByColumn,
                    groupPrefix: _groupPrefix,
                    columns: _visibleColumns,
                    companyName: _companyName,
                    signerTitle: _signerTitle,
                    showGroupTotals: _showGroupTotals,
                    pageFooter: _pageFooter,
                    dateFormat: _dateFormat,
                    showStt: _showStt,
                    landscape: _landscape,
                    fontSizePt: _fontSizePt,
                    interactivePreview: true);
            }
            else
            {
                _webViewer.DocumentText = _currentHtml ?? "";
            }
        }

        private static int Clamp(int val, int min, int max) => val < min ? min : (val > max ? max : val);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = null;
                _hiddenColumnsMenu?.Dispose();
                _hiddenColumnsMenu = null;
                _columnActionMenu?.Dispose();
                _columnActionMenu = null;
                _boldFont?.Dispose();
                _boldFont = null;
            }
            base.Dispose(disposing);
        }
    }
}
