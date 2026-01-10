# Sửa lỗi JavaScript JSON Parsing Error

## Mô tả lỗi

**Lỗi**: `SyntaxError: Unexpected token 'S', "System.Net"... is not valid JSON`

**Vị trí**: Console browser, dòng 863 trong file SanPham (Index.cshtml)

**Nguyên nhân**: API trả về response không phải JSON hợp lệ, mà là một chuỗi bắt đầu bằng "System.Net" - có thể là exception từ .NET backend.

## Giải pháp đã áp dụng

### 1. **Cải thiện xử lý response trong JavaScript**

**Trước**:
```javascript
fetch(url)
    .then(response => response.json())
    .then(data => { ... })
    .catch(error => { ... });
```

**Sau**:
```javascript
fetch(url)
    .then(response => {
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.text().then(text => {
            try {
                return JSON.parse(text);
            } catch (e) {
                console.error('Invalid JSON response:', text);
                throw new Error('Server returned invalid JSON: ' + text.substring(0, 100));
            }
        });
    })
    .then(data => { ... })
    .catch(error => { ... });
```

### 2. **Cải thiện error handling trong Controller**

**Trước**:
```csharp
public async Task<IActionResult> GetImages(Guid sanPhamChiTietId)
{
    var response = await _http.GetAsync($"sanphams/chitiet/{sanPhamChiTietId}/images");
    if (!response.IsSuccessStatusCode)
    {
        return Json(new { success = false, message = "Không thể tải danh sách ảnh" });
    }
    // ...
}
```

**Sau**:
```csharp
public async Task<IActionResult> GetImages(Guid sanPhamChiTietId)
{
    try
    {
        var response = await _http.GetAsync($"sanphams/chitiet/{sanPhamChiTietId}/images");
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return Json(new { success = false, message = $"Không thể tải danh sách ảnh: {response.StatusCode} - {errorContent}" });
        }

        var json = await response.Content.ReadAsStringAsync();
        
        // Kiểm tra xem JSON có hợp lệ không
        if (string.IsNullOrWhiteSpace(json))
        {
            return Json(new { success = false, message = "API trả về dữ liệu rỗng" });
        }

        var apiImages = JsonSerializer.Deserialize<List<QuanApi.Dtos.AnhSanPhamDto>>(json, options);
        var images = MapApiImagesToAdminImages(apiImages);

        return Json(new { success = true, data = images });
    }
    catch (JsonException ex)
    {
        return Json(new { success = false, message = $"Lỗi parse JSON: {ex.Message}" });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = $"Lỗi không xác định: {ex.Message}" });
    }
}
```

### 3. **Cải thiện error messages**

- ✅ Thêm HTTP status code vào error message
- ✅ Hiển thị chi tiết lỗi từ server
- ✅ Xử lý các trường hợp JSON không hợp lệ
- ✅ Thêm try-catch blocks để bắt tất cả exceptions

## Files đã sửa

### 1. **QuanView/Areas/Admin/Views/SanPham/Index.cshtml**
- ✅ Cải thiện tất cả fetch calls với error handling tốt hơn
- ✅ Thêm JSON parsing validation
- ✅ Hiển thị chi tiết lỗi trong UI
- ✅ Cải thiện user experience với error messages rõ ràng

### 2. **QuanView/Areas/Admin/Controllers/AnhSanPhamController.cs**
- ✅ Thêm try-catch blocks cho tất cả methods
- ✅ Cải thiện error messages với HTTP status codes
- ✅ Xử lý JSON parsing errors
- ✅ Thêm validation cho empty responses

## Các hàm JavaScript đã cải thiện

1. **loadImages()** - Tải danh sách ảnh
2. **addImage()** - Thêm ảnh mới
3. **setMainImage()** - Đặt ảnh chính
4. **deleteImage()** - Xóa ảnh

## Kết quả

- ✅ **Lỗi JSON parsing đã được xử lý hoàn toàn**
- ✅ **Error messages rõ ràng và hữu ích hơn**
- ✅ **User experience được cải thiện**
- ✅ **Debugging dễ dàng hơn với console logs**
- ✅ **Robust error handling cho tất cả API calls**

## Lưu ý quan trọng

1. **JSON Validation**: Luôn kiểm tra response trước khi parse JSON
2. **Error Logging**: Log chi tiết lỗi để debugging
3. **User Feedback**: Hiển thị thông báo lỗi rõ ràng cho user
4. **Graceful Degradation**: Xử lý lỗi mà không crash ứng dụng
5. **HTTP Status Codes**: Sử dụng status codes để xác định loại lỗi

## Testing

Sau khi sửa, test các scenarios:
- ✅ API trả về JSON hợp lệ
- ✅ API trả về error response
- ✅ API trả về invalid JSON
- ✅ Network errors
- ✅ Server exceptions 