using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

namespace ExamHelper.Database
{
    /// <summary>
    /// Hỗ trợ các thao tác CRUD tổng quát với SQL Server.
    /// Tên bảng/cột được kiểm tra whitelist, giá trị dữ liệu luôn dùng SqlParameter.
    /// </summary>
    public static class CrudHelper
    {
        #region Kiểm tra tên định danh

        /// <summary>
        /// Pattern hợp lệ cho tên bảng/cột: chữ cái, số, gạch dưới, dấu chấm, dấu ngoặc vuông.
        /// </summary>
        private static readonly Regex IdentifierPattern = new Regex(
            @"^[\w\[\]\.\s]+$", RegexOptions.Compiled);

        /// <summary>
        /// Kiểm tra tên bảng hoặc cột có hợp lệ không (chống SQL injection qua tên bảng).
        /// </summary>
        /// <param name="name">Tên cần kiểm tra</param>
        /// <exception cref="ArgumentException">Nếu tên chứa ký tự không hợp lệ</exception>
        private static void ValidateIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên bảng hoặc cột không được để trống.");
            if (!IdentifierPattern.IsMatch(name))
                throw new ArgumentException($"Tên '{name}' chứa ký tự không hợp lệ. Chỉ cho phép chữ cái, số, gạch dưới và dấu ngoặc vuông.");
        }

        /// <summary>
        /// Bọc tên bảng/cột trong dấu ngoặc vuông nếu chưa có.
        /// Hỗ trợ cả tên có tiền tố schema (ví dụ: dbo.NhanVien -> [dbo].[NhanVien]).
        /// </summary>
        private static string Bracket(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            string[] parts = name.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim().Trim('[', ']');
                parts[i] = $"[{p}]";
            }
            return string.Join(".", parts);
        }

        #endregion

        #region Insert

        /// <summary>
        /// Thêm một dòng dữ liệu vào bảng.
        /// </summary>
        /// <param name="table">Tên bảng</param>
        /// <param name="values">Dictionary (tên cột → giá trị)</param>
        /// <param name="dbName">Tên database (mặc định "default")</param>
        /// <returns>Số dòng bị ảnh hưởng (thường là 1 nếu thành công)</returns>
        /// <example>
        /// CrudHelper.Insert("NhanVien", new Dictionary&lt;string, object&gt; {
        ///     {"MaNV", "NV001"},
        ///     {"HoTen", "Nguyễn Văn A"},
        ///     {"NgaySinh", dtpNgaySinh.Value},
        ///     {"Luong", 5000000m}
        /// });
        /// </example>
        public static int Insert(string table, Dictionary<string, object> values, string dbName = "default")
        {
            ValidateIdentifier(table);
            if (values == null || values.Count == 0)
                throw new ArgumentException("Danh sách giá trị không được rỗng.");

            var columns = new List<string>();
            var paramNames = new List<string>();
            var parameters = new List<SqlParameter>();

            foreach (var kvp in values)
            {
                ValidateIdentifier(kvp.Key);
                columns.Add(Bracket(kvp.Key));
                string cleanKey = Regex.Replace(kvp.Key.Replace("[", "").Replace("]", ""), @"\W", "_");
                string paramName = $"@{cleanKey}_{parameters.Count}";
                paramNames.Add(paramName);
                parameters.Add(new SqlParameter(paramName, kvp.Value ?? DBNull.Value));
            }

            string sql = $"INSERT INTO {Bracket(table)} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)})";
            return DbHelper.ExecuteNonQuery(sql, dbName, parameters.ToArray());
        }

        /// <summary>
        /// Thêm một dòng dữ liệu vào bảng và trả về giá trị khóa tự tăng (IDENTITY) vừa sinh ra.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của khóa tự tăng (int, long, decimal,...).</typeparam>
        /// <param name="table">Tên bảng.</param>
        /// <param name="values">Dictionary chứa tên cột và giá trị.</param>
        /// <param name="dbName">Tên kết nối CSDL.</param>
        /// <returns>Giá trị khóa tự tăng vừa sinh ra, hoặc default(T) nếu thất bại.</returns>
        public static T InsertAndGetId<T>(string table, Dictionary<string, object> values, string dbName = "default")
        {
            ValidateIdentifier(table);
            if (values == null || values.Count == 0)
                throw new ArgumentException("Danh sách giá trị không được rỗng.");

            var columns = new List<string>();
            var paramNames = new List<string>();
            var parameters = new List<SqlParameter>();

            foreach (var kvp in values)
            {
                ValidateIdentifier(kvp.Key);
                columns.Add(Bracket(kvp.Key));
                string cleanKey = Regex.Replace(kvp.Key.Replace("[", "").Replace("]", ""), @"\W", "_");
                string paramName = $"@{cleanKey}_{parameters.Count}";
                paramNames.Add(paramName);
                parameters.Add(new SqlParameter(paramName, kvp.Value ?? DBNull.Value));
            }

            string sql = $"INSERT INTO {Bracket(table)} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)}); SELECT SCOPE_IDENTITY();";
            return DbHelper.ExecuteScalar<T>(sql, dbName, parameters.ToArray());
        }

        #endregion

        #region Update

        /// <summary>
        /// Cập nhật một dòng dữ liệu theo khóa chính.
        /// </summary>
        /// <param name="table">Tên bảng</param>
        /// <param name="values">Dictionary (tên cột → giá trị mới), không bao gồm cột khóa</param>
        /// <param name="keyColumn">Tên cột khóa chính</param>
        /// <param name="keyValue">Giá trị khóa chính của dòng cần cập nhật</param>
        /// <param name="dbName">Tên database</param>
        /// <returns>Số dòng bị ảnh hưởng</returns>
        /// <example>
        /// CrudHelper.Update("NhanVien",
        ///     new Dictionary&lt;string, object&gt; {
        ///         {"HoTen", "Nguyễn Văn B"},
        ///         {"Luong", 6000000m}
        ///     },
        ///     "MaNV", "NV001");
        /// </example>
        public static int Update(string table, Dictionary<string, object> values,
            string keyColumn, object keyValue, string dbName = "default")
        {
            ValidateIdentifier(table);
            ValidateIdentifier(keyColumn);
            if (values == null || values.Count == 0)
                throw new ArgumentException("Danh sách giá trị không được rỗng.");

            var setClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            foreach (var kvp in values)
            {
                // Tự động bỏ qua cột khóa nếu giá trị không đổi (tránh lỗi SQL Server "Cannot update identity column"
                // và giúp lập trình viên không cần gọi values.Remove(keyColumn) thủ công)
                if (string.Equals(kvp.Key.Replace("[", "").Replace("]", ""), keyColumn.Replace("[", "").Replace("]", ""), StringComparison.OrdinalIgnoreCase))
                {
                    if (Equals(kvp.Value?.ToString(), keyValue?.ToString()))
                        continue;
                }

                ValidateIdentifier(kvp.Key);
                string cleanKey = Regex.Replace(kvp.Key.Replace("[", "").Replace("]", ""), @"\W", "_");
                string paramName = $"@{cleanKey}_{parameters.Count}";
                setClauses.Add($"{Bracket(kvp.Key)} = {paramName}");
                parameters.Add(new SqlParameter(paramName, kvp.Value ?? DBNull.Value));
            }

            if (setClauses.Count == 0)
                return 0;

            // Tham số cho điều kiện WHERE
            string cleanKeyCol = Regex.Replace(keyColumn.Replace("[", "").Replace("]", ""), @"\W", "_");
            string keyParamName = $"@key_{cleanKeyCol}";
            parameters.Add(new SqlParameter(keyParamName, keyValue ?? DBNull.Value));

            string sql = $"UPDATE {Bracket(table)} SET {string.Join(", ", setClauses)} WHERE {Bracket(keyColumn)} = {keyParamName}";
            return DbHelper.ExecuteNonQuery(sql, dbName, parameters.ToArray());
        }

        /// <summary>
        /// Cập nhật dữ liệu với khóa chính phức hợp (nhiều cột khóa).
        /// </summary>
        /// <param name="table">Tên bảng.</param>
        /// <param name="values">Dictionary chứa tên cột và giá trị mới.</param>
        /// <param name="keys">Dictionary chứa các cột khóa và giá trị định danh dòng cần cập nhật.</param>
        /// <param name="dbName">Tên database.</param>
        /// <returns>Số dòng bị ảnh hưởng.</returns>
        public static int Update(string table, Dictionary<string, object> values,
            Dictionary<string, object> keys, string dbName = "default")
        {
            ValidateIdentifier(table);
            if (values == null || values.Count == 0)
                throw new ArgumentException("Danh sách giá trị không được rỗng.");
            if (keys == null || keys.Count == 0)
                throw new ArgumentException("Danh sách khóa chính không được rỗng.");

            var setClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            foreach (var kvp in values)
            {
                string cleanField = kvp.Key.Replace("[", "").Replace("]", "");
                var matchingKey = keys.FirstOrDefault(k => string.Equals(k.Key.Replace("[", "").Replace("]", ""), cleanField, StringComparison.OrdinalIgnoreCase));
                if (!matchingKey.Equals(default(KeyValuePair<string, object>)))
                {
                    if (Equals(kvp.Value?.ToString(), matchingKey.Value?.ToString()))
                        continue;
                }

                ValidateIdentifier(kvp.Key);
                string cleanKey = Regex.Replace(cleanField, @"\W", "_");
                string paramName = $"@{cleanKey}_{parameters.Count}";
                setClauses.Add($"{Bracket(kvp.Key)} = {paramName}");
                parameters.Add(new SqlParameter(paramName, kvp.Value ?? DBNull.Value));
            }

            if (setClauses.Count == 0)
                return 0;

            var whereClauses = new List<string>();
            foreach (var kvp in keys)
            {
                ValidateIdentifier(kvp.Key);
                string cleanKey = Regex.Replace(kvp.Key.Replace("[", "").Replace("]", ""), @"\W", "_");
                string paramName = $"@k_{cleanKey}_{parameters.Count}";
                whereClauses.Add($"{Bracket(kvp.Key)} = {paramName}");
                parameters.Add(new SqlParameter(paramName, kvp.Value ?? DBNull.Value));
            }

            string sql = $"UPDATE {Bracket(table)} SET {string.Join(", ", setClauses)} WHERE {string.Join(" AND ", whereClauses)}";
            return DbHelper.ExecuteNonQuery(sql, dbName, parameters.ToArray());
        }

        #endregion

        #region Delete

        /// <summary>
        /// Xóa một dòng theo khóa chính.
        /// </summary>
        /// <param name="table">Tên bảng</param>
        /// <param name="keyColumn">Tên cột khóa chính</param>
        /// <param name="keyValue">Giá trị khóa của dòng cần xóa</param>
        /// <param name="dbName">Tên database</param>
        /// <returns>Số dòng bị ảnh hưởng</returns>
        public static int Delete(string table, string keyColumn, object keyValue, string dbName = "default")
        {
            ValidateIdentifier(table);
            ValidateIdentifier(keyColumn);

            string paramName = "@keyValue";
            string sql = $"DELETE FROM {Bracket(table)} WHERE {Bracket(keyColumn)} = {paramName}";
            return DbHelper.ExecuteNonQuery(sql, dbName, new SqlParameter(paramName, keyValue ?? DBNull.Value));
        }

        /// <summary>
        /// Xóa một dòng theo khóa chính phức hợp (nhiều cột khóa).
        /// </summary>
        /// <param name="table">Tên bảng.</param>
        /// <param name="keys">Dictionary chứa các cột khóa và giá trị của dòng cần xóa.</param>
        /// <param name="dbName">Tên database.</param>
        /// <returns>Số dòng bị ảnh hưởng.</returns>
        public static int Delete(string table, Dictionary<string, object> keys, string dbName = "default")
        {
            ValidateIdentifier(table);
            if (keys == null || keys.Count == 0)
                throw new ArgumentException("Danh sách khóa chính không được rỗng.");

            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            foreach (var kvp in keys)
            {
                ValidateIdentifier(kvp.Key);
                string cleanKey = Regex.Replace(kvp.Key.Replace("[", "").Replace("]", ""), @"\W", "_");
                string paramName = $"@k_{cleanKey}_{parameters.Count}";
                whereClauses.Add($"{Bracket(kvp.Key)} = {paramName}");
                parameters.Add(new SqlParameter(paramName, kvp.Value ?? DBNull.Value));
            }

            string sql = $"DELETE FROM {Bracket(table)} WHERE {string.Join(" AND ", whereClauses)}";
            return DbHelper.ExecuteNonQuery(sql, dbName, parameters.ToArray());
        }

        #endregion

        #region Kiểm tra tồn tại

        /// <summary>
        /// Kiểm tra giá trị đã tồn tại trong bảng chưa (dùng khi thêm mới).
        /// </summary>
        /// <param name="table">Tên bảng</param>
        /// <param name="column">Tên cột cần kiểm tra</param>
        /// <param name="value">Giá trị cần kiểm tra</param>
        /// <param name="dbName">Tên database</param>
        /// <returns>true nếu đã tồn tại</returns>
        public static bool Exists(string table, string column, object value, string dbName = "default")
        {
            ValidateIdentifier(table);
            ValidateIdentifier(column);

            string sql = $"SELECT COUNT(*) FROM {Bracket(table)} WHERE {Bracket(column)} = @checkValue";
            int count = DbHelper.ExecuteScalar<int>(sql, dbName, new SqlParameter("@checkValue", value ?? DBNull.Value));
            return count > 0;
        }

        /// <summary>
        /// Kiểm tra tồn tại với nhiều điều kiện kết hợp (Dictionary).
        /// </summary>
        /// <param name="table">Tên bảng.</param>
        /// <param name="keys">Dictionary chứa các cột và giá trị cần kiểm tra.</param>
        /// <param name="dbName">Tên database.</param>
        /// <returns>true nếu đã tồn tại bản ghi thỏa mãn tất cả điều kiện.</returns>
        public static bool Exists(string table, Dictionary<string, object> keys, string dbName = "default")
        {
            ValidateIdentifier(table);
            if (keys == null || keys.Count == 0)
                return false;

            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            foreach (var kvp in keys)
            {
                ValidateIdentifier(kvp.Key);
                string cleanKey = Regex.Replace(kvp.Key.Replace("[", "").Replace("]", ""), @"\W", "_");
                string paramName = $"@c_{cleanKey}_{parameters.Count}";
                whereClauses.Add($"{Bracket(kvp.Key)} = {paramName}");
                parameters.Add(new SqlParameter(paramName, kvp.Value ?? DBNull.Value));
            }

            string sql = $"SELECT COUNT(*) FROM {Bracket(table)} WHERE {string.Join(" AND ", whereClauses)}";
            int count = DbHelper.ExecuteScalar<int>(sql, dbName, parameters.ToArray());
            return count > 0;
        }

        /// <summary>
        /// Kiểm tra giá trị đã tồn tại nhưng loại trừ dòng hiện tại (dùng khi sửa).
        /// Ví dụ: kiểm tra mã nhân viên trùng nhưng bỏ qua chính dòng đang sửa.
        /// </summary>
        /// <param name="table">Tên bảng</param>
        /// <param name="checkColumn">Cột cần kiểm tra trùng</param>
        /// <param name="checkValue">Giá trị cần kiểm tra</param>
        /// <param name="keyColumn">Cột khóa chính</param>
        /// <param name="keyValue">Giá trị khóa chính của dòng hiện tại (loại trừ)</param>
        /// <param name="dbName">Tên database</param>
        /// <returns>true nếu đã tồn tại ở dòng khác</returns>
        public static bool ExistsExcept(string table, string checkColumn, object checkValue,
            string keyColumn, object keyValue, string dbName = "default")
        {
            ValidateIdentifier(table);
            ValidateIdentifier(checkColumn);
            ValidateIdentifier(keyColumn);

            if (keyValue == null || keyValue == DBNull.Value)
            {
                return Exists(table, checkColumn, checkValue, dbName);
            }

            string sql = $"SELECT COUNT(*) FROM {Bracket(table)} WHERE {Bracket(checkColumn)} = @checkValue AND {Bracket(keyColumn)} <> @keyValue";
            int count = DbHelper.ExecuteScalar<int>(sql, dbName,
                new SqlParameter("@checkValue", checkValue ?? DBNull.Value),
                new SqlParameter("@keyValue", keyValue));
            return count > 0;
        }

        #endregion
    }
}
