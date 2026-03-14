using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ShopRAR.Domain;

namespace ShopRAR.BLL.SP;

public class SpOrderService : IOrderService
{
    private readonly string _connectionString;

    public SpOrderService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Order? GetOrderById(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetOrderById", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@OrderId", id);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var order = MapOrder(reader);
           
            if (order.TotalAmount == null || order.TotalAmount == 0)
            {
                using var conn2 = new SqlConnection(_connectionString);
                using var cmd2 = new SqlCommand(
                    "SELECT ISNULL(SUM(LineTotalAmount), 0) FROM OrderItems WHERE OrderId = @OrderId", 
                    conn2);
                cmd2.Parameters.AddWithValue("@OrderId", id);
                conn2.Open();
                var calculatedTotal = cmd2.ExecuteScalar();
                if (calculatedTotal != null && calculatedTotal != DBNull.Value)
                {
                    order.TotalAmount = Convert.ToDecimal(calculatedTotal);
                }
            }
            
            return order;
        }

        return null;
    }

    public Order CreateOrder(int customerId, int adminUserId, string? status = null)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_CreateOrder", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CustomerId", customerId);
        cmd.Parameters.AddWithValue("@AdminUserId", adminUserId);
        cmd.Parameters.AddWithValue("@Status", status ?? "Pending");

        var idParam = new SqlParameter("@NewOrderId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(idParam);

        conn.Open();
        cmd.ExecuteNonQuery();

        var order = new Order
        {
            OrderId = (int)idParam.Value,
            CustomerId = customerId,
            AdminUserId = adminUserId,
            Status = status ?? "Pending",
            OrderDate = DateTime.Now,
            TotalAmount = 0
        };

        return order;
    }

    public void UpdateOrderStatus(int orderId, string status)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_UpdateOrderStatus", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@OrderId", orderId);
        cmd.Parameters.AddWithValue("@Status", status);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void CancelOrder(int orderId)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_CancelOrder", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@OrderId", orderId);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public IEnumerable<Order> GetOrdersByCustomer(int customerId)
    {
        var result = new List<Order>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetOrdersByCustomer", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CustomerId", customerId);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(MapOrder(reader));
        }

        return result;
    }

    public void AddOrderItem(int orderId, int productId, int quantity, decimal unitPrice)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_AddOrderItem", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@OrderId", orderId);
        cmd.Parameters.AddWithValue("@ProductId", productId);
        cmd.Parameters.AddWithValue("@Quantity", quantity);
        cmd.Parameters.AddWithValue("@UnitPriceAtOrder", unitPrice);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public IEnumerable<OrderItem> GetOrderItems(int orderId)
    {
        var result = new List<OrderItem>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetOrderItems", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@OrderId", orderId);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(MapOrderItem(reader));
        }

        return result;
    }

    public OrderSummary? GetOrderSummaryById(int orderId)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetOrderSummaryById", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@OrderId", orderId);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapOrderSummary(reader);
        }

        return null;
    }

    public PagedResult<OrderSummary> GetAllOrderSummariesPaged(int pageNumber, int pageSize)
    {
        var result = new PagedResult<OrderSummary>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = new List<OrderSummary>()
        };

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetAllOrderSummariesPaged", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        
        var totalCountParam = new SqlParameter("@TotalCount", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(totalCountParam);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        var items = new List<OrderSummary>();
        while (reader.Read())
        {
            items.Add(MapOrderSummary(reader));
        }
       
        reader.Close();
        
        result.Items = items;
        result.TotalCount = totalCountParam.Value != DBNull.Value ? (int)totalCountParam.Value : 0;

        return result;
    }

    private static Order MapOrder(SqlDataReader reader)
    {
        return new Order
        {
            OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
            OrderDate = reader.IsDBNull(reader.GetOrdinal("OrderDate")) ? null : reader.GetDateTime(reader.GetOrdinal("OrderDate")),
            Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader.GetString(reader.GetOrdinal("Status")),
            TotalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount")) ? null : reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
            CustomerId = reader.IsDBNull(reader.GetOrdinal("CustomerId")) ? null : reader.GetInt32(reader.GetOrdinal("CustomerId")),
            AdminUserId = reader.IsDBNull(reader.GetOrdinal("AdminUserId")) ? null : reader.GetInt32(reader.GetOrdinal("AdminUserId"))
        };
    }

    private static OrderItem MapOrderItem(SqlDataReader reader)
    {
        return new OrderItem
        {
            OrderItemId = reader.GetInt32(reader.GetOrdinal("OrderItemId")),
            OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
            ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
            UnitPriceAtOrder = reader.GetDecimal(reader.GetOrdinal("UnitPriceAtOrder")),
            Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
            LineTotalAmount = reader.GetDecimal(reader.GetOrdinal("LineTotalAmount"))
        };
    }

    private static OrderSummary MapOrderSummary(SqlDataReader reader)
    {
        return new OrderSummary
        {
            OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
            OrderDate = reader.IsDBNull(reader.GetOrdinal("OrderDate")) ? null : reader.GetDateTime(reader.GetOrdinal("OrderDate")),
            Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader.GetString(reader.GetOrdinal("Status")),
            StoredTotalAmount = reader.IsDBNull(reader.GetOrdinal("StoredTotalAmount")) ? null : reader.GetDecimal(reader.GetOrdinal("StoredTotalAmount")),
            CalculatedTotalAmount = reader.GetDecimal(reader.GetOrdinal("CalculatedTotalAmount")),
            CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
            CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
            CustomerEmail = reader.IsDBNull(reader.GetOrdinal("CustomerEmail")) ? null : reader.GetString(reader.GetOrdinal("CustomerEmail")),
            AdminUserId = reader.IsDBNull(reader.GetOrdinal("AdminUserId")) ? null : reader.GetInt32(reader.GetOrdinal("AdminUserId")),
            AdminEmail = reader.IsDBNull(reader.GetOrdinal("AdminEmail")) ? null : reader.GetString(reader.GetOrdinal("AdminEmail"))
        };
    }
}

