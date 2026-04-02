using System.Text.Json;
using System.Text.Json.Serialization;
using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Repository;
using QuanApi.Repository.IRepository;
using QuanApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BanQuanAu1DbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Đăng ký các dịch vụ
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOrderHistoryService, OrderHistoryService>();
builder.Services.AddScoped<IKhachHangService, KhachHangService>();
builder.Services.AddScoped<IKhachHangPhieuGiamService, KhachHangPhieuGiamService>();
builder.Services.AddScoped<IPhieuGiamGiaService, PhieuGiamGiaService>();
builder.Services.AddScoped<IDotGiamGiaService, DotGiamGiaService>();
builder.Services.AddScoped<ISanPhamNguoiDungService, SanPhamNguoiDungService>();
builder.Services.AddScoped<GioHangIRepository, GioHangRepository>();
builder.Services.AddScoped<INhanVienService, NhanVienService>();
builder.Services.AddScoped<IThuongHieuService, ThuongHieuService>();
builder.Services.AddScoped<ISanPhamService, SanPhamService>();
builder.Services.AddScoped<ISanPhamChiTietService, SanPhamChiTietService>();
builder.Services.AddScoped<IChatLieuService, ChatLieuService>();
builder.Services.AddScoped<IMauSacService, MauSacService>();
builder.Services.AddScoped<IKichCoService, KichCoService>();
builder.Services.AddScoped<SanPhamValidationService>();
builder.Services.AddScoped<IDanhMucService, DanhMucService>();
builder.Services.AddScoped<ILoaiOngService, LoaiOngService>();
builder.Services.AddScoped<IKieuDangService, KieuDangService>();
builder.Services.AddScoped<IHoaTietService, HoaTietService>();
builder.Services.AddScoped<IVaiTroService, VaiTroService>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
builder.Services.AddScoped<IShippingPolicyService, ShippingPolicyService>();
builder.Services.Configure<QuanApi.Models.GHNSettings>(builder.Configuration.GetSection(QuanApi.Models.GHNSettings.SectionName));
builder.Services.AddHttpClient<IGHNService, GHNService>();
builder.Services.AddScoped<IPhuongThucThanhToanService, PhuongThucThanhToanService>();
builder.Services.AddScoped<IHoaDonService, HoaDonService>();
builder.Services.AddScoped<IGioHangService, GioHangService>();
builder.Services.AddScoped<IBanHangTaiQuayService, BanHangTaiQuayService>();
builder.Services.AddScoped<ILungQuanService, LungQuanService>();

builder.Services.AddScoped<DotGiamGiaIRepository, DotGiamGiaRepository>();

var profileType = Type.GetType("MyApi.MappingProfiles.KhachHangMappingProfile, QuanApi");
if (profileType != null)
{
    Console.WriteLine($"[DIAGNOSTIC] Đã tìm thấy KhachHangMappingProfile: {profileType.FullName}");
    builder.Services.AddAutoMapper(profileType.Assembly);
}
else
{
    Console.WriteLine("[DIAGNOSTIC] KHÔNG tìm thấy KhachHangMappingProfile. Sử dụng fallback.");
    builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseMigration");
    var dbContext = scope.ServiceProvider.GetRequiredService<BanQuanAu1DbContext>();

    try
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending migrations: {Migrations}",
                pendingMigrations.Count(),
                string.Join(", ", pendingMigrations));
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migration completed.");
        }
        else
        {
            logger.LogInformation("Database schema is up to date.");
        }

        await EnsureLoyaltyShippingSchemaAsync(dbContext, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations at startup.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

static async Task EnsureLoyaltyShippingSchemaAsync(BanQuanAu1DbContext dbContext, ILogger logger)
{
    if (!dbContext.Database.IsNpgsql())
    {
        logger.LogInformation("Skip loyalty/shipping schema self-heal because provider is not PostgreSQL.");
        return;
    }

    // Idempotent schema repair for environments where migration history and real schema diverge.
    const string sql = """
CREATE TABLE IF NOT EXISTS "CauHinhBanHangs" (
    "IDCauHinhBanHang" uuid NOT NULL PRIMARY KEY,
    "PhiShipMacDinh" numeric NOT NULL DEFAULT 50000,
    "PhiShipNoiThanh" numeric NOT NULL DEFAULT 20000,
    "PhiShipNgoaiThanh" numeric NOT NULL DEFAULT 35000,
    "PhiShipToanQuoc" numeric NOT NULL DEFAULT 50000,
    "TinhApDungPhiShip" character varying(100) NOT NULL DEFAULT 'Hà Nội',
    "DanhSachQuanHuyenNoiThanh" character varying(2000) NULL,
    "NguonTinhPhiShipMacDinh" character varying(20) NOT NULL DEFAULT 'Config',
    "SoTienTrenMotDiemTich" numeric NOT NULL DEFAULT 10000,
    "SoTienGiamTrenMotDiem" numeric NOT NULL DEFAULT 1000,
    "DiemToiDaSuDungMoiDon" integer NOT NULL DEFAULT 0,
    "NgayTao" timestamp with time zone NOT NULL DEFAULT now(),
    "NguoiTao" character varying(100) NULL,
    "LanCapNhatCuoi" timestamp with time zone NULL,
    "NguoiCapNhat" character varying(100) NULL,
    "TrangThai" boolean NOT NULL DEFAULT TRUE
);

ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "PhiShipMacDinh" numeric NOT NULL DEFAULT 50000;
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "PhiShipNoiThanh" numeric NOT NULL DEFAULT 20000;
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "PhiShipNgoaiThanh" numeric NOT NULL DEFAULT 35000;
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "PhiShipToanQuoc" numeric NOT NULL DEFAULT 50000;
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "TinhApDungPhiShip" character varying(100) NOT NULL DEFAULT 'Hà Nội';
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "DanhSachQuanHuyenNoiThanh" character varying(2000) NULL;
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "NguonTinhPhiShipMacDinh" character varying(20) NOT NULL DEFAULT 'Config';
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "SoTienTrenMotDiemTich" numeric NOT NULL DEFAULT 10000;
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "SoTienGiamTrenMotDiem" numeric NOT NULL DEFAULT 1000;
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "DiemToiDaSuDungMoiDon" integer NOT NULL DEFAULT 0;
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "NgayTao" timestamp with time zone NOT NULL DEFAULT now();
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "NguoiTao" character varying(100) NULL;
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "LanCapNhatCuoi" timestamp with time zone NULL;
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "NguoiCapNhat" character varying(100) NULL;
ALTER TABLE "CauHinhBanHangs" ADD COLUMN IF NOT EXISTS "TrangThai" boolean NOT NULL DEFAULT TRUE;

CREATE TABLE IF NOT EXISTS "HangKhachHangs" (
    "IDHangKhachHang" uuid NOT NULL PRIMARY KEY,
    "MaHang" character varying(50) NOT NULL,
    "TenHang" character varying(100) NOT NULL,
    "DiemTu" integer NOT NULL,
    "DiemDen" integer NULL,
    "PhanTramGiamPhiShip" numeric NOT NULL DEFAULT 0,
    "NgayTao" timestamp with time zone NOT NULL DEFAULT now(),
    "NguoiTao" character varying(100) NULL,
    "LanCapNhatCuoi" timestamp with time zone NULL,
    "NguoiCapNhat" character varying(100) NULL,
    "TrangThai" boolean NOT NULL DEFAULT TRUE
);

ALTER TABLE "HangKhachHangs" ADD COLUMN IF NOT EXISTS "MaHang" character varying(50) NOT NULL DEFAULT '';
ALTER TABLE "HangKhachHangs" ADD COLUMN IF NOT EXISTS "TenHang" character varying(100) NOT NULL DEFAULT '';
ALTER TABLE "HangKhachHangs" ADD COLUMN IF NOT EXISTS "DiemTu" integer NOT NULL DEFAULT 0;
ALTER TABLE "HangKhachHangs" ADD COLUMN IF NOT EXISTS "DiemDen" integer NULL;
ALTER TABLE "HangKhachHangs" ADD COLUMN IF NOT EXISTS "PhanTramGiamPhiShip" numeric NOT NULL DEFAULT 0;
ALTER TABLE "HangKhachHangs" ADD COLUMN IF NOT EXISTS "NgayTao" timestamp with time zone NOT NULL DEFAULT now();
ALTER TABLE "HangKhachHangs" ADD COLUMN IF NOT EXISTS "NguoiTao" character varying(100) NULL;
ALTER TABLE "HangKhachHangs" ADD COLUMN IF NOT EXISTS "LanCapNhatCuoi" timestamp with time zone NULL;
ALTER TABLE "HangKhachHangs" ADD COLUMN IF NOT EXISTS "NguoiCapNhat" character varying(100) NULL;
ALTER TABLE "HangKhachHangs" ADD COLUMN IF NOT EXISTS "TrangThai" boolean NOT NULL DEFAULT TRUE;

ALTER TABLE "KhachHang" ADD COLUMN IF NOT EXISTS "IDHangKhachHang" uuid NULL;
ALTER TABLE "KhachHang" ADD COLUMN IF NOT EXISTS "SoDiemHienTai" integer NOT NULL DEFAULT 0;
ALTER TABLE "KhachHang" ADD COLUMN IF NOT EXISTS "TongDiemTichLuy" integer NOT NULL DEFAULT 0;

ALTER TABLE "HoaDons" ADD COLUMN IF NOT EXISTS "DiemCong" integer NOT NULL DEFAULT 0;
ALTER TABLE "HoaDons" ADD COLUMN IF NOT EXISTS "DiemDaDung" integer NOT NULL DEFAULT 0;
ALTER TABLE "HoaDons" ADD COLUMN IF NOT EXISTS "PhiVanChuyenGoc" numeric NULL;
ALTER TABLE "HoaDons" ADD COLUMN IF NOT EXISTS "SoTienGiamPhiVanChuyen" numeric NULL;
ALTER TABLE "HoaDons" ADD COLUMN IF NOT EXISTS "SoTienGiamTuDiem" numeric NOT NULL DEFAULT 0;
ALTER TABLE "HoaDons" ADD COLUMN IF NOT EXISTS "TyLeQuyDoiDiem" numeric NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS "LichSuDiemKhachHangs" (
    "IDLichSuDiemKhachHang" uuid NOT NULL PRIMARY KEY,
    "IDKhachHang" uuid NOT NULL,
    "IDHoaDon" uuid NULL,
    "LoaiBienDong" character varying(30) NOT NULL,
    "SoDiemBienDong" integer NOT NULL,
    "SoDiemTruoc" integer NOT NULL,
    "SoDiemSau" integer NOT NULL,
    "MoTa" character varying(255) NULL,
    "NgayTao" timestamp with time zone NOT NULL DEFAULT now(),
    "NguoiTao" character varying(100) NULL,
    "TrangThai" boolean NOT NULL DEFAULT TRUE
);

INSERT INTO "CauHinhBanHangs"
    ("IDCauHinhBanHang", "PhiShipMacDinh", "PhiShipNoiThanh", "PhiShipNgoaiThanh", "PhiShipToanQuoc",
     "TinhApDungPhiShip", "DanhSachQuanHuyenNoiThanh", "NguonTinhPhiShipMacDinh",
     "SoTienTrenMotDiemTich", "SoTienGiamTrenMotDiem", "DiemToiDaSuDungMoiDon", "NgayTao", "NguoiTao", "TrangThai")
SELECT
    '8f3aa6f2-0608-4f47-a406-5de9ef3366d2'::uuid, 50000, 20000, 35000, 50000,
    'Hà Nội', 'Ba Đình,Hoàn Kiếm,Tây Hồ,Long Biên,Cầu Giấy,Đống Đa,Hai Bà Trưng,Hoàng Mai,Thanh Xuân,Nam Từ Liêm,Bắc Từ Liêm,Hà Đông',
    'Config', 10000, 1000, 0, now(), 'System', TRUE
WHERE NOT EXISTS (SELECT 1 FROM "CauHinhBanHangs" WHERE "TrangThai" = TRUE);

INSERT INTO "HangKhachHangs"
    ("IDHangKhachHang", "MaHang", "TenHang", "DiemTu", "DiemDen", "PhanTramGiamPhiShip", "NgayTao", "NguoiTao", "TrangThai")
SELECT 'd58c8c83-0f0a-48a8-a2dd-c7ac8f668f69'::uuid, 'BRONZE', 'Đồng', 0, 499, 0, now(), 'System', TRUE
WHERE NOT EXISTS (SELECT 1 FROM "HangKhachHangs" WHERE "MaHang" = 'BRONZE');

INSERT INTO "HangKhachHangs"
    ("IDHangKhachHang", "MaHang", "TenHang", "DiemTu", "DiemDen", "PhanTramGiamPhiShip", "NgayTao", "NguoiTao", "TrangThai")
SELECT '49cb8a18-d15d-4df5-97be-f98f6ef88ca4'::uuid, 'SILVER', 'Bạc', 500, 1499, 10, now(), 'System', TRUE
WHERE NOT EXISTS (SELECT 1 FROM "HangKhachHangs" WHERE "MaHang" = 'SILVER');

INSERT INTO "HangKhachHangs"
    ("IDHangKhachHang", "MaHang", "TenHang", "DiemTu", "DiemDen", "PhanTramGiamPhiShip", "NgayTao", "NguoiTao", "TrangThai")
SELECT 'b72f6741-bfd9-4c29-8aeb-6f4f2b8cbf3d'::uuid, 'GOLD', 'Vàng', 1500, 2999, 20, now(), 'System', TRUE
WHERE NOT EXISTS (SELECT 1 FROM "HangKhachHangs" WHERE "MaHang" = 'GOLD');

INSERT INTO "HangKhachHangs"
    ("IDHangKhachHang", "MaHang", "TenHang", "DiemTu", "DiemDen", "PhanTramGiamPhiShip", "NgayTao", "NguoiTao", "TrangThai")
SELECT '38fbce12-fc6f-4d1f-badf-fc2b70f6c396'::uuid, 'PLATINUM', 'Bạch kim', 3000, NULL, 30, now(), 'System', TRUE
WHERE NOT EXISTS (SELECT 1 FROM "HangKhachHangs" WHERE "MaHang" = 'PLATINUM');
""";

    await dbContext.Database.ExecuteSqlRawAsync(sql);
    logger.LogInformation("Loyalty/shipping schema self-heal completed.");
}