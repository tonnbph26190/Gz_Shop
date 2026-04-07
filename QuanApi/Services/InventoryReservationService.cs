using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace QuanApi.Services
{
    public class InventoryReservationService : IInventoryReservationService
    {
        private readonly BanQuanAu1DbContext _context;

        public InventoryReservationService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<InventoryActionResult> ReserveAsync(IEnumerable<InventoryLine> lines, string updatedBy)
        {
            var normalized = Normalize(lines);
            if (normalized.Count == 0)
            {
                return new InventoryActionResult(true);
            }

            return await ExecuteInTransactionAsync(async () =>
            {
                foreach (var line in normalized)
                {
                    var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                        $@"UPDATE ""SanPhamChiTiets""
                           SET ""SoLuongDatCho"" = ""SoLuongDatCho"" + {line.Quantity},
                               ""LanCapNhatCuoi"" = {DateTime.UtcNow},
                               ""NguoiCapNhat"" = {updatedBy}
                           WHERE ""IDSanPhamChiTiet"" = {line.ProductDetailId}
                             AND (""SoLuong"" - ""SoLuongDatCho"") >= {line.Quantity};");

                    if (affected == 0)
                    {
                        return new InventoryActionResult(false, "Không đủ tồn khả dụng để giữ chỗ.");
                    }
                }

                return new InventoryActionResult(true);
            });
        }

        public async Task<InventoryActionResult> ReleaseAsync(IEnumerable<InventoryLine> lines, string updatedBy)
        {
            var normalized = Normalize(lines);
            if (normalized.Count == 0)
            {
                return new InventoryActionResult(true);
            }

            return await ExecuteInTransactionAsync(async () =>
            {
                foreach (var line in normalized)
                {
                    var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                        $@"UPDATE ""SanPhamChiTiets""
                           SET ""SoLuongDatCho"" = ""SoLuongDatCho"" - {line.Quantity},
                               ""LanCapNhatCuoi"" = {DateTime.UtcNow},
                               ""NguoiCapNhat"" = {updatedBy}
                           WHERE ""IDSanPhamChiTiet"" = {line.ProductDetailId}
                             AND ""SoLuongDatCho"" >= {line.Quantity};");

                    if (affected == 0)
                    {
                        return new InventoryActionResult(false, "Không thể nhả giữ chỗ vì dữ liệu tồn không hợp lệ.");
                    }
                }

                return new InventoryActionResult(true);
            });
        }

        public async Task<InventoryActionResult> CommitReservedAsync(IEnumerable<InventoryLine> lines, string updatedBy)
        {
            var normalized = Normalize(lines);
            if (normalized.Count == 0)
            {
                return new InventoryActionResult(true);
            }

            return await ExecuteInTransactionAsync(async () =>
            {
                foreach (var line in normalized)
                {
                    var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                        $@"UPDATE ""SanPhamChiTiets""
                           SET ""SoLuongDatCho"" = ""SoLuongDatCho"" - {line.Quantity},
                               ""SoLuong"" = ""SoLuong"" - {line.Quantity},
                               ""LanCapNhatCuoi"" = {DateTime.UtcNow},
                               ""NguoiCapNhat"" = {updatedBy}
                           WHERE ""IDSanPhamChiTiet"" = {line.ProductDetailId}
                             AND ""SoLuongDatCho"" >= {line.Quantity}
                             AND ""SoLuong"" >= {line.Quantity};");

                    if (affected == 0)
                    {
                        return new InventoryActionResult(false, "Không đủ tồn để hoàn tất thanh toán từ phần đã giữ chỗ.");
                    }
                }

                return new InventoryActionResult(true);
            });
        }

        public async Task<InventoryActionResult> CommitDirectAsync(IEnumerable<InventoryLine> lines, string updatedBy)
        {
            var normalized = Normalize(lines);
            if (normalized.Count == 0)
            {
                return new InventoryActionResult(true);
            }

            return await ExecuteInTransactionAsync(async () =>
            {
                foreach (var line in normalized)
                {
                    var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                        $@"UPDATE ""SanPhamChiTiets""
                           SET ""SoLuong"" = ""SoLuong"" - {line.Quantity},
                               ""LanCapNhatCuoi"" = {DateTime.UtcNow},
                               ""NguoiCapNhat"" = {updatedBy}
                           WHERE ""IDSanPhamChiTiet"" = {line.ProductDetailId}
                             AND (""SoLuong"" - ""SoLuongDatCho"") >= {line.Quantity};");

                    if (affected == 0)
                    {
                        return new InventoryActionResult(false, "Không đủ tồn khả dụng để thanh toán.");
                    }
                }

                return new InventoryActionResult(true);
            });
        }

        public async Task<InventoryActionResult> RestockAsync(IEnumerable<InventoryLine> lines, string updatedBy)
        {
            var normalized = Normalize(lines);
            if (normalized.Count == 0)
            {
                return new InventoryActionResult(true);
            }

            return await ExecuteInTransactionAsync(async () =>
            {
                foreach (var line in normalized)
                {
                    var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                        $@"UPDATE ""SanPhamChiTiets""
                           SET ""SoLuong"" = ""SoLuong"" + {line.Quantity},
                               ""LanCapNhatCuoi"" = {DateTime.UtcNow},
                               ""NguoiCapNhat"" = {updatedBy}
                           WHERE ""IDSanPhamChiTiet"" = {line.ProductDetailId};");

                    if (affected == 0)
                    {
                        return new InventoryActionResult(false, "Không thể hoàn kho vì sản phẩm không tồn tại.");
                    }
                }

                return new InventoryActionResult(true);
            });
        }

        private static List<InventoryLine> Normalize(IEnumerable<InventoryLine> lines)
        {
            return lines
                .Where(x => x.Quantity > 0)
                .GroupBy(x => x.ProductDetailId)
                .Select(g => new InventoryLine(g.Key, g.Sum(x => x.Quantity)))
                .ToList();
        }

        private async Task<InventoryActionResult> ExecuteInTransactionAsync(Func<Task<InventoryActionResult>> action)
        {
            if (_context.Database.CurrentTransaction != null)
            {
                return await action();
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            var result = await action();
            if (!result.Success)
            {
                await tx.RollbackAsync();
                return result;
            }

            await tx.CommitAsync();
            return result;
        }
    }
}
