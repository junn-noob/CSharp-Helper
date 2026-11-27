using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ExamHelper.Database
{
    /// <summary>
    /// Hệ quản trị CSDL được hỗ trợ để tự động định dạng dấu ngoặc tên cột/bảng và tham số SQL.
    /// </summary>
    public enum DbDialect
    {
        SqlServer,
        Sqlite,
        MySql,
        PostgreSql,
        OleDb,
        Odbc
    }

    /// <summary>
    /// Lớp tiện ích tĩnh hỗ trợ thao tác đa cơ sở dữ liệu (SQL Server, SQLite, MySQL, PostgreSQL, OleDb, Odbc).
    /// Tự động mở và đóng kết nối theo từng truy vấn, hỗ trợ cấu hình qua ConnectionString hoặc App.config,
    /// đọc danh sách bảng / schema động và chống SQL Injection bằng DbParameter.
    /// </summary>
    public static class DbHelper
    {
        private sealed class ConnectionConfig
        {
            public string ConnectionString { get; set; }
            public DbProviderFactory Factory { get; set; }
            public DbDialect Dialect { get; set; }
        }

        private static readonly object _syncLock = new object();
        private static readonly Dictionary<string, ConnectionConfig> _connections =
            new Dictionary<string, ConnectionConfig>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Chuỗi kết nối mặc định ("default"). Chỉ cần gán trực tiếp thuộc tính này để kết nối hoặc đổi CSDL ngay lập tức.
        /// Nếu chưa gán, <see cref="DbHelper"/> sẽ tự tìm chuỗi kết nối đầu tiên trong file App.config.
        /// </summary>
        public static string ConnectionString
        {
            get
            {
                lock (_syncLock)
                {
                    if (_connections.TryGetValue("default", out var cfg))
                        return cfg.ConnectionString;
                }
                var fromCfg = TryReadConfigEntry(null);
                return fromCfg?.ConnectionString;
            }
            set => SetDefaultConnectionString(value);
        }

        /// <summary>
        /// Lỗi gần nhất phát sinh khi thực thi truy vấn cơ sở dữ liệu (null nếu lệnh vừa chạy thành công).
        /// </summary>
        public static Exception LastError { get; set; }

        /// <summary>
        /// Trả về True nếu thao tác DB gần nhất phát sinh lỗi (tránh nhầm bảng rỗng với lỗi SQL).
        /// </summary>
        public static bool HasError => LastError != null;

        /// <summary>
        /// Nếu true, ném lại Exception ngay sau khi xử lý lỗi thay vì trả về DataTable rỗng/0. Mặc định: false.
        /// </summary>
        public static bool ThrowOnError { get; set; } = false;

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

            if (ThrowOnError)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }

        #region Cấu hình kết nối & Phát hiện Provider

        /// <summary>
        /// Đăng ký một chuỗi kết nối với tên chỉ định (tùy chọn truyền DbProviderFactory và DbDialect cho DB khác ngoài SQL Server).
        /// </summary>
        public static void SetConnectionString(string name, string connectionString,
            DbProviderFactory factory = null, DbDialect? dialect = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên kết nối không được để trống.", nameof(name));

            lock (_syncLock)
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    _connections.Remove(name);
                    return;
                }

                DbDialect resolvedDialect = dialect ?? InferDialectFromFactory(factory) ?? DetectDialect(connectionString, null);
                DbProviderFactory resolvedFactory = factory ?? ResolveFactory(resolvedDialect, null);
                _connections[name] = new ConnectionConfig
                {
                    ConnectionString = connectionString,
                    Dialect = resolvedDialect,
                    Factory = resolvedFactory
                };
            }
        }

        /// <summary>
        /// Đăng ký chuỗi kết nối mặc định (khóa "default"). Hỗ trợ tùy chọn DbProviderFactory cho SQLite / MySQL / PostgreSQL / OleDb / Odbc.
        /// </summary>
        public static void SetDefaultConnectionString(string connectionString,
            DbProviderFactory factory = null, DbDialect? dialect = null)
        {
            SetConnectionString("default", connectionString, factory, dialect);
        }

        /// <summary>
        /// Đọc chuỗi kết nối (và providerName nếu có) từ thẻ &lt;connectionStrings&gt; trong App.config và đăng ký vào <see cref="DbHelper"/>.
        /// </summary>
        public static bool SetConnectionStringFromConfig(string configName, string dbName = "default", string configFilePath = null)
        {
            if (string.IsNullOrWhiteSpace(configName))
                throw new ArgumentException("Tên cấu hình kết nối không được để trống.", nameof(configName));

            var cfg = TryReadConfigEntry(configName, configFilePath);
            if (cfg == null) return false;

            lock (_syncLock)
            {
                _connections[dbName] = cfg;
            }
            return true;
        }

        /// <summary>Lấy loại hệ quản trị CSDL (<see cref="DbDialect"/>) đang cấu hình cho tên kết nối.</summary>
        public static DbDialect GetDialect(string dbName = "default")
        {
            return ResolveConfig(dbName).Dialect;
        }

        internal static DbDialect TryGetDialect(string dbName = "default")
        {
            try { return ResolveConfig(dbName).Dialect; }
            catch { return DbDialect.SqlServer; }
        }

        /// <summary>Bọc định danh bảng hoặc cột đúng cú pháp theo từng loại CSDL.</summary>
        public static string QuoteIdentifier(string identifier, string dbName = "default")
        {
            return QuoteIdentifier(identifier, GetDialect(dbName));
        }

        /// <summary>Bọc định danh bảng hoặc cột đúng cú pháp theo <see cref="DbDialect"/>.</summary>
        public static string QuoteIdentifier(string identifier, DbDialect dialect)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return "";

            string[] parts = identifier.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                string clean = parts[i].Trim().Trim('[', ']', '`', '"');
                switch (dialect)
                {
                    case DbDialect.MySql:
                        parts[i] = $"`{clean}`";
                        break;
                    case DbDialect.Sqlite:
                    case DbDialect.PostgreSql:
                        parts[i] = $"\"{clean}\"";
                        break;
                    default:
                        parts[i] = $"[{clean}]";
                        break;
                }
            }
            return string.Join(".", parts);
        }

        /// <summary>Trả về câu SQL lấy ID tự tăng vừa thêm gần nhất theo từng loại CSDL.</summary>
        public static string GetLastInsertIdSql(DbDialect dialect)
        {
            switch (dialect)
            {
                case DbDialect.Sqlite:
                    return "SELECT last_insert_rowid()";
                case DbDialect.MySql:
                    return "SELECT LAST_INSERT_ID()";
                case DbDialect.PostgreSql:
                    return "SELECT LASTVAL()";
                case DbDialect.OleDb:
                case DbDialect.Odbc:
                    return "SELECT @@IDENTITY";
                default:
                    return "SELECT SCOPE_IDENTITY()";
            }
        }

        private static string ResolveAppConfigPath(string configFilePath = null)
        {
            if (!string.IsNullOrWhiteSpace(configFilePath))
                return configFilePath;

#if NETFRAMEWORK
            return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
#else
            string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? "";
            string friendly = AppDomain.CurrentDomain.FriendlyName ?? "";
            string dllCfg = Path.Combine(baseDir, friendly + ".dll.config");
            if (File.Exists(dllCfg)) return dllCfg;

            string exeCfg = Path.Combine(baseDir, friendly + ".exe.config");
            if (File.Exists(exeCfg)) return exeCfg;

            string appCfg = Path.Combine(baseDir, "App.config");
            if (File.Exists(appCfg)) return appCfg;

            return null;
#endif
        }

        private static ConnectionConfig TryReadConfigEntry(string configName, string configFilePath = null)
        {
            string path = ResolveAppConfigPath(configFilePath);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            try
            {
                XDocument doc = XDocument.Load(path);
                var nodes = doc.Root?.Element("connectionStrings")?.Elements("add").ToList();
                if (nodes == null || nodes.Count == 0)
                    return null;

                XElement addNode;
                if (!string.IsNullOrWhiteSpace(configName))
                {
                    addNode = nodes.FirstOrDefault(e =>
                        string.Equals((string)e.Attribute("name"), configName, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    addNode = nodes.FirstOrDefault(e => string.Equals((string)e.Attribute("name"), "default", StringComparison.OrdinalIgnoreCase))
                        ?? nodes.FirstOrDefault(e => !string.Equals((string)e.Attribute("name"), "LocalSqlServer", StringComparison.OrdinalIgnoreCase)
                            && !string.IsNullOrWhiteSpace((string)e.Attribute("connectionString")));
                }

                string connStr = (string)addNode?.Attribute("connectionString");
                if (string.IsNullOrWhiteSpace(connStr)) return null;

                string providerName = (string)addNode?.Attribute("providerName");
                DbDialect dialect = DetectDialect(connStr, providerName);
                DbProviderFactory factory = ResolveFactory(dialect, providerName);

                return new ConnectionConfig
                {
                    ConnectionString = connStr,
                    Dialect = dialect,
                    Factory = factory
                };
            }
            catch (Exception ex)
            {
                LastError = ex;
                return null;
            }
        }

        private static bool LooksLikeConnectionString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return value.Contains("=") && value.Contains(";");
        }

        /// <summary>Tự động nhận diện loại hệ quản trị CSDL từ chuỗi kết nối hoặc tên provider.</summary>
        public static DbDialect DetectDialect(string connStr, string providerName = null)
        {
            string prov = (providerName ?? "").ToLowerInvariant();
            string cs = (connStr ?? "").ToLowerInvariant();

            if (prov.Contains("sqlite")) return DbDialect.Sqlite;
            if (prov.Contains("mysql")) return DbDialect.MySql;
            if (prov.Contains("npgsql") || prov.Contains("postgres")) return DbDialect.PostgreSql;
            if (prov.Contains("oledb")) return DbDialect.OleDb;
            if (prov.Contains("odbc")) return DbDialect.Odbc;
            if (prov.Contains("sqlclient")) return DbDialect.SqlServer;

            if (cs.Contains("provider=")) return DbDialect.OleDb;
            if (cs.Contains("driver=") || cs.Contains("dsn=")) return DbDialect.Odbc;
            if (cs.Contains(".sqlite") || cs.Contains(".db;") || cs.EndsWith(".db", StringComparison.OrdinalIgnoreCase)
                || cs.Contains("mode=memory") || cs.Contains("cache=shared"))
                return DbDialect.Sqlite;
            if (cs.Contains("sslmode=") || cs.Contains("allow user variables=") || (cs.Contains("uid=") && cs.Contains("pwd=")))
                return DbDialect.MySql;
            if (cs.Contains("host=") && (cs.Contains("username=") || cs.Contains("port=5432")))
                return DbDialect.PostgreSql;

            return DbDialect.SqlServer;
        }

        private static DbDialect? InferDialectFromFactory(DbProviderFactory factory)
        {
            if (factory == null) return null;
            string name = factory.GetType().FullName?.ToLowerInvariant() ?? "";
            if (name.Contains("sqlite")) return DbDialect.Sqlite;
            if (name.Contains("mysql")) return DbDialect.MySql;
            if (name.Contains("npgsql")) return DbDialect.PostgreSql;
            if (name.Contains("oledb")) return DbDialect.OleDb;
            if (name.Contains("odbc")) return DbDialect.Odbc;
            if (name.Contains("sqlclient")) return DbDialect.SqlServer;
            return null;
        }

        private static DbProviderFactory ResolveFactory(DbDialect dialect, string providerName)
        {
            if (!string.IsNullOrWhiteSpace(providerName))
            {
                var byProv = FindFactoryByProviderName(providerName.Trim());
                if (byProv != null) return byProv;
            }

            switch (dialect)
            {
                case DbDialect.SqlServer:
                    return SqlClientFactory.Instance;
                case DbDialect.Sqlite:
                    return FindFactoryType("Microsoft.Data.Sqlite.SqliteFactory", "Microsoft.Data.Sqlite")
                        ?? FindFactoryType("System.Data.SQLite.SQLiteFactory", "System.Data.SQLite");
                case DbDialect.MySql:
                    return FindFactoryType("MySqlConnector.MySqlConnectorFactory", "MySqlConnector")
                        ?? FindFactoryType("MySql.Data.MySqlClient.MySqlClientFactory", "MySql.Data");
                case DbDialect.PostgreSql:
                    return FindFactoryType("Npgsql.NpgsqlFactory", "Npgsql");
                case DbDialect.OleDb:
                    return FindFactoryType("System.Data.OleDb.OleDbFactory", "System.Data.OleDb")
                        ?? FindFactoryType("System.Data.OleDb.OleDbFactory", "System.Data");
                case DbDialect.Odbc:
                    return FindFactoryType("System.Data.Odbc.OdbcFactory", "System.Data.Odbc")
                        ?? FindFactoryType("System.Data.Odbc.OdbcFactory", "System.Data");
                default:
                    return SqlClientFactory.Instance;
            }
        }

        private static DbProviderFactory FindFactoryByProviderName(string providerName)
        {
            if (string.Equals(providerName, "System.Data.SqlClient", StringComparison.OrdinalIgnoreCase))
                return SqlClientFactory.Instance;
            if (string.Equals(providerName, "Microsoft.Data.SqlClient", StringComparison.OrdinalIgnoreCase))
                return FindFactoryType("Microsoft.Data.SqlClient.SqlClientFactory", "Microsoft.Data.SqlClient") ?? SqlClientFactory.Instance;

#if NETFRAMEWORK
            try
            {
                return DbProviderFactories.GetFactory(providerName);
            }
            catch { }
#endif
            return null;
        }

        private static DbProviderFactory FindFactoryType(string typeFullName, string assemblyName)
        {
            Type type = Type.GetType($"{typeFullName}, {assemblyName}", throwOnError: false);
            if (type == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(typeFullName, throwOnError: false);
                    if (type != null) break;
                }
            }
            if (type == null) return null;

            var instField = type.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instField != null && instField.GetValue(null) is DbProviderFactory f1)
                return f1;

            var instProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instProp != null && instProp.GetValue(null) is DbProviderFactory f2)
                return f2;

            return null;
        }

        private static ConnectionConfig ResolveConfig(string dbName = "default")
        {
            string key = string.IsNullOrWhiteSpace(dbName) ? "default" : dbName;

            lock (_syncLock)
            {
                if (_connections.TryGetValue(key, out var found))
                {
                    if (found.Factory == null)
                        found.Factory = ResolveFactory(found.Dialect, null);
                    return found;
                }
            }

            if (LooksLikeConnectionString(key))
            {
                DbDialect dialect = DetectDialect(key, null);
                return new ConnectionConfig
                {
                    ConnectionString = key,
                    Dialect = dialect,
                    Factory = ResolveFactory(dialect, null)
                };
            }

            var fromCfg = TryReadConfigEntry(
                string.Equals(key, "default", StringComparison.OrdinalIgnoreCase) ? null : key);
            if (fromCfg != null)
            {
                return fromCfg;
            }

            throw new KeyNotFoundException(
                $"Chuỗi kết nối \"{key}\" chưa được cấu hình. " +
                $"Hãy gán DbHelper.ConnectionString = \"...\" hoặc khai báo trong App.config.");
        }

        private static DbProviderFactory EnsureFactory(ConnectionConfig cfg)
        {
            if (cfg.Factory != null) return cfg.Factory;

            string hint;
            switch (cfg.Dialect)
            {
                case DbDialect.Sqlite: hint = "Microsoft.Data.Sqlite hoặc System.Data.SQLite"; break;
                case DbDialect.MySql: hint = "MySqlConnector hoặc MySql.Data"; break;
                case DbDialect.PostgreSql: hint = "Npgsql"; break;
                case DbDialect.OleDb: hint = "System.Data.OleDb"; break;
                case DbDialect.Odbc: hint = "System.Data.Odbc"; break;
                default: hint = "System.Data.SqlClient"; break;
            }

            throw new InvalidOperationException(
                $"Đã phát hiện CSDL loại {cfg.Dialect}, nhưng project chưa tham chiếu driver {hint} " +
                $"(hoặc hãy truyền DbProviderFactory vào DbHelper.SetConnectionString(..., factory)).");
        }

        /// <summary>
        /// Kiểm tra xem chuỗi kết nối đã đăng ký (hoặc truyền trực tiếp) có kết nối thành công tới CSDL hay không.
        /// Áp dụng timeout ngắn (mặc định 3 giây) để không làm treo Form nếu sai tên server.
        /// </summary>
        public static bool TestConnection(string dbName = "default", bool showError = false, int timeoutSeconds = 3)
        {
            LastError = null;
            try
            {
                ConnectionConfig cfg = ResolveConfig(dbName);
                DbProviderFactory factory = EnsureFactory(cfg);
                int effectiveTimeout = timeoutSeconds > 0 ? timeoutSeconds : 3;
                string connStr = cfg.ConnectionString;

                if (cfg.Dialect == DbDialect.SqlServer)
                {
                    var builder = new SqlConnectionStringBuilder(connStr)
                    {
                        ConnectTimeout = effectiveTimeout
                    };
                    connStr = builder.ConnectionString;
                }

                Exception openEx = null;
                bool opened = false;
                var openTask = Task.Run(() =>
                {
                    try
                    {
                        using (DbConnection conn = factory.CreateConnection())
                        {
                            conn.ConnectionString = connStr;
                            conn.Open();
                            opened = conn.State == ConnectionState.Open;
                        }
                    }
                    catch (Exception ex)
                    {
                        openEx = ex;
                    }
                });

                if (!openTask.Wait(TimeSpan.FromSeconds(effectiveTimeout)))
                {
                    throw new TimeoutException($"Quá thời gian chờ kết nối ({effectiveTimeout} giây).");
                }

                if (openEx != null)
                {
                    ExceptionDispatchInfo.Capture(openEx).Throw();
                }

                return opened;
            }
            catch (Exception ex)
            {
                LastError = ex;
                if (showError)
                {
                    bool savedThrow = ThrowOnError;
                    try
                    {
                        ThrowOnError = false;
                        HandleError("Không thể kết nối cơ sở dữ liệu", "TEST CONNECTION", ex);
                    }
                    finally
                    {
                        ThrowOnError = savedThrow;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Tạo và mở một <see cref="SqlConnection"/> đến SQL Server (dùng cho code chuyên biệt SQL Server).
        /// Với đa CSDL hãy dùng <see cref="OpenConnection"/>.
        /// </summary>
        public static SqlConnection GetConnection(string dbName = "default")
        {
            ConnectionConfig cfg = ResolveConfig(dbName);
            if (cfg.Dialect != DbDialect.SqlServer)
            {
                throw new InvalidOperationException(
                    $"Kết nối \"{dbName}\" là loại {cfg.Dialect}, không phải SqlConnection. Hãy dùng DbHelper.OpenConnection().");
            }

            SqlConnection connection = new SqlConnection(cfg.ConnectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Tạo và mở kết nối tổng quát (<see cref="DbConnection"/>) cho bất kỳ loại CSDL nào đã cấu hình.
        /// </summary>
        public static DbConnection OpenConnection(string dbName = "default")
        {
            ConnectionConfig cfg = ResolveConfig(dbName);
            DbProviderFactory factory = EnsureFactory(cfg);
            DbConnection conn = factory.CreateConnection()
                ?? throw new InvalidOperationException("Provider factory không tạo được DbConnection.");
            conn.ConnectionString = cfg.ConnectionString;
            conn.Open();
            return conn;
        }

        #endregion

        #region Đọc Schema / Danh sách bảng tự động

        /// <summary>
        /// Lấy danh sách tên các bảng dữ liệu trong CSDL đang kết nối (tự bỏ qua bảng hệ thống).
        /// </summary>
        public static List<string> GetTableNames(string dbName = "default")
        {
            LastError = null;
            var result = new List<string>();
            try
            {
                ConnectionConfig cfg = ResolveConfig(dbName);
                using (DbConnection conn = OpenConnection(dbName))
                {
                    if (cfg.Dialect == DbDialect.SqlServer)
                    {
                        using (DbCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME <> 'sysdiagrams' ORDER BY TABLE_SCHEMA, TABLE_NAME";
                            using (DbDataReader r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string schema = r.IsDBNull(0) ? "dbo" : r.GetString(0);
                                    string name = r.GetString(1);
                                    result.Add(string.Equals(schema, "dbo", StringComparison.OrdinalIgnoreCase) ? name : $"{schema}.{name}");
                                }
                            }
                        }
                        return result;
                    }

                    if (cfg.Dialect == DbDialect.Sqlite)
                    {
                        using (DbCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
                            using (DbDataReader r = cmd.ExecuteReader())
                            {
                                while (r.Read()) result.Add(r.GetString(0));
                            }
                        }
                        return result;
                    }

                    DataTable schemaTables = conn.GetSchema("Tables");
                    foreach (DataRow row in schemaTables.Rows)
                    {
                        string tableType = schemaTables.Columns.Contains("TABLE_TYPE") ? row["TABLE_TYPE"]?.ToString() : "TABLE";
                        if (string.Equals(tableType, "VIEW", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(tableType, "SYSTEM TABLE", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string tableName = schemaTables.Columns.Contains("TABLE_NAME") ? row["TABLE_NAME"]?.ToString() : null;
                        if (!string.IsNullOrWhiteSpace(tableName) &&
                            !tableName.StartsWith("MSys", StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(tableName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("Lỗi khi đọc danh sách bảng CSDL", "GET TABLES", ex);
            }
            return result;
        }

        /// <summary>
        /// Đọc cấu trúc cột (DataType, AllowDBNull, MaxLength, AutoIncrement, PrimaryKey) của bất kỳ bảng nào từ CSDL.
        /// Hoạt động trên mọi loại DbConnection.
        /// </summary>
        public static DataTable GetTableSchema(string tableName, string dbName = "default")
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Tên bảng không được để trống.", nameof(tableName));

            LastError = null;
            DataTable dt = new DataTable(tableName);
            try
            {
                ConnectionConfig cfg = ResolveConfig(dbName);
                DbProviderFactory factory = EnsureFactory(cfg);
                string quoted = QuoteIdentifier(tableName, cfg.Dialect);
                string sql = $"SELECT * FROM {quoted} WHERE 1 = 0";

                using (DbConnection conn = factory.CreateConnection())
                {
                    conn.ConnectionString = cfg.ConnectionString;
                    conn.Open();

                    using (DbCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        DbDataAdapter adapter = factory.CreateDataAdapter();
                        if (adapter != null)
                        {
                            using (adapter)
                            {
                                adapter.SelectCommand = cmd;
                                adapter.FillSchema(dt, SchemaType.Source);
                            }
                        }
                        else
                        {
                            using (DbDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo))
                            {
                                dt.Load(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError($"Lỗi khi đọc cấu trúc bảng '{tableName}'", tableName, ex);
            }
            return dt;
        }

        #endregion

        #region Command & Parameter Binding Nội bộ

        private static DbCommand PrepareCommand(DbConnection conn, DbDialect dialect, string sql, DbParameter[] parameters)
        {
            DbCommand cmd = conn.CreateCommand();
            if (parameters == null || parameters.Length == 0)
            {
                cmd.CommandText = sql;
                return cmd;
            }

            if (dialect == DbDialect.OleDb || dialect == DbDialect.Odbc)
            {
                var lookup = new Dictionary<string, DbParameter>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in parameters)
                {
                    if (p == null) continue;
                    string key = p.ParameterName?.Trim().TrimStart('@') ?? "";
                    lookup[key] = p;
                }

                // Thay @param thành ? đúng thứ tự xuất hiện (bỏ qua phần bên trong dấu nháy đơn '...')
                var rewritten = new StringBuilder();
                int i = 0;
                while (i < sql.Length)
                {
                    char ch = sql[i];
                    if (ch == '\'')
                    {
                        rewritten.Append(ch);
                        i++;
                        while (i < sql.Length)
                        {
                            rewritten.Append(sql[i]);
                            if (sql[i] == '\'')
                            {
                                if (i + 1 < sql.Length && sql[i + 1] == '\'')
                                {
                                    i++;
                                    rewritten.Append(sql[i]);
                                }
                                else
                                {
                                    i++;
                                    break;
                                }
                            }
                            i++;
                        }
                        continue;
                    }

                    if (ch == '@' && i + 1 < sql.Length && (char.IsLetterOrDigit(sql[i + 1]) || sql[i + 1] == '_'))
                    {
                        int start = i + 1;
                        int end = start;
                        while (end < sql.Length && (char.IsLetterOrDigit(sql[end]) || sql[end] == '_'))
                            end++;
                        string token = sql.Substring(start, end - start);
                        if (lookup.TryGetValue(token, out var matched))
                        {
                            rewritten.Append('?');
                            DbParameter newP = cmd.CreateParameter();
                            newP.ParameterName = token;
                            newP.Value = matched.Value ?? DBNull.Value;
                            cmd.Parameters.Add(newP);
                            i = end;
                            continue;
                        }
                    }

                    rewritten.Append(ch);
                    i++;
                }

                cmd.CommandText = rewritten.ToString();
                return cmd;
            }

            cmd.CommandText = sql;
            foreach (var p in parameters)
            {
                if (p == null) continue;
                DbParameter cloned = cmd.CreateParameter();
                cloned.ParameterName = p.ParameterName;
                cloned.Value = p.Value ?? DBNull.Value;
                cmd.Parameters.Add(cloned);
            }
            return cmd;
        }

        private static T ExecuteWithCommand<T>(
            string sql,
            string dbName,
            DbParameter[] parameters,
            string errorTitle,
            T fallbackValue,
            Func<DbCommand, DbProviderFactory, T> executor)
        {
            LastError = null;
            try
            {
                ConnectionConfig cfg = ResolveConfig(dbName);
                DbProviderFactory factory = EnsureFactory(cfg);

                using (DbConnection conn = factory.CreateConnection())
                {
                    conn.ConnectionString = cfg.ConnectionString;
                    conn.Open();

                    using (DbCommand cmd = PrepareCommand(conn, cfg.Dialect, sql, parameters))
                    {
                        return executor(cmd, factory);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(errorTitle, sql, ex);
                return fallbackValue;
            }
        }

        #endregion

        #region GetDataTable & GetDataRow

        /// <summary>Thực thi SQL query (đa CSDL) và trả về DataTable. Tự mở và đóng kết nối.</summary>
        public static DataTable GetDataTable(string sql, params DbParameter[] parameters)
        {
            return GetDataTable(sql, "default", parameters);
        }

        /// <summary>Thực thi SQL query theo tên kết nối hoặc connectionString trực tiếp và trả về DataTable.</summary>
        public static DataTable GetDataTable(string sql, string dbName, params DbParameter[] parameters)
        {
            DataTable dt = new DataTable();
            ExecuteWithCommand(sql, dbName, parameters, "Lỗi khi truy vấn dữ liệu", dt, (cmd, factory) =>
            {
                DbDataAdapter adapter = factory.CreateDataAdapter();
                if (adapter != null)
                {
                    using (adapter)
                    {
                        adapter.SelectCommand = cmd;
                        adapter.Fill(dt);
                    }
                }
                else
                {
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                }
                return dt;
            });
            return dt;
        }

        /// <summary>Thực thi SELECT kết hợp FilterBuilder và ORDER BY (tùy chọn).</summary>
        public static DataTable GetDataTable(string sql, FilterBuilder filter, string orderBy = null, string dbName = "default")
        {
            string fullSql = sql ?? "";
            DbParameter[] parms = null;

            if (filter != null)
            {
                var built = filter.Build();
                fullSql += built.WhereClause;
                parms = built.Parameters;
            }

            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                string trimmedOrder = orderBy.Trim();
                fullSql += trimmedOrder.StartsWith("ORDER BY", StringComparison.OrdinalIgnoreCase)
                    ? " " + trimmedOrder
                    : " ORDER BY " + trimmedOrder;
            }

            return GetDataTable(fullSql, dbName, parms ?? Array.Empty<DbParameter>());
        }

        /// <summary>Thực thi câu truy vấn và trả về dòng đầu tiên (<see cref="DataRow"/>) hoặc null.</summary>
        public static DataRow GetDataRow(string sql, params DbParameter[] parameters)
        {
            return GetDataRow(sql, "default", parameters);
        }

        /// <summary>Thực thi câu truy vấn theo kết nối chỉ định và trả về dòng đầu tiên (<see cref="DataRow"/>).</summary>
        public static DataRow GetDataRow(string sql, string dbName, params DbParameter[] parameters)
        {
            DataTable dt = GetDataTable(sql, dbName, parameters);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        #endregion

        #region ExecuteNonQuery & Transaction

        /// <summary>Thực thi câu lệnh SQL không trả về bảng (INSERT, UPDATE, DELETE).</summary>
        public static int ExecuteNonQuery(string sql, params DbParameter[] parameters)
        {
            return ExecuteNonQuery(sql, "default", parameters);
        }

        /// <summary>Thực thi câu lệnh SQL (INSERT, UPDATE, DELETE) trên kết nối chỉ định.</summary>
        public static int ExecuteNonQuery(string sql, string dbName, params DbParameter[] parameters)
        {
            return ExecuteWithCommand(sql, dbName, parameters, "Lỗi khi thực thi câu lệnh SQL", 0,
                (cmd, _) => cmd.ExecuteNonQuery());
        }

        /// <summary>Thực thi câu lệnh SQL không trả về kết quả trong một SqlTransaction hiện hành.</summary>
        public static int ExecuteNonQuery(string sql, SqlTransaction transaction, params SqlParameter[] parameters)
        {
            return ExecuteNonQuery(sql, (DbTransaction)transaction, parameters);
        }

        /// <summary>Thực thi câu lệnh SQL không trả về kết quả trong một DbTransaction (đa CSDL) hiện hành.</summary>
        public static int ExecuteNonQuery(string sql, DbTransaction transaction, params DbParameter[] parameters)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction), "Transaction không được null.");

            DbDialect dialect = transaction is SqlTransaction ? DbDialect.SqlServer : DetectDialect(transaction.Connection?.ConnectionString, transaction.Connection?.GetType().FullName);
            using (DbCommand cmd = PrepareCommand(transaction.Connection, dialect, sql, parameters))
            {
                cmd.Transaction = transaction;
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Thực thi câu lệnh SQL trả về giá trị đơn (scalar) trong một SqlTransaction hiện hành.</summary>
        public static T ExecuteScalar<T>(string sql, SqlTransaction transaction, params SqlParameter[] parameters)
        {
            return ExecuteScalar<T>(sql, (DbTransaction)transaction, parameters);
        }

        /// <summary>Thực thi câu lệnh SQL trả về giá trị đơn (scalar) trong một DbTransaction (đa CSDL) hiện hành.</summary>
        public static T ExecuteScalar<T>(string sql, DbTransaction transaction, params DbParameter[] parameters)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction), "Transaction không được null.");

            DbDialect dialect = transaction is SqlTransaction ? DbDialect.SqlServer : DetectDialect(transaction.Connection?.ConnectionString, transaction.Connection?.GetType().FullName);
            using (DbCommand cmd = PrepareCommand(transaction.Connection, dialect, sql, parameters))
            {
                cmd.Transaction = transaction;
                object result = cmd.ExecuteScalar();
                return ExamHelper.Utilities.DataHelper.ChangeType<T>(result);
            }
        }

        private static bool RunTransaction<TConn, TTx>(
            Func<TConn> openConn,
            Func<TConn, TTx> beginTx,
            Action<TTx> action)
            where TConn : DbConnection
            where TTx : DbTransaction
        {
            LastError = null;
            TConn connection = null;
            TTx transaction = null;

            try
            {
                connection = openConn();
                transaction = beginTx(connection);
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

        /// <summary>Thực thi chuỗi thao tác SQL Server trong SqlTransaction (tự Commit / Rollback).</summary>
        public static bool ExecuteTransaction(Action<SqlTransaction> action, string dbName = "default")
        {
            return RunTransaction(() => GetConnection(dbName), c => c.BeginTransaction(), action);
        }

        /// <summary>Thực thi chuỗi thao tác CSDL đa nhà cung cấp bên trong DbTransaction (tự Commit / Rollback).</summary>
        public static bool ExecuteDbTransaction(Action<DbTransaction> action, string dbName = "default")
        {
            return RunTransaction(() => OpenConnection(dbName), c => c.BeginTransaction(), action);
        }

        #endregion

        #region ExecuteScalar & Exists

        /// <summary>Thực thi câu truy vấn và trả về giá trị đơn kiểu T (đa CSDL).</summary>
        public static T ExecuteScalar<T>(string sql, params DbParameter[] parameters)
        {
            return ExecuteScalar<T>(sql, "default", parameters);
        }

        /// <summary>Thực thi câu truy vấn theo tên kết nối/connectionString và trả về giá trị đơn kiểu T.</summary>
        public static T ExecuteScalar<T>(string sql, string dbName, params DbParameter[] parameters)
        {
            return ExecuteWithCommand(sql, dbName, parameters, "Lỗi khi thực thi truy vấn scalar", default(T),
                (cmd, _) => ExamHelper.Utilities.DataHelper.ChangeType<T>(cmd.ExecuteScalar()));
        }

        /// <summary>Kiểm tra câu truy vấn có trả về ít nhất 1 dòng hay không.</summary>
        public static bool Exists(string sql, params DbParameter[] parameters)
        {
            return Exists(sql, "default", parameters);
        }

        /// <summary>Kiểm tra câu truy vấn trên kết nối chỉ định có trả về ít nhất 1 dòng hay không.</summary>
        public static bool Exists(string sql, string dbName, params DbParameter[] parameters)
        {
            return ExecuteWithCommand(sql, dbName, parameters, "Lỗi khi kiểm tra sự tồn tại dữ liệu", false, (cmd, _) =>
            {
                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    return reader.HasRows;
                }
            });
        }

        #endregion

        #region Tạo tham số nhanh

        private static string NormalizeParamName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên tham số SQL không được để trống.", nameof(name));
            string trimmed = name.Trim();
            return trimmed.StartsWith("@") ? trimmed : "@" + trimmed;
        }

        /// <summary>Tạo nhanh SqlParameter (tương thích mọi DbParameter trong DbHelper). Tự động đổi null → DBNull.Value.</summary>
        public static SqlParameter Param(string name, object value)
        {
            return new SqlParameter(NormalizeParamName(name), value ?? DBNull.Value);
        }

        /// <summary>Tạo nhanh SqlParameter cho DateTime?.</summary>
        public static SqlParameter ParamDate(string name, DateTime? value)
        {
            return new SqlParameter(NormalizeParamName(name), (object)value ?? DBNull.Value);
        }

        /// <summary>Tạo nhanh SqlParameter cho int?.</summary>
        public static SqlParameter ParamInt(string name, int? value)
        {
            return new SqlParameter(NormalizeParamName(name), (object)value ?? DBNull.Value);
        }

        /// <summary>Tạo nhanh SqlParameter cho decimal?.</summary>
        public static SqlParameter ParamDecimal(string name, decimal? value)
        {
            return new SqlParameter(NormalizeParamName(name), (object)value ?? DBNull.Value);
        }

        /// <summary>Tạo nhanh SqlParameter bọc % cho LIKE.</summary>
        public static SqlParameter ParamLike(string name, string value)
        {
            return new SqlParameter(NormalizeParamName(name), $"%{value ?? string.Empty}%");
        }

        #endregion
    }
}
