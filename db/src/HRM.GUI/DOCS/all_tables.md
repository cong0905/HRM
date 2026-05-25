# Danh sách bảng và cấu trúc dữ liệu (Toàn bộ project)

Tệp này liệt kê các bảng (dự trên các lớp entity trong `HRM.Domain.Entities`). Bạn có thể copy từng phần vào Word để nhập liệu.

---

## `NHANVIEN` (NhanVien)

| Tên trường       | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả                             |
| ---------------- | ----------------: | ------------------------- | --------------------------------- |
| MaNhanVien       |               int | PK                        | Mã nhân viên                      |
| MaNV             |           string? |                           | Mã nội bộ (mã số nhân viên)       |
| HoTen            |            string |                           | Họ tên                            |
| NgaySinh         |          DateTime |                           | Ngày sinh                         |
| GioiTinh         |           string? |                           | Giới tính                         |
| CCCD             |           string? |                           | CMND/CCCD                         |
| DiaChi           |           string? |                           | Địa chỉ                           |
| SoDienThoai      |           string? |                           | SĐT                               |
| Email            |           string? |                           | Email                             |
| TinhTrangHonNhan |           string? |                           | Tình trạng hôn nhân               |
| MaPhongBan       |              int? | FK -> PhongBan.MaPhongBan | Mã phòng ban                      |
| MaChucVu         |              int? | FK -> ChucVu.MaChucVu     | Mã chức vụ                        |
| NgayVaoLam       |          DateTime |                           | Ngày vào làm                      |
| MucLuong         |           decimal |                           | Mức lương cơ bản                  |
| TrangThai        |            string |                           | Trạng thái (ví dụ: Đang làm việc) |
| NgayNghiViec     |         DateTime? |                           | Ngày nghỉ việc                    |
| AnhDaiDien       |           string? |                           | Đường dẫn ảnh                     |
| NgayTao          |          DateTime |                           | Ngày tạo                          |
| NgayCapNhat      |          DateTime |                           | Ngày cập nhật                     |

---

## `PHONGBAN` (PhongBan)

| Tên trường     | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả             |
| -------------- | ----------------: | ------------------------- | ----------------- |
| MaPhongBan     |               int | PK                        | Mã phòng ban      |
| TenPhongBan    |            string |                           | Tên phòng ban     |
| MoTaChucNang   |           string? |                           | Mô tả chức năng   |
| NgayThanhLap   |         DateTime? |                           | Ngày thành lập    |
| DiaDiemLamViec |           string? |                           | Địa điểm làm việc |
| NganSach       |          decimal? |                           | Ngân sách         |
| TrangThai      |            string |                           | Trạng thái        |
| MaTruongPhong  |              int? | FK -> NhanVien.MaNhanVien | Mã trưởng phòng   |
| NgayTao        |          DateTime |                           | Ngày tạo          |
| NgayCapNhat    |          DateTime |                           | Ngày cập nhật     |

---

## `CHUCVU` (ChucVu)

| Tên trường | Kiểu dữ liệu (C#) | PK / FK | Mô tả       |
| ---------- | ----------------: | ------- | ----------- |
| MaChucVu   |               int | PK      | Mã chức vụ  |
| TenChucVu  |            string |         | Tên chức vụ |
| MoTa       |           string? |         | Mô tả       |
| CapBac     |               int |         | Cấp bậc     |

---

## `TAIKHOAN` (TaiKhoan)

| Tên trường      | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả              |
| --------------- | ----------------: | ------------------------- | ------------------ |
| MaTaiKhoan      |               int | PK                        | Mã tài khoản       |
| MaNhanVien      |               int | FK -> NhanVien.MaNhanVien | Mã nhân viên       |
| TenDangNhap     |            string |                           | Tên đăng nhập      |
| MatKhauHash     |            string |                           | Hash mật khẩu      |
| VaiTro          |            string |                           | Vai trò            |
| TrangThai       |            string |                           | Trạng thái         |
| LanDangNhapCuoi |         DateTime? |                           | Lần đăng nhập cuối |
| NgayTao         |          DateTime |                           | Ngày tạo           |

---

## `DONNGHIPHEP` (DonNghiPhep)

| Tên trường    | Kiểu dữ liệu (C#) | PK / FK                       | Mô tả                         |
| ------------- | ----------------: | ----------------------------- | ----------------------------- |
| MaDonPhep     |               int | PK                            | Mã đơn nghỉ phép              |
| MaNhanVien    |               int | FK -> NhanVien.MaNhanVien     | Mã nhân viên                  |
| MaLoaiPhep    |               int | FK -> LoaiNghiPhep.MaLoaiPhep | Loại nghỉ phép                |
| NgayBatDau    |          DateTime |                               | Ngày bắt đầu                  |
| NgayKetThuc   |          DateTime |                               | Ngày kết thúc                 |
| SoNgayNghi    |           decimal |                               | Số ngày nghỉ                  |
| LyDo          |            string |                               | Lý do                         |
| TrangThai     |            string |                               | Trạng thái (ví dụ: Chờ duyệt) |
| NguoiPheDuyet |              int? | FK -> NhanVien.MaNhanVien     | Người phê duyệt               |
| NgayPheDuyet  |         DateTime? |                               | Ngày phê duyệt                |
| LyDoTuChoi    |           string? |                               | Lý do từ chối                 |
| NgayTao       |          DateTime |                               | Ngày tạo                      |
| NgayHuy       |         DateTime? |                               | Ngày hủy                      |
| LyDoHuy       |           string? |                               | Lý do hủy                     |

---

## `LOAINGHIPHEP` (LoaiNghiPhep)

| Tên trường  | Kiểu dữ liệu (C#) | PK / FK | Mô tả              |
| ----------- | ----------------: | ------- | ------------------ |
| MaLoaiPhep  |               int | PK      | Mã loại nghỉ phép  |
| TenLoaiPhep |            string |         | Tên loại           |
| MoTa        |           string? |         | Mô tả              |
| CoLuong     |              bool |         | Có lương hay không |

---

## `CHAMCONG` (ChamCong)

| Tên trường   | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả            |
| ------------ | ----------------: | ------------------------- | ---------------- |
| MaChamCong   |               int | PK                        | Mã chấm công     |
| MaNhanVien   |               int | FK -> NhanVien.MaNhanVien | Mã nhân viên     |
| NgayChamCong |          DateTime |                           | Ngày chấm công   |
| GioVao       |         TimeSpan? |                           | Giờ vào          |
| GioRa        |         TimeSpan? |                           | Giờ ra           |
| TongGioLam   |          decimal? |                           | Tổng giờ làm     |
| GioLamThem   |           decimal |                           | Giờ làm thêm     |
| HinhThuc     |           string? |                           | Hình thức        |
| TrangThai    |            string |                           | Trạng thái       |
| GhiChu       |           string? |                           | Ghi chú          |
| Hwid         |           string? |                           | ID máy chấm công |

---

## `BANGluong` (BangLuong)

| Tên trường    | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả            |
| ------------- | ----------------: | ------------------------- | ---------------- |
| MaBangLuong   |               int | PK                        | Mã bảng lương    |
| MaNhanVien    |               int | FK -> NhanVien.MaNhanVien | Mã nhân viên     |
| Thang         |               int |                           | Tháng            |
| Nam           |               int |                           | Năm              |
| LuongCoBan    |           decimal |                           | Lương cơ bản     |
| TongPhuCap    |           decimal |                           | Tổng phụ cấp     |
| SoNgayLamViec |               int |                           | Số ngày làm việc |
| SoGioLamThem  |           decimal |                           | Số giờ làm thêm  |
| TienLamThem   |           decimal |                           | Tiền làm thêm    |
| TongThuong    |           decimal |                           | Tổng thưởng      |
| TongPhat      |           decimal |                           | Tổng phạt        |
| BHXH          |           decimal |                           | BHXH             |
| BHYT          |           decimal |                           | BHYT             |
| BHTN          |           decimal |                           | BHTN             |
| ThueTNCN      |           decimal |                           | Thuế TNCN        |
| TongThuNhap   |          decimal? |                           | Tổng thu nhập    |
| TongKhauTru   |          decimal? |                           | Tổng khấu trừ    |
| LuongThucNhan |          decimal? |                           | Lương thực nhận  |
| NgayTinhLuong |          DateTime |                           | Ngày tính lương  |
| TrangThai     |            string |                           | Trạng thái       |

---

## `THUONGPHAT` (ThuongPhat)

| Tên trường    | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả              |
| ------------- | ----------------: | ------------------------- | ------------------ |
| MaThuongPhat  |               int | PK                        | Mã thưởng/phạt     |
| MaNhanVien    |               int | FK -> NhanVien.MaNhanVien | Mã nhân viên       |
| Loai          |            string |                           | Loại (Thưởng/Phạt) |
| LoaiChiTiet   |           string? |                           | Loại chi tiết      |
| LyDo          |            string |                           | Lý do              |
| SoTien        |           decimal |                           | Số tiền            |
| NgayApDung    |          DateTime |                           | Ngày áp dụng       |
| NguoiPheDuyet |              int? | FK -> NhanVien.MaNhanVien | Người phê duyệt    |
| TrangThai     |            string |                           | Trạng thái         |

---

## `HIEUSUATNHANVIEN` (HieuSuatNhanVien)

| Tên trường            | Kiểu dữ liệu (C#) | PK / FK                     | Mô tả                     |
| --------------------- | ----------------: | --------------------------- | ------------------------- |
| MaHieuSuat            |               int | PK                          | Mã hiệu suất              |
| MaNhanVien            |               int | FK -> NhanVien.MaNhanVien   | Mã nhân viên              |
| MaKyDanhGia           |               int | FK -> KyDanhGia.MaKyDanhGia | Mã kỳ đánh giá            |
| DiemKPI               |          decimal? |                             | Điểm KPI                  |
| KetQuaCongViec        |           string? |                             | Kết quả công việc         |
| TyLeHoanThanhDeadline |          decimal? |                             | Tỷ lệ hoàn thành deadline |
| SoGioLamViec          |          decimal? |                             | Số giờ làm việc           |
| NgayDanhGia           |          DateTime |                             | Ngày đánh giá             |

---

## `KYDANHGIA` (KyDanhGia)

| Tên trường   | Kiểu dữ liệu (C#) | PK / FK | Mô tả           |
| ------------ | ----------------: | ------- | --------------- |
| MaKyDanhGia  |               int | PK      | Mã kỳ đánh giá  |
| TenKyDanhGia |            string |         | Tên kỳ đánh giá |
| NgayBatDau   |          DateTime |         | Ngày bắt đầu    |
| NgayKetThuc  |          DateTime |         | Ngày kết thúc   |
| TrangThai    |            string |         | Trạng thái      |

---

## `PHONGVAN` (PhongVan)

| Tên trường     | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả           |
| -------------- | ----------------: | ------------------------- | --------------- |
| MaPhongVan     |               int | PK                        | Mã phỏng vấn    |
| MaUngVien      |               int | FK -> UngVien.MaUngVien   | Mã ứng viên     |
| VongPhongVan   |               int |                           | Vòng phỏng vấn  |
| NgayPhongVan   |          DateTime |                           | Ngày phỏng vấn  |
| DiaDiem        |           string? |                           | Địa điểm        |
| NguoiPhongVan  |              int? | FK -> NhanVien.MaNhanVien | Người phỏng vấn |
| CauHoiPhongVan |           string? |                           | Câu hỏi         |
| KetQua         |           string? |                           | Kết quả         |
| DiemDanhGia    |          decimal? |                           | Điểm đánh giá   |
| NhanXet        |           string? |                           | Nhận xét        |
| TrangThai      |            string |                           | Trạng thái      |

---

## `UNgVIEN` (UngVien)

| Tên trường         | Kiểu dữ liệu (C#) | PK / FK                           | Mô tả                  |
| ------------------ | ----------------: | --------------------------------- | ---------------------- |
| MaUngVien          |               int | PK                                | Mã ứng viên            |
| MaTinTuyenDung     |               int | FK -> TinTuyenDung.MaTinTuyenDung | Mã tin tuyển dụng      |
| HoTen              |            string |                                   | Họ tên                 |
| Email              |           string? |                                   | Email                  |
| SoDienThoai        |           string? |                                   | SĐT                    |
| DuongDanCV         |           string? |                                   | Đường dẫn CV           |
| DuongDanThuXinViec |           string? |                                   | Đường dẫn thư xin việc |
| KinhNghiem         |           string? |                                   | Kinh nghiệm            |
| BangCap            |           string? |                                   | Bằng cấp               |
| KyNang             |           string? |                                   | Kỹ năng                |
| PhanLoai           |            string |                                   | Phân loại              |
| TrangThai          |            string |                                   | Trạng thái             |
| GhiChu             |           string? |                                   | Ghi chú                |
| NgayNop            |          DateTime |                                   | Ngày nộp               |

---

## `TINTUYENDUNG` (TinTuyenDung)

| Tên trường      | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả               |
| --------------- | ----------------: | ------------------------- | ------------------- |
| MaTinTuyenDung  |               int | PK                        | Mã tin tuyển dụng   |
| ViTriTuyenDung  |            string |                           | Vị trí tuyển dụng   |
| MaPhongBan      |              int? | FK -> PhongBan.MaPhongBan | Mã phòng ban        |
| MoTaCongViec    |           string? |                           | Mô tả công việc     |
| YeuCauUngVien   |           string? |                           | Yêu cầu ứng viên    |
| SoLuongCanTuyen |               int |                           | Số lượng            |
| MucLuongMin     |          decimal? |                           | Mức lương tối thiểu |
| MucLuongMax     |          decimal? |                           | Mức lương tối đa    |
| ThoiHanNhanHoSo |         DateTime? |                           | Hạn nhận hồ sơ      |
| DiadiemLamViec  |           string? |                           | Địa điểm làm việc   |
| TrangThai       |            string |                           | Trạng thái          |
| NguoiTao        |              int? | FK -> NhanVien.MaNhanVien | Người tạo           |
| NgayDang        |          DateTime |                           | Ngày đăng           |

---

## `QUYETDINHTUYENDUNG` (QuyetDinhTuyenDung)

| Tên trường        | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả                 |
| ----------------- | ----------------: | ------------------------- | --------------------- |
| MaQuyetDinh       |               int | PK                        | Mã quyết định         |
| MaUngVien         |               int | FK -> UngVien.MaUngVien   | Mã ứng viên           |
| KetQua            |            string |                           | Kết quả               |
| NgayQuyetDinh     |          DateTime |                           | Ngày quyết định       |
| NguoiQuyetDinh    |              int? | FK -> NhanVien.MaNhanVien | Người quyết định      |
| MucLuongDeXuat    |          decimal? |                           | Mức lương đề xuất     |
| NgayBatDauLamViec |         DateTime? |                           | Ngày bắt đầu làm việc |
| DaGuiOfferLetter  |              bool |                           | Đã gửi offer letter   |
| PhanHoiUngVien    |           string? |                           | Phản hồi của ứng viên |
| GhiChu            |           string? |                           | Ghi chú               |

---

## `TAILIEUNHANVIEN` (TaiLieuNhanVien)

| Tên trường   | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả          |
| ------------ | ----------------: | ------------------------- | -------------- |
| MaTaiLieu    |               int | PK                        | Mã tài liệu    |
| MaNhanVien   |               int | FK -> NhanVien.MaNhanVien | Mã nhân viên   |
| LoaiTaiLieu  |            string |                           | Loại tài liệu  |
| TenTaiLieu   |            string |                           | Tên tài liệu   |
| DuongDanFile |            string |                           | Đường dẫn file |
| NgayTaiLen   |          DateTime |                           | Ngày tải lên   |
| GhiChu       |           string? |                           | Ghi chú        |

---

## `THONGBAO` (ThongBao)

| Tên trường   | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả          |
| ------------ | ----------------: | ------------------------- | -------------- |
| MaThongBao   |               int | PK                        | Mã thông báo   |
| MaNguoiNhan  |               int | FK -> NhanVien.MaNhanVien | Mã người nhận  |
| TieuDe       |            string |                           | Tiêu đề        |
| NoiDung      |           string? |                           | Nội dung       |
| LoaiThongBao |           string? |                           | Loại thông báo |
| DaDoc        |              bool |                           | Đã đọc         |
| NgayTao      |          DateTime |                           | Ngày tạo       |

---

## `CHUNGCHI` (ChungChi)

| Tên trường   | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả          |
| ------------ | ----------------: | ------------------------- | -------------- |
| MaChungChi   |               int | PK                        | Mã chứng chỉ   |
| MaNhanVien   |               int | FK -> NhanVien.MaNhanVien | Mã nhân viên   |
| TenChungChi  |            string |                           | Tên chứng chỉ  |
| LoaiChungChi |           string? |                           | Loại chứng chỉ |
| ToChucCap    |           string? |                           | Tổ chức cấp    |
| NgayCap      |          DateTime |                           | Ngày cấp       |
| NgayHetHan   |         DateTime? |                           | Ngày hết hạn   |
| DuongDanFile |           string? |                           | Đường dẫn file |
| GhiChu       |           string? |                           | Ghi chú        |

---

## `DIEUCHINHCHAMCONG` (DieuChinhChamCong)

| Tên trường     | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả            |
| -------------- | ----------------: | ------------------------- | ---------------- |
| MaDieuChinh    |               int | PK                        | Mã điều chỉnh    |
| MaChamCong     |               int | FK -> ChamCong.MaChamCong | Mã chấm công     |
| NguoiDieuChinh |               int | FK -> NhanVien.MaNhanVien | Người điều chỉnh |
| GioVaoCu       |         TimeSpan? |                           | Giờ vào cũ       |
| GioRaCu        |         TimeSpan? |                           | Giờ ra cũ        |
| GioVaoMoi      |         TimeSpan? |                           | Giờ vào mới      |
| GioRaMoi       |         TimeSpan? |                           | Giờ ra mới       |
| LyDo           |            string |                           | Lý do            |
| NgayDieuChinh  |          DateTime |                           | Ngày điều chỉnh  |

---

## `SONGAYPHEP` (SoNgayPhep)

| Tên trường      | Kiểu dữ liệu (C#) | PK / FK                               | Mô tả                       |
| --------------- | ----------------: | ------------------------------------- | --------------------------- |
| MaNhanVien      |               int | PK (part) / FK -> NhanVien.MaNhanVien | Mã nhân viên                |
| Nam             |               int | PK (part)                             | Năm                         |
| TongSoNgayPhep  |               int |                                       | Tổng số ngày phép           |
| SoNgayDaSuDung  |               int |                                       | Số ngày đã sử dụng          |
| SoNgayConLai    |               int |                                       | Số ngày còn lại (tính toán) |
| PhepNamCuConLai |               int |                                       | Phép năm cũ còn lại         |

---

## `PHANCONGDAOTAO` (PhanCongDaoTao)

| Tên trường    | Kiểu dữ liệu (C#) | PK / FK                          | Mô tả            |
| ------------- | ----------------: | -------------------------------- | ---------------- |
| MaPhanCong    |               int | PK                               | Mã phân công     |
| MaDaoTao      |               int | FK -> ChuongTrinhDaoTao.MaDaoTao | Mã đào tạo       |
| MaNhanVien    |               int | FK -> NhanVien.MaNhanVien        | Mã nhân viên     |
| NgayDangKy    |          DateTime |                                  | Ngày đăng ký     |
| TyLeThamDu    |           decimal |                                  | Tỷ lệ tham dự    |
| KetQuaKiemTra |          decimal? |                                  | Kết quả kiểm tra |
| PhanHoi       |           string? |                                  | Phản hồi         |
| TrangThai     |            string |                                  | Trạng thái       |

---

## `CHUONGTRINHDAOTAO` (ChuongTrinhDaoTao)

| Tên trường     | Kiểu dữ liệu (C#) | PK / FK | Mô tả              |
| -------------- | ----------------: | ------- | ------------------ |
| MaDaoTao       |               int | PK      | Mã đào tạo         |
| TenKhoaHoc     |            string |         | Tên khóa học       |
| MucTieu        |           string? |         | Mục tiêu           |
| NoiDung        |           string? |         | Nội dung           |
| ThoiLuong      |              int? |         | Thời lượng         |
| GiangVien      |           string? |         | Giảng viên         |
| DiaDiem        |           string? |         | Địa điểm           |
| ChiPhi         |          decimal? |         | Chi phí            |
| SoHocVienToiDa |              int? |         | Số học viên tối đa |
| NgayBatDau     |         DateTime? |         | Ngày bắt đầu       |
| NgayKetThuc    |         DateTime? |         | Ngày kết thúc      |
| TrangThai      |            string |         | Trạng thái         |
| NgayTao        |          DateTime |         | Ngày tạo           |

---

## `DANHGIADAOTAO` (DanhGiaDaoTao)

| Tên trường       | Kiểu dữ liệu (C#) | PK / FK                          | Mô tả               |
| ---------------- | ----------------: | -------------------------------- | ------------------- |
| MaDanhGia        |               int | PK                               | Mã đánh giá         |
| MaDaoTao         |               int | FK -> ChuongTrinhDaoTao.MaDaoTao | Mã đào tạo          |
| MaNhanVien       |               int | FK -> NhanVien.MaNhanVien        | Mã nhân viên        |
| DiemSo           |          decimal? |                                  | Điểm số             |
| DanhGiaGiangVien |           string? |                                  | Đánh giá giảng viên |
| PhanHoiHocVien   |           string? |                                  | Phản hồi học viên   |
| ChatLuongKhoaHoc |              int? |                                  | Chất lượng khóa học |
| NgayDanhGia      |          DateTime |                                  | Ngày đánh giá       |

---

## `LICHSUDIEUCHUYEN` (LichSuDieuChuyen)

| Tên trường    | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả           |
| ------------- | ----------------: | ------------------------- | --------------- |
| MaLichSu      |               int | PK                        | Mã lịch sử      |
| MaNhanVien    |               int | FK -> NhanVien.MaNhanVien | Mã nhân viên    |
| MaPhongBanCu  |              int? | FK -> PhongBan.MaPhongBan | Phòng ban cũ    |
| MaPhongBanMoi |              int? | FK -> PhongBan.MaPhongBan | Phòng ban mới   |
| MaChucVuCu    |              int? | FK -> ChucVu.MaChucVu     | Chức vụ cũ      |
| MaChucVuMoi   |              int? | FK -> ChucVu.MaChucVu     | Chức vụ mới     |
| MucLuongCu    |          decimal? |                           | Mức lương cũ    |
| MucLuongMoi   |          decimal? |                           | Mức lương mới   |
| NgayThayDoi   |          DateTime |                           | Ngày thay đổi   |
| LyDo          |           string? |                           | Lý do           |
| NguoiThucHien |              int? | FK -> NhanVien.MaNhanVien | Người thực hiện |

---

## `LICHSUCHINHSACH` (LichSuChinhSach)

| Tên trường     | Kiểu dữ liệu (C#) | PK / FK                     | Mô tả             |
| -------------- | ----------------: | --------------------------- | ----------------- |
| MaLichSu       |               int | PK                          | Mã lịch sử        |
| MaChinhSach    |               int | FK -> ChinhSach.MaChinhSach | Mã chính sách     |
| PhienBanCu     |              int? |                             | Phiên bản cũ      |
| PhienBanMoi    |              int? |                             | Phiên bản mới     |
| NoiDungThayDoi |           string? |                             | Nội dung thay đổi |
| LyDoSuaDoi     |            string |                             | Lý do sửa đổi     |
| NguoiSuaDoi    |              int? | FK -> NhanVien.MaNhanVien   | Người sửa đổi     |
| NgaySuaDoi     |          DateTime |                             | Ngày sửa đổi      |

---

## `CHINHSACH` (ChinhSach)

| Tên trường     | Kiểu dữ liệu (C#) | PK / FK                   | Mô tả             |
| -------------- | ----------------: | ------------------------- | ----------------- |
| MaChinhSach    |               int | PK                        | Mã chính sách     |
| TenChinhSach   |            string |                           | Tên chính sách    |
| LoaiChinhSach  |           string? |                           | Loại chính sách   |
| NoiDung        |            string |                           | Nội dung          |
| PhamViApDung   |           string? |                           | Phạm vi áp dụng   |
| NgayHieuLuc    |          DateTime |                           | Ngày hiệu lực     |
| NgayHetHieuLuc |         DateTime? |                           | Ngày hết hiệu lực |
| PhienBan       |               int |                           | Phiên bản         |
| NguoiPheDuyet  |              int? | FK -> NhanVien.MaNhanVien | Người phê duyệt   |
| TrangThai      |            string |                           | Trạng thái        |
| NgayTao        |          DateTime |                           | Ngày tạo          |
| NgayCapNhat    |          DateTime |                           | Ngày cập nhật     |

---

## `XACNHANCHINHSACH` (XacNhanChinhSach)

| Tên trường  | Kiểu dữ liệu (C#) | PK / FK                     | Mô tả         |
| ----------- | ----------------: | --------------------------- | ------------- |
| MaXacNhan   |               int | PK                          | Mã xác nhận   |
| MaChinhSach |               int | FK -> ChinhSach.MaChinhSach | Mã chính sách |
| MaNhanVien  |               int | FK -> NhanVien.MaNhanVien   | Mã nhân viên  |
| DaDoc       |              bool |                             | Đã đọc        |
| NgayXacNhan |         DateTime? |                             | Ngày xác nhận |

---

## `PHUCAPNHANVIEN` (PhuCapNhanVien)

| Tên trường  | Kiểu dữ liệu (C#) | PK / FK                               | Mô tả         |
| ----------- | ----------------: | ------------------------------------- | ------------- |
| MaNhanVien  |               int | PK (part) / FK -> NhanVien.MaNhanVien | Mã nhân viên  |
| MaPhuCap    |               int | PK (part) / FK -> LoaiPhuCap.MaPhuCap | Mã phụ cấp    |
| NgayApDung  |          DateTime |                                       | Ngày áp dụng  |
| NgayKetThuc |         DateTime? |                                       | Ngày kết thúc |

---

## `LOAIPHUCAP` (LoaiPhuCap)

| Tên trường | Kiểu dữ liệu (C#) | PK / FK | Mô tả       |
| ---------- | ----------------: | ------- | ----------- |
| MaPhuCap   |               int | PK      | Mã phụ cấp  |
| TenPhuCap  |            string |         | Tên phụ cấp |
| MoTa       |           string? |         | Mô tả       |
| SoTien     |           decimal |         | Số tiền     |

---

(End of file)
