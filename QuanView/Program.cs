using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using QuanApi.Services;
using QuanView.Models;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// CẤU HÌNH DbContext
builder.Services.AddDbContext<BanQuanAu1DbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(120); // 2 phút
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
});


// 2️⃣ CẤU HÌNH HttpClient GỌI API (chỉ đăng ký một lần)
builder.Services.AddHttpClient("MyApi", client =>
{
    var baseUrl = builder.Configuration["ApiSettings:KhachHangApiBaseUrl"]
        ?? builder.Configuration["ApiSettings:BaseUrl"]
        ?? "https://localhost:7130/api/";
    baseUrl = baseUrl.TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Đọc cấu hình từ appsettings
var emailConfig = builder.Configuration.GetSection("EmailSettings").Get<EmailConfig>();

// Đăng ký cấu hình và dịch vụ Email
builder.Services.AddSingleton(emailConfig);
builder.Services.AddScoped<IEmailService, EmailService>();

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
//builder.Services.AddAuthorization(options =>
//{
//    // Policy cho Admin area - chỉ admin và nhân viên mới được truy cập
//    options.AddPolicy("AdminPolicy", policy =>
//        policy.RequireRole("admin", "nhanvien"));

//    // Policy cho khách hàng
//    options.AddPolicy("CustomerPolicy", policy =>
//        policy.RequireRole("KhachHang"));
//});

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

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseSchemaRepair");
    var dbContext = scope.ServiceProvider.GetRequiredService<BanQuanAu1DbContext>();
    await EnsureBannerLinkSchemaAsync(dbContext, logger);
}

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

static async Task EnsureBannerLinkSchemaAsync(BanQuanAu1DbContext dbContext, ILogger logger)
{
    if (!dbContext.Database.IsNpgsql())
    {
        logger.LogInformation("Skip banner-link schema repair because provider is not PostgreSQL.");
        return;
    }

    const string sql = """
CREATE TABLE IF NOT EXISTS "BannerSanPhams" (
    "IDBannerSanPham" uuid NOT NULL PRIMARY KEY,
    "BannerId" integer NOT NULL,
    "IDSanPham" uuid NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_BannerSanPhams_BannerId" ON "BannerSanPhams" ("BannerId");
CREATE INDEX IF NOT EXISTS "IX_BannerSanPhams_IDSanPham" ON "BannerSanPhams" ("IDSanPham");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_BannerSanPhams_BannerId_IDSanPham" ON "BannerSanPhams" ("BannerId", "IDSanPham");

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_BannerSanPhams_Banners_BannerId'
    ) THEN
        ALTER TABLE "BannerSanPhams"
            ADD CONSTRAINT "FK_BannerSanPhams_Banners_BannerId"
            FOREIGN KEY ("BannerId") REFERENCES "Banners" ("Id") ON DELETE CASCADE;
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_BannerSanPhams_SanPhams_IDSanPham'
    ) THEN
        ALTER TABLE "BannerSanPhams"
            ADD CONSTRAINT "FK_BannerSanPhams_SanPhams_IDSanPham"
            FOREIGN KEY ("IDSanPham") REFERENCES "SanPhams" ("IDSanPham") ON DELETE CASCADE;
    END IF;
END
$$;
""";

    await dbContext.Database.ExecuteSqlRawAsync(sql);
    logger.LogInformation("Banner-link schema repair completed.");
}

