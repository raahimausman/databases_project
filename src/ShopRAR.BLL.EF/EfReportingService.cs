using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ShopRAR.Domain;

namespace ShopRAR.BLL.EF;

public class EfReportingService : IReportingService
{
    private readonly AppDbContext _context;

    public EfReportingService(AppDbContext context)
    {
        _context = context;
    }

    public IEnumerable<TopSellingProduct> GetTopSellingProducts(int year, int month, int topN = 10)
    {
        var monthOrders = _context.Orders
            .Where(o => o.OrderDate.HasValue &&
                       o.OrderDate.Value.Year == year &&
                       o.OrderDate.Value.Month == month &&
                       o.Status != "Cancelled")
            .Select(o => o.OrderId)
            .ToList();

        var productSales = _context.OrderItems
            .Where(oi => monthOrders.Contains(oi.OrderId))
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalQty = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.LineTotalAmount)
            })
            .OrderByDescending(ps => ps.TotalQty)
            .Take(topN)
            .ToList();

        var productIds = productSales.Select(ps => ps.ProductId).ToList();
        var products = _context.Products
            .Where(p => productIds.Contains(p.ProductId))
            .ToDictionary(p => p.ProductId);

        return productSales.Select(ps => new TopSellingProduct
        {
            ProductId = ps.ProductId,
            ProductName = products.ContainsKey(ps.ProductId) ? products[ps.ProductId].Name : "Unknown",
            TotalQty = ps.TotalQty,
            TotalRevenue = ps.TotalRevenue
        }).ToList();
    }

    public PagedResult<CustomerLifetimeSpend> GetCustomerLifetimeSpendPaged(int pageNumber, int pageSize)
    {
        var result = new PagedResult<CustomerLifetimeSpend>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = new List<CustomerLifetimeSpend>()
        };

        var totalCount = _context.Orders
            .Where(o => o.Status != "Cancelled")
            .Select(o => o.CustomerId)
            .Distinct()
            .Count();

        result.TotalCount = totalCount;

        var customerSpend = _context.Orders
            .Where(o => o.Status != "Cancelled")
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                LifetimeSpend = g.Sum(o => o.TotalAmount ?? 0),
                OrderCount = g.Count(),
                LastOrderDate = g.Max(o => o.OrderDate)
            })
            .OrderByDescending(cs => cs.LifetimeSpend)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var customerIds = customerSpend.Select(cs => cs.CustomerId).Where(id => id.HasValue).Cast<int>().ToList();
        var customers = _context.Customers
            .Where(c => customerIds.Contains(c.CustomerId))
            .ToDictionary(c => c.CustomerId);

        result.Items = customerSpend
            .Where(cs => cs.CustomerId.HasValue && customers.ContainsKey(cs.CustomerId.Value))
            .Select(cs => new CustomerLifetimeSpend
            {
                CustomerId = cs.CustomerId!.Value,
                FullName = customers[cs.CustomerId.Value].FullName,
                LifetimeSpend = cs.LifetimeSpend,
                OrderCount = cs.OrderCount,
                LastOrderDate = cs.LastOrderDate
            })
            .ToList();

        return result;
    }
}

