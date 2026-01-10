# Sửa lỗi Type Conversion Error CS0029

## Mô tả lỗi
Lỗi biên dịch C# CS0029: "Cannot implicitly convert type 'System.Collections.Generic.List<AnhSanPhamDto>' to 'System.Collections.Generic.List<QuanView.Areas.Admin.Models.AnhSanPhamDto>'"

## Nguyên nhân
Có hai class `AnhSanPhamDto` khác nhau trong hai namespace khác nhau:
1. `QuanApi.Dtos.AnhSanPhamDto` (từ API project)
2. `QuanView.Areas.Admin.Models.AnhSanPhamDto` (từ Admin Models)

Khi deserialize JSON từ API, nó tạo ra `List<QuanApi.Dtos.AnhSanPhamDto>` nhưng cần gán cho `List<QuanView.Areas.Admin.Models.AnhSanPhamDto>`.

## Giải pháp đã áp dụng

### 1. Thêm using statements
```csharp
using QuanApi.Dtos;
using System.Linq;
```

### 2. Tạo helper method để map giữa hai types
```csharp
private List<QuanView.Areas.Admin.Models.AnhSanPhamDto> MapApiImagesToAdminImages(List<QuanApi.Dtos.AnhSanPhamDto> apiImages)
{
    return apiImages?.Select(img => new QuanView.Areas.Admin.Models.AnhSanPhamDto
    {
        IDAnhSanPham = img.IDAnhSanPham,
        MaAnh = img.MaAnh,
        IDSanPhamChiTiet = img.IDSanPhamChiTiet,
        UrlAnh = img.UrlAnh,
        LaAnhChinh = img.LaAnhChinh,
        NgayTao = img.NgayTao,
        NguoiTao = img.NguoiTao,
        LanCapNhatCuoi = img.LanCapNhatCuoi,
        NguoiCapNhat = img.NguoiCapNhat,
        TrangThai = img.TrangThai
    }).ToList() ?? new List<QuanView.Areas.Admin.Models.AnhSanPhamDto>();
}
```

### 3. Cập nhật tất cả các dòng deserialize
Thay vì:
```csharp
ct.DanhSachAnh = JsonSerializer.Deserialize<List<AnhSanPhamDto>>(imagesJson, options);
```

Sử dụng:
```csharp
var apiImages = JsonSerializer.Deserialize<List<QuanApi.Dtos.AnhSanPhamDto>>(imagesJson, options);
ct.DanhSachAnh = MapApiImagesToAdminImages(apiImages);
```

## Files đã sửa

### 1. SanPhamController.cs
- Thêm using statements
- Thêm helper method `MapApiImagesToAdminImages`
- Cập nhật 3 vị trí deserialize trong các methods:
  - `Index()` method
  - `Edit(Guid id)` method  
  - `Delete(Guid id)` method

### 2. AnhSanPhamController.cs
- Thêm using statements
- Thêm helper method `MapApiImagesToAdminImages`
- Cập nhật `GetImages()` method

## Kết quả
- ✅ Lỗi biên dịch CS0029 đã được sửa
- ✅ Code trở nên sạch sẽ hơn với helper method
- ✅ Tránh lặp lại code mapping
- ✅ Dễ bảo trì và mở rộng trong tương lai

## Lưu ý
- Cần đảm bảo rằng cả hai DTO classes có cùng structure
- Nếu có thay đổi trong API DTO, cần cập nhật helper method tương ứng
- Có thể cân nhắc sử dụng AutoMapper trong tương lai để tự động mapping 