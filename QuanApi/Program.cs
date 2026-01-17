using System.Text.Json;
using System.Text.Json.Serialization;
using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddScoped<IHoaTietService,  HoaTietService>();
builder.Services.AddScoped<ILoaiOngService, LoaiOngService>();
builder.Services.AddScoped<INhanVienService, NhanVienService>();
builder.Services.AddScoped<IVaiTroService, VaiTroService>();

// Đăng ký Shipping Service
//builder.Services.AddScoped<IShippingService, ShippingService>();

//builder.Services.AddScoped<DotGiamGiaIRepository, DotGiamGiaRepository>();
//builder.Services.AddScoped<GioHangIRepository, GioHangRepository>();

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


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
