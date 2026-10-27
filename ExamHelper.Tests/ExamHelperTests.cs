using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using ExamHelper.Binding;
using ExamHelper.Database;
using ExamHelper.Utilities;
using ExamHelper.Validation;
using Xunit;

namespace ExamHelper.Tests
{
    public class ExamHelperTests
    {
        public ExamHelperTests()
        {
            DbHelper.ShowErrorDialog = false;
        }

        #region 1. Kiểm thử FilterBuilder

        [Fact]
        public void FilterBuilder_Empty_ReturnsEmptyWhereClause()
        {
            var filter = FilterBuilder.New();
            var (where, parameters) = filter.Build();

            Assert.Equal("", where);
            Assert.Empty(parameters);
            Assert.False(filter.HasConditions);
        }

        [Fact]
        public void FilterBuilder_SingleEqual_BuildsCorrectClause()
        {
            var (where, parms) = FilterBuilder.New()
                .Equal("MaPhong", "IT")
                .Build();

            Assert.Equal(" WHERE [MaPhong] = @f0", where);
            Assert.Single(parms);
            Assert.Equal("@f0", parms[0].ParameterName);
            Assert.Equal("IT", parms[0].Value);
        }

        [Fact]
        public void FilterBuilder_SkipsEmptyAndNullValues()
        {
            var (where, parms) = FilterBuilder.New()
                .Equal("MaPhong", "")
                .Equal("MaNV", null)
                .Contains("HoTen", "   ")
                .Build();

            Assert.Equal("", where);
            Assert.Empty(parms);
        }

        [Fact]
        public void FilterBuilder_MultipleConditions_AndChained()
        {
            var (where, parms) = FilterBuilder.New()
                .Equal("MaPhong", "IT")
                .Contains("HoTen", "Nguyen")
                .GreaterThanOrEqual("Luong", 5000000m)
                .Build();

            Assert.Equal(" WHERE [MaPhong] = @f0 AND [HoTen] LIKE @f1 AND [Luong] >= @f2", where);
            Assert.Equal(3, parms.Length);
            Assert.Equal("%Nguyen%", parms[1].Value);
            Assert.Equal(5000000m, parms[2].Value);
        }

        [Fact]
        public void FilterBuilder_Between_BothValues_GeneratesBetweenClause()
        {
            var (where, parms) = FilterBuilder.New()
                .Between("Tuoi", 18, 60)
                .Build();

            Assert.Equal(" WHERE [Tuoi] BETWEEN @f0 AND @f1", where);
            Assert.Equal(2, parms.Length);
            Assert.Equal(18, parms[0].Value);
            Assert.Equal(60, parms[1].Value);
        }

        [Fact]
        public void FilterBuilder_In_GeneratesInClause()
        {
            var (where, parms) = FilterBuilder.New()
                .In("TrangThai", 1, 2, 3)
                .Build();

            Assert.Equal(" WHERE [TrangThai] IN (@f0, @f1, @f2)", where);
            Assert.Equal(3, parms.Length);
        }

        [Fact]
        public void FilterBuilder_SkipIfAllSelected_SkipsWhenIndexIsZero()
        {
            var combo = new ComboBox();
            combo.Items.Add("-- Tất cả --");
            combo.Items.Add("Phòng IT");
            combo.SelectedIndex = 0;

            var (where, parms) = FilterBuilder.New()
                .SkipIfAllSelected(combo, "MaPhong")
                .Build();

            Assert.Equal("", where);
            Assert.Empty(parms);
        }

        [Fact]
        public void FilterBuilder_ZeroValue_KeptByDefault_AndSkippedWhenRequested()
        {
            // Giá trị 0 hợp lệ (ví dụ: GioiTinh = 0 (Nữ), TrangThai = 0) không bị bỏ qua
            var (where1, parms1) = FilterBuilder.New()
                .Equal("GioiTinh", 0)
                .Build();

            Assert.Equal(" WHERE [GioiTinh] = @f0", where1);
            Assert.Single(parms1);
            Assert.Equal(0, parms1[0].Value);

            // Khi yêu cầu skipZero = true hoặc dùng SkipIfZero -> bỏ qua 0
            var (where2, parms2) = FilterBuilder.New()
                .Equal("TrangThai", 0, skipZero: true)
                .SkipIfZero("MaPB", 0)
                .Build();

            Assert.Equal("", where2);
            Assert.Empty(parms2);
        }

        [Fact]
        public void FilterBuilder_OrGroup_BuildsCorrectClause()
        {
            var (where, parms) = FilterBuilder.New()
                .Or(sub => sub
                    .Equal("TrangThai", 1)
                    .Equal("TrangThai", 2))
                .Build();

            Assert.Equal(" WHERE ([TrangThai] = @f0 OR [TrangThai] = @f1)", where);
            Assert.Equal(2, parms.Length);
            Assert.Equal(1, parms[0].Value);
            Assert.Equal(2, parms[1].Value);
        }

        [Fact]
        public void FilterBuilder_ContainsAny_BuildsMultiColumnOr()
        {
            var (where, parms) = FilterBuilder.New()
                .ContainsAny(new[] { "MaNV", "HoTen" }, "An")
                .Build();

            Assert.Equal(" WHERE ([MaNV] LIKE @f0 OR [HoTen] LIKE @f0)", where);
            Assert.Single(parms);
            Assert.Equal("%An%", parms[0].Value);
        }

        [Fact]
        public void FilterBuilder_CombinedAndOr_BuildsCorrectStructure()
        {
            var (where, parms) = FilterBuilder.New()
                .ContainsAny(new[] { "MaNV", "HoTen" }, "Nguyen")
                .Equal("MaPhong", "IT")
                .Build();

            Assert.Equal(" WHERE ([MaNV] LIKE @f0 OR [HoTen] LIKE @f0) AND [MaPhong] = @f1", where);
            Assert.Equal(2, parms.Length);
            Assert.Equal("%Nguyen%", parms[0].Value);
            Assert.Equal("IT", parms[1].Value);
        }

        #endregion

        #region 2. Kiểm thử Validator

        [Fact]
        public void Validator_CalculateAge_AccurateYearAndBirthdayCheck()
        {
            // Sinh 2000-05-15, tại 2020-05-14 (chưa tới sinh nhật 2020) -> 19 tuổi
            int ageBefore = Validator.CalculateAge(new DateTime(2000, 5, 15), new DateTime(2020, 5, 14));
            Assert.Equal(19, ageBefore);

            // Tại 2020-05-15 (đúng ngày sinh nhật) -> 20 tuổi
            int ageOnDay = Validator.CalculateAge(new DateTime(2000, 5, 15), new DateTime(2020, 5, 15));
            Assert.Equal(20, ageOnDay);

            // Tại 2020-05-16 (đã qua sinh nhật) -> 20 tuổi
            int ageAfter = Validator.CalculateAge(new DateTime(2000, 5, 15), new DateTime(2020, 5, 16));
            Assert.Equal(20, ageAfter);
        }

        [Fact]
        public void Validator_Required_FailsWhenEmpty()
        {
            var txt = new TextBox { Text = "   " };
            var result = Validator.ValidateAll(Validator.Required(txt, "Mã NV"));

            Assert.False(result.IsValid);
            Assert.Contains("không được để trống", result.ErrorMessage);
            Assert.Same(txt, result.ErrorControl);
        }

        [Fact]
        public void Validator_Pattern_RegexCheck()
        {
            var txt = new TextBox { Text = "123AB" };
            // Mã NV phải bắt đầu bằng NV theo sau 3 số
            var result = Validator.ValidateAll(Validator.Pattern(txt, "Mã NV", @"^NV\d{3}$"));

            Assert.False(result.IsValid);

            txt.Text = "NV001";
            var resultSuccess = Validator.ValidateAll(Validator.Pattern(txt, "Mã NV", @"^NV\d{3}$"));
            Assert.True(resultSuccess.IsValid);
        }

        [Fact]
        public void Validator_AgeRangeAtDate_SpecificHireDateTest()
        {
            // Nghiệp vụ đề thi: Tuổi được tính tại ngày vào làm (18 - 60)
            var dtpBirth = new DateTimePicker { Value = new DateTime(2005, 1, 1) };
            DateTime hireDate = new DateTime(2020, 1, 1); // 15 tuổi tại ngày vào làm -> Không hợp lệ (< 18)

            var result = Validator.ValidateAll(hireDate,
                Validator.AgeRangeAtDate(dtpBirth, "Ngày sinh", 18, 60));

            Assert.False(result.IsValid);
            Assert.Contains("phải từ 18 đến 60", result.ErrorMessage);

            // Sửa lại ngày sinh thành 2000 -> 20 tuổi tại ngày vào làm -> Hợp lệ
            dtpBirth.Value = new DateTime(2000, 1, 1);
            var resultSuccess = Validator.ValidateAll(hireDate,
                Validator.AgeRangeAtDate(dtpBirth, "Ngày sinh", 18, 60));

            Assert.True(resultSuccess.IsValid);
        }

        [Fact]
        public void Validator_DecimalRange_SalaryCoefficientCheck()
        {
            // Đề thi: Hệ số lương từ 1.0 đến 15.0
            var txtHeSo = new TextBox { Text = "0.8" };
            var result = Validator.ValidateAll(Validator.DecimalRange(txtHeSo, "Hệ số lương", 1.0m, 15.0m));
            Assert.False(result.IsValid);

            txtHeSo.Text = "2.34";
            var resultSuccess = Validator.ValidateAll(Validator.DecimalRange(txtHeSo, "Hệ số lương", 1.0m, 15.0m));
            Assert.True(resultSuccess.IsValid);
        }

        [Fact]
        public void Validator_Custom_Rule()
        {
            var txt = new TextBox { Text = "test" };
            var result = Validator.ValidateAll(
                Validator.Custom(txt, "Trường tùy chọn", val => val.StartsWith("A"), "Phải bắt đầu bằng chữ A"));

            Assert.False(result.IsValid);
            Assert.Equal("Phải bắt đầu bằng chữ A", result.ErrorMessage);
        }

        #endregion

        #region 3. Kiểm thử DataHelper (Thống kê và tính toán)

        private DataTable CreateSampleDataTable()
        {
            var dt = new DataTable("NhanVien");
            dt.Columns.Add("MaNV", typeof(string));
            dt.Columns.Add("HoTen", typeof(string));
            dt.Columns.Add("MaPhong", typeof(string));
            dt.Columns.Add("Luong", typeof(decimal));
            dt.Columns.Add("HeSoLuong", typeof(double));

            dt.Rows.Add("NV01", "Nguyễn Văn A", "IT", 10000000m, 2.5);
            dt.Rows.Add("NV02", "Trần Thị B", "IT", 12000000m, 3.0);
            dt.Rows.Add("NV03", "Lê Văn C", "HR", 8000000m, 2.0);
            return dt;
        }

        [Fact]
        public void DataHelper_Count_CalculatesCorrectly()
        {
            var dt = CreateSampleDataTable();
            Assert.Equal(3, DataHelper.Count(dt));
            Assert.Equal(2, DataHelper.Count(dt, "MaPhong = 'IT'"));
            Assert.Equal(1, DataHelper.Count(dt, "Luong > 11000000"));
        }

        [Fact]
        public void DataHelper_Sum_CalculatesCorrectly()
        {
            var dt = CreateSampleDataTable();
            Assert.Equal(30000000m, DataHelper.Sum(dt, "Luong"));
            Assert.Equal(22000000m, DataHelper.Sum(dt, "Luong", "MaPhong = 'IT'"));
        }

        [Fact]
        public void DataHelper_Average_CalculatesCorrectly()
        {
            var dt = CreateSampleDataTable();
            Assert.Equal(10000000m, DataHelper.Average(dt, "Luong"));
            Assert.Equal(11000000m, DataHelper.Average(dt, "Luong", "MaPhong = 'IT'"));
        }

        [Fact]
        public void DataHelper_Max_Min_CalculatesCorrectly()
        {
            var dt = CreateSampleDataTable();
            Assert.Equal(12000000m, DataHelper.Max<decimal>(dt, "Luong"));
            Assert.Equal(8000000m, DataHelper.Min<decimal>(dt, "Luong"));
        }

        [Fact]
        public void DataHelper_EmptyTable_ReturnsDefaultValuesGracefully()
        {
            var dt = new DataTable();
            dt.Columns.Add("Luong", typeof(decimal));

            Assert.Equal(0, DataHelper.Count(dt));
            Assert.Equal(0m, DataHelper.Sum(dt, "Luong"));
            Assert.Equal(0m, DataHelper.Average(dt, "Luong"));
            Assert.Equal(0m, DataHelper.Max<decimal>(dt, "Luong"));
            Assert.Equal(0m, DataHelper.Min<decimal>(dt, "Luong"));
        }

        [Fact]
        public void DataHelper_TypeConversions_SafeWithDBNull()
        {
            Assert.Equal(100m, DataHelper.ToDecimal("100"));
            Assert.Equal(0m, DataHelper.ToDecimal(DBNull.Value));
            Assert.Equal(50m, DataHelper.ToDecimal(null, 50m));
            Assert.Equal(123, DataHelper.ToInt("123"));
            Assert.Equal(0, DataHelper.ToInt(DBNull.Value));
            Assert.Null(DataHelper.ToDateTime(DBNull.Value));
            Assert.Equal("", DataHelper.ToString(DBNull.Value));
        }

        #endregion

        #region 4. Kiểm thử FormMapper

        [Fact]
        public void FormMapper_GetValues_ReadsControlsProperly()
        {
            var txtMa = new TextBox { Text = "NV01" };
            var txtTen = new TextBox { Text = "  Nguyễn A  " };
            var nudLuong = new NumericUpDown { Minimum = 0, Maximum = 100000000, Value = 5000000 };
            var chkGioiTinh = new CheckBox { Checked = true };

            var dict = FormMapper.GetValues(
                FormMapper.Map(txtMa, "MaNV"),
                FormMapper.Map(txtTen, "HoTen"),
                FormMapper.Map(nudLuong, "Luong"),
                FormMapper.Map(chkGioiTinh, "GioiTinh")
            );

            Assert.Equal("NV01", dict["MaNV"]);
            Assert.Equal("Nguyễn A", dict["HoTen"]); // Đã trim khoảng trắng
            Assert.Equal(5000000m, dict["Luong"]);
            Assert.Equal(true, dict["GioiTinh"]);
        }

        [Fact]
        public void FormMapper_LoadRow_PopulatesControlsSafely()
        {
            var dt = CreateSampleDataTable();
            DataRow row = dt.Rows[0]; // NV01, Nguyễn Văn A, IT, 10000000

            var txtMa = new TextBox();
            var txtTen = new TextBox();

            FormMapper.LoadRow(row,
                FormMapper.Map(txtMa, "MaNV"),
                FormMapper.Map(txtTen, "HoTen")
            );

            Assert.Equal("NV01", txtMa.Text);
            Assert.Equal("Nguyễn Văn A", txtTen.Text);
        }

        [Fact]
        public void FormMapper_ClearControls_ResetsState()
        {
            var txtMa = new TextBox { Text = "NV01" };
            var chk = new CheckBox { Checked = true };

            FormMapper.ClearControls(txtMa, chk);

            Assert.Equal("", txtMa.Text);
            Assert.False(chk.Checked);
        }

        #endregion

        #region 5. Kiểm thử DbHelper Connection Manager & Params

        [Fact]
        public void DbHelper_MultiDatabaseConnectionRegistration()
        {
            DbHelper.SetDefaultConnectionString("Server=127.0.0.1;Database=DbNhanVien;User Id=sa;Password=123;");
            DbHelper.SetConnectionString("BookDb", "Server=127.0.0.1;Database=DbSach;User Id=sa;Password=123;");
            DbHelper.SetConnectionString("StudentDb", "Server=127.0.0.1;Database=DbHocSinh;User Id=sa;Password=123;");

            // Kiểm tra Param helpers
            var p1 = DbHelper.Param("MaNV", null);
            Assert.Equal(DBNull.Value, p1.Value);

            var p2 = DbHelper.ParamLike("HoTen", "An");
            Assert.Equal("%An%", p2.Value);

            var p3 = DbHelper.ParamInt("Tuoi", 25);
            Assert.Equal(25, p3.Value);

            var p4 = DbHelper.ParamDate("NgayVaoLam", null);
            Assert.Equal(DBNull.Value, p4.Value);
        }

        #endregion

        #region 6. Kiểm thử CrudHelper Security (Whitelist Identifier Validation)

        [Fact]
        public void CrudHelper_RejectsSqlInjectionInTableName()
        {
            var maliciousTable = "NhanVien; DROP TABLE NhanVien; --";
            var values = new Dictionary<string, object> { { "MaNV", "NV01" } };

            Assert.Throws<ArgumentException>(() =>
            {
                CrudHelper.Insert(maliciousTable, values);
            });
        }

        [Fact]
        public void CrudHelper_RejectsSqlInjectionInColumnName()
        {
            var values = new Dictionary<string, object>
            {
                { "MaNV; DROP TABLE NhanVien; --", "NV01" }
            };

            Assert.Throws<ArgumentException>(() =>
            {
                CrudHelper.Insert("NhanVien", values);
            });
        }

        #endregion

        #region 7. Kiểm thử ComboBoxHelper & GridHelper DataStructures

        [Fact]
        public void GridColumn_SetsPropertiesCorrectly()
        {
            var col = new GridHelper.GridColumn("Luong", "Lương cơ bản", 120, "N0", DataGridViewContentAlignment.MiddleRight);
            Assert.Equal("Luong", col.DataField);
            Assert.Equal("Lương cơ bản", col.HeaderText);
            Assert.Equal(120, col.Width);
            Assert.Equal("N0", col.Format);
            Assert.Equal(DataGridViewContentAlignment.MiddleRight, col.Alignment);
        }

        [Fact]
        public void ComboBoxHelper_Bind_SetsMemberPropertiesBeforeDataSource()
        {
            // Kiểm tra việc gán DisplayMember/ValueMember trước DataSource
            // tránh lỗi trả về DataRowView khi SelectedIndexChanged được kích hoạt
            var dt = CreateSampleDataTable();
            var combo = new ComboBox();

            object capturedValue = null;
            combo.SelectedIndexChanged += (s, e) =>
            {
                capturedValue = combo.SelectedValue;
            };

            ComboBoxHelper.Bind(combo, dt, "MaNV", "HoTen");

            // SelectedValue phải là MaNV ("NV01"), KHÔNG PHẢI DataRowView
            Assert.NotNull(capturedValue);
            Assert.False(capturedValue is DataRowView);
            Assert.Equal("NV01", capturedValue.ToString());
        }

        [Fact]
        public void ComboBoxHelper_SelectByValue_HandlesTypeConversion()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add(1, "Phòng IT");
            dt.Rows.Add(2, "Phòng HR");

            var combo = new ComboBox();
            ComboBoxHelper.Bind(combo, dt, "Id", "Name");

            // Truyền string "2" vào cột Id có DataType là int -> SelectByValue vẫn chọn đúng dòng 2
            ComboBoxHelper.SelectByValue(combo, "2");
            Assert.Equal(2, ComboBoxHelper.GetSelectedValue<int>(combo));
            Assert.Equal("Phòng HR", ComboBoxHelper.GetSelectedText(combo));
        }

        #endregion

        #region 8. Kiểm thử DataHelper.ChangeType & An toàn Nullable

        [Fact]
        public void DataHelper_ChangeType_HandlesNullableTypesProperly()
        {
            // Nullable int
            int? val1 = DataHelper.ChangeType<int?>("456");
            Assert.Equal(456, val1);

            int? nullVal1 = DataHelper.ChangeType<int?>(null);
            Assert.Null(nullVal1);

            int? dbNullVal = DataHelper.ChangeType<int?>(DBNull.Value);
            Assert.Null(dbNullVal);

            // Nullable decimal
            decimal? val2 = DataHelper.ChangeType<decimal?>("12345.67");
            Assert.Equal(12345.67m, val2);

            // Guid parsing
            var guid = Guid.NewGuid();
            var parsedGuid = DataHelper.ChangeType<Guid>(guid.ToString());
            Assert.Equal(guid, parsedGuid);
        }

        [Fact]
        public void DataHelper_Sum_MissingColumn_ReturnsZeroGracefully()
        {
            var dt = CreateSampleDataTable();
            // Cột không tồn tại không làm crash ứng dụng
            decimal sum = DataHelper.Sum(dt, "CotKhongTonTai");
            Assert.Equal(0m, sum);

            decimal avg = DataHelper.Average(dt, "CotKhongTonTai");
            Assert.Equal(0m, avg);

            decimal max = DataHelper.Max<decimal>(dt, "CotKhongTonTai");
            Assert.Equal(0m, max);
        }

        #endregion

        #region 9. Kiểm thử FilterBuilder NotIn & Nulls

        [Fact]
        public void FilterBuilder_NotIn_BuildsCorrectClause()
        {
            var (where, parms) = FilterBuilder.New()
                .NotIn("TrangThai", 0, -1)
                .Build();

            Assert.Equal(" WHERE [TrangThai] NOT IN (@f0, @f1)", where);
            Assert.Equal(2, parms.Length);
            Assert.Equal(0, parms[0].Value);
            Assert.Equal(-1, parms[1].Value);
        }

        [Fact]
        public void FilterBuilder_IsNull_And_IsNotNull()
        {
            var (where, parms) = FilterBuilder.New()
                .IsNull("NgayNghi")
                .IsNotNull("Email")
                .Build();

            Assert.Equal(" WHERE [NgayNghi] IS NULL AND [Email] IS NOT NULL", where);
            Assert.Empty(parms);
        }

        #endregion

        #region 10. Kiểm thử Validator An toàn & Culture Tolerant

        [Fact]
        public void Validator_DecimalRange_AcceptsBothDotAndComma()
        {
            // Dấu chấm (chuẩn quốc tế)
            var txt1 = new TextBox { Text = " 2.34 " };
            var res1 = Validator.ValidateAll(Validator.DecimalRange(txt1, "Hệ số", 1.0m, 5.0m));
            Assert.True(res1.IsValid);

            // Dấu phẩy (chuẩn Việt Nam)
            var txt2 = new TextBox { Text = "2,34" };
            var res2 = Validator.ValidateAll(Validator.DecimalRange(txt2, "Hệ số", 1.0m, 5.0m));
            Assert.True(res2.IsValid);
        }

        [Fact]
        public void Validator_NullControls_And_NullRules_HandledSafely()
        {
            // Null rules không gây NullReferenceException
            var res1 = Validator.ValidateAll(null);
            Assert.True(res1.IsValid);

            // Rule với Control null không gây crash
            var nullCtrlRule = Validator.Required(null, "Trường ảo");
            var res2 = Validator.ValidateAll(nullCtrlRule);
            Assert.False(res2.IsValid);
            Assert.Contains("không được để trống", res2.ErrorMessage);
        }

        #endregion

        #region 11. Kiểm thử CrudHelper Schema & Composite Keys

        [Fact]
        public void CrudHelper_IdentifierValidation_PreventsSqlInjection()
        {
            var values = new Dictionary<string, object> { { "MaNV", "NV99" } };

            // Tên bảng chứa SQL injection -> Ném ArgumentException
            Assert.Throws<ArgumentException>(() =>
                CrudHelper.Insert("NhanVien; DROP TABLE NhanVien;--", values));

            // Tên cột chứa SQL injection -> Ném ArgumentException
            var invalidColValues = new Dictionary<string, object> { { "MaNV;--", "NV99" } };
            Assert.Throws<ArgumentException>(() =>
                CrudHelper.Insert("NhanVien", invalidColValues));

            // Tên bảng rỗng -> Ném ArgumentException
            Assert.Throws<ArgumentException>(() =>
                CrudHelper.Insert("", values));

            // Danh sách values rỗng -> Ném ArgumentException
            Assert.Throws<ArgumentException>(() =>
                CrudHelper.Insert("NhanVien", new Dictionary<string, object>()));
        }

        [Fact]
        public void CrudHelper_CompositeKeys_ValidatesArguments()
        {
            var values = new Dictionary<string, object> { { "SoLuong", 5 } };

            // Khóa chính rỗng -> Ném ArgumentException
            Assert.Throws<ArgumentException>(() =>
                CrudHelper.Update("ChiTietHoaDon", values, new Dictionary<string, object>()));

            // Khóa chính có tên cột chứa ký tự injection -> Ném ArgumentException
            var invalidKey = new Dictionary<string, object> { { "MaHD; DROP TABLE--", "HD01" } };
            Assert.Throws<ArgumentException>(() =>
                CrudHelper.Update("ChiTietHoaDon", values, invalidKey));
        }

        #endregion

        #region 12. Kiểm thử Tiện ích mở rộng & Edge Cases

        [Fact]
        public void DataHelper_TryParseDecimal_HandlesAllFormats()
        {
            // Định dạng quốc tế
            Assert.True(DataHelper.TryParseDecimal("2.34", out decimal d1));
            Assert.Equal(2.34m, d1);

            // Định dạng Việt Nam (dấu phẩy thập phân)
            Assert.True(DataHelper.TryParseDecimal("2,34", out decimal d2));
            Assert.Equal(2.34m, d2);

            // Phân cách hàng nghìn (US)
            Assert.True(DataHelper.TryParseDecimal("1,000,000", out decimal d3));
            Assert.Equal(1000000m, d3);

            // Phân cách hàng nghìn + thập phân (VN/EU: 1.250.000,50)
            Assert.True(DataHelper.TryParseDecimal("1.250.000,50", out decimal d4));
            Assert.Equal(1250000.50m, d4);

            // Phân cách hàng nghìn + thập phân (US: 1,250,000.50)
            Assert.True(DataHelper.TryParseDecimal("1,250,000.50", out decimal d5));
            Assert.Equal(1250000.50m, d5);

            // Chuỗi không hợp lệ
            Assert.False(DataHelper.TryParseDecimal("abc", out _));
            Assert.False(DataHelper.TryParseDecimal("", out _));
        }

        [Fact]
        public void DataHelper_EscapeFilter_EscapesSingleQuotes()
        {
            Assert.Equal("O''Reilly", DataHelper.EscapeFilter("O'Reilly"));
            Assert.Equal("Nguyễn Văn A", DataHelper.EscapeFilter("Nguyễn Văn A"));
            Assert.Equal("", DataHelper.EscapeFilter(null));
        }

        [Fact]
        public void FilterBuilder_SkipIfZero_SkipsStringZero()
        {
            // Chuỗi "0" với skipZero = true -> bỏ qua
            var filter1 = FilterBuilder.New().SkipIfZero("MaPB", "0");
            Assert.False(filter1.HasConditions);

            // Chuỗi "0" với skipZero = false -> giữ lại (vd: GioiTinh = "0")
            var filter2 = FilterBuilder.New().Equal("GioiTinh", "0", skipZero: false);
            Assert.True(filter2.HasConditions);
            Assert.Contains("[GioiTinh] = @f0", filter2.ToWhereClause());
        }

        [Fact]
        public void FormHelper_FormOperations_WorkCorrectly()
        {
            var txt1 = new TextBox { Text = "Test1" };
            var txt2 = new TextBox { Text = "Test2" };

            // Khóa ReadOnly
            FormHelper.SetTextBoxesReadOnly(true, txt1, txt2);
            Assert.True(txt1.ReadOnly);
            Assert.True(txt2.ReadOnly);

            // Mở khóa
            FormHelper.SetTextBoxesReadOnly(false, txt1, txt2);
            Assert.False(txt1.ReadOnly);
            Assert.False(txt2.ReadOnly);

            // Container Enable/Disable
            var panel = new Panel();
            var btn = new Button();
            panel.Controls.Add(btn);
            FormHelper.EnableContainer(panel, false);
            Assert.False(btn.Enabled);

            // ResetForm unchecks ShowCheckBox DateTimePicker
            var dtp = new DateTimePicker { ShowCheckBox = true, Checked = true };
            FormHelper.ResetForm(txt1, dtp);
            Assert.Equal("", txt1.Text);
            Assert.False(dtp.Checked);
        }

        [Fact]
        public void Validator_Selected_ValidatesComboBoxCorrectly()
        {
            var cmb = new ComboBox();
            // Chưa có item và chưa chọn
            var rule = Validator.Selected(cmb, "Phòng ban");
            var res1 = Validator.ValidateAll(rule);
            Assert.False(res1.IsValid);

            // Thêm item và chọn
            cmb.Items.Add("Phòng Kế toán");
            cmb.SelectedIndex = 0;
            var res2 = Validator.ValidateAll(rule);
            Assert.True(res2.IsValid);
        }

        #endregion

        #region 8. Kiểm thử ReportHelper (Xuất Báo Cáo HTML & Gom nhóm)

        [Fact]
        public void ReportHelper_GenerateHtml_WithGroupingAndSums_GeneratesExpectedStructure()
        {
            var dt = new DataTable();
            dt.Columns.Add("MaNV", typeof(string));
            dt.Columns.Add("HoTen", typeof(string));
            dt.Columns.Add("TenPB", typeof(string));
            dt.Columns.Add("Luong", typeof(decimal));

            dt.Rows.Add("NV01", "Nguyễn Văn A", "Kế toán", 10000000m);
            dt.Rows.Add("NV02", "Trần Thị B", "Kế toán", 15000000m);
            dt.Rows.Add("NV03", "Lê Văn C", "Kỹ thuật", 20000000m);

            var columns = new[]
            {
                new ReportColumn("MaNV", "Mã NV", HorizontalAlignment.Center),
                new ReportColumn("HoTen", "Họ và tên"),
                new ReportColumn("Luong", "Mức lương", "N0", HorizontalAlignment.Right, isSum: true)
            };

            string html = ReportHelper.GenerateHtml(dt, "BẢNG LƯƠNG NHÂN VIÊN", "Phòng ban: Tất cả", 
                groupByColumn: "TenPB", groupPrefix: "Phòng: ", columns: columns);

            Assert.Contains("BẢNG LƯƠNG NHÂN VIÊN", html);
            Assert.Contains("Phòng: Kế toán (2 bản ghi)", html);
            Assert.Contains("Phòng: Kỹ thuật (1 bản ghi)", html);
            Assert.Contains("Nguyễn Văn A", html);
            Assert.Contains("TỔNG CỘNG", html);
            Assert.Contains("45,000,000", html); // 10tr + 15tr + 20tr = 45tr
        }

        [Fact]
        public void ReportHelper_GenerateHtml_AutoDetectColumns_WorksCorrectly()
        {
            var dt = new DataTable();
            dt.Columns.Add("MaSP", typeof(string));
            dt.Columns.Add("Gia", typeof(decimal));

            dt.Rows.Add("SP01", 500000m);

            string html = ReportHelper.GenerateHtml(dt, "DANH SÁCH SẢN PHẨM");
            Assert.Contains("DANH SÁCH SẢN PHẨM", html);
            Assert.Contains("SP01", html);
            Assert.Contains("500,000", html);
        }

        #endregion
    }
}

