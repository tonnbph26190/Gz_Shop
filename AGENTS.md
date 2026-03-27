# AGENTS.md

## Cursor Cloud specific instructions

### Project Overview
This is **Dazio (GZ Shop)** — a Vietnamese e-commerce platform for selling dress pants/trousers, built with ASP.NET Core 8.0. It consists of two projects in `QuanApi.sln`:

- **QuanApi** (REST API, port 5228 HTTP / 7130 HTTPS): Backend with 158+ endpoints, Swagger UI at `/swagger`
- **QuanView** (MVC frontend, port 5261 HTTP / 7182 HTTPS): Customer storefront + Admin area, uses Razor views with Bootstrap/jQuery

### Prerequisites
- **.NET 8 SDK** — install via `dotnet-install.sh --channel 8.0`
- **PostgreSQL** — required for both projects; both connect via Npgsql
- **EF Core tools** — `dotnet tool install --global dotnet-ef`

### Running the applications

Both projects use the connection string from `ConnectionStrings:DefaultConnection`. Override it via the environment variable `ConnectionStrings__DefaultConnection` to point to a local PostgreSQL instance.

QuanView calls QuanApi via HTTP. Override the API base URL with `ApiSettings__KhachHangApiBaseUrl`.

**Start QuanApi first** (QuanView depends on it at runtime):
```bash
# Start API (HTTP mode, port 5228)
cd /workspace/QuanApi
ConnectionStrings__DefaultConnection="Server=localhost;port=5432;Database=SD45_1;User Id=postgres;Password=postgres;CommandTimeout=60;Pooling=true;MinPoolSize=1;MaxPoolSize=200" \
dotnet run --launch-profile http

# Start QuanView (HTTP mode, port 5261)
cd /workspace/QuanView
ConnectionStrings__DefaultConnection="Server=localhost;port=5432;Database=SD45_1;User Id=postgres;Password=postgres;CommandTimeout=60;Pooling=true;MinPoolSize=1;MaxPoolSize=200" \
ApiSettings__KhachHangApiBaseUrl="http://localhost:5228/api" \
dotnet run --launch-profile http
```

### Database setup
Run EF migrations against the local PostgreSQL DB:
```bash
ConnectionStrings__DefaultConnection="Server=localhost;..." dotnet ef database update --project QuanApi/QuanApi.csproj
```
The `SeedVaiTroData` migration seeds three roles: ADMIN, NHANVIEN, KHACHHANG.

### Gotchas
- **No HTTPS in containers**: Use the `http` launch profile to avoid dev-cert issues. Set `ApiSettings__KhachHangApiBaseUrl` to `http://localhost:5228/api` for QuanView.
- **Login uses plain-text password comparison** (no hashing) — see `LoginController.FormLogin()`.
- **Test project incomplete**: `QuanApi.Tests/` has test source files but no `.csproj`. Tests are not currently buildable/runnable.
- **No README/docs exist** in the repository.
- The `appsettings.json` files contain a remote PostgreSQL server connection string. For local development, always override via environment variables.

### Build & Lint
```bash
dotnet build QuanApi.sln    # Builds both projects
```
No separate linter is configured; the build includes compiler warnings.

### Admin test account
Create an admin user in the local database for testing:
```sql
INSERT INTO "NhanViens" ("IDNhanVien", "MaNhanVien", "TenNhanVien", "Email", "MatKhau", "SoDienThoai", "NgayTao", "TrangThai", "IDVaiTro")
VALUES (gen_random_uuid(), 'NV001', 'Admin User', 'admin@dazio.com', 'admin123', '0123456789', NOW(), true, 'a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d');
```
Login at `http://localhost:5261/Login/Index` with email `admin@dazio.com` / password `admin123`.
