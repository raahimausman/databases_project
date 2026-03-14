using System;
using System.Collections.Generic;
using System.Linq;
using ShopRAR.Domain;

namespace ShopRAR.BLL.EF;

public class EfOrderService : IOrderService
{
    private readonly AppDbContext _context;

    public EfOrderService(AppDbContext context)
    {
        _context = context;
    }

    public Order? GetOrderById(int id)
    {
        var order = _context.Orders.FirstOrDefault(o => o.OrderId == id);
        if (order != null)
        {
            _context.Entry(order).Reload();
        }
        return order;
    }

    public Order CreateOrder(int customerId, int adminUserId, string? status = null)
    {
        var maxId = _context.Orders.Any() 
            ? _context.Orders.Max(o => o.OrderId) 
            : 0;

        var order = new Order
        {
            OrderId = maxId + 1,
            OrderDate = DateTime.Now,
            Status = status ?? "Pending",
            TotalAmount = 0,
            CustomerId = customerId,
            AdminUserId = adminUserId
        };

        _context.Orders.Add(order);
        _context.SaveChanges();
        return order;
    }

    public void UpdateOrderStatus(int orderId, string status)
    {
        var existing = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
        if (existing == null) return;

        existing.Status = status;
        _context.SaveChanges();
    }

    public void CancelOrder(int orderId)
    {
        UpdateOrderStatus(orderId, "Cancelled");
    }

    public IEnumerable<Order> GetOrdersByCustomer(int customerId)
    {
        return _context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToList();
    }

    public void AddOrderItem(int orderId, int productId, int quantity, decimal unitPrice)
    {
        var maxItemId = _context.OrderItems.Any() 
            ? _context.OrderItems.Max(oi => oi.OrderItemId) 
            : 0;

        var lineTotal = quantity * unitPrice;

        var orderItem = new OrderItem
        {
            OrderItemId = maxItemId + 1,
            OrderId = orderId,
            ProductId = productId,
            UnitPriceAtOrder = unitPrice,
            Quantity = quantity,
            LineTotalAmount = lineTotal
        };

        _context.OrderItems.Add(orderItem);

        var inventory = _context.Inventories.FirstOrDefault(i => i.ProductId == productId);
        if (inventory != null)
        {
            inventory.QuantityOnHand -= quantity;
        }

        _context.SaveChanges();

        var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
        if (order != null)
        {
            var total = _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .Sum(oi => (decimal?)oi.LineTotalAmount) ?? 0;
            
            order.TotalAmount = total;
            _context.SaveChanges();
            
            _context.Entry(order).Reload();
        }
    }

    public IEnumerable<OrderItem> GetOrderItems(int orderId)
    {
        return _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .ToList();
    }

    public OrderSummary? GetOrderSummaryById(int orderId)
    {
        var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
        
        if (order == null)
            return null;

        var customer = order.CustomerId.HasValue 
            ? _context.Customers.FirstOrDefault(c => c.CustomerId == order.CustomerId.Value) 
            : null;
        
        var admin = order.AdminUserId.HasValue 
            ? _context.AdminUsers.FirstOrDefault(a => a.AdminUserId == order.AdminUserId.Value) 
            : null;

        var calculatedTotal = _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .Sum(oi => (decimal?)oi.LineTotalAmount) ?? 0;

        return new OrderSummary
        {
            OrderId = order.OrderId,
            OrderDate = order.OrderDate,
            Status = order.Status,
            StoredTotalAmount = order.TotalAmount,
            CalculatedTotalAmount = calculatedTotal,
            CustomerId = order.CustomerId ?? 0,
            CustomerName = customer?.FullName ?? "Unknown",
            CustomerEmail = customer?.Email,
            AdminUserId = order.AdminUserId,
            AdminEmail = admin?.Email
        };
    }

    public PagedResult<OrderSummary> GetAllOrderSummariesPaged(int pageNumber, int pageSize)
    {
        var result = new PagedResult<OrderSummary>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = new List<OrderSummary>()
        };

        var totalCount = _context.Orders.Count();
        result.TotalCount = totalCount;

        var orders = _context.Orders
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.OrderId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = new List<OrderSummary>();

        foreach (var order in orders)
        {
            var customer = order.CustomerId.HasValue 
                ? _context.Customers.FirstOrDefault(c => c.CustomerId == order.CustomerId.Value) 
                : null;
            
            var admin = order.AdminUserId.HasValue 
                ? _context.AdminUsers.FirstOrDefault(a => a.AdminUserId == order.AdminUserId.Value) 
                : null;

            var calculatedTotal = _context.OrderItems
                .Where(oi => oi.OrderId == order.OrderId)
                .Sum(oi => (decimal?)oi.LineTotalAmount) ?? 0;

            items.Add(new OrderSummary
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                StoredTotalAmount = order.TotalAmount,
                CalculatedTotalAmount = calculatedTotal,
                CustomerId = order.CustomerId ?? 0,
                CustomerName = customer?.FullName ?? "Unknown",
                CustomerEmail = customer?.Email,
                AdminUserId = order.AdminUserId,
                AdminEmail = admin?.Email
            });
        }

        result.Items = items;
        return result;
    }
}

