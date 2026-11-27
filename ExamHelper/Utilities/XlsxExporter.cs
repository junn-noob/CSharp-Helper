using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ExamHelper.Utilities
{
    /// <summary>
    /// Tạo file Excel chuẩn OpenXML (.xlsx) bằng ZipArchive tích hợp sẵn (không cần NuGet ngoài).
    /// </summary>
    internal static class XlsxExporter
    {
        public static byte[] BuildBytes(
            DataTable table,
            string reportTitle,
            string filterCondition,
            string groupByColumn,
            string groupPrefix,
            IEnumerable<ReportColumn> columns,
            string companyName,
            string signerTitle,
            bool showGroupTotals,
            string pageFooter,
            string dateFormat,
            bool showStt,
            bool landscape = false,
            int fontSizePt = 13)
        {
            if (table == null) table = new DataTable();

            var colList = columns?.ToList();
            if (colList == null || colList.Count == 0)
            {
                colList = ReportHelper.BuildAutoColumns(table, groupByColumn, null);
            }

            int totalCols = Math.Max(1, colList.Count + (showStt ? 1 : 0));
            string lastColLetter = ToColLetters(totalCols);
            string safeDateFmt = string.IsNullOrWhiteSpace(dateFormat) ? "dd/MM/yyyy" : dateFormat;

            var rowsXml = new StringBuilder();
            var merges = new List<string>();
            int rowIdx = 1;

            // Hàng 1: Tên công ty (trái) & Ngày lập (phải)
            AppendRowStart(rowsXml, rowIdx, 22);
            AppendTextCell(rowsXml, 1, rowIdx, (companyName ?? "").ToUpperInvariant(), styleId: 1);
            if (totalCols >= 2)
            {
                AppendTextCell(rowsXml, totalCols, rowIdx, "Ngày lập: " + DateTime.Now.ToString(safeDateFmt), styleId: 2);
                if (totalCols > 2)
                    merges.Add($"A{rowIdx}:{ToColLetters(totalCols - 1)}{rowIdx}");
            }
            AppendRowEnd(rowsXml);
            rowIdx++;

            // Hàng 2: Tiêu đề báo cáo (merge toàn bảng)
            AppendRowStart(rowsXml, rowIdx, 28);
            AppendTextCell(rowsXml, 1, rowIdx, (reportTitle ?? "").ToUpperInvariant(), styleId: 3);
            for (int c = 2; c <= totalCols; c++) AppendEmptyStyledCell(rowsXml, c, rowIdx, 3);
            AppendRowEnd(rowsXml);
            if (totalCols > 1) merges.Add($"A{rowIdx}:{lastColLetter}{rowIdx}");
            rowIdx++;

            // Hàng 3: Điều kiện lọc (nếu có)
            if (!string.IsNullOrWhiteSpace(filterCondition))
            {
                AppendRowStart(rowsXml, rowIdx, 20);
                AppendTextCell(rowsXml, 1, rowIdx, "Điều kiện lọc: " + filterCondition, styleId: 2);
                for (int c = 2; c <= totalCols; c++) AppendEmptyStyledCell(rowsXml, c, rowIdx, 2);
                AppendRowEnd(rowsXml);
                if (totalCols > 1) merges.Add($"A{rowIdx}:{lastColLetter}{rowIdx}");
                rowIdx++;
            }

            rowIdx++; // Dòng trống ngăn cách

            // Hàng tiêu đề bảng
            AppendRowStart(rowsXml, rowIdx, 24);
            int headerColIdx = 1;
            if (showStt)
            {
                AppendTextCell(rowsXml, headerColIdx++, rowIdx, "STT", styleId: 4);
            }
            foreach (var col in colList)
            {
                AppendTextCell(rowsXml, headerColIdx++, rowIdx, col.HeaderText ?? col.ColumnName, styleId: 4);
            }
            AppendRowEnd(rowsXml);
            rowIdx++;

            var grandBuckets = ReportHelper.CreateSummaryBuckets(colList);

            bool hasGrouping = !string.IsNullOrEmpty(groupByColumn) && table.Columns.Contains(groupByColumn);
            int stt = 1;

            if (hasGrouping)
            {
                string prefix = groupPrefix ?? (groupByColumn + ": ");
                var grouped = table.Rows.Cast<DataRow>()
                    .GroupBy(r => r[groupByColumn] == DBNull.Value ? string.Empty : (r[groupByColumn]?.ToString() ?? string.Empty));

                foreach (var group in grouped)
                {
                    var rows = group.ToList();
                    if (rows.Count == 0) continue;

                    string groupDisplay = string.IsNullOrEmpty(group.Key) ? "Khác" : group.Key;
                    AppendRowStart(rowsXml, rowIdx, 22);
                    AppendTextCell(rowsXml, 1, rowIdx, $"{prefix}{groupDisplay} ({rows.Count} bản ghi)", styleId: 10);
                    for (int c = 2; c <= totalCols; c++) AppendEmptyStyledCell(rowsXml, c, rowIdx, 10);
                    AppendRowEnd(rowsXml);
                    if (totalCols > 1) merges.Add($"A{rowIdx}:{lastColLetter}{rowIdx}");
                    rowIdx++;

                    var groupBuckets = ReportHelper.CreateSummaryBuckets(colList);

                    foreach (DataRow r in rows)
                    {
                        AppendDataRow(rowsXml, r, colList, stt++, rowIdx++, showStt);
                        ReportHelper.AccumulateSummaries(r, colList, groupBuckets, grandBuckets);
                    }

                    if (showGroupTotals && groupBuckets.Count > 0)
                    {
                        AppendTotalRow(rowsXml, ReportHelper.GetGroupSummaryLabel(groupDisplay, colList), colList, groupBuckets, rowIdx++, showStt);
                    }
                }
            }
            else
            {
                foreach (DataRow r in table.Rows)
                {
                    AppendDataRow(rowsXml, r, colList, stt++, rowIdx++, showStt);
                    ReportHelper.AccumulateSummaries(r, colList, null, grandBuckets);
                }
            }

            if (grandBuckets.Count > 0)
            {
                AppendTotalRow(rowsXml, ReportHelper.GetGrandSummaryLabel(colList), colList, grandBuckets, rowIdx++, showStt);
            }

            // Chữ ký
            if (!string.IsNullOrWhiteSpace(signerTitle))
            {
                rowIdx++;
                AppendRowStart(rowsXml, rowIdx, 20);
                AppendTextCell(rowsXml, totalCols, rowIdx, signerTitle, styleId: 1);
                AppendRowEnd(rowsXml);
                rowIdx++;

                AppendRowStart(rowsXml, rowIdx, 18);
                AppendTextCell(rowsXml, totalCols, rowIdx, "(Ký, ghi rõ họ tên)", styleId: 2);
                AppendRowEnd(rowsXml);
                rowIdx++;
            }

            // Chân trang
            if (!string.IsNullOrWhiteSpace(pageFooter))
            {
                rowIdx++;
                AppendRowStart(rowsXml, rowIdx, 18);
                AppendTextCell(rowsXml, 1, rowIdx, pageFooter, styleId: 2);
                AppendRowEnd(rowsXml);
                if (totalCols > 1) merges.Add($"A{rowIdx}:{lastColLetter}{rowIdx}");
            }

            string sheetXml = BuildWorksheetXml(colList, showStt, totalCols, rowsXml.ToString(), merges, landscape);

            using (var ms = new MemoryStream())
            {
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
                {
                    WriteEntry(zip, "[Content_Types].xml", ContentTypesXml);
                    WriteEntry(zip, "_rels/.rels", RootRelsXml);
                    WriteEntry(zip, "xl/workbook.xml", WorkbookXml);
                    WriteEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml);
                    WriteEntry(zip, "xl/styles.xml", BuildStylesXml(fontSizePt));
                    WriteEntry(zip, "xl/worksheets/sheet1.xml", sheetXml);
                }
                return ms.ToArray();
            }
        }

        private static void AppendDataRow(StringBuilder sb, DataRow r, List<ReportColumn> colList, int stt, int rowIdx, bool showStt)
        {
            AppendRowStart(sb, rowIdx, 20);
            int cIdx = 1;
            if (showStt)
            {
                AppendNumericCell(sb, cIdx++, rowIdx, stt, styleId: 6);
            }

            foreach (var col in colList)
            {
                object val = r.Table.Columns.Contains(col.ColumnName) ? r[col.ColumnName] : null;
                if (IsNumericValue(val, out decimal numVal) && string.IsNullOrEmpty(TryGetCustomTextFormat(col.Format)))
                {
                    int styleId = GetNumericStyleId(col.Format);
                    AppendNumericCell(sb, cIdx++, rowIdx, numVal, styleId);
                }
                else
                {
                    string formatted = ReportHelper.FormatCellValue(val, col.Format);
                    int alignStyle = col.Alignment == HorizontalAlignment.Center ? 6
                        : (col.Alignment == HorizontalAlignment.Right ? 7 : 5);
                    AppendTextCell(sb, cIdx++, rowIdx, formatted, alignStyle);
                }
            }

            AppendRowEnd(sb);
        }

        private static void AppendTotalRow(StringBuilder sb, string label, List<ReportColumn> colList,
            Dictionary<string, SummaryBucket> buckets, int rowIdx, bool showStt)
        {
            AppendRowStart(sb, rowIdx, 22);
            int totalCols = colList.Count + (showStt ? 1 : 0);

            int startDataColIndex;
            if (showStt)
            {
                bool labelInFirstDataCol = colList.Count >= 2 && !ReportHelper.HasSummary(colList[0]);
                if (labelInFirstDataCol)
                {
                    AppendEmptyStyledCell(sb, 1, rowIdx, 11);
                    AppendTextCell(sb, 2, rowIdx, label, styleId: 11);
                    startDataColIndex = 1;
                }
                else
                {
                    AppendTextCell(sb, 1, rowIdx, label, styleId: 11);
                    startDataColIndex = 0;
                }
            }
            else if (colList.Count > 0 && !ReportHelper.HasSummary(colList[0]))
            {
                AppendTextCell(sb, 1, rowIdx, label, styleId: 11);
                startDataColIndex = 1;
            }
            else if (colList.Count > 0)
            {
                buckets.TryGetValue(colList[0].ColumnName, out SummaryBucket b0);
                string disp0 = ReportHelper.FormatSummaryDisplay(colList[0], b0);
                AppendTextCell(sb, 1, rowIdx, $"{label}: {disp0}", styleId: 12);
                startDataColIndex = 1;
            }
            else
            {
                startDataColIndex = 0;
            }

            for (int i = startDataColIndex; i < colList.Count; i++)
            {
                int cellColIdx = i + 1 + (showStt ? 1 : 0);
                if (cellColIdx > totalCols) break;
                var col = colList[i];
                if (ReportHelper.HasSummary(col) && buckets.TryGetValue(col.ColumnName, out SummaryBucket bucket))
                {
                    if (col.SummaryMode == ReportSummaryMode.Sum && bucket.TryGetResult(ReportSummaryMode.Sum, out decimal sumVal))
                    {
                        AppendNumericCell(sb, cellColIdx, rowIdx, sumVal, styleId: 12);
                    }
                    else
                    {
                        AppendTextCell(sb, cellColIdx, rowIdx, ReportHelper.FormatSummaryDisplay(col, bucket), styleId: 12);
                    }
                }
                else
                {
                    AppendEmptyStyledCell(sb, cellColIdx, rowIdx, 11);
                }
            }

            AppendRowEnd(sb);
        }

        private static string TryGetCustomTextFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format)) return null;
            string upper = format.Trim().ToUpperInvariant();
            if (upper == "N0" || upper == "N2" || upper == "0.##" || upper == "F2")
                return null;
            return format;
        }

        private static int GetNumericStyleId(string format)
        {
            if (!string.IsNullOrWhiteSpace(format))
            {
                string f = format.Trim().ToUpperInvariant();
                if (f == "0.##" || f == "N2" || f == "F2")
                    return 9;
            }
            return 8;
        }

        private static bool IsNumericValue(object val, out decimal num)
        {
            num = 0m;
            if (val == null || val == DBNull.Value) return false;

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
            return false;
        }

        private static void AppendRowStart(StringBuilder sb, int rowIdx, double heightPt)
        {
            sb.Append($"<row r=\"{rowIdx}\" ht=\"{heightPt.ToString(CultureInfo.InvariantCulture)}\" customHeight=\"1\">");
        }

        private static void AppendRowEnd(StringBuilder sb)
        {
            sb.Append("</row>");
        }

        private static void AppendTextCell(StringBuilder sb, int colIdx, int rowIdx, string text, int styleId)
        {
            string cellRef = ToColLetters(colIdx) + rowIdx;
            string escaped = EscapeXml(text);
            sb.Append($"<c r=\"{cellRef}\" s=\"{styleId}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{escaped}</t></is></c>");
        }

        private static void AppendNumericCell(StringBuilder sb, int colIdx, int rowIdx, decimal number, int styleId)
        {
            string cellRef = ToColLetters(colIdx) + rowIdx;
            sb.Append($"<c r=\"{cellRef}\" s=\"{styleId}\"><v>{number.ToString(CultureInfo.InvariantCulture)}</v></c>");
        }

        private static void AppendEmptyStyledCell(StringBuilder sb, int colIdx, int rowIdx, int styleId)
        {
            string cellRef = ToColLetters(colIdx) + rowIdx;
            sb.Append($"<c r=\"{cellRef}\" s=\"{styleId}\"/>");
        }

        private static string BuildWorksheetXml(List<ReportColumn> colList, bool showStt, int totalCols,
            string sheetDataInner, List<string> merges, bool landscape)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");

            sb.Append("<cols>");
            int colNum = 1;
            if (showStt)
            {
                sb.Append($"<col min=\"{colNum}\" max=\"{colNum}\" width=\"7\" customWidth=\"1\"/>");
                colNum++;
            }
            foreach (var col in colList)
            {
                int w = Math.Max(14, Math.Min(36, (col.HeaderText ?? col.ColumnName ?? "").Length + 8));
                sb.Append($"<col min=\"{colNum}\" max=\"{colNum}\" width=\"{w}\" customWidth=\"1\"/>");
                colNum++;
            }
            sb.Append("</cols>");

            sb.Append("<sheetData>");
            sb.Append(sheetDataInner);
            sb.Append("</sheetData>");

            if (merges.Count > 0)
            {
                sb.Append($"<mergeCells count=\"{merges.Count}\">");
                foreach (string m in merges)
                {
                    sb.Append($"<mergeCell ref=\"{m}\"/>");
                }
                sb.Append("</mergeCells>");
            }

            sb.Append($"<pageSetup paperSize=\"9\" orientation=\"{(landscape ? "landscape" : "portrait")}\"/>");
            sb.Append("</worksheet>");
            return sb.ToString();
        }

        private static string ToColLetters(int colNumber)
        {
            var sb = new StringBuilder();
            while (colNumber > 0)
            {
                int mod = (colNumber - 1) % 26;
                sb.Insert(0, (char)('A' + mod));
                colNumber = (colNumber - 1) / 26;
            }
            return sb.ToString();
        }

        private static string EscapeXml(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var sb = new StringBuilder(value.Length);
            foreach (char ch in value)
            {
                // Lọc bỏ các ký tự điều khiển XML không hợp lệ
                if (ch < 0x20 && ch != '\t' && ch != '\n' && ch != '\r')
                    continue;
                switch (ch)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\'': sb.Append("&apos;"); break;
                    default: sb.Append(ch); break;
                }
            }
            return sb.ToString();
        }

        private static void WriteEntry(ZipArchive zip, string path, string content)
        {
            ZipArchiveEntry entry = zip.CreateEntry(path, CompressionLevel.Fastest);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private const string ContentTypesXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
            "</Types>";

        private const string RootRelsXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
            "</Relationships>";

        private const string WorkbookXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
            "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets><sheet name=\"BaoCao\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
            "</workbook>";

        private const string WorkbookRelsXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
            "</Relationships>";

        private static string BuildStylesXml(int fontSizePt)
        {
            int bodyPt = Math.Max(8, Math.Min(20, fontSizePt <= 0 ? 11 : (fontSizePt >= 12 ? fontSizePt - 2 : fontSizePt)));
            int italicPt = Math.Max(8, bodyPt - 1);
            int titlePt = bodyPt + 3;

            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<numFmts count=\"2\">" +
                "<numFmt numFmtId=\"164\" formatCode=\"#,##0\"/>" +
                "<numFmt numFmtId=\"165\" formatCode=\"0.##\"/>" +
                "</numFmts>" +
                "<fonts count=\"4\">" +
                $"<font><sz val=\"{bodyPt}\"/><name val=\"Times New Roman\"/></font>" +
                $"<font><b/><sz val=\"{bodyPt}\"/><name val=\"Times New Roman\"/></font>" +
                $"<font><i/><sz val=\"{italicPt}\"/><color rgb=\"FF444444\"/><name val=\"Times New Roman\"/></font>" +
                $"<font><b/><sz val=\"{titlePt}\"/><color rgb=\"FF1F497D\"/><name val=\"Times New Roman\"/></font>" +
                "</fonts>" +
                "<fills count=\"5\">" +
                "<fill><patternFill patternType=\"none\"/></fill>" +
                "<fill><patternFill patternType=\"gray125\"/></fill>" +
                "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFDBE5F1\"/></patternFill></fill>" +
                "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFF2F4F7\"/></patternFill></fill>" +
                "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFDE9D9\"/></patternFill></fill>" +
                "</fills>" +
                "<borders count=\"2\">" +
                "<border><left/><right/><top/><bottom/><diagonal/></border>" +
                "<border><left style=\"thin\"><color rgb=\"FF444444\"/></left><right style=\"thin\"><color rgb=\"FF444444\"/></right><top style=\"thin\"><color rgb=\"FF444444\"/></top><bottom style=\"thin\"><color rgb=\"FF444444\"/></bottom><diagonal/></border>" +
                "</borders>" +
                "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                "<cellXfs count=\"13\">" +
                "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
                "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
                "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\"/></xf>" +
                "<xf numFmtId=\"0\" fontId=\"3\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>" +
                "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>" +
                "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"left\" vertical=\"center\"/></xf>" +
                "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>" +
                "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\" vertical=\"center\"/></xf>" +
                "<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\" vertical=\"center\"/></xf>" +
                "<xf numFmtId=\"165\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\" vertical=\"center\"/></xf>" +
                "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"left\" vertical=\"center\"/></xf>" +
                "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"4\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>" +
                "<xf numFmtId=\"164\" fontId=\"1\" fillId=\"4\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\" vertical=\"center\"/></xf>" +
                "</cellXfs>" +
                "</styleSheet>";
        }
    }
}
