using System.Collections.Generic;

namespace ShopRAR.Domain;

public interface IProductService
{
    IEnumerable<Product> GetAllProducts();
    PagedResult<Product> GetAllProductsPaged(int pageNumber, int pageSize);
    Product? GetProductById(int id);
    Product CreateProduct(Product product);
    void UpdateProduct(Product product);
    void DeleteProduct(int id);
    PagedResult<Product> GetActiveProductsPaged(int pageNumber, int pageSize);
    PagedResult<Product> GetProductsByCategoryPaged(int categoryId, int pageNumber, int pageSize);
    PagedResult<Product> SearchProductsPaged(string searchTerm, int pageNumber, int pageSize);
}

