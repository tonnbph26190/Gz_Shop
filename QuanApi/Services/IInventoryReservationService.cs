namespace QuanApi.Services
{
    public record InventoryLine(Guid ProductDetailId, int Quantity);

    public record InventoryActionResult(bool Success, string? ErrorMessage = null);

    public interface IInventoryReservationService
    {
        Task<InventoryActionResult> ReserveAsync(IEnumerable<InventoryLine> lines, string updatedBy);
        Task<InventoryActionResult> ReleaseAsync(IEnumerable<InventoryLine> lines, string updatedBy);
        Task<InventoryActionResult> CommitReservedAsync(IEnumerable<InventoryLine> lines, string updatedBy);
        Task<InventoryActionResult> CommitDirectAsync(IEnumerable<InventoryLine> lines, string updatedBy);
        Task<InventoryActionResult> RestockAsync(IEnumerable<InventoryLine> lines, string updatedBy);
    }
}
