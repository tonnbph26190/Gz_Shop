using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using BanQuanAu1.Web.Data;

using QuanApi.Repository;
using QuanApi.Repository.IRepository;

using QuanApi.Models; 
using QuanApi.Services; 

using AutoMapper;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using QuanApi.MappingProfiles;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<BanQuanAu1DbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Đăng ký các dịch vụ
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOrderHistoryService, OrderHistoryService>();
builder.Services.AddScoped<SanPhamValidationService>();

// Đăng ký Shipping Service
builder.Services.AddScoped<IShippingService, ShippingService>();

builder.Services.AddScoped<DotGiamGiaIRepository, DotGiamGiaRepository>();
builder.Services.AddScoped<GioHangIRepository, GioHangRepository>();

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
