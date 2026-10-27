using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ExamHelper.Database
{
    /// <summary>
    /// Lớp tiện ích tĩnh hỗ trợ thao tác với cơ sở dữ liệu SQL Server.
    /// Hỗ trợ đa kết nối thông qua hệ thống đặt tên connection string.
    /// Tất cả các truy vấn đều sử dụng tham số hóa (parameterized) để chống SQL Injection.
    /// </summary>
    public static class DbHelper
    {
        /// <summary>
        /// Từ điển lưu trữ các chuỗi kết nối theo tên. Khóa mặc định là "default".
        /// </summary>
        private static readonly Dictionary<string, string> _connectionStrings = new Dictionary<string, string>();

        /// <summary>
        /// Lỗi gần nhất phát sinh khi thực thi truy vấn cơ sở dữ liệu.
        /// </summary>
        public static Exception LastError { get; set; }

        /// <summary>
        /// Cho phép hiển thị hộp thoại MessageBox khi có lỗi CSDL (mặc định: true).
        /// Đặt false nếu muốn xử lý âm thầm hoặc trong môi trường kiểm thử.
        /// </summary>
        public static bool ShowErrorDialog { get; set; } = true;

        /// <summary>
        /// Sự kiện callback được gọi khi có lỗi CSDL xảy ra.
        /// Nhận vào (câu truy vấn SQL, Exception).
        /// </summary>
        public static Action<string, Exception> OnError { get; set; }

        /// <summary>
        /// Xử lý thông báo lỗi nội bộ.
        /// </summary>
        private static void HandleError(string title, string sql, Exception ex)
        {
            LastError = ex;
            OnError?.Invoke(sql, ex);

            if (ShowErrorDialog)
            {
                MessageBox.Show(
                    $"{title}:\n\n" +
                    $"SQL: {sql}\n\n" +
                    $"Chi tiết lỗi: {ex.Message}",
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        #region Cấu hình kết nối

        /// <summary>
        /// Đăng ký một chuỗi kết nối với tên chỉ định.
        /// Nếu tên đã tồn tại, chuỗi kết nối sẽ được ghi đè.
        /// </summary>
        /// <param name="name">Tên định danh cho chuỗi kết nối (ví dụ: "default", "report", "backup").</param>
        /// <param name="connectionString">Chuỗi kết nối đến SQL Server.</param>
        public static void SetConnectionString(string name, string connectionString)
        {
            _connectionStrings[name] = connectionString;
        }

        /// <summary>
        /// Đăng ký chuỗi kết nối mặc định (khóa "default").
        /// Đây là phím tắt cho <see cref="SetConnectionString(string, string)"/> với tên "default".
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến SQL Server.</param>
        public static void SetDefaultConnectionString(string connectionString)
        {
            _connectionStrings["default"] = connectionString;
        }

        /// <summary>
        /// Tạo và mở một kết nối mới đến cơ sở dữ liệu.
        /// </summary>
        /// <param name="dbName">Tên của chuỗi kết nối đã đăng ký. Mặc định là "default".</param>
        /// <returns>Đối tượng <see cref="SqlConnection"/> đã được mở sẵn.</returns>
        /// <exception cref="KeyNotFoundException">Ném ra khi tên kết nối chưa được đăng ký.</exception>
        public static SqlConnection GetConnection(string dbName = "default")
        {
            if (!_connectionStrings.ContainsKey(dbName))
            {
                throw new KeyNotFoundException(
                    $"Chuỗi kết nối với tên \"{dbName}\" chưa được đăng ký. " +
                    $"Hãy gọi SetConnectionString() trước.");
            }

            SqlConnection connection = new SqlConnection(_connectionStrings[dbName]);
            connection.Open();
            return connection;
        }

        #endregion

        #region GetDataTable

        /// <summary>
        /// Thực thi câu truy vấn SQL và trả về kết quả dưới dạng <see cref="DataTable"/>.
        /// Sử dụng kết nối mặc định ("default").
        /// </summary>
        /// <param name="sql">Câu truy vấn SQL (nên dùng tham số hóa, ví dụ: SELECT * FROM Users WHERE Id = @Id).</param>
        /// <param name="parameters">Danh sách các tham số SQL. Sử dụng <see cref="Param"/> để tạo nhanh.</param>
        /// <returns>
        /// <see cref="DataTable"/> chứa kết quả truy vấn.
        /// Trả về DataTable rỗng nếu có lỗi xảy ra.
        /// </returns>
        public static DataTable GetDataTable(string sql, params SqlParameter[] parameters)
        {
            return GetDataTable(sql, "default", parameters);
        }

        /// <summary>
        /// Thực thi câu truy vấn SQL và trả về kết quả dưới dạng <see cref="DataTable"/>.
        /// Cho phép chỉ định tên kết nối cụ thể.
        /// </summary>
        /// <param name="sql">Câu truy vấn SQL (nên dùng tham số hóa).</param>
        /// <param name="dbName">Tên của chuỗi kết nối đã đăng ký.</param>
        /// <param name="parameters">Danh sách các tham số SQL.</param>
        /// <returns>
        /// <see cref="DataTable"/> chứa kết quả truy vấn.
        /// Trả về DataTable rỗng nếu có lỗi xảy ra.
        /// </returns>
        public static DataTable GetDataTable(string sql, string dbName, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            SqlConnection connection = null;

            try
            {
                connection = GetConnection(dbName);

                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    if (parameters != null && parameters.Length > 0)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("Lỗi khi truy vấn dữ liệu", sql, ex);
            }
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }

            return dt;
        }

        #endregion

        #region ExecuteNonQuery

        /// <summary>
        /// Thực thi câu lệnh SQL không trả về kết quả (INSERT, UPDATE, DELETE).
        /// Sử dụng kết nối mặc định ("default").
        /// </summary>
        /// <param name="sql">Câu lệnh SQL (nên dùng tham số hóa, ví dụ: DELETE FROM Users WHERE Id = @Id).</param>
        /// <param name="parameters">Danh sách các tham số SQL. Sử dụng <see cref="Param"/> để tạo nhanh.</param>
        /// <returns>Số dòng bị ảnh hưởng. Trả về 0 nếu có lỗi xảy ra.</returns>
        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            return ExecuteNonQuery(sql, "default", parameters);
        }

        /// <summary>
        /// Thực thi câu lệnh SQL không trả về kết quả (INSERT, UPDATE, DELETE).
        /// Cho phép chỉ định tên kết nối cụ thể.
        /// </summary>
        /// <param name="sql">Câu lệnh SQL (nên dùng tham số hóa).</param>
        /// <param name="dbName">Tên của chuỗi kết nối đã đăng ký.</param>
        /// <param name="parameters">Danh sách các tham số SQL.</param>
        /// <returns>Số dòng bị ảnh hưởng. Trả về 0 nếu có lỗi xảy ra.</returns>
        public static int ExecuteNonQuery(string sql, string dbName, params SqlParameter[] parameters)
        {
            SqlConnection connection = null;

            try
            {
                connection = GetConnection(dbName);

                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    if (parameters != null && parameters.Length > 0)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                HandleError("Lỗi khi thực thi câu lệnh SQL", sql, ex);
                return 0;
            }
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }

        /// <summary>
        /// Thực thi câu lệnh SQL không trả về kết quả trong một Transaction hiện hành.
        /// </summary>
        /// <param name="sql">Câu lệnh SQL.</param>
        /// <param name="transaction">Đối tượng SqlTransaction đang hoạt động.</param>
        /// <param name="parameters">Danh sách các tham số SQL.</param>
        /// <returns>Số dòng bị ảnh hưởng.</returns>
        public static int ExecuteNonQuery(string sql, SqlTransaction transaction, params SqlParameter[] parameters)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction), "SqlTransaction không được null.");

            using (SqlCommand cmd = new SqlCommand(sql, transaction.Connection, transaction))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Thực thi câu lệnh SQL trả về giá trị đơn (scalar) trong một Transaction hiện hành.
        /// Thường dùng để lấy ID tự tăng (SCOPE_IDENTITY()) ngay sau khi thêm trong transaction.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu trả về.</typeparam>
        /// <param name="sql">Câu lệnh SQL.</param>
        /// <param name="transaction">Đối tượng SqlTransaction đang hoạt động.</param>
        /// <param name="parameters">Danh sách các tham số SQL.</param>
        /// <returns>Giá trị đơn kiểu T.</returns>
        public static T ExecuteScalar<T>(string sql, SqlTransaction transaction, params SqlParameter[] parameters)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction), "SqlTransaction không được null.");

            using (SqlCommand cmd = new SqlCommand(sql, transaction.Connection, transaction))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                object result = cmd.ExecuteScalar();
                return ExamHelper.Utilities.DataHelper.ChangeType<T>(result);
            }
        }

        /// <summary>
        /// Thực thi một chuỗi các thao tác CSDL bên trong một Transaction.
        /// Tự động Commit nếu thành công, tự động Rollback nếu có lỗi xảy ra.
        /// </summary>
        /// <param name="action">Hành động nhận vào SqlTransaction.</param>
        /// <param name="dbName">Tên kết nối CSDL (mặc định: "default").</param>
        /// <returns>True nếu transaction hoàn tất thành công, False nếu thất bại và đã rollback.</returns>
        public static bool ExecuteTransaction(Action<SqlTransaction> action, string dbName = "default")
        {
            SqlConnection connection = null;
            SqlTransaction transaction = null;

            try
            {
                connection = GetConnection(dbName);
                transaction = connection.BeginTransaction();

                action(transaction);

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { transaction?.Rollback(); } catch { }
                HandleError("Lỗi khi thực thi Transaction (đã rollback)", "TRANSACTION BATCH", ex);
                return false;
            }
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }

        #endregion

        #region ExecuteScalar

        /// <summary>
        /// Thực thi câu truy vấn SQL và trả về giá trị đơn (scalar) với kiểu dữ liệu chỉ định.
        /// Sử dụng kết nối mặc định ("default").
        /// Thường dùng cho các truy vấn COUNT, SUM, MAX, MIN hoặc lấy một giá trị duy nhất.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu mong muốn trả về (int, string, decimal, DateTime, v.v.).</typeparam>
        /// <param name="sql">Câu truy vấn SQL (ví dụ: SELECT COUNT(*) FROM Users WHERE Active = @Active).</param>
        /// <param name="parameters">Danh sách các tham số SQL.</param>
        /// <returns>Giá trị scalar được chuyển đổi sang kiểu T. Trả về default(T) nếu có lỗi hoặc kết quả NULL.</returns>
        public static T ExecuteScalar<T>(string sql, params SqlParameter[] parameters)
        {
            return ExecuteScalar<T>(sql, "default", parameters);
        }

        /// <summary>
        /// Thực thi câu truy vấn SQL và trả về giá trị đơn (scalar) với kiểu dữ liệu chỉ định.
        /// Cho phép chỉ định tên kết nối cụ thể.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu mong muốn trả về.</typeparam>
        /// <param name="sql">Câu truy vấn SQL.</param>
        /// <param name="dbName">Tên của chuỗi kết nối đã đăng ký.</param>
        /// <param name="parameters">Danh sách các tham số SQL.</param>
        /// <returns>Giá trị scalar được chuyển đổi sang kiểu T. Trả về default(T) nếu có lỗi hoặc kết quả NULL.</returns>
        public static T ExecuteScalar<T>(string sql, string dbName, params SqlParameter[] parameters)
        {
            SqlConnection connection = null;

            try
            {
                connection = GetConnection(dbName);

                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    if (parameters != null && parameters.Length > 0)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    object result = cmd.ExecuteScalar();
                    return ExamHelper.Utilities.DataHelper.ChangeType<T>(result);
                }
            }
            catch (Exception ex)
            {
                HandleError("Lỗi khi thực thi truy vấn scalar", sql, ex);
                return default(T);
            }
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }

        #endregion

        #region Exists

        /// <summary>
        /// Kiểm tra xem câu truy vấn SQL có trả về ít nhất một dòng kết quả hay không.
        /// Sử dụng kết nối mặc định ("default").
        /// Thường dùng để kiểm tra sự tồn tại của dữ liệu trước khi thực hiện thao tác.
        /// </summary>
        /// <param name="sql">Câu truy vấn SQL (ví dụ: SELECT 1 FROM Users WHERE Username = @Username).</param>
        /// <param name="parameters">Danh sách các tham số SQL.</param>
        /// <returns>True nếu có ít nhất một dòng kết quả, False nếu không có hoặc có lỗi.</returns>
        public static bool Exists(string sql, params SqlParameter[] parameters)
        {
            return Exists(sql, "default", parameters);
        }

        /// <summary>
        /// Kiểm tra xem câu truy vấn SQL có trả về ít nhất một dòng kết quả hay không.
        /// Cho phép chỉ định tên kết nối cụ thể.
        /// </summary>
        /// <param name="sql">Câu truy vấn SQL.</param>
        /// <param name="dbName">Tên của chuỗi kết nối đã đăng ký.</param>
        /// <param name="parameters">Danh sách các tham số SQL.</param>
        /// <returns>True nếu có ít nhất một dòng kết quả, False nếu không có hoặc có lỗi.</returns>
        public static bool Exists(string sql, string dbName, params SqlParameter[] parameters)
        {
            SqlConnection connection = null;

            try
            {
                connection = GetConnection(dbName);

                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    if (parameters != null && parameters.Length > 0)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.HasRows;
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("Lỗi khi kiểm tra sự tồn tại dữ liệu", sql, ex);
                return false;
            }
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }

        #endregion

        #region Tạo tham số nhanh

        /// <summary>
        /// Tạo nhanh một <see cref="SqlParameter"/>.
        /// Tự động chuyển đổi giá trị null thành <see cref="DBNull.Value"/>.
        /// </summary>
        /// <param name="name">Tên tham số (ví dụ: "@Id", "@TenSV"). Có thể bỏ ký tự @ ở đầu.</param>
        /// <param name="value">Giá trị của tham số. Nếu null sẽ được chuyển thành DBNull.Value.</param>
        /// <returns>Đối tượng <see cref="SqlParameter"/> đã được khởi tạo.</returns>
        public static SqlParameter Param(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        /// <summary>
        /// Tạo nhanh một <see cref="SqlParameter"/> cho giá trị kiểu <see cref="DateTime"/> nullable.
        /// Nếu giá trị là null, tham số sẽ có giá trị <see cref="DBNull.Value"/>.
        /// </summary>
        /// <param name="name">Tên tham số (ví dụ: "@NgaySinh").</param>
        /// <param name="value">Giá trị DateTime nullable.</param>
        /// <returns>Đối tượng <see cref="SqlParameter"/> đã được khởi tạo.</returns>
        public static SqlParameter ParamDate(string name, DateTime? value)
        {
            return new SqlParameter(name, (object)value ?? DBNull.Value);
        }

        /// <summary>
        /// Tạo nhanh một <see cref="SqlParameter"/> cho giá trị kiểu <see cref="int"/> nullable.
        /// Nếu giá trị là null, tham số sẽ có giá trị <see cref="DBNull.Value"/>.
        /// </summary>
        /// <param name="name">Tên tham số (ví dụ: "@SoLuong").</param>
        /// <param name="value">Giá trị int nullable.</param>
        /// <returns>Đối tượng <see cref="SqlParameter"/> đã được khởi tạo.</returns>
        public static SqlParameter ParamInt(string name, int? value)
        {
            return new SqlParameter(name, (object)value ?? DBNull.Value);
        }

        /// <summary>
        /// Tạo nhanh một <see cref="SqlParameter"/> cho giá trị kiểu <see cref="decimal"/> nullable.
        /// Nếu giá trị là null, tham số sẽ có giá trị <see cref="DBNull.Value"/>.
        /// </summary>
        /// <param name="name">Tên tham số (ví dụ: "@GiaTien", "@DiemTB").</param>
        /// <param name="value">Giá trị decimal nullable.</param>
        /// <returns>Đối tượng <see cref="SqlParameter"/> đã được khởi tạo.</returns>
        public static SqlParameter ParamDecimal(string name, decimal? value)
        {
            return new SqlParameter(name, (object)value ?? DBNull.Value);
        }

        /// <summary>
        /// Tạo nhanh một <see cref="SqlParameter"/> cho truy vấn LIKE.
        /// Tự động bọc giá trị với ký tự % ở hai đầu (ví dụ: "Nguyễn" → "%Nguyễn%").
        /// Nếu giá trị là null hoặc rỗng, tham số sẽ có giá trị "%%".
        /// </summary>
        /// <param name="name">Tên tham số (ví dụ: "@TenTimKiem").</param>
        /// <param name="value">Giá trị cần tìm kiếm (không cần thêm % thủ công).</param>
        /// <returns>Đối tượng <see cref="SqlParameter"/> với giá trị đã được bọc %.</returns>
        public static SqlParameter ParamLike(string name, string value)
        {
            return new SqlParameter(name, $"%{value ?? string.Empty}%");
        }

        #endregion
    }
}
