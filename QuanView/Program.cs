using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using QuanView.Models;
using QuanView.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ CẤU HÌNH DbContext
builder.Services.AddDbContext<BanQuanAu1DbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), 
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.CommandTimeout(120); // 2 phút timeout
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
});

// 2️⃣ CẤU HÌNH HttpClient GỌI API
builder.Services.AddHttpClient("MyApi", client =>
{
    var baseUrl = builder.Configuration["ApiSettings:KhachHangApiBaseUrl"];
    if (string.IsNullOrEmpty(baseUrl))
    {
        throw new InvalidOperationException("Thiếu cấu hình 'ApiSettings:KhachHangApiBaseUrl' trong appsettings.json.");
    }
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient("MyApi", c => c.BaseAddress = new Uri("https://localhost:7130/api/"));

// Đọc cấu hình từ appsettings
var emailConfig = builder.Configuration.GetSection("EmailSettings").Get<EmailConfig>();

// Đăng ký cấu hình và dịch vụ Email
builder.Services.AddSingleton(emailConfig);
builder.Services.AddSingleton<IEmailService, EmailService>();

//Connect VNPay API
builder.Services.AddScoped<IVnPayService, VnPayService>();


// 3️⃣ CẤU HÌNH XÁC THỰC Google + Cookie
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Login/Index";
    options.LogoutPath = "/Login/Logout";
    options.AccessDeniedPath = "/Login/AccessDenied";
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["GoogleKeys:ClientId"];
    options.ClientSecret = builder.Configuration["GoogleKeys:ClientSecret"];
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.CallbackPath = "/signin-google";
    options.Scope.Add("profile");
    options.ClaimActions.MapJsonKey("picture", "picture", "url");

    options.Events = new OAuthEvents
    {
        OnRemoteFailure = context =>
        {
            context.Response.Redirect("/Home/Index?error=" + Uri.EscapeDataString(context.Failure?.Message ?? "unknown"));
            context.HandleResponse();
            return Task.CompletedTask;
        },
        OnCreatingTicket = ctx =>
        {
            var name = ctx.Identity.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(name))
            {
                ctx.Identity.AddClaim(new Claim(ClaimTypes.Name, name));
            }
            return Task.CompletedTask;
        }
    };
});

// 3️⃣ CẤU HÌNH AUTHORIZATION
builder.Services.AddAuthorization(options =>
{
    // Policy cho Admin area - chỉ admin và nhân viên mới được truy cập
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireRole("admin", "nhanvien"));
    
    // Policy cho khách hàng
    options.AddPolicy("CustomerPolicy", policy =>
        policy.RequireRole("KhachHang"));
});

// 4️⃣ CẤU HÌNH CORS CHO FRONTEND
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:7286")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 5️⃣ CẤU HÌNH JSON VÀ MVC
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// 6️⃣ ĐĂNG KÝ HttpContextAccessor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 7️⃣ MIDDLEWARE PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

// 8️⃣ ROUTING
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=ProductManage}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

