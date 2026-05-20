using System;
using System.Threading.Tasks;
using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Services;
using Xunit;

namespace QuanApi.Tests.Controllers
{
    public class InventoryReservationServiceTests
    {
        private static DbContextOptions<BanQuanAu1DbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<BanQuanAu1DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task ReserveAndCommitReserved_ShouldDecreaseOnHandAndClearReserved()
        {
            var options = CreateOptions();
            var productId = Guid.NewGuid();

            await using (var seed = new BanQuanAu1DbContext(options))
            {
                seed.SanPhamChiTiets.Add(new SanPhamChiTiet
                {
                    IDSanPhamChiTiet = productId,
                    MaSPChiTiet = "SPCT-TEST-01",
                    SoLuong = 10,
                    SoLuongDatCho = 0,
                    GiaBan = 100000,
                    IDSanPham = Guid.NewGuid(),
                    IDKichCo = Guid.NewGuid(),
                    IDMauSac = Guid.NewGuid()
                });
                await seed.SaveChangesAsync();
            }

            await using (var context = new BanQuanAu1DbContext(options))
            {
                var service = new InventoryReservationService(context);
                var reserveResult = await service.ReserveAsync(new[] { new InventoryLine(productId, 4) }, "test");
                Assert.True(reserveResult.Success);

                var commitResult = await service.CommitReservedAsync(new[] { new InventoryLine(productId, 4) }, "test");
                Assert.True(commitResult.Success);
            }

            await using (var assertContext = new BanQuanAu1DbContext(options))
            {
                var product = await assertContext.SanPhamChiTiets.FindAsync(productId);
                Assert.NotNull(product);
                Assert.Equal(6, product!.SoLuong);
                Assert.Equal(0, product.SoLuongDatCho);
            }
        }

        [Fact]
        public async Task Reserve_ShouldRejectWhenAvailableQuantityIsExceeded()
        {
            var options = CreateOptions();
            var productId = Guid.NewGuid();

            await using (var seed = new BanQuanAu1DbContext(options))
            {
                seed.SanPhamChiTiets.Add(new SanPhamChiTiet
                {
                    IDSanPhamChiTiet = productId,
                    MaSPChiTiet = "SPCT-TEST-02",
                    SoLuong = 5,
                    SoLuongDatCho = 0,
                    GiaBan = 100000,
                    IDSanPham = Guid.NewGuid(),
                    IDKichCo = Guid.NewGuid(),
                    IDMauSac = Guid.NewGuid()
                });
                await seed.SaveChangesAsync();
            }

            await using (var context = new BanQuanAu1DbContext(options))
            {
                var service = new InventoryReservationService(context);
                var firstReserve = await service.ReserveAsync(new[] { new InventoryLine(productId, 4) }, "test");
                var secondReserve = await service.ReserveAsync(new[] { new InventoryLine(productId, 2) }, "test");

                Assert.True(firstReserve.Success);
                Assert.False(secondReserve.Success);
            }
        }
    }
}
