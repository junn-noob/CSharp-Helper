using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

namespace ExamHelper.Database
{
    /// <summary>
    /// Hỗ trợ các thao tác CRUD tổng quát đa hệ quản trị CSDL (SQL Server, SQLite, MySQL, PostgreSQL, OleDb, Odbc).
    /// Tên bảng/cột được kiểm tra whitelist và bọc ngoặc theo từng hệ CSDL; giá trị dữ liệu luôn truyền qua tham số.
    /// </summary>
    public static class CrudHelper
    {
        #region Kiểm tra & Chuẩn hóa định danh

        /// <summary>
        /// Pattern hợp lệ cho tên bảng/cột: chữ cái, số, gạch dưới, dấu chấm, dấu ngoặc vuông.
        /// </summary>
        private static readonly Regex IdentifierPattern = new Regex(
            @"^[\w\[\]\.\s]+$", RegexOptions.Compiled);

        private static string CleanIdentifier(string name)
            => (name ?? "").Trim().Trim('[', ']', '`', '"');

        private static string SafeParamSuffix(string columnName)
            => Regex.Replace(CleanIdentifier(columnName), @"\W", "_");

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

            string[] parts = name.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim().Trim('[', ']');
                if (string.IsNullOrWhiteSpace(part) || part.Contains("[") || part.Contains("]"))
                    throw new ArgumentException($"Tên '{name}' không hợp lệ.");
            }
        }

        /// <summary>
        /// Bọc tên bảng/cột đúng cú pháp theo từng loại CSDL đang kết nối ([a].[b] cho SQL Server, `a`.`b` cho MySQL, "a"."b" cho SQLite/PostgreSQL).
        /// </summary>
        private static string Bracket(string name, string dbName = "default")
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            DbDialect dialect = DbHelper.TryGetDialect(dbName);
            return DbHelper.QuoteIdentifier(name, dialect);
        }

        private static string BuildInsertSql(string table, Dictionary<string, object> values, string dbName, out List<SqlParameter> parameters)
        {
            ValidateIdentifier(table);
            if (values == null || values.Count == 0)
                throw new ArgumentException("Danh sách giá trị không được rỗng.");

            var columns = new List<string>();
            var paramNames = new List<string>();
            parameters = new List<SqlParameter>();

            foreach (var kvp in values)
            {
                ValidateIdentifier(kvp.Key);
                columns.Add(Bracket(kvp.Key, dbName));
                string paramName = $"@{SafeParamSuffix(kvp.Key)}_{parameters.Count}";
                paramNames.Add(paramName);
                parameters.Add(new SqlParameter(paramName, kvp.Value ?? DBNull.Value));
            }

            return $"INSERT INTO {Bracket(table, dbName)} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)})";
        }

        private static List<string> BuildUpdateSetClauses(
            Dictionary<string, object> values,
            IList<KeyValuePair<string, object>> keyValues,
            string dbName,
            List<SqlParameter> parameters)
        {
            var setClauses = new List<string>();
            foreach (var kvp in values)
            {
                string cleanField = CleanIdentifier(kvp.Key);
                bool matchesUnchangedKey = false;
                for (int i = 0; i < keyValues.Count; i++)
                {
                    if (string.Equals(CleanIdentifier(keyValues[i].Key), cleanField, StringComparison.OrdinalIgnoreCase))
                    {
                        if (Equals(kvp.Value?.ToString()?.Trim(), keyValues[i].Value?.ToString()?.Trim()))
                            matchesUnchangedKey = true;
                        break;
                    }
                }
                if (matchesUnchangedKey) continue;

                ValidateIdentifier(kvp.Key);
                string paramName = $"@{SafeParamSuffix(kvp.Key)}_{parameters.Count}";
                setClauses.Add($"{Bracket(kvp.Key, dbName)} = {paramName}");
                parameters.Add(new SqlParameter(paramName, kvp.Value ?? DBNull.Value));
            }
            return setClauses;
        }

        private static string BuildWhereClause(
            IEnumerable<KeyValuePair<string, object>> keys,
            string dbName,
            string paramPrefix,
            List<SqlParameter> parameters)
        {
            var whereClauses = new List<string>();
            foreach (var kvp in keys)
            {
                ValidateIdentifier(kvp.Key);
                string paramName = $"@{paramPrefix}{SafeParamSuffix(kvp.Key)}_{parameters.Count}";
                whereClauses.Add($"{Bracket(kvp.Key, dbName)} = {paramName}");
                parameters.Add(new SqlParameter(paramName, kvp.Value ?? DBNull.Value));
            }
            return string.Join(" AND ", whereClauses);
        }

        #endregion

        #region Insert

        /// <summary>
        /// Thêm một dòng dữ liệu vào bảng.
        /// </summary>
        public static int Insert(string table, Dictionary<string, object> values, string dbName = "default")
        {
            string sql = BuildInsertSql(table, values, dbName, out List<SqlParameter> parameters);
            return DbHelper.ExecuteNonQuery(sql, dbName, parameters.ToArray());
        }

        /// <summary>
        /// Thêm một dòng dữ liệu vào bảng và trả về giá trị khóa tự tăng (IDENTITY) vừa sinh ra.
        /// </summary>
        public static T InsertAndGetId<T>(string table, Dictionary<string, object> values, string dbName = "default")
        {
            string insertSql = BuildInsertSql(table, values, dbName, out List<SqlParameter> parameters);
            string idSql = DbHelper.GetLastInsertIdSql(DbHelper.TryGetDialect(dbName));
            return DbHelper.ExecuteScalar<T>($"{insertSql}; {idSql};", dbName, parameters.ToArray());
        }

        #endregion

        #region Update

        /// <summary>
        /// Cập nhật một dòng dữ liệu theo khóa chính.
        /// </summary>
        public static int Update(string table, Dictionary<string, object> values,
            string keyColumn, object keyValue, string dbName = "default")
        {
            ValidateIdentifier(table);
            ValidateIdentifier(keyColumn);
            if (values == null || values.Count == 0)
                throw new ArgumentException("Danh sách giá trị không được rỗng.");

            var parameters = new List<SqlParameter>();
            var keyPairs = new[] { new KeyValuePair<string, object>(keyColumn, keyValue) };
            var setClauses = BuildUpdateSetClauses(values, keyPairs, dbName, parameters);
            if (setClauses.Count == 0)
                return 0;

            string keyParamName = $"@key_{SafeParamSuffix(keyColumn)}";
            parameters.Add(new SqlParameter(keyParamName, keyValue ?? DBNull.Value));

            string sql = $"UPDATE {Bracket(table, dbName)} SET {string.Join(", ", setClauses)} WHERE {Bracket(keyColumn, dbName)} = {keyParamName}";
            return DbHelper.ExecuteNonQuery(sql, dbName, parameters.ToArray());
        }

        /// <summary>
        /// Cập nhật dữ liệu với khóa chính phức hợp (nhiều cột khóa).
        /// </summary>
        public static int Update(string table, Dictionary<string, object> values,
            Dictionary<string, object> keys, string dbName = "default")
        {
            ValidateIdentifier(table);
            if (values == null || values.Count == 0)
                throw new ArgumentException("Danh sách giá trị không được rỗng.");
            if (keys == null || keys.Count == 0)
                throw new ArgumentException("Danh sách khóa chính không được rỗng.");

            var parameters = new List<SqlParameter>();
            var setClauses = BuildUpdateSetClauses(values, keys.ToList(), dbName, parameters);
            if (setClauses.Count == 0)
                return 0;

            string whereClause = BuildWhereClause(keys, dbName, "k_", parameters);
            string sql = $"UPDATE {Bracket(table, dbName)} SET {string.Join(", ", setClauses)} WHERE {whereClause}";
            return DbHelper.ExecuteNonQuery(sql, dbName, parameters.ToArray());
        }

        #endregion

        #region Delete

        /// <summary>
        /// Xóa một dòng theo khóa chính.
        /// </summary>
        public static int Delete(string table, string keyColumn, object keyValue, string dbName = "default")
        {
            ValidateIdentifier(table);
            ValidateIdentifier(keyColumn);

            string paramName = "@keyValue";
            string sql = $"DELETE FROM {Bracket(table, dbName)} WHERE {Bracket(keyColumn, dbName)} = {paramName}";
            return DbHelper.ExecuteNonQuery(sql, dbName, new SqlParameter(paramName, keyValue ?? DBNull.Value));
        }

        /// <summary>
        /// Xóa một dòng theo khóa chính phức hợp (nhiều cột khóa).
        /// </summary>
        public static int Delete(string table, Dictionary<string, object> keys, string dbName = "default")
        {
            ValidateIdentifier(table);
            if (keys == null || keys.Count == 0)
                throw new ArgumentException("Danh sách khóa chính không được rỗng.");

            var parameters = new List<SqlParameter>();
            string whereClause = BuildWhereClause(keys, dbName, "k_", parameters);
            string sql = $"DELETE FROM {Bracket(table, dbName)} WHERE {whereClause}";
            return DbHelper.ExecuteNonQuery(sql, dbName, parameters.ToArray());
        }

        #endregion

        #region Kiểm tra tồn tại

        /// <summary>
        /// Kiểm tra giá trị đã tồn tại trong bảng chưa (dùng khi thêm mới).
        /// </summary>
        public static bool Exists(string table, string column, object value, string dbName = "default")
        {
            ValidateIdentifier(table);
            ValidateIdentifier(column);

            string sql = $"SELECT COUNT(*) FROM {Bracket(table, dbName)} WHERE {Bracket(column, dbName)} = @checkValue";
            int count = DbHelper.ExecuteScalar<int>(sql, dbName, new SqlParameter("@checkValue", value ?? DBNull.Value));
            return count > 0;
        }

        /// <summary>
        /// Kiểm tra tồn tại với nhiều điều kiện kết hợp (Dictionary).
        /// </summary>
        public static bool Exists(string table, Dictionary<string, object> keys, string dbName = "default")
        {
            ValidateIdentifier(table);
            if (keys == null || keys.Count == 0)
                return false;

            var parameters = new List<SqlParameter>();
            string whereClause = BuildWhereClause(keys, dbName, "c_", parameters);
            string sql = $"SELECT COUNT(*) FROM {Bracket(table, dbName)} WHERE {whereClause}";
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

            string sql = $"SELECT COUNT(*) FROM {Bracket(table, dbName)} WHERE {Bracket(checkColumn, dbName)} = @checkValue AND {Bracket(keyColumn, dbName)} <> @keyValue";
            int count = DbHelper.ExecuteScalar<int>(sql, dbName,
                new SqlParameter("@checkValue", checkValue ?? DBNull.Value),
                new SqlParameter("@keyValue", keyValue));
            return count > 0;
        }

        #endregion

        #region Truy vấn nhanh (GetAll / GetById)

        /// <summary>
        /// Lấy toàn bộ dữ liệu từ bảng trong 1 câu lệnh (tùy chọn ORDER BY an toàn).
        /// Tự động mở và đóng kết nối sau khi lấy xong.
        /// </summary>
        public static DataTable GetAll(string table, string orderBy = null, string dbName = "default")
        {
            ValidateIdentifier(table);
            string sql = $"SELECT * FROM {Bracket(table, dbName)}";

            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                var orderParts = new List<string>();
                foreach (string segment in orderBy.Split(','))
                {
                    string[] tokens = segment.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length == 0 || tokens.Length > 2)
                        throw new ArgumentException($"Mệnh đề ORDER BY '{orderBy}' không hợp lệ.");

                    ValidateIdentifier(tokens[0]);
                    string dir = "";
                    if (tokens.Length == 2)
                    {
                        if (!string.Equals(tokens[1], "ASC", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(tokens[1], "DESC", StringComparison.OrdinalIgnoreCase))
                            throw new ArgumentException($"Hướng sắp xếp '{tokens[1]}' không hợp lệ (chỉ hỗ trợ ASC/DESC).");
                        dir = " " + tokens[1].ToUpperInvariant();
                    }
                    orderParts.Add(Bracket(tokens[0], dbName) + dir);
                }

                if (orderParts.Count > 0)
                    sql += " ORDER BY " + string.Join(", ", orderParts);
            }

            return DbHelper.GetDataTable(sql, dbName);
        }

        /// <summary>
        /// Lấy nhanh 1 dòng dữ liệu theo khóa chính (trả về null nếu không tìm thấy).
        /// </summary>
        public static DataRow GetById(string table, string keyColumn, object keyValue, string dbName = "default")
        {
            ValidateIdentifier(table);
            ValidateIdentifier(keyColumn);

            string sql = $"SELECT * FROM {Bracket(table, dbName)} WHERE {Bracket(keyColumn, dbName)} = @id";
            return DbHelper.GetDataRow(sql, dbName, new SqlParameter("@id", keyValue ?? DBNull.Value));
        }

        #endregion
    }
}
