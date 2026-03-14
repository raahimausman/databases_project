using System.Collections.Generic;
using System.Linq;
using ShopRAR.Domain;

namespace ShopRAR.BLL.EF;

public class EfProductService : IProductService
{
    private readonly AppDbContext _context;

    public EfProductService(AppDbContext context)
    {
        _context = context;
    }

    public IEnumerable<Product> GetAllProducts()
    {
        return _context.Products
            .OrderBy(p => p.ProductId)
            .ToList();
    }

    public PagedResult<Product> GetAllProductsPaged(int pageNumber, int pageSize)
    {
        var totalCount = _context.Products.Count();
        
        var items = _context.Products
            .OrderBy(p => p.ProductId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public Product? GetProductById(int id)
    {
        return _context.Products.FirstOrDefault(p => p.ProductId == id);
    }

    public Product CreateProduct(Product product)
    {
        if (product.ProductId == 0)
        {
            var maxId = _context.Products.Any() 
                ? _context.Products.Max(p => p.ProductId) 
                : 0;
            product.ProductId = maxId + 1;
        }
        
        if (string.IsNullOrWhiteSpace(product.SKU))
        {
            product.SKU = GenerateUniqueSKU();
        }
        
        _context.Products.Add(product);
        _context.SaveChanges();
        return product;
    }

    private string GenerateUniqueSKU()
    {
        int maxSkuNumber = 19999; 
        
        var existingSkus = _context.Products
            .Where(p => p.SKU != null && p.SKU.StartsWith("SKU-"))
            .Select(p => p.SKU)
            .ToList();
        
        foreach (var sku in existingSkus)
        {
            if (sku != null && sku.Length >= 10 && sku.StartsWith("SKU-"))
            {
                var numberPart = sku.Substring(4); 
                if (int.TryParse(numberPart, out var skuNumber) && skuNumber > maxSkuNumber)
                {
                    maxSkuNumber = skuNumber;
                }
            }
        }
        
        string newSku;
        int attempts = 0;
        do
        {
            var nextSkuNumber = maxSkuNumber + 1 + attempts;
            newSku = $"SKU-{nextSkuNumber:D6}";
            attempts++;

            if (attempts > 1000)
            {
                throw new InvalidOperationException("Unable to generate unique SKU after 1000 attempts.");
            }
        } while (_context.Products.Any(p => p.SKU == newSku));
        
        return newSku;
    }

    public void UpdateProduct(Product product)
    {
        var existing = _context.Products.FirstOrDefault(p => p.ProductId == product.ProductId);
        if (existing == null) return;

        existing.Name = product.Name;
        
        existing.Price = product.Price;
        existing.Description = product.Description;
        existing.IsActive = product.IsActive;

        _context.SaveChanges();
    }

    public void DeleteProduct(int id)
    {
        var existing = _context.Products.FirstOrDefault(p => p.ProductId == id);
        if (existing == null) return;

        _context.Products.Remove(existing);
        _context.SaveChanges();
    }

    public PagedResult<Product> GetActiveProductsPaged(int pageNumber, int pageSize)
    {
        var totalCount = _context.Products.Count(p => p.IsActive == "True");
        
        var items = _context.Products
            .Where(p => p.IsActive == "True")
            .OrderBy(p => p.ProductId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public PagedResult<Product> GetProductsByCategoryPaged(int categoryId, int pageNumber, int pageSize)
    {
        var totalCount = _context.Products
            .Count(p => _context.ProductCategories
                .Any(pc => pc.ProductId == p.ProductId && pc.CategoryId == categoryId));
        
        var items = _context.Products
            .Where(p => _context.ProductCategories
                .Any(pc => pc.ProductId == p.ProductId && pc.CategoryId == categoryId))
            .OrderBy(p => p.ProductId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public PagedResult<Product> SearchProductsPaged(string searchTerm, int pageNumber, int pageSize)
    {
        var term = searchTerm.ToLower();
        var query = _context.Products
            .Where(p => p.Name != null && p.Name.ToLower().Contains(term));

        var totalCount = query.Count();
        
        var items = query
            .OrderBy(p => p.ProductId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}

