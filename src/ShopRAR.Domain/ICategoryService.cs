using System.Collections.Generic;

namespace ShopRAR.Domain;

public interface ICategoryService
{
    IEnumerable<Category> GetAllCategories();
    Category? GetCategoryById(int id);
    Category CreateCategory(Category category);
    void UpdateCategory(Category category);
    void DeleteCategory(int id);
    IEnumerable<Category> GetActiveCategories();
}

