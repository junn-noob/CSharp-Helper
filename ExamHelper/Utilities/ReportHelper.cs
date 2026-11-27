using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ExamHelper.Database;

namespace ExamHelper.Utilities
{
    /// <summary>
    /// Chế độ tính toán thống kê cuối nhóm / cuối bảng cho một cột báo cáo.
    /// </summary>
    public enum ReportSummaryMode
    {
        None = 0,
        Sum = 1,
        Average = 2,
        Count = 3,
        Max = 4,
        Min = 5
    }

    internal sealed class SummaryBucket
    {
        public decimal Sum;
        public int Count;
        public decimal? Min;
        public decimal? Max;

        public void Add(object rawValue, ReportSummaryMode mode)
        {
            if (rawValue == null || rawValue == DBNull.Value) return;
            if (mode == ReportSummaryMode.Count)
            {
                if (!(rawValue is string s) || !string.IsNullOrWhiteSpace(s))
                    Count++;
                return;
            }

            if (!TryConvertDecimal(rawValue, out decimal d)) return;
            Sum += d;
            Count++;
            if (!Min.HasValue || d < Min.Value) Min = d;
            if (!Max.HasValue || d > Max.Value) Max = d;
        }

        public bool TryGetResult(ReportSummaryMode mode, out decimal value)
        {
            value = 0m;
            if (mode == ReportSummaryMode.None) return false;
            if (mode == ReportSummaryMode.Count)
            {
                value = Count;
                return true;
            }
            if (Count == 0) return false;
            switch (mode)
            {
                case ReportSummaryMode.Sum:
                    value = Sum;
                    return true;
                case ReportSummaryMode.Average:
                    value = Sum / Count;
                    return true;
                case ReportSummaryMode.Max:
                    value = Max.Value;
                    return true;
                case ReportSummaryMode.Min:
                    value = Min.Value;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryConvertDecimal(object val, out decimal num)
        {
            num = 0m;
            if (val is byte b) { num = b; return true; }
            if (val is short s) { num = s; return true; }
            if (val is int i) { num = i; return true; }
            if (val is long l) { num = l; return true; }
            if (val is decimal d) { num = d; return true; }
            if (val is double db)
            {
                if (double.IsNaN(db) || double.IsInfinity(db)) return false;
                num = Convert.ToDecimal(db);
                return true;
            }
            if (val is float fl)
            {
                if (float.IsNaN(fl) || float.IsInfinity(fl)) return false;
                num = Convert.ToDecimal(fl);
                return true;
            }
            string text = val.ToString();
            return !string.IsNullOrWhiteSpace(text) && DataHelper.TryParseDecimal(text, out num);
        }
    }

    /// <summary>
    /// Định nghĩa cấu hình một cột trong báo cáo HTML/Excel.
    /// </summary>
    public class ReportColumn
    {
        private ReportSummaryMode _summaryMode = ReportSummaryMode.None;

        /// <summary>Tên cột trong DataTable nguồn.</summary>
        public string ColumnName { get; set; }

        /// <summary>Tiêu đề hiển thị trên đầu cột báo cáo.</summary>
        public string HeaderText { get; set; }

        /// <summary>Định dạng dữ liệu (ví dụ: "N0", "dd/MM/yyyy", "0.##").</summary>
        public string Format { get; set; }

        /// <summary>Căn lề cột (Trái, Giữa, Phải).</summary>
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        /// <summary>Cột này có tính tổng (SUM) ở dòng Tổng cộng hay không.</summary>
        public bool IsSum
        {
            get => _summaryMode == ReportSummaryMode.Sum;
            set
            {
                if (value) _summaryMode = ReportSummaryMode.Sum;
                else if (_summaryMode == ReportSummaryMode.Sum) _summaryMode = ReportSummaryMode.None;
            }
        }

        /// <summary>Chế độ thống kê cuối nhóm / cuối bảng (Sum, Average, Count, Max, Min).</summary>
        public ReportSummaryMode SummaryMode
        {
            get => _summaryMode;
            set => _summaryMode = value;
        }

        internal string SummaryBadge
        {
            get
            {
                switch (_summaryMode)
                {
                    case ReportSummaryMode.Sum: return " [Σ]";
                    case ReportSummaryMode.Average: return " [TB]";
                    case ReportSummaryMode.Count: return " [Đếm]";
                    case ReportSummaryMode.Max: return " [Max]";
                    case ReportSummaryMode.Min: return " [Min]";
                    default: return "";
                }
            }
        }

        /// <summary>Khởi tạo cột báo cáo đầy đủ thông số.</summary>
        public ReportColumn(string columnName, string headerText = null, string format = null,
            HorizontalAlignment alignment = HorizontalAlignment.Left, bool isSum = false)
            : this(columnName, headerText, format, alignment, isSum ? ReportSummaryMode.Sum : ReportSummaryMode.None)
        {
        }

        /// <summary>Khởi tạo cột báo cáo với chế độ thống kê chỉ định.</summary>
        public ReportColumn(string columnName, string headerText, string format,
            HorizontalAlignment alignment, ReportSummaryMode summaryMode)
        {
            ColumnName = columnName;
            HeaderText = headerText ?? columnName;
            Format = format;
            Alignment = alignment;
            _summaryMode = summaryMode;
        }

        /// <summary>Khởi tạo cột báo cáo căn lề nhanh.</summary>
        public ReportColumn(string columnName, string headerText, HorizontalAlignment alignment)
            : this(columnName, headerText, null, alignment, ReportSummaryMode.None) { }

        /// <summary>Tạo bản sao cấu hình cột độc lập.</summary>
        public ReportColumn Clone() => new ReportColumn(ColumnName, HeaderText, Format, Alignment, SummaryMode);
    }

    /// <summary>
    /// Lớp tiện ích tạo báo cáo HTML chuẩn in ấn, xem trước qua WebBrowser, xuất file Excel (.xls),
    /// đồng thời hỗ trợ chuẩn bị bảng STT và thao tác/xuất PDF/Excel cho RDLC ReportViewer.
    /// </summary>
    public static class ReportHelper
    {
        /// <summary>
        /// Tạo chuỗi mã HTML báo cáo hoàn chỉnh từ DataTable (hỗ trợ gom nhóm, tính tổng, chân trang số trang).
        /// </summary>
        /// <param name="table">DataTable nguồn chứa dữ liệu.</param>
        /// <param name="reportTitle">Tiêu đề lớn của báo cáo.</param>
        /// <param name="filterCondition">Chuỗi mô tả điều kiện lọc (ví dụ: "Phòng ban: Tất cả; Giới tính: Nữ").</param>
        /// <param name="groupByColumn">Tên cột dùng để gom nhóm (ví dụ: "TenPB", "TenCK"). Để null nếu không gom nhóm.</param>
        /// <param name="groupPrefix">Tiền tố hiển thị trước tên nhóm (ví dụ: "Phòng ban: ", "Chuyên khoa: ").</param>
        /// <param name="columns">Danh sách cấu hình các cột hiển thị. Nếu null sẽ tự động lấy tất cả các cột.</param>
        /// <param name="companyName">Tên cơ quan / doanh nghiệp ở góc trên bên trái.</param>
        /// <param name="signerTitle">Chức danh người ký dưới chân báo cáo.</param>
        /// <param name="showGroupTotals">Có hiển thị dòng tổng phụ ở cuối mỗi nhóm hay không (mặc định: false).</param>
        /// <param name="pageFooter">Văn bản chân trang (mặc định: "Trang 1/1"). Để null nếu muốn ẩn.</param>
        /// <param name="dateFormat">Định dạng ngày lập (mặc định: "dd/MM/yyyy").</param>
        /// <param name="showStt">Có hiển thị cột STT ở đầu bảng hay không (mặc định: true).</param>
        /// <param name="landscape">In khổ giấy ngang (A4 landscape) nếu true (mặc định: false - khổ dọc).</param>
        /// <param name="fontSizePt">Cỡ chữ bảng dữ liệu khi hiển thị/in HTML (mặc định: 13).</param>
        /// <returns>Chuỗi mã HTML hoàn chỉnh sẵn sàng nạp vào WebBrowser hoặc lưu thành file .xls.</returns>
        public static string GenerateHtml(DataTable table, string reportTitle, string filterCondition = null,
            string groupByColumn = null, string groupPrefix = null, IEnumerable<ReportColumn> columns = null,
            string companyName = "ĐƠN VỊ QUẢN LÝ", string signerTitle = "Người lập biểu",
            bool showGroupTotals = false, string pageFooter = "Trang 1/1", string dateFormat = "dd/MM/yyyy",
            bool showStt = true, bool landscape = false, int fontSizePt = 13)
        {
            return GenerateHtmlCore(table, reportTitle, filterCondition, groupByColumn, groupPrefix, columns,
                companyName, signerTitle, showGroupTotals, pageFooter, dateFormat, showStt, landscape, fontSizePt,
                interactivePreview: false);
        }

        internal static string GenerateHtmlCore(DataTable table, string reportTitle, string filterCondition,
            string groupByColumn, string groupPrefix, IEnumerable<ReportColumn> columns,
            string companyName, string signerTitle,
            bool showGroupTotals, string pageFooter, string dateFormat,
            bool showStt, bool landscape, int fontSizePt, bool interactivePreview)
        {
            if (table == null) table = new DataTable();

            var colList = columns?.ToList();
            if (colList == null || colList.Count == 0)
            {
                // Tự động nhận diện cột từ DataTable
                colList = BuildAutoColumns(table, groupByColumn, null);
            }

            int cellPx = Math.Max(9, Math.Min(22, fontSizePt <= 0 ? 13 : fontSizePt));
            int bodyPx = cellPx + 1;

            var sb = new StringBuilder();
            sb.Append(@"<!DOCTYPE html><html><head><meta charset='utf-8'><title>" + EscapeHtml(reportTitle) + @"</title><style>
                @page { size: A4 " + (landscape ? "landscape" : "portrait") + @"; margin: 12mm; }
                body { font-family: 'Times New Roman', serif; margin: 25px 35px; font-size: " + bodyPx + @"px; color: #222; }
                table { width: 100%; border-collapse: collapse; margin-top: 15px; }
                th, td { border: 1px solid #444; padding: 6px 8px; font-size: " + cellPx + @"px; }
                th { background-color: #dbe5f1; text-align: center; font-weight: bold; }
                .nhom { font-weight: bold; background-color: #f2f4f7; color: #1f497d; font-size: " + bodyPx + @"px; }
                .subtotal { font-style: italic; background-color: #fafbfc; font-weight: bold; }
                .tong { font-weight: bold; background-color: #fde9d9; font-size: " + bodyPx + @"px; }
                .c { text-align: center; } .r { text-align: right; } .l { text-align: left; }
                .footer { text-align: center; font-style: italic; margin-top: 30px; font-size: " + Math.Max(9, cellPx - 1) + @"px; color: #444; }");

            if (interactivePreview)
            {
                sb.Append(@"
                .preview-tip { font-family: 'Segoe UI', sans-serif; font-size: 12px; color: #1e40af; background: #eff6ff; border: 1px solid #bfdbfe; padding: 5px 10px; margin-bottom: 12px; border-radius: 4px; text-align: center; }
                [data-edit], th[data-col] { cursor: pointer; }
                [data-edit]:hover, th[data-col]:hover { outline: 2px dashed #2563eb; background-color: #eff6ff !important; }");
            }

            sb.Append(@"
                @media print {
                    body { margin: 12mm; }
                    .no-print { display: none; }
                    [data-edit]:hover, th[data-col]:hover { outline: none !important; background-color: inherit !important; }
                }
            </style></head><body>");

            if (interactivePreview)
            {
                sb.Append("<div class='preview-tip no-print'>Mẹo: Bấm trực tiếp vào <b>Đơn vị</b>, <b>Tiêu đề</b>, <b>Điều kiện lọc</b>, <b>Người ký</b> hoặc <b>tiêu đề cột</b> trên bảng để sửa nhanh.</div>");
            }

            // Phần đầu biểu mẫu
            string safeDateFmt = string.IsNullOrWhiteSpace(dateFormat) ? "dd/MM/yyyy" : dateFormat;
            string companyAttr = interactivePreview ? " data-edit='company' onclick=\"notifyHost('company')\" title='Bấm để sửa tên đơn vị'" : "";
            string titleAttr = interactivePreview ? " data-edit='title' onclick=\"notifyHost('title')\" title='Bấm để sửa tiêu đề báo cáo'" : "";
            string filterAttr = interactivePreview ? " data-edit='filter' onclick=\"notifyHost('filter')\" title='Bấm để sửa điều kiện lọc'" : "";
            string signerAttr = interactivePreview ? " data-edit='signer' onclick=\"notifyHost('signer')\" title='Bấm để sửa chức danh người ký'" : "";

            sb.Append("<table><tr>");
            sb.Append("<td" + companyAttr + " style='border:none;font-weight:bold;text-transform:uppercase;'>" + EscapeHtml(companyName) + "</td>");
            sb.Append("<td style='border:none;text-align:right;font-style:italic;'>Ngày lập: " + DateTime.Now.ToString(safeDateFmt) + "</td>");
            sb.Append("</tr></table>");

            sb.Append("<h2" + titleAttr + " style='text-align:center;margin:15px 0 5px;color:#1f497d;text-transform:uppercase;'>" + EscapeHtml(reportTitle) + "</h2>");

            if (!string.IsNullOrWhiteSpace(filterCondition))
            {
                sb.Append("<p" + filterAttr + " style='text-align:center;font-style:italic;margin:2px 0 15px;color:#555;'>Điều kiện lọc: " + EscapeHtml(filterCondition) + "</p>");
            }
            else if (interactivePreview)
            {
                sb.Append("<p class='no-print'" + filterAttr + " style='text-align:center;font-style:italic;margin:2px 0 15px;color:#94a3b8;'>(+ Bấm để nhập điều kiện lọc)</p>");
            }

            // Bắt đầu bảng dữ liệu
            sb.Append("<table><thead><tr>");
            if (showStt) sb.Append("<th style='width:45px;'>STT</th>");
            foreach (var col in colList)
            {
                if (interactivePreview)
                {
                    string safeColAttr = EscapeHtml(col.ColumnName);
                    string jsArg = EscapeJsString("col:" + col.ColumnName);
                    sb.Append($"<th data-col='{safeColAttr}' onclick=\"notifyHost('{jsArg}')\" title='Bấm để sửa/căn lề/định dạng/tính tổng hoặc ẩn cột'>{EscapeHtml(col.HeaderText)}</th>");
                }
                else
                {
                    sb.Append("<th>" + EscapeHtml(col.HeaderText) + "</th>");
                }
            }
            sb.Append("</tr></thead><tbody>");

            int totalColumns = colList.Count + (showStt ? 1 : 0);
            int stt = 1;
            var grandBuckets = CreateSummaryBuckets(colList);

            bool hasGrouping = !string.IsNullOrEmpty(groupByColumn) && table.Columns.Contains(groupByColumn);

            if (hasGrouping)
            {
                string prefix = groupPrefix ?? (groupByColumn + ": ");
                var groupedRows = table.Rows.Cast<DataRow>()
                    .GroupBy(r => r[groupByColumn] == DBNull.Value ? string.Empty : (r[groupByColumn]?.ToString() ?? string.Empty));

                foreach (var group in groupedRows)
                {
                    var rows = group.ToList();
                    if (rows.Count == 0) continue;

                    string groupDisplay = string.IsNullOrEmpty(group.Key) ? "Khác" : group.Key;
                    sb.Append($"<tr><td colspan='{totalColumns}' class='nhom'>{EscapeHtml(prefix + groupDisplay)} ({rows.Count} bản ghi)</td></tr>");

                    var groupBuckets = CreateSummaryBuckets(colList);

                    foreach (DataRow r in rows)
                    {
                        RenderRow(sb, r, colList, stt++, showStt);
                        AccumulateSummaries(r, colList, groupBuckets, grandBuckets);
                    }

                    if (showGroupTotals && groupBuckets.Count > 0)
                    {
                        RenderTotalRow(sb, GetGroupSummaryLabel(groupDisplay, colList), colList, groupBuckets, "subtotal", showStt);
                    }
                }
            }
            else
            {
                foreach (DataRow r in table.Rows)
                {
                    RenderRow(sb, r, colList, stt++, showStt);
                    AccumulateSummaries(r, colList, null, grandBuckets);
                }
            }

            // Dòng TỔNG CỘNG / THỐNG KÊ chung
            if (grandBuckets.Count > 0)
            {
                RenderTotalRow(sb, GetGrandSummaryLabel(colList), colList, grandBuckets, "tong", showStt);
            }

            sb.Append("</tbody></table>");

            // Chữ ký Người lập biểu
            if (!string.IsNullOrWhiteSpace(signerTitle))
            {
                sb.Append("<table style='margin-top:35px;'><tr><td style='border:none;'></td><td" + signerAttr + " style='border:none;text-align:right;padding-right:45px;'><b>" + EscapeHtml(signerTitle) + "</b><br><i>(Ký, ghi rõ họ tên)</i><br><br><br><br></td></tr></table>");
            }

            // Chân trang số trang
            if (!string.IsNullOrWhiteSpace(pageFooter))
            {
                sb.Append($"<div class='footer'>{EscapeHtml(pageFooter)}</div>");
            }

            if (interactivePreview)
            {
                sb.Append("<script type='text/javascript'>function notifyHost(act){try{if(window.external&&window.external.OnPreviewClick){window.external.OnPreviewClick(act);}}catch(e){}}</script>");
            }

            sb.Append("</body></html>");

            return sb.ToString();
        }

        private static string EscapeJsString(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "")
                .Replace("\n", "");
        }

        internal static List<ReportColumn> BuildAutoColumns(DataTable table, string groupByColumn, string sumColumn)
        {
            var cols = new List<ReportColumn>();
            if (table == null) return cols;

            foreach (DataColumn dc in table.Columns)
            {
                if (!string.IsNullOrEmpty(groupByColumn) && dc.ColumnName == groupByColumn)
                    continue;

                bool hasExplicitSum = !string.IsNullOrEmpty(sumColumn);
                bool isSum = hasExplicitSum && (dc.ColumnName == sumColumn);
                var align = HorizontalAlignment.Left;
                string fmt = null;

                if (dc.DataType == typeof(DateTime))
                {
                    align = HorizontalAlignment.Center;
                    fmt = "dd/MM/yyyy";
                }
                else if (dc.DataType == typeof(decimal) || dc.DataType == typeof(int) || dc.DataType == typeof(long))
                {
                    align = HorizontalAlignment.Right;
                    fmt = "N0";
                    if (!hasExplicitSum) isSum = true;
                }
                else if (dc.DataType == typeof(double) || dc.DataType == typeof(float))
                {
                    align = HorizontalAlignment.Right;
                    fmt = isSum ? "N0" : "0.##";
                }
                else if (dc.DataType == typeof(bool))
                {
                    align = HorizontalAlignment.Center;
                }

                cols.Add(new ReportColumn(dc.ColumnName, dc.ColumnName, fmt, align, isSum));
            }
            return cols;
        }

        private static void RenderTotalRow(StringBuilder sb, string label, List<ReportColumn> colList,
            Dictionary<string, SummaryBucket> buckets, string cssClass, bool showStt = true)
        {
            if (showStt)
            {
                // Nếu có >= 2 cột dữ liệu và cột đầu tiên không thống kê (như Mã NV), đặt nhãn ở cột dữ liệu đầu tiên
                bool placeLabelInFirstDataCol = colList.Count >= 2 && !HasSummary(colList[0]);

                if (placeLabelInFirstDataCol)
                {
                    sb.Append($"<tr class='{cssClass}'><td></td><td class='c'>{EscapeHtml(label)}</td>");
                    RenderSummaryCells(sb, colList, buckets, startDataColIndex: 1);
                }
                else
                {
                    sb.Append($"<tr class='{cssClass}'><td class='c'>{EscapeHtml(label)}</td>");
                    RenderSummaryCells(sb, colList, buckets, startDataColIndex: 0);
                }
            }
            else if (colList.Count > 0)
            {
                if (!HasSummary(colList[0]))
                {
                    sb.Append($"<tr class='{cssClass}'><td class='c'>{EscapeHtml(label)}</td>");
                    RenderSummaryCells(sb, colList, buckets, startDataColIndex: 1);
                }
                else
                {
                    buckets.TryGetValue(colList[0].ColumnName, out SummaryBucket bucket0);
                    string text0 = FormatSummaryDisplay(colList[0], bucket0);
                    sb.Append($"<tr class='{cssClass}'><td class='r'><span style='float:left;font-weight:bold;'>{EscapeHtml(label)}</span>{EscapeHtml(text0)}</td>");
                    RenderSummaryCells(sb, colList, buckets, startDataColIndex: 1);
                }
            }
            sb.Append("</tr>");
        }

        private static void RenderRow(StringBuilder sb, DataRow r, List<ReportColumn> colList, int stt, bool showStt = true)
        {
            sb.Append("<tr>");
            if (showStt) sb.Append($"<td class='c'>{stt}</td>");
            foreach (var col in colList)
            {
                string alignClass = col.Alignment == HorizontalAlignment.Center ? "c" : (col.Alignment == HorizontalAlignment.Right ? "r" : "l");
                object val = r.Table.Columns.Contains(col.ColumnName) ? r[col.ColumnName] : null;
                string displayText = FormatValue(val, col.Format);
                sb.Append($"<td class='{alignClass}'>{EscapeHtml(displayText)}</td>");
            }
            sb.Append("</tr>");
        }

        internal static bool HasSummary(ReportColumn col) => col != null && col.SummaryMode != ReportSummaryMode.None;

        internal static string GetGrandSummaryLabel(List<ReportColumn> colList)
            => colList != null && colList.Any(c => c.SummaryMode == ReportSummaryMode.Sum) ? "TỔNG CỘNG" : "THỐNG KÊ";

        internal static string GetGroupSummaryLabel(string groupDisplay, List<ReportColumn> colList)
            => colList != null && colList.Any(c => c.SummaryMode == ReportSummaryMode.Sum)
                ? $"Tổng {groupDisplay}:"
                : $"Thống kê {groupDisplay}:";

        internal static Dictionary<string, SummaryBucket> CreateSummaryBuckets(IEnumerable<ReportColumn> colList)
        {
            var dict = new Dictionary<string, SummaryBucket>(StringComparer.OrdinalIgnoreCase);
            if (colList == null) return dict;
            foreach (var col in colList.Where(HasSummary))
            {
                if (!dict.ContainsKey(col.ColumnName))
                    dict[col.ColumnName] = new SummaryBucket();
            }
            return dict;
        }

        internal static void AccumulateSummaries(DataRow r, List<ReportColumn> colList,
            Dictionary<string, SummaryBucket> groupBuckets, Dictionary<string, SummaryBucket> grandBuckets)
        {
            foreach (var col in colList.Where(HasSummary))
            {
                if (!r.Table.Columns.Contains(col.ColumnName)) continue;
                object raw = r[col.ColumnName];
                if (groupBuckets != null && groupBuckets.TryGetValue(col.ColumnName, out SummaryBucket gb))
                    gb.Add(raw, col.SummaryMode);
                if (grandBuckets != null && grandBuckets.TryGetValue(col.ColumnName, out SummaryBucket grb))
                    grb.Add(raw, col.SummaryMode);
            }
        }

        internal static string FormatSummaryDisplay(ReportColumn col, SummaryBucket bucket)
        {
            if (col == null || bucket == null || !bucket.TryGetResult(col.SummaryMode, out decimal val))
                return "";

            switch (col.SummaryMode)
            {
                case ReportSummaryMode.Sum:
                    return val.ToString(string.IsNullOrWhiteSpace(col.Format) ? "N0" : col.Format);
                case ReportSummaryMode.Average:
                    return "TB: " + val.ToString(string.IsNullOrWhiteSpace(col.Format) ? "0.##" : col.Format);
                case ReportSummaryMode.Count:
                    return "Đếm: " + val.ToString("N0");
                case ReportSummaryMode.Max:
                    return "Max: " + val.ToString(string.IsNullOrWhiteSpace(col.Format) ? "N0" : col.Format);
                case ReportSummaryMode.Min:
                    return "Min: " + val.ToString(string.IsNullOrWhiteSpace(col.Format) ? "N0" : col.Format);
                default:
                    return "";
            }
        }

        private static void RenderSummaryCells(StringBuilder sb, List<ReportColumn> colList,
            Dictionary<string, SummaryBucket> buckets, int startDataColIndex)
        {
            for (int i = startDataColIndex; i < colList.Count; i++)
            {
                var col = colList[i];
                if (HasSummary(col) && buckets.TryGetValue(col.ColumnName, out SummaryBucket bucket))
                {
                    string display = FormatSummaryDisplay(col, bucket);
                    sb.Append($"<td class='r'>{EscapeHtml(display)}</td>");
                }
                else
                {
                    sb.Append("<td></td>");
                }
            }
        }

        internal static string FormatCellValue(object val, string format) => FormatValue(val, format);

        private static string FormatValue(object val, string format)
        {
            if (val == null || val == DBNull.Value) return "";
            if (!string.IsNullOrEmpty(format))
            {
                if (val is IFormattable formattable)
                    return formattable.ToString(format, null);
                if (DateTime.TryParse(val.ToString(), out var dt))
                    return dt.ToString(format);
                if (decimal.TryParse(val.ToString(), out var dec))
                    return dec.ToString(format);
            }
            return val.ToString();
        }

        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;");
        }

        /// <summary>
        /// Loại bỏ ký tự không hợp lệ trong tên file để truyền an toàn vào SaveFileDialog.
        /// </summary>
        public static string SanitizeFileName(string fileName, string fallback = "BaoCao")
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return fallback;

            char[] invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(fileName.Length);
            foreach (char c in fileName.Trim())
            {
                sb.Append(invalidChars.Contains(c) ? '_' : c);
            }
            string cleaned = sb.ToString().Trim();
            return string.IsNullOrEmpty(cleaned) ? fallback : cleaned;
        }

        /// <summary>
        /// Tạo bản sao DataTable có chèn thêm cột STT (1..N) ở đầu bảng.
        /// Rất hữu ích khi truyền DataTable vào RDLC ReportViewer hoặc Crystal Report.
        /// </summary>
        public static DataTable PrepareReportTable(DataTable source, string sttColumnName = "STT")
        {
            DataTable result = source != null ? source.Copy() : new DataTable();
            if (!result.Columns.Contains(sttColumnName))
            {
                DataColumn sttCol = result.Columns.Add(sttColumnName, typeof(int));
                sttCol.SetOrdinal(0);
            }

            for (int i = 0; i < result.Rows.Count; i++)
            {
                result.Rows[i][sttColumnName] = i + 1;
            }
            return result;
        }

        /// <summary>
        /// Mở hộp thoại lưu file và xuất nội dung HTML thành file Excel (.xls) có định dạng chuẩn.
        /// </summary>
        /// <param name="htmlContent">Chuỗi HTML của báo cáo.</param>
        /// <param name="defaultFileName">Tên file mặc định khi lưu (mặc định: "BaoCao.xls").</param>
        /// <returns>True nếu người dùng đã lưu thành công, False nếu bấm Hủy.</returns>
        public static bool ExportExcel(string htmlContent, string defaultFileName = "BaoCao.xls")
        {
            string safeName = SanitizeFileName(defaultFileName, "BaoCao.xls");
            if (!safeName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) &&
                !safeName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                safeName += ".xls";
            }

            using (var sfd = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xls)|*.xls|HTML File (*.html)|*.html",
                FileName = safeName
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(sfd.FileName, htmlContent ?? "", new UTF8Encoding(true));
                        MessageHelper.Success("xuất file Excel");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageHelper.Error("Lỗi khi lưu file: " + ex.Message);
                        return false;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Tạo mảng byte file Excel chuẩn OpenXML (.xlsx) thật (không cảnh báo định dạng khi mở bằng Microsoft Excel).
        /// </summary>
        public static byte[] BuildXlsxBytes(DataTable table, string reportTitle, string filterCondition = null,
            string groupByColumn = null, string groupPrefix = null, IEnumerable<ReportColumn> columns = null,
            string companyName = "ĐƠN VỊ QUẢN LÝ", string signerTitle = "Người lập biểu",
            bool showGroupTotals = false, string pageFooter = "Trang 1/1", string dateFormat = "dd/MM/yyyy",
            bool showStt = true, bool landscape = false, int fontSizePt = 13)
        {
            return XlsxExporter.BuildBytes(table, reportTitle, filterCondition, groupByColumn, groupPrefix,
                columns, companyName, signerTitle, showGroupTotals, pageFooter, dateFormat, showStt, landscape, fontSizePt);
        }

        /// <summary>
        /// Mở hộp thoại lưu file và xuất báo cáo ra file Excel chuẩn OpenXML (.xlsx) hoặc HTML.
        /// </summary>
        public static bool ExportXlsx(DataTable table, string reportTitle, string htmlFallback = null,
            string filterCondition = null, string groupByColumn = null, string groupPrefix = null,
            IEnumerable<ReportColumn> columns = null, string companyName = "ĐƠN VỊ QUẢN LÝ",
            string signerTitle = "Người lập biểu", bool showGroupTotals = false, string pageFooter = "Trang 1/1",
            string dateFormat = "dd/MM/yyyy", bool showStt = true, string defaultFileName = null,
            bool landscape = false, int fontSizePt = 13)
        {
            string baseName = SanitizeFileName(defaultFileName ?? reportTitle, "BaoCao");
            if (baseName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                baseName = baseName.Substring(0, baseName.Length - 4);
            if (!baseName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                baseName += ".xlsx";

            using (var sfd = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|HTML File (*.html)|*.html",
                FileName = baseName
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK)
                    return false;

                try
                {
                    if (sfd.FileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    {
                        string html = htmlFallback ?? GenerateHtml(table, reportTitle, filterCondition, groupByColumn,
                            groupPrefix, columns, companyName, signerTitle, showGroupTotals, pageFooter, dateFormat,
                            showStt, landscape, fontSizePt);
                        File.WriteAllText(sfd.FileName, html, new UTF8Encoding(true));
                    }
                    else
                    {
                        byte[] bytes = BuildXlsxBytes(table, reportTitle, filterCondition, groupByColumn,
                            groupPrefix, columns, companyName, signerTitle, showGroupTotals, pageFooter, dateFormat,
                            showStt, landscape, fontSizePt);
                        File.WriteAllBytes(sfd.FileName, bytes);
                    }
                    MessageHelper.Success("xuất file Excel");
                    return true;
                }
                catch (Exception ex)
                {
                    MessageHelper.Error("Lỗi khi lưu file: " + ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// Hiển thị Form Báo Cáo trực quan dùng chung: tự gom tất cả trường dữ liệu vào 1 bảng,
        /// hỗ trợ kéo-thả ẩn/hiện/sắp xếp/gom nhóm cột, sửa tiêu đề trực tiếp và In/Lưu PDF, Xuất Excel.
        /// </summary>
        public static void ShowReport(DataTable dataTable, string reportTitle = "BÁO CÁO THỐNG KÊ", string filterCondition = null,
            string groupByColumn = null, string groupPrefix = null, IEnumerable<ReportColumn> columns = null,
            string companyName = "ĐƠN VỊ QUẢN LÝ", IWin32Window owner = null,
            string signerTitle = "Người lập biểu", bool showGroupTotals = false, string pageFooter = "Trang 1/1",
            bool openDesigner = true, bool landscape = false, int fontSizePt = 13)
        {
            var options = new ReportDesignOptions
            {
                ReportTitle = reportTitle,
                FilterCondition = filterCondition,
                GroupByColumn = groupByColumn,
                GroupPrefix = groupPrefix,
                Columns = columns,
                CompanyName = companyName,
                SignerTitle = signerTitle,
                ShowGroupTotals = showGroupTotals,
                PageFooter = pageFooter,
                OpenDesigner = openDesigner,
                Landscape = landscape,
                FontSizePt = fontSizePt
            };

            ShowReport(dataTable, options, owner);
        }

        /// <summary>
        /// Mở Form Báo Cáo trực quan bằng cấu hình <see cref="ReportDesignOptions"/> (hoặc tạo từ <see cref="ReportTemplate"/>).
        /// </summary>
        public static void ShowReport(DataTable dataTable, ReportDesignOptions options, IWin32Window owner = null)
        {
            using (var form = new FormReportViewer(dataTable, options))
            {
                if (owner != null) form.ShowDialog(owner);
                else form.ShowDialog();
            }
        }

        /// <summary>
        /// Hiển thị báo cáo nhanh trong 1 dòng code (tự động gom nhóm và tính tổng cột chỉ định),
        /// cho phép người dùng chỉnh sửa/kéo-thả cột trực tiếp trước khi in.
        /// </summary>
        public static void ShowReport(DataTable dataTable, string reportTitle, string groupByColumn,
            string sumColumn, string filterCondition = null, IWin32Window owner = null,
            string companyName = "ĐƠN VỊ QUẢN LÝ", string groupPrefix = null, bool showGroupTotals = false,
            bool openDesigner = true, bool landscape = false, int fontSizePt = 13)
        {
            var cols = BuildAutoColumns(dataTable, groupByColumn, sumColumn);
            ShowReport(dataTable, reportTitle, filterCondition, groupByColumn, groupPrefix, cols,
                companyName, owner, showGroupTotals: showGroupTotals, openDesigner: openDesigner,
                landscape: landscape, fontSizePt: fontSizePt);
        }

        /// <summary>
        /// Gom toàn bộ dữ liệu và cấu hình cột trực tiếp từ DataGridView để mở Form báo cáo trực quan
        /// (cho phép kéo-thả ẩn/hiện cột, đổi tiêu đề và in ngay không cần cấu hình trước).
        /// </summary>
        public static void ShowReport(DataGridView grid, string reportTitle = "BÁO CÁO THỐNG KÊ",
            string filterCondition = null, string groupByColumn = null, string groupPrefix = null,
            string companyName = "ĐƠN VỊ QUẢN LÝ", IWin32Window owner = null,
            string signerTitle = "Người lập biểu", bool showGroupTotals = false, string pageFooter = "Trang 1/1",
            bool openDesigner = true, bool landscape = false, int fontSizePt = 13)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid), "DataGridView không được null.");

            DataTable dt = ExtractTable(grid, out List<ReportColumn> cols);
            ShowReport(dt, reportTitle, filterCondition, groupByColumn, groupPrefix, cols,
                companyName, owner, signerTitle, showGroupTotals, pageFooter, openDesigner, landscape, fontSizePt);
        }

        /// <summary>
        /// Thực thi trực tiếp câu lệnh SQL query và mở ngay Form báo cáo trực quan
        /// (tự gom toàn bộ trường từ kết quả truy vấn vào 1 bảng, cho phép kéo-thả chỉnh sửa trước khi in).
        /// </summary>
        public static void ShowReport(string sql, string reportTitle = "BÁO CÁO THỐNG KÊ",
            string filterCondition = null, string groupByColumn = null, string groupPrefix = null,
            string companyName = "ĐƠN VỊ QUẢN LÝ", IWin32Window owner = null, string dbName = "default",
            string signerTitle = "Người lập biểu", bool showGroupTotals = false, string pageFooter = "Trang 1/1",
            bool openDesigner = true, bool landscape = false, int fontSizePt = 13)
        {
            DataTable dt = DbHelper.GetDataTable(sql, dbName);
            ShowReport(dt, reportTitle, filterCondition, groupByColumn, groupPrefix, null,
                companyName, owner, signerTitle, showGroupTotals, pageFooter, openDesigner, landscape, fontSizePt);
        }

        /// <summary>
        /// Trích xuất DataTable và danh sách ReportColumn hiển thị từ một DataGridView (có hỗ trợ DataSource hoặc nhập tay).
        /// </summary>
        public static DataTable ExtractTable(DataGridView grid, out List<ReportColumn> visibleColumns)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));

            DataTable boundTable = grid.DataSource as DataTable;
            if (boundTable == null && grid.DataSource is BindingSource bs)
            {
                boundTable = bs.DataSource as DataTable ?? (bs.List as DataView)?.Table;
            }
            else if (boundTable == null && grid.DataSource is DataView dv)
            {
                boundTable = dv.Table;
            }

            var cols = new List<ReportColumn>();
            var orderedGridCols = grid.Columns.Cast<DataGridViewColumn>().OrderBy(c => c.DisplayIndex).ToList();

            if (boundTable != null)
            {
                DataTable copy = boundTable.Copy();
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var gCol in orderedGridCols)
                {
                    if (!gCol.Visible) continue;
                    string propName = !string.IsNullOrWhiteSpace(gCol.DataPropertyName) ? gCol.DataPropertyName : gCol.Name;
                    if (string.IsNullOrWhiteSpace(propName) || !copy.Columns.Contains(propName) || !usedNames.Add(propName))
                        continue;

                    DataColumn dc = copy.Columns[propName];
                    HorizontalAlignment align = MapGridAlignment(gCol.DefaultCellStyle.Alignment, dc.DataType);
                    string fmt = !string.IsNullOrWhiteSpace(gCol.DefaultCellStyle.Format)
                        ? gCol.DefaultCellStyle.Format
                        : InferDefaultFormat(dc.DataType);
                    bool isSum = dc.DataType == typeof(decimal) || dc.DataType == typeof(int) || dc.DataType == typeof(long);

                    string header = string.IsNullOrWhiteSpace(gCol.HeaderText) ? dc.ColumnName : gCol.HeaderText;
                    cols.Add(new ReportColumn(dc.ColumnName, header, fmt, align, isSum));
                }

                visibleColumns = cols.Count > 0 ? cols : null;
                return copy;
            }

            // DataGridView nhập trực tiếp không bind DataSource
            var dt = new DataTable();
            var colIndexMap = new List<(int GridColIndex, string ColumnName)>();
            foreach (var gCol in orderedGridCols)
            {
                string baseName = !string.IsNullOrWhiteSpace(gCol.DataPropertyName)
                    ? gCol.DataPropertyName
                    : (!string.IsNullOrWhiteSpace(gCol.Name) ? gCol.Name : "Col" + gCol.Index);
                string uniqueName = baseName;
                int suffix = 1;
                while (dt.Columns.Contains(uniqueName))
                {
                    uniqueName = baseName + "_" + (++suffix);
                }

                Type colType = gCol.ValueType ?? typeof(string);
                dt.Columns.Add(uniqueName, colType);
                colIndexMap.Add((gCol.Index, uniqueName));

                if (gCol.Visible)
                {
                    HorizontalAlignment align = MapGridAlignment(gCol.DefaultCellStyle.Alignment, colType);
                    string fmt = !string.IsNullOrWhiteSpace(gCol.DefaultCellStyle.Format)
                        ? gCol.DefaultCellStyle.Format
                        : InferDefaultFormat(colType);
                    bool isSum = colType == typeof(decimal) || colType == typeof(int) || colType == typeof(long);
                    string header = string.IsNullOrWhiteSpace(gCol.HeaderText) ? uniqueName : gCol.HeaderText;
                    cols.Add(new ReportColumn(uniqueName, header, fmt, align, isSum));
                }
            }

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                DataRow dr = dt.NewRow();
                foreach (var pair in colIndexMap)
                {
                    dr[pair.ColumnName] = row.Cells[pair.GridColIndex].Value ?? DBNull.Value;
                }
                dt.Rows.Add(dr);
            }

            visibleColumns = cols.Count > 0 ? cols : null;
            return dt;
        }

        private static HorizontalAlignment MapGridAlignment(DataGridViewContentAlignment gridAlign, Type dataType)
        {
            switch (gridAlign)
            {
                case DataGridViewContentAlignment.TopCenter:
                case DataGridViewContentAlignment.MiddleCenter:
                case DataGridViewContentAlignment.BottomCenter:
                    return HorizontalAlignment.Center;
                case DataGridViewContentAlignment.TopRight:
                case DataGridViewContentAlignment.MiddleRight:
                case DataGridViewContentAlignment.BottomRight:
                    return HorizontalAlignment.Right;
                case DataGridViewContentAlignment.TopLeft:
                case DataGridViewContentAlignment.MiddleLeft:
                case DataGridViewContentAlignment.BottomLeft:
                    return HorizontalAlignment.Left;
                default:
                    if (dataType == typeof(decimal) || dataType == typeof(int) || dataType == typeof(long)
                        || dataType == typeof(double) || dataType == typeof(float))
                        return HorizontalAlignment.Right;
                    if (dataType == typeof(DateTime) || dataType == typeof(bool))
                        return HorizontalAlignment.Center;
                    return HorizontalAlignment.Left;
            }
        }

        private static string InferDefaultFormat(Type dataType)
        {
            if (dataType == typeof(DateTime)) return "dd/MM/yyyy";
            if (dataType == typeof(decimal) || dataType == typeof(int) || dataType == typeof(long)) return "N0";
            if (dataType == typeof(double) || dataType == typeof(float)) return "0.##";
            return null;
        }

        #region Hỗ trợ RDLC ReportViewer (Reflection - Không phụ thuộc NuGet)

        /// <summary>
        /// Nạp DataTable và danh sách ReportParameter vào Microsoft ReportViewer (RDLC) bằng Reflection
        /// để không gây phụ thuộc cứng giữa các phiên bản .NET Framework và .NET Core/8/10.
        /// </summary>
        /// <param name="reportViewer">Control ReportViewer trên Form.</param>
        /// <param name="dataSetName">Tên DataSet khai báo trong file .rdlc.</param>
        /// <param name="data">DataTable dữ liệu nguồn (đã lọc).</param>
        /// <param name="parameters">Từ điển (Tên tham số → Giá trị) truyền vào RDLC.</param>
        /// <param name="reportEmbeddedResource">Tên Embedded Resource của file .rdlc (tùy chọn).</param>
        /// <param name="reportPath">Đường dẫn file .rdlc trên đĩa (tùy chọn).</param>
        public static void BindRdlc(object reportViewer, string dataSetName, DataTable data,
            Dictionary<string, string> parameters = null,
            string reportEmbeddedResource = null, string reportPath = null)
        {
            if (reportViewer == null)
                throw new ArgumentNullException(nameof(reportViewer), "ReportViewer không được null.");
            if (string.IsNullOrWhiteSpace(dataSetName))
                throw new ArgumentException("Tên DataSet không được để trống.", nameof(dataSetName));

            Type viewerType = reportViewer.GetType();
            PropertyInfo localReportProp = viewerType.GetProperty("LocalReport")
                ?? throw new InvalidOperationException("Đối tượng truyền vào không có thuộc tính LocalReport.");

            object localReport = localReportProp.GetValue(reportViewer)
                ?? throw new InvalidOperationException("LocalReport đang là null.");

            Type localReportType = localReport.GetType();
            Assembly rdlcAssembly = localReportType.Assembly;

            if (!string.IsNullOrWhiteSpace(reportEmbeddedResource))
            {
                localReportType.GetProperty("ReportEmbeddedResource")?.SetValue(localReport, reportEmbeddedResource);
            }
            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                localReportType.GetProperty("ReportPath")?.SetValue(localReport, reportPath);
            }

            // Nạp DataSources
            object dataSources = localReportType.GetProperty("DataSources")?.GetValue(localReport);
            if (dataSources != null)
            {
                Type dsCollectionType = dataSources.GetType();
                dsCollectionType.GetMethod("Clear")?.Invoke(dataSources, null);

                Type rdsType = FindTypeInAssembly(rdlcAssembly, "ReportDataSource")
                    ?? throw new InvalidOperationException("Không tìm thấy kiểu ReportDataSource trong assembly của ReportViewer.");

                object rds = Activator.CreateInstance(rdsType, dataSetName, data ?? new DataTable());
                dsCollectionType.GetMethod("Add")?.Invoke(dataSources, new[] { rds });
            }

            // Truyền ReportParameter nếu có
            if (parameters != null && parameters.Count > 0)
            {
                Type rpType = FindTypeInAssembly(rdlcAssembly, "ReportParameter")
                    ?? throw new InvalidOperationException("Không tìm thấy kiểu ReportParameter trong assembly của ReportViewer.");

                Array rpArray = Array.CreateInstance(rpType, parameters.Count);
                int idx = 0;
                foreach (var kvp in parameters)
                {
                    object rp = Activator.CreateInstance(rpType, kvp.Key, kvp.Value ?? "");
                    rpArray.SetValue(rp, idx++);
                }

                Type ienumRpType = typeof(IEnumerable<>).MakeGenericType(rpType);
                MethodInfo setParamsMethod = localReportType.GetMethod("SetParameters", new[] { ienumRpType });
                if (setParamsMethod != null)
                {
                    setParamsMethod.Invoke(localReport, new object[] { rpArray });
                }
            }

            viewerType.GetMethod("RefreshReport", Type.EmptyTypes)?.Invoke(reportViewer, null);
        }

        /// <summary>
        /// Render và lưu báo cáo RDLC ra file PDF, Excel (.xlsx/.xls) hoặc Word qua SaveFileDialog.
        /// </summary>
        /// <param name="reportViewer">Control ReportViewer đã nạp dữ liệu.</param>
        /// <param name="format">Định dạng Render RDLC: "PDF", "EXCELOPENXML", "EXCEL", "WORDOPENXML".</param>
        /// <param name="defaultFileName">Tên file mặc định khi lưu (tự gắn đuôi nếu chưa có).</param>
        /// <returns>True nếu xuất thành công, False nếu hủy hoặc lỗi.</returns>
        public static bool ExportRdlc(object reportViewer, string format = "PDF", string defaultFileName = "BaoCao")
        {
            if (reportViewer == null)
                throw new ArgumentNullException(nameof(reportViewer), "ReportViewer không được null.");

            string fmtUpper = (format ?? "PDF").Trim().ToUpperInvariant();
            string filter;
            string ext;
            string actionLabel;

            switch (fmtUpper)
            {
                case "EXCELOPENXML":
                    filter = "Excel Workbook (*.xlsx)|*.xlsx";
                    ext = ".xlsx";
                    actionLabel = "xuất file Excel";
                    break;
                case "EXCEL":
                    filter = "Excel 97-2003 (*.xls)|*.xls";
                    ext = ".xls";
                    actionLabel = "xuất file Excel";
                    break;
                case "WORDOPENXML":
                    filter = "Word Document (*.docx)|*.docx";
                    ext = ".docx";
                    actionLabel = "xuất file Word";
                    break;
                default:
                    fmtUpper = "PDF";
                    filter = "PDF Document (*.pdf)|*.pdf";
                    ext = ".pdf";
                    actionLabel = "xuất file PDF";
                    break;
            }

            string safeName = SanitizeFileName(defaultFileName, "BaoCao");
            if (!safeName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                safeName += ext;
            }

            using (var sfd = new SaveFileDialog { Filter = filter, FileName = safeName })
            {
                if (sfd.ShowDialog() != DialogResult.OK)
                    return false;

                try
                {
                    byte[] bytes = RenderRdlcBytes(reportViewer, fmtUpper);
                    File.WriteAllBytes(sfd.FileName, bytes);
                    MessageHelper.Success(actionLabel);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageHelper.Error("Lỗi khi xuất báo cáo RDLC: " + (ex.InnerException?.Message ?? ex.Message));
                    return false;
                }
            }
        }

        /// <summary>
        /// Gọi LocalReport.Render(format) bằng Reflection và trả về mảng byte của file đã render.
        /// </summary>
        public static byte[] RenderRdlcBytes(object reportViewer, string format = "PDF")
        {
            if (reportViewer == null)
                throw new ArgumentNullException(nameof(reportViewer), "ReportViewer không được null.");

            object localReport = reportViewer.GetType().GetProperty("LocalReport")?.GetValue(reportViewer)
                ?? throw new InvalidOperationException("Đối tượng không có LocalReport.");

            MethodInfo renderMethod = localReport.GetType().GetMethod("Render", new[] { typeof(string) })
                ?? throw new InvalidOperationException("LocalReport không hỗ trợ phương thức Render(string).");

            object result = renderMethod.Invoke(localReport, new object[] { format ?? "PDF" });
            return result as byte[] ?? Array.Empty<byte>();
        }

        private static Type FindTypeInAssembly(Assembly assembly, string shortTypeName)
        {
            if (assembly == null) return null;
            return assembly.GetTypes().FirstOrDefault(t => string.Equals(t.Name, shortTypeName, StringComparison.Ordinal));
        }

        #endregion

        #region Hỗ trợ Crystal Reports (Reflection - Không phụ thuộc DLL ngoài)

        /// <summary>
        /// Nạp DataTable và Parameter vào ReportDocument rồi gắn lên CrystalReportViewer bằng Reflection.
        /// </summary>
        /// <param name="crystalReportViewer">Control CrystalReportViewer trên Form (có thể null nếu chỉ chuẩn bị ReportDocument).</param>
        /// <param name="reportDocument">Đối tượng ReportClass / ReportDocument của Crystal Report.</param>
        /// <param name="data">DataTable dữ liệu nguồn.</param>
        /// <param name="parameters">Từ điển (Tên tham số → Giá trị) cần truyền vào báo cáo.</param>
        public static void BindCrystalReport(object crystalReportViewer, object reportDocument, DataTable data,
            Dictionary<string, object> parameters = null)
        {
            if (reportDocument == null)
                throw new ArgumentNullException(nameof(reportDocument), "ReportDocument không được null.");

            Type docType = reportDocument.GetType();
            MethodInfo setDataSource = docType.GetMethod("SetDataSource", new[] { typeof(DataTable) })
                ?? docType.GetMethods().FirstOrDefault(m => m.Name == "SetDataSource" && m.GetParameters().Length == 1);

            setDataSource?.Invoke(reportDocument, new object[] { data ?? new DataTable() });

            if (parameters != null && parameters.Count > 0)
            {
                MethodInfo setParam = docType.GetMethod("SetParameterValue", new[] { typeof(string), typeof(object) })
                    ?? docType.GetMethods().FirstOrDefault(m => m.Name == "SetParameterValue" && m.GetParameters().Length == 2);

                if (setParam != null)
                {
                    foreach (var kvp in parameters)
                    {
                        setParam.Invoke(reportDocument, new object[] { kvp.Key, kvp.Value ?? "" });
                    }
                }
            }

            if (crystalReportViewer != null)
            {
                Type viewerType = crystalReportViewer.GetType();
                viewerType.GetProperty("ReportSource")?.SetValue(crystalReportViewer, reportDocument);
                (viewerType.GetMethod("RefreshReport", Type.EmptyTypes) ?? viewerType.GetMethod("Refresh", Type.EmptyTypes))
                    ?.Invoke(crystalReportViewer, null);
            }
        }

        /// <summary>
        /// Xuất Crystal Report ra file PDF hoặc Excel bằng ExportToDisk thông qua SaveFileDialog.
        /// </summary>
        /// <param name="reportDocument">Đối tượng ReportDocument đã nạp dữ liệu.</param>
        /// <param name="format">Định dạng xuất: "PDF", "EXCEL" (.xls), "EXCELOPENXML" (.xlsx).</param>
        /// <param name="defaultFileName">Tên file mặc định khi mở SaveFileDialog.</param>
        public static bool ExportCrystalReport(object reportDocument, string format = "PDF", string defaultFileName = "BaoCao")
        {
            if (reportDocument == null)
                throw new ArgumentNullException(nameof(reportDocument), "ReportDocument không được null.");

            string fmtUpper = (format ?? "PDF").Trim().ToUpperInvariant();
            string enumName;
            string filter;
            string ext;
            string actionLabel;

            switch (fmtUpper)
            {
                case "EXCEL":
                case "XLS":
                    enumName = "Excel";
                    filter = "Excel 97-2003 (*.xls)|*.xls";
                    ext = ".xls";
                    actionLabel = "xuất file Excel";
                    break;
                case "EXCELOPENXML":
                case "XLSX":
                    enumName = "ExcelWorkbook";
                    filter = "Excel Workbook (*.xlsx)|*.xlsx";
                    ext = ".xlsx";
                    actionLabel = "xuất file Excel";
                    break;
                default:
                    enumName = "PortableDocFormat";
                    filter = "PDF Document (*.pdf)|*.pdf";
                    ext = ".pdf";
                    actionLabel = "xuất file PDF";
                    break;
            }

            MethodInfo exportMethod = reportDocument.GetType().GetMethods()
                .FirstOrDefault(m => m.Name == "ExportToDisk" && m.GetParameters().Length == 2
                    && m.GetParameters()[1].ParameterType == typeof(string))
                ?? throw new InvalidOperationException("ReportDocument không hỗ trợ phương thức ExportToDisk.");

            Type formatEnumType = exportMethod.GetParameters()[0].ParameterType;
            object formatEnumVal = Enum.Parse(formatEnumType, enumName, ignoreCase: true);

            string safeName = SanitizeFileName(defaultFileName, "BaoCao");
            if (!safeName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                safeName += ext;

            using (var sfd = new SaveFileDialog { Filter = filter, FileName = safeName })
            {
                if (sfd.ShowDialog() != DialogResult.OK)
                    return false;

                try
                {
                    exportMethod.Invoke(reportDocument, new object[] { formatEnumVal, sfd.FileName });
                    MessageHelper.Success(actionLabel);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageHelper.Error("Lỗi khi xuất Crystal Report: " + (ex.InnerException?.Message ?? ex.Message));
                    return false;
                }
            }
        }

        #endregion

        #region Hỗ trợ File mẫu (.examrpt) & Xuất file thiết kế .rdlc

        /// <summary>Lưu đối tượng <see cref="ReportTemplate"/> thành file mẫu XML (.examrpt).</summary>
        public static void SaveTemplate(ReportTemplate template, string filePath)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            template.Save(filePath);
        }

        /// <summary>Đọc file mẫu báo cáo (.examrpt / .xml) từ đĩa.</summary>
        public static ReportTemplate LoadTemplate(string filePath) => ReportTemplate.Load(filePath);

        /// <summary>
        /// Mở Form báo cáo trực tiếp từ file mẫu (.examrpt) đã thiết kế sẵn và đổ dữ liệu từ <paramref name="dataTable"/>.
        /// </summary>
        public static void ShowReportFromTemplate(DataTable dataTable, string templatePath,
            string overrideFilterCondition = null, IWin32Window owner = null, bool openDesigner = false)
        {
            ReportTemplate template = ReportTemplate.Load(templatePath);
            ReportDesignOptions options = template.ToDesignOptions(dataTable, overrideFilterCondition, openDesigner);
            ShowReport(dataTable, options, owner);
        }

        /// <summary>
        /// Mở Form báo cáo trực tiếp từ file mẫu (.examrpt) đã thiết kế sẵn và đổ dữ liệu lấy từ <paramref name="grid"/>.
        /// </summary>
        public static void ShowReportFromTemplate(DataGridView grid, string templatePath,
            string overrideFilterCondition = null, IWin32Window owner = null, bool openDesigner = false)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            DataTable dt = ExtractTable(grid, out _);
            ShowReportFromTemplate(dt, templatePath, overrideFilterCondition, owner, openDesigner);
        }

        /// <summary>
        /// Sinh chuỗi XML file thiết kế Microsoft RDLC (.rdlc) từ DataTable và cấu hình cột trực quan.
        /// </summary>
        public static string GenerateRdlcXml(DataTable table, string reportTitle = "BÁO CÁO THỐNG KÊ",
            string filterCondition = null, string groupByColumn = null, string groupPrefix = null,
            IEnumerable<ReportColumn> columns = null, string companyName = "ĐƠN VỊ QUẢN LÝ",
            string signerTitle = "Người lập biểu", bool showGroupTotals = false, string pageFooter = "Trang 1/1",
            string dateFormat = "dd/MM/yyyy", bool showStt = true, bool landscape = false, int fontSizePt = 13,
            string dataSetName = "dsBaoCao")
        {
            return RdlcGenerator.GenerateXml(table, reportTitle, filterCondition, groupByColumn, groupPrefix,
                columns, companyName, signerTitle, showGroupTotals, pageFooter, dateFormat, showStt,
                landscape, fontSizePt, dataSetName);
        }

        /// <summary>
        /// Sinh và lưu ra file .rdlc chuẩn (nếu <paramref name="filePath"/> là null sẽ mở SaveFileDialog).
        /// </summary>
        public static bool ExportRdlcFile(DataTable table, string reportTitle = "BÁO CÁO THỐNG KÊ",
            string filterCondition = null, string groupByColumn = null, string groupPrefix = null,
            IEnumerable<ReportColumn> columns = null, string companyName = "ĐƠN VỊ QUẢN LÝ",
            string signerTitle = "Người lập biểu", bool showGroupTotals = false, string pageFooter = "Trang 1/1",
            string dateFormat = "dd/MM/yyyy", bool showStt = true, bool landscape = false, int fontSizePt = 13,
            string dataSetName = "dsBaoCao", string filePath = null)
        {
            string xml = GenerateRdlcXml(table, reportTitle, filterCondition, groupByColumn, groupPrefix,
                columns, companyName, signerTitle, showGroupTotals, pageFooter, dateFormat, showStt,
                landscape, fontSizePt, dataSetName);

            string targetPath = filePath;
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                string safeName = SanitizeFileName(reportTitle, "BaoCao");
                if (!safeName.EndsWith(".rdlc", StringComparison.OrdinalIgnoreCase))
                    safeName += ".rdlc";

                using (var sfd = new SaveFileDialog
                {
                    Filter = "RDLC Report File (*.rdlc)|*.rdlc",
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
                string dir = Path.GetDirectoryName(Path.GetFullPath(targetPath));
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(targetPath, xml, new UTF8Encoding(true));
                if (string.IsNullOrWhiteSpace(filePath))
                    MessageHelper.Success("xuất file thiết kế .rdlc");
                return true;
            }
            catch (Exception ex)
            {
                MessageHelper.Error("Lỗi khi lưu file .rdlc: " + ex.Message);
                return false;
            }
        }

        #endregion
    }
}
