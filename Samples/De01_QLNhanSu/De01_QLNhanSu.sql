IF DB_ID(N'QLNhanSu') IS NULL
BEGIN
    CREATE DATABASE QLNhanSu;
END
GO

USE QLNhanSu;
GO

IF OBJECT_ID(N'dbo.NhanVien', N'U') IS NOT NULL DROP TABLE dbo.NhanVien;
IF OBJECT_ID(N'dbo.PhongBan', N'U') IS NOT NULL DROP TABLE dbo.PhongBan;
GO

CREATE TABLE PhongBan (
    MaPB NVARCHAR(10) PRIMARY KEY,
    TenPB NVARCHAR(100) NOT NULL
);

CREATE TABLE NhanVien (
    MaNV NVARCHAR(10) PRIMARY KEY,
    HoTen NVARCHAR(50) NOT NULL,
    GioiTinh BIT NOT NULL, -- 1: Nam, 0: Nữ
    NgaySinh DATE NOT NULL,
    MaPB NVARCHAR(10) NOT NULL FOREIGN KEY REFERENCES PhongBan(MaPB),
    HeSoLuong FLOAT NOT NULL,
    NgayVaoLam DATE NOT NULL
);
GO

INSERT INTO PhongBan (MaPB, TenPB) VALUES
(N'PB01', N'Kinh doanh'),
(N'PB02', N'Kế toán'),
(N'PB03', N'Kỹ thuật'),
(N'PB04', N'Hành chính');

INSERT INTO NhanVien (MaNV, HoTen, GioiTinh, NgaySinh, MaPB, HeSoLuong, NgayVaoLam) VALUES
(N'NV001', N'Trần Minh Đức', 1, '1990-03-12', N'PB01', 3.33, '2015-07-01'),
(N'NV002', N'Lê Thu Hà',     0, '1995-11-25', N'PB02', 2.67, '2019-08-15'),
(N'NV003', N'Phạm Quốc Bảo', 1, '1988-06-08', N'PB03', 4.4,  '2012-01-02'),
(N'NV004', N'Nguyễn Thị Lan',0, '1997-09-19', N'PB01', 2.34, '2021-03-10'),
(N'NV005', N'Đỗ Văn Hùng',   1, '1985-01-30', N'PB04', 3.66, '2010-05-20'),
(N'NV006', N'Vũ Thị Hồng',   0, '1992-04-14', N'PB01', 3.0,  '2017-09-01'),
(N'NV007', N'Hoàng Gia Huy', 1, '1999-12-05', N'PB03', 2.34, '2023-04-04');
GO
