using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
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

        [Fact]
        public void ReportHelper_GenerateHtml_ExamFormat_Footer_GroupTotals_And_EdgeCases()
        {
            var dt = new DataTable();
            dt.Columns.Add("MaNV", typeof(string));
            dt.Columns.Add("TenPB", typeof(string));
            dt.Columns.Add("Luong", typeof(decimal));

            dt.Rows.Add("NV01", "Kinh doanh", 5000000m);
            dt.Rows.Add("NV02", DBNull.Value, 3000000m);

            var cols = new[]
            {
                new ReportColumn("MaNV", "Mã NV", HorizontalAlignment.Center),
                new ReportColumn("Luong", "Lương", "N0", HorizontalAlignment.Right, isSum: true)
            };

            // Mặc định: có Trang 1/1, không có tổng phụ từng nhóm
            string htmlDefault = ReportHelper.GenerateHtml(dt, "BẢNG LƯƠNG", groupByColumn: "TenPB", groupPrefix: "Phòng ban: ", columns: cols);
            Assert.Contains("Trang 1/1", htmlDefault);
            Assert.Contains("Phòng ban: Kinh doanh (1 bản ghi)", htmlDefault);
            Assert.Contains("Phòng ban: Khác (1 bản ghi)", htmlDefault);
            Assert.DoesNotContain("Tổng Kinh doanh:", htmlDefault);
            Assert.Contains("8,000,000", htmlDefault);

            // Khi bật showGroupTotals = true: hiển thị tổng từng nhóm
            string htmlSub = ReportHelper.GenerateHtml(dt, "BẢNG LƯƠNG", groupByColumn: "TenPB", groupPrefix: "Phòng ban: ", columns: cols, showGroupTotals: true);
            Assert.Contains("Tổng Kinh doanh:", htmlSub);

            // Trường hợp chỉ có 1 cột và cột đó có IsSum = true
            var singleSumCol = new[] { new ReportColumn("Luong", "Lương", "N0", HorizontalAlignment.Right, isSum: true) };
            string htmlSingle = ReportHelper.GenerateHtml(dt, "TỔNG HỢP", columns: singleSumCol);
            Assert.Contains("TỔNG CỘNG", htmlSingle);
            Assert.Contains("8,000,000", htmlSingle);

            // Tắt cột STT (showStt = false) vẫn giữ thẳng cột và tính TỔNG CỘNG chính xác
            string htmlNoStt = ReportHelper.GenerateHtml(dt, "KHÔNG STT", columns: cols, showStt: false);
            Assert.DoesNotContain(">STT<", htmlNoStt);
            Assert.Contains("TỔNG CỘNG", htmlNoStt);
            Assert.Contains("8,000,000", htmlNoStt);

            string htmlSingleNoStt = ReportHelper.GenerateHtml(dt, "1 CỘT KHÔNG STT", columns: singleSumCol, showStt: false);
            Assert.DoesNotContain(">STT<", htmlSingleNoStt);
            Assert.Contains("TỔNG CỘNG", htmlSingleNoStt);
            Assert.Contains("8,000,000", htmlSingleNoStt);
        }

        [Fact]
        public void ReportHelper_ExtractTable_FromBoundAndUnboundGrid_WorksCorrectly()
        {
            // 1. DataGridView bind DataTable qua GridHelper (cột ẩn trong DataTable vẫn giữ nguyên)
            var dt = CreateSampleDataTable();
            using (var dgvBound = new DataGridView())
            {
                GridHelper.Bind(dgvBound, dt,
                    new GridHelper.GridColumn("MaNV", "Mã NV", 90),
                    new GridHelper.GridColumn("HoTen", "Họ tên", 150),
                    new GridHelper.GridColumn("Luong", "Mức lương", 100, "N0", DataGridViewContentAlignment.MiddleRight));

                DataTable extracted = ReportHelper.ExtractTable(dgvBound, out var visCols);
                Assert.Equal(dt.Columns.Count, extracted.Columns.Count);
                Assert.Equal(3, extracted.Rows.Count);
                Assert.Equal(3, visCols.Count);
                Assert.Equal("Mức lương", visCols[2].HeaderText);
                Assert.True(visCols[2].IsSum);
            }

            // 2. DataGridView nhập dữ liệu tay không có DataSource
            using (var dgvManual = new DataGridView())
            {
                dgvManual.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ma", HeaderText = "Mã SP" });
                dgvManual.Columns.Add(new DataGridViewTextBoxColumn { Name = "Gia", HeaderText = "Đơn giá", ValueType = typeof(decimal) });
                dgvManual.Rows.Add("SP01", 250000m);
                dgvManual.Rows.Add("SP02", 150000m);

                DataTable extractedManual = ReportHelper.ExtractTable(dgvManual, out var manualCols);
                Assert.Equal(2, extractedManual.Rows.Count);
                Assert.Equal(2, manualCols.Count);
                Assert.Equal("Mã SP", manualCols[0].HeaderText);
                Assert.True(manualCols[1].IsSum);
            }
        }

        [Fact]
        public void FormReportViewer_DynamicDesigner_GathersAllFields_Reorders_Hides_Groups_And_Resets()
        {
            var dt = CreateSampleDataTable(); // MaNV, HoTen, MaPhong, Luong, HeSoLuong

            using (var viewer = new FormReportViewer(dt, new ReportDesignOptions { ReportTitle = "BÁO CÁO MẪU" }))
            {
                // Tự gom tất cả 5 trường trong DataTable vào bảng ban đầu
                Assert.Equal(5, viewer.VisibleColumns.Count);
                Assert.Empty(viewer.HiddenColumns);
                Assert.Contains("BÁO CÁO MẪU", viewer.CurrentHtml);
                Assert.Contains(">STT<", viewer.CurrentHtml);

                // Sửa tiêu đề, công ty, tắt STT -> CurrentHtml cập nhật ngay
                viewer.ReportTitle = "BẢNG LƯƠNG THÁNG 11";
                viewer.ReportCompanyName = "CÔNG TY ABC";
                viewer.ShowStt = false;
                Assert.Contains("BẢNG LƯƠNG THÁNG 11", viewer.CurrentHtml);
                Assert.Contains("CÔNG TY ABC", viewer.CurrentHtml);
                Assert.DoesNotContain(">STT<", viewer.CurrentHtml);

                // Gom nhóm theo MaPhong -> tự đưa MaPhong sang danh sách ẩn & gom nhóm HTML
                Assert.True(viewer.SetGroupByColumn("MaPhong"));
                Assert.Equal("MaPhong", viewer.GroupByColumn);
                Assert.DoesNotContain(viewer.VisibleColumns, c => c.ColumnName == "MaPhong");
                Assert.Contains(viewer.HiddenColumns, c => c.ColumnName == "MaPhong");
                Assert.Contains("IT (2 bản ghi)", viewer.CurrentHtml);

                // Ẩn MaNV & đổi thứ tự cột Luong lên đầu
                Assert.True(viewer.HideColumn("MaNV"));
                Assert.DoesNotContain("<th>MaNV</th>", viewer.CurrentHtml);
                Assert.True(viewer.MoveVisibleColumn("Luong", 0));
                Assert.Equal("Luong", viewer.VisibleColumns[0].ColumnName);

                // Hiện lại MaNV vào vị trí đầu
                Assert.True(viewer.ShowColumn("MaNV", 0));
                Assert.Equal("MaNV", viewer.VisibleColumns[0].ColumnName);
                Assert.Contains("<th>MaNV</th>", viewer.CurrentHtml);

                // Ẩn hết mọi cột -> báo hướng dẫn sạch, không tự bật lại BuildAutoColumns
                foreach (var name in new[] { "MaNV", "Luong", "HoTen", "HeSoLuong" })
                {
                    viewer.HideColumn(name);
                }
                Assert.Empty(viewer.VisibleColumns);
                Assert.Contains("Chưa chọn cột hiển thị", viewer.CurrentHtml);

                // Nhấn Mặc định -> khôi phục trạng thái ban đầu
                viewer.ResetDesign();
                Assert.Equal("BÁO CÁO MẪU", viewer.ReportTitle);
                Assert.True(viewer.ShowStt);
                Assert.Equal(5, viewer.VisibleColumns.Count);
                Assert.Empty(viewer.HiddenColumns);
                Assert.Contains(">STT<", viewer.CurrentHtml);
            }
        }

        [Fact]
        public void FormReportViewer_WithExplicitSubsetColumns_PlacesExtraFieldsInHiddenList()
        {
            var dt = CreateSampleDataTable(); // MaNV, HoTen, MaPhong, Luong, HeSoLuong
            var options = new ReportDesignOptions
            {
                ReportTitle = "DANH SÁCH RÚT GỌN",
                Columns = new[]
                {
                    new ReportColumn("MaNV", "Mã NV"),
                    new ReportColumn("HoTen", "Họ và tên")
                }
            };

            using (var viewer = new FormReportViewer(dt, options))
            {
                Assert.Equal(2, viewer.VisibleColumns.Count);
                Assert.Equal(3, viewer.HiddenColumns.Count);
                Assert.Contains(viewer.HiddenColumns, c => c.ColumnName == "MaPhong");
                Assert.Contains(viewer.HiddenColumns, c => c.ColumnName == "Luong");
                Assert.Contains(viewer.HiddenColumns, c => c.ColumnName == "HeSoLuong");

                // Người dùng có thể kéo cột ẩn (Luong) vào báo cáo ngay lúc xem
                Assert.True(viewer.ShowColumn("Luong"));
                Assert.Equal(3, viewer.VisibleColumns.Count);
                Assert.Contains("30,000,000", viewer.CurrentHtml);
            }
        }

        [Fact]
        public void FormReportViewer_StaRealHandleAndWebBrowser_CreatesDragDropAndPreviewWithoutError()
        {
            Exception capturedEx = null;
            string htmlAfterEdit = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    var dt = CreateSampleDataTable();
                    using (var viewer = new FormReportViewer(dt, new ReportDesignOptions { ReportTitle = "TEST LIVE UI" }))
                    using (var closeTimer = new System.Windows.Forms.Timer { Interval = 250 })
                    {
                        viewer.ShowInTaskbar = false;
                        viewer.StartPosition = FormStartPosition.Manual;
                        viewer.Location = new System.Drawing.Point(-32000, -32000);
                        closeTimer.Tick += (s, e) =>
                        {
                            closeTimer.Stop();
                            viewer.ReportTitle = "TIÊU ĐỀ MỚI";
                            viewer.HideColumn("MaPhong");
                            htmlAfterEdit = viewer.CurrentHtml;
                            viewer.Close();
                        };
                        viewer.Shown += (s, e) => closeTimer.Start();
                        Application.Run(viewer);
                    }
                }
                catch (Exception ex)
                {
                    capturedEx = ex;
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            bool finished = thread.Join(8000);

            Assert.True(finished, "FormReportViewer UI thread timed out.");
            Assert.Null(capturedEx);
            Assert.NotNull(htmlAfterEdit);
            Assert.Contains("TIÊU ĐỀ MỚI", htmlAfterEdit);
            Assert.DoesNotContain("<th>MaPhong</th>", htmlAfterEdit);
        }

        [Fact]
        public void ErgonomicAudit_DbHelper_Binding_Validation_DataHelper_Improvements()
        {
            // 1. DbHelper.ConnectionString property & Param auto-@ normalization
            string oldConn = DbHelper.ConnectionString;
            try
            {
                DbHelper.ConnectionString = "Server=localhost;Database=DemoDb;Trusted_Connection=True;";
                Assert.Equal("Server=localhost;Database=DemoDb;Trusted_Connection=True;", DbHelper.ConnectionString);

                var p = DbHelper.Param("MaNV", "NV01");
                Assert.Equal("@MaNV", p.ParameterName);
                Assert.Equal("NV01", p.Value);
            }
            finally
            {
                DbHelper.ConnectionString = oldConn;
            }

            // 2. GridHelper.GetSelectedValue đọc được cả cột ẩn trong DataRowView & Bind reset AutoGenerateColumns
            var dt = CreateSampleDataTable();
            using (var dgv = new DataGridView())
            {
                GridHelper.Bind(dgv, dt,
                    new GridHelper.GridColumn("MaNV", "Mã NV", 90),
                    new GridHelper.GridColumn("HoTen", "Họ tên", 150));
                GridHelper.SetReadOnly(dgv);
                Assert.True(GridHelper.SelectRowByValue(dgv, "MaNV", "NV02"));
                Assert.Equal("IT", GridHelper.GetSelectedValue<string>(dgv, "MaPhong"));

                dgv.Columns.Clear();
                GridHelper.Bind(dgv, dt);
                Assert.True(dgv.AutoGenerateColumns);
            }

            // 3. ComboBoxHelper.Bind 2 tham số tự nhận diện cột Value/Display & SelectByValue không throw với giá trị sai
            var cbo = new ComboBox();
            ComboBoxHelper.Bind(cbo, dt);
            Assert.Equal("MaNV", cbo.ValueMember);
            Assert.Equal("HoTen", cbo.DisplayMember);

            ComboBoxHelper.SelectByValue(cbo, 999);

            // 4. FilterBuilder.SkipIfAllSelected với ComboBoxHelper.BindItems + FormMapper DropDownList
            var cboTrangThai = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            ComboBoxHelper.BindItems(cboTrangThai, "Tất cả", "Đang làm", "Đã nghỉ");
            Assert.False(FilterBuilder.New().SkipIfAllSelected(cboTrangThai, "TrangThai").HasConditions);

            cboTrangThai.SelectedIndex = 1;
            var (wStatus, pStatus) = FilterBuilder.New().SkipIfAllSelected(cboTrangThai, "TrangThai").Build();
            Assert.Equal(" WHERE [TrangThai] = @f0", wStatus);
            Assert.Equal("Đang làm", pStatus[0].Value);

            var cboDrop = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            ComboBoxHelper.BindItems(cboDrop, "Nam", "Nữ");
            var dtMap = new DataTable();
            dtMap.Columns.Add("GioiTinh", typeof(string));
            dtMap.Rows.Add("Nữ");
            Assert.True(FormMapper.LoadRow(dtMap, FormMapper.Map(cboDrop, "GioiTinh")));
            Assert.Equal("Nữ", cboDrop.Text);

            // 5. Validator.Selected chặn -1 ở SelectedIndex == 0 & DataHelper.Count an toàn với filter sai
            var cboPick = new ComboBox();
            var dtOpts = new DataTable();
            dtOpts.Columns.Add("Id", typeof(int));
            dtOpts.Columns.Add("Ten", typeof(string));
            dtOpts.Rows.Add(1, "Kinh doanh");
            ComboBoxHelper.BindWithAll(cboPick, dtOpts, "Id", "Ten", "-- Chọn --", -1);
            Assert.False(Validator.ValidateAll(Validator.Selected(cboPick, "Phòng ban")).IsValid);
            cboPick.SelectedIndex = 1;
            Assert.True(Validator.ValidateAll(Validator.Selected(cboPick, "Phòng ban")).IsValid);

            Assert.Equal(3, DataHelper.Count(dt, ""));
            Assert.Equal(0, DataHelper.Count(dt, "Invalid === Syntax"));

            // 6. CrudHelper.GetAll từ chối ORDER BY injection
            Assert.Throws<ArgumentException>(() => CrudHelper.GetAll("NhanVien", "MaNV; DROP TABLE NhanVien"));
        }

        [Fact]
        public void XlsxExporter_BuildXlsxBytes_ProducesValidOpenXmlWorkbookZip()
        {
            var dt = CreateSampleDataTable();
            byte[] xlsxBytes = ReportHelper.BuildXlsxBytes(
                dt,
                reportTitle: "BẢNG LƯƠNG & <TEST>",
                filterCondition: "Phòng: IT",
                groupByColumn: "MaPhong",
                groupPrefix: "Phòng: ",
                companyName: "CÔNG TY ABC",
                showGroupTotals: true,
                showStt: true);

            Assert.True(xlsxBytes.Length > 200);
            Assert.Equal((byte)'P', xlsxBytes[0]);
            Assert.Equal((byte)'K', xlsxBytes[1]);

            using (var ms = new System.IO.MemoryStream(xlsxBytes))
            using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read))
            {
                Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
                Assert.NotNull(zip.GetEntry("_rels/.rels"));
                Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
                Assert.NotNull(zip.GetEntry("xl/_rels/workbook.xml.rels"));
                Assert.NotNull(zip.GetEntry("xl/styles.xml"));

                var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml");
                Assert.NotNull(sheetEntry);
                using (var sr = new System.IO.StreamReader(sheetEntry.Open()))
                {
                    string sheetXml = sr.ReadToEnd();
                    var doc = System.Xml.Linq.XDocument.Parse(sheetXml);
                    Assert.NotNull(doc.Root);
                    Assert.Contains("BẢNG LƯƠNG &amp; &lt;TEST&gt;", sheetXml);
                    Assert.Contains("Phòng: IT (2 bản ghi)", sheetXml);
                    Assert.Contains("<v>10000000</v>", sheetXml);
                    Assert.Contains("<v>30000000</v>", sheetXml);
                    Assert.Contains("TỔNG CỘNG", sheetXml);
                    Assert.Contains("mergeCell", sheetXml);
                }
            }
        }

        [Fact]
        public void FormMapper_AutoBind_AutoLoad_AutoGetValues_AutoClear_And_TypedConversion()
        {
            var dtPB = new DataTable();
            dtPB.Columns.Add("MaPB", typeof(string));
            dtPB.Columns.Add("TenPB", typeof(string));
            dtPB.Rows.Add("PB01", "Kinh doanh");
            dtPB.Rows.Add("PB02", "Kỹ thuật");

            var dtSchema = new DataTable();
            dtSchema.Columns.Add("MaNV", typeof(string));
            dtSchema.Columns.Add("HoTen", typeof(string));
            dtSchema.Columns.Add("GioiTinh", typeof(bool));
            dtSchema.Columns.Add("NgaySinh", typeof(DateTime));
            dtSchema.Columns.Add("MaPB", typeof(string));
            dtSchema.Columns.Add("HeSoLuong", typeof(double));
            dtSchema.Columns.Add("Luong", typeof(decimal)); // Cột chỉ hiển thị, không có input control
            dtSchema.Rows.Add("NV001", "Nguyễn An", true, new DateTime(1998, 5, 20), "PB02", 2.34, 5475600m);

            using (var form = new Form())
            {
                var grp = new GroupBox();
                var pnlGender = new Panel();
                var txtMaNV = new TextBox { Name = "txtMaNV" };
                var txtHoTen = new TextBox { Name = "txtHoTen" };
                var rdoNam = new RadioButton { Name = "rdoNam", Tag = "GioiTinh:1" };
                var rdoNu = new RadioButton { Name = "rdoNu", Tag = "GioiTinh:0" };
                var dtpNgaySinh = new DateTimePicker { Name = "dtpNgaySinh" };
                var cboPhongBan = new ComboBox { Name = "cboPhongBan", DropDownStyle = ComboBoxStyle.DropDownList };
                var txtHeSoLuong = new TextBox { Name = "txtHeSoLuong" };
                var txtTimKiem = new TextBox { Name = "txtTimKiem", Text = "Bỏ qua" };

                ComboBoxHelper.Bind(cboPhongBan, dtPB, "MaPB", "TenPB");
                pnlGender.Controls.AddRange(new Control[] { rdoNam, rdoNu });
                grp.Controls.AddRange(new Control[] { txtMaNV, txtHoTen, pnlGender, dtpNgaySinh, cboPhongBan, txtHeSoLuong });
                form.Controls.AddRange(new Control[] { grp, txtTimKiem });

                Assert.True(FormMapper.AutoLoad(form, dtSchema));
                Assert.Equal("NV001", txtMaNV.Text);
                Assert.Equal("Nguyễn An", txtHoTen.Text);
                Assert.True(rdoNam.Checked);
                Assert.False(rdoNu.Checked);
                Assert.Equal(new DateTime(1998, 5, 20), dtpNgaySinh.Value.Date);
                Assert.Equal("PB02", cboPhongBan.SelectedValue);
                Assert.Equal("2.34", txtHeSoLuong.Text);

                // Thay đổi input bằng dấu phẩy VN và RadioButton -> AutoGetValues tự ép đúng kiểu double/bool theo schema
                txtHeSoLuong.Text = "3,66";
                rdoNu.Checked = true;
                ComboBoxHelper.SelectByValue(cboPhongBan, "PB01");

                var values = FormMapper.AutoGetValues(form, dtSchema);
                Assert.Equal(6, values.Count);
                Assert.IsType<double>(values["HeSoLuong"]);
                Assert.Equal(3.66, (double)values["HeSoLuong"], 4);
                Assert.Equal(false, values["GioiTinh"]);
                Assert.Equal("PB01", values["MaPB"]);
                Assert.False(values.ContainsKey("TimKiem"));

                FormMapper.AutoClear(grp);
                Assert.Equal("", txtMaNV.Text);
                Assert.Equal("", txtHeSoLuong.Text);
            }
        }

        [Fact]
        public void DbHelper_HasError_ThrowOnError_And_FastTimeoutConnectionTest()
        {
            bool oldThrow = DbHelper.ThrowOnError;
            try
            {
                DbHelper.ShowErrorDialog = false;
                DbHelper.LastError = null;
                Assert.False(DbHelper.HasError);

                DbHelper.ThrowOnError = true;
                Assert.Throws<KeyNotFoundException>(() => DbHelper.GetDataTable("SELECT 1", "NonExistentConfigKey_XYZ"));
                Assert.True(DbHelper.HasError);

                DbHelper.ThrowOnError = false;
                var sw = System.Diagnostics.Stopwatch.StartNew();
                bool ok = DbHelper.TestConnection("Server=192.0.2.1;Database=Missing;", showError: false, timeoutSeconds: 1);
                sw.Stop();
                Assert.False(ok);
                Assert.True(DbHelper.HasError);
                Assert.True(sw.ElapsedMilliseconds < 6000, $"TestConnection took too long: {sw.ElapsedMilliseconds}ms");
            }
            finally
            {
                DbHelper.ThrowOnError = oldThrow;
                DbHelper.LastError = null;
            }
        }

        [Fact]
        public void FormReportViewer_RealUiControls_QuickTitle_Chips_DragDropSimulation_Buttons_And_Reset()
        {
            Exception capturedEx = null;
            string htmlAfterInteractions = null;
            string htmlAfterReset = null;
            int chipCountBeforeHide = 0;
            int chipCountAfterHide = 0;
            bool sidebarCollapsedAfterToggle = false;

            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    var dt = CreateSampleDataTable();
                    using (var viewer = new FormReportViewer(dt, new ReportDesignOptions { ReportTitle = "TIÊU ĐỀ GỐC" }))
                    using (var runTimer = new System.Windows.Forms.Timer { Interval = 200 })
                    {
                        viewer.ShowInTaskbar = false;
                        viewer.StartPosition = FormStartPosition.Manual;
                        viewer.Location = new System.Drawing.Point(-32000, -32000);

                        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                        var txtQuickTitle = (TextBox)typeof(FormReportViewer).GetField("_txtQuickTitle", flags).GetValue(viewer);
                        var txtTitle = (TextBox)typeof(FormReportViewer).GetField("_txtTitle", flags).GetValue(viewer);
                        var chipStrip = (FlowLayoutPanel)typeof(FormReportViewer).GetField("_chipStrip", flags).GetValue(viewer);
                        var btnHide = (Button)typeof(FormReportViewer).GetField("_btnHideCol", flags).GetValue(viewer);
                        var btnShow = (Button)typeof(FormReportViewer).GetField("_btnShowCol", flags).GetValue(viewer);
                        var btnMoveDown = (Button)typeof(FormReportViewer).GetField("_btnMoveDown", flags).GetValue(viewer);
                        var btnThietKe = (Button)typeof(FormReportViewer).GetField("_btnThietKe", flags).GetValue(viewer);
                        var btnDatLai = (Button)typeof(FormReportViewer).GetField("_btnDatLai", flags).GetValue(viewer);
                        var lstVisible = (ListBox)typeof(FormReportViewer).GetField("_lstVisible", flags).GetValue(viewer);
                        var pnlQuickGroupDrop = (Panel)typeof(FormReportViewer).GetField("_pnlQuickGroupDrop", flags).GetValue(viewer);
                        var splitMain = (SplitContainer)typeof(FormReportViewer).GetField("_splitMain", flags).GetValue(viewer);
                        var debounceTimer = (System.Windows.Forms.Timer)typeof(FormReportViewer).GetField("_debounceTimer", flags).GetValue(viewer);

                        runTimer.Tick += (s, e) =>
                        {
                            runTimer.Stop();

                            // 1. Sửa thẳng ô Tiêu đề phía trên bản xem trước -> đồng bộ sang ô bên trái + cập nhật HTML
                            txtQuickTitle.Text = "TIÊU ĐỀ ĐÃ SỬA";
                            Assert.Equal("TIÊU ĐỀ ĐÃ SỬA", txtTitle.Text);

                            // 2. Đếm số chip cột trực tiếp trên Preview Strip và bấm nút thật (Hide / MoveDown / Show)
                            chipCountBeforeHide = chipStrip.Controls.OfType<Panel>().Count();
                            lstVisible.SelectedIndex = 0; // MaNV
                            btnMoveDown.PerformClick();
                            Assert.Equal("MaNV", viewer.VisibleColumns[1].ColumnName);

                            btnHide.PerformClick(); // Ẩn MaNV
                            chipCountAfterHide = chipStrip.Controls.OfType<Panel>().Count();
                            Assert.Contains(viewer.HiddenColumns, c => c.ColumnName == "MaNV");

                            // 3. Mở lại MaNV bằng nút Show thật
                            btnShow.PerformClick();
                            Assert.Equal(5, viewer.VisibleColumns.Count);

                            // 4. Giả lập DragEventArgs thả trực tiếp payload cột MaPhong vào vùng _pnlQuickGroupDrop
                            var payloadType = typeof(FormReportViewer).GetNestedType("ColumnDragPayload", flags);
                            var maPhongCol = viewer.VisibleColumns.First(c => c.ColumnName == "MaPhong");
                            object payload = Activator.CreateInstance(payloadType, maPhongCol, lstVisible, 2);
                            var dataObj = new DataObject();
                            dataObj.SetData(payloadType, payload);
                            var dragArgs = new DragEventArgs(dataObj, 0, 0, 0, DragDropEffects.Move, DragDropEffects.Move);
                            var onDragDrop = typeof(Control).GetMethod("OnDragDrop", flags);
                            onDragDrop.Invoke(pnlQuickGroupDrop, new object[] { dragArgs });
                            Assert.Equal("MaPhong", viewer.GroupByColumn);

                            // 5. Ép debounce tick / EnsurePrintable trước khi đọc HTML
                            typeof(FormReportViewer).GetMethod("EnsurePrintable", flags).Invoke(viewer, null);
                            htmlAfterInteractions = viewer.CurrentHtml;

                            // 6. Toggle nút Ẩn/Hiện panel Thiết kế bên trái + bấm Mặc định
                            btnThietKe.PerformClick();
                            sidebarCollapsedAfterToggle = splitMain.Panel1Collapsed;

                            btnDatLai.PerformClick();
                            htmlAfterReset = viewer.CurrentHtml;

                            viewer.Close();
                        };

                        viewer.Shown += (s, e) => runTimer.Start();
                        Application.Run(viewer);
                    }
                }
                catch (Exception ex)
                {
                    capturedEx = ex;
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            bool finished = thread.Join(10000);

            Assert.True(finished, "FormReportViewer UI interaction test timed out.");
            Assert.Null(capturedEx);
            Assert.Equal(5, chipCountBeforeHide);
            Assert.Equal(4, chipCountAfterHide);
            Assert.True(sidebarCollapsedAfterToggle);
            Assert.Contains("TIÊU ĐỀ ĐÃ SỬA", htmlAfterInteractions);
            Assert.Contains("IT (2 bản ghi)", htmlAfterInteractions);
            Assert.Contains("TIÊU ĐỀ GỐC", htmlAfterReset);
        }

        [Fact]
        public void ReportHelper_PrepareReportTable_AddsSequentialStt()
        {
            var dt = CreateSampleDataTable();
            DataTable prepared = ReportHelper.PrepareReportTable(dt);

            Assert.Equal("STT", prepared.Columns[0].ColumnName);
            Assert.Equal(1, prepared.Rows[0]["STT"]);
            Assert.Equal(2, prepared.Rows[1]["STT"]);
            Assert.Equal(3, prepared.Rows[2]["STT"]);
        }

        [Fact]
        public void ReportHelper_BindRdlc_And_RenderRdlcBytes_WorksViaReflection()
        {
            var viewer = new MockReportViewer();
            var dt = CreateSampleDataTable();
            var parms = new Dictionary<string, string>
            {
                { "pDieuKien", "Phòng ban: Tất cả" },
                { "pNgayLap", "27/11/2026" }
            };

            ReportHelper.BindRdlc(viewer, "dsNhanVien", dt, parms, reportEmbeddedResource: "App.rptBangLuong.rdlc");

            Assert.Equal("App.rptBangLuong.rdlc", viewer.LocalReport.ReportEmbeddedResource);
            Assert.Single(viewer.LocalReport.DataSources);
            Assert.Equal("dsNhanVien", viewer.LocalReport.DataSources[0].Name);
            Assert.Equal(2, viewer.LocalReport.Parameters.Count);
            Assert.True(viewer.Refreshed);

            byte[] pdfBytes = ReportHelper.RenderRdlcBytes(viewer, "PDF");
            Assert.Equal(new byte[] { 1, 2, 3 }, pdfBytes);
        }

        [Fact]
        public void Upgrades_Validation_DataHelper_ComboBox_And_FormMapper_RadioPair()
        {
            // 1. DataHelper.TryParseDecimal hỗ trợ nhiều dấu chấm kiểu VN ("1.500.000")
            Assert.True(DataHelper.TryParseDecimal("1.500.000", out decimal vnMoney));
            Assert.Equal(1500000m, vnMoney);

            // 2. Validator.Pattern tự trim khoảng trắng thừa và NotFutureDate
            var txtMa = new TextBox { Text = "  NV008  " };
            var dtpFuture = new DateTimePicker { Value = DateTime.Today.AddDays(1) };
            Assert.True(Validator.ValidateAll(Validator.Pattern(txtMa, "Mã NV", @"^NV\d{3}$")).IsValid);
            Assert.False(Validator.ValidateAll(Validator.NotFutureDate(dtpFuture, "Ngày nhập")).IsValid);

            // 3. ComboBoxHelper.SelectByValue khớp chuỗi có khoảng trắng thừa từ NCHAR
            var dtPB = new DataTable();
            dtPB.Columns.Add("MaPB", typeof(string));
            dtPB.Columns.Add("TenPB", typeof(string));
            dtPB.Rows.Add("PB01   ", "Kinh doanh");
            dtPB.Rows.Add("PB02   ", "Kế toán");
            var cbo = new ComboBox();
            ComboBoxHelper.Bind(cbo, dtPB, "MaPB", "TenPB");
            ComboBoxHelper.SelectByValue(cbo, "pb02");
            Assert.Equal(1, cbo.SelectedIndex);

            // 4. FormMapper cặp RadioButton Nam/Nữ
            var grp = new GroupBox();
            var rdoNam = new RadioButton { Checked = true };
            var rdoNu = new RadioButton { Checked = false };
            grp.Controls.Add(rdoNam);
            grp.Controls.Add(rdoNu);

            var dtNV = new DataTable();
            dtNV.Columns.Add("GioiTinh", typeof(bool));
            dtNV.Rows.Add(false);

            FormMapper.LoadRow(dtNV.Rows[0], FormMapper.Map(rdoNam, rdoNu, "GioiTinh"));
            Assert.False(rdoNam.Checked);
            Assert.True(rdoNu.Checked);

            var values = FormMapper.GetValues(FormMapper.Map(rdoNam, rdoNu, "GioiTinh"));
            Assert.Equal(false, values["GioiTinh"]);
        }

        [Fact]
        public void StudentRoleplay_RootUpgrades_AllWorkTogetherCleanly()
        {
            // 1. FormMapper typed numeric mapping ("2,34" -> double 2.34, "1.500.000" -> decimal 1500000m)
            var txtHeSo = new TextBox { Text = "2,34" };
            var txtLuong = new TextBox { Text = "1.500.000" };
            var txtOptional = new TextBox { Text = "   " };

            var dict = FormMapper.GetValues(
                FormMapper.MapDouble(txtHeSo, "HeSoLuong"),
                FormMapper.MapDecimal(txtLuong, "DonGia"),
                FormMapper.MapInt(txtOptional, "PhuCap", nullable: true)
            );
            Assert.IsType<double>(dict["HeSoLuong"]);
            Assert.Equal(2.34, (double)dict["HeSoLuong"], 4);
            Assert.IsType<decimal>(dict["DonGia"]);
            Assert.Equal(1500000m, dict["DonGia"]);
            Assert.Equal(DBNull.Value, dict["PhuCap"]);

            // 2. FormMapper.LoadRow fallback cho ComboBox khi thiếu MaPB trong SELECT nhưng có TenPB
            var dtPB = new DataTable();
            dtPB.Columns.Add("MaPB", typeof(string));
            dtPB.Columns.Add("TenPB", typeof(string));
            dtPB.Rows.Add("PB01", "Kinh doanh");
            dtPB.Rows.Add("PB02", "Kế toán");
            var cbo = new ComboBox();
            ComboBoxHelper.Bind(cbo, dtPB, "MaPB", "TenPB");

            var dtRowWithoutKey = new DataTable();
            dtRowWithoutKey.Columns.Add("MaNV", typeof(string));
            dtRowWithoutKey.Columns.Add("TenPB", typeof(string));
            dtRowWithoutKey.Rows.Add("NV001", "Kế toán");

            FormMapper.LoadRow(dtRowWithoutKey.Rows[0], FormMapper.Map(cbo, "MaPB"));
            Assert.Equal("PB02", ComboBoxHelper.GetSelectedValue<string>(cbo));

            // 3. ComboBoxHelper graceful handling khi DataTable rỗng hoàn toàn (0 cột do lỗi kết nối)
            var emptyBrokenDt = new DataTable();
            ComboBoxHelper.Bind(cbo, emptyBrokenDt, "MaPB", "TenPB");
            Assert.Empty(cbo.Items);
            ComboBoxHelper.BindWithAll(cbo, emptyBrokenDt, "MaPB", "TenPB", "Tất cả");
            Assert.Single(cbo.Items);
            Assert.Equal("Tất cả", cbo.Text);

            // 4. ComboBoxHelper.BindItems nạp danh sách tĩnh không trùng lặp
            var cboGioiTinh = new ComboBox();
            ComboBoxHelper.BindItems(cboGioiTinh, "Tất cả", "Nam", "Nữ");
            ComboBoxHelper.BindItems(cboGioiTinh, "Tất cả", "Nam", "Nữ");
            Assert.Equal(3, cboGioiTinh.Items.Count);
            Assert.Equal("Tất cả", cboGioiTinh.Text);

            // 5. GridHelper.ClearSelection & SelectRowByValue
            var dtGrid = CreateSampleDataTable();
            var dgv = new DataGridView();
            GridHelper.Bind(dgv, dtGrid,
                new GridHelper.GridColumn("MaNV", "Mã NV", 90),
                new GridHelper.GridColumn("HoTen", "Họ tên", 150));
            GridHelper.SetReadOnly(dgv);
            Assert.True(GridHelper.SelectRowByValue(dgv, "MaNV", "NV02"));
            Assert.Equal("NV02", GridHelper.GetSelectedValue<string>(dgv, "MaNV"));
            GridHelper.ClearSelection(dgv);
            Assert.False(GridHelper.HasSelection(dgv));

            // 6. Validator.AgeRangeAtDate với ReferenceControl trực tiếp + DateBeforeOrEqual
            var dtpBirth = new DateTimePicker { Value = new DateTime(2002, 5, 15) };
            var dtpHire = new DateTimePicker { Value = new DateTime(2020, 5, 14) }; // chưa đủ 18 tuổi
            var failAge = Validator.ValidateAll(
                Validator.DateBeforeOrEqual(dtpBirth, dtpHire, "Ngày sinh"),
                Validator.AgeRangeAtDate(dtpBirth, dtpHire, "Tuổi", 18, 120, "Phải đủ 18 tuổi")
            );
            Assert.False(failAge.IsValid);
            Assert.Equal("Phải đủ 18 tuổi", failAge.ErrorMessage);

            dtpHire.Value = new DateTime(2020, 5, 15); // đúng ngày tròn 18 tuổi
            var passAge = Validator.ValidateAll(
                Validator.DateBeforeOrEqual(dtpBirth, dtpHire, "Ngày sinh"),
                Validator.AgeRangeAtDate(dtpBirth, dtpHire, "Tuổi", 18, 120)
            );
            Assert.True(passAge.IsValid);

            // 7. FormHelper.FormatDatePickers
            FormHelper.FormatDatePickers(dtpBirth, dtpHire);
            Assert.Equal(DateTimePickerFormat.Custom, dtpBirth.Format);
            Assert.Equal("dd/MM/yyyy", dtpBirth.CustomFormat);

            // 8. DbHelper.SetConnectionStringFromConfig đọc file XML config
            string tempCfg = System.IO.Path.GetTempFileName();
            try
            {
                System.IO.File.WriteAllText(tempCfg,
                    "<configuration><connectionStrings><add name=\"QLNhanSu\" connectionString=\"Server=.;Database=QLNhanSu;Trusted_Connection=True;\" /></connectionStrings></configuration>");
                bool loaded = DbHelper.SetConnectionStringFromConfig("QLNhanSu", "cfgTest", tempCfg);
                Assert.True(loaded);
                Assert.False(DbHelper.TestConnection("NonExistentDbKey", showError: false));
                Assert.NotNull(DbHelper.LastError);
            }
            finally
            {
                if (System.IO.File.Exists(tempCfg)) System.IO.File.Delete(tempCfg);
            }

            // 9. Crystal Report Reflection binding
            var crystalViewer = new MockCrystalReportViewer();
            var rptDoc = new MockCrystalReportDocument();
            ReportHelper.BindCrystalReport(crystalViewer, rptDoc, dtGrid, new Dictionary<string, object> { { "pDieuKien", "Tất cả" } });
            Assert.Same(dtGrid, rptDoc.DataSource);
            Assert.Equal("Tất cả", rptDoc.Params["pDieuKien"]);
            Assert.Same(rptDoc, crystalViewer.ReportSource);
            Assert.True(crystalViewer.Refreshed);
        }

        [Fact]
        public void GridHelper_SelectionChanged_ReadsSelectedRow_And_MessageHelper_SuppressDialogs()
        {
            // Kiểm tra GetSelectedRow lấy đúng SelectedRows[0] trong sự kiện SelectionChanged khi Form đã tạo Handle
            var dt = CreateSampleDataTable();
            using (var form = new Form())
            {
                var dgv = new DataGridView { Dock = DockStyle.Fill };
                form.Controls.Add(dgv);
                form.CreateControl();
                GridHelper.Bind(dgv, dt,
                    new GridHelper.GridColumn("MaNV", "Mã NV", 90),
                    new GridHelper.GridColumn("HoTen", "Họ tên", 150));
                GridHelper.SetReadOnly(dgv);

                string capturedDuringEvent = null;
                dgv.SelectionChanged += (s, e) =>
                {
                    capturedDuringEvent = GridHelper.GetSelectedValue<string>(dgv, "MaNV");
                };

                Assert.True(GridHelper.SelectRowByValue(dgv, "MaNV", "NV02"));
                Assert.Equal("NV02", capturedDuringEvent);
            }

            // Kiểm tra MessageHelper.SuppressDialogs & LastMessage
            try
            {
                MessageHelper.SuppressDialogs = true;
                MessageHelper.AutoConfirmResult = false;
                Assert.False(MessageHelper.ConfirmDelete("nhân viên NV01"));
                Assert.Equal("Bạn có chắc chắn muốn xóa nhân viên NV01 không?", MessageHelper.LastMessage);

                MessageHelper.AutoConfirmResult = true;
                Assert.True(MessageHelper.Confirm("Bạn có muốn thoát không?"));
                MessageHelper.Success("thêm mới");
                Assert.Equal("Thao tác thêm mới thành công!", MessageHelper.LastMessage);
            }
            finally
            {
                MessageHelper.SuppressDialogs = false;
                MessageHelper.AutoConfirmResult = true;
            }
        }

        #endregion

        #region 9. Kiểm thử Multi-DB, Validator.FromSchema, Report đa thống kê & FormGenericManager

        [Fact]
        public void MultiDb_Dialects_FilterBuilder_And_QuoteIdentifier_WorkAcrossEngines()
        {
            Assert.Equal(DbDialect.MySql, DbHelper.DetectDialect("Server=localhost;Database=shop;Uid=root;Pwd=123;", null));
            Assert.Equal(DbDialect.Sqlite, DbHelper.DetectDialect("Data Source=C:\\data\\exam.db;Mode=ReadWriteCreate", null));
            Assert.Equal(DbDialect.PostgreSql, DbHelper.DetectDialect("Host=localhost;Port=5432;Database=exam;Username=pg", null));
            Assert.Equal(DbDialect.OleDb, DbHelper.DetectDialect("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=db.accdb;", null));
            Assert.Equal(DbDialect.Odbc, DbHelper.DetectDialect("Driver={MySQL ODBC 8.0};Server=localhost;", null));
            Assert.Equal(DbDialect.SqlServer, DbHelper.DetectDialect("Server=(localdb)\\MSSQLLocalDB;Database=QLNhanSu;Trusted_Connection=True;", null));

            Assert.Equal("`nv`.`HoTen`", DbHelper.QuoteIdentifier("nv.HoTen", DbDialect.MySql));
            Assert.Equal("\"nv\".\"HoTen\"", DbHelper.QuoteIdentifier("nv.HoTen", DbDialect.Sqlite));
            Assert.Equal("\"nv\".\"HoTen\"", DbHelper.QuoteIdentifier("nv.HoTen", DbDialect.PostgreSql));
            Assert.Equal("[nv].[HoTen]", DbHelper.QuoteIdentifier("nv.HoTen", DbDialect.SqlServer));

            Assert.Equal("SELECT last_insert_rowid()", DbHelper.GetLastInsertIdSql(DbDialect.Sqlite));
            Assert.Equal("SELECT LAST_INSERT_ID()", DbHelper.GetLastInsertIdSql(DbDialect.MySql));
            Assert.Equal("SELECT LASTVAL()", DbHelper.GetLastInsertIdSql(DbDialect.PostgreSql));
            Assert.Equal("SELECT @@IDENTITY", DbHelper.GetLastInsertIdSql(DbDialect.OleDb));

            var (mySqlWhere, _) = FilterBuilder.New(DbDialect.MySql)
                .Equal("MaPB", "IT")
                .ContainsAny(new[] { "nv.HoTen", "MaNV" }, "An")
                .Build();
            Assert.Equal(" WHERE `MaPB` = @f0 AND (`nv`.`HoTen` LIKE @f1 OR `MaNV` LIKE @f1)", mySqlWhere);

            try
            {
                DbHelper.SetConnectionString("test_pg", "Host=localhost;Port=5432;Database=app;Username=u", dialect: DbDialect.PostgreSql);
                var (pgWhere, _) = FilterBuilder.ForConnection("test_pg").Equal("TenSP", "Laptop").Build();
                Assert.Equal(" WHERE \"TenSP\" = @f0", pgWhere);
            }
            finally
            {
                DbHelper.SetConnectionString("test_pg", null);
            }
        }

        [Fact]
        public void DbHelper_MockProvider_HandlesNullAdapterFallback_OleDbPositionalRewrite_And_SchemaDiscovery()
        {
            var products = new DataTable("SanPham");
            products.Columns.Add(new DataColumn("MaSP", typeof(string)) { MaxLength = 10, AllowDBNull = false });
            products.Columns.Add(new DataColumn("TenSP", typeof(string)) { MaxLength = 120, AllowDBNull = false });
            products.Columns.Add(new DataColumn("DonGia", typeof(decimal)) { AllowDBNull = false });
            products.Columns.Add(new DataColumn("NgayNhap", typeof(DateTime)) { AllowDBNull = true });
            products.Columns.Add(new DataColumn("ConHang", typeof(bool)) { AllowDBNull = false });
            products.PrimaryKey = new[] { products.Columns["MaSP"] };
            products.Rows.Add("SP01", "Laptop A", 15000000m, new DateTime(2026, 5, 1), true);
            products.Rows.Add("SP02", "Chuột B", 250000m, DBNull.Value, false);

            var orders = new DataTable("DonHang");
            orders.Columns.Add(new DataColumn("MaDH", typeof(int)) { AutoIncrement = true, AllowDBNull = false });
            orders.Columns.Add(new DataColumn("GhiChu", typeof(string)) { MaxLength = 200, AllowDBNull = true });
            orders.PrimaryKey = new[] { orders.Columns["MaDH"] };
            orders.Rows.Add(1, "Giao gấp");

            var factory = new MockDbProviderFactory(new Dictionary<string, DataTable>
            {
                { "SanPham", products },
                { "DonHang", orders }
            });

            try
            {
                // 1. OleDb dialect: adapter == null -> reader fallback + @param -> ? rewrite (không đụng chuỗi trong '...')
                DbHelper.SetConnectionString("mock_ole", "Provider=Mock;Data Source=test.accdb;", factory, DbDialect.OleDb);
                DataTable loaded = DbHelper.GetDataTable(
                    "SELECT * FROM [SanPham] WHERE Email <> 'a@b.c' AND MaSP = @id AND DonGia > @min AND MaSP <> @id",
                    "mock_ole",
                    DbHelper.Param("id", "SP01"),
                    DbHelper.Param("min", 1000m));

                Assert.Equal(2, loaded.Rows.Count);
                Assert.Equal("SELECT * FROM [SanPham] WHERE Email <> 'a@b.c' AND MaSP = ? AND DonGia > ? AND MaSP <> ?", factory.LastCommandText);
                Assert.Equal(3, factory.LastParameters.Count);
                Assert.Equal("SP01", factory.LastParameters[0].Value);
                Assert.Equal(1000m, factory.LastParameters[1].Value);
                Assert.Equal("SP01", factory.LastParameters[2].Value);

                // 2. GetTableNames & GetTableSchema hoạt động trên provider generic
                List<string> tables = DbHelper.GetTableNames("mock_ole");
                Assert.Contains("SanPham", tables);
                Assert.Contains("DonHang", tables);

                DataTable schema = DbHelper.GetTableSchema("SanPham", "mock_ole");
                Assert.Equal(5, schema.Columns.Count);
                Assert.Single(schema.PrimaryKey);
                Assert.Equal("MaSP", schema.PrimaryKey[0].ColumnName);
                Assert.False(schema.Columns["MaSP"].AllowDBNull);
                Assert.Equal(10, schema.Columns["MaSP"].MaxLength);

                // 3. FormGenericManager tự dựng giao diện theo bất kỳ bảng/schema nào và chuyển bảng động
                using (var mgr = new FormGenericManager("SanPham", "mock_ole"))
                {
                    Assert.Equal("SanPham", mgr.CurrentTable);
                    Assert.IsType<TextBox>(mgr.FindFieldControl("MaSP"));
                    Assert.IsType<DateTimePicker>(mgr.FindFieldControl("NgayNhap"));
                    Assert.IsType<CheckBox>(mgr.FindFieldControl("ConHang"));
                    Assert.Equal(10, ((TextBox)mgr.FindFieldControl("MaSP")).MaxLength);
                    Assert.True(((DateTimePicker)mgr.FindFieldControl("NgayNhap")).ShowCheckBox);
                    Assert.Equal(2, mgr.Grid.Rows.Count);

                    Assert.True(mgr.SelectTable("DonHang"));
                    Assert.Equal("DonHang", mgr.CurrentTable);
                    Assert.True(((TextBox)mgr.FindFieldControl("MaDH")).ReadOnly);
                    Assert.Null(mgr.FindFieldControl("MaSP"));
                }
            }
            finally
            {
                DbHelper.SetConnectionString("mock_ole", null);
            }
        }

        [Fact]
        public void Validator_FromSchema_EnforcesRequiredMaxLengthTypes_And_SkipsAutoIncrement()
        {
            var schema = new DataTable("HangHoa");
            schema.Columns.Add(new DataColumn("Id", typeof(int)) { AutoIncrement = true, AllowDBNull = false });
            schema.Columns.Add(new DataColumn("MaHH", typeof(string)) { MaxLength = 5, AllowDBNull = false });
            schema.Columns.Add(new DataColumn("SoLuong", typeof(int)) { AllowDBNull = false });
            schema.Columns.Add(new DataColumn("DonGia", typeof(decimal)) { AllowDBNull = true });

            using (var container = new Panel())
            {
                var txtId = new TextBox { Name = "txtId", Text = "" };
                var txtMa = new TextBox { Name = "txtMaHH", Text = "" };
                var txtSl = new TextBox { Name = "txtSoLuong", Text = "10" };
                var txtGia = new TextBox { Name = "txtDonGia", Text = "50000" };
                container.Controls.AddRange(new Control[] { txtId, txtMa, txtSl, txtGia });

                Assert.False(Validator.FromSchema(container, schema).IsValid);

                txtMa.Text = "TOOLONG123";
                Assert.False(Validator.FromSchema(container, schema).IsValid);

                txtMa.Text = "HH01";
                txtSl.Text = "abc";
                Assert.False(Validator.FromSchema(container, schema).IsValid);

                txtSl.Text = "10";
                txtGia.Text = "khong_phai_so";
                Assert.False(Validator.FromSchema(container, schema).IsValid);

                txtGia.Text = "125000";
                Assert.True(Validator.FromSchema(container, schema).IsValid);
                Assert.False(Validator.FromSchema(container, schema,
                    Validator.Custom(txtSl, "Số lượng", v => int.Parse(v) > 100, "Phải > 100")).IsValid);
            }
        }

        [Fact]
        public void ReportHelper_And_FormReportViewer_SupportMultiSummaryModes_Landscape_And_FontSize()
        {
            var dt = new DataTable();
            dt.Columns.Add("MaNV", typeof(string));
            dt.Columns.Add("Phong", typeof(string));
            dt.Columns.Add("Luong", typeof(decimal));
            dt.Columns.Add("Diem", typeof(decimal));
            dt.Rows.Add("NV01", "IT", 1000m, 8m);
            dt.Rows.Add("NV02", "IT", 2000m, 10m);
            dt.Rows.Add("NV03", "HR", 3000m, 6m);

            var cols = new[]
            {
                new ReportColumn("MaNV", "Mã NV", null, HorizontalAlignment.Center, ReportSummaryMode.Count),
                new ReportColumn("Luong", "Lương TB", "N0", HorizontalAlignment.Right, ReportSummaryMode.Average),
                new ReportColumn("Diem", "Điểm Max", "0.#", HorizontalAlignment.Right, ReportSummaryMode.Max)
            };

            string html = ReportHelper.GenerateHtml(dt, "THỐNG KÊ ĐA CHẾ ĐỘ", groupByColumn: "Phong",
                columns: cols, showGroupTotals: true, landscape: true, fontSizePt: 16);

            Assert.Contains("@page { size: A4 landscape;", html);
            Assert.Contains("font-size: 16px;", html);
            Assert.Contains("Đếm: 3", html);
            Assert.Contains("TB: 2,000", html);
            Assert.Contains("Max: 10", html);
            Assert.Contains("Thống kê IT:", html);
            Assert.Contains("THỐNG KÊ", html);

            byte[] xlsx = ReportHelper.BuildXlsxBytes(dt, "THỐNG KÊ ĐA CHẾ ĐỘ", columns: cols,
                landscape: true, fontSizePt: 16);
            using (var ms = new MemoryStream(xlsx))
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                string sheetXml;
                using (var sr = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml").Open()))
                    sheetXml = sr.ReadToEnd();
                string stylesXml;
                using (var sr = new StreamReader(zip.GetEntry("xl/styles.xml").Open()))
                    stylesXml = sr.ReadToEnd();

                Assert.Contains("orientation=\"landscape\"", sheetXml);
                Assert.Contains("TB: 2,000", sheetXml);
                Assert.Contains("<sz val=\"14\"/>", stylesXml);
            }

            // Kiểm chứng control thực tế trên FormReportViewer trong luồng STA
            Exception capturedEx = null;
            string htmlAfterFormEdit = null;
            var sta = new System.Threading.Thread(() =>
            {
                try
                {
                    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    using (var viewer = new FormReportViewer(dt, new ReportDesignOptions { ReportTitle = "TEST MODES", OpenDesigner = true }))
                    using (var timer = new System.Windows.Forms.Timer { Interval = 100 })
                    {
                        timer.Tick += (s, e) =>
                        {
                            timer.Stop();
                            var cboPaper = (ComboBox)typeof(FormReportViewer).GetField("_cboPaperOrientation", flags).GetValue(viewer);
                            var nudFont = (NumericUpDown)typeof(FormReportViewer).GetField("_nudFontSize", flags).GetValue(viewer);
                            var cboSummary = (ComboBox)typeof(FormReportViewer).GetField("_cboColSummary", flags).GetValue(viewer);
                            var lstVis = (ListBox)typeof(FormReportViewer).GetField("_lstVisible", flags).GetValue(viewer);

                            int luongIdx = -1;
                            for (int i = 0; i < lstVis.Items.Count; i++)
                            {
                                if (((ReportColumn)lstVis.Items[i]).ColumnName == "Luong") { luongIdx = i; break; }
                            }
                            Assert.True(luongIdx >= 0);
                            lstVis.SelectedIndex = luongIdx;
                            cboSummary.SelectedIndex = (int)ReportSummaryMode.Average;
                            cboPaper.SelectedIndex = 1; // A4 Ngang
                            nudFont.Value = 15;

                            typeof(FormReportViewer).GetMethod("EnsurePrintable", flags).Invoke(viewer, null);
                            htmlAfterFormEdit = viewer.CurrentHtml;
                            viewer.Close();
                        };
                        viewer.Shown += (s, e) => timer.Start();
                        Application.Run(viewer);
                    }
                }
                catch (Exception ex) { capturedEx = ex; }
            });
            sta.SetApartmentState(System.Threading.ApartmentState.STA);
            sta.Start();
            Assert.True(sta.Join(10000));
            Assert.Null(capturedEx);
            Assert.Contains("size: A4 landscape", htmlAfterFormEdit);
            Assert.Contains("font-size: 15px;", htmlAfterFormEdit);
            Assert.Contains("TB: 2,000", htmlAfterFormEdit);
        }

        [Fact]
        public void ReportTemplate_SaveLoadAndApply_PreservesOrderHiddenColumnsAndFormatting()
        {
            var dt = new DataTable();
            dt.Columns.Add("MaNV", typeof(string));
            dt.Columns.Add("HoTen", typeof(string));
            dt.Columns.Add("TenPB", typeof(string));
            dt.Columns.Add("Luong", typeof(decimal));
            dt.Rows.Add("NV01", "Nguyễn An", "Kinh doanh", 12000000m);

            string tempPath = Path.Combine(Path.GetTempPath(), "examhelper_tpl_" + Guid.NewGuid().ToString("N") + ".examrpt");
            try
            {
                using (var viewer = new FormReportViewer(dt, new ReportDesignOptions { ReportTitle = "BẢNG LƯƠNG CŨ", OpenDesigner = false }))
                {
                    viewer.ReportTitle = "BẢNG LƯƠNG ĐÃ LƯU";
                    viewer.ReportCompanyName = "CÔNG TY MẪU";
                    viewer.Landscape = true;
                    viewer.FontSizePt = 15;
                    viewer.SetGroupByColumn("TenPB");
                    viewer.HideColumn("MaNV");
                    var colLuong = viewer.VisibleColumns.First(c => c.ColumnName == "Luong");
                    colLuong.HeaderText = "Thực lãnh";
                    colLuong.SummaryMode = ReportSummaryMode.Sum;
                    viewer.MoveVisibleColumn("Luong", 0);

                    Assert.True(viewer.SaveTemplateFile(tempPath));
                }

                Assert.True(File.Exists(tempPath));
                var loaded = ReportHelper.LoadTemplate(tempPath);
                Assert.Equal("BẢNG LƯƠNG ĐÃ LƯU", loaded.ReportTitle);
                Assert.True(loaded.Landscape);
                Assert.Equal(15, loaded.FontSizePt);
                Assert.Equal("TenPB", loaded.GroupByColumn);

                // Dữ liệu mới có thêm 1 cột chưa có trong template
                var dtNew = dt.Copy();
                dtNew.Columns.Add("GhiChu", typeof(string));

                using (var reopened = new FormReportViewer(dtNew, loaded.ToDesignOptions(dtNew, "Phòng: Tất cả", openDesigner: false)))
                {
                    Assert.Equal("Phòng: Tất cả", reopened.FilterCondition);
                    Assert.Equal("BẢNG LƯƠNG ĐÃ LƯU", reopened.ReportTitle);
                    Assert.True(reopened.Landscape);
                    Assert.Equal(15, reopened.FontSizePt);
                    Assert.Equal("Luong", reopened.VisibleColumns[0].ColumnName);
                    Assert.Equal("Thực lãnh", reopened.VisibleColumns[0].HeaderText);
                    Assert.Equal(ReportSummaryMode.Sum, reopened.VisibleColumns[0].SummaryMode);
                    Assert.Contains(reopened.HiddenColumns, c => c.ColumnName == "MaNV");
                    Assert.Contains(reopened.HiddenColumns, c => c.ColumnName == "GhiChu");
                }
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        [Fact]
        public void ReportHelper_GenerateRdlcXml_ProducesValidRdl2008SchemaWithGroupingAndSummaries()
        {
            var dt = new DataTable("NhanVien");
            dt.Columns.Add("MaNV", typeof(string));
            dt.Columns.Add("Họ tên", typeof(string)); // Kiểm tra chuẩn hóa tên Field CLS
            dt.Columns.Add("TenPB", typeof(string));
            dt.Columns.Add("Luong", typeof(decimal));
            dt.Rows.Add("NV01", "Trần Bình", "Kỹ thuật", 15000000m);

            var cols = new[]
            {
                new ReportColumn("MaNV", "Mã NV", HorizontalAlignment.Center),
                new ReportColumn("Họ tên", "Họ và tên"),
                new ReportColumn("Luong", "Mức lương", "N0", HorizontalAlignment.Right, ReportSummaryMode.Sum)
            };

            string xml = ReportHelper.GenerateRdlcXml(
                dt, "BẢNG LƯƠNG THỬ NGHIỆM", "Phòng: Tất cả",
                groupByColumn: "TenPB", groupPrefix: "Phòng: ",
                columns: cols, companyName: "ĐƠN VỊ TEST",
                showGroupTotals: true, landscape: true, dataSetName: "dsNhanVien");

            XDocument doc = XDocument.Parse(xml);
            XNamespace ns = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

            Assert.Equal("29.7cm", doc.Descendants(ns + "PageWidth").Single().Value);
            Assert.Equal("21cm", doc.Descendants(ns + "PageHeight").Single().Value);
            Assert.Equal("dsNhanVien", doc.Descendants(ns + "DataSet").Single().Attribute("Name")?.Value);

            var hoTenField = doc.Descendants(ns + "Field").First(f => f.Element(ns + "DataField")?.Value == "Họ tên");
            string hoTenCls = hoTenField.Attribute("Name")?.Value;
            Assert.DoesNotContain(" ", hoTenCls);

            Assert.Contains("=Fields!TenPB.Value", doc.Descendants(ns + "GroupExpression").Select(x => x.Value));
            Assert.Contains("=Sum(Fields!Luong.Value)", xml);
            Assert.Contains("=RowNumber(Nothing)", xml);
            Assert.Contains("#,##0", xml);

            // Kiểm tra tên Textbox duy nhất toàn tài liệu và khớp số hàng/cột Tablix
            var tbNames = doc.Descendants(ns + "Textbox").Select(t => t.Attribute("Name")?.Value).ToList();
            Assert.Equal(tbNames.Count, tbNames.Distinct(StringComparer.Ordinal).Count());

            var tablix = doc.Descendants(ns + "Tablix").Single();
            int tablixRowsCount = tablix.Element(ns + "TablixBody").Element(ns + "TablixRows").Elements(ns + "TablixRow").Count();
            int leafRowMembers = tablix.Element(ns + "TablixRowHierarchy")
                .Descendants(ns + "TablixMember")
                .Count(m => m.Element(ns + "TablixMembers") == null);
            Assert.Equal(tablixRowsCount, leafRowMembers);

            int tablixColsCount = tablix.Element(ns + "TablixBody").Element(ns + "TablixColumns").Elements(ns + "TablixColumn").Count();
            int colMembers = tablix.Element(ns + "TablixColumnHierarchy").Element(ns + "TablixMembers").Elements(ns + "TablixMember").Count();
            Assert.Equal(tablixColsCount, colMembers);

            string tempRdlc = Path.Combine(Path.GetTempPath(), "examhelper_test_" + Guid.NewGuid().ToString("N") + ".rdlc");
            try
            {
                Assert.True(ReportHelper.ExportRdlcFile(dt, "BẢNG LƯƠNG", columns: cols, filePath: tempRdlc));
                Assert.True(File.Exists(tempRdlc));
                Assert.Contains("tblReportData", File.ReadAllText(tempRdlc));
            }
            finally
            {
                if (File.Exists(tempRdlc)) File.Delete(tempRdlc);
            }
        }

        [Fact]
        public void FormReportViewer_InteractivePreview_KeepsCurrentHtmlClean_And_AttachesScriptingBridge()
        {
            var dt = CreateSampleDataTable();
            string plainHtml = ReportHelper.GenerateHtml(dt, "TĨNH");
            string interactiveHtml = ReportHelper.GenerateHtmlCore(dt, "TƯƠNG TÁC", null, null, null, null,
                "ĐƠN VỊ", "Người ký", false, "Trang 1/1", "dd/MM/yyyy", true, false, 13, interactivePreview: true);

            Assert.DoesNotContain("notifyHost", plainHtml);
            Assert.Contains("data-edit='title'", interactiveHtml);
            Assert.Contains("data-col='MaNV'", interactiveHtml);
            Assert.Contains("notifyHost('col:MaNV')", interactiveHtml);

            Exception capturedEx = null;
            object scriptingBridge = null;
            string currentHtmlSnapshot = null;
            int tabCount = 0;

            var sta = new System.Threading.Thread(() =>
            {
                try
                {
                    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    using (var viewer = new FormReportViewer(dt, new ReportDesignOptions { ReportTitle = "BÁO CÁO MỚI", OpenDesigner = true }))
                    using (var timer = new System.Windows.Forms.Timer { Interval = 120 })
                    {
                        timer.Tick += (s, e) =>
                        {
                            timer.Stop();
                            var web = (WebBrowser)typeof(FormReportViewer).GetField("_webViewer", flags).GetValue(viewer);
                            var tabs = (TabControl)typeof(FormReportViewer).GetField("_designerTabs", flags).GetValue(viewer);
                            scriptingBridge = web?.ObjectForScripting;
                            tabCount = tabs?.TabPages.Count ?? 0;
                            currentHtmlSnapshot = viewer.CurrentHtml;
                            viewer.Close();
                        };
                        viewer.Shown += (s, e) => timer.Start();
                        Application.Run(viewer);
                    }
                }
                catch (Exception ex) { capturedEx = ex; }
            });
            sta.SetApartmentState(System.Threading.ApartmentState.STA);
            sta.Start();
            Assert.True(sta.Join(10000));
            Assert.Null(capturedEx);
            Assert.Equal(2, tabCount);
            Assert.IsType<ReportPreviewBridge>(scriptingBridge);
            Assert.DoesNotContain("notifyHost", currentHtmlSnapshot);
        }

        [Fact]
        public void ReportHelper_ExtractTable_WithGroupByColumn_HidesGroupedColumnAndPreservesHeader()
        {
            var dt = CreateSampleDataTable();
            dt.Columns.Add("TenPB", typeof(string));
            foreach (DataRow r in dt.Rows) r["TenPB"] = "Kinh doanh";

            using (var dgv = new DataGridView())
            {
                GridHelper.Bind(dgv, dt,
                    new GridHelper.GridColumn("MaNV", "Mã NV", 90),
                    new GridHelper.GridColumn("HoTen", "Họ tên", 150),
                    new GridHelper.GridColumn("TenPB", "Phòng ban", 120),
                    new GridHelper.GridColumn("Luong", "Lương", 110, "N0", DataGridViewContentAlignment.MiddleRight));

                DataTable extracted = ReportHelper.ExtractTable(
                    dgv, out ReportDesignOptions opts, "BÁO CÁO", null, "TenPB", "Phòng ban: ");

                using (var viewer = new FormReportViewer(extracted, opts))
                {
                    Assert.DoesNotContain(viewer.VisibleColumns, c => c.ColumnName == "TenPB");
                    var hiddenGroup = viewer.HiddenColumns.FirstOrDefault(c => c.ColumnName == "TenPB");
                    Assert.NotNull(hiddenGroup);
                    Assert.Equal("Phòng ban", hiddenGroup.HeaderText);
                }
            }
        }

        #endregion
    }

    #region Mock DbProviderFactory for Multi-DB & Generic Schema Testing

    internal sealed class MockDbProviderFactory : DbProviderFactory
    {
        private readonly Dictionary<string, DataTable> _tables;
        public string LastCommandText { get; private set; }
        public List<DbParameter> LastParameters { get; } = new List<DbParameter>();

        public MockDbProviderFactory(Dictionary<string, DataTable> tables)
        {
            _tables = tables;
        }

        public override DbConnection CreateConnection() => new MockDbConnection(this);
        public override DbCommand CreateCommand() => new MockDbCommand(this);
        public override DbDataAdapter CreateDataAdapter() => null; // Cố tình trả null giống Microsoft.Data.Sqlite

        internal DataTable ResolveTableForSql(string sql)
        {
            foreach (var kvp in _tables)
            {
                if (sql != null && sql.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return kvp.Value;
            }
            return _tables.Values.FirstOrDefault() ?? new DataTable();
        }

        internal DataTable GetTablesCatalog()
        {
            var dt = new DataTable("Tables");
            dt.Columns.Add("TABLE_NAME", typeof(string));
            dt.Columns.Add("TABLE_TYPE", typeof(string));
            foreach (var name in _tables.Keys)
                dt.Rows.Add(name, "TABLE");
            return dt;
        }

        internal void RecordExecution(string sql, DbParameterCollection parms)
        {
            LastCommandText = sql;
            LastParameters.Clear();
            foreach (DbParameter p in parms)
                LastParameters.Add(new MockDbParameter { ParameterName = p.ParameterName, Value = p.Value });
        }
    }

    internal sealed class MockDbConnection : DbConnection
    {
        private readonly MockDbProviderFactory _factory;
        private ConnectionState _state = ConnectionState.Closed;

        public MockDbConnection(MockDbProviderFactory factory) => _factory = factory;
        public override string ConnectionString { get; set; }
        public override string Database => "MockDB";
        public override string DataSource => "MockHost";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() => _state = ConnectionState.Closed;
        public override void Open() => _state = ConnectionState.Open;
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new MockDbCommand(_factory) { Connection = this };
        public override DataTable GetSchema(string collectionName)
            => string.Equals(collectionName, "Tables", StringComparison.OrdinalIgnoreCase) ? _factory.GetTablesCatalog() : new DataTable();
    }

    internal sealed class MockDbCommand : DbCommand
    {
        private readonly MockDbProviderFactory _factory;
        private readonly MockDbParameterCollection _parms = new MockDbParameterCollection();

        public MockDbCommand(MockDbProviderFactory factory) => _factory = factory;
        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; } = CommandType.Text;
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => _parms;
        protected override DbTransaction DbTransaction { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override void Cancel() { }
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new MockDbParameter();

        public override int ExecuteNonQuery()
        {
            _factory.RecordExecution(CommandText, _parms);
            return 1;
        }

        public override object ExecuteScalar()
        {
            _factory.RecordExecution(CommandText, _parms);
            return 1;
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            _factory.RecordExecution(CommandText, _parms);
            DataTable src = _factory.ResolveTableForSql(CommandText);
            return (behavior & CommandBehavior.SchemaOnly) == CommandBehavior.SchemaOnly
                ? src.Clone().CreateDataReader()
                : src.CreateDataReader();
        }
    }

    internal sealed class MockDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; }
        public override int Size { get; set; }
        public override string SourceColumn { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override object Value { get; set; }
        public override void ResetDbType() { }
    }

    internal sealed class MockDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = new List<DbParameter>();
        public override int Count => _items.Count;
        public override object SyncRoot => ((ICollection)_items).SyncRoot;
        public override int Add(object value) { _items.Add((DbParameter)value); return _items.Count - 1; }
        public override void AddRange(Array values) { foreach (DbParameter p in values) _items.Add(p); }
        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains((DbParameter)value);
        public override bool Contains(string value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _items.FindIndex(p => string.Equals(p.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase));
        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) { int i = IndexOf(parameterName); if (i >= 0) _items.RemoveAt(i); }
        protected override DbParameter GetParameter(int index) => _items[index];
        protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) { int i = IndexOf(parameterName); if (i >= 0) _items[i] = value; }
    }

    #endregion

    #region Mock RDLC & Crystal Types for Reflection Testing

    public class MockCrystalReportDocument
    {
        public DataTable DataSource { get; private set; }
        public Dictionary<string, object> Params { get; } = new Dictionary<string, object>();
        public void SetDataSource(DataTable dt) => DataSource = dt;
        public void SetParameterValue(string name, object val) => Params[name] = val;
    }

    public class MockCrystalReportViewer
    {
        public object ReportSource { get; set; }
        public bool Refreshed { get; private set; }
        public void RefreshReport() => Refreshed = true;
    }

    public class ReportDataSource
    {
        public string Name { get; }
        public object Value { get; }
        public ReportDataSource(string name, object value) { Name = name; Value = value; }
    }

    public class ReportParameter
    {
        public string Name { get; }
        public string Value { get; }
        public ReportParameter(string name, string value) { Name = name; Value = value; }
    }

    public class MockLocalReport
    {
        public string ReportEmbeddedResource { get; set; }
        public string ReportPath { get; set; }
        public List<ReportDataSource> DataSources { get; } = new List<ReportDataSource>();
        public List<ReportParameter> Parameters { get; } = new List<ReportParameter>();

        public void SetParameters(IEnumerable<ReportParameter> parameters)
        {
            Parameters.Clear();
            Parameters.AddRange(parameters);
        }

        public byte[] Render(string format) => new byte[] { 1, 2, 3 };
    }

    public class MockReportViewer
    {
        public MockLocalReport LocalReport { get; } = new MockLocalReport();
        public bool Refreshed { get; private set; }
        public void RefreshReport() => Refreshed = true;
    }

    #endregion
}

