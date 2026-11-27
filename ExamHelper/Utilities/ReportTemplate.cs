using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ExamHelper.Utilities
{
    /// <summary>
    /// Cấu hình lưu trữ một cột trong file mẫu báo cáo (<see cref="ReportTemplate"/>).
    /// </summary>
    [Serializable]
    public class ReportTemplateColumn
    {
        public string ColumnName { get; set; }
        public string HeaderText { get; set; }
        public string Format { get; set; }
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;
        public ReportSummaryMode SummaryMode { get; set; } = ReportSummaryMode.None;
        public bool Visible { get; set; } = true;

        public ReportTemplateColumn() { }

        public ReportTemplateColumn(ReportColumn col, bool visible)
        {
            if (col == null) return;
            ColumnName = col.ColumnName;
            HeaderText = col.HeaderText;
            Format = col.Format;
            Alignment = col.Alignment;
            SummaryMode = col.SummaryMode;
            Visible = visible;
        }

        public ReportColumn ToReportColumn()
        {
            return new ReportColumn(ColumnName, HeaderText, Format, Alignment, SummaryMode);
        }
    }

    /// <summary>
    /// File mẫu cấu hình báo cáo (.examrpt / .xml) có thể lưu ra đĩa và nạp lại cho bất kỳ DataTable nào.
    /// </summary>
    [Serializable]
    [XmlRoot("ExamReportTemplate")]
    public class ReportTemplate
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(ReportTemplate));

        public string Version { get; set; } = "1.0";
        public string ReportTitle { get; set; } = "BÁO CÁO THỐNG KÊ";
        public string CompanyName { get; set; } = "ĐƠN VỊ QUẢN LÝ";
        public string FilterCondition { get; set; } = "";
        public string GroupByColumn { get; set; } = "";
        public string GroupPrefix { get; set; } = "";
        public string SignerTitle { get; set; } = "Người lập biểu";
        public string PageFooter { get; set; } = "Trang 1/1";
        public string DateFormat { get; set; } = "dd/MM/yyyy";
        public bool ShowGroupTotals { get; set; }
        public bool ShowStt { get; set; } = true;
        public bool Landscape { get; set; }
        public int FontSizePt { get; set; } = 13;

        [XmlArray("Columns")]
        [XmlArrayItem("Column")]
        public List<ReportTemplateColumn> Columns { get; set; } = new List<ReportTemplateColumn>();

        /// <summary>Chuyển mẫu cấu hình thành chuỗi XML UTF-8.</summary>
        public string ToXml()
        {
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms, new UTF8Encoding(false)))
            {
                Serializer.Serialize(writer, this);
                writer.Flush();
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>Đọc mẫu cấu hình từ chuỗi XML.</summary>
        public static ReportTemplate FromXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentException("Nội dung XML mẫu báo cáo đang trống.", nameof(xml));

            using (var reader = new StringReader(xml))
            {
                return (ReportTemplate)Serializer.Deserialize(reader);
            }
        }

        /// <summary>Lưu mẫu báo cáo ra file (.examrpt / .xml).</summary>
        public void Save(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Đường dẫn file không được để trống.", nameof(filePath));

            string dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(filePath, ToXml(), new UTF8Encoding(true));
        }

        /// <summary>Nạp mẫu báo cáo từ file trên đĩa.</summary>
        public static ReportTemplate Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Đường dẫn file không được để trống.", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Không tìm thấy file mẫu báo cáo: " + filePath, filePath);

            string xml = File.ReadAllText(filePath, Encoding.UTF8);
            return FromXml(xml);
        }

        /// <summary>
        /// Tạo <see cref="ReportDesignOptions"/> từ mẫu đã lưu để mở <see cref="FormReportViewer"/>
        /// hoặc dựng HTML/Excel mà vẫn khớp với cột hiện có của <paramref name="sourceTable"/>.
        /// </summary>
        public ReportDesignOptions ToDesignOptions(DataTable sourceTable = null, string overrideFilterCondition = null, bool openDesigner = true)
        {
            var visibleCols = new List<ReportColumn>();
            var savedHiddenCols = new List<ReportColumn>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (Columns != null)
            {
                foreach (var tc in Columns)
                {
                    if (tc == null || string.IsNullOrWhiteSpace(tc.ColumnName)) continue;
                    if (sourceTable != null && !sourceTable.Columns.Contains(tc.ColumnName)) continue;
                    if (!seenNames.Add(tc.ColumnName)) continue;

                    if (tc.Visible)
                        visibleCols.Add(tc.ToReportColumn());
                    else
                        savedHiddenCols.Add(tc.ToReportColumn());
                }
            }

            string groupCol = string.IsNullOrWhiteSpace(GroupByColumn) ? null : GroupByColumn.Trim();
            if (groupCol != null && sourceTable != null && !sourceTable.Columns.Contains(groupCol))
                groupCol = null;

            return new ReportDesignOptions
            {
                ReportTitle = string.IsNullOrWhiteSpace(ReportTitle) ? "BÁO CÁO THỐNG KÊ" : ReportTitle,
                CompanyName = CompanyName ?? "ĐƠN VỊ QUẢN LÝ",
                FilterCondition = overrideFilterCondition != null ? overrideFilterCondition : (FilterCondition ?? ""),
                GroupByColumn = groupCol,
                GroupPrefix = GroupPrefix ?? "",
                Columns = visibleCols,
                HiddenColumns = savedHiddenCols,
                SignerTitle = SignerTitle ?? "Người lập biểu",
                PageFooter = PageFooter ?? "Trang 1/1",
                DateFormat = string.IsNullOrWhiteSpace(DateFormat) ? "dd/MM/yyyy" : DateFormat,
                ShowGroupTotals = ShowGroupTotals,
                ShowStt = ShowStt,
                Landscape = Landscape,
                FontSizePt = FontSizePt <= 0 ? 13 : FontSizePt,
                OpenDesigner = openDesigner
            };
        }
    }
}
