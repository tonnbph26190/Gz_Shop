using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Dtos;
using QuanApi.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DotGiamGiasController : ControllerBase
    {
        private readonly DotGiamGiaIRepository _repository;

        public DotGiamGiasController(DotGiamGiaIRepository repository)
        {
            _repository = repository;
        }

        // GET: api/DotGiamGias
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DotGiamGiaFilterDto filter)
        {
            var result = await _repository.GetDotGiamGia(
                filter.MaDot,
                filter.TenDot,
                filter.PhanTramGiam,
                filter.TuNgay,
                filter.DenNgay,
                filter.TrangThai,
                filter.Page,
                filter.PageSize
            );

            var now = DateTime.Now;

            foreach (var item in result.Data)
            {
                bool trangThaiMoi = item.NgayBatDau <= now && item.NgayKetThuc >= now;

                if (item.TrangThai != trangThaiMoi)
                {
                    item.TrangThai = trangThaiMoi;
                    await _repository.UpdateTrangThaiAsync(item.IDDotGiamGia, trangThaiMoi);
                }
            }


            return Ok(result);
        }


        // GET: api/DotGiamGias/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var dot = await _repository.GetByIdAsync(id);
            if (dot == null) return NotFound();
            return Ok(dot);
        }

        // POST: api/DotGiamGias
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DotGiamGiaCreateDto dto)
        {
            if (dto == null || dto.Dot == null)
                return BadRequest("Dữ liệu không hợp lệ");

            var success = await _repository.CreateAsync(dto.Dot, dto.ChiTietIds ?? new List<Guid>());
            if (success)
            {
                return Ok(new { 
                    Success = true, 
                    Message = "Đợt giảm giá đã được xử lý thành công. Nếu sản phẩm đã có đợt giảm giá đang hoạt động, đã được cập nhật thay vì tạo mới." 
                });
            }
            return BadRequest("Tạo thất bại (có thể do ngày không hợp lệ)");
        }

        // PUT: api/DotGiamGias/{id}
        [HttpPut("{id}")]

        public async Task<IActionResult> Update(Guid id, [FromBody] DotGiamGiaUpdateDto dto)
        {
            if (id != dto.IDDotGiamGia)
                return BadRequest("ID không khớp");

            var dot = new DotGiamGia
            {
                IDDotGiamGia = dto.IDDotGiamGia,
                MaDot = dto.MaDot,
                TenDot = dto.TenDot,
                PhanTramGiam = dto.PhanTramGiam,
                NgayBatDau = dto.NgayBatDau,
                NgayKetThuc = dto.NgayKetThuc
            };

            var success = await _repository.UpdateAsync(dot, dto.SanPhamChiTietIds);
            return success ? Ok() : BadRequest("Cập nhật thất bại");
        }



        [HttpGet("{id}/SanPhams")]
        public async Task<IActionResult> GetSanPhamsCuaDot(Guid id)
        {
            var sanPhams = await _repository.GetAllSanPhamChiTietWithSelected(id);
            return Ok(sanPhams);
        }

        // GET: api/DotGiamGias/CheckActiveDiscounts
        [HttpGet("CheckActiveDiscounts")]
        public async Task<IActionResult> CheckActiveDiscounts([FromQuery] List<Guid> productIds)
        {
            if (productIds == null || !productIds.Any())
                return BadRequest("Danh sách sản phẩm không hợp lệ");

            var productsWithActiveDiscounts = await _repository.GetProductsWithActiveDiscounts(productIds);
            return Ok(new { 
                HasActiveDiscounts = productsWithActiveDiscounts.Any(),
                ProductIdsWithActiveDiscounts = productsWithActiveDiscounts,
                Message = productsWithActiveDiscounts.Any() 
                    ? $"Có {productsWithActiveDiscounts.Count} sản phẩm đã có đợt giảm giá đang hoạt động. Đợt giảm giá hiện tại sẽ được cập nhật thay vì tạo mới."
                    : "Không có sản phẩm nào có đợt giảm giá đang hoạt động."
            });
        }


        // DELETE: api/DotGiamGias/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _repository.DeleteAsync(id);
            return success ? Ok(new { Success = true }) : NotFound();
        }

        // PUT: api/DotGiamGias/UpdateTrangThai?id=GUID&trangThai=true
        [HttpPut("UpdateTrangThai")]
        public async Task<IActionResult> UpdateTrangThai([FromQuery] Guid id, [FromQuery] bool trangThai)
        {
            var success = await _repository.UpdateTrangThaiAsync(id, trangThai);
            return success ? Ok(new { Success = true }) : NotFound();
        }

    }
}
