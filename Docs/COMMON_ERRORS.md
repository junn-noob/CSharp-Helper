# CÁC LỖI THƯỜNG GẶP TRONG PHÒNG THI & CÁCH XỬ LÝ NHANH

Tổng hợp những lỗi 90% sinh viên gặp phải khi thi thực hành C# WinForms và cách ExamHelper giúp bạn giải quyết triệt để.

---

### Lỗi 1: Lỗi kết nối CSDL (Cannot open database / Login failed)
* **Nguyên nhân:** Tên máy chủ SQL Server sai hoặc tên Database gõ sai chính tả.
* **Cách khắc phục:**
  - Nếu máy phòng thi dùng Windows Authentication:
    ```csharp
    DbHelper.SetDefaultConnectionString("Server=.;Database=TenDB;Trusted_Connection=True;");
    // hoặc
    DbHelper.SetDefaultConnectionString("Server=localhost;Database=TenDB;Trusted_Connection=True;");
    // hoặc dùng SQLEXPRESS
    DbHelper.SetDefaultConnectionString(@"Server=.\SQLEXPRESS;Database=TenDB;Trusted_Connection=True;");
    ```
  - Nếu máy phòng thi yêu cầu tài khoản `sa`:
    ```csharp
    DbHelper.SetDefaultConnectionString("Server=.;Database=TenDB;User Id=sa;Password=matkhau;");
    ```

---

### Lỗi 2: DataGridView bị nhân đôi / nạp chồng cột
* **Nguyên nhân:** Giao diện đã kéo sẵn cột trong Designer nhưng khi code lại gán `DataSource` trực tiếp khiến DataGridView tự sinh thêm cột.
* **Cách khắc phục:**
  - `GridHelper.Bind(...)` đã tự động xử lý `AutoGenerateColumns = false` và xóa các cột cũ trước khi cấu hình:
    ```csharp
    GridHelper.Bind(dgvNhanVien, dt,
        new GridHelper.GridColumn("MaNV", "Mã nhân viên", 100),
        new GridHelper.GridColumn("HoTen", "Họ và tên", 180)
    );
    ```

---

### Lỗi 3: Bấm Sửa nhưng báo "Mã đã tồn tại"
* **Nguyên nhân:** Khi nhấn nút Lưu, sinh viên kiểm tra trùng mã `SELECT COUNT(*) WHERE MaNV = ...` nhưng quên mất bản ghi này đang tồn tại trong bảng (chính là bản ghi đang sửa).
* **Cách khắc phục:**
  - Phân tách rõ ràng giữa Thêm mới và Cập nhật:
    ```csharp
    if (_isAdding)
    {
        // Chỉ kiểm tra trùng mã khi Thêm mới
        if (CrudHelper.Exists("NhanVien", "MaNV", maNV))
        {
            MessageHelper.DuplicateKey("Mã nhân viên", maNV);
            return;
        }
        CrudHelper.Insert("NhanVien", values);
    }
    else
    {
        // Khi Sửa: không cho sửa ô Mã (txtMaNV.ReadOnly = true)
        // và loại bỏ cột khóa chính ra khỏi danh sách cập nhật
        values.Remove("MaNV");
        CrudHelper.Update("NhanVien", values, "MaNV", maNV);
    }
    ```

---

### Lỗi 4: Lỗi NullReferenceException khi bấm Sửa/Xóa khi bảng chưa có dữ liệu
* **Nguyên nhân:** Bảng rỗng hoặc người dùng chưa chọn dòng nào trên DataGridView mà đã bấm nút Sửa hoặc Xóa, dẫn tới `dgv.CurrentRow == null`.
* **Cách khắc phục:**
  - Luôn kiểm tra bằng `GridHelper.HasSelection(dgv)` ở dòng đầu tiên của sự kiện:
    ```csharp
    if (!GridHelper.HasSelection(dgvNhanVien))
    {
        MessageHelper.NoSelection("nhân viên");
        return;
    }
    ```

---

### Lỗi 5: Lỗi tính sai tuổi (Tính theo năm trừ năm)
* **Nguyên nhân:** Viết `DateTime.Now.Year - dtpNgaySinh.Value.Year`. Nếu bạn sinh tháng 12/2000, vào tháng 2/2018 bạn mới chỉ 17 tuổi, nhưng phép trừ ra 18 tuổi. Giám khảo chấm thi kiểm tra dữ liệu biên sẽ trừ điểm.
* **Cách khắc phục:**
  - Sử dụng hàm chuẩn của ExamHelper:
    ```csharp
    // Tự động kiểm tra chính xác ngày sinh đã qua trong năm hay chưa
    int age = Validator.CalculateAge(dtpNgaySinh.Value, dtpNgayVaoLam.Value);
    
    // Hoặc kiểm tra tự động qua bộ rule:
    Validator.AgeRangeAtDate(dtpNgaySinh, "Tuổi nhân viên", 18, 60);
    ```

---

### Lỗi 6: Lỗi ComboBox `SelectedValue` bị null hoặc ép kiểu thất bại
* **Nguyên nhân:** Khi ComboBox đang nạp `DataSource`, sự kiện `SelectedIndexChanged` được kích hoạt khi `SelectedValue` vẫn là kiểu `DataRowView`.
* **Cách khắc phục:**
  - Dùng `ComboBoxHelper.GetSelectedValue<string>(cbo)` hoặc `ComboBoxHelper.GetSelectedValue<int>(cbo)`. Hàm này đã tích hợp sẵn try-catch và kiểm tra null an toàn.

---

### Lỗi 7: SQL Injection hoặc lỗi dấu nháy đơn khi tên có ký tự đặc biệt
* **Nguyên nhân:** Viết SQL nối chuỗi: `"SELECT * FROM NhanVien WHERE HoTen LIKE '%" + txtTen.Text + "%'"`. Nếu người nhập `O'Reilly`, câu SQL bị vỡ cú pháp.
* **Cách khắc phục:**
  - Dùng `FilterBuilder`:
    ```csharp
    var (where, parms) = FilterBuilder.New()
        .Contains("HoTen", txtTen.Text)
        .Build();
    var dt = DbHelper.GetDataTable("SELECT * FROM NhanVien" + where, parms);
    ```
    Toàn bộ giá trị đều được truyền qua `SqlParameter`, hoàn toàn an toàn và không bao giờ bị lỗi nháy đơn.
