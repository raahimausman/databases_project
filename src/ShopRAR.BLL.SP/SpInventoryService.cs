using System.Data;
using Microsoft.Data.SqlClient;
using ShopRAR.Domain;

namespace ShopRAR.BLL.SP;

public class SpInventoryService : IInventoryService
{
    private readonly string _connectionString;

    public SpInventoryService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Inventory? GetInventoryByProductId(int productId)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetInventoryByProductId", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ProductId", productId);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapInventory(reader);
        }

        return null;
    }

    public void UpdateInventory(int productId, int quantity)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_UpdateInventory", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ProductId", productId);
        cmd.Parameters.AddWithValue("@Quantity", quantity);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void AdjustInventory(int productId, int quantityChange)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_AdjustInventory", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ProductId", productId);
        cmd.Parameters.AddWithValue("@QuantityChange", quantityChange);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public int GetStockQuantity(int productId)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetStockQuantity", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ProductId", productId);

        var quantityParam = new SqlParameter("@Quantity", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(quantityParam);

        conn.Open();
        cmd.ExecuteNonQuery();

        return (int)quantityParam.Value;
    }

    public PagedResult<Inventory> GetLowStockProductsPaged(int threshold, int pageNumber, int pageSize)
    {
        var result = new PagedResult<Inventory>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = new List<Inventory>()
        };

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetLowStockProductsPaged", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Threshold", threshold);
        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        
        var totalCountParam = new SqlParameter("@TotalCount", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(totalCountParam);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        var items = new List<Inventory>();
        while (reader.Read())
        {
            items.Add(MapInventory(reader));
        }
       
        reader.Close();
        
        result.Items = items;
        result.TotalCount = totalCountParam.Value != DBNull.Value ? (int)totalCountParam.Value : 0;

        return result;
    }

    private static Inventory MapInventory(SqlDataReader reader)
    {
        return new Inventory
        {
            ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
            QuantityOnHand = reader.GetInt32(reader.GetOrdinal("QuantityOnHand"))
        };
    }
}

