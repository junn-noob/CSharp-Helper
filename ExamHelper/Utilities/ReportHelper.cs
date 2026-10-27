using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace ExamHelper.Utilities
{
    /// <summary>
    /// Định nghĩa cấu hình một cột trong báo cáo HTML/Excel.
    /// </summary>
    public class ReportColumn
    {
        /// <summary>Tên cột trong DataTable nguồn.</summary>
        public string ColumnName { get; set; }

        /// <summary>Tiêu đề hiển thị trên đầu cột báo cáo.</summary>
        public string HeaderText { get; set; }

        /// <summary>Định dạng dữ liệu (ví dụ: "N0", "dd/MM/yyyy", "0.##").</summary>
        public string Format { get; set; }

        /// <summary>Căn lề cột (Trái, Giữa, Phải).</summary>
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        /// <summary>Cột này có cần tính tổng ở dòng Tổng cộng hay không.</summary>
        public bool IsSum { get; set; }

        /// <summary>Khởi tạo cột báo cáo đầy đủ thông số.</summary>
        public ReportColumn(string columnName, string headerText = null, string format = null, 
            HorizontalAlignment alignment = HorizontalAlignment.Left, bool isSum = false)
        {
            ColumnName = columnName;
            HeaderText = headerText ?? columnName;
            Format = format;
            Alignment = alignment;
            IsSum = isSum;
        }

        /// <summary>Khởi tạo cột báo cáo căn lề nhanh.</summary>
        public ReportColumn(string columnName, string headerText, HorizontalAlignment alignment)
            : this(columnName, headerText, null, alignment, false) { }
    }

    /// <summary>
    /// Lớp tiện ích tạo báo cáo HTML chuẩn in ấn, xem trước qua WebBrowser và xuất file Excel (.xls) dùng chung cho mọi bài thi.
    /// </summary>
    public static class ReportHelper
    {
        /// <summary>
        /// Tạo chuỗi mã HTML báo cáo hoàn chỉnh từ DataTable (hỗ trợ gom nhóm, tính tổng, phân trang).
        /// </summary>
        /// <param name="table">DataTable nguồn chứa dữ liệu.</param>
        /// <param name="reportTitle">Tiêu đề lớn của báo cáo.</param>
        /// <param name="filterCondition">Chuỗi mô tả điều kiện lọc (ví dụ: "Phòng ban: Tất cả").</param>
        /// <param name="groupByColumn">Tên cột dùng để gom nhóm (ví dụ: "TenPB", "TenCK"). Để null nếu không gom nhóm.</param>
        /// <param name="groupPrefix">Tiền tố hiển thị trước tên nhóm (ví dụ: "Phòng ban: ", "Chuyên khoa: ").</param>
        /// <param name="columns">Danh sách cấu hình các cột hiển thị. Nếu null sẽ tự động lấy tất cả các cột.</param>
        /// <param name="companyName">Tên cơ quan / doanh nghiệp ở góc trên bên trái.</param>
        /// <param name="signerTitle">Chức danh người ký dưới chân báo cáo.</param>
        /// <returns>Chuỗi mã HTML hoàn chỉnh sẵn sàng nạp vào WebBrowser hoặc lưu thành file .xls.</returns>
        public static string GenerateHtml(DataTable table, string reportTitle, string filterCondition = null,
            string groupByColumn = null, string groupPrefix = null, IEnumerable<ReportColumn> columns = null,
            string companyName = "ĐƠN VỊ QUẢN LÝ", string signerTitle = "Người lập biểu")
        {
            if (table == null) table = new DataTable();

            var colList = columns?.ToList();
            if (colList == null || colList.Count == 0)
            {
                // Tự động nhận diện cột từ DataTable
                colList = new List<ReportColumn>();
                foreach (DataColumn dc in table.Columns)
                {
                    var align = HorizontalAlignment.Left;
                    string format = null;
                    bool isSum = false;

                    if (dc.DataType == typeof(DateTime))
                    {
                        align = HorizontalAlignment.Center;
                        format = "dd/MM/yyyy";
                    }
                    else if (dc.DataType == typeof(decimal) || dc.DataType == typeof(double) || dc.DataType == typeof(int) || dc.DataType == typeof(long))
                    {
                        align = HorizontalAlignment.Right;
                        format = "N0";
                        isSum = true;
                    }
                    else if (dc.DataType == typeof(bool))
                    {
                        align = HorizontalAlignment.Center;
                    }

                    colList.Add(new ReportColumn(dc.ColumnName, dc.ColumnName, format, align, isSum));
                }
            }

            var sb = new StringBuilder();
            sb.Append(@"<!DOCTYPE html><html><head><meta charset='utf-8'><title>" + EscapeHtml(reportTitle) + @"</title><style>
                body { font-family: 'Times New Roman', serif; margin: 25px 35px; font-size: 14px; color: #222; }
                table { width: 100%; border-collapse: collapse; margin-top: 15px; }
                th, td { border: 1px solid #444; padding: 6px 8px; font-size: 13.5px; }
                th { background-color: #dbe5f1; text-align: center; font-weight: bold; }
                .nhom { font-weight: bold; background-color: #f2f4f7; color: #1f497d; font-size: 14px; }
                .subtotal { font-style: italic; background-color: #fafbfc; font-weight: bold; }
                .tong { font-weight: bold; background-color: #fde9d9; font-size: 14px; }
                .c { text-align: center; } .r { text-align: right; } .l { text-align: left; }
                @media print {
                    body { margin: 15mm; }
                    .no-print { display: none; }
                }
            </style></head><body>");

            // Phần đầu biểu mẫu
            sb.Append("<table><tr>");
            sb.Append("<td style='border:none;font-weight:bold;text-transform:uppercase;'>" + EscapeHtml(companyName) + "</td>");
            sb.Append("<td style='border:none;text-align:right;font-style:italic;'>Ngày lập: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "</td>");
            sb.Append("</tr></table>");

            sb.Append("<h2 style='text-align:center;margin:15px 0 5px;color:#1f497d;text-transform:uppercase;'>" + EscapeHtml(reportTitle) + "</h2>");

            if (!string.IsNullOrWhiteSpace(filterCondition))
            {
                sb.Append("<p style='text-align:center;font-style:italic;margin:2px 0 15px;color:#555;'>Điều kiện lọc: " + EscapeHtml(filterCondition) + "</p>");
            }

            // Bắt đầu bảng dữ liệu
            sb.Append("<table><thead><tr><th style='width:45px;'>STT</th>");
            foreach (var col in colList)
            {
                sb.Append("<th>" + EscapeHtml(col.HeaderText) + "</th>");
            }
            sb.Append("</tr></thead><tbody>");

            int totalColumns = colList.Count + 1; // +1 cho cột STT
            int stt = 1;
            var grandTotals = new Dictionary<string, decimal>();
            foreach (var col in colList.Where(c => c.IsSum)) grandTotals[col.ColumnName] = 0;

            bool hasGrouping = !string.IsNullOrEmpty(groupByColumn) && table.Columns.Contains(groupByColumn);

            if (hasGrouping)
            {
                DataTable distinctGroups = table.DefaultView.ToTable(true, groupByColumn);
                string prefix = groupPrefix ?? (groupByColumn + ": ");

                foreach (DataRow gRow in distinctGroups.Rows)
                {
                    string groupVal = gRow[groupByColumn]?.ToString() ?? "";
                    string safeVal = groupVal.Replace("'", "''");
                    DataRow[] rows = table.Select($"[{groupByColumn}] = '{safeVal}'");
                    if (rows.Length == 0) continue;

                    sb.Append($"<tr><td colspan='{totalColumns}' class='nhom'>{EscapeHtml(prefix + groupVal)} ({rows.Length} bản ghi)</td></tr>");

                    var groupTotals = new Dictionary<string, decimal>();
                    foreach (var col in colList.Where(c => c.IsSum)) groupTotals[col.ColumnName] = 0;

                    foreach (DataRow r in rows)
                    {
                        RenderRow(sb, r, colList, stt++);
                        AccumulateSums(r, colList, groupTotals, grandTotals);
                    }

                    // Dòng tổng cộng nhóm (nếu có cột tính tổng)
                    if (groupTotals.Count > 0)
                    {
                        sb.Append($"<tr class='subtotal'><td colspan='2' class='c'>Tổng {EscapeHtml(groupVal)}:</td>");
                        RenderSumCells(sb, colList, groupTotals, 2);
                        sb.Append("</tr>");
                    }
                }
            }
            else
            {
                foreach (DataRow r in table.Rows)
                {
                    RenderRow(sb, r, colList, stt++);
                    AccumulateSums(r, colList, null, grandTotals);
                }
            }

            // Dòng TỔNG CỘNG chung
            if (grandTotals.Count > 0)
            {
                sb.Append($"<tr class='tong'><td colspan='2' class='c'>TỔNG CỘNG</td>");
                RenderSumCells(sb, colList, grandTotals, 2);
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");

            // Chữ ký Người lập biểu
            sb.Append("<table style='margin-top:35px;'><tr><td style='border:none;'></td><td style='border:none;text-align:right;padding-right:45px;'><b>" + EscapeHtml(signerTitle) + "</b><br><i>(Ký, ghi rõ họ tên)</i><br><br><br><br></td></tr></table>");
            sb.Append("</body></html>");

            return sb.ToString();
        }

        private static void RenderRow(StringBuilder sb, DataRow r, List<ReportColumn> colList, int stt)
        {
            sb.Append($"<tr><td class='c'>{stt}</td>");
            foreach (var col in colList)
            {
                string alignClass = col.Alignment == HorizontalAlignment.Center ? "c" : (col.Alignment == HorizontalAlignment.Right ? "r" : "l");
                object val = r.Table.Columns.Contains(col.ColumnName) ? r[col.ColumnName] : null;
                string displayText = FormatValue(val, col.Format);
                sb.Append($"<td class='{alignClass}'>{EscapeHtml(displayText)}</td>");
            }
            sb.Append("</tr>");
        }

        private static void AccumulateSums(DataRow r, List<ReportColumn> colList, 
            Dictionary<string, decimal> groupTotals, Dictionary<string, decimal> grandTotals)
        {
            foreach (var col in colList.Where(c => c.IsSum))
            {
                if (r.Table.Columns.Contains(col.ColumnName) && r[col.ColumnName] != DBNull.Value)
                {
                    decimal d = DataHelper.ToDecimal(r[col.ColumnName], 0);
                    if (groupTotals != null) groupTotals[col.ColumnName] += d;
                    if (grandTotals != null) grandTotals[col.ColumnName] += d;
                }
            }
        }

        private static void RenderSumCells(StringBuilder sb, List<ReportColumn> colList, 
            Dictionary<string, decimal> totals, int skippedFirstCols)
        {
            // skippedFirstCols là số cột đã bị gộp ở ô tiêu đề "TỔNG CỘNG" (thường là 2: STT + cột đầu tiên)
            for (int i = skippedFirstCols - 1; i < colList.Count; i++)
            {
                var col = colList[i];
                if (col.IsSum && totals.ContainsKey(col.ColumnName))
                {
                    string fmt = col.Format ?? "N0";
                    sb.Append($"<td class='r'>{totals[col.ColumnName].ToString(fmt)}</td>");
                }
                else
                {
                    sb.Append("<td></td>");
                }
            }
        }

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
        /// Mở hộp thoại lưu file và xuất nội dung HTML thành file Excel (.xls) có định dạng chuẩn.
        /// </summary>
        /// <param name="htmlContent">Chuỗi HTML của báo cáo.</param>
        /// <param name="defaultFileName">Tên file mặc định khi lưu (mặc định: "BaoCao.xls").</param>
        /// <returns>True nếu người dùng đã lưu thành công, False nếu bấm Hủy.</returns>
        public static bool ExportExcel(string htmlContent, string defaultFileName = "BaoCao.xls")
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xls)|*.xls|HTML File (*.html)|*.html",
                FileName = defaultFileName
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(sfd.FileName, htmlContent, Encoding.UTF8);
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
        /// Hiển thị Form Báo Cáo dùng chung dạng Modal Dialog với đầy đủ tính năng: Xem trước, In ấn và Xuất Excel.
        /// </summary>
        public static void ShowReport(DataTable dataTable, string reportTitle, string filterCondition = null,
            string groupByColumn = null, string groupPrefix = null, IEnumerable<ReportColumn> columns = null,
            string companyName = "ĐƠN VỊ QUẢN LÝ", IWin32Window owner = null)
        {
            string html = GenerateHtml(dataTable, reportTitle, filterCondition, groupByColumn, groupPrefix, columns, companyName);
            using (var form = new FormReportViewer(html, reportTitle))
            {
                if (owner != null) form.ShowDialog(owner);
                else form.ShowDialog();
            }
        }

        /// <summary>
        /// Hiển thị báo cáo nhanh trong 1 dòng code (tự động gom nhóm và tính tổng cột chỉ định).
        /// </summary>
        public static void ShowReport(DataTable dataTable, string reportTitle, string groupByColumn, 
            string sumColumn, string filterCondition = null, IWin32Window owner = null)
        {
            var cols = new List<ReportColumn>();
            foreach (DataColumn dc in dataTable.Columns)
            {
                if (dc.ColumnName == groupByColumn) continue; // Ẩn cột gom nhóm khỏi bảng chi tiết để đỡ trùng

                bool isSum = (dc.ColumnName == sumColumn);
                string fmt = isSum ? "N0" : (dc.DataType == typeof(DateTime) ? "dd/MM/yyyy" : null);
                var align = isSum ? HorizontalAlignment.Right : (dc.DataType == typeof(DateTime) ? HorizontalAlignment.Center : HorizontalAlignment.Left);
                cols.Add(new ReportColumn(dc.ColumnName, dc.ColumnName, fmt, align, isSum));
            }

            ShowReport(dataTable, reportTitle, filterCondition, groupByColumn, null, cols, owner: owner);
        }
    }

    /// <summary>
    /// Form hiển thị báo cáo dùng chung có sẵn thanh công cụ: In ấn, Xuất Excel, Đóng.
    /// </summary>
    public class FormReportViewer : Form
    {
        private readonly string _htmlContent;
        private WebBrowser webViewer;
        private Button btnIn;
        private Button btnXuatExcel;
        private Button btnDong;

        /// <summary>Khởi tạo form xem báo cáo với mã HTML đã có sẵn.</summary>
        public FormReportViewer(string htmlContent, string title = "Báo cáo thống kê")
        {
            _htmlContent = htmlContent ?? "";
            InitializeComponents(title);
        }

        private void InitializeComponents(string title)
        {
            this.Text = title;
            this.Size = new Size(960, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) this.Close(); };

            // Panel thanh công cụ phía trên
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(245, 247, 250)
            };

            btnIn = new Button
            {
                Text = "🖨️ In báo cáo (&P)",
                Width = 135,
                Height = 32,
                Left = 15,
                Top = 7,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            btnIn.Click += (s, e) => webViewer.ShowPrintDialog();

            btnXuatExcel = new Button
            {
                Text = "📊 Xuất Excel (&E)",
                Width = 135,
                Height = 32,
                Left = 160,
                Top = 7
            };
            btnXuatExcel.Click += (s, e) => ReportHelper.ExportExcel(_htmlContent, this.Text + ".xls");

            btnDong = new Button
            {
                Text = "Đóng (&N)",
                Width = 95,
                Height = 32,
                Left = 305,
                Top = 7
            };
            btnDong.Click += (s, e) => this.Close();

            topPanel.Controls.AddRange(new Control[] { btnIn, btnXuatExcel, btnDong });

            // Control WebBrowser hiển thị HTML
            webViewer = new WebBrowser
            {
                Dock = DockStyle.Fill,
                IsWebBrowserContextMenuEnabled = true
            };

            this.Controls.Add(webViewer);
            this.Controls.Add(topPanel);

            this.Load += (s, e) => webViewer.DocumentText = _htmlContent;
        }
    }
}
