using System.Reflection;
using System.Text.RegularExpressions;
using HRM.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace HRM.Tests.Regression.Bugs;

public class F01_MigrationHwidTests
{
    /// <summary>
    /// F01.1: Kiểm tra metadata discovery của các migration trong HRM.DAL.
    ///
    /// Nghiệp vụ / Thiết kế chuẩn:
    /// - Toàn bộ các class migration kế thừa từ Migration trong HRM.DAL phải có [MigrationAttribute]
    ///   hợp lệ để EF Core migrations discovery nhận diện đầy đủ.
    ///
    /// Hành vi lỗi ở baseline:
    /// - File 20260504161102_AddHwidToChamCong.cs có class AddHwidToChamCong nhưng THIẾU [MigrationAttribute].
    /// - Kết quả: EF Core migrations assembly bỏ qua migration này, chỉ nhận diện 6/7 migration -> BUG REPRODUCED.
    /// </summary>
    [Fact]
    [Trait("Category", "RegressionBug")]
    public void AllMigrationClasses_MustHaveValidMigrationAttribute()
    {
        var dalAssembly = typeof(HrmDbContext).Assembly;
        var migrationTypes = dalAssembly.GetTypes()
            .Where(t => typeof(Migration).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        foreach (var mType in migrationTypes)
        {
            var migrationAttr = mType.GetCustomAttribute<MigrationAttribute>();
            Assert.True(migrationAttr != null,
                $"LỖ HỔNG F01 (Metadata): Migration class '{mType.Name}' thiếu [MigrationAttribute]. EF Core migrations discovery sẽ bỏ qua migration này!");
        }
    }

    /// <summary>
    /// F01.2: Kiểm tra độc lập schema sinh từ EF Core migrations / IMigrator offline.
    ///
    /// Mục tiêu:
    /// - Xác minh độc lập rằng DDL schema được tạo bởi chuỗi EF Core migrations thông qua IMigrator
    ///   thực sự có định nghĩa cột Hwid trên bảng ChamCong (thông qua ALTER TABLE hoặc CREATE TABLE).
    /// - Test này không kết nối SQL Server thật.
    /// - Test này KHÔNG assert tên migration history cụ thể để không buộc bản sửa tương lai phải
    ///   chỉnh đúng một class migration lịch sử cụ thể (schema có thể được tạo bởi migration hiện tại hoặc bổ sung).
    ///
    /// Kết quả trên baseline:
    /// - EF Migrator sinh thành công DDL schema có lệnh ALTER TABLE [ChamCong] ADD [Hwid] nvarchar(128) NULL.
    /// - Test này đạt chuẩn (PASS), đóng vai trò kiểm thử năng lực sinh schema offline của EF Core.
    /// </summary>
    [Fact]
    [Trait("Category", "HarnessCheck")]
    public void EfMigrator_OfflineScript_SchemaMustContainHwidColumnOnChamCong()
    {
        var options = new DbContextOptionsBuilder<HrmDbContext>()
            .UseSqlServer("Server=(localdb)\\dummy;Database=HrmBaselineOffline;Trusted_Connection=True;")
            .Options;

        using var context = new HrmDbContext(options);
        var migrator = context.Database.GetService<IMigrator>();
        var offlineScript = migrator.GenerateScript(fromMigration: null, toMigration: null);

        // Kiểm tra DDL schema có thao tác tạo cột Hwid trên bảng ChamCong
        var hasHwidColumnInChamCong = Regex.IsMatch(
            offlineScript,
            @"(ALTER\s+TABLE\s+\[ChamCong\]\s+ADD\s+\[Hwid\]|CREATE\s+TABLE\s+\[ChamCong\].*?\[Hwid\])",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        Assert.True(hasHwidColumnInChamCong,
            "LỖ HỔNG F01 (EF Schema): Script DDL sinh từ IMigrator không hề có thao tác tạo cột Hwid trên bảng ChamCong!");
    }

    /// <summary>
    /// F01.3: Kiểm tra riêng rẽ MigrationOperations của class AddHwidToChamCong.
    ///
    /// Mục tiêu:
    /// - Tách riêng kiểm tra phương thức Up() của class AddHwidToChamCong để kiểm tra xem class này
    ///   có thực sự thực thi thao tác tạo cột Hwid hay không (tránh lỗi ngầm chỉ đổi seed data).
    ///
    /// Hành vi lỗi ở baseline:
    /// - Phương thức Up() của class AddHwidToChamCong chỉ cập nhật seed data NhanVien/PhongBan,
    ///   hoàn toàn KHÔNG chứa AddColumnOperation cho cột Hwid -> BUG REPRODUCED.
    /// </summary>
    [Fact]
    [Trait("Category", "RegressionBug")]
    public void AddHwidToChamCong_MigrationOperations_MustContainAddHwidColumn()
    {
        var addHwidType = typeof(HrmDbContext).Assembly.GetType("HRM.DAL.Migrations.AddHwidToChamCong");
        Assert.NotNull(addHwidType);

        var migrationInstance = (Migration)Activator.CreateInstance(addHwidType)!;
        var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var upMethod = addHwidType.GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(upMethod);
        upMethod.Invoke(migrationInstance, new object[] { builder });

        var hasAddColumnOp = builder.Operations.OfType<AddColumnOperation>()
            .Any(op => op.Table == "ChamCong" && op.Name == "Hwid");

        Assert.True(hasAddColumnOp,
            "LỖ HỔNG F01 (Operations): Migration 'AddHwidToChamCong' không có thao tác AddColumn('Hwid') trên bảng 'ChamCong'!");
    }

    /// <summary>
    /// F01.4: Kiểm tra file script SQL tài liệu hệ thống (db/docs/HRM_Database.sql).
    ///
    /// Lưu ý: Đây là kiểm tra file script tài liệu lưu kèm dự án; không gọi file này là bằng chứng schema sinh từ EF.
    ///
    /// Hành vi lỗi ở baseline:
    /// - File db/docs/HRM_Database.sql định nghĩa bảng ChamCong không có cột Hwid -> BUG REPRODUCED.
    /// </summary>
    [Fact]
    [Trait("Category", "RegressionBug")]
    public void DatabaseDocumentationScript_MustDefineHwidColumnOnChamCongTable()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null && !Directory.Exists(Path.Combine(currentDir.FullName, "db", "docs")))
        {
            currentDir = currentDir.Parent;
        }

        Assert.NotNull(currentDir);
        var scriptPath = Path.Combine(currentDir.FullName, "db", "docs", "HRM_Database.sql");
        Assert.True(File.Exists(scriptPath), $"Không tìm thấy file schema script tại {scriptPath}");

        var scriptContent = File.ReadAllText(scriptPath);

        var chamCongTableRegex = new Regex(
            @"CREATE\s+TABLE\s+ChamCong\s*\((?<bodys>.*?)\);",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        var match = chamCongTableRegex.Match(scriptContent);
        Assert.True(match.Success, "Không tìm thấy câu lệnh CREATE TABLE ChamCong trong script HRM_Database.sql!");

        var tableBody = match.Groups["bodys"].Value;
        var hasHwidColumn = Regex.IsMatch(
            tableBody,
            @"\bHwid\s+NVARCHAR\s*\(",
            RegexOptions.IgnoreCase);

        Assert.True(hasHwidColumn,
            "LỖ HỔNG F01 (SQL Tài liệu): Cột 'Hwid' hoàn toàn không có trong định nghĩa bảng ChamCong của file script tài liệu db/docs/HRM_Database.sql!");
    }
}
