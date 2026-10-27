# 🎯 3 BỘ ĐỀ THI MẪU LUYỆN TẬP KÈM CSDL SQL SERVER

Tài liệu này cung cấp 3 bộ đề thi thực hành WinForms & SQL Server (thời lượng 75 - 90 phút) với **nghiệp vụ khác biệt, CSDL khác biệt và các ràng buộc nghiệp vụ đa dạng** để bạn tự luyện tập áp dụng thư viện `ExamHelper Pro`.

---

## 📋 ĐỀ SỐ 02: HỆ THỐNG QUẢN LÝ PHÒNG KHÁM & BÁC SĨ (QLPhongKham)

### 1. Script Khởi Tạo Cơ Sở Dữ Liệu
Chạy script sau trên SQL Server Management Studio (SSMS) để tạo CSDL `QLPhongKham`:

```sql
CREATE DATABASE QLPhongKham;
GO
USE QLPhongKham;
GO

CREATE TABLE ChuyenKhoa (
    MaCK NVARCHAR(10) PRIMARY KEY,
    TenCK NVARCHAR(100) NOT NULL
);

CREATE TABLE BacSi (
    MaBS NVARCHAR(10) PRIMARY KEY,
    HoTen NVARCHAR(50) NOT NULL,
    GioiTinh BIT NOT NULL, -- 1: Nam, 0: Nữ
    NgaySinh DATE NOT NULL,
    MaCK NVARCHAR(10) FOREIGN KEY REFERENCES ChuyenKhoa(MaCK),
    DonGiaKham DECIMAL(18,0) NOT NULL,
    NgayVaoLam DATE NOT NULL
);

-- Dữ liệu mẫu
INSERT INTO ChuyenKhoa VALUES 
(N'CK01', N'Nội tổng quát'), 
(N'CK02', N'Răng Hàm Mặt'), 
(N'CK03', N'Tai Mũi Họng'),
(N'CK04', N'Nhi khoa');

INSERT INTO BacSi VALUES
(N'BS001', N'Nguyễn Hoài Nam', 1, '1985-04-12', N'CK01', 150000, '2015-01-10'),
(N'BS002', N'Trần Thu Hà', 0, '1992-08-20', N'CK02', 200000, '2019-06-01'),
(N'BS003', N'Lê Quốc Anh', 1, '1988-11-05', N'CK03', 180000, '2016-09-15'),
(N'BS004', N'Phạm Mỹ Linh', 0, '1995-02-14', N'CK04', 250000, '2021-03-01'),
(N'BS005', N'Đỗ Minh Trí', 1, '1990-10-30', N'CK01', 160000, '2018-05-20');
```

### 2. Yêu Cầu Chức Năng
* **Form 1 – `frmQuanLyBacSi` (5.0 điểm):**
  1. DataGridView hiển thị danh sách: Mã BS, Họ tên, Giới tính (`Nam`/`Nữ`), Ngày sinh (`dd/MM/yyyy`), Chuyên khoa (`JOIN ChuyenKhoa` lấy `TenCK`), Đơn giá khám (`N0`), Ngày vào làm (`dd/MM/yyyy`), và **Cột tính toán:** `Phụ cấp ngoài giờ = Đơn giá khám × 1.5` (hiển thị `N0`).
  2. Nạp ComboBox Chuyên khoa (`DisplayMember = TenCK`, `ValueMember = MaCK`).
  3. Khi click chọn một dòng trên Grid: nạp thông tin lên controls; khóa ô `txtMaBS` sang ReadOnly.
  4. Nút **Thêm (&T)**:
     - Mã BS không được rỗng, không được trùng, có định dạng `BS\d{3}` (ví dụ: `BS006`).
     - Bác sĩ phải từ **24 tuổi trở lên** tính đến ngày vào làm (`Validator.AgeRangeAtDate`).
     - Đơn giá khám là số nguyên từ **100.000 đ đến 2.000.000 đ**.
  5. Nút **Sửa (&S)**: Kiểm tra hợp lệ như khi thêm (trừ trùng mã), cập nhật CSDL và nạp lại bảng.
  6. Nút **Xóa (&X)**: Xác nhận `"Bạn có chắc chắn muốn xóa bác sĩ … không?"` với 2 nút Yes/No.
  7. Nút **Làm mới (&L)**, **Thống kê (&K)**, **Thoát (&H)**.

* **Form 2 – `frmThongKeBacSi` (2.5 điểm):**
  1. ComboBox Chuyên khoa có thêm dòng `Tất cả` ở đầu danh sách. ComboBox Giới tính gồm 3 mục: `Tất cả`, `Nam`, `Nữ`.
  2. Nút **Tìm kiếm (&T)**: Kết hợp lọc AND theo cả Chuyên khoa và Giới tính. Nếu không có dòng nào thì thông báo `"Không tìm thấy dữ liệu phù hợp"`.
  3. Cập nhật nhóm **Thống kê**: Số lượng bác sĩ, Tổng đơn giá khám, Đơn giá khám cao nhất.
  4. Nút **Đóng (&N)**.

* **Báo cáo (2.5 điểm):**
  1. Hiển thị báo cáo gom nhóm theo **Chuyên khoa**, hiển thị số lượng bác sĩ từng khoa và dòng **Tổng cộng đơn giá khám**.

---

## 📋 ĐỀ SỐ 03: HỆ THỐNG QUẢN LÝ SẢN PHẨM ĐIỆN MÁY (QLDienMay)

### 1. Script Khởi Tạo Cơ Sở Dữ Liệu
```sql
CREATE DATABASE QLDienMay;
GO
USE QLDienMay;
GO

CREATE TABLE NhomHang (
    MaNhom NVARCHAR(10) PRIMARY KEY,
    TenNhom NVARCHAR(100) NOT NULL
);

CREATE TABLE SanPham (
    MaSP NVARCHAR(10) PRIMARY KEY,
    TenSP NVARCHAR(100) NOT NULL,
    MaNhom NVARCHAR(10) FOREIGN KEY REFERENCES NhomHang(MaNhom),
    DonGiaNhap DECIMAL(18,0) NOT NULL,
    TiLeLai FLOAT NOT NULL, -- Tỉ lệ % (ví dụ: 15.5)
    SoLuongTon INT NOT NULL,
    NgayNhap DATE NOT NULL
);

-- Dữ liệu mẫu
INSERT INTO NhomHang VALUES 
(N'NH01', N'Tivi - Âm thanh'), 
(N'NH02', N'Tủ lạnh - Tủ đông'), 
(N'NH03', N'Máy giặt - Máy sấy'),
(N'NH04', N'Gia dụng');

INSERT INTO SanPham VALUES
(N'SP001', N'Smart Tivi Samsung 4K 55 inch', N'NH01', 8500000, 15.0, 12, '2024-01-15'),
(N'SP002', N'Tủ lạnh Panasonic Inverter 322L', N'NH02', 11200000, 12.5, 8, '2024-02-10'),
(N'SP003', N'Máy giặt Electrolux 10kg', N'NH03', 9400000, 18.0, 15, '2024-01-20'),
(N'SP004', N'Nồi chiên không dầu Philips 6.2L', N'NH04', 1850000, 25.0, 30, '2024-03-05'),
(N'SP005', N'Loa thanh Sony Soundbar 2.1', N'NH01', 2900000, 20.0, 20, '2024-02-28');
```

### 2. Yêu Cầu Chức Năng
* **Form 1 – `frmQuanLySanPham` (5.0 điểm):**
  1. Hiển thị DataGridView: Mã SP, Tên SP, Nhóm hàng (`TenNhom`), Đơn giá nhập (`N0`), Tỉ lệ lãi (`0.## %`), Số lượng tồn, Ngày nhập (`dd/MM/yyyy`), và **Cột tính toán:** `Giá bán dự kiến = Đơn giá nhập × (1 + TiLeLai / 100)` (hiển thị `N0`).
  2. Nút **Thêm (&T)**:
     - Mã SP dạng `SP\d{3}` (ví dụ: `SP006`), không trùng.
     - Đơn giá nhập từ **50.000 đ đến 100.000.000 đ**.
     - Tỉ lệ lãi là số thực trong khoảng **5,0% đến 50,0%** (chấp nhận cả `15.5` và `15,5`).
     - Số lượng tồn là số nguyên **từ 0 đến 1.000**.
     - Ngày nhập không được lớn hơn ngày hiện tại (`<= DateTime.Today`).
  3. Nút **Sửa (&S)**, **Xóa (&X)**, **Làm mới (&L)**.

* **Form 2 – `frmTraCuuSanPham` (2.5 điểm):**
  1. Lọc kết hợp: Nhóm hàng (có `-- Tất cả --`), Tình trạng tồn kho (Tất cả / Còn hàng / Hết hàng) và ô tìm kiếm từ khóa theo **Tên sản phẩm**.
  2. Thống kê: **Tổng số mặt hàng**, **Tổng giá trị vốn tồn kho** (`Sum(DonGiaNhap * SoLuongTon)`), **Giá bán cao nhất**.

* **Báo cáo (2.5 điểm):**
  1. Xuất **BẢNG KÊ SẢN PHẨM TỒN KHO** gom nhóm theo Nhóm hàng, hỗ trợ xuất Excel.

---

## 📋 ĐỀ SỐ 04: HỆ THỐNG QUẢN LÝ PHÒNG KHÁCH SẠN (QLKhachSan)

### 1. Script Khởi Tạo Cơ Sở Dữ Liệu
```sql
CREATE DATABASE QLKhachSan;
GO
USE QLKhachSan;
GO

CREATE TABLE LoaiPhong (
    MaLoai NVARCHAR(10) PRIMARY KEY,
    TenLoai NVARCHAR(100) NOT NULL
);

CREATE TABLE Phong (
    MaPhong NVARCHAR(10) PRIMARY KEY,
    TenPhong NVARCHAR(50) NOT NULL,
    MaLoai NVARCHAR(10) FOREIGN KEY REFERENCES LoaiPhong(MaLoai),
    Tang INT NOT NULL,
    DonGiaNgay DECIMAL(18,0) NOT NULL,
    TrangThai NVARCHAR(20) NOT NULL, -- N'Trống', N'Đang thuê', N'Bảo trì'
    NgayDuaVaoSD DATE NOT NULL
);

-- Dữ liệu mẫu
INSERT INTO LoaiPhong VALUES 
(N'LP01', N'Phòng Đơn Tiêu Chuẩn (Standard Single)'), 
(N'LP02', N'Phòng Đôi Tiêu Chuẩn (Standard Double)'), 
(N'LP03', N'Phòng Hạng Sang (Deluxe VIP)'),
(N'LP04', N'Phòng Gia Đình (Family Suite)');

INSERT INTO Phong VALUES
(N'P101', N'Phòng 101', N'LP01', 1, 350000, N'Trống', '2020-05-10'),
(N'P102', N'Phòng 102', N'LP02', 1, 550000, N'Đang thuê', '2020-05-10'),
(N'P201', N'Phòng 201', N'LP03', 2, 950000, N'Trống', '2021-01-15'),
(N'P202', N'Phòng 202', N'LP04', 2, 1400000, N'Đang thuê', '2021-01-15'),
(N'P301', N'Phòng 301', N'LP03', 3, 1000000, N'Bảo trì', '2022-08-01');
```

### 2. Yêu Cầu Chức Năng
* **Form 1 – `frmQuanLyPhong` (5.0 điểm):**
  1. Hiển thị DataGridView: Mã phòng, Tên phòng, Loại phòng (`TenLoai`), Tầng, Đơn giá ngày (`N0`), Trạng thái, Ngày đưa vào SD (`dd/MM/yyyy`), và **Cột tính toán:** `Giá thuê giờ đầu = Đơn giá ngày × 0.25` (hiển thị `N0`).
  2. Nạp ComboBox Loại phòng. ComboBox Trạng thái gồm 3 mục: `Trống`, `Đang thuê`, `Bảo trì`.
  3. Nút **Thêm (&T)**:
     - Mã phòng có dạng `P\d{3}` (ví dụ: `P302`), không trùng.
     - Tầng là số nguyên từ **1 đến 15**.
     - Đơn giá ngày từ **200.000 đ đến 15.000.000 đ**.
     - Bắt buộc chọn Loại phòng và Trạng thái.
  4. Nút **Sửa (&S)**, **Xóa (&X)**, **Làm mới (&L)**.

* **Form 2 – `frmThongKePhong` (2.5 điểm):**
  1. Lọc kết hợp: Loại phòng (có `-- Tất cả --`) và Trạng thái phòng (Tất cả / Trống / Đang thuê / Bảo trì).
  2. Thống kê: **Tổng số phòng**, **Số phòng đang trống** (`DataHelper.Count(dt, "TrangThai = 'Trống'")`), **Đơn giá thuê ngày cao nhất**.

* **Báo cáo (2.5 điểm):**
  1. Xuất **BẢNG KÊ DANH MỤC PHÒNG KHÁCH SẠN** gom nhóm theo Loại phòng, tính tổng đơn giá ngày toàn khách sạn.
