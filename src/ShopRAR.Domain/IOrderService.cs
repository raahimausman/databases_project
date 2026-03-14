using System.Collections.Generic;

namespace ShopRAR.Domain;

public interface IOrderService
{
    Order? GetOrderById(int id);
    Order CreateOrder(int customerId, int adminUserId, string? status = null);
    void UpdateOrderStatus(int orderId, string status);
    void CancelOrder(int orderId);
    IEnumerable<Order> GetOrdersByCustomer(int customerId);
    void AddOrderItem(int orderId, int productId, int quantity, decimal unitPrice);
    IEnumerable<OrderItem> GetOrderItems(int orderId);
    

    OrderSummary? GetOrderSummaryById(int orderId);
    PagedResult<OrderSummary> GetAllOrderSummariesPaged(int pageNumber, int pageSize);
}

