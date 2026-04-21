using HRM.BLL.Interfaces;
using HRM.BLL.Services;
using HRM.DAL.Context;
using HRM.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


namespace HRM.GUI;

static class Program
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // Tự động Migration khi khởi động
        using (var scope = ServiceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            try
            {
                // Tự động tạo DB và cập nhật bản migration mới nhất
                dbContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo Database: {ex.Message}\n\nHãy đảm bảo SQL Server (local) đang bật.",
                    "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        Application.Run(ServiceProvider.GetRequiredService<Forms.Auth.frmLogin>());
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // DbContext
        services.AddDbContext<HrmDbContext>(options =>
            options.UseSqlServer(
                "Server=.;Database=HRM_System;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
            ));

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<INhanVienRepository, NhanVienRepository>();
        services.AddScoped<IPhongBanRepository, PhongBanRepository>();
        services.AddScoped<IChamCongRepository, ChamCongRepository>();
        services.AddScoped<IDonNghiPhepRepository, DonNghiPhepRepository>();
        services.AddScoped<ITaiKhoanRepository, TaiKhoanRepository>();
        services.AddScoped<IPhongVanRepository, PhongVanRepository>();
        services.AddScoped<ITinTuyenDungRepository, TinTuyenDungRepository>();

        // Services (BLL)
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INhanVienService, NhanVienService>();
        services.AddScoped<IPhongBanService, PhongBanService>();
        services.AddScoped<IChamCongService, ChamCongService>();
        services.AddScoped<IDonNghiPhepService, DonNghiPhepService>();
        services.AddScoped<ITaiKhoanService, TaiKhoanService>(); // Add TaiKhoanService
        services.AddScoped<IPhongVanService, PhongVanService>();
        services.AddScoped<ITinTuyenDungService, TinTuyenDungService>();

        // Forms
        services.AddTransient<Forms.Auth.frmLogin>();
        services.AddTransient<Forms.Main.frmMain>();
        services.AddTransient<Forms.Auth.frmTaoTaiKhoan>();
        services.AddTransient<Forms.Main.frmThemNhanVien>();
        services.AddTransient<Forms.Main.frmThemPhongVan>();
        services.AddTransient<Forms.Main.TinTuyenDung.frmThemTinTuyenDung>();
        services.AddTransient<Forms.Main.TinTuyenDung.frmSuaTinTuyenDung>();
    }
}