# Hướng dẫn sử dụng chức năng quản lý ảnh sản phẩm

## Tổng quan
Chức năng quản lý ảnh sản phẩm cho phép admin thêm, xem, xóa và đặt ảnh chính cho từng biến thể sản phẩm (sản phẩm chi tiết).

## Các tính năng chính

### 1. Xem danh sách ảnh
- **Vị trí**: Trang danh sách sản phẩm (Index)
- **Cách sử dụng**: 
  - Nhấn nút "📷 Xem ảnh" bên cạnh mỗi biến thể sản phẩm
  - Modal sẽ hiển thị tất cả ảnh của biến thể đó
  - Thông tin hiển thị: ảnh preview, mã ảnh, ngày tạo, trạng thái, URL

### 2. Thêm ảnh mới
- **Vị trí**: Trang danh sách sản phẩm (Index)
- **Cách sử dụng**:
  - Nhấn nút "➕ Thêm ảnh" bên cạnh biến thể sản phẩm
  - Nhập URL ảnh vào ô input
  - Chọn "Đặt làm ảnh chính" nếu muốn
  - Nhấn "Thêm ảnh" để lưu

### 3. Đặt ảnh chính
- **Cách sử dụng**:
  - Trong modal xem ảnh, nhấn nút "⭐ Đặt chính" bên cạnh ảnh muốn đặt làm chính
  - Xác nhận hành động
  - Ảnh sẽ được đặt làm ảnh chính và hiển thị badge "Ảnh chính"

### 4. Xóa ảnh
- **Cách sử dụng**:
  - Trong modal xem ảnh, nhấn nút "🗑️ Xóa" bên cạnh ảnh muốn xóa
  - Xác nhận hành động
  - Ảnh sẽ bị đánh dấu vô hiệu (không xóa thực sự khỏi database)

## Giao diện

### Trang danh sách sản phẩm
- Cột "Ảnh sản phẩm": Hiển thị ảnh chính của biến thể
- Cột "Thao tác ảnh": Chứa 2 nút:
  - 📷 Xem ảnh: Mở modal xem tất cả ảnh
  - ➕ Thêm ảnh: Mở modal thêm ảnh mới

### Modal xem ảnh
- Hiển thị danh sách tất cả ảnh của biến thể
- Mỗi ảnh hiển thị:
  - Preview ảnh
  - Thông tin chi tiết (mã, ngày tạo, trạng thái, URL)
  - Nút thao tác (đặt chính, xóa)

### Modal thêm ảnh
- Form nhập URL ảnh
- Preview ảnh khi nhập URL
- Checkbox "Đặt làm ảnh chính"
- Nút thêm ảnh với loading state

## Tính năng bổ sung

### Preview ảnh
- Khi nhập URL ảnh, hệ thống sẽ tự động hiển thị preview
- Nếu URL không hợp lệ, preview sẽ ẩn đi

### Thông báo
- Thông báo thành công/lỗi hiển thị ở góc trên bên phải
- Tự động ẩn sau 5 giây
- Có thể đóng thủ công

### Loading state
- Nút thêm ảnh hiển thị spinner khi đang xử lý
- Modal xem ảnh hiển thị loading khi tải dữ liệu

### Responsive design
- Giao diện tương thích với mobile
- Các nút thao tác tự động điều chỉnh kích thước

## Lưu ý kỹ thuật

### API Endpoints
- `GET /Admin/AnhSanPham/GetImages?sanPhamChiTietId={id}`: Lấy danh sách ảnh
- `POST /Admin/AnhSanPham/AddImage?sanPhamChiTietId={id}`: Thêm ảnh mới
- `PUT /Admin/AnhSanPham/SetMainImage?imageId={id}`: Đặt ảnh chính
- `DELETE /Admin/AnhSanPham/DeleteImage?imageId={id}`: Xóa ảnh

### Validation
- URL ảnh phải hợp lệ
- Chỉ cho phép thêm ảnh cho biến thể tồn tại
- Không thể xóa ảnh chính (phải đặt ảnh khác làm chính trước)

### Security
- Tất cả request đều có validation
- Xóa ảnh chỉ đánh dấu vô hiệu, không xóa thực sự
- Chỉ admin mới có quyền truy cập

## Troubleshooting

### Lỗi thường gặp
1. **Không tải được ảnh**: Kiểm tra URL ảnh có hợp lệ không
2. **Không thêm được ảnh**: Kiểm tra kết nối mạng và quyền truy cập
3. **Ảnh không hiển thị**: Kiểm tra URL có thể truy cập từ server không

### Debug
- Mở Developer Tools (F12) để xem console logs
- Kiểm tra Network tab để xem API calls
- Kiểm tra Response của API để biết lỗi chi tiết 