using System;
using System.Data;

namespace ExamHelper.Utilities
{
    /// <summary>
    /// Lớp tiện ích cung cấp các phương thức thống kê và chuyển đổi dữ liệu trên DataTable.
    /// </summary>
    public static class DataHelper
    {
        #region Đếm số dòng

        /// <summary>
        /// Đếm tổng số dòng trong DataTable.
        /// </summary>
        /// <param name="table">Bảng dữ liệu cần đếm.</param>
        /// <returns>Số dòng trong bảng. Trả về 0 nếu bảng null.</returns>
        public static int Count(DataTable table)
        {
            if (table == null)
                return 0;

            return table.Rows.Count;
        }

        /// <summary>
        /// Đếm số dòng trong DataTable thỏa mãn điều kiện lọc.
        /// </summary>
        /// <param name="table">Bảng dữ liệu cần đếm.</param>
        /// <param name="filter">Biểu thức lọc (ví dụ: "Diem >= 5").</param>
        /// <returns>Số dòng thỏa mãn điều kiện. Trả về 0 nếu bảng null.</returns>
        public static int Count(DataTable table, string filter)
        {
            if (table == null)
                return 0;

            return table.Select(filter).Length;
        }

        #endregion

        #region Tính tổng

        /// <summary>
        /// Tính tổng giá trị của một cột số trong DataTable.
        /// </summary>
        /// <param name="table">Bảng dữ liệu.</param>
        /// <param name="columnName">Tên cột cần tính tổng.</param>
        /// <returns>Tổng giá trị. Trả về 0 nếu bảng rỗng hoặc null.</returns>
        public static decimal Sum(DataTable table, string columnName)
        {
            return Sum(table, columnName, "");
        }

        /// <summary>
        /// Tính tổng giá trị của một cột số trong DataTable với điều kiện lọc.
        /// </summary>
        /// <param name="table">Bảng dữ liệu.</param>
        /// <param name="columnName">Tên cột cần tính tổng.</param>
        /// <param name="filter">Biểu thức lọc (ví dụ: "Lop = 'A1'").</param>
        /// <returns>Tổng giá trị. Trả về 0 nếu bảng rỗng hoặc null.</returns>
        public static decimal Sum(DataTable table, string columnName, string filter)
        {
            if (table == null || table.Rows.Count == 0 || !table.Columns.Contains(columnName))
                return 0;

            try
            {
                object result = table.Compute($"SUM([{columnName}])", filter ?? "");
                return ToDecimal(result, 0);
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Tính trung bình

        /// <summary>
        /// Tính giá trị trung bình của một cột số trong DataTable.
        /// </summary>
        /// <param name="table">Bảng dữ liệu.</param>
        /// <param name="columnName">Tên cột cần tính trung bình.</param>
        /// <returns>Giá trị trung bình. Trả về 0 nếu bảng rỗng hoặc null.</returns>
        public static decimal Average(DataTable table, string columnName)
        {
            return Average(table, columnName, "");
        }

        /// <summary>
        /// Tính giá trị trung bình của một cột số trong DataTable với điều kiện lọc.
        /// </summary>
        /// <param name="table">Bảng dữ liệu.</param>
        /// <param name="columnName">Tên cột cần tính trung bình.</param>
        /// <param name="filter">Biểu thức lọc.</param>
        /// <returns>Giá trị trung bình. Trả về 0 nếu bảng rỗng hoặc null.</returns>
        public static decimal Average(DataTable table, string columnName, string filter)
        {
            if (table == null || table.Rows.Count == 0 || !table.Columns.Contains(columnName))
                return 0;

            try
            {
                object result = table.Compute($"AVG([{columnName}])", filter ?? "");
                return ToDecimal(result, 0);
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Tìm giá trị lớn nhất

        /// <summary>
        /// Tìm giá trị lớn nhất của một cột trong DataTable.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu trả về (int, decimal, double, DateTime, ...).</typeparam>
        /// <param name="table">Bảng dữ liệu.</param>
        /// <param name="columnName">Tên cột cần tìm giá trị lớn nhất.</param>
        /// <returns>Giá trị lớn nhất. Trả về default(T) nếu bảng rỗng hoặc null.</returns>
        public static T Max<T>(DataTable table, string columnName)
        {
            return Max<T>(table, columnName, "");
        }

        /// <summary>
        /// Tìm giá trị lớn nhất của một cột trong DataTable với điều kiện lọc.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu trả về.</typeparam>
        /// <param name="table">Bảng dữ liệu.</param>
        /// <param name="columnName">Tên cột cần tìm giá trị lớn nhất.</param>
        /// <param name="filter">Biểu thức lọc.</param>
        /// <returns>Giá trị lớn nhất. Trả về default(T) nếu bảng rỗng hoặc null.</returns>
        public static T Max<T>(DataTable table, string columnName, string filter)
        {
            if (table == null || table.Rows.Count == 0 || !table.Columns.Contains(columnName))
                return default(T);

            try
            {
                object result = table.Compute($"MAX([{columnName}])", filter ?? "");
                return ChangeType<T>(result);
            }
            catch
            {
                return default(T);
            }
        }

        #endregion

        #region Tìm giá trị nhỏ nhất

        /// <summary>
        /// Tìm giá trị nhỏ nhất của một cột trong DataTable.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu trả về (int, decimal, double, DateTime, ...).</typeparam>
        /// <param name="table">Bảng dữ liệu.</param>
        /// <param name="columnName">Tên cột cần tìm giá trị nhỏ nhất.</param>
        /// <returns>Giá trị nhỏ nhất. Trả về default(T) nếu bảng rỗng hoặc null.</returns>
        public static T Min<T>(DataTable table, string columnName)
        {
            return Min<T>(table, columnName, "");
        }

        /// <summary>
        /// Tìm giá trị nhỏ nhất của một cột trong DataTable với điều kiện lọc.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu trả về.</typeparam>
        /// <param name="table">Bảng dữ liệu.</param>
        /// <param name="columnName">Tên cột cần tìm giá trị nhỏ nhất.</param>
        /// <param name="filter">Biểu thức lọc.</param>
        /// <returns>Giá trị nhỏ nhất. Trả về default(T) nếu bảng rỗng hoặc null.</returns>
        public static T Min<T>(DataTable table, string columnName, string filter)
        {
            if (table == null || table.Rows.Count == 0 || !table.Columns.Contains(columnName))
                return default(T);

            try
            {
                object result = table.Compute($"MIN([{columnName}])", filter ?? "");
                return ChangeType<T>(result);
            }
            catch
            {
                return default(T);
            }
        }

        #endregion

        #region Chuyển đổi kiểu dữ liệu an toàn

        /// <summary>
        /// Phân tích chuỗi số sang decimal an toàn, tự động nhận diện định dạng quốc tế (dấu chấm)
        /// và định dạng Việt Nam/châu Âu (dấu phẩy), hỗ trợ cả số có dấu phân cách hàng nghìn.
        /// </summary>
        /// <param name="value">Chuỗi số cần phân tích.</param>
        /// <param name="result">Giá trị decimal đầu ra.</param>
        /// <returns>True nếu phân tích thành công, ngược lại False.</returns>
        public static bool TryParseDecimal(string value, out decimal result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string clean = value.Trim().Replace(" ", "");

            int lastDot = clean.LastIndexOf('.');
            int lastComma = clean.LastIndexOf(',');

            if (lastDot >= 0 && lastComma >= 0)
            {
                if (lastComma > lastDot)
                {
                    // Định dạng 1.234,56 (VN/EU) -> bỏ dấu chấm, đổi phẩy thành chấm
                    clean = clean.Replace(".", "").Replace(',', '.');
                }
                else
                {
                    // Định dạng 1,234.56 (US) -> bỏ dấu phẩy
                    clean = clean.Replace(",", "");
                }
            }
            else if (lastComma >= 0)
            {
                int commaCount = 0;
                for (int i = 0; i < clean.Length; i++)
                {
                    if (clean[i] == ',') commaCount++;
                }

                if (commaCount == 1)
                {
                    // 1 dấu phẩy: "2,34" hoặc "2,5" -> phần thập phân
                    clean = clean.Replace(',', '.');
                }
                else
                {
                    // Nhiều dấu phẩy: "1,000,000" -> phân cách hàng nghìn
                    clean = clean.Replace(",", "");
                }
            }

            return decimal.TryParse(clean, System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.AllowExponent, System.Globalization.CultureInfo.InvariantCulture, out result);
        }

        /// <summary>
        /// Chuyển đổi an toàn kiểu dữ liệu sang kiểu T, hỗ trợ cả kiểu Nullable (int?, decimal?,...),
        /// kiểu Guid, xử lý DBNull và các lỗi ép kiểu không hợp lệ.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu đích.</typeparam>
        /// <param name="value">Giá trị nguồn.</param>
        /// <param name="defaultValue">Giá trị trả về nếu null hoặc lỗi ép kiểu.</param>
        /// <returns>Giá trị kiểu T.</returns>
        public static T ChangeType<T>(object value, T defaultValue = default(T))
        {
            if (value == null || value == DBNull.Value)
                return defaultValue;

            Type targetType = typeof(T);
            Type underlying = Nullable.GetUnderlyingType(targetType);
            Type conversionType = underlying ?? targetType;

            try
            {
                if (conversionType == typeof(Guid))
                {
                    if (value is Guid g) return (T)(object)g;
                    return (T)(object)Guid.Parse(value.ToString());
                }

                if (conversionType.IsEnum)
                {
                    return (T)Enum.Parse(conversionType, value.ToString(), true);
                }

                if (conversionType == typeof(decimal) && value is string sDec)
                {
                    if (TryParseDecimal(sDec, out decimal decRes))
                        return (T)(object)decRes;
                    return defaultValue;
                }

                if (conversionType == typeof(double) && value is string sDbl)
                {
                    if (TryParseDecimal(sDbl, out decimal dblRes))
                        return (T)(object)(double)dblRes;
                    return defaultValue;
                }

                if (conversionType == typeof(float) && value is string sFlt)
                {
                    if (TryParseDecimal(sFlt, out decimal fltRes))
                        return (T)(object)(float)fltRes;
                    return defaultValue;
                }

                return (T)Convert.ChangeType(value, conversionType, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                try
                {
                    return (T)Convert.ChangeType(value, conversionType);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        /// <summary>
        /// Chuyển đổi an toàn một giá trị sang kiểu decimal.
        /// Xử lý DBNull, null và các giá trị không hợp lệ.
        /// </summary>
        /// <param name="value">Giá trị cần chuyển đổi.</param>
        /// <param name="defaultValue">Giá trị mặc định nếu chuyển đổi thất bại (mặc định: 0).</param>
        /// <returns>Giá trị decimal hoặc giá trị mặc định.</returns>
        public static decimal ToDecimal(object value, decimal defaultValue = 0)
        {
            return ChangeType<decimal>(value, defaultValue);
        }

        /// <summary>
        /// Chuyển đổi an toàn một giá trị sang kiểu int.
        /// Xử lý DBNull, null và các giá trị không hợp lệ.
        /// </summary>
        /// <param name="value">Giá trị cần chuyển đổi.</param>
        /// <param name="defaultValue">Giá trị mặc định nếu chuyển đổi thất bại (mặc định: 0).</param>
        /// <returns>Giá trị int hoặc giá trị mặc định.</returns>
        public static int ToInt(object value, int defaultValue = 0)
        {
            return ChangeType<int>(value, defaultValue);
        }

        /// <summary>
        /// Chuyển đổi an toàn một giá trị sang kiểu DateTime.
        /// Trả về null nếu giá trị là DBNull, null hoặc không hợp lệ.
        /// </summary>
        /// <param name="value">Giá trị cần chuyển đổi.</param>
        /// <returns>Giá trị DateTime hoặc null.</returns>
        public static DateTime? ToDateTime(object value)
        {
            return ChangeType<DateTime?>(value, null);
        }

        /// <summary>
        /// Chuyển đổi an toàn một giá trị sang kiểu string.
        /// Xử lý DBNull và null.
        /// </summary>
        /// <param name="value">Giá trị cần chuyển đổi.</param>
        /// <param name="defaultValue">Giá trị mặc định nếu giá trị là null/DBNull (mặc định: "").</param>
        /// <returns>Chuỗi kết quả hoặc giá trị mặc định.</returns>
        public static string ToString(object value, string defaultValue = "")
        {
            if (value == null || value == DBNull.Value)
                return defaultValue;

            return value.ToString();
        }

        /// <summary>
        /// Thoát ký tự đặc biệt cho biểu thức lọc của DataTable (DataView.RowFilter hoặc DataTable.Compute/Select).
        /// Tránh lỗi cú pháp khi từ khóa tìm kiếm chứa ký tự nháy đơn.
        /// </summary>
        /// <param name="text">Chuỗi cần lọc.</param>
        /// <returns>Chuỗi đã được escape an toàn.</returns>
        public static string EscapeFilter(string text)
        {
            if (text == null)
                return "";
            return text.Replace("'", "''");
        }

        #endregion
    }
}
