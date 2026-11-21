using HRM.BLL.Interfaces;
using HRM.BLL.Services;
using HRM.Common.DTOs;
using HRM.DAL.Context;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace HRM.Tests.Regression.Characterization;

/// <summary>
/// BỘ KIỂM THỬ ĐẶC TRƯNG HÀNH VI (CHARACTERIZATION TESTS) CHO MAPPING VÀ PARTIAL UPDATE
/// 
/// Nhiệm vụ: T03 - Khoanh vùng lỗi legacy và lập bản đồ chuyển nghiệp vụ.
/// Mục tiêu:
/// 1. Gọi trực tiếp mã nguồn legacy thật (NhanVienService.UpdateAsync, GetByIdAsync, NhanVienCreateDTO, NhanVien entity)
///    để chứng minh và tái hiện chính xác hành vi thực tế của hệ thống hiện tại.
/// 2. Làm rõ sự bất lực của DTO legacy trong việc phân biệt:
///    - Trường vắng mặt / không gửi (Omitted field)
///    - Trường chủ ý xóa về null (Intentional null deletion)
///    - Giá trị mới được cung cấp (New value)
/// 3. Chứng minh Bug F10: Form cập nhật hoặc API client không gửi DiaChi/TinhTrangHonNhan sẽ làm mất trắng dữ liệu.
/// 4. Chứng minh Bug F02 liên kết: Cập nhật nhân viên làm biến động lịch sử bảng lương qua DongBoBangLuongTheoNhanVienAsync.
/// 5. Kiểm chứng tầng đọc MapToDTO giữ nguyên DiaChi, TinhTrangHonNhan và tên liên kết PhongBan/ChucVu.
/// 
/// LƯU Ý BẮT BUỘC:
/// - Constructor NhanVienService nhận đúng 3 tham số: INhanVienRepository, ITaiKhoanService, IBangLuongService.
/// - Không sửa mã nguồn service sản phẩm.
/// - Bộ test này phản ánh THỰC TẾ HÀNH VI HIỆN TẠI của mã nguồn legacy; không giả lập rằng partial update đã được sửa.
/// </summary>
[Trait("Category", "Characterization")]
[Trait("Finding", "F10")]
public class F10_PartialUpdateCharacterizationTests
{
    private static HrmDbContext CreateIsolatedDbContext()
    {
        var options = new DbContextOptionsBuilder<HrmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new HrmDbContext(options);
    }

    /// <summary>
    /// TEST 1: CHỨNG MINH BUG F10 - TRƯỜNG VẮNG MẶT BỊ GHI ĐÈ BẰNG NULL LÀM MẤT DỮ LIỆU
    /// 
    /// Ngữ cảnh: Nhân viên đã có đầy đủ DiaChi ("123 Lê Lợi, Q1") và TinhTrangHonNhan ("Đã kết hôn").
    /// Người dùng mở form sửa thông tin (frmSuaNhanVien), form này chỉ cho sửa Họ tên, SĐT, Email, v.v.,
    /// và KHÔNG có control cho DiaChi, TinhTrangHonNhan.
    /// 
    /// DTO NhanVienCreateDTO được khởi tạo mà không gán DiaChi và TinhTrangHonNhan.
    /// Trong C#, các property dạng nullable reference type (string?) mặc định nhận giá trị null.
    /// 
    /// Hành vi thực tế của NhanVienService.UpdateAsync:
    ///   entity.DiaChi = dto.DiaChi; // dto.DiaChi là null
    ///   entity.TinhTrangHonNhan = dto.TinhTrangHonNhan; // dto.TinhTrangHonNhan là null
    /// Kết quả: Dữ liệu địa chỉ và hôn nhân bị XÓA TRẮNG khỏi database.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenOptionalFieldsOmittedInDto_DestructivelyOverwritesExistingDataWithNull()
    {
        // 1. Arrange: Khởi tạo database và nhân viên mẫu có đầy đủ dữ liệu
        using var db = CreateIsolatedDbContext();
        const int employeeId = 10;
        const string initialAddress = "123 Lê Lợi, Quận 1, TP.HCM";
        const string initialMaritalStatus = "Đã kết hôn";

        var existingEmployee = new NhanVien
        {
            MaNhanVien = employeeId,
            MaNV = "NV0010",
            HoTen = "Nguyễn Văn C",
            NgaySinh = new DateTime(1990, 5, 20),
            GioiTinh = "Nam",
            CCCD = "079090001234",
            DiaChi = initialAddress,
            SoDienThoai = "0901234567",
            Email = "nvc@example.com",
            TinhTrangHonNhan = initialMaritalStatus,
            NgayVaoLam = new DateTime(2022, 1, 15),
            MucLuong = 15_000_000m,
            TrangThai = "Đang làm việc"
        };
        db.NhanVien.Add(existingEmployee);
        await db.SaveChangesAsync();

        var repo = new NhanVienRepository(db);
        var mockTaiKhoan = new Mock<ITaiKhoanService>();
        var mockBangLuong = new Mock<IBangLuongService>();

        var service = new NhanVienService(repo, mockTaiKhoan.Object, mockBangLuong.Object);

        // Giả lập DTO gửi từ frmSuaNhanVien: chỉ gán các trường có trên form,
        // KHÔNG gán DiaChi và TinhTrangHonNhan (omitted)
        var updatePayload = new NhanVienCreateDTO
        {
            HoTen = "Nguyễn Văn C (Đã cập nhật)",
            NgaySinh = new DateTime(1990, 5, 20),
            GioiTinh = "Nam",
            CCCD = "079090001234",
            SoDienThoai = "0909999888", // Cập nhật SĐT mới
            Email = "nvc_updated@example.com",
            MaPhongBan = null,
            MaChucVu = null,
            NgayVaoLam = new DateTime(2022, 1, 15),
            MucLuong = 15_000_000m,
            TrangThai = "Đang làm việc"
            // Lưu ý: DiaChi và TinhTrangHonNhan hoàn toàn không được thiết lập!
        };

        // Xác nhận trước khi gọi service: DTO có DiaChi và TinhTrangHonNhan là null do C# default
        Assert.Null(updatePayload.DiaChi);
        Assert.Null(updatePayload.TinhTrangHonNhan);

        // 2. Act: Thực thi phương thức cập nhật legacy
        await service.UpdateAsync(employeeId, updatePayload);

        // 3. Assert: Kiểm chứng hành vi thực tế trong DB
        db.ChangeTracker.Clear();
        var employeeInDb = await db.NhanVien.FindAsync(employeeId);
        Assert.NotNull(employeeInDb);
        Assert.Equal("Nguyễn Văn C (Đã cập nhật)", employeeInDb.HoTen);
        Assert.Equal("0909999888", employeeInDb.SoDienThoai);

        // CHỨNG MINH LỖ HỔNG F10: Địa chỉ và Tình trạng hôn nhân cũ ĐÃ BỊ XÓA MẤT (nhận giá trị null)
        Assert.Null(employeeInDb.DiaChi);
        Assert.Null(employeeInDb.TinhTrangHonNhan);
        Assert.NotEqual(initialAddress, employeeInDb.DiaChi);
        Assert.NotEqual(initialMaritalStatus, employeeInDb.TinhTrangHonNhan);
    }

    /// <summary>
    /// TEST 2: CHỨNG MINH NGỮ NGHĨA DTO KHÔNG THỂ PHÂN BIỆT GIỮA "KHÔNG GỬI TRƯỜNG" VÀ "CHỦ Ý XÓA VỀ NULL"
    /// 
    /// Ngữ cảnh so sánh 2 tình huống:
    /// - Tình huống A (Omitted): Client chỉ muốn cập nhật Họ tên, bỏ qua trường Email (không gửi).
    /// - Tình huống B (Intentional Null): Client chủ ý muốn xóa Email (đặt Email = null).
    /// 
    /// Trong mô hình DTO hiện tại (NhanVienCreateDTO là POCO), cả 2 tình huống đều tạo ra
    /// một DTO có `dto.Email == null`. Service legacy không thể nào nhận biết được ý định của client
    /// vì không có cờ IsSet, không có JsonPatchDocument, và không có cấu trúc Optional<T>.
    /// </summary>
    [Fact]
    public void DtoSemantics_CannotDistinguish_BetweenOmittedField_AndIntentionalNullDeletion()
    {
        // Tình huống A: Không gán Email (mặc định C# gán null)
        var dtoOmitted = new NhanVienCreateDTO
        {
            HoTen = "Lê Văn A"
        };

        // Tình huống B: Cố ý gán Email = null để xóa
        var dtoIntentionalNull = new NhanVienCreateDTO
        {
            HoTen = "Lê Văn A",
            Email = null
        };

        // CHỨNG MINH: Hai đối tượng DTO có trạng thái hoàn toàn giống hệt nhau về trường Email
        Assert.Equal(dtoOmitted.Email, dtoIntentionalNull.Email);
        Assert.Null(dtoOmitted.Email);
        Assert.Null(dtoIntentionalNull.Email);

        // Kết luận kỹ thuật: Với cấu trúc DTO hiện tại, backend legacy KHÔNG THỂ hỗ trợ partial update
        // an toàn mà không làm mất dữ liệu của các trường không được truyền.
    }

    /// <summary>
    /// TEST 3: KIỂU DỮ LIỆU VALUE TYPE (DateTime, Decimal) BỊ GÁN GIÁ TRỊ CLR DEFAULT KHI VẮNG MẶT
    /// 
    /// Ngữ cảnh: Trong NhanVienCreateDTO:
    ///   public DateTime NgaySinh { get; set; }
    ///   public decimal MucLuong { get; set; }
    /// Cả hai đều là non-nullable value types.
    /// Nếu client chỉ gửi thông tin cập nhật một trường duy nhất (ví dụ chỉ sửa Họ tên),
    /// hai trường này sẽ nhận giá trị CLR default: DateTime.MinValue và 0m.
    /// 
    /// Khi gọi UpdateAsync, nhân viên sẽ bị gán ngày sinh về 0001-01-01 và mức lương bị tụt về 0 VNĐ!
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenValueTypesOmittedInDto_CorruptsEntityWithClrDefaults()
    {
        using var db = CreateIsolatedDbContext();
        const int employeeId = 20;

        var existingEmployee = new NhanVien
        {
            MaNhanVien = employeeId,
            HoTen = "Trần Thị D",
            NgaySinh = new DateTime(1995, 8, 10),
            NgayVaoLam = new DateTime(2021, 3, 1),
            MucLuong = 20_000_000m,
            TrangThai = "Đang làm việc"
        };
        db.NhanVien.Add(existingEmployee);
        await db.SaveChangesAsync();

        var repo = new NhanVienRepository(db);
        var mockTaiKhoan = new Mock<ITaiKhoanService>();
        var mockBangLuong = new Mock<IBangLuongService>();

        var service = new NhanVienService(repo, mockTaiKhoan.Object, mockBangLuong.Object);

        // Payload giả lập client chỉ gửi HoTen mới, bỏ qua NgaySinh và MucLuong
        var partialDto = new NhanVienCreateDTO
        {
            HoTen = "Trần Thị D (Sửa Tên Duy Nhất)"
        };

        // Xác nhận giá trị mặc định của C#
        Assert.Equal(default(DateTime), partialDto.NgaySinh);
        Assert.Equal(0m, partialDto.MucLuong);

        // Thực thi cập nhật
        await service.UpdateAsync(employeeId, partialDto);

        // CHỨNG MINH: Dữ liệu trong DB bị hỏng nặng do CLR default
        db.ChangeTracker.Clear();
        var updated = await db.NhanVien.FindAsync(employeeId);
        Assert.NotNull(updated);
        Assert.Equal(DateTime.MinValue, updated.NgaySinh);
        Assert.Equal(0m, updated.MucLuong);
        Assert.NotEqual(20_000_000m, updated.MucLuong);
    }

    /// <summary>
    /// TEST 4: CHỨNG MINH TÁC DỤNG PHỤ (SIDE-EFFECT) LÀM BIẾN ĐỘNG LỊCH SỬ LƯƠNG (F02) KHI SỬA NHÂN VIÊN
    /// 
    /// Trong NhanVienService.cs (dòng 121-122):
    ///   if (entity.MucLuong != mucLuongCu)
    ///       await _bangLuongService.DongBoBangLuongTheoNhanVienAsync(id);
    /// 
    /// Khi sửa mức lương của nhân viên, service legacy tự ý kích hoạt hàm đồng bộ bảng lương,
    /// hàm này truy vấn toàn bộ bản ghi BangLuong trong quá khứ và ghi đè mức lương mới lên các kỳ đã khóa!
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenSalaryModified_InadvertentlyTriggersHistoricalPayrollSync_F02()
    {
        using var db = CreateIsolatedDbContext();
        const int employeeId = 30;

        var existingEmployee = new NhanVien
        {
            MaNhanVien = employeeId,
            HoTen = "Phạm Văn E",
            NgaySinh = new DateTime(1992, 1, 1),
            NgayVaoLam = new DateTime(2020, 1, 1),
            MucLuong = 12_000_000m,
            TrangThai = "Đang làm việc"
        };
        db.NhanVien.Add(existingEmployee);
        await db.SaveChangesAsync();

        var repo = new NhanVienRepository(db);
        var mockTaiKhoan = new Mock<ITaiKhoanService>();
        var mockBangLuong = new Mock<IBangLuongService>();

        var service = new NhanVienService(repo, mockTaiKhoan.Object, mockBangLuong.Object);

        // Cập nhật tăng lương lên 18.000.000
        var updateDto = new NhanVienCreateDTO
        {
            HoTen = existingEmployee.HoTen,
            NgaySinh = existingEmployee.NgaySinh,
            NgayVaoLam = existingEmployee.NgayVaoLam,
            MucLuong = 18_000_000m,
            TrangThai = existingEmployee.TrangThai
        };

        await service.UpdateAsync(employeeId, updateDto);

        // CHỨNG MINH: DongBoBangLuongTheoNhanVienAsync ĐÃ BỊ GỌI khi sửa lương
        mockBangLuong.Verify(b => b.DongBoBangLuongTheoNhanVienAsync(employeeId), Times.Once,
            "LỖ HỔNG F02: Khi sửa lương nhân viên, UpdateAsync tự động gọi DongBoBangLuongTheoNhanVienAsync, gây nguy cơ ghi đè bảng lương lịch sử!");
    }

    /// <summary>
    /// TEST 5: TEST SERVICE THỰC CHO TRƯỜNG CHỦ Ý GÁN NULL / XÓA DỮ LIỆU
    /// 
    /// Ngữ cảnh: Nhân viên ban đầu có DiaChi và TinhTrangHonNhan.
    /// Client CHỦ Ý muốn xóa địa chỉ và tình trạng hôn nhân (ví dụ: nhân viên chuyển đi chưa có địa chỉ mới,
    /// hoặc ly hôn/thay đổi trạng thái muốn đưa về null).
    /// DTO gửi lên với DiaChi = null và TinhTrangHonNhan = null.
    /// 
    /// Kết quả sau khi UpdateAsync: Database thực sự lưu giá trị null.
    /// Ý nghĩa kỹ thuật: Service thực thi thành công việc lưu null, nhưng vì cấu trúc DTO không có cờ phân biệt
    /// (như IsSet/JsonPatch), service xử lý Ca 5 (chủ ý xóa) và Ca 1 (vắng mặt ngoài ý muốn) HOÀN TOÀN GIỐNG NHAU.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenOptionalFieldsExplicitlySetToNull_PersistsNullInDatabase()
    {
        // 1. Arrange: Khởi tạo nhân viên có đầy đủ thông tin ban đầu
        using var db = CreateIsolatedDbContext();
        const int employeeId = 40;

        var existingEmployee = new NhanVien
        {
            MaNhanVien = employeeId,
            MaNV = "NV0040",
            HoTen = "Hoàng Văn F",
            NgaySinh = new DateTime(1988, 3, 12),
            GioiTinh = "Nam",
            CCCD = "079088009999",
            DiaChi = "789 Điện Biên Phủ, Bình Thạnh",
            SoDienThoai = "0918123456",
            Email = "hvf@example.com",
            TinhTrangHonNhan = "Đã kết hôn",
            NgayVaoLam = new DateTime(2019, 6, 1),
            MucLuong = 25_000_000m,
            TrangThai = "Đang làm việc"
        };
        db.NhanVien.Add(existingEmployee);
        await db.SaveChangesAsync();

        var repo = new NhanVienRepository(db);
        var mockTaiKhoan = new Mock<ITaiKhoanService>();
        var mockBangLuong = new Mock<IBangLuongService>();

        var service = new NhanVienService(repo, mockTaiKhoan.Object, mockBangLuong.Object);

        // 2. Act: Client cố ý gán DiaChi = null và TinhTrangHonNhan = null để xóa
        var explicitNullDto = new NhanVienCreateDTO
        {
            HoTen = "Hoàng Văn F",
            NgaySinh = new DateTime(1988, 3, 12),
            GioiTinh = "Nam",
            CCCD = "079088009999",
            DiaChi = null, // Chủ ý xóa địa chỉ
            SoDienThoai = "0918123456",
            Email = "hvf@example.com",
            TinhTrangHonNhan = null, // Chủ ý xóa tình trạng hôn nhân
            NgayVaoLam = new DateTime(2019, 6, 1),
            MucLuong = 25_000_000m,
            TrangThai = "Đang làm việc"
        };

        await service.UpdateAsync(employeeId, explicitNullDto);

        // 3. Assert: Truy vấn lại từ DB để xác nhận dữ liệu đã được cập nhật thành null
        db.ChangeTracker.Clear();
        var employeeInDb = await db.NhanVien.FindAsync(employeeId);
        Assert.NotNull(employeeInDb);
        Assert.Null(employeeInDb.DiaChi);
        Assert.Null(employeeInDb.TinhTrangHonNhan);
        Assert.Equal("Hoàng Văn F", employeeInDb.HoTen);
        Assert.Equal(25_000_000m, employeeInDb.MucLuong);
    }

    /// <summary>
    /// TEST 6: TEST ĐỌC GetByIdAsync XÁC NHẬN MapToDTO GIỮ NGUYÊN DiaChi, TinhTrangHonNhan VÀ TÊN PHÒNG BAN/CHỨC VỤ
    /// 
    /// Ngữ cảnh: Kiểm chứng tầng đọc dữ liệu GetByIdAsync -> MapToDTO.
    /// Fixture mẫu gồm Nhân viên có liên kết PhongBan ("Phòng Công nghệ Thông tin")
    /// và ChucVu ("Trưởng nhóm Kỹ thuật"), cùng với DiaChi và TinhTrangHonNhan.
    /// 
    /// Kết quả mong đợi:
    /// - DTO trả về bảo toàn nguyên vẹn DiaChi và TinhTrangHonNhan từ entity.
    /// - DTO lấy đúng TenPhongBan từ navigation property nv.PhongBan.TenPhongBan.
    /// - DTO lấy đúng TenChucVu từ navigation property nv.ChucVu.TenChucVu.
    /// 
    /// Ý nghĩa: Chứng minh tầng đọc (Query) của hệ thống legacy hoạt động đúng và giữ đủ dữ liệu,
    /// nhưng chính tầng ghi (UpdateAsync) khi dùng chung NhanVienCreateDTO đã làm rơi mất dữ liệu (F10).
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhenEmployeeHasFullDetailsAndRelations_PreservesAddressMaritalStatusAndNavNames()
    {
        // 1. Arrange: Khởi tạo dữ liệu liên kết Phòng ban, Chức vụ và Nhân viên
        using var db = CreateIsolatedDbContext();

        var phongBan = new PhongBan
        {
            MaPhongBan = 101,
            TenPhongBan = "Phòng Công nghệ Thông tin"
        };
        db.PhongBan.Add(phongBan);

        var chucVu = new ChucVu
        {
            MaChucVu = 201,
            TenChucVu = "Trưởng nhóm Kỹ thuật"
        };
        db.ChucVu.Add(chucVu);

        const int employeeId = 50;
        const string expectedAddress = "456 Nguyễn Huệ, Phường Bến Nghé, Quận 1";
        const string expectedMaritalStatus = "Độc thân";

        var employeeWithRelations = new NhanVien
        {
            MaNhanVien = employeeId,
            MaNV = "NV0050",
            HoTen = "Vũ Thị Mai",
            NgaySinh = new DateTime(1994, 11, 25),
            GioiTinh = "Nữ",
            CCCD = "079194008888",
            DiaChi = expectedAddress,
            SoDienThoai = "0933456789",
            Email = "vtm@example.com",
            TinhTrangHonNhan = expectedMaritalStatus,
            MaPhongBan = phongBan.MaPhongBan,
            PhongBan = phongBan,
            MaChucVu = chucVu.MaChucVu,
            ChucVu = chucVu,
            NgayVaoLam = new DateTime(2020, 8, 1),
            MucLuong = 30_000_000m,
            TrangThai = "Đang làm việc"
        };
        db.NhanVien.Add(employeeWithRelations);
        await db.SaveChangesAsync();

        var repo = new NhanVienRepository(db);
        var mockTaiKhoan = new Mock<ITaiKhoanService>();
        var mockBangLuong = new Mock<IBangLuongService>();

        var service = new NhanVienService(repo, mockTaiKhoan.Object, mockBangLuong.Object);

        // 2. Act: Gọi phương thức đọc dữ liệu chi tiết
        db.ChangeTracker.Clear();
        var dto = await service.GetByIdAsync(employeeId);

        // 3. Assert: Kiểm chứng toàn diện các trường nghiệp vụ và liên kết
        Assert.NotNull(dto);
        Assert.Equal(employeeId, dto.MaNhanVien);
        Assert.Equal("NV0050", dto.MaNV);
        Assert.Equal("Vũ Thị Mai", dto.HoTen);
        Assert.Equal(expectedAddress, dto.DiaChi);
        Assert.Equal(expectedMaritalStatus, dto.TinhTrangHonNhan);
        Assert.Equal(101, dto.MaPhongBan);
        Assert.Equal("Phòng Công nghệ Thông tin", dto.TenPhongBan);
        Assert.Equal(201, dto.MaChucVu);
        Assert.Equal("Trưởng nhóm Kỹ thuật", dto.TenChucVu);
        Assert.Equal(30_000_000m, dto.MucLuong);
    }
}
