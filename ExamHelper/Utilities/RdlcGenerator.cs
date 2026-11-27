using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ExamHelper.Utilities
{
    /// <summary>
    /// Trình sinh file báo cáo chuẩn Microsoft RDLC (RDL 2008 XML) từ DataTable và cấu hình cột trực quan.
    /// File sinh ra mở được trong Visual Studio Report Designer và nạp được qua ReportHelper.BindRdlc.
    /// </summary>
    internal static class RdlcGenerator
    {
        private static readonly XNamespace Ns = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";
        private static readonly XNamespace Rd = "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner";

        public static string GenerateXml(
            DataTable table,
            string reportTitle,
            string filterCondition = null,
            string groupByColumn = null,
            string groupPrefix = null,
            IEnumerable<ReportColumn> columns = null,
            string companyName = "ĐƠN VỊ QUẢN LÝ",
            string signerTitle = "Người lập biểu",
            bool showGroupTotals = false,
            string pageFooter = "Trang 1/1",
            string dateFormat = "dd/MM/yyyy",
            bool showStt = true,
            bool landscape = false,
            int fontSizePt = 13,
            string dataSetName = "dsBaoCao")
        {
            if (table == null) table = new DataTable("BaoCao");
            string safeDataSetName = SanitizeIdentifier(string.IsNullOrWhiteSpace(dataSetName) ? "dsBaoCao" : dataSetName, "dsBaoCao");
            string safeTableName = SanitizeIdentifier(string.IsNullOrWhiteSpace(table.TableName) ? "BaoCaoTable" : table.TableName, "BaoCaoTable");

            var colList = columns?.Where(c => c != null && !string.IsNullOrWhiteSpace(c.ColumnName)).ToList();
            if (colList == null || colList.Count == 0)
            {
                colList = ReportHelper.BuildAutoColumns(table, groupByColumn, null);
            }

            string normalizedGroup = string.IsNullOrWhiteSpace(groupByColumn) ? null : groupByColumn.Trim();
            bool hasGrouping = !string.IsNullOrEmpty(normalizedGroup)
                && (table.Columns.Count == 0 || table.Columns.Contains(normalizedGroup));

            var fieldInfos = BuildFieldInfos(table, colList, hasGrouping ? normalizedGroup : null);
            var colClsMap = fieldInfos.ToDictionary(f => f.ColumnName, f => f.ClsName, StringComparer.OrdinalIgnoreCase);

            int pt = Math.Max(8, Math.Min(18, fontSizePt <= 0 ? 11 : fontSizePt - 2));
            double pageWidthCm = landscape ? 29.7 : 21.0;
            double pageHeightCm = landscape ? 21.0 : 29.7;
            double marginCm = 1.2;
            double usableWidthCm = pageWidthCm - marginCm * 2;

            List<double> colWidths = ComputeColumnWidthsCm(table, colList, showStt, usableWidthCm);
            double tableWidthCm = colWidths.Sum();

            bool hasSummary = colList.Any(c => c.SummaryMode != ReportSummaryMode.None);
            string safeDateFmt = string.IsNullOrWhiteSpace(dateFormat) ? "dd/MM/yyyy" : dateFormat;

            var reportItems = new List<XElement>();
            double cursorY = 0.0;

            // Header đơn vị + Ngày lập
            double halfWidth = Math.Round(usableWidthCm / 2.0, 3);
            reportItems.Add(CreateTextbox(
                "txtCompanyHeader",
                0, cursorY, halfWidth, 0.7,
                "=Parameters!pDonVi.Value",
                pt, true, "Left", "#111827"));

            reportItems.Add(CreateTextbox(
                "txtPrintDate",
                halfWidth, cursorY, usableWidthCm - halfWidth, 0.7,
                $"=\"Ngày lập: \" & Format(Now(), \"{EscapeRdlString(safeDateFmt)}\")",
                Math.Max(8, pt - 1), false, "Right", "#374151", italic: true));
            cursorY += 0.9;

            // Tiêu đề lớn
            reportItems.Add(CreateTextbox(
                "txtReportTitle",
                0, cursorY, usableWidthCm, 0.9,
                "=Parameters!pTieuDe.Value",
                pt + 4, true, "Center", "#1F497D"));
            cursorY += 0.95;

            // Điều kiện lọc
            reportItems.Add(CreateTextbox(
                "txtFilterCondition",
                0, cursorY, usableWidthCm, 0.65,
                "=IIF(Parameters!pDieuKienLoc.Value = \"\", \"\", \"Điều kiện lọc: \" & Parameters!pDieuKienLoc.Value)",
                Math.Max(8, pt - 1), false, "Center", "#4B5563", italic: true));
            cursorY += 0.85;

            // Bảng Tablix chính
            string groupPrefixText = !string.IsNullOrEmpty(groupPrefix) ? groupPrefix : (hasGrouping ? normalizedGroup + ": " : "");
            XElement tablix = BuildTablix(
                colList, colClsMap, colWidths, tableWidthCm, cursorY, pt,
                showStt, hasGrouping, normalizedGroup, groupPrefixText, showGroupTotals, hasSummary,
                out double tablixHeightCm);
            reportItems.Add(tablix);
            cursorY += tablixHeightCm + 0.6;

            // Chữ ký Người lập biểu
            if (!string.IsNullOrWhiteSpace(signerTitle))
            {
                double sigWidth = Math.Min(7.0, usableWidthCm * 0.4);
                reportItems.Add(CreateTextbox(
                    "txtSignerTitle",
                    usableWidthCm - sigWidth, cursorY, sigWidth, 1.4,
                    "=Parameters!pNguoiKy.Value & vbCrLf & \"(Ký, ghi rõ họ tên)\"",
                    pt, true, "Center", "#111827"));
                cursorY += 1.6;
            }

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(Ns + "Report",
                    new XAttribute(XNamespace.Xmlns + "rd", Rd.NamespaceName),
                    new XElement(Ns + "DataSources",
                        new XElement(Ns + "DataSource",
                            new XAttribute("Name", "ExamHelperDataSource"),
                            new XElement(Ns + "ConnectionProperties",
                                new XElement(Ns + "DataProvider", "System.Data.DataSet"),
                                new XElement(Ns + "ConnectString", "/* Local Connection */")),
                            new XElement(Rd + "DataSourceID", Guid.NewGuid().ToString()))),
                    new XElement(Ns + "DataSets",
                        new XElement(Ns + "DataSet",
                            new XAttribute("Name", safeDataSetName),
                            new XElement(Ns + "Query",
                                new XElement(Ns + "DataSourceName", "ExamHelperDataSource"),
                                new XElement(Ns + "CommandText", "/* Local Query */")),
                            new XElement(Ns + "Fields",
                                fieldInfos.Select(f =>
                                    new XElement(Ns + "Field",
                                        new XAttribute("Name", f.ClsName),
                                        new XElement(Ns + "DataField", f.ColumnName),
                                        new XElement(Rd + "TypeName", f.TypeFullName)))),
                            new XElement(Rd + "DataSetInfo",
                                new XElement(Rd + "DataSetName", safeDataSetName),
                                new XElement(Rd + "TableName", safeTableName)))),
                    new XElement(Ns + "ReportParameters",
                        CreateReportParameter("pTieuDe", string.IsNullOrWhiteSpace(reportTitle) ? "BÁO CÁO THỐNG KÊ" : reportTitle.ToUpperInvariant()),
                        CreateReportParameter("pDonVi", (companyName ?? "ĐƠN VỊ QUẢN LÝ").ToUpperInvariant()),
                        CreateReportParameter("pDieuKienLoc", filterCondition ?? ""),
                        CreateReportParameter("pNguoiKy", signerTitle ?? "Người lập biểu")),
                    new XElement(Ns + "Body",
                        new XElement(Ns + "ReportItems", reportItems),
                        new XElement(Ns + "Height", Cm(Math.Max(6.0, cursorY + 0.5))),
                        new XElement(Ns + "Style")),
                    new XElement(Ns + "Width", Cm(usableWidthCm)),
                    new XElement(Ns + "Page",
                        BuildPageFooter(pageFooter, usableWidthCm, Math.Max(8, pt - 1)),
                        new XElement(Ns + "PageHeight", Cm(pageHeightCm)),
                        new XElement(Ns + "PageWidth", Cm(pageWidthCm)),
                        new XElement(Ns + "LeftMargin", Cm(marginCm)),
                        new XElement(Ns + "RightMargin", Cm(marginCm)),
                        new XElement(Ns + "TopMargin", Cm(marginCm)),
                        new XElement(Ns + "BottomMargin", Cm(marginCm)),
                        new XElement(Ns + "Style")),
                    new XElement(Rd + "ReportUnitType", "Cm"),
                    new XElement(Rd + "ReportID", Guid.NewGuid().ToString())));

            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms, new UTF8Encoding(false)))
            {
                doc.Save(writer);
                writer.Flush();
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private static XElement BuildPageFooter(string pageFooter, double usableWidthCm, int pt)
        {
            if (string.IsNullOrWhiteSpace(pageFooter))
            {
                return new XElement(Ns + "PageFooter",
                    new XElement(Ns + "Height", "0.2cm"),
                    new XElement(Ns + "PrintOnFirstPage", "false"),
                    new XElement(Ns + "PrintOnLastPage", "false"));
            }

            string expr = pageFooter.IndexOf("Trang", StringComparison.OrdinalIgnoreCase) >= 0
                ? "=\"Trang \" & Globals!PageNumber & \"/\" & Globals!TotalPages"
                : pageFooter;

            return new XElement(Ns + "PageFooter",
                new XElement(Ns + "Height", "0.7cm"),
                new XElement(Ns + "PrintOnFirstPage", "true"),
                new XElement(Ns + "PrintOnLastPage", "true"),
                new XElement(Ns + "ReportItems",
                    CreateTextbox("txtPageFooter", 0, 0.1, usableWidthCm, 0.55, expr, pt, false, "Center", "#4B5563", italic: true)),
                new XElement(Ns + "Style"));
        }

        private static XElement BuildTablix(
            List<ReportColumn> cols,
            Dictionary<string, string> colClsMap,
            List<double> widthsCm,
            double tableWidthCm,
            double topCm,
            int pt,
            bool showStt,
            bool hasGrouping,
            string groupByColumn,
            string groupPrefix,
            bool showGroupTotals,
            bool hasSummary,
            out double heightCm)
        {
            int totalCols = cols.Count + (showStt ? 1 : 0);
            var rows = new List<XElement>();
            double rowH = 0.72;
            heightCm = 0;

            // 1. Header row
            rows.Add(BuildHeaderRow(cols, showStt, rowH, pt));
            heightCm += rowH;

            // 2. Group header row (merged across all columns)
            string safeGroupField = hasGrouping && colClsMap.TryGetValue(groupByColumn, out string gCls) ? gCls : null;
            if (hasGrouping && safeGroupField != null)
            {
                string grpExpr = $"=\"{EscapeRdlString(groupPrefix)}\" & Fields!{safeGroupField}.Value & \" (\" & CountRows() & \" bản ghi)\"";
                rows.Add(BuildMergedRow("grpHdr", grpExpr, totalCols, rowH, pt, "#F2F4F7", "#1F497D", bold: true, italic: false, align: "Left"));
                heightCm += rowH;
            }

            // 3. Detail row
            rows.Add(BuildDetailRow(cols, colClsMap, showStt, rowH, pt));
            heightCm += rowH;

            // 4. Optional group subtotal row
            bool includeGroupSubtotal = hasGrouping && safeGroupField != null && showGroupTotals && hasSummary;
            if (includeGroupSubtotal)
            {
                rows.Add(BuildSummaryRow("sub", "Tổng nhóm:", cols, colClsMap, showStt, rowH, pt, "#FAFBFD", bold: true, italic: true));
                heightCm += rowH;
            }

            // 5. Grand total row
            if (hasSummary)
            {
                rows.Add(BuildSummaryRow("tot", "TỔNG CỘNG", cols, colClsMap, showStt, rowH, pt, "#FDE9D9", bold: true, italic: false));
                heightCm += rowH;
            }

            var colHierarchy = new XElement(Ns + "TablixColumnHierarchy",
                new XElement(Ns + "TablixMembers",
                    widthsCm.Select(_ => new XElement(Ns + "TablixMember"))));

            var rowMembers = new List<XElement>
            {
                // Header static member
                new XElement(Ns + "TablixMember",
                    new XElement(Ns + "KeepWithGroup", "After"),
                    new XElement(Ns + "RepeatOnNewPage", "true"))
            };

            if (hasGrouping && safeGroupField != null)
            {
                var innerMembers = new List<XElement>
                {
                    new XElement(Ns + "TablixMember",
                        new XElement(Ns + "KeepWithGroup", "After")),
                    new XElement(Ns + "TablixMember",
                        new XElement(Ns + "Group", new XAttribute("Name", "Details")))
                };
                if (includeGroupSubtotal)
                {
                    innerMembers.Add(new XElement(Ns + "TablixMember",
                        new XElement(Ns + "KeepWithGroup", "Before")));
                }

                rowMembers.Add(
                    new XElement(Ns + "TablixMember",
                        new XElement(Ns + "Group",
                            new XAttribute("Name", "grp_" + safeGroupField),
                            new XElement(Ns + "GroupExpressions",
                                new XElement(Ns + "GroupExpression", $"=Fields!{safeGroupField}.Value"))),
                        new XElement(Ns + "SortExpressions",
                            new XElement(Ns + "SortExpression",
                                new XElement(Ns + "Value", $"=Fields!{safeGroupField}.Value"))),
                        new XElement(Ns + "TablixMembers", innerMembers)));
            }
            else
            {
                rowMembers.Add(new XElement(Ns + "TablixMember",
                    new XElement(Ns + "Group", new XAttribute("Name", "Details"))));
            }

            if (hasSummary)
            {
                rowMembers.Add(new XElement(Ns + "TablixMember",
                    new XElement(Ns + "KeepWithGroup", "Before")));
            }

            return new XElement(Ns + "Tablix",
                new XAttribute("Name", "tblReportData"),
                new XElement(Ns + "TablixBody",
                    new XElement(Ns + "TablixColumns",
                        widthsCm.Select(w => new XElement(Ns + "TablixColumn", new XElement(Ns + "Width", Cm(w))))),
                    new XElement(Ns + "TablixRows", rows)),
                colHierarchy,
                new XElement(Ns + "TablixRowHierarchy",
                    new XElement(Ns + "TablixMembers", rowMembers)),
                new XElement(Ns + "Top", Cm(topCm)),
                new XElement(Ns + "Left", "0cm"),
                new XElement(Ns + "Height", Cm(heightCm)),
                new XElement(Ns + "Width", Cm(tableWidthCm)),
                new XElement(Ns + "Style",
                    new XElement(Ns + "Border",
                        new XElement(Ns + "Style", "None"))));
        }

        private static XElement BuildHeaderRow(List<ReportColumn> cols, bool showStt, double rowH, int pt)
        {
            var cells = new List<XElement>();
            int idx = 0;

            if (showStt)
            {
                cells.Add(CreateGridCell($"hdr_{idx++}_STT", "STT", pt, "#DBE5F1", "#111827", true, false, "Center", null));
            }

            foreach (var col in cols)
            {
                cells.Add(CreateGridCell($"hdr_{idx++}", col.HeaderText ?? col.ColumnName, pt, "#DBE5F1", "#111827", true, false, "Center", null));
            }

            return new XElement(Ns + "TablixRow",
                new XElement(Ns + "Height", Cm(rowH)),
                new XElement(Ns + "TablixCells", cells));
        }

        private static XElement BuildDetailRow(List<ReportColumn> cols, Dictionary<string, string> colClsMap, bool showStt, double rowH, int pt)
        {
            var cells = new List<XElement>();
            int idx = 0;

            if (showStt)
            {
                cells.Add(CreateGridCell($"dtl_{idx++}_STT", "=RowNumber(Nothing)", pt, "#FFFFFF", "#111827", false, false, "Center", null));
            }

            foreach (var col in cols)
            {
                string fieldCls = colClsMap.TryGetValue(col.ColumnName, out string mapped)
                    ? mapped
                    : SanitizeIdentifier(col.ColumnName, "Col" + idx);
                string align = MapRdlAlign(col.Alignment);
                string rdlFormat = ConvertNetFormatToRdl(col.Format);
                cells.Add(CreateGridCell($"dtl_{idx++}", $"=Fields!{fieldCls}.Value", pt, "#FFFFFF", "#111827", false, false, align, rdlFormat));
            }

            return new XElement(Ns + "TablixRow",
                new XElement(Ns + "Height", Cm(rowH)),
                new XElement(Ns + "TablixCells", cells));
        }

        private static XElement BuildMergedRow(string prefix, string valueExpr, int totalCols, double rowH, int pt,
            string bgHex, string fgHex, bool bold, bool italic, string align)
        {
            var cells = new List<XElement>();
            for (int i = 0; i < totalCols; i++)
            {
                cells.Add(i == 0
                    ? CreateGridCell($"{prefix}_0", valueExpr, pt, bgHex, fgHex, bold, italic, align, null)
                    : new XElement(Ns + "TablixCell"));
            }

            return new XElement(Ns + "TablixRow",
                new XElement(Ns + "Height", Cm(rowH)),
                new XElement(Ns + "TablixCells", cells));
        }

        private static XElement BuildSummaryRow(string prefix, string label, List<ReportColumn> cols, Dictionary<string, string> colClsMap,
            bool showStt, double rowH, int pt, string bgHex, bool bold, bool italic)
        {
            var cells = new List<XElement>();
            int idx = 0;
            bool labelPlaced = false;

            if (showStt)
            {
                cells.Add(CreateGridCell($"{prefix}_{idx++}_STT", label, pt, bgHex, "#111827", bold, italic, "Center", null));
                labelPlaced = true;
            }

            foreach (var col in cols)
            {
                string fieldCls = colClsMap.TryGetValue(col.ColumnName, out string mapped)
                    ? mapped
                    : SanitizeIdentifier(col.ColumnName, "Col" + idx);

                if (col.SummaryMode != ReportSummaryMode.None)
                {
                    string expr = BuildSummaryExpr(fieldCls, col.SummaryMode);
                    string fmt = col.SummaryMode == ReportSummaryMode.Count ? "#,##0" : ConvertNetFormatToRdl(col.Format);
                    cells.Add(CreateGridCell($"{prefix}_{idx++}", expr, pt, bgHex, "#111827", bold, italic, MapRdlAlign(col.Alignment), fmt));
                }
                else if (!labelPlaced)
                {
                    cells.Add(CreateGridCell($"{prefix}_{idx++}", label, pt, bgHex, "#111827", bold, italic, "Left", null));
                    labelPlaced = true;
                }
                else
                {
                    cells.Add(CreateGridCell($"{prefix}_{idx++}", "", pt, bgHex, "#111827", bold, italic, "Left", null));
                }
            }

            return new XElement(Ns + "TablixRow",
                new XElement(Ns + "Height", Cm(rowH)),
                new XElement(Ns + "TablixCells", cells));
        }

        private static string BuildSummaryExpr(string fieldCls, ReportSummaryMode mode)
        {
            switch (mode)
            {
                case ReportSummaryMode.Sum: return $"=Sum(Fields!{fieldCls}.Value)";
                case ReportSummaryMode.Average: return $"=Avg(Fields!{fieldCls}.Value)";
                case ReportSummaryMode.Count: return $"=Count(Fields!{fieldCls}.Value)";
                case ReportSummaryMode.Max: return $"=Max(Fields!{fieldCls}.Value)";
                case ReportSummaryMode.Min: return $"=Min(Fields!{fieldCls}.Value)";
                default: return "";
            }
        }

        private static XElement CreateGridCell(string name, string valueExpr, int pt, string bgHex, string fgHex,
            bool bold, bool italic, string align, string format)
        {
            var tbStyle = new List<XElement>
            {
                new XElement(Ns + "Border",
                    new XElement(Ns + "Color", "#444444"),
                    new XElement(Ns + "Style", "Solid")),
                new XElement(Ns + "BackgroundColor", bgHex),
                new XElement(Ns + "PaddingLeft", "4pt"),
                new XElement(Ns + "PaddingRight", "4pt"),
                new XElement(Ns + "PaddingTop", "3pt"),
                new XElement(Ns + "PaddingBottom", "3pt"),
                new XElement(Ns + "VerticalAlign", "Middle")
            };

            var textRunStyle = new List<XElement>
            {
                new XElement(Ns + "FontFamily", "Times New Roman"),
                new XElement(Ns + "FontSize", pt + "pt"),
                new XElement(Ns + "Color", fgHex)
            };
            if (bold) textRunStyle.Add(new XElement(Ns + "FontWeight", "Bold"));
            if (italic) textRunStyle.Add(new XElement(Ns + "FontStyle", "Italic"));
            if (!string.IsNullOrEmpty(format)) textRunStyle.Add(new XElement(Ns + "Format", format));

            return new XElement(Ns + "TablixCell",
                new XElement(Ns + "CellContents",
                    new XElement(Ns + "Textbox",
                        new XAttribute("Name", name),
                        new XElement(Ns + "CanGrow", "true"),
                        new XElement(Ns + "KeepTogether", "true"),
                        new XElement(Ns + "Paragraphs",
                            new XElement(Ns + "Paragraph",
                                new XElement(Ns + "TextRuns",
                                    new XElement(Ns + "TextRun",
                                        new XElement(Ns + "Value", valueExpr ?? ""),
                                        new XElement(Ns + "Style", textRunStyle))),
                                new XElement(Ns + "Style",
                                    new XElement(Ns + "TextAlign", align)))),
                        new XElement(Ns + "Style", tbStyle))));
        }

        private static XElement CreateTextbox(string name, double leftCm, double topCm, double widthCm, double heightCm,
            string valueExpr, int pt, bool bold, string align, string fgHex, bool italic = false)
        {
            var runStyle = new List<XElement>
            {
                new XElement(Ns + "FontFamily", "Times New Roman"),
                new XElement(Ns + "FontSize", pt + "pt"),
                new XElement(Ns + "Color", fgHex)
            };
            if (bold) runStyle.Add(new XElement(Ns + "FontWeight", "Bold"));
            if (italic) runStyle.Add(new XElement(Ns + "FontStyle", "Italic"));

            return new XElement(Ns + "Textbox",
                new XAttribute("Name", name),
                new XElement(Ns + "CanGrow", "true"),
                new XElement(Ns + "KeepTogether", "true"),
                new XElement(Ns + "Paragraphs",
                    new XElement(Ns + "Paragraph",
                        new XElement(Ns + "TextRuns",
                            new XElement(Ns + "TextRun",
                                new XElement(Ns + "Value", valueExpr ?? ""),
                                new XElement(Ns + "Style", runStyle))),
                        new XElement(Ns + "Style",
                            new XElement(Ns + "TextAlign", align)))),
                new XElement(Ns + "Top", Cm(topCm)),
                new XElement(Ns + "Left", Cm(leftCm)),
                new XElement(Ns + "Height", Cm(heightCm)),
                new XElement(Ns + "Width", Cm(widthCm)),
                new XElement(Ns + "Style",
                    new XElement(Ns + "Border",
                        new XElement(Ns + "Style", "None"))));
        }

        private static XElement CreateReportParameter(string name, string defaultValue)
        {
            return new XElement(Ns + "ReportParameter",
                new XAttribute("Name", name),
                new XElement(Ns + "DataType", "String"),
                new XElement(Ns + "Nullable", "true"),
                new XElement(Ns + "DefaultValue",
                    new XElement(Ns + "Values",
                        new XElement(Ns + "Value", defaultValue ?? ""))),
                new XElement(Ns + "AllowBlank", "true"),
                new XElement(Ns + "Prompt", name));
        }

        private sealed class RdlFieldInfo
        {
            public string ColumnName;
            public string ClsName;
            public string TypeFullName;
        }

        private static List<RdlFieldInfo> BuildFieldInfos(DataTable table, List<ReportColumn> cols, string groupByColumn)
        {
            var result = new List<RdlFieldInfo>();
            var addedCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usedCls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Action<string, Type> addField = (colName, type) =>
            {
                if (string.IsNullOrWhiteSpace(colName) || !addedCols.Add(colName)) return;
                string baseCls = SanitizeIdentifier(colName, "Field" + (result.Count + 1));
                string uniqueCls = baseCls;
                int suffix = 1;
                while (!usedCls.Add(uniqueCls))
                {
                    uniqueCls = baseCls + "_" + (++suffix);
                }

                result.Add(new RdlFieldInfo
                {
                    ColumnName = colName,
                    ClsName = uniqueCls,
                    TypeFullName = (type ?? typeof(string)).FullName
                });
            };

            if (table != null)
            {
                foreach (DataColumn dc in table.Columns)
                    addField(dc.ColumnName, dc.DataType);
            }
            foreach (var c in cols)
                addField(c.ColumnName, table != null && table.Columns.Contains(c.ColumnName) ? table.Columns[c.ColumnName].DataType : typeof(string));
            if (!string.IsNullOrEmpty(groupByColumn))
                addField(groupByColumn, table != null && table.Columns.Contains(groupByColumn) ? table.Columns[groupByColumn].DataType : typeof(string));

            return result;
        }

        private static List<double> ComputeColumnWidthsCm(DataTable table, List<ReportColumn> cols, bool showStt, double usableWidthCm)
        {
            var weights = new List<double>();
            if (showStt) weights.Add(1.1);

            foreach (var col in cols)
            {
                Type colType = table != null && table.Columns.Contains(col.ColumnName)
                    ? table.Columns[col.ColumnName].DataType
                    : typeof(string);
                int headerLen = (col.HeaderText ?? col.ColumnName ?? "").Length;

                double w = 2.6;
                if (colType == typeof(DateTime)) w = 2.4;
                else if (colType == typeof(decimal) || colType == typeof(int) || colType == typeof(long)
                    || colType == typeof(double) || colType == typeof(float)) w = 2.5;
                else if (headerLen >= 10) w = 3.6;
                else w = 3.0;

                weights.Add(w);
            }

            double totalWeight = weights.Sum();
            if (totalWeight <= 0) return weights;

            var scaled = new List<double>(weights.Count);
            double running = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                double val = (i == weights.Count - 1)
                    ? Math.Max(0.8, Math.Round(usableWidthCm - running, 3))
                    : Math.Round(weights[i] * usableWidthCm / totalWeight, 3);
                scaled.Add(val);
                running += val;
            }
            return scaled;
        }

        private static string MapRdlAlign(HorizontalAlignment align)
        {
            switch (align)
            {
                case HorizontalAlignment.Center: return "Center";
                case HorizontalAlignment.Right: return "Right";
                default: return "Left";
            }
        }

        private static string ConvertNetFormatToRdl(string netFormat)
        {
            if (string.IsNullOrWhiteSpace(netFormat)) return null;
            string fmt = netFormat.Trim();
            if (string.Equals(fmt, "N0", StringComparison.OrdinalIgnoreCase)) return "#,##0";
            if (string.Equals(fmt, "N2", StringComparison.OrdinalIgnoreCase)) return "#,##0.00";
            return fmt;
        }

        private static string SanitizeIdentifier(string raw, string fallback)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            var sb = new StringBuilder();
            foreach (char c in raw.Trim())
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }
            if (sb.Length == 0 || (sb[0] >= '0' && sb[0] <= '9'))
                sb.Insert(0, '_');
            return sb.ToString();
        }

        private static string EscapeRdlString(string value)
        {
            return (value ?? "").Replace("\"", "\"\"");
        }

        private static string Cm(double cm)
        {
            return cm.ToString("0.###", CultureInfo.InvariantCulture) + "cm";
        }
    }
}
