using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ShopRAR.Domain;

namespace ShopRAR.BLL.EF;

public class EfCategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public EfCategoryService(AppDbContext context)
    {
        _context = context;
    }

    public IEnumerable<Category> GetAllCategories()
    {
        return _context.Categories
            .OrderBy(c => c.CategoryId)
            .ToList();
    }

    public Category? GetCategoryById(int id)
    {
        return _context.Categories.FirstOrDefault(c => c.CategoryId == id);
    }

    public Category CreateCategory(Category category)
    {
        var newCategory = new Category
        {
            Name = category.Name,
            IsActive = category.IsActive
        };
        
        if (category.CategoryId == 0)
        {
            var maxId = _context.Categories.AsNoTracking().Any() 
                ? _context.Categories.AsNoTracking().Max(c => c.CategoryId) 
                : 0;
            newCategory.CategoryId = maxId + 1;
        }
        else
        {
            newCategory.CategoryId = category.CategoryId;
        }
        
        _context.Categories.Add(newCategory);
        _context.SaveChanges();
        return newCategory;
    }

    public void UpdateCategory(Category category)
    {
        var existing = _context.Categories.FirstOrDefault(c => c.CategoryId == category.CategoryId);
        if (existing == null) return;

        existing.Name = category.Name;
        existing.IsActive = category.IsActive;

        _context.SaveChanges();
    }

    public void DeleteCategory(int id)
    {
        var existing = _context.Categories.FirstOrDefault(c => c.CategoryId == id);
        if (existing == null) return;

        _context.Categories.Remove(existing);
        _context.SaveChanges();
    }

    public IEnumerable<Category> GetActiveCategories()
    {
        return _context.Categories
            .Where(c => c.IsActive == "True")
            .OrderBy(c => c.CategoryId)
            .ToList();
    }
}

