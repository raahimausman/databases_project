using System.Collections.Generic;

namespace ShopRAR.Domain;

public interface IInventoryService
{
    Inventory? GetInventoryByProductId(int productId);
    void UpdateInventory(int productId, int quantity);
    void AdjustInventory(int productId, int quantityChange);
    int GetStockQuantity(int productId);
    PagedResult<Inventory> GetLowStockProductsPaged(int threshold, int pageNumber, int pageSize);
}

