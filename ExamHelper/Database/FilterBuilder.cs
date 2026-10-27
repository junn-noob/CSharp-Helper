
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace ExamHelper.Database
{
    /// <summary>
    /// Tạo mệnh đề WHERE với nhiều điều kiện AND.
    /// Hỗ trợ bỏ qua giá trị rỗng và sử dụng SqlParameter
    /// để truyền tham số an toàn.
    /// </summary>
    public class FilterBuilder
    {
        private readonly List<string> _conditions =
            new List<string>();

        private readonly List<SqlParameter> _parameters =
            new List<SqlParameter>();

        private int _paramIndex = 0;

        #region Factory

        public static FilterBuilder New()
        {
            return new FilterBuilder();
        }

        #endregion

        #region Định dạng tên cột

        /// <summary>
        /// Định dạng tên cột thành identifier SQL.
        /// Ví dụ:
        /// nv.MaPB  -> [nv].[MaPB]
        /// HoTen    -> [HoTen]
        /// [nv].[MaPB] -> [nv].[MaPB]
        /// </summary>
        private static string FormatColumn(string column)
        {
            if (string.IsNullOrWhiteSpace(column))
            {
                throw new ArgumentException(
                    "Tên cột không được để trống.",
                    nameof(column));
            }

            string[] parts = column.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim().Trim('[', ']');

                if (string.IsNullOrWhiteSpace(part))
                {
                    throw new ArgumentException(
                        "Tên cột không hợp lệ.",
                        nameof(column));
                }

                // Không cho phép chèn biểu thức SQL vào tên cột.
                if (part.Contains("[") ||
                    part.Contains("]") ||
                    part.Contains(";") ||
                    part.Contains("--") ||
                    part.Contains("/*") ||
                    part.Contains("*/"))
                {
                    throw new ArgumentException(
                        "Tên cột không hợp lệ.",
                        nameof(column));
                }

                parts[i] = $"[{part}]";
            }

            return string.Join(".", parts);
        }

        #endregion

        #region Điều kiện so sánh

        /// <summary>
        /// Thêm điều kiện bằng (=).
        /// Bỏ qua nếu giá trị null hoặc rỗng. Nếu skipZero = true thì bỏ qua cả giá trị số bằng 0.
        /// </summary>
        public FilterBuilder Equal(
            string column,
            object value,
            string paramName = null,
            bool skipZero = false)
        {
            if (ShouldSkip(value, skipZero))
                return this;

            string pName = paramName ?? NextParam();

            _conditions.Add(
                $"{FormatColumn(column)} = {pName}");

            _parameters.Add(
                new SqlParameter(pName, value));

            return this;
        }

        /// <summary>
        /// Thêm điều kiện bằng (=) nhưng tự động bỏ qua nếu giá trị bằng 0 (hữu ích cho dropdown ID).
        /// </summary>
        public FilterBuilder SkipIfZero(
            string column,
            object value,
            string paramName = null)
        {
            return Equal(column, value, paramName, skipZero: true);
        }

        /// <summary>
        /// Thêm điều kiện khác (&lt;&gt;).
        /// </summary>
        public FilterBuilder NotEqual(
            string column,
            object value,
            string paramName = null,
            bool skipZero = false)
        {
            if (ShouldSkip(value, skipZero))
                return this;

            string pName = paramName ?? NextParam();

            _conditions.Add(
                $"{FormatColumn(column)} <> {pName}");

            _parameters.Add(
                new SqlParameter(pName, value));

            return this;
        }

        /// <summary>
        /// Tìm chuỗi chứa giá trị: LIKE %value%.
        /// </summary>
        public FilterBuilder Contains(
            string column,
            string value,
            string paramName = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return this;

            string pName = paramName ?? NextParam();

            _conditions.Add(
                $"{FormatColumn(column)} LIKE {pName}");

            _parameters.Add(
                new SqlParameter(pName, $"%{value.Trim()}%"));

            return this;
        }

        /// <summary>
        /// Tìm chuỗi bắt đầu bằng giá trị: LIKE value%.
        /// </summary>
        public FilterBuilder StartsWith(
            string column,
            string value,
            string paramName = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return this;

            string pName = paramName ?? NextParam();

            _conditions.Add(
                $"{FormatColumn(column)} LIKE {pName}");

            _parameters.Add(
                new SqlParameter(pName, $"{value.Trim()}%"));

            return this;
        }

        /// <summary>
        /// Tìm chuỗi kết thúc bằng giá trị: LIKE %value.
        /// </summary>
        public FilterBuilder EndsWith(
            string column,
            string value,
            string paramName = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return this;

            string pName = paramName ?? NextParam();

            _conditions.Add(
                $"{FormatColumn(column)} LIKE {pName}");

            _parameters.Add(
                new SqlParameter(pName, $"%{value.Trim()}"));

            return this;
        }

        /// <summary>
        /// Điều kiện lớn hơn (>).
        /// </summary>
        public FilterBuilder GreaterThan(
            string column,
            object value,
            string paramName = null)
        {
            if (ShouldSkip(value))
                return this;

            string pName = paramName ?? NextParam();

            _conditions.Add(
                $"{FormatColumn(column)} > {pName}");

            _parameters.Add(
                new SqlParameter(pName, value));

            return this;
        }

        /// <summary>
        /// Điều kiện lớn hơn hoặc bằng (>=).
        /// </summary>
        public FilterBuilder GreaterThanOrEqual(
            string column,
            object value,
            string paramName = null)
        {
            if (ShouldSkip(value))
                return this;

            string pName = paramName ?? NextParam();

            _conditions.Add(
                $"{FormatColumn(column)} >= {pName}");

            _parameters.Add(
                new SqlParameter(pName, value));

            return this;
        }

        /// <summary>
        /// Điều kiện nhỏ hơn (&lt;).
        /// </summary>
        public FilterBuilder LessThan(
            string column,
            object value,
            string paramName = null)
        {
            if (ShouldSkip(value))
                return this;

            string pName = paramName ?? NextParam();

            _conditions.Add(
                $"{FormatColumn(column)} < {pName}");

            _parameters.Add(
                new SqlParameter(pName, value));

            return this;
        }

        /// <summary>
        /// Điều kiện nhỏ hơn hoặc bằng (&lt;=).
        /// </summary>
        public FilterBuilder LessThanOrEqual(
            string column,
            object value,
            string paramName = null)
        {
            if (ShouldSkip(value))
                return this;

            string pName = paramName ?? NextParam();

            _conditions.Add(
                $"{FormatColumn(column)} <= {pName}");

            _parameters.Add(
                new SqlParameter(pName, value));

            return this;
        }

        #endregion

        #region Between

        /// <summary>
        /// Có cả min và max: BETWEEN.
        /// Chỉ có min: &gt;=.
        /// Chỉ có max: &lt;=.
        /// </summary>
        public FilterBuilder Between(
            string column,
            object min,
            object max,
            string paramMin = null,
            string paramMax = null)
        {
            bool hasMin = !ShouldSkip(min);
            bool hasMax = !ShouldSkip(max);

            if (!hasMin && !hasMax)
                return this;

            string formattedColumn = FormatColumn(column);

            if (hasMin && hasMax)
            {
                string pMin = paramMin ?? NextParam();
                string pMax = paramMax ?? NextParam();

                _conditions.Add(
                    $"{formattedColumn} BETWEEN {pMin} AND {pMax}");

                _parameters.Add(new SqlParameter(pMin, min));
                _parameters.Add(new SqlParameter(pMax, max));
            }
            else if (hasMin)
            {
                GreaterThanOrEqual(column, min, paramMin);
            }
            else
            {
                LessThanOrEqual(column, max, paramMax);
            }

            return this;
        }

        /// <summary>
        /// Lọc khoảng ngày từ fromDate đến toDate (tự động tính trọn vẹn ngày toDate đến 23:59:59).
        /// Bỏ qua nếu cả hai đều null. Nếu chỉ có fromDate thì &gt;=, chỉ có toDate thì &lt;= 23:59:59.
        /// </summary>
        /// <param name="column">Tên cột ngày.</param>
        /// <param name="fromDate">Từ ngày.</param>
        /// <param name="toDate">Đến ngày.</param>
        public FilterBuilder BetweenDates(string column, DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue && !toDate.HasValue)
                return this;

            DateTime? adjustedTo = toDate.HasValue
                ? (toDate.Value.TimeOfDay == TimeSpan.Zero ? toDate.Value.Date.AddDays(1).AddTicks(-1) : toDate.Value)
                : (DateTime?)null;

            return Between(column, fromDate, adjustedTo);
        }

        #endregion

        #region IN

        /// <summary>
        /// Điều kiện IN với danh sách giá trị.
        /// </summary>
        public FilterBuilder In(
            string column,
            params object[] values)
        {
            if (values == null || values.Length == 0)
                return this;

            string formattedColumn = FormatColumn(column);

            var paramNames = new List<string>();

            foreach (object val in values)
            {
                string pName = NextParam();

                paramNames.Add(pName);

                _parameters.Add(
                    new SqlParameter(
                        pName,
                        val ?? DBNull.Value));
            }

            _conditions.Add(
                $"{formattedColumn} IN ({string.Join(", ", paramNames)})");

            return this;
        }

        /// <summary>
        /// Điều kiện NOT IN với danh sách giá trị.
        /// </summary>
        public FilterBuilder NotIn(
            string column,
            params object[] values)
        {
            if (values == null || values.Length == 0)
                return this;

            string formattedColumn = FormatColumn(column);

            var paramNames = new List<string>();

            foreach (object val in values)
            {
                string pName = NextParam();

                paramNames.Add(pName);

                _parameters.Add(
                    new SqlParameter(
                        pName,
                        val ?? DBNull.Value));
            }

            _conditions.Add(
                $"{formattedColumn} NOT IN ({string.Join(", ", paramNames)})");

            return this;
        }

        #endregion

        #region NULL

        /// <summary>
        /// Điều kiện IS NULL.
        /// </summary>
        public FilterBuilder IsNull(string column)
        {
            _conditions.Add(
                $"{FormatColumn(column)} IS NULL");

            return this;
        }

        /// <summary>
        /// Điều kiện IS NOT NULL.
        /// </summary>
        public FilterBuilder IsNotNull(string column)
        {
            _conditions.Add(
                $"{FormatColumn(column)} IS NOT NULL");

            return this;
        }

        #endregion

        #region ComboBox

        /// <summary>
        /// Chỉ thêm điều kiện khi ComboBox không chọn
        /// dòng Tất cả (SelectedIndex > 0).
        /// Lấy giá trị từ SelectedValue.
        /// </summary>
        public FilterBuilder SkipIfAllSelected(
            ComboBox combo,
            string column,
            string paramName = null)
        {
            if (combo == null || combo.SelectedIndex <= 0)
                return this;

            return Equal(
                column,
                combo.SelectedValue,
                paramName);
        }

        #endregion

        #region Raw

        /// <summary>
        /// Thêm điều kiện SQL tùy chỉnh.
        /// Người gọi phải tự bảo đảm condition an toàn.
        /// </summary>
        public FilterBuilder Raw(
            string condition,
            params SqlParameter[] rawParams)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return this;

            _conditions.Add(condition);

            if (rawParams != null)
                _parameters.AddRange(rawParams);

            return this;
        }

        #endregion

        #region Nhóm điều kiện OR

        /// <summary>
        /// Thêm một nhóm điều kiện OR: (cond1 OR cond2 OR ...)
        /// Nhóm này được kết hợp với các điều kiện khác bằng AND.
        /// </summary>
        /// <param name="configure">Action cấu hình các điều kiện bên trong nhóm OR.</param>
        /// <example>
        /// filter.Or(or => or.Contains("MaNV", kw).Contains("HoTen", kw));
        /// // Sinh ra: WHERE ([MaNV] LIKE @f0 OR [HoTen] LIKE @f1)
        /// </example>
        public FilterBuilder Or(Action<FilterBuilder> configure)
        {
            if (configure == null) return this;

            var sub = new FilterBuilder { _paramIndex = this._paramIndex };
            configure(sub);

            if (sub._conditions.Count > 0)
            {
                _conditions.Add("(" + string.Join(" OR ", sub._conditions) + ")");
                _parameters.AddRange(sub._parameters);
                this._paramIndex = sub._paramIndex;
            }

            return this;
        }

        /// <summary>
        /// Tìm kiếm một từ khóa trên nhiều cột bằng phép toán OR.
        /// Rất hữu ích cho ô tìm kiếm chung (ví dụ: tìm từ khóa trong Mã NV hoặc Họ tên).
        /// </summary>
        /// <param name="columns">Danh sách tên cột cần tìm.</param>
        /// <param name="value">Từ khóa tìm kiếm.</param>
        /// <example>
        /// filter.ContainsAny(new[] { "MaNV", "HoTen" }, keyword);
        /// // Sinh ra: WHERE ([MaNV] LIKE @f0 OR [HoTen] LIKE @f0)
        /// </example>
        public FilterBuilder ContainsAny(string[] columns, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || columns == null || columns.Length == 0)
                return this;

            string pName = NextParam();
            var orParts = new List<string>();
            foreach (var col in columns)
            {
                if (!string.IsNullOrWhiteSpace(col))
                    orParts.Add($"{FormatColumn(col)} LIKE {pName}");
            }

            if (orParts.Count > 0)
            {
                _conditions.Add("(" + string.Join(" OR ", orParts) + ")");
                _parameters.Add(new SqlParameter(pName, $"%{value.Trim()}%"));
            }

            return this;
        }

        #endregion

        #region Kết quả

        public bool HasConditions => _conditions.Count > 0;

        /// <summary>
        /// Trả về chuỗi WHERE hoặc chuỗi rỗng.
        /// </summary>
        public string ToWhereClause()
        {
            if (_conditions.Count == 0)
                return string.Empty;

            return " WHERE " +
                   string.Join(" AND ", _conditions);
        }

        /// <summary>
        /// Trả về các SqlParameter.
        /// </summary>
        public SqlParameter[] ToParameters()
        {
            return _parameters.ToArray();
        }

        /// <summary>
        /// Trả về WHERE và danh sách tham số.
        /// </summary>
        public (string WhereClause, SqlParameter[] Parameters) Build()
        {
            return (ToWhereClause(), ToParameters());
        }

        #endregion

        #region Helper nội bộ

        private string NextParam()
        {
            return $"@f{_paramIndex++}";
        }

        /// <summary>
        /// Xác định giá trị có cần bỏ qua hay không.
        /// Mặc định: bỏ qua null, DBNull, chuỗi rỗng hoặc chỉ có khoảng trắng.
        /// Nếu skipZero = true: bỏ qua thêm các giá trị số bằng 0.
        /// </summary>
        private static bool ShouldSkip(object value, bool skipZero = false)
        {
            if (value == null || value == DBNull.Value)
                return true;

            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s))
                    return true;
                if (skipZero && (s.Trim() == "0" || s.Trim() == "0.0"))
                    return true;
            }

            if (skipZero)
            {
                if (value is int i) return i == 0;
                if (value is long l) return l == 0;
                if (value is short sh) return sh == 0;
                if (value is byte b) return b == 0;
                if (value is decimal d) return d == 0m;
                if (value is double db) return db == 0.0;
                if (value is float f) return f == 0.0f;
            }

            return false;
        }

        #endregion
    }
}
