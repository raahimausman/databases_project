using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ShopRAR.Domain;

namespace ShopRAR.BLL.SP;

public class SpReportingService : IReportingService
{
    private readonly string _connectionString;

    public SpReportingService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IEnumerable<TopSellingProduct> GetTopSellingProducts(int year, int month, int topN = 10)
    {
        var result = new List<TopSellingProduct>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetTopSellingProducts", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Year", year);
        cmd.Parameters.AddWithValue("@Month", month);
        cmd.Parameters.AddWithValue("@TopN", topN);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new TopSellingProduct
            {
                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                TotalQty = reader.GetInt32(reader.GetOrdinal("TotalQty")),
                TotalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue"))
            });
        }

        return result;
    }

    public PagedResult<CustomerLifetimeSpend> GetCustomerLifetimeSpendPaged(int pageNumber, int pageSize)
    {
        var result = new PagedResult<CustomerLifetimeSpend>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = new List<CustomerLifetimeSpend>()
        };

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetCustomerLifetimeSpendPaged", conn)
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
        var items = new List<CustomerLifetimeSpend>();
        while (reader.Read())
        {
            items.Add(new CustomerLifetimeSpend
            {
                CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                LifetimeSpend = reader.GetDecimal(reader.GetOrdinal("LifetimeSpend")),
                OrderCount = reader.GetInt32(reader.GetOrdinal("OrderCount")),
                LastOrderDate = reader.IsDBNull(reader.GetOrdinal("LastOrderDate")) 
                    ? null 
                    : reader.GetDateTime(reader.GetOrdinal("LastOrderDate"))
            });
        }
        
        reader.Close();
        
        result.Items = items;
        result.TotalCount = totalCountParam.Value != DBNull.Value ? (int)totalCountParam.Value : 0;

        return result;
    }
}

