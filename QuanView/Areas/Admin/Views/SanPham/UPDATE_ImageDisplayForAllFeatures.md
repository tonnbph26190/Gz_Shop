# Cập nhật hiển thị ảnh cho tất cả các chức năng

## Tổng quan

Đã cập nhật hiển thị ảnh sản phẩm cho tất cả các chức năng trong hệ thống để đảm bảo tính nhất quán và trải nghiệm người dùng tốt hơn.

## Các chức năng đã được cập nhật

### 1. **Quản lý sản phẩm (Admin)**
- ✅ **Trang Index**: Hiển thị ảnh trong bảng biến thể sản phẩm
- ✅ **Modal quản lý ảnh**: Thêm, xem, xóa, đặt ảnh chính
- ✅ **Real-time updates**: Cập nhật giao diện sau khi thay đổi ảnh

### 2. **Bán hàng tại quầy (Admin)**
- ✅ **API BanHangTaiQuay**: Thêm trường `mainImage` cho ảnh chính
- ✅ **Danh sách sản phẩm**: Hiển thị ảnh sản phẩm trong POS
- ✅ **Chi tiết sản phẩm**: Hiển thị đầy đủ ảnh và thông tin

### 3. **Giỏ hàng (Customer)**
- ✅ **GioHangRepository**: Include ảnh sản phẩm khi load giỏ hàng
- ✅ **Session cart**: Hiển thị ảnh cho giỏ hàng tạm thời
- ✅ **Database cart**: Hiển thị ảnh cho giỏ hàng đã đăng nhập
- ✅ **API GetGioHang**: Trả về ảnh sản phẩm trong JSON

### 4. **Thanh toán (Customer)**
- ✅ **CheckoutController**: Hiển thị ảnh trong quá trình thanh toán
- ✅ **Session cart**: Cập nhật ảnh cho checkout process

## Chi tiết các thay đổi

### 1. **API BanHangTaiQuayController.cs**

**Thêm trường mainImage**:
```csharp
// Thêm ảnh chính riêng biệt
mainImage = x.AnhSanPhams
    .Where(a => a.TrangThai && a.LaAnhChinh)
    .Select(a => a.UrlAnh)
    .FirstOrDefault() ?? "/img/default-product.jpg",
```

**Kết quả**: API trả về cả `img` (ảnh đầu tiên) và `mainImage` (ảnh chính)

### 2. **GioHangRepository.cs**

**Cập nhật GetByUserId**:
```csharp
public GioHang GetByUserId(Guid userId)
{
    return _db.GioHangs
        .Include(g => g.ChiTietGioHangs)
            .ThenInclude(ct => ct.SanPhamChiTiet)
                .ThenInclude(sp => sp.AnhSanPhams.Where(a => a.TrangThai))
        .Include(g => g.ChiTietGioHangs)
            .ThenInclude(ct => ct.SanPhamChiTiet)
                .ThenInclude(sp => sp.SanPham)
        .FirstOrDefault(gt => gt.IDKhachHang == userId);
}
```

**Kết quả**: Giỏ hàng database hiển thị đầy đủ ảnh sản phẩm

### 3. **GioHangController.cs**

**Cập nhật session cart**:
```csharp
item.SanPhamChiTiet = new SanPhamChiTiet
{
    SanPham = new SanPham
    {
        TenSanPham = spct.TenSanPham
    },
    GiaBan = spct.price,
    AnhSanPhams = new List<AnhSanPham>
    {
        new AnhSanPham
        {
            UrlAnh = spct.AnhDaiDien ?? "/img/default-product.jpg",
            LaAnhChinh = true
        }
    }
};
```

**Kết quả**: Session cart hiển thị ảnh sản phẩm

### 4. **CheckoutController.cs**

**Cập nhật checkout process**:
```csharp
item.SanPhamChiTiet = new SanPhamChiTiet
{
    SanPham = new SanPham
    {
        TenSanPham = spct.TenSanPham
    },
    GiaBan = spct.price,
    AnhSanPhams = new List<AnhSanPham>
    {
        new AnhSanPham
        {
            UrlAnh = spct.AnhDaiDien ?? "/img/default-product.jpg",
            LaAnhChinh = true
        }
    }
};
```

**Kết quả**: Quá trình thanh toán hiển thị ảnh sản phẩm

## Cấu trúc dữ liệu ảnh

### **AnhSanPham Entity**
```csharp
public class AnhSanPham
{
    public Guid IDAnhSanPham { get; set; }
    public string MaAnh { get; set; }
    public Guid IDSanPhamChiTiet { get; set; }
    public string UrlAnh { get; set; }
    public bool LaAnhChinh { get; set; }
    public DateTime NgayTao { get; set; }
    public bool TrangThai { get; set; }
}
```

### **API Response Structure**
```json
{
    "id": "guid",
    "name": "Tên sản phẩm",
    "img": "/path/to/image.jpg",
    "mainImage": "/path/to/main-image.jpg",
    "images": [
        {
            "id": "guid",
            "url": "/path/to/image.jpg",
            "isMain": true
        }
    ]
}
```

## Fallback Images

### **Default Images**
- **Sản phẩm**: `/img/default-product.jpg`
- **Người dùng**: `/img/default-user.png`

### **Error Handling**
```csharp
// Fallback khi không có ảnh
UrlAnh = spct.AnhDaiDien ?? "/img/default-product.jpg"
```

## Performance Considerations

### 1. **Lazy Loading**
- Ảnh chỉ được load khi cần thiết
- Sử dụng `Include()` có điều kiện

### 2. **Image Optimization**
- Chỉ load ảnh có `TrangThai = true`
- Sắp xếp theo `LaAnhChinh` và `NgayTao`

### 3. **Caching**
- Session cache cho giỏ hàng tạm thời
- Database cache cho giỏ hàng đã đăng nhập

## Testing Scenarios

### ✅ **Đã test**:
1. **Admin - Quản lý sản phẩm**:
   - Thêm ảnh mới → Hiển thị trong bảng
   - Đặt ảnh chính → Cập nhật ảnh đại diện
   - Xóa ảnh → Cập nhật hiển thị

2. **Admin - Bán hàng tại quầy**:
   - Load danh sách sản phẩm → Hiển thị ảnh
   - Chi tiết sản phẩm → Hiển thị đầy đủ ảnh

3. **Customer - Giỏ hàng**:
   - Session cart → Hiển thị ảnh
   - Database cart → Hiển thị ảnh
   - API GetGioHang → Trả về ảnh

4. **Customer - Thanh toán**:
   - Checkout process → Hiển thị ảnh
   - Order confirmation → Hiển thị ảnh

## Lưu ý quan trọng

### 1. **Consistency**
- Tất cả chức năng đều sử dụng cùng cấu trúc ảnh
- Fallback images nhất quán

### 2. **Error Handling**
- Kiểm tra null/empty trước khi hiển thị
- Fallback về default image khi lỗi

### 3. **Performance**
- Optimize queries với Include có điều kiện
- Lazy loading cho ảnh lớn

### 4. **User Experience**
- Loading states cho ảnh
- Error states với fallback images
- Responsive design cho mobile

## Future Improvements

### 1. **Image Optimization**
- Implement image compression
- WebP format support
- Responsive images

### 2. **Caching Strategy**
- CDN integration
- Browser caching
- Server-side caching

### 3. **Real-time Updates**
- SignalR for live image updates
- WebSocket for real-time notifications

### 4. **Advanced Features**
- Image gallery with zoom
- Image carousel
- Image search/filter 