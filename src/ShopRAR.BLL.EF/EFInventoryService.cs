using System.Collections.Generic;
using System.Linq;
using ShopRAR.Domain;

namespace ShopRAR.BLL.EF;

public class EfInventoryService : IInventoryService
{
    private readonly AppDbContext _context;

    public EfInventoryService(AppDbContext context)
    {
        _context = context;
    }

    public Inventory? GetInventoryByProductId(int productId)
    {
        return _context.Inventories.FirstOrDefault(i => i.ProductId == productId);
    }

    public void UpdateInventory(int productId, int quantity)
    {
        var existing = _context.Inventories.FirstOrDefault(i => i.ProductId == productId);
        if (existing != null)
        {
            existing.QuantityOnHand = quantity;
        }
        else
        {
            var inventory = new Inventory
            {
                ProductId = productId,
                QuantityOnHand = quantity
            };
            _context.Inventories.Add(inventory);
        }

        _context.SaveChanges();
    }

    public void AdjustInventory(int productId, int quantityChange)
    {
        var existing = _context.Inventories.FirstOrDefault(i => i.ProductId == productId);
        if (existing == null) return;

        existing.QuantityOnHand += quantityChange;
        _context.SaveChanges();
    }

    public int GetStockQuantity(int productId)
    {
        var inventory = _context.Inventories.FirstOrDefault(i => i.ProductId == productId);
        return inventory?.QuantityOnHand ?? 0;
    }

    public PagedResult<Inventory> GetLowStockProductsPaged(int threshold, int pageNumber, int pageSize)
    {
        var totalCount = _context.Inventories
            .Count(i => i.QuantityOnHand < threshold);
        
        var items = _context.Inventories
            .Where(i => i.QuantityOnHand < threshold)
            .OrderBy(i => i.QuantityOnHand)
            .ThenBy(i => i.ProductId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Inventory>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}

