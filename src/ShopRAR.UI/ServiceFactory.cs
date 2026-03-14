using System;
using ShopRAR.BLL.EF;
using ShopRAR.BLL.SP;
using ShopRAR.Domain;

namespace ShopRAR.UI;

public static class ServiceFactory
{
    private static AppDbContext? _efContext;
    private static string? _connectionString;
    private static string? _mode;

    public static void Initialize(string type, string connectionString)
    {
        _mode = type.ToLower();
        _connectionString = connectionString;
        
        if (_mode == "linq")
        {
            _efContext = new AppDbContext(connectionString);
        }
    }

    public static ICustomerService CreateCustomerService()
    {
        return _mode switch
        {
            "linq"  => new EfCustomerService(_efContext!),
            "sproc" => new SpCustomerService(_connectionString!),
            _       => throw new ArgumentException($"Unknown data access type: {_mode} (use 'linq' or 'sproc')")
        };
    }

    public static IProductService CreateProductService()
    {
        return _mode switch
        {
            "linq"  => new EfProductService(_efContext!),
            "sproc" => new SpProductService(_connectionString!),
            _       => throw new ArgumentException($"Unknown data access type: {_mode} (use 'linq' or 'sproc')")
        };
    }

    public static ICategoryService CreateCategoryService()
    {
        return _mode switch
        {
            "linq"  => new EfCategoryService(_efContext!),
            "sproc" => new SpCategoryService(_connectionString!),
            _       => throw new ArgumentException($"Unknown data access type: {_mode} (use 'linq' or 'sproc')")
        };
    }

    public static IOrderService CreateOrderService()
    {
        return _mode switch
        {
            "linq"  => new EfOrderService(_efContext!),
            "sproc" => new SpOrderService(_connectionString!),
            _       => throw new ArgumentException($"Unknown data access type: {_mode} (use 'linq' or 'sproc')")
        };
    }

    public static IReviewService CreateReviewService()
    {
        return _mode switch
        {
            "linq"  => new EfReviewService(_efContext!),
            "sproc" => new SpReviewService(_connectionString!),
            _       => throw new ArgumentException($"Unknown data access type: {_mode} (use 'linq' or 'sproc')")
        };
    }

    public static IAdminUserService CreateAdminUserService()
    {
        return _mode switch
        {
            "linq"  => new EfAdminUserService(_efContext!),
            "sproc" => new SpAdminUserService(_connectionString!),
            _       => throw new ArgumentException($"Unknown data access type: {_mode} (use 'linq' or 'sproc')")
        };
    }

    public static IInventoryService CreateInventoryService()
    {
        return _mode switch
        {
            "linq"  => new EfInventoryService(_efContext!),
            "sproc" => new SpInventoryService(_connectionString!),
            _       => throw new ArgumentException($"Unknown data access type: {_mode} (use 'linq' or 'sproc')")
        };
    }

    public static IReportingService CreateReportingService()
    {
        return _mode switch
        {
            "linq"  => new EfReportingService(_efContext!),
            "sproc" => new SpReportingService(_connectionString!),
            _       => throw new ArgumentException($"Unknown data access type: {_mode} (use 'linq' or 'sproc')")
        };
    }
}
